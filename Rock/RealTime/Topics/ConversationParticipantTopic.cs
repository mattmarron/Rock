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
using System.Collections.Concurrent;
using System.Threading.Tasks;

using Rock.Data;
using Rock.Model;
using Rock.ViewModels.Communication;
using Rock.Web.Cache;

namespace Rock.RealTime.Topics
{
    [RealTimeTopic]
    internal class ConversationParticipantTopic : Topic<IConversationParticipant>
    {
        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();
        }

        public async Task JoinSmsNumber( Guid rockPhoneNumber )
        {
            var state = this.GetConnectionState<ConversationState>( Context.ConnectionId );
            var definedValue = DefinedValueCache.Get( rockPhoneNumber );

            if ( definedValue == null )
            {
                throw new RealTimeException( "Phone number was not found." );
            }

            using ( var rockContext = new RockContext() )
            {
                var person = Context.CurrentPersonId.HasValue
                    ? new PersonService( rockContext ).Get( Context.CurrentPersonId.Value )
                    : null;

                if ( !definedValue.IsAuthorized( Security.Authorization.VIEW, person ) )
                {
                    throw new RealTimeException( "You are not authorized for this phone number." );
                }

                // Multiple blocks could be monitoring the number, so increment
                // a join count and if it is our first one then join the channel.
                var newValue = state.JoinCount.AddOrUpdate( rockPhoneNumber, 1, ( key, oldValue ) => oldValue + 1 );

                if ( newValue == 1 )
                {
                    await Channels.AddToChannelAsync( Context.ConnectionId, GetChannelForPhoneNumber( definedValue ) );
                }
            }
        }

        public async Task LeaveSmsNumber( Guid rockPhoneNumber )
        {
            var state = this.GetConnectionState<ConversationState>( Context.ConnectionId );
            var definedValue = DefinedValueCache.Get( rockPhoneNumber );

            if ( definedValue == null )
            {
                throw new RealTimeException( "Phone number was not found." );
            }

            // Multiple blocks could be monitoring the number, so decrement
            // our join count for this number and if it hits zero then leave
            // the channel.
            var newValue = state.JoinCount.AddOrUpdate( rockPhoneNumber, 0, ( key, oldValue ) => oldValue > 0 ? oldValue - 1 : 0 );

            if ( newValue == 0 )
            {
                await Channels.RemoveFromChannelAsync( Context.ConnectionId, GetChannelForPhoneNumber( definedValue ) );
            }
        }

        /// <summary>
        /// Gets the channel name that should be used to send the message.
        /// </summary>
        /// <param name="conversationKey">The conversation key representing the conversation to be communicated with..</param>
        /// <returns>A string that represents the RealTime channel name.</returns>
        public static string GetChannelForConversationKey( string conversationKey )
        {
            var guid = CommunicationService.GetRockPhoneNumberGuidForConversationKey( conversationKey );

            if ( guid.HasValue )
            {
                return $"sms:{guid}";
            }
            else
            {
                throw new Exception( "Conversation key is not valid." );
            }
        }

        /// <summary>
        /// Gets the channel to use when sending messages for the Rock phone number.
        /// </summary>
        /// <param name="rockPhoneNumber">The rock phone number.</param>
        /// <returns>A string that represents the RealTime channel name.</returns>
        private static string GetChannelForPhoneNumber( DefinedValueCache rockPhoneNumber )
        {
            return $"sms:{rockPhoneNumber.Guid}";
        }

        private class ConversationState
        {
            public ConcurrentDictionary<Guid, int> JoinCount { get; } = new ConcurrentDictionary<Guid, int>();
        }
    }
}
