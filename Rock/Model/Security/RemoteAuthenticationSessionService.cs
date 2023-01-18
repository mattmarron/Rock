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
using System.Linq;
using Rock.Data;

namespace Rock.Model
{
    public partial class RemoteAuthenticationSessionService : Service<RemoteAuthenticationSession>
    {
        #region Constants

        private static readonly Random Random = new Random();
        private const int GeneratedCodeLength = 6;
        private const int MaxCodeGenerationAttempts = 50;

        #endregion

        internal RemoteAuthenticationSession StartRemoteAuthenticationSession( string ipAddress, int passwordlessSignInDailyIpThrottle, string uniqueIdentifier, DateTime codeIssueDate, TimeSpan codeLifetime )
        {
            ValidateIpCountWithinLimits( ipAddress, passwordlessSignInDailyIpThrottle );

            var code = GenerateUsableCode( codeIssueDate, codeLifetime );

            var remoteAuthenticationSession = new RemoteAuthenticationSession
            {
                ClientIpAddress = ipAddress,
                Code = code,
                SessionStartDateTime = RockDateTime.Now,
                DeviceUniqueIdentifier = uniqueIdentifier
            };

            Add( remoteAuthenticationSession );

            return remoteAuthenticationSession;
        }

        internal RemoteAuthenticationSession VerifyRemoteAuthenticationSession( string uniqueIdentifier, string code, DateTime codeIssueDate, TimeSpan codeLifetime )
        {
            var now = RockDateTime.Now;

            var validatedRemoteAuthenticationSession = Queryable()
                .WhereIsActive( codeLifetime, now )
                .WhereUsingCode( code, codeIssueDate, codeLifetime )
                .Where( s => s.DeviceUniqueIdentifier != null && s.DeviceUniqueIdentifier == uniqueIdentifier )
                .OrderByDescending( s => s.SessionStartDateTime.Value )
                .FirstOrDefault();

            return validatedRemoteAuthenticationSession;
        }

        internal void CompleteRemoteAuthenticationSession( RemoteAuthenticationSession remoteAuthenticationSession, int authorizedPersonAliasId )
        {
            remoteAuthenticationSession.AuthorizedPersonAliasId = authorizedPersonAliasId;
            remoteAuthenticationSession.SessionEndDateTime = RockDateTime.Now;
        }

        #region Private Methods

        /// <summary>
        /// Validates that the client IP addressed used for this remote authentication session is within the allowed limit.
        /// </summary>
        /// <param name="ipAddress">The ip address.</param>
        /// <param name="ipLimit">The ip limit.</param>
        /// <exception cref="Rock.Model.RemoteAuthenticationIpLimitReachedException">if the IP limit has been reached</exception>
        private void ValidateIpCountWithinLimits( string ipAddress, int ipLimit )
        {
            var currentCount = Queryable()
                .Where( r => r.ClientIpAddress == ipAddress )
                .WasCreatedToday()
                .Count();

            if ( currentCount >= ipLimit )
            {
                throw new RemoteAuthenticationIpLimitReachedException();
            }
        }

        /// <summary>
        /// Generates a usable code.
        /// </summary>
        /// <param name="issueDate">The issue date of the new code.</param>
        /// <param name="lifetime">The code lifetime.</param>
        /// <returns>The generated code.</returns>
        /// <exception cref="RemoteAuthenticationCodeGenerationException">if unable to generate a usable code within the allowed generation attempt limit</exception>
        private string GenerateUsableCode( DateTime issueDate, TimeSpan lifetime )
        {
            var generationCount = 0;
            while ( generationCount < MaxCodeGenerationAttempts )
            {
                var code = RandomString( GeneratedCodeLength );
                generationCount++;

                if ( ContainsBadSequence( code ) )
                {
                    continue;
                }

                if ( !IsCodeAvailable( code, issueDate, lifetime) )
                {
                    continue;
                }

                return code;
            }

            throw new RemoteAuthenticationCodeGenerationException( $"Unable to generate a usable remote authentication code in {MaxCodeGenerationAttempts} attempts." );
        }

        private bool IsCodeAvailable( string code, DateTime issueDate, TimeSpan lifetime )
        {
            var areAnyActiveSessionsUsingCode = Queryable()
                .WhereIsActive( lifetime )
                .WhereUsingCode( code, issueDate, lifetime )
                .Any();
            return !areAnyActiveSessionsUsingCode;
        }

        /// <summary>
        /// Randomizes the string.
        /// </summary>
        /// <param name="length">The length.</param>
        /// <returns></returns>
        private static string RandomString( int length )
        {
            // Removed vowels to prevent bad words,
            // the number nine to prevent other immature references,
            // and other characters that can cause confusion.
            const string AllowedChars = "BCDFGHJKLMNPRSTXZ245678";
            return new string( Enumerable.Repeat( AllowedChars, length )
                .Select( s => s[Random.Next( s.Length )] ).ToArray() );
        }

        /// <summary>
        /// Determines whether [contains bad sequence] [the specified authentication code].
        /// </summary>
        /// <param name="value">The authentication code.</param>
        /// <returns>
        ///   <c>true</c> if [contains bad sequence] [the specified authentication code]; otherwise, <c>false</c>.
        /// </returns>
        private bool ContainsBadSequence( string value )
        {
            foreach ( var badSequence in AttendanceCodeService.NoGood )
            {
                if ( value.Contains( badSequence ) )
                {
                    return true;
                }
            }

            return false;
        }

        #endregion
    }

    /// <summary>
    /// The exception that is thrown when a usable remote authentication code failed to generate.
    /// </summary>
    public class RemoteAuthenticationCodeGenerationException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RemoteAuthenticationCodeGenerationException"/> class.
        /// </summary>
        public RemoteAuthenticationCodeGenerationException() :
            base( "Unable to generate a usable remote authentication code." )
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RemoteAuthenticationCodeGenerationException"/> class.
        /// </summary>
        /// <param name="message">The exception message.</param>
        public RemoteAuthenticationCodeGenerationException( string message ) :
            base( message )
        {
        }
    }

    /// <summary>
    /// The exception that is thrown when the remote authentication IP throttle limit has been reached.
    /// </summary>
    public class RemoteAuthenticationIpLimitReachedException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RemoteAuthenticationIpLimitReachedException"/> class.
        /// </summary>
        public RemoteAuthenticationIpLimitReachedException() :
            base( "Your IP address is over the maximum number of requests per day. Please request assistance from the organization administrator." )
        {
        }
    }
}
