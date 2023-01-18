// <copyright>
// Copyright by the Spark Development Network
//
// Licensed under the Rock Community License (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.rockrms.com/license
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
using System;
using System.ComponentModel.Composition;
using System.ComponentModel;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data.Entity;
using System.Linq;
using System.Web;
using Rock.Communication;
using Rock.Data;
using Rock.Lava;
using Rock.Model;
using Rock.Web.Cache;

namespace Rock.Security.Authentication
{
    /// <summary>
    /// Authenticates a user using Google
    /// </summary>
    /// <seealso cref="Rock.Security.AuthenticationComponent" />
    [Description( "Passwordless Authentication Provider" )]
    [Export( typeof( AuthenticationComponent ) )]
    [ExportMetadata( "ComponentName", "Passwordless Authentication" )]

    [SystemGuid.EntityTypeGuid( SystemGuid.EntityType.AUTHENTICATION_PASSWORDLESS )]
    internal class PasswordlessAuthentication : AuthenticationComponent
    {
        /// <inheritdoc/>
        public override bool RequiresRemoteAuthentication
        {
            get
            {
                return false;
            }
        }

        /// <inheritdoc/>
        public override AuthenticationServiceType ServiceType
        {
            get
            {
                // This must be external to allow passwordless UserLogins.
                return AuthenticationServiceType.External;
            }
        }

        /// <inheritdoc/>
        public override bool SupportsChangePassword
        {
            get
            {
                return false;
            }
        }

        /// <inheritdoc/>
        public override bool Authenticate( UserLogin user, string password )
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public override bool Authenticate( HttpRequest request, out string userName, out string returnUrl )
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public override bool Authenticate( string redirectUri, NameValueCollection queryString, out string userName, out string returnUrl )
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public override bool ChangePassword( UserLogin user, string oldPassword, string newPassword, out string warningMessage )
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public override string EncodePassword( UserLogin user, string password )
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public override Uri GenerateLoginUrl( HttpRequest request )
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public override Uri GenerateLoginUrl( string redirectUri, string returnUrl )
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the passwordless username given a <paramref name="userIdentifier"/>.
        /// </summary>
        /// <param name="userIdentifier">The user identifier to use when generating the passwordless username.</param>
        /// <returns>The passwordless username.</returns>
        public static string GetUsername( string userIdentifier )
        {
            return $"PASSWORDLESS_{userIdentifier}";
        }

        /// <inheritdoc/>
        public override string ImageUrl()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public override bool IsFromExternalAuthentication( NameValueCollection queryString )
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public override bool IsReturningFromAuthentication( HttpRequest request )
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Sends a one time passcode (OTP) via email or SMS.
        /// </summary>
        /// <param name="sendOneTimePasscodeOptions">The OTP options.</param>
        /// <param name="rockContext">The Rock context.</param>
        /// <returns>A result containing the encrypted passwordless state, if successful.</returns>
        internal SendOneTimePasscodeResult SendOneTimePasscode( SendOneTimePasscodeOptions sendOneTimePasscodeOptions, RockContext rockContext )
        {
            if ( !IsRequestValid( sendOneTimePasscodeOptions ) )
            {
                return new SendOneTimePasscodeResult
                {
                    IsSuccessful = false,
                    ErrorMessage = "Please provide Email or Phone for passwordless login.",
                    State = null
                };
            }

            var securitySettings = new SecuritySettingsService().SecuritySettings;

            var passwordlessSystemCommunication = new SystemCommunicationService( rockContext ).Get( securitySettings.PasswordlessConfirmationCommunicationTemplateGuid );

            var remoteAuthenticationSessionService = new RemoteAuthenticationSessionService( rockContext );

            if ( sendOneTimePasscodeOptions.ShouldSendSmsCode )
            {
                var uniqueIdentifier = PhoneNumber.CleanNumber( sendOneTimePasscodeOptions.PhoneNumber );
                var codeIssueDate = RockDateTime.Now;
                var remoteAuthenticationSession = remoteAuthenticationSessionService.StartRemoteAuthenticationSession( sendOneTimePasscodeOptions.IpAddress, securitySettings.PasswordlessSignInDailyIpThrottle, uniqueIdentifier, codeIssueDate, sendOneTimePasscodeOptions.OtpLifetime );

                var state = new PasswordlessAuthenticationState
                {
                    Code = remoteAuthenticationSession.Code,
                    CodeIssueDate = codeIssueDate,
                    CodeLifetime = sendOneTimePasscodeOptions.OtpLifetime,
                    Email = sendOneTimePasscodeOptions.Email,
                    PhoneNumber = sendOneTimePasscodeOptions.PhoneNumber,
                    UniqueIdentifier = uniqueIdentifier,
                };

                var smsMessage = new RockSMSMessage( passwordlessSystemCommunication )
                {
                    CreateCommunicationRecord = false
                };

                smsMessage.SetRecipients(
                    new List<RockSMSMessageRecipient>
                    {
                        RockSMSMessageRecipient.CreateAnonymous(
                            sendOneTimePasscodeOptions.PhoneNumber,
                            CombineMergeFields(
                                sendOneTimePasscodeOptions.CommonMergeFields,
                                new Dictionary<string, object>
                                {
                                    { "Code", state.Code }
                                } ) )
                    } );

                List<string> errorMessages;
                if ( smsMessage.Send(out errorMessages) )
                {
                    rockContext.SaveChanges();

                    return new SendOneTimePasscodeResult()
                    {
                        IsSuccessful = true,
                        ErrorMessage = null,
                        State = Encryption.EncryptString( state.ToJson() )
                    };
                }
                else
                {
                    return new SendOneTimePasscodeResult()
                    {
                        IsSuccessful = false,
                        ErrorMessage = $"{passwordlessSystemCommunication.Title} System Communication is not configured for SMS",
                        State = null
                    };
                } 
            }

            if ( sendOneTimePasscodeOptions.ShouldSendEmailCode || sendOneTimePasscodeOptions.ShouldSendEmailLink )
            {
                var uniqueIdentifier = sendOneTimePasscodeOptions.Email;
                var codeIssueDate = RockDateTime.Now;
                var remoteAuthenticationSession = remoteAuthenticationSessionService.StartRemoteAuthenticationSession( sendOneTimePasscodeOptions.IpAddress, securitySettings.PasswordlessSignInDailyIpThrottle, uniqueIdentifier, codeIssueDate, sendOneTimePasscodeOptions.OtpLifetime );

                var state = new PasswordlessAuthenticationState
                {
                    Code = remoteAuthenticationSession.Code,
                    CodeIssueDate = codeIssueDate,
                    CodeLifetime = sendOneTimePasscodeOptions.OtpLifetime,
                    Email = sendOneTimePasscodeOptions.Email,
                    PhoneNumber = sendOneTimePasscodeOptions.PhoneNumber,
                    UniqueIdentifier = uniqueIdentifier,
                };
                var encryptedState = Encryption.EncryptString( state.ToJson() );

                var emailMessage = new RockEmailMessage( passwordlessSystemCommunication )
                {
                    CreateCommunicationRecord = false
                };

                var isExistingPerson = GetMatchingPeopleQuery( rockContext, sendOneTimePasscodeOptions.PhoneNumber, sendOneTimePasscodeOptions.Email ).Any();

                var mergeFields = CombineMergeFields(
                    sendOneTimePasscodeOptions.CommonMergeFields,
                    new Dictionary<string, object>
                    {
                        { "IsNewPerson", !isExistingPerson },
                        { "LinkExpiration", LavaFilters.HumanizeTimeSpan(codeIssueDate, codeIssueDate + sendOneTimePasscodeOptions.OtpLifetime, 2 ) }
                    } );

                if ( sendOneTimePasscodeOptions.ShouldSendEmailCode )
                {
                    mergeFields.Add( "Code", state.Code );
                }

                if ( sendOneTimePasscodeOptions.ShouldSendEmailLink )
                {
                    var queryParams = new Dictionary<string, string>
                    {
                        { "Code", state.Code },
                        { "State", encryptedState },
                        { "IsPasswordless", true.ToString() }
                    };

                    if ( sendOneTimePasscodeOptions.PostAuthenticationRedirectUrl.IsNotNullOrWhiteSpace() )
                    {
                        queryParams.Add( "ReturnUrl", sendOneTimePasscodeOptions.PostAuthenticationRedirectUrl );
                    }

                    mergeFields.Add( "Link", sendOneTimePasscodeOptions.GetLink( queryParams ) );
                }

                emailMessage.SetRecipients( new List<RockEmailMessageRecipient>
                {
                    RockEmailMessageRecipient.CreateAnonymous( sendOneTimePasscodeOptions.Email, mergeFields )
                } );

                if ( emailMessage.Send() )
                {
                    rockContext.SaveChanges();

                    return new SendOneTimePasscodeResult()
                    {
                        IsSuccessful = true,
                        ErrorMessage = null,
                        State = encryptedState
                    };
                }
            }

            return new SendOneTimePasscodeResult
            {
                IsSuccessful = false,
                ErrorMessage = "Verification code failed to send",
                State = null
            };
        }

        /// <inheritdoc/>
        public override void SetPassword( UserLogin user, string password )
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Verifies a one-time passcode and authenticates the individual.
        /// </summary>
        /// <param name="verifyOneTimePasscodeOptions">The "verify OTP" options.</param>
        /// <param name="rockContext">The context.</param>
        /// <returns>The OTP verification result.</returns>
        internal VerifyOneTimePasscodeResult VerifyOneTimePasscode( VerifyOneTimePasscodeOptions verifyOneTimePasscodeOptions, RockContext rockContext )
        {
            if ( !IsRequestValid( verifyOneTimePasscodeOptions, out var state ) )
            {
                return new VerifyOneTimePasscodeResult
                {
                    ErrorMessage = "Request is invalid",
                    State = verifyOneTimePasscodeOptions.State
                };
            }

            if ( !IsOneTimePasscodeValid( rockContext, state, out var remoteAuthenticationSession ) )
            {
                return new VerifyOneTimePasscodeResult
                {
                    ErrorMessage = "Code is invalid",
                    State = verifyOneTimePasscodeOptions.State
                };
            }

            var user = GetExistingPasswordlessUser( rockContext, state.UniqueIdentifier );
            if ( user != null )
            {
                return AuthenticateExistingPasswordlessUser( rockContext, verifyOneTimePasscodeOptions, remoteAuthenticationSession, user );
            }
            else
            {
                return AuthenticateNewPasswordlessUser( rockContext, verifyOneTimePasscodeOptions, state, remoteAuthenticationSession );
            }
        }

        #region Private Methods

        /// <summary>
        /// Authenticates an existing passwordless user.
        /// </summary>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="verifyOneTimePasscodeOptions">The verify one time passcode options.</param>
        /// <param name="remoteAuthenticationSession">The remote authentication session.</param>
        /// <param name="user">The user.</param>
        /// <returns>The result of authenticating an existing passwordless user.</returns>
        private static VerifyOneTimePasscodeResult AuthenticateExistingPasswordlessUser( RockContext rockContext, VerifyOneTimePasscodeOptions verifyOneTimePasscodeOptions, RemoteAuthenticationSession remoteAuthenticationSession, UserLogin user )
        {
            if ( !IsPasswordlessAuthenticationAllowedForProtectionProfile( user.Person ) )
            {
                return new VerifyOneTimePasscodeResult
                {
                    ErrorMessage = "Passwordless login not available for your protection profile. Please request assistance from the organization administrator."
                };
            }

            CompleteRemoteAuthenticationSession( rockContext, remoteAuthenticationSession, user.Person );

            AuthenticatePasswordlessUser( user );

            return new VerifyOneTimePasscodeResult
            {
                IsAuthenticated = true,
                State = verifyOneTimePasscodeOptions.State
            };
        }

        /// <summary>
        /// Authenticates a new passwordless user.
        /// </summary>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="verifyOneTimePasscodeOptions">The verify one time passcode options.</param>
        /// <param name="state">The state.</param>
        /// <param name="remoteAuthenticationSession">The remote authentication session.</param>
        /// <returns>The result of authenticating a new passwordless user.</returns>
        private static VerifyOneTimePasscodeResult AuthenticateNewPasswordlessUser( RockContext rockContext, VerifyOneTimePasscodeOptions verifyOneTimePasscodeOptions, PasswordlessAuthenticationState state, RemoteAuthenticationSession remoteAuthenticationSession )
        {
            /*
               12/21/2022 - JMH

               If there are no existing people who match the phone number or email provided, then redirect the individual to the registration page.
               If there is exactly one person who matches, then authenticate the individual.
               If there are multiple people who match, then return the list of people for the individual to select from.
                   Once the individual selects the intended person, the request should contain a person id and this block action should run through the authentication process.

               Reason: Passwordless Sign In
             */

            var matchingPeople = GetMatchingPeople( rockContext, state.PhoneNumber, state.Email );

            if ( !matchingPeople.Any() )
            {
                return new VerifyOneTimePasscodeResult
                {
                    IsRegistrationRequired = true,
                    State = verifyOneTimePasscodeOptions.State
                };
            }

            var personService = new PersonService( rockContext );

            Person person = null;

            // If a matching person parameter was passed in, then verify that it is one of the matched people to prevent hijacking.
            if ( verifyOneTimePasscodeOptions.MatchingPersonValue.IsNotNullOrWhiteSpace() )
            {
                var matchingPersonState = Encryption.DecryptString( verifyOneTimePasscodeOptions.MatchingPersonValue ).FromJsonOrNull<PasswordlessMatchingPersonState>();

                person = matchingPersonState == null
                    ? null
                    : matchingPeople.FirstOrDefault( p => p.Id == matchingPersonState.PersonId );

                if ( person == null )
                {
                    return new VerifyOneTimePasscodeResult
                    {
                        ErrorMessage = "The selected person is invalid"
                    };
                }
            }
            else if ( matchingPeople.Count == 1 )
            {
                person = matchingPeople[0];
            }
            else
            {
                // Multiple people match phone number or email provided.
                // Individual must select the person they want to authenticate as.
                var matchingPersonResults = matchingPeople
                    .Select( p => new PasswordlessMatchingPersonState
                    {
                        PersonId = p.Id,
                        FullName = p.FullName
                    } )
                    .Select( p => new MatchingPersonResult
                    {
                        State = Encryption.EncryptString( p.ToJson() ),
                        FullName = p.FullName
                    } )
                    .ToList();

                var providedValues = new List<string>();

                if ( state.Email.IsNotNullOrWhiteSpace() )
                {
                    providedValues.Add( "email" );
                }

                if ( state.PhoneNumber.IsNotNullOrWhiteSpace() )
                {
                    providedValues.Add( "phone number" );
                }

                return new VerifyOneTimePasscodeResult
                {
                    ErrorMessage = $"The {( providedValues.Any() ? string.Join( " or ", providedValues ) : "data" )} you provided is matched to several different individuals. Please select the one that is you.",
                    IsPersonSelectionRequired = true,
                    MatchingPeopleResults = matchingPersonResults
                };
            }

            // If we made it here, we know who is attempting to authenticate.
            if ( !IsPasswordlessAuthenticationAllowedForProtectionProfile( person ) )
            {
                return new VerifyOneTimePasscodeResult
                {
                    ErrorMessage = "Passwordless login not available for your protection profile. Please request assistance from the organization administrator."
                };
            }

            var username = GetUsername( state.UniqueIdentifier );
            var user = person?.Users.FirstOrDefault( u => u.UserName == username );
            if ( user == null )
            {
                user = UserLoginService.Create( rockContext, person, AuthenticationServiceType.External, EntityTypeCache.Get( typeof( PasswordlessAuthentication ) ).Id, username, null, true );
            }

            CompleteRemoteAuthenticationSession( rockContext, remoteAuthenticationSession, person );

            AuthenticatePasswordlessUser( user );

            return new VerifyOneTimePasscodeResult
            {
                IsAuthenticated = true,
                State = verifyOneTimePasscodeOptions.State
            };
        }

        /// <summary>
        /// Authenticates the passwordless user.
        /// </summary>
        /// <param name="user">The user.</param>
        private static void AuthenticatePasswordlessUser( UserLogin user )
        {
            var securitySettings = new SecuritySettingsService().SecuritySettings;
            UserLoginService.UpdateLastLogin( user.UserName );
            Authorization.SetAuthCookie( user.UserName, true, false, TimeSpan.FromMinutes( securitySettings.PasswordlessSignInSessionDuration ) );
        }

        /// <summary>
        /// Combines the merge field dictionaries into a new dictionary.
        /// </summary>
        /// <param name="mergeFields">The merge fields.</param>
        /// <param name="additionalMergeFields">The additional merge fields.</param>
        /// <returns>The new dictionary with combined merge fields.</returns>
        private static Dictionary<string, object> CombineMergeFields( IDictionary<string, object> mergeFields, IDictionary<string, object> additionalMergeFields )
        {
            if ( mergeFields == null && additionalMergeFields == null )
            {
                return null;
            }

            if ( mergeFields == null )
            {
                return additionalMergeFields.ToDictionary( kvp => kvp.Key, kvp => kvp.Value );
            }

            if ( additionalMergeFields == null )
            {
                return mergeFields.ToDictionary( kvp => kvp.Key, kvp => kvp.Value );
            }

            var combinedMergeFields = mergeFields.ToDictionary( kvp => kvp.Key, kvp => kvp.Value );

            foreach ( var additionalMergeField in additionalMergeFields )
            {
                combinedMergeFields.Add( additionalMergeField.Key, additionalMergeField.Value );
            }

            return combinedMergeFields;
        }

        /// <summary>
        /// Completes the remote authentication session and saves the context.
        /// </summary>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="remoteAuthenticationSession">The remote authentication session.</param>
        /// <param name="person">The person.</param>
        private static void CompleteRemoteAuthenticationSession( RockContext rockContext, RemoteAuthenticationSession remoteAuthenticationSession, Person person )
        {
            var remoteAuthenticationSessionService = new RemoteAuthenticationSessionService( rockContext );
            remoteAuthenticationSessionService.CompleteRemoteAuthenticationSession( remoteAuthenticationSession, person.PrimaryAliasId.Value );
            rockContext.SaveChanges();
        }

        /// <summary>
        /// Gets the existing passwordless user.
        /// </summary>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="uniqueIdentifier">The unique identifier (phone or email).</param>
        /// <returns>The existing passwordless user or <c>null</c>.</returns>
        private static UserLogin GetExistingPasswordlessUser( RockContext rockContext, string uniqueIdentifier )
        {
            var userLoginService = new UserLoginService( rockContext );
            var username = GetUsername( uniqueIdentifier );
            return userLoginService.GetByUserName( username );
        }

        /// <summary>
        /// Gets the people who match phone or email with duplicates removed.
        /// </summary>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="phoneNumber">The phone number.</param>
        /// <param name="email">The email.</param>
        /// <returns>The list of matching people.</returns>
        private static List<Person> GetMatchingPeople( RockContext rockContext, string phoneNumber, string email )
        {
            var peopleQuery = GetMatchingPeopleQuery( rockContext, phoneNumber, email );

            return peopleQuery.ToList()
                .GroupBy( p => new
                {
                    FirstishName = p.NickName + p.FirstName,
                    p.LastName,
                    p.Age,
                    Role = p.GetFamilyRole()?.Id
                } )
                .Select( g => g.OrderBy( p => p.Id ).First() )
                .GroupBy( p => new
                {
                    p.NickName
                } )
                .Select( g => g.OrderBy( p => p.Id ).First() )
                .GroupBy( p => new
                {
                    p.FirstName
                } )
                .Select( g => g.OrderBy( p => p.Id ).First() )
                .ToList();
        }

        /// <summary>
        /// Gets the matching people query.
        /// </summary>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="phoneNumber">The phone number.</param>
        /// <param name="email">The email.</param>
        /// <returns>The matching people query.</returns>
        private static IQueryable<Person> GetMatchingPeopleQuery( RockContext rockContext, string phoneNumber, string email )
        {
            var personService = new PersonService( rockContext );
            var phoneNumberService = new PhoneNumberService( rockContext );
            IQueryable<Person> peopleQuery = null;

            if ( email.IsNotNullOrWhiteSpace() )
            {
                peopleQuery = personService.GetByEmail( email ).AsNoTracking();
            }

            if ( phoneNumber.IsNotNullOrWhiteSpace() )
            {
                if ( peopleQuery == null )
                {
                    peopleQuery = personService.Queryable().AsNoTracking();
                }

                var personIdsByPhoneNumber = phoneNumberService.GetPersonIdsByNumber( phoneNumber );

                peopleQuery = peopleQuery.Where( p => personIdsByPhoneNumber.Contains( p.Id ) );
            }

            return peopleQuery ?? Enumerable.Empty<Person>().AsQueryable();
        }

        /// <summary>
        /// Determines if the one-time passcode is valid.
        /// </summary>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="state">The state.</param>
        /// <param name="remoteAuthenticationSession">The remote authentication session set if the one-time passcode is valid.</param>
        private static bool IsOneTimePasscodeValid( RockContext rockContext, PasswordlessAuthenticationState state, out RemoteAuthenticationSession remoteAuthenticationSession )
        {
            var remoteAuthenticationSessionService = new RemoteAuthenticationSessionService( rockContext );
            remoteAuthenticationSession = remoteAuthenticationSessionService.VerifyRemoteAuthenticationSession( state.UniqueIdentifier, state.Code, state.CodeIssueDate, state.CodeLifetime );

            return remoteAuthenticationSession != null;
        }

        /// <summary>
        /// Determines whether passwordless authentication is allowed for <paramref name="person"/>.
        /// </summary>
        /// <param name="person">The person to check.</param>
        private static bool IsPasswordlessAuthenticationAllowedForProtectionProfile( Person person )
        {
            var securitySettings = new SecuritySettingsService().SecuritySettings;

            if ( securitySettings.DisablePasswordlessSignInForAccountProtectionProfiles.Contains( person.AccountProtectionProfile ) )
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Determines whether the passwordless login start request is valid.
        /// </summary>
        /// <param name="sendOneTimePasscodeRequest">The passwordless login request.</param>
        /// <returns>
        ///   <c>true</c> if the request is valid; otherwise, <c>false</c>.
        /// </returns>
        private static bool IsRequestValid( SendOneTimePasscodeOptions sendOneTimePasscodeRequest )
        {
            if ( sendOneTimePasscodeRequest == null )
            {
                return false;
            }

            // Individual must opt in to sending a code or a link via SMS or Email.
            if ( !sendOneTimePasscodeRequest.ShouldSendEmailCode
                 && !sendOneTimePasscodeRequest.ShouldSendEmailLink
                 && !sendOneTimePasscodeRequest.ShouldSendSmsCode )
            {
                return false;
            }

            if ( sendOneTimePasscodeRequest.ShouldSendSmsCode && sendOneTimePasscodeRequest.PhoneNumber.IsNullOrWhiteSpace() )
            {
                return false;
            }

            if ( ( sendOneTimePasscodeRequest.ShouldSendEmailCode || sendOneTimePasscodeRequest.ShouldSendEmailLink ) && sendOneTimePasscodeRequest.Email.IsNullOrWhiteSpace() )
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Determines whether the passwordless login start request is valid.
        /// </summary>
        /// <param name="verifyOneTimePasscodeOptions">The verify OTP options.</param>
        /// <param name="state">The passwordless authentication state that will be set if the request is valid.</param>
        /// <returns>
        ///   <c>true</c> if the request is valid; otherwise, <c>false</c>.
        /// </returns>
        private static bool IsRequestValid( VerifyOneTimePasscodeOptions verifyOneTimePasscodeOptions, out PasswordlessAuthenticationState state )
        {
            if ( verifyOneTimePasscodeOptions?.State.IsNullOrWhiteSpace() == true )
            {
                state = null;
                return false;
            }

            state = Encryption.DecryptString( verifyOneTimePasscodeOptions.State ).FromJsonOrNull<PasswordlessAuthenticationState>();

            if ( state == null )
            {
                return false;
            }

            if ( verifyOneTimePasscodeOptions.Code != state.Code )
            {
                return false;
            }

            return true;
        }

        #endregion

        #region Helper Classes

        /// <summary>
        /// State sent to client during passwordless login
        /// to ensure no tampering takes place.
        /// </summary>
        /// <remarks>Should be encrypted when sending.</remarks>
        internal class PasswordlessAuthenticationState
        {
            /// <summary>
            /// The unique identifier for the remote authentication session.
            /// </summary>
            public string UniqueIdentifier { get; set; }

            /// <summary>
            /// The email used to start the remote authentication session.
            /// </summary>
            public string Email { get; set; }

            /// <summary>
            /// The phone number used to start the remote authentication session.
            /// </summary>
            public string PhoneNumber { get; set; }

            /// <summary>
            /// The one-time passcode.
            /// </summary>
            public string Code { get; set; }

            /// <summary>
            /// The one-time passcode issue date.
            /// </summary>
            public DateTime CodeIssueDate { get; set; }

            /// <summary>
            /// The one-time passcode lifetime.
            /// </summary>
            public TimeSpan CodeLifetime { get; set; }
        }

        /// <summary>
        /// Class SendOneTimePasscodeRequest.
        /// </summary>
        internal class SendOneTimePasscodeOptions
        {
            /// <summary>
            /// Gets or sets the email.
            /// </summary>
            /// <value>The email.</value>
            public string Email { get; set; }

            /// <summary>
            /// Gets or sets the phone number.
            /// </summary>
            /// <value>The phone number.</value>
            public string PhoneNumber { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether to send email link.
            /// </summary>
            /// <value><c>true</c> to send email link; otherwise, <c>false</c>.</value>
            public bool ShouldSendEmailLink { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether to send email code.
            /// </summary>
            /// <value><c>true</c> to send email code; otherwise, <c>false</c>.</value>
            public bool ShouldSendEmailCode { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether to send SMS code.
            /// </summary>
            /// <value><c>true</c> to send SMS code; otherwise, <c>false</c>.</value>
            public bool ShouldSendSmsCode { get; set; }

            /// <summary>
            /// The IP address that initiated the OTP request.
            /// </summary>
            public string IpAddress { get; set; }

            /// <summary>
            /// The OTP lifetime.
            /// </summary>
            public TimeSpan OtpLifetime { get; set; }

            /// <summary>
            /// The URL to redirect to after authentication is complete.
            /// </summary>
            public string PostAuthenticationRedirectUrl { get; set; }

            /// <summary>
            /// The common merge fields used for OTP communication.
            /// </summary>
            public Dictionary<string, object> CommonMergeFields { get; set; }

            /// <summary>
            /// The delegate used to generate a link to a page.
            /// </summary>
            /// <remarks>
            /// The argument passed to the delegate contains the unencoded query parameters to complete the OTP process.
            /// </remarks>
            public Func<IDictionary<string, string>, string> GetLink { get; internal set; }
        }

        /// <summary>
        /// The result from sending a one-time passcode.
        /// </summary>
        internal class SendOneTimePasscodeResult
        {
            /// <summary>
            /// Indicates whether the passwordless login start step was successful.
            /// </summary>
            /// <value>
            ///   <c>true</c> if the passwordless login start step was successful; otherwise, <c>false</c>.
            /// </value>
            public bool IsSuccessful { get; set; }

            /// <summary>
            /// The error message if the passwordless login start step was unsuccessful.
            /// </summary>
            public string ErrorMessage { get; set; }

            /// <summary>
            /// The auto-generated state value that should be sent during the passwordless login verify step.
            /// </summary>
            public string State { get; set; }
        }

        /// <summary>
        /// The options for verifying a one-time passcode.
        /// </summary>
        internal class VerifyOneTimePasscodeOptions
        {
            /// <summary>
            /// The encrypted state that was generated when the OTP was sent.
            /// </summary>
            public string State { get; set; }

            /// <summary>
            /// The encrypted matching person state that was generated when multiple people matched the OTP email/phone number.
            /// </summary>
            public string MatchingPersonValue { get; set; }

            /// <summary>
            /// The one-time passcode to verify.
            /// </summary>
            public string Code { get; set; }
        }

        /// <summary>
        /// The result from verifying a one-time passcode.
        /// </summary>
        internal class VerifyOneTimePasscodeResult
        {
            /// <summary>
            /// Gets or sets a value indicating whether the person is authenticated.
            /// </summary>
            /// <value><c>true</c> if the person is authenticated; otherwise, <c>false</c>.</value>
            public bool IsAuthenticated { get; set; }

            /// <summary>
            /// The error message.
            /// </summary>
            public string ErrorMessage { get; set; }

            /// <summary>
            /// Indicates whether account registration is required.
            /// </summary>
            /// <value>
            ///   <c>true</c> if account registration is required; otherwise, <c>false</c>.
            /// </value>
            public bool IsRegistrationRequired { get; set; }

            /// <summary>
            /// Indicates whether person selection is required.
            /// </summary>
            /// <value>
            ///   <c>true</c> if person selection is required; otherwise, <c>false</c>.
            /// </value>
            public bool IsPersonSelectionRequired { get; set; }

            /// <summary>
            /// The people matching the email or phone number.
            /// </summary>
            /// <remarks>Only set when multiple matches are found.</remarks>
            public List<MatchingPersonResult> MatchingPeopleResults { get; set; }

            /// <summary>
            /// Gets or sets the registration URL.
            /// </summary>
            public string RegistrationUrl { get; set; }

            /// <summary>
            /// The encrypted state that was generated when the OTP was sent.
            /// </summary>
            public string State { get; set; }
        }

        /// <summary>
        /// A matching person result.
        /// </summary>
        internal class MatchingPersonResult
        {
            /// <summary>
            /// The encrypted matching person state.
            /// </summary>
            public string State { get; set; }

            /// <summary>
            /// The full name of the matching person.
            /// </summary>
            public string FullName { get; set; }
        }

        /// <summary>
        /// The state for a passwordless matching person.
        /// </summary>
        private class PasswordlessMatchingPersonState
        {
            /// <summary>
            /// Gets or sets the person identifier.
            /// </summary>
            /// <value>
            /// The person identifier.
            /// </value>
            public int PersonId { get; set; }

            /// <summary>
            /// Gets or sets the full name.
            /// </summary>
            /// <value>
            /// The full name.
            /// </value>
            public string FullName { get; set; }
        }

        #endregion
    }
}
