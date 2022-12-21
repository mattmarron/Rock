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
using System.Collections.Generic;
using System.Threading.Tasks;

using Rock.Data;
using Rock.Model;

namespace Rock.RealTime.Topics
{
    internal interface ITestCommunicationTransportTopic
    {
        Task SmsMessageSent( TestCommunicationTransportTopic.SmsMessage message );
    }

    [RealTimeTopic]
    internal class TestCommunicationTransportTopic : Topic<ITestCommunicationTransportTopic>
    {
        public override Task OnConnectedAsync()
        {
            using ( var rockContext = new RockContext() )
            {
                var transport = new EntityTypeService( rockContext ).Get( new Guid( "c50fb8f9-6ada-4c4b-88ee-ed7bc93b1819" ) );

                if ( transport == null )
                {
                    throw new RealTimeException( "Test SMS transport was not found." );
                }

                var currentPerson = new PersonService( rockContext ).Get( Context.CurrentPersonId ?? 0 );

                if ( !transport.IsAuthorized( Security.Authorization.ADMINISTRATE, currentPerson ) )
                {
                    throw new RealTimeException( "Not authorized to access test SMS transport." );
                }
            }

            return base.OnConnectedAsync();
        }

        public Task MessageReceived( SmsMessage message )
        {
            new Rock.Communication.Medium.Sms().ProcessResponse( message.ToNumber, message.FromNumber, message.Body, out var errorMessage );

            if ( errorMessage.IsNotNullOrWhiteSpace() )
            {
                throw new RealTimeException( errorMessage );
            }

            return Task.CompletedTask;
        }

        internal class SmsMessage
        {
            public string FromNumber { get; set; }

            public string ToNumber { get; set; }

            public string Body { get; set; }

            public List<string> AttachmentUrls { get; set; }
        }

        public static async Task PostSmsMessage( string toNumber, string fromNumber, string body )
        {
            await RealTimeHelper.GetTopicContext<ITestCommunicationTransportTopic>()
                .Clients
                .All
                .SmsMessageSent( new SmsMessage
                {
                    FromNumber = fromNumber,
                    ToNumber = toNumber,
                    Body = body
                } );
        }
    }
}
