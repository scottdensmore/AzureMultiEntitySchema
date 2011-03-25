using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration.Provider;
using System.Data.Services.Client;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Security;
using System.Web;
using System.Web.SessionState;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;

namespace AExpense.AspProviders
{
    public class TableStorageSessionStateProvider : SessionStateStoreProviderBase
    {
        private const int NumRetries = 3;
        private static readonly object ThisLock = new object();
        private readonly ProviderRetryPolicy providerRetry = ProviderRetryPolicies.RetryN(NumRetries, TimeSpan.FromSeconds(1));
        private readonly RetryPolicy tableRetry = RetryPolicies.Retry(NumRetries, TimeSpan.FromSeconds(1));
        private string applicationName;
        private BlobProvider blobProvider;
        private string containerName;
        private string tableName;
        private CloudTableClient tableStorage;

        public override SessionStateStoreData CreateNewStoreData(HttpContext context, int timeout)
        {
            return new SessionStateStoreData(
                new SessionStateItemCollection(),
                SessionStateUtility.GetSessionStaticObjects(context),
                timeout);
        }

        public override void CreateUninitializedItem(HttpContext context, string id, int timeout)
        {
            SecUtility.CheckParameter(ref id, true, true, false, ProviderConfiguration.MaxStringPropertySizeInChars, "id");
            if (timeout < 0)
            {
                throw new ArgumentException("Parameter timeout must be a non-negative integer!");
            }

            try
            {
                TableServiceContext svc = CreateDataServiceContext();
                var session = new SessionRow(id, applicationName)
                                  {
                                      Lock = 0,
                                      Initialized = false,
                                      Id = id,
                                      Timeout = timeout,
                                      ExpiresUtc = DateTime.UtcNow.AddMinutes(timeout)
                                  };

                svc.AddObject(tableName, session);
                svc.SaveChangesWithRetries();
            }
            catch (InvalidOperationException e)
            {
                var innerEx = e.InnerException as DataServiceClientException;
                if (innerEx != null && innerEx.StatusCode == (int) HttpStatusCode.Conflict)
                {
                    // the data already exists so we can return because we are reusing a session
                    return;
                }
                throw new ProviderException("Error accessing the data store.", e);
            }
        }

        public override void Dispose()
        {
        }

        public override void EndRequest(HttpContext context)
        {
            // no specific logic for ending requests in this provider
        }

        public override SessionStateStoreData GetItem(HttpContext context, string id, out bool locked, out TimeSpan lockAge, out object lockId, out SessionStateActions actions)
        {
            SecUtility.CheckParameter(ref id, true, true, false, ProviderConfiguration.MaxStringPropertySizeInChars, "id");

            SessionStateStoreData sessionStateStoreData = GetSession(context, id, out locked, out lockAge, out lockId, out actions, false);

            return sessionStateStoreData;
        }

        public override SessionStateStoreData GetItemExclusive(HttpContext context, string id, out bool locked, out TimeSpan lockAge, out object lockId,
                                                               out SessionStateActions actions)
        {
            SecUtility.CheckParameter(ref id, true, true, false, ProviderConfiguration.MaxStringPropertySizeInChars, "id");

            SessionStateStoreData sessionStateStoreData = GetSession(context, id, out locked, out lockAge, out lockId, out actions, true);
            return sessionStateStoreData;
        }

        public override void Initialize(string name, NameValueCollection config)
        {
            // Verify that config isn't null
            if (config == null)
            {
                throw new ArgumentNullException("config");
            }

            // Assign the provider a default name if it doesn't have one
            if (String.IsNullOrEmpty(name))
            {
                name = "TableServiceSessionStateProvider";
            }

            // Add a default "description" attribute to config if the
            // attribute doesn't exist or is empty
            if (string.IsNullOrEmpty(config["description"]))
            {
                config.Remove("description");
                config.Add("description", "Session state provider using table storage");
            }

            // Call the base class's Initialize method
            base.Initialize(name, config);

            bool allowInsecureRemoteEndpoints = ProviderConfiguration.GetBooleanValue(config, "allowInsecureRemoteEndpoints", false);

            // structure storage-related properties
            applicationName = ProviderConfiguration.GetStringValueWithGlobalDefault(
                config,
                "applicationName",
                ProviderConfiguration.DefaultProviderApplicationNameConfigurationString,
                ProviderConfiguration.DefaultProviderApplicationName,
                false);

            tableName = ProviderConfiguration.GetStringValueWithGlobalDefault(
                config,
                "sessionTableName",
                ProviderConfiguration.DefaultSessionTableNameConfigurationString,
                ProviderConfiguration.DefaultSessionTableName,
                false);

            containerName = ProviderConfiguration.GetStringValueWithGlobalDefault(
                config,
                "containerName",
                ProviderConfiguration.DefaultSessionContainerNameConfigurationString,
                ProviderConfiguration.DefaultSessionContainerName,
                false);

            if (!SecUtility.IsValidContainerName(containerName))
            {
                throw new ProviderException(
                    "The provider configuration for the TableStorageSessionStateProvider does not contain a valid container name. " +
                    "Please refer to the documentation for the concrete rules for valid container names." +
                    "The current container name is: " + containerName);
            }

            config.Remove("allowInsecureRemoteEndpoints");
            config.Remove("containerName");
            config.Remove("applicationName");
            config.Remove("sessionTableName");

            // Throw an exception if unrecognized attributes remain
            if (config.Count > 0)
            {
                string attr = config.GetKey(0);
                if (!String.IsNullOrEmpty(attr))
                {
                    throw new ProviderException("Unrecognized attribute: " + attr);
                }
            }

            CloudStorageAccount account = null;
            try
            {
                account = ProviderConfiguration.GetStorageAccount(ProviderConfiguration.DefaultStorageConfigurationString);
                SecUtility.CheckAllowInsecureEndpoints(allowInsecureRemoteEndpoints, account.Credentials, account.TableEndpoint);
                SecUtility.CheckAllowInsecureEndpoints(allowInsecureRemoteEndpoints, account.Credentials, account.BlobEndpoint);

                tableStorage = account.CreateCloudTableClient();
                tableStorage.RetryPolicy = tableRetry;

                lock (ThisLock)
                {
                    TableStorageExtensionMethods.CreateTableIfNotExist<SessionRow>(tableStorage, tableName);
                }

                blobProvider = new BlobProvider(account.Credentials, account.BlobEndpoint, containerName);
            }
            catch (SecurityException)
            {
                throw;
            }
            catch (Exception e)
            {
                string exceptionDescription = ProviderConfiguration.GetInitExceptionDescription(
                    account.Credentials,
                    account.TableEndpoint,
                    account.BlobEndpoint);
                string table = tableName ?? "no session table name specified";
                string container = containerName ?? "no container name specified";
                Log.Write(
                    EventKind.Error,
                    "Initialization of data service structures (tables and/or blobs) failed!" +
                    exceptionDescription + Environment.NewLine +
                    "Configured blob container: " + container + Environment.NewLine +
                    "Configured table name: " + table + Environment.NewLine +
                    e.Message + Environment.NewLine + e.StackTrace);
                throw new ProviderException(
                    "Initialization of data service structures (tables and/or blobs) failed!" +
                    "The most probable reason for this is that " +
                    "the storage endpoints are not configured correctly. Please look at the configuration settings " +
                    "in your .cscfg and Web.config files. More information about this error " +
                    "can be found in the logs when running inside the hosting environment or in the output " +
                    "window of Visual Studio.",
                    e);
            }
        }

        public override void InitializeRequest(HttpContext context)
        {
            // no specific logic for initializing requests in this provider
        }

        public override void ReleaseItemExclusive(HttpContext context, string id, object lockId)
        {
            SecUtility.CheckParameter(ref id, true, true, false, ProviderConfiguration.MaxStringPropertySizeInChars, "id");

            try
            {
                TableServiceContext svc = CreateDataServiceContext();
                SessionRow session = GetSession(id, svc);
                ReleaseItemExclusive(svc, session, lockId);
            }
            catch (InvalidOperationException e)
            {
                throw new ProviderException("Error accessing the data store!", e);
            }
        }

        public override void RemoveItem(HttpContext context, string id, object lockId, SessionStateStoreData item)
        {
            SecUtility.CheckParameter(ref id, true, true, false, ProviderConfiguration.MaxStringPropertySizeInChars, "id");

            try
            {
                TableServiceContext svc = CreateDataServiceContext();
                SessionRow session = GetSession(id, svc);
                if (session == null)
                {
                    return;
                }

                if (session.Lock != (int) lockId)
                {
                    return;
                }

                svc.DeleteObject(session);
                svc.SaveChangesWithRetries();
            }
            catch (InvalidOperationException e)
            {
                throw new ProviderException("Error accessing the data store!", e);
            }

            // delete associated blobs
            try
            {
                IEnumerable<IListBlobItem> e = blobProvider.ListBlobs(GetBlobNamePrefix(id));
                if (e == null)
                {
                    return;
                }

                IEnumerator<IListBlobItem> props = e.GetEnumerator();
                if (props == null)
                {
                    return;
                }

                while (props.MoveNext())
                {
                    if (props.Current != null)
                    {
                        if (!blobProvider.DeleteBlob(props.Current.Uri.ToString()))
                        {
                            // ignore this; it is possible that another thread could try to delete the blob
                            // at the same time
                        }
                    }
                }
            }
            catch (Exception e)
            {
                throw new ProviderException("Error accessing blob storage.", e);
            }
        }

        public override void ResetItemTimeout(HttpContext context, string id)
        {
            SecUtility.CheckParameter(ref id, true, true, false, ProviderConfiguration.MaxStringPropertySizeInChars, "id");

            providerRetry(() =>
                              {
                                  TableServiceContext svc = CreateDataServiceContext();
                                  SessionRow session = GetSession(id, svc);
                                  session.ExpiresUtc = DateTime.UtcNow.AddMinutes(session.Timeout);
                                  svc.UpdateObject(session);
                                  svc.SaveChangesWithRetries();
                              });
        }

        public override void SetAndReleaseItemExclusive(HttpContext context, string id, SessionStateStoreData item, object lockId, bool newItem)
        {
            SecUtility.CheckParameter(ref id, true, true, false, ProviderConfiguration.MaxStringPropertySizeInChars, "id");

            providerRetry(() =>
                              {
                                  TableServiceContext svc = CreateDataServiceContext();
                                  SessionRow session;

                                  if (!newItem)
                                  {
                                      session = GetSession(id, svc);
                                      if (session == null || session.Lock != (int) lockId)
                                      {
                                          return;
                                      }
                                  }
                                  else
                                  {
                                      session = new SessionRow(id, applicationName)
                                                    {
                                                        Lock = 1,
                                                        LockDateUtc = DateTime.UtcNow
                                                    };
                                  }

                                  session.Initialized = true;
                                  session.Timeout = item.Timeout;
                                  session.ExpiresUtc = DateTime.UtcNow.AddMinutes(session.Timeout);
                                  session.Locked = false;

                                  // yes, we always create a new blob here
                                  session.BlobName = GetBlobNamePrefix(id) + Guid.NewGuid().ToString("N");

                                  // Serialize the session and write the blob
                                  byte[] items, statics;
                                  SerializeSession(item, out items, out statics);
                                  string serializedItems = Convert.ToBase64String(items);
                                  string serializedStatics = Convert.ToBase64String(statics);
                                  var output = new MemoryStream();
                                  var writer = new StreamWriter(output);

                                  try
                                  {
                                      writer.WriteLine(serializedItems);
                                      writer.WriteLine(serializedStatics);
                                      writer.Flush();

                                      // for us, it shouldn't matter whether newItem is set to true or false
                                      // because we always create the entire blob and cannot append to an 
                                      // existing one
                                      blobProvider.UploadStream(session.BlobName, output);
                                      writer.Close();
                                      output.Close();
                                  }
                                  catch (Exception e)
                                  {
                                      if (!newItem)
                                      {
                                          ReleaseItemExclusive(svc, session, lockId);
                                      }

                                      throw new ProviderException("Error accessing the data store.", e);
                                  }
                                  finally
                                  {
                                      writer.Close();
                                      output.Close();
                                  }

                                  if (newItem)
                                  {
                                      svc.AddObject(tableName, session);
                                      svc.SaveChangesWithRetries();
                                  }
                                  else
                                  {
                                      // Unlock the session and save changes
                                      ReleaseItemExclusive(svc, session, lockId);
                                  }
                              });
        }

        public override bool SetItemExpireCallback(SessionStateItemExpireCallback expireCallback)
        {
            // This provider doesn't support expiration callbacks
            // so simply return false here
            return false;
        }

        private static SessionStateStoreData DeserializeSession(byte[] items, IEnumerable<byte> statics, int timeout)
        {
            SessionStateItemCollection itemCol;
            const HttpStaticObjectsCollection StaticCol = null;

            using (var memoryStream = new MemoryStream(items))
            {
                using (var binaryReader = new BinaryReader(memoryStream))
                {
                    bool hasItems = binaryReader.ReadBoolean();
                    itemCol = hasItems ? SessionStateItemCollection.Deserialize(binaryReader) : new SessionStateItemCollection();
                }
            }

            if (HttpContext.Current != null && HttpContext.Current.Application != null &&
                HttpContext.Current.Application.StaticObjects != null && HttpContext.Current.Application.StaticObjects.Count > 0)
            {
                throw new ProviderException("This provider does not support static session objects because of security-related hosting constraints.");
            }

            if (statics != null && statics.Count() > 0)
            {
                throw new ProviderException("This provider does not support static session objects because of security-related hosting constraints.");
            }

            return new SessionStateStoreData(itemCol, StaticCol, timeout);
        }

        private static void ReleaseItemExclusive(TableServiceContext svc, SessionRow session, object lockId)
        {
            if ((int) lockId != session.Lock)
            {
                return;
            }

            session.ExpiresUtc = DateTime.UtcNow.AddMinutes(session.Timeout);
            session.Locked = false;
            svc.UpdateObject(session);
            svc.SaveChangesWithRetries();
        }

        private static void SerializeSession(SessionStateStoreData store, out byte[] items, out byte[] statics)
        {
            bool hasItems = store.Items != null && store.Items.Count > 0;
            bool hasStaticObjects = store.StaticObjects != null && store.StaticObjects.Count > 0 && !store.StaticObjects.NeverAccessed;
            statics = new byte[0];

            using (var memoryStream = new MemoryStream())
            {
                using (var binaryWriter = new BinaryWriter(memoryStream))
                {
                    binaryWriter.Write(hasItems);
                    if (hasItems)
                    {
                        ((SessionStateItemCollection) store.Items).Serialize(binaryWriter);
                    }

                    items = memoryStream.ToArray();
                }
            }

            if (hasStaticObjects)
            {
                throw new ProviderException("Static objects are not supported in this provider because of security-related hosting constraints.");
            }
        }

        private TableServiceContext CreateDataServiceContext()
        {
            return tableStorage.GetDataServiceContext();
        }

        private string GetBlobNamePrefix(string id)
        {
            return string.Format(CultureInfo.InstalledUICulture, "{0}{1}", id, applicationName);
        }

        private SessionRow GetSession(string id, DataServiceContext context)
        {
            try
            {
                DataServiceQuery<SessionRow> queryObj = context.CreateQuery<SessionRow>(tableName);
                var query = (from session in queryObj
                             where session.PartitionKey == SecUtility.CombineToKey(applicationName, id)
                             select session).AsTableServiceQuery();
                IEnumerable<SessionRow> sessions = query.Execute();

                // enumerate the result and store it in a list
                var sessionList = new List<SessionRow>(sessions);
                if (sessionList.Count() == 1)
                {
                    return sessionList.First();
                }

                if (sessionList.Count() > 1)
                {
                    throw new ProviderException("Multiple sessions with the same name!");
                }

                return null;
            }
            catch (Exception e)
            {
                throw new ProviderException("Error accessing storage.", e);
            }
        }

        // we don't use the retry policy itself in this function because out parameters are not well handled by 
        // retry policies
        private SessionStateStoreData GetSession(HttpContext context, string id, out bool locked, out TimeSpan lockAge, out object lockId, out SessionStateActions actions,
                                                 bool exclusive)
        {
            SecUtility.CheckParameter(ref id, true, true, false, ProviderConfiguration.MaxStringPropertySizeInChars, "id");

            SessionRow session = null;

            int curRetry = 0;
            bool retry;

            // Assign default values to out parameters
            locked = false;
            lockId = null;
            lockAge = TimeSpan.Zero;
            actions = SessionStateActions.None;

            do
            {
                retry = false;
                try
                {
                    TableServiceContext svc = CreateDataServiceContext();
                    session = GetSession(id, svc);

                    // Assign default values to out parameters
                    locked = false;
                    lockId = null;
                    lockAge = TimeSpan.Zero;
                    actions = SessionStateActions.None;

                    // if the blob does not exist, we return null
                    // ASP.NET will call the corresponding method for creating the session
                    if (session == null)
                    {
                        return null;
                    }

                    if (session.Initialized == false)
                    {
                        actions = SessionStateActions.InitializeItem;
                    }

                    session.ExpiresUtc = DateTime.UtcNow.AddMinutes(session.Timeout);
                    if (exclusive)
                    {
                        if (!session.Locked)
                        {
                            if (session.Lock == Int32.MaxValue)
                            {
                                session.Lock = 0;
                            }
                            else
                            {
                                session.Lock++;
                            }

                            session.LockDateUtc = DateTime.UtcNow;
                        }

                        lockId = session.Lock;
                        locked = session.Locked;
                        session.Locked = true;
                    }

                    lockAge = DateTime.UtcNow.Subtract(session.LockDateUtc);
                    lockId = session.Lock;

                    if (locked)
                    {
                        return null;
                    }

                    // let's try to write this back to the data store
                    // in between, someone else could have written something to the store for the same session
                    // we retry a number of times; if all fails, we throw an exception
                    svc.UpdateObject(session);
                    svc.SaveChangesWithRetries();
                }
                catch (InvalidOperationException e)
                {
                    // precondition fails indicates problems with the status code
                    // not found means we have had the session deleted from under us
                    if (e.InnerException is DataServiceClientException &&
                        (e.InnerException as DataServiceClientException).StatusCode == (int) HttpStatusCode.PreconditionFailed)
                    {
                        retry = true;
                    }
                    else if (e.InnerException is DataServiceClientException &&
                             (e.InnerException as DataServiceClientException).StatusCode == (int) HttpStatusCode.NotFound)
                    {
                        return null;
                    }
                    else
                    {
                        throw new ProviderException("Error accessing the data store.", e);
                    }
                }
            } while (retry && curRetry++ < NumRetries);

            // ok, now we have successfully written back our state
            // we can now read the blob 
            // we do not need to care about read/write locking when accessing the 
            // blob because each time we write a new session we create a new blob with a different name
            if (actions == SessionStateActions.InitializeItem || string.IsNullOrEmpty(session.BlobName))
            {
                // Return an empty SessionStateStoreData                    
                return new SessionStateStoreData(
                    new SessionStateItemCollection(),
                    SessionStateUtility.GetSessionStaticObjects(context),
                    session.Timeout);
            }
            try
            {
                BlobProperties properties;
                using (MemoryStream stream = blobProvider.GetBlobContent(session.BlobName, out properties))
                using (var reader = new StreamReader(stream))
                {
                    // Read Items, StaticObjects, and Timeout from the file
                    byte[] items = Convert.FromBase64String(reader.ReadLine());
                    byte[] statics = Convert.FromBase64String(reader.ReadLine());
                    int timeout = session.Timeout;

                    // Deserialize the session
                    return DeserializeSession(items, statics, timeout);
                }
            }
            catch (Exception e)
            {
                throw new ProviderException("Couldn't read session blob!", e);
            }
        }
    }
}