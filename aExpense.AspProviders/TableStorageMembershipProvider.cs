﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration.Provider;
using System.Data.Services.Client;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Security;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;

namespace AExpense.AspProviders
{
    public class TableStorageMembershipProvider : MembershipProvider
    {
        private const int MaxFindUserSize = 10000;
        private const int MaxTableEmailLength = 256;
        private const int MaxTablePasswordAnswerLength = 128;
        private const int MaxTablePasswordQuestionLength = 256;
        private const int MaxTablePasswordSize = 128;
        // this is the absolute minimum password size when generating new password
        // the number is chosen so that it corresponds to the SQL membership provider implementation
        private const int MinGeneratedPasswordSize = 14;
        private const int NumRetries = 3;

        // member variables shared between most providers
        private readonly object _lock = new object();
        // retry policies are used sparingly throughout this class because we often want to be
        // very conservative when it comes to security-related functions
        private readonly RetryPolicy _tableRetry = RetryPolicies.Retry(NumRetries, TimeSpan.FromSeconds(1));
        private string _applicationName;

        // membership provider specific member variables
        private bool _enablePasswordReset;
        private bool _enablePasswordRetrieval;
        private int _maxInvalidPasswordAttempts;
        private int _minRequiredNonalphanumericCharacters;
        private int _minRequiredPasswordLength;
        private int _passwordAttemptWindow;
        private MembershipPasswordFormat _passwordFormat;
        private string _passwordStrengthRegularExpression;
        private bool _requiresQuestionAndAnswer;
        private bool _requiresUniqueEmail;
        private string _tableName;
        private CloudTableClient _tableStorage;

        /// <summary>
        ///   The app name is not used in this implementation.
        /// </summary>
        public override string ApplicationName
        {
            get { return _applicationName; }
            set
            {
                lock (_lock)
                {
                    SecUtility.CheckParameter(ref value, true, true, true, Constants.MaxTableApplicationNameLength, "ApplicationName");
                    _applicationName = value;
                }
            }
        }

        public override bool EnablePasswordReset
        {
            get { return _enablePasswordReset; }
        }

        public override bool EnablePasswordRetrieval
        {
            get { return _enablePasswordRetrieval; }
        }

        public override int MaxInvalidPasswordAttempts
        {
            get { return _maxInvalidPasswordAttempts; }
        }

        public override int MinRequiredNonAlphanumericCharacters
        {
            get { return _minRequiredNonalphanumericCharacters; }
        }

        public override int MinRequiredPasswordLength
        {
            get { return _minRequiredPasswordLength; }
        }

        public override int PasswordAttemptWindow
        {
            get { return _passwordAttemptWindow; }
        }

        public override MembershipPasswordFormat PasswordFormat
        {
            get { return _passwordFormat; }
        }

        public override string PasswordStrengthRegularExpression
        {
            get { return _passwordStrengthRegularExpression; }
        }

        public override bool RequiresQuestionAndAnswer
        {
            get { return _requiresQuestionAndAnswer; }
        }

        public override bool RequiresUniqueEmail
        {
            get { return _requiresUniqueEmail; }
        }

        public virtual string GeneratePassword()
        {
            return Membership.GeneratePassword(
                MinRequiredPasswordLength < MinGeneratedPasswordSize ? MinGeneratedPasswordSize : MinRequiredPasswordLength,
                MinRequiredNonAlphanumericCharacters);
        }


        /// <summary>
        ///   Changes a users password. We don't use retries in this highly security-related function. 
        ///   All errors are exposed to the user of this function.
        /// </summary>
        public override bool ChangePassword(string username, string oldPassword, string newPassword)
        {
            SecUtility.CheckParameter(ref username, true, true, true, Constants.MaxTableUsernameLength, "username");
            SecUtility.CheckParameter(ref oldPassword, true, true, false, MaxTablePasswordSize, "oldPassword");
            SecUtility.CheckParameter(ref newPassword, true, true, false, MaxTablePasswordSize, "newPassword");

            try
            {
                MembershipRow member;
                TableServiceContext svc = CreateDataServiceContext();

                if (!CheckPassword(svc, username, oldPassword, false, false, out member))
                {
                    return false;
                }
                string salt = member.PasswordSalt;
                int passwordFormat = member.PasswordFormat;

                if (newPassword.Length < MinRequiredPasswordLength)
                {
                    throw new ArgumentException("The new password is to short.");
                }

                int count = 0;

                for (int i = 0; i < newPassword.Length; i++)
                {
                    if (!char.IsLetterOrDigit(newPassword, i))
                    {
                        count++;
                    }
                }

                if (count < MinRequiredNonAlphanumericCharacters)
                {
                    throw new ArgumentException("The new password does not have enough non-alphanumeric characters!");
                }

                if (PasswordStrengthRegularExpression.Length > 0)
                {
                    if (!Regex.IsMatch(newPassword, PasswordStrengthRegularExpression))
                    {
                        throw new ArgumentException("The new password does not match the specified password strength regular expression.");
                    }
                }

                string pass = EncodePassword(newPassword, passwordFormat, salt);
                if (pass.Length > MaxTablePasswordSize)
                {
                    throw new ArgumentException("Password is too long!");
                }

                ValidatePasswordEventArgs e = new ValidatePasswordEventArgs(username, newPassword, false);
                OnValidatingPassword(e);

                if (e.Cancel)
                {
                    if (e.FailureInformation != null)
                    {
                        throw e.FailureInformation;
                    }
                    else
                    {
                        throw new ArgumentException("Password validation failure!");
                    }
                }

                member.Password = pass;
                member.PasswordSalt = salt;
                member.PasswordFormat = passwordFormat;
                member.LastPasswordChangedDateUtc = DateTime.UtcNow;
                svc.UpdateObject(member);
                svc.SaveChanges();

                return true;
            }
            catch (InvalidOperationException e)
            {
                if (e.InnerException is DataServiceClientException)
                {
                    throw new ProviderException("Error accessing the data source.", e);
                }
                else
                {
                    throw;
                }
            }
        }

        /// <summary>
        ///   Change the password answer for a user.
        /// </summary>
        public override bool ChangePasswordQuestionAndAnswer(string username, string password, string newPasswordQuestion, string newPasswordAnswer)
        {
            SecUtility.CheckParameter(ref username, true, true, true, Constants.MaxTableUsernameLength, "username");
            SecUtility.CheckParameter(ref password, true, true, false, MaxTablePasswordSize, "password");

            try
            {
                MembershipRow member;
                TableServiceContext svc = CreateDataServiceContext();
                if (!CheckPassword(svc, username, password, false, false, out member))
                {
                    return false;
                }

                SecUtility.CheckParameter(ref newPasswordQuestion, RequiresQuestionAndAnswer, RequiresQuestionAndAnswer, false, MaxTablePasswordQuestionLength,
                                          "newPasswordQuestion");
                string encodedPasswordAnswer;
                if (newPasswordAnswer != null)
                {
                    newPasswordAnswer = newPasswordAnswer.Trim();
                }

                SecUtility.CheckParameter(ref newPasswordAnswer, RequiresQuestionAndAnswer, RequiresQuestionAndAnswer, false, MaxTablePasswordAnswerLength, "newPasswordAnswer");
                if (!string.IsNullOrEmpty(newPasswordAnswer))
                {
                    encodedPasswordAnswer = EncodePassword(newPasswordAnswer.ToLowerInvariant(), member.PasswordFormat, member.PasswordSalt);
                }
                else
                {
                    encodedPasswordAnswer = newPasswordAnswer;
                }
                SecUtility.CheckParameter(ref encodedPasswordAnswer, RequiresQuestionAndAnswer, RequiresQuestionAndAnswer, false, MaxTablePasswordAnswerLength,
                                          "newPasswordAnswer");

                member.PasswordQuestion = newPasswordQuestion;
                member.PasswordAnswer = encodedPasswordAnswer;

                svc.UpdateObject(member);
                svc.SaveChanges();
                return true;
            }
            catch (Exception e)
            {
                if (e.InnerException is DataServiceClientException)
                {
                    throw new ProviderException("Error accessing the data source.", e);
                }
                else
                {
                    throw;
                }
            }
        }

        /// <summary>
        ///   Creates a new user and stores it in the membership table. We do not use retry policies in this 
        ///   highly security-related function. All error conditions are directly exposed to the user.
        /// </summary>
        public override MembershipUser CreateUser(string username, string password, string email, string passwordQuestion,
                                                  string passwordAnswer, bool isApproved, object providerUserKey,
                                                  out MembershipCreateStatus status)
        {
            if (!SecUtility.ValidateParameter(ref password, true, true, false, MaxTablePasswordSize))
            {
                status = MembershipCreateStatus.InvalidPassword;
                return null;
            }

            string salt = GenerateSalt();
            string pass = EncodePassword(password, (int) _passwordFormat, salt);
            if (pass.Length > MaxTablePasswordSize)
            {
                status = MembershipCreateStatus.InvalidPassword;
                return null;
            }

            string encodedPasswordAnswer;
            if (passwordAnswer != null)
            {
                passwordAnswer = passwordAnswer.Trim();
            }

            if (!string.IsNullOrEmpty(passwordAnswer))
            {
                if (passwordAnswer.Length > MaxTablePasswordSize)
                {
                    status = MembershipCreateStatus.InvalidAnswer;
                    return null;
                }
                encodedPasswordAnswer = EncodePassword(passwordAnswer.ToLowerInvariant(), (int) _passwordFormat, salt);
            }
            else
            {
                encodedPasswordAnswer = passwordAnswer;
            }

            if (!SecUtility.ValidateParameter(ref encodedPasswordAnswer, RequiresQuestionAndAnswer, true, false, MaxTablePasswordSize))
            {
                status = MembershipCreateStatus.InvalidAnswer;
                return null;
            }

            if (!SecUtility.ValidateParameter(ref username, true, true, true, Constants.MaxTableUsernameLength))
            {
                status = MembershipCreateStatus.InvalidUserName;
                return null;
            }

            if (!SecUtility.ValidateParameter(ref email,
                                              RequiresUniqueEmail,
                                              RequiresUniqueEmail,
                                              false,
                                              Constants.MaxTableUsernameLength))
            {
                status = MembershipCreateStatus.InvalidEmail;
                return null;
            }

            if (!SecUtility.ValidateParameter(ref passwordQuestion, RequiresQuestionAndAnswer, true, false, Constants.MaxTableUsernameLength))
            {
                status = MembershipCreateStatus.InvalidQuestion;
                return null;
            }

            if (providerUserKey != null)
            {
                if (!(providerUserKey is Guid))
                {
                    status = MembershipCreateStatus.InvalidProviderUserKey;
                    return null;
                }
            }

            if (!EvaluatePasswordRequirements(password))
            {
                status = MembershipCreateStatus.InvalidPassword;
                return null;
            }

            ValidatePasswordEventArgs e = new ValidatePasswordEventArgs(username, password, true);
            OnValidatingPassword(e);

            if (e.Cancel)
            {
                status = MembershipCreateStatus.InvalidPassword;
                return null;
            }

            // Check whether a user with the same email address already exists.
            // The danger here is (as we don't have transaction support here) that 
            // there are overlapping requests for creating two users with the same email 
            // address at the same point in time. 
            // A solution for this would be to have a separate table for email addresses.
            // At this point here in the code we would try to insert this user's email address into the 
            // table and thus check whether the email is unique (the email would be the primary key of the 
            // separate table). There are quite some problems
            // associated with that. For example, what happens if the user creation fails etc., stale data in the 
            // email table etc.
            // Another solution is to already insert the user at this point and then check at the end of this 
            // funcation whether the email is unique. 
            if (RequiresUniqueEmail && !IsUniqueEmail(email))
            {
                status = MembershipCreateStatus.DuplicateEmail;
                return null;
            }

            try
            {
                TableServiceContext svc = CreateDataServiceContext();
                MembershipRow newUser = new MembershipRow(_applicationName, username);
                if (providerUserKey == null)
                {
                    providerUserKey = Guid.NewGuid();
                }
                newUser.UserId = (Guid) providerUserKey;
                newUser.Password = pass;
                newUser.PasswordSalt = salt;
                newUser.Email = (email == null) ? string.Empty : email;
                ;
                newUser.PasswordQuestion = (passwordQuestion == null) ? string.Empty : passwordQuestion;
                newUser.PasswordAnswer = (encodedPasswordAnswer == null) ? string.Empty : encodedPasswordAnswer;
                newUser.IsApproved = isApproved;
                newUser.PasswordFormat = (int) _passwordFormat;
                DateTime now = DateTime.UtcNow;
                newUser.CreateDateUtc = now;
                newUser.LastActivityDateUtc = now;
                newUser.LastPasswordChangedDateUtc = now;
                newUser.LastLoginDateUtc = now;
                newUser.IsLockedOut = false;

                svc.AddObject(_tableName, newUser);
                svc.SaveChanges();

                status = MembershipCreateStatus.Success;
                return new MembershipUser(Name,
                                          username,
                                          providerUserKey,
                                          email,
                                          passwordQuestion,
                                          null,
                                          isApproved,
                                          false,
                                          now.ToLocalTime(),
                                          now.ToLocalTime(),
                                          now.ToLocalTime(),
                                          now.ToLocalTime(),
                                          ProviderConfiguration.MinSupportedDateTime);
            }
            catch (InvalidOperationException ex)
            {
                if (ex.InnerException is DataServiceClientException && (ex.InnerException as DataServiceClientException).StatusCode == (int) HttpStatusCode.Conflict)
                {
                    // in this case, some membership providers update the last activity time of the user
                    // we don't do this in this implementation because it would add another roundtrip
                    status = MembershipCreateStatus.DuplicateUserName;
                    return null;
                }
                else if (ex.InnerException is DataServiceClientException)
                {
                    throw new ProviderException("Cannot add user to membership data store because of problems when accessing the data store.", ex);
                }
                else
                {
                    throw;
                }
            }
        }

        /// <summary>
        ///   Deletes the user from the membership table.
        ///   This implementation ignores the deleteAllRelatedData argument
        /// </summary>
        public override bool DeleteUser(string username, bool deleteAllRelatedData)
        {
            SecUtility.CheckParameter(ref username, true, true, true, Constants.MaxTableUsernameLength, "username");

            try
            {
                TableServiceContext svc = CreateDataServiceContext();
                MembershipRow user = new MembershipRow(_applicationName, username);
                svc.AttachTo(_tableName, user, "*");
                svc.DeleteObject(user);
                svc.SaveChangesWithRetries();
                return true;
            }
            catch (InvalidOperationException e)
            {
                if (e.InnerException is DataServiceClientException)
                {
                    var dsce = e.InnerException as DataServiceClientException;

                    if (dsce.StatusCode == (int) HttpStatusCode.NotFound)
                    {
                        return false;
                    }
                    else
                    {
                        throw new ProviderException("Error accessing the data source.", e);
                    }
                }
                else
                {
                    throw;
                }
            }
        }

        /// <summary>
        ///   Find users based on their email addresses.
        ///   The emailToMatch must be a complete email string like abc@def.com or can contain a '%' character at the end.
        ///   A '%' character at the end implies that arbitrary characters can follow. 
        ///   Supporting additional searches right now would be very expensive because the filtering would have to be done on the 
        ///   client side.
        /// </summary>
        [SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling", Justification = "Predefined interface.")]
        public override MembershipUserCollection FindUsersByEmail(string emailToMatch, int pageIndex, int pageSize, out int totalRecords)
        {
            SecUtility.CheckParameter(ref emailToMatch, false, false, false, Constants.MaxTableUsernameLength, "emailToMatch");

            if (pageIndex < 0)
            {
                throw new ArgumentException("Page index must be a non-negative integer.");
            }
            if (pageSize < 1)
            {
                throw new ArgumentException("Page size must be a positive integer.");
            }
            if (emailToMatch == null)
            {
                emailToMatch = string.Empty;
            }
            bool startswith = false;
            if (emailToMatch.Contains('%'))
            {
                if (emailToMatch.IndexOf('%') != emailToMatch.Length - 1)
                {
                    throw new ArgumentException("The TableStorageMembershipProvider only supports search strings that contain '%' as the last character!");
                }
                emailToMatch = emailToMatch.Substring(0, emailToMatch.Length - 1);
                startswith = true;
            }

            long upperBound = (long) pageIndex*pageSize + pageSize - 1;
            if (upperBound > Int32.MaxValue)
            {
                throw new ArgumentException("Cannot return so many elements!");
            }

            MembershipUserCollection users = new MembershipUserCollection();
            try
            {
                TableServiceContext svc = CreateDataServiceContext();
                DataServiceQuery<MembershipRow> queryObj = svc.CreateQuery<MembershipRow>(_tableName);

                CloudTableQuery<MembershipRow> query;
                if (startswith && string.IsNullOrEmpty(emailToMatch))
                {
                    query = (from user in queryObj
                             where user.PartitionKey.CompareTo(SecUtility.EscapedFirst(_applicationName)) > 0 &&
                                   user.PartitionKey.CompareTo(SecUtility.NextComparisonString(SecUtility.EscapedFirst(_applicationName))) < 0 &&
                                   user.ProfileIsCreatedByProfileProvider == false
                             select user).AsTableServiceQuery();
                }
                else if (startswith)
                {
                    // so far, the table storage service does not support StartsWith; thus, we retrieve all users whose email is "larger" than the one 
                    // specified and do the comparison locally
                    // this can result in significant overhead
                    query = (from user in queryObj
                             where user.PartitionKey.CompareTo(SecUtility.EscapedFirst(_applicationName)) > 0 &&
                                   user.PartitionKey.CompareTo(SecUtility.NextComparisonString(SecUtility.EscapedFirst(_applicationName))) < 0 &&
                                   user.ProfileIsCreatedByProfileProvider == false &&
                                   user.Email.CompareTo(emailToMatch) >= 0
                             select user).AsTableServiceQuery();
                }
                else
                {
                    query = (from user in queryObj
                             where user.PartitionKey.CompareTo(SecUtility.EscapedFirst(_applicationName)) > 0 &&
                                   user.PartitionKey.CompareTo(SecUtility.NextComparisonString(SecUtility.EscapedFirst(_applicationName))) < 0 &&
                                   user.ProfileIsCreatedByProfileProvider == false &&
                                   user.Email == emailToMatch
                             select user).AsTableServiceQuery();
                }

                IEnumerable<MembershipRow> allUsers = query.Execute();

                int startIndex = pageIndex*pageSize;
                int endIndex = startIndex + pageSize;
                int i = 0;
                List<MembershipRow> allUsersList = new List<MembershipRow>(allUsers);
                allUsersList.Sort(new EmailComparer());
                MembershipRow row;
                bool userMatches = true;
                for (i = startIndex; i < endIndex && i < allUsersList.Count && userMatches; i++)
                {
                    row = allUsersList.ElementAt(i);
                    Debug.Assert(emailToMatch != null);
                    if (startswith && !string.IsNullOrEmpty(emailToMatch))
                    {
                        if (!row.Email.StartsWith(emailToMatch, StringComparison.Ordinal))
                        {
                            userMatches = false;
                            continue;
                        }
                    }
                    users.Add(new MembershipUser(Name,
                                                 row.UserName,
                                                 row.UserId,
                                                 row.Email,
                                                 row.PasswordQuestion,
                                                 row.Comment,
                                                 row.IsApproved,
                                                 row.IsLockedOut,
                                                 row.CreateDateUtc.ToLocalTime(),
                                                 row.LastLoginDateUtc.ToLocalTime(),
                                                 row.LastActivityDateUtc.ToLocalTime(),
                                                 row.LastPasswordChangedDateUtc.ToLocalTime(),
                                                 row.LastLockoutDateUtc.ToLocalTime()));
                }
            }
            catch (Exception e)
            {
                if (e.InnerException is DataServiceClientException)
                {
                    throw new ProviderException("Error accessing the data source.", e);
                }
                else
                {
                    throw;
                }
            }

            totalRecords = users.Count;
            return users;
        }

        /// <summary>
        ///   Find users by their names.
        ///   The usernameToMatch must be the complete username like frank or can contain a '%' character at the end.
        ///   A '%' character at the end implies that arbitrary characters can follow. 
        ///   Supporting additional searches right now would be very expensive because the filtering would have to be done on the 
        ///   client side; i.e., all users would have to be retrieved in order to do the filtering.
        ///   IMPORTANT: because of this decision, user names must not contain a % character when using this function.
        /// </summary>
        public override MembershipUserCollection FindUsersByName(string usernameToMatch, int pageIndex, int pageSize, out int totalRecords)
        {
            SecUtility.CheckParameter(ref usernameToMatch, true, true, false, Constants.MaxTableUsernameLength, "usernameToMatch");

            if (pageIndex < 0)
            {
                throw new ArgumentException("Page index must be a non-negative integer.");
            }
            if (pageSize < 1)
            {
                throw new ArgumentException("Page size must be a positive integer.");
            }

            bool startswith = false;
            if (usernameToMatch.Contains('%'))
            {
                if (usernameToMatch.IndexOf('%') != usernameToMatch.Length - 1)
                {
                    throw new ArgumentException("The TableStorageMembershipProvider only supports search strings that contain '%' as the last character!");
                }
                usernameToMatch = usernameToMatch.Substring(0, usernameToMatch.Length - 1);
                startswith = true;
            }

            long upperBound = (long) pageIndex*pageSize + pageSize - 1;
            if (upperBound > Int32.MaxValue)
            {
                throw new ArgumentException("Cannot return so many elements!");
            }

            MembershipUserCollection users = new MembershipUserCollection();
            try
            {
                TableServiceContext svc = CreateDataServiceContext();
                DataServiceQuery<MembershipRow> queryObj = svc.CreateQuery<MembershipRow>(_tableName);

                CloudTableQuery<MembershipRow> query;
                if (startswith && string.IsNullOrEmpty(usernameToMatch))
                {
                    query = (from user in queryObj
                             where user.PartitionKey.CompareTo(SecUtility.EscapedFirst(_applicationName)) > 0 &&
                                   user.PartitionKey.CompareTo(SecUtility.NextComparisonString(SecUtility.EscapedFirst(_applicationName))) < 0 &&
                                   user.ProfileIsCreatedByProfileProvider == false
                             select user).AsTableServiceQuery();
                }
                else if (startswith)
                {
                    // note that we cannot include the usernameToMatch in the query over the partition key because the partitionkey is escaped, which destroys
                    // the sorting order
                    // and yes, we get all users here whose username is larger than the usernameToMatch because StartsWith is not supported in the current
                    // table storage service
                    query = (from user in queryObj
                             where user.PartitionKey.CompareTo(SecUtility.EscapedFirst(_applicationName)) > 0 &&
                                   user.PartitionKey.CompareTo(SecUtility.NextComparisonString(SecUtility.EscapedFirst(_applicationName))) < 0 &&
                                   user.UserName.CompareTo(usernameToMatch) >= 0 &&
                                   user.ProfileIsCreatedByProfileProvider == false
                             select user).AsTableServiceQuery();
                }
                else
                {
                    query = (from user in queryObj
                             where user.PartitionKey == SecUtility.CombineToKey(_applicationName, usernameToMatch) &&
                                   user.ProfileIsCreatedByProfileProvider == false
                             select user).AsTableServiceQuery();
                }

                IEnumerable<MembershipRow> allUsers = query.Execute();

                int startIndex = pageIndex*pageSize;
                int endIndex = startIndex + pageSize;
                int i;
                List<MembershipRow> allUsersList = new List<MembershipRow>(allUsers);
                // default sorting is by user name (not the escaped version in the partition key)
                allUsersList.Sort();
                MembershipRow row;
                bool userMatches = true;
                for (i = startIndex; i < endIndex && i < allUsersList.Count && userMatches; i++)
                {
                    row = allUsersList.ElementAt(i);
                    if (startswith && !string.IsNullOrEmpty(usernameToMatch))
                    {
                        if (!row.UserName.StartsWith(usernameToMatch, StringComparison.Ordinal))
                        {
                            userMatches = false;
                            continue;
                        }
                    }
                    users.Add(new MembershipUser(Name,
                                                 row.UserName,
                                                 row.UserId,
                                                 row.Email,
                                                 row.PasswordQuestion,
                                                 row.Comment,
                                                 row.IsApproved,
                                                 row.IsLockedOut,
                                                 row.CreateDateUtc.ToLocalTime(),
                                                 row.LastLoginDateUtc.ToLocalTime(),
                                                 row.LastActivityDateUtc.ToLocalTime(),
                                                 row.LastPasswordChangedDateUtc.ToLocalTime(),
                                                 row.LastLockoutDateUtc.ToLocalTime()));
                }
            }
            catch (Exception e)
            {
                if (e.InnerException is DataServiceClientException)
                {
                    throw new ProviderException("Error accessing the data source.", e);
                }
                else
                {
                    throw;
                }
            }

            totalRecords = users.Count;
            return users;
        }

        /// <summary>
        ///   Retrieves a collection of all the users.
        /// </summary>
        public override MembershipUserCollection GetAllUsers(int pageIndex, int pageSize, out int totalRecords)
        {
            if (pageIndex < 0)
            {
                throw new ArgumentException("The page index cannot be negative.");
            }
            if (pageSize < 1)
            {
                throw new ArgumentException("The page size can only be a positive integer.");
            }


            long upperBound = (long) pageIndex*pageSize + pageSize - 1;
            if (upperBound > Int32.MaxValue)
            {
                throw new ArgumentException("pageIndex and pageSize are too big.");
            }

            totalRecords = 0;
            MembershipUserCollection users = new MembershipUserCollection();
            TableServiceContext svc = CreateDataServiceContext();
            try
            {
                DataServiceQuery<MembershipRow> queryObj = svc.CreateQuery<MembershipRow>(_tableName);

                var query = (from user in queryObj
                             where user.PartitionKey.CompareTo(SecUtility.EscapedFirst(_applicationName)) > 0 &&
                                   user.PartitionKey.CompareTo(SecUtility.NextComparisonString(SecUtility.EscapedFirst(_applicationName))) < 0 &&
                                   user.ProfileIsCreatedByProfileProvider == false
                             select user).AsTableServiceQuery();

                IEnumerable<MembershipRow> allUsers = query.Execute();
                List<MembershipRow> allUsersSorted = new List<MembershipRow>(allUsers);

                // the result should already be sorted because the user name is part of the primary key
                // we have to sort anyway because we have encoded the user names in the key
                // this is also why we cannot use the table stoage pagination mechanism here and need to retrieve all elements
                // for every page
                allUsersSorted.Sort();

                int startIndex = pageIndex*pageSize;
                int endIndex = startIndex + pageSize;
                MembershipRow row;
                for (int i = startIndex; i < endIndex && i < allUsersSorted.Count; i++)
                {
                    row = allUsersSorted.ElementAt(i);
                    users.Add(new MembershipUser(Name,
                                                 row.UserName,
                                                 row.UserId,
                                                 row.Email,
                                                 row.PasswordQuestion,
                                                 row.Comment,
                                                 row.IsApproved,
                                                 row.IsLockedOut,
                                                 row.CreateDateUtc.ToLocalTime(),
                                                 row.LastLoginDateUtc.ToLocalTime(),
                                                 row.LastActivityDateUtc.ToLocalTime(),
                                                 row.LastPasswordChangedDateUtc.ToLocalTime(),
                                                 row.LastLockoutDateUtc.ToLocalTime()));
                }
            }
            catch (InvalidOperationException e)
            {
                if (e.InnerException is DataServiceClientException)
                {
                    throw new ProviderException("Error accessing the data source.", e);
                }
                else
                {
                    throw;
                }
            }
            totalRecords = users.Count;
            return users;
        }

        /// <summary>
        ///   Get the number of users that are currently online
        /// </summary>
        /// <returns></returns>
        public override int GetNumberOfUsersOnline()
        {
            TableServiceContext svc = CreateDataServiceContext();
            DataServiceQuery<MembershipRow> queryObj = svc.CreateQuery<MembershipRow>(_tableName);

            DateTime thresh = DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(Membership.UserIsOnlineTimeWindow));
            var query = (from user in queryObj
                         where user.PartitionKey.CompareTo(SecUtility.EscapedFirst(_applicationName)) > 0 &&
                               user.PartitionKey.CompareTo(SecUtility.NextComparisonString(SecUtility.EscapedFirst(_applicationName))) < 0 &&
                               user.LastActivityDateUtc > thresh &&
                               user.ProfileIsCreatedByProfileProvider == false
                         select user).AsTableServiceQuery();

            IEnumerable<MembershipRow> allUsers = query.Execute();
            if (allUsers == null)
            {
                return 0;
            }
            List<MembershipRow> allUsersList = new List<MembershipRow>(allUsers);
            if (allUsersList == null)
            {
                return 0;
            }
            return allUsersList.Count;
        }

        /// <summary>
        ///   Gets the password of a user given the provided password answer
        /// </summary>
        public override string GetPassword(string username, string answer)
        {
            if (!EnablePasswordRetrieval)
            {
                throw new NotSupportedException("Membership provider is configured to reject password retrieval.");
            }

            SecUtility.CheckParameter(ref username, true, true, true, Constants.MaxTableUsernameLength, "username");

            try
            {
                if (answer != null)
                {
                    answer = answer.Trim();
                }
                string encodedPasswordAnswer;
                DataServiceContext svc = CreateDataServiceContext();
                MembershipRow member = GetUserFromTable(svc, username);
                if (member == null)
                {
                    throw new ProviderException("Couldn't find a unique user with that name.");
                }
                if (member.IsLockedOut)
                {
                    throw new MembershipPasswordException("User is locked out.");
                }
                if (string.IsNullOrEmpty(answer))
                {
                    encodedPasswordAnswer = answer;
                }
                else
                {
                    encodedPasswordAnswer = EncodePassword(answer.ToLowerInvariant(), member.PasswordFormat, member.PasswordSalt);
                }
                SecUtility.CheckParameter(ref encodedPasswordAnswer, RequiresQuestionAndAnswer, RequiresQuestionAndAnswer, false, MaxTablePasswordAnswerLength,
                                          "passwordAnswer");

                Exception ex = null;
                if (RequiresQuestionAndAnswer)
                {
                    DateTime now = DateTime.UtcNow;
                    if (string.IsNullOrEmpty(member.PasswordAnswer) || encodedPasswordAnswer != member.PasswordAnswer)
                    {
                        ex = new MembershipPasswordException("Password answer is invalid.");
                        if (now > member.FailedPasswordAnswerAttemptWindowStartUtc.Add(TimeSpan.FromMinutes(PasswordAttemptWindow)))
                        {
                            member.FailedPasswordAnswerAttemptWindowStartUtc = now;
                            member.FailedPasswordAnswerAttemptCount = 1;
                        }
                        else
                        {
                            member.FailedPasswordAnswerAttemptWindowStartUtc = now;
                            member.FailedPasswordAnswerAttemptCount++;
                        }
                        if (member.FailedPasswordAnswerAttemptCount >= MaxInvalidPasswordAttempts)
                        {
                            member.IsLockedOut = true;
                            member.LastLockoutDateUtc = now;
                        }
                    }
                    else
                    {
                        if (member.FailedPasswordAnswerAttemptCount > 0)
                        {
                            member.FailedPasswordAnswerAttemptCount = 0;
                            member.FailedPasswordAnswerAttemptWindowStartUtc = ProviderConfiguration.MinSupportedDateTime;
                        }
                    }
                }
                svc.UpdateObject(member);
                svc.SaveChanges();
                if (ex != null)
                {
                    throw ex;
                }
                return UnEncodePassword(member.Password, member.PasswordFormat);
            }
            catch (Exception e)
            {
                if (e.InnerException is DataServiceClientException)
                {
                    throw new ProviderException("Error accessing the data source.", e);
                }
                else
                {
                    throw;
                }
            }
        }

        /// <summary>
        ///   Get a user based on the username parameter.
        ///   If the userIsOnline parameter is set the lastActivity flag of the user
        ///   is changed in the data store
        /// </summary>
        public override MembershipUser GetUser(object providerUserKey, bool userIsOnline)
        {
            if (providerUserKey == null)
            {
                throw new ArgumentNullException("providerUserKey");
            }

            if (providerUserKey.GetType() != typeof (Guid))
            {
                throw new ArgumentException("Provided key is not a Guid!");
            }
            Guid key = (Guid) providerUserKey;

            try
            {
                TableServiceContext svc = CreateDataServiceContext();
                DataServiceQuery<MembershipRow> queryObj = svc.CreateQuery<MembershipRow>(_tableName);

                // we need an IQueryable here because we do a Top(2) in the ProcessGetUserQuery() 
                // and cast it to DataServiceQuery object in this function
                // this does not work when we use IEnumerable as a type here
                IQueryable<MembershipRow> query = from user in queryObj
                                                  where user.PartitionKey.CompareTo(SecUtility.EscapedFirst(_applicationName)) > 0 &&
                                                        user.PartitionKey.CompareTo(SecUtility.NextComparisonString(SecUtility.EscapedFirst(_applicationName))) < 0 &&
                                                        user.UserId == key &&
                                                        user.ProfileIsCreatedByProfileProvider == false
                                                  select user;
                return ProcessGetUserQuery(svc, query, userIsOnline);
            }
            catch (InvalidOperationException e)
            {
                if (e.InnerException is DataServiceClientException)
                {
                    throw new ProviderException("Error accessing the data source.", e);
                }
                else
                {
                    throw;
                }
            }
        }

        /// <summary>
        ///   Retrieves a user based on his/her username.
        ///   The userIsOnline parameter determines whether to update the lastActivityDate of 
        ///   the user in the data store
        /// </summary>
        public override MembershipUser GetUser(string username, bool userIsOnline)
        {
            SecUtility.CheckParameter(
                ref username,
                true,
                false,
                true,
                Constants.MaxTableUsernameLength,
                "username");

            try
            {
                TableServiceContext svc = CreateDataServiceContext();
                DataServiceQuery<MembershipRow> queryObj = svc.CreateQuery<MembershipRow>(_tableName);

                // we need an IQueryable here because we do a Top(2) in the ProcessGetUserQuery() 
                // and cast it to DataServiceQuery object in this function
                // this does not work when we use IEnumerable as a type here
                IQueryable<MembershipRow> query = from user in queryObj
                                                  where user.PartitionKey == SecUtility.CombineToKey(_applicationName, username) &&
                                                        user.ProfileIsCreatedByProfileProvider == false
                                                  select user;
                return ProcessGetUserQuery(svc, query, userIsOnline);
            }
            catch (InvalidOperationException e)
            {
                if (e.InnerException is DataServiceClientException)
                {
                    throw new ProviderException("Error accessing the data source.", e);
                }
                else
                {
                    throw;
                }
            }
        }

        /// <summary>
        ///   Retrieves a username based on a matching email.
        /// </summary>
        public override string GetUserNameByEmail(string email)
        {
            SecUtility.CheckParameter(ref email, false, false, false, MaxTableEmailLength, "email");

            string nonNullEmail = (email == null) ? string.Empty : email;

            try
            {
                DataServiceContext svc = CreateDataServiceContext();
                DataServiceQuery<MembershipRow> queryObj = svc.CreateQuery<MembershipRow>(_tableName);

                var query = (from user in queryObj
                             where user.PartitionKey.CompareTo(SecUtility.EscapedFirst(_applicationName)) > 0 &&
                                   user.PartitionKey.CompareTo(SecUtility.NextComparisonString(SecUtility.EscapedFirst(_applicationName))) < 0 &&
                                   user.Email == nonNullEmail &&
                                   user.ProfileIsCreatedByProfileProvider == false
                             select user).AsTableServiceQuery();

                IEnumerable<MembershipRow> allUsers = query.Execute();
                if (allUsers == null)
                {
                    return null;
                }
                List<MembershipRow> allUsersList = new List<MembershipRow>(allUsers);
                if (allUsersList == null || allUsersList.Count < 1)
                {
                    return null;
                }
                if (allUsersList.Count > 1 && RequiresUniqueEmail)
                {
                    throw new ProviderException("No unique email address!");
                }
                MembershipRow firstMatch = allUsersList.ElementAt(0);
                return (string.IsNullOrEmpty(firstMatch.Email)) ? null : firstMatch.Email;
            }
            catch (InvalidOperationException e)
            {
                if (e.InnerException is DataServiceClientException)
                {
                    throw new ProviderException("Error accessing the data source.", e);
                }
                else
                {
                    throw;
                }
            }
        }

        /// <summary>
        ///   Initializes the membership provider. This is the only function that cannot be accessed
        ///   in parallel by multiple applications. The function reads the properties of the 
        ///   provider specified in the Web.config file and stores them in member variables.
        /// </summary>
        public override void Initialize(string name, NameValueCollection config)
        {
            if (config == null)
            {
                throw new ArgumentNullException("config");
            }

            if (String.IsNullOrEmpty(name))
            {
                name = "TableStorageMembershipProvider";
            }

            if (string.IsNullOrEmpty(config["description"]))
            {
                config.Remove("description");
                config.Add("description", "Table storage-based membership provider");
            }

            base.Initialize(name, config);

            bool allowInsecureRemoteEndpoints = ProviderConfiguration.GetBooleanValue(config, "allowInsecureRemoteEndpoints", false);
            _enablePasswordRetrieval = ProviderConfiguration.GetBooleanValue(config, "enablePasswordRetrieval", false);
            _enablePasswordReset = ProviderConfiguration.GetBooleanValue(config, "enablePasswordReset", true);
            _requiresQuestionAndAnswer = ProviderConfiguration.GetBooleanValue(config, "requiresQuestionAndAnswer", true);
            _requiresUniqueEmail = ProviderConfiguration.GetBooleanValue(config, "requiresUniqueEmail", true);
            _maxInvalidPasswordAttempts = ProviderConfiguration.GetIntValue(config, "maxInvalidPasswordAttempts", 5, false, 0);
            _passwordAttemptWindow = ProviderConfiguration.GetIntValue(config, "passwordAttemptWindow", 10, false, 0);
            _minRequiredPasswordLength = ProviderConfiguration.GetIntValue(config, "minRequiredPasswordLength", 7, false, MaxTablePasswordSize);
            _minRequiredNonalphanumericCharacters = ProviderConfiguration.GetIntValue(config, "minRequiredNonalphanumericCharacters", 1, true,
                                                                                                   MaxTablePasswordSize);

            _passwordStrengthRegularExpression = config["passwordStrengthRegularExpression"];
            if (_passwordStrengthRegularExpression != null)
            {
                _passwordStrengthRegularExpression = _passwordStrengthRegularExpression.Trim();
                if (_passwordStrengthRegularExpression.Length != 0)
                {
                    try
                    {
                        Regex testIfRegexIsValid = new Regex(_passwordStrengthRegularExpression);
                    }
                    catch (ArgumentException e)
                    {
                        throw new ProviderException(e.Message, e);
                    }
                }
            }
            else
            {
                _passwordStrengthRegularExpression = string.Empty;
            }

            if (_minRequiredNonalphanumericCharacters > _minRequiredPasswordLength)
            {
                throw new HttpException("The minRequiredNonalphanumericCharacters can not be greater than minRequiredPasswordLength.");
            }

            string strTemp = config["passwordFormat"];
            if (strTemp == null)
            {
                strTemp = "Hashed";
            }

            switch (strTemp)
            {
                case "Clear":
                    _passwordFormat = MembershipPasswordFormat.Clear;
                    break;
                case "Encrypted":
                    _passwordFormat = MembershipPasswordFormat.Encrypted;
                    break;
                case "Hashed":
                    _passwordFormat = MembershipPasswordFormat.Hashed;
                    break;
                default:
                    throw new ProviderException("Password format specified is invalid.");
            }

            if (PasswordFormat == MembershipPasswordFormat.Hashed && EnablePasswordRetrieval)
                throw new ProviderException(
                    "Configured settings are invalid: Hashed passwords cannot be retrieved. Either set the password format to different type, or set supportsPasswordRetrieval to false.");


            // Table storage-related properties
            _applicationName = ProviderConfiguration.GetStringValueWithGlobalDefault(config, "applicationName",
                                                                                                  ProviderConfiguration.DefaultProviderApplicationNameConfigurationString,
                                                                                                  ProviderConfiguration.DefaultProviderApplicationName, false);
            _tableName = ProviderConfiguration.GetStringValueWithGlobalDefault(config, "membershipTableName",
                                                                                            ProviderConfiguration.DefaultMembershipTableNameConfigurationString,
                                                                                            ProviderConfiguration.DefaultMembershipTableName, false);

            config.Remove("allowInsecureRemoteEndpoints");
            config.Remove("enablePasswordRetrieval");
            config.Remove("enablePasswordReset");
            config.Remove("requiresQuestionAndAnswer");
            config.Remove("requiresUniqueEmail");
            config.Remove("maxInvalidPasswordAttempts");
            config.Remove("passwordAttemptWindow");
            config.Remove("passwordFormat");
            config.Remove("minRequiredPasswordLength");
            config.Remove("minRequiredNonalphanumericCharacters");
            config.Remove("passwordStrengthRegularExpression");
            config.Remove("applicationName");
            config.Remove("membershipTableName");

            // Throw an exception if unrecognized attributes remain
            if (config.Count > 0)
            {
                string attr = config.GetKey(0);
                if (!String.IsNullOrEmpty(attr))
                    throw new ProviderException("Unrecognized attribute: " + attr);
            }

            CloudStorageAccount account = null;
            try
            {
                account = ProviderConfiguration.GetStorageAccount(ProviderConfiguration.DefaultStorageConfigurationString);

                SecUtility.CheckAllowInsecureEndpoints(allowInsecureRemoteEndpoints, account.Credentials, account.TableEndpoint);
                _tableStorage = account.CreateCloudTableClient();
                _tableStorage.RetryPolicy = _tableRetry;
                TableStorageExtensionMethods.CreateTableIfNotExist<MembershipRow>(_tableStorage, _tableName);
            }
            catch (SecurityException)
            {
                throw;
            }
            catch (Exception e)
            {
                string exceptionDescription = ProviderConfiguration.GetInitExceptionDescription(account.Credentials, account.TableEndpoint,
                                                                                                             "table storage configuration");
                string tableName = (_tableName == null) ? "no membership table name specified" : _tableName;
                Log.Write(EventKind.Error, "Could not create or find membership table: " + tableName + "!" + Environment.NewLine +
                                           exceptionDescription + Environment.NewLine +
                                           e.Message + Environment.NewLine + e.StackTrace);
                throw new ProviderException("Could not create or find membership table. The most probable reason for this is that " +
                                            "the storage endpoints are not configured correctly. Please look at the configuration settings " +
                                            "in your .cscfg and Web.config files. More information about this error " +
                                            "can be found in the logs when running inside the hosting environment or in the output " +
                                            "window of Visual Studio.", e);
            }
        }

        /// <summary>
        ///   Reset the password of a user. No retry policies are used in this function.
        /// </summary>
        public override string ResetPassword(string username, string answer)
        {
            if (!EnablePasswordReset)
            {
                throw new NotSupportedException("Membership provider is configured to not allow password resets!");
            }

            SecUtility.CheckParameter(ref username, true, true, true, Constants.MaxTableUsernameLength, "username");

            try
            {
                TableServiceContext svc = CreateDataServiceContext();
                MembershipRow member = GetUserFromTable(svc, username);
                if (member == null)
                {
                    throw new ProviderException(string.Format(CultureInfo.InstalledUICulture, "Couldn't find a unique user with the name {0}.", username));
                }
                if (member.IsLockedOut)
                {
                    throw new MembershipPasswordException(string.Format(CultureInfo.InstalledUICulture, "The user {0} is currently locked out!", username));
                }

                int passwordFormat = member.PasswordFormat;
                string salt = member.PasswordSalt;
                string encodedPasswordAnswer;

                if (answer != null)
                {
                    answer = answer.Trim();
                }
                if (!string.IsNullOrEmpty(answer))
                {
                    encodedPasswordAnswer = EncodePassword(answer.ToLowerInvariant(), passwordFormat, salt);
                }
                else
                {
                    encodedPasswordAnswer = answer;
                }
                SecUtility.CheckParameter(ref encodedPasswordAnswer, RequiresQuestionAndAnswer, RequiresQuestionAndAnswer, false, MaxTablePasswordSize, "passwordAnswer");

                string newPassword = GeneratePassword();
                ValidatePasswordEventArgs e = new ValidatePasswordEventArgs(username, newPassword, false);
                OnValidatingPassword(e);
                if (e.Cancel)
                {
                    if (e.FailureInformation != null)
                    {
                        throw e.FailureInformation;
                    }
                    else
                    {
                        throw new ProviderException("Password validation failed.");
                    }
                }

                DateTime now = DateTime.UtcNow;
                Exception ex = null;
                if (encodedPasswordAnswer == null || encodedPasswordAnswer == member.PasswordAnswer)
                {
                    member.Password = EncodePassword(newPassword, passwordFormat, salt);
                    member.LastPasswordChangedDateUtc = now;
                    if (member.FailedPasswordAnswerAttemptCount > 0 && encodedPasswordAnswer != null)
                    {
                        member.FailedPasswordAnswerAttemptCount = 0;
                        member.FailedPasswordAnswerAttemptWindowStartUtc = ProviderConfiguration.MinSupportedDateTime;
                    }
                }
                else
                {
                    if (now > member.FailedPasswordAnswerAttemptWindowStartUtc.Add(TimeSpan.FromMinutes(PasswordAttemptWindow)))
                    {
                        member.FailedPasswordAnswerAttemptWindowStartUtc = now;
                        member.FailedPasswordAnswerAttemptCount = 1;
                    }
                    else
                    {
                        member.FailedPasswordAnswerAttemptWindowStartUtc = now;
                        member.FailedPasswordAnswerAttemptCount++;
                    }
                    if (member.FailedPasswordAnswerAttemptCount >= MaxInvalidPasswordAttempts)
                    {
                        member.IsLockedOut = true;
                        member.LastLockoutDateUtc = now;
                    }
                    ex = new MembershipPasswordException("Wrong password answer.");
                }

                svc.UpdateObject(member);
                svc.SaveChanges();

                if (ex != null)
                {
                    throw ex;
                }
                return newPassword;
            }
            catch (Exception e)
            {
                if (e.InnerException is DataServiceClientException)
                {
                    throw new ProviderException("Error accessing the data source.", e);
                }
                else
                {
                    throw;
                }
            }
        }

        /// <summary>
        ///   Unlock a user
        /// </summary>
        public override bool UnlockUser(string userName)
        {
            SecUtility.CheckParameter(ref userName, true, true, true, Constants.MaxTableUsernameLength, "username");

            try
            {
                TableServiceContext svc = CreateDataServiceContext();
                MembershipRow member = GetUserFromTable(svc, userName);
                if (member == null)
                {
                    return false;
                }
                member.IsLockedOut = false;
                member.FailedPasswordAttemptCount = 0;
                member.FailedPasswordAttemptWindowStartUtc = ProviderConfiguration.MinSupportedDateTime;
                member.FailedPasswordAnswerAttemptCount = 0;
                member.FailedPasswordAnswerAttemptWindowStartUtc = ProviderConfiguration.MinSupportedDateTime;
                member.LastLockoutDateUtc = ProviderConfiguration.MinSupportedDateTime;
                svc.UpdateObject(member);
                svc.SaveChangesWithRetries();
                return true;
            }
            catch (Exception e)
            {
                if (e.InnerException is DataServiceClientException)
                {
                    throw new ProviderException("Error accessing the data source.", e);
                }
                else
                {
                    throw;
                }
            }
        }

        /// <summary>
        ///   Updates a user. The username will not be changed. We explicitly don't use a large retry policy statement between 
        ///   reading the user data and updating the user data.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1725:ParameterNamesShouldMatchBaseDeclaration",
            MessageId = "0#", Justification = "Code clarity.")]
        public override void UpdateUser(MembershipUser updatedUser)
        {
            if (updatedUser == null)
            {
                throw new ArgumentNullException("updatedUser");
            }

            try
            {
                string temp = updatedUser.UserName;
                SecUtility.CheckParameter(ref temp, true, true, true, Constants.MaxTableUsernameLength, "username");
                temp = updatedUser.Email;
                SecUtility.CheckParameter(ref temp, RequiresUniqueEmail, RequiresUniqueEmail, false, MaxTableEmailLength, "Email");
                updatedUser.Email = temp;

                MembershipRow member = null;
                if (RequiresUniqueEmail && !IsUniqueEmail(updatedUser.Email, out member) &&
                    member != null && member.UserName != updatedUser.UserName)
                {
                    throw new ProviderException("Not a unique email address!");
                }

                TableServiceContext svc = CreateDataServiceContext();
                DataServiceQuery<MembershipRow> queryObj = svc.CreateQuery<MembershipRow>(_tableName);

                var query = (from user in queryObj
                             where user.PartitionKey == SecUtility.CombineToKey(_applicationName, updatedUser.UserName) &&
                                   user.ProfileIsCreatedByProfileProvider == false
                             select user).AsTableServiceQuery();

                IEnumerable<MembershipRow> allUsers = query.Execute();

                if (allUsers == null)
                {
                    throw new ProviderException("Cannot update user. User not found.");
                }
                List<MembershipRow> allUsersList = new List<MembershipRow>(allUsers);
                if (allUsersList == null || allUsersList.Count != 1)
                {
                    throw new ProviderException("No or no unique user to update.");
                }
                MembershipRow userToUpdate = allUsersList.ElementAt(0);
                userToUpdate.Email = updatedUser.Email;
                userToUpdate.Comment = (updatedUser.Comment == null) ? string.Empty : updatedUser.Comment;
                userToUpdate.IsApproved = updatedUser.IsApproved;
                userToUpdate.LastLoginDateUtc = updatedUser.LastLoginDate.ToUniversalTime();
                userToUpdate.LastActivityDateUtc = updatedUser.LastActivityDate.ToUniversalTime();

                svc.UpdateObject(userToUpdate);
                svc.SaveChangesWithRetries();
            }
            catch (Exception e)
            {
                if (e.InnerException is DataServiceClientException)
                {
                    throw new ProviderException("Error accessing the data source.", e);
                }
                else
                {
                    throw;
                }
            }
        }

        /// <summary>
        ///   Returns true if the username and password match an exsisting user.
        ///   This implementation does not update a user's login time and does
        ///   not raise corresponding Web events
        /// </summary>
        public override bool ValidateUser(string username, string password)
        {
            if (SecUtility.ValidateParameter(ref username, true, true, true, Constants.MaxTableUsernameLength) &&
                SecUtility.ValidateParameter(ref password, true, true, false, MaxTablePasswordSize) &&
                CheckPassword(username, password, true, true))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private static string GenerateSalt()
        {
            byte[] buf = new byte[16];
            (new RNGCryptoServiceProvider()).GetBytes(buf);
            return Convert.ToBase64String(buf);
        }

        private bool CheckPassword(string username, string password, bool updateLastLoginActivityDate, bool failIfNotApproved)
        {
            MembershipRow member = null;
            return CheckPassword(username, password, updateLastLoginActivityDate, failIfNotApproved, out member);
        }

        private bool CheckPassword(string username, string password, bool updateLastLoginActivityDate, bool failIfNotApproved, out MembershipRow member)
        {
            return CheckPassword(null, username, password, updateLastLoginActivityDate, failIfNotApproved, out member);
        }

        private bool CheckPassword(DataServiceContext svc, string username, string password, bool updateLastLoginActivityDate, bool failIfNotApproved, out MembershipRow member)
        {
            bool createContextAndWriteState = false;
            try
            {
                if (svc == null)
                {
                    svc = CreateDataServiceContext();
                    createContextAndWriteState = true;
                }
                member = GetUserFromTable(svc, username);
                if (member == null)
                {
                    return false;
                }
                if (member.IsLockedOut)
                {
                    return false;
                }
                if (!member.IsApproved && failIfNotApproved)
                {
                    return false;
                }

                DateTime now = DateTime.UtcNow;
                string encodedPasswd = EncodePassword(password, member.PasswordFormat, member.PasswordSalt);

                bool isPasswordCorrect = member.Password.Equals(encodedPasswd);

                if (isPasswordCorrect && member.FailedPasswordAttemptCount == 0 && member.FailedPasswordAnswerAttemptCount == 0)
                {
                    if (createContextAndWriteState)
                    {
                        svc.UpdateObject(member);
                        svc.SaveChanges();
                    }
                    return true;
                }

                if (!isPasswordCorrect)
                {
                    if (now > member.FailedPasswordAttemptWindowStartUtc.Add(TimeSpan.FromMinutes(PasswordAttemptWindow)))
                    {
                        member.FailedPasswordAttemptWindowStartUtc = now;
                        member.FailedPasswordAttemptCount = 1;
                    }
                    else
                    {
                        member.FailedPasswordAttemptWindowStartUtc = now;
                        member.FailedPasswordAttemptCount++;
                    }
                    if (member.FailedPasswordAttemptCount >= MaxInvalidPasswordAttempts)
                    {
                        member.IsLockedOut = true;
                        member.LastLockoutDateUtc = now;
                    }
                }
                else
                {
                    if (member.FailedPasswordAttemptCount > 0 || member.FailedPasswordAnswerAttemptCount > 0)
                    {
                        member.FailedPasswordAnswerAttemptWindowStartUtc = ProviderConfiguration.MinSupportedDateTime;
                        member.FailedPasswordAnswerAttemptCount = 0;
                        member.FailedPasswordAttemptWindowStartUtc = ProviderConfiguration.MinSupportedDateTime;
                        member.FailedPasswordAttemptCount = 0;
                        member.LastLockoutDateUtc = ProviderConfiguration.MinSupportedDateTime;
                    }
                }
                if (isPasswordCorrect && updateLastLoginActivityDate)
                {
                    member.LastActivityDateUtc = now;
                    member.LastLoginDateUtc = now;
                }

                if (createContextAndWriteState)
                {
                    svc.UpdateObject(member);
                    svc.SaveChanges();
                }

                return isPasswordCorrect;
            }
            catch (Exception e)
            {
                if (e.InnerException is DataServiceClientException && (e.InnerException as DataServiceClientException).StatusCode == (int) HttpStatusCode.PreconditionFailed)
                {
                    // this element was changed between read and writes
                    Log.Write(EventKind.Warning, "A membership element has been changed between read and writes.");
                    member = null;
                    return false;
                }
                else
                {
                    throw new ProviderException("Error accessing the data store!", e);
                }
            }
        }

        private TableServiceContext CreateDataServiceContext()
        {
            return _tableStorage.GetDataServiceContext();
        }

        private string EncodePassword(string pass, int passwordFormat, string salt)
        {
            if (passwordFormat == 0)
            {
                // MembershipPasswordFormat.Clear
                return pass;
            }

            byte[] bIn = Encoding.Unicode.GetBytes(pass);
            byte[] bSalt = Convert.FromBase64String(salt);
            byte[] bAll = new byte[bSalt.Length + bIn.Length];
            byte[] bRet = null;

            Buffer.BlockCopy(bSalt, 0, bAll, 0, bSalt.Length);
            Buffer.BlockCopy(bIn, 0, bAll, bSalt.Length, bIn.Length);
            if (passwordFormat == 1)
            {
                // MembershipPasswordFormat.Hashed
                HashAlgorithm s = HashAlgorithm.Create(Membership.HashAlgorithmType);
                bRet = s.ComputeHash(bAll);
            }
            else
            {
                bRet = EncryptPassword(bAll);
            }

            return Convert.ToBase64String(bRet);
        }

        private bool EvaluatePasswordRequirements(string password)
        {
            if (password.Length < MinRequiredPasswordLength)
            {
                return false;
            }

            int count = 0;
            for (int i = 0; i < password.Length; i++)
            {
                if (!char.IsLetterOrDigit(password, i))
                {
                    count++;
                }
            }

            if (count < MinRequiredNonAlphanumericCharacters)
            {
                return false;
            }

            if (PasswordStrengthRegularExpression.Length > 0)
            {
                if (!Regex.IsMatch(password, PasswordStrengthRegularExpression))
                {
                    return false;
                }
            }
            return true;
        }

        private MembershipRow GetUserFromTable(DataServiceContext svc, string username)
        {
            SecUtility.CheckParameter(ref username, true, true, true, Constants.MaxTableUsernameLength, "username");

            DataServiceQuery<MembershipRow> queryObj = svc.CreateQuery<MembershipRow>(_tableName);

            var query = (from user in queryObj
                         where user.PartitionKey == SecUtility.CombineToKey(_applicationName, username) &&
                               user.ProfileIsCreatedByProfileProvider == false
                         select user).AsTableServiceQuery();

            IEnumerable<MembershipRow> allUsers = query.Execute();
            if (allUsers == null)
            {
                return null;
            }
            IEnumerator<MembershipRow> e = allUsers.GetEnumerator();
            if (e == null)
            {
                return null;
            }
            // e.Reset() throws a not implemented exception
            // according to the spec, the enumerator is at the beginning of the collections after a call to GetEnumerator()
            if (!e.MoveNext())
            {
                return null;
            }
            MembershipRow ret = e.Current;
            if (e.MoveNext())
            {
                throw new ProviderException("Duplicate elements for primary keys application and user name.");
            }
            return ret;
        }

        private bool IsUniqueEmail(string email)
        {
            MembershipRow member;
            return IsUniqueEmail(email, out member);
        }

        private bool IsUniqueEmail(string email, out MembershipRow member)
        {
            member = null;
            SecUtility.ValidateParameter(ref email, true, true, true, ProviderConfiguration.MaxStringPropertySizeInChars);

            TableServiceContext svc = CreateDataServiceContext();
            DataServiceQuery<MembershipRow> queryObj = svc.CreateQuery<MembershipRow>(_tableName);

            var query = (from user in queryObj
                         where user.PartitionKey.CompareTo(SecUtility.EscapedFirst(_applicationName)) > 0 &&
                               user.PartitionKey.CompareTo(SecUtility.NextComparisonString(SecUtility.EscapedFirst(_applicationName))) < 0 &&
                               user.Email == email &&
                               user.ProfileIsCreatedByProfileProvider == false
                         select user).AsTableServiceQuery();

            IEnumerable<MembershipRow> allUsers = query.Execute();
            if (allUsers == null)
            {
                return true;
            }
            IEnumerator<MembershipRow> e = allUsers.GetEnumerator();
            if (e == null)
            {
                return true;
            }
            // e.Reset() throws a not implemented exception
            // according to the spec, the enumerator is at the beginning of the collections after a call to GetEnumerator()

            if (!e.MoveNext())
            {
                return true;
            }
            else
            {
                member = e.Current;
            }
            return false;
        }

        private TimeSpan PasswordAttemptWindowAsTimeSpan()
        {
            return new TimeSpan(0, PasswordAttemptWindow, 0);
        }

        private MembershipUser ProcessGetUserQuery(TableServiceContext svc, IQueryable<MembershipRow> query, bool updateLastActivityDate)
        {
            if (query == null)
            {
                throw new ArgumentNullException("query");
            }

            // if no user is found, we return null
            MembershipUser res;

            // the GetUser query should return at most 1 result, we do a Take(2) to detect error conditions
            query = query.Take(2);

            IEnumerable<MembershipRow> qResult = query.AsTableServiceQuery().Execute();
            if (qResult == null)
            {
                return null;
            }
            var l = new List<MembershipRow>(qResult);
            if (l.Count == 0)
            {
                return null;
            }
            if (l.Count > 1)
            {
                throw new ProviderException("Non-unique primary keys!");
            }
            MembershipRow row = l.First();
            if (updateLastActivityDate)
            {
                row.LastActivityDateUtc = DateTime.UtcNow;
            }
            res = new MembershipUser(Name,
                                     row.UserName,
                                     row.UserId,
                                     row.Email,
                                     row.PasswordQuestion,
                                     row.Comment,
                                     row.IsApproved,
                                     row.IsLockedOut,
                                     row.CreateDateUtc.ToLocalTime(),
                                     row.LastLoginDateUtc.ToLocalTime(),
                                     row.LastActivityDateUtc.ToLocalTime(),
                                     row.LastPasswordChangedDateUtc.ToLocalTime(),
                                     row.LastLockoutDateUtc.ToLocalTime());

            if (updateLastActivityDate)
            {
                svc.UpdateObject(row);
                svc.SaveChangesWithRetries();
            }
            return res;
        }

        private string UnEncodePassword(string pass, int passwordFormat)
        {
            switch (passwordFormat)
            {
                case 0: // MembershipPasswordFormat.Clear:
                    return pass;
                case 1: // MembershipPasswordFormat.Hashed:
                    throw new ProviderException("Hashed password cannot be decrypted.");
                default:
                    byte[] bIn = Convert.FromBase64String(pass);
                    byte[] bRet = DecryptPassword(bIn);
                    if (bRet == null)
                    {
                        return null;
                    }
                    return Encoding.Unicode.GetString(bRet, 16, bRet.Length - 16);
            }
        }
    }
}