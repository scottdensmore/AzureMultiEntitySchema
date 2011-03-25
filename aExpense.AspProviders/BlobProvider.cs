using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;

namespace AExpense.AspProviders
{
    internal class BlobProvider
    {
        private const string PathSeparator = "/";
        private static readonly RetryPolicy RetryPolicy = RetryPolicies.Retry(3, TimeSpan.FromSeconds(1));
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);
        private readonly CloudBlobClient client;
        private readonly string containerName;
        private readonly object lockObject = new object();
        private CloudBlobContainer container;

        internal BlobProvider(StorageCredentials info, Uri baseUri, string containerName)
        {
            this.containerName = containerName;
            client = new CloudBlobClient(baseUri.ToString(), info);
        }

        internal string ContainerUrl
        {
            get { return string.Join(PathSeparator, new[] {client.BaseUri.AbsolutePath, containerName}); }
        }

        public IEnumerable<IListBlobItem> ListBlobs(string folder)
        {
            var cloudBlobContainer = GetContainer();
            try
            {
                return cloudBlobContainer.ListBlobs().Where(blob => blob.Uri.PathAndQuery.StartsWith(cloudBlobContainer.Uri.LocalPath + "/" + folder));
            }
            catch (InvalidOperationException se)
            {
                Log.Write(EventKind.Error, "Error enumerating contents of folder {0} exists: {1}", ContainerUrl + PathSeparator + folder, se.Message);
                throw;
            }
        }

        internal bool DeleteBlob(string blobName)
        {
            var cloudBlobContainer = GetContainer();
            try
            {
                cloudBlobContainer.GetBlobReference(blobName).Delete();

                return true;
            }
            catch (InvalidOperationException se)
            {
                Log.Write(EventKind.Error, "Error deleting blob {0}: {1}", ContainerUrl + PathSeparator + blobName, se.Message);
                throw;
            }
        }

        internal bool DeleteBlobsWithPrefix(string prefix)
        {
            bool ret = true;

            var e = ListBlobs(prefix);
            if (e == null)
            {
                return true;
            }

            var props = e.GetEnumerator();

            while (props.MoveNext())
            {
                if (props.Current != null)
                {
                    if (!DeleteBlob(props.Current.Uri.ToString()))
                    {
                        // ignore this; it is possible that another thread could try to delete the blob
                        // at the same time
                        ret = false;
                    }
                }
            }

            return ret;
        }

        internal MemoryStream GetBlobContent(string blobName, out BlobProperties properties)
        {
            var blobContent = new MemoryStream();
            properties = GetBlobContent(blobName, blobContent);
            blobContent.Seek(0, SeekOrigin.Begin);
            return blobContent;
        }

        internal BlobProperties GetBlobContent(string blobName, Stream outputStream)
        {
            if (blobName == string.Empty)
            {
                return null;
            }

            var cloudBlobContainer = GetContainer();
            try
            {
                var blob = cloudBlobContainer.GetBlobReference(blobName);

                blob.DownloadToStream(outputStream);

                BlobProperties properties = blob.Properties;
                Log.Write(EventKind.Information, "Getting contents of blob {0}", ContainerUrl + PathSeparator + blobName);
                return properties;
            }
            catch (InvalidOperationException sc)
            {
                Log.Write(EventKind.Error, "Error getting contents of blob {0}: {1}", ContainerUrl + PathSeparator + blobName, sc.Message);
                throw;
            }
        }

        internal bool GetBlobContentsWithoutInitialization(string blobName, Stream outputStream, out BlobProperties properties)
        {
            var cloudBlobContainer = GetContainer();

            try
            {
                var blob = cloudBlobContainer.GetBlobReference(blobName);

                blob.DownloadToStream(outputStream);

                properties = blob.Properties;
                Log.Write(EventKind.Information, "Getting contents of blob {0}", client.BaseUri + PathSeparator + containerName + PathSeparator + blobName);
                return true;
            }
            catch (InvalidOperationException ex)
            {
                if (ex.InnerException is WebException)
                {
                    var webEx = ex.InnerException as WebException;
                    var resp = webEx.Response as HttpWebResponse;

                    if (resp != null)
                    {
                        if (resp.StatusCode == HttpStatusCode.NotFound)
                        {
                            properties = null;
                            return false;
                        }
                    }

                    throw;
                }

                throw;
            }
        }

        internal void UploadStream(string blobName, Stream output)
        {
            UploadStream(blobName, output, true);
        }

        internal bool UploadStream(string blobName, Stream output, bool overwrite)
        {
            var cloudBlobContainer = GetContainer();
            try
            {
                output.Position = 0; // Rewind to start
                Log.Write(EventKind.Information, "Uploading contents of blob {0}", ContainerUrl + PathSeparator + blobName);

                var blob = cloudBlobContainer.GetBlockBlobReference(blobName);

                blob.UploadFromStream(output);

                return true;
            }
            catch (InvalidOperationException se)
            {
                Log.Write(EventKind.Error, "Error uploading blob {0}: {1}", ContainerUrl + PathSeparator + blobName, se.Message);
                throw;
            }
        }

        private CloudBlobContainer GetContainer()
        {
            // we have to make sure that only one thread tries to create the container
            lock (lockObject)
            {
                if (container != null)
                {
                    return container;
                }

                try
                {
                    var cloudBlobContainer = new CloudBlobContainer(containerName, client);
                    var requestModifiers = new BlobRequestOptions
                                               {
                                                   Timeout = Timeout,
                                                   RetryPolicy = RetryPolicy
                                               };

                    cloudBlobContainer.CreateIfNotExist(requestModifiers);

                    container = cloudBlobContainer;

                    return container;
                }
                catch (InvalidOperationException se)
                {
                    Log.Write(EventKind.Error, "Error creating container {0}: {1}", ContainerUrl, se.Message);
                    throw;
                }
            }
        }
    }
}