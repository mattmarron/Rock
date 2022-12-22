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
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;

using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Reporting;
using Rock.Web.Cache;

namespace Rock.Blocks.Types.Mobile.Communication
{
    /// <summary>
    /// Displays a single SMS conversation between Rock and individual.
    /// </summary>
    /// <seealso cref="Rock.Blocks.RockMobileBlockType" />

    [DisplayName( "SMS Conversation" )]
    [Category( "Mobile > Communication" )]
    [Description( "Displays a single SMS conversation between Rock and individual." )]
    [IconCssClass( "fa fa-comments" )]

    #region Block Attributes

    [IntegerField( "Database Timeout",
        Description = "The number of seconds to wait before reporting a database timeout.",
        IsRequired = false,
        DefaultIntegerValue = 180,
        Key = AttributeKey.DatabaseTimeoutSeconds,
        Order = 5 )]

    #endregion

    [Rock.SystemGuid.EntityTypeGuid( "99812f83-b514-4a76-a79d-01a97369f726" )]
    [Rock.SystemGuid.BlockTypeGuid( "4ef4250e-2d22-426c-adac-571c1301d18e" )]
    public class SmsConversation : RockMobileBlockType
    {
        #region Block Attributes

        /// <summary>
        /// The block setting attribute keys for the <see cref="SmsConversationList"/> block.
        /// </summary>
        private static class AttributeKey
        {
            public const string DatabaseTimeoutSeconds = "DatabaseTimeoutSeconds";
        }

        #endregion

        #region IRockMobileBlockType Implementation

        /// <summary>
        /// Gets the required mobile application binary interface version required to render this block.
        /// </summary>
        /// <value>
        /// The required mobile application binary interface version required to render this block.
        /// </value>
        public override int RequiredMobileAbiVersion => 5;

        /// <summary>
        /// Gets the class name of the mobile block to use during rendering on the device.
        /// </summary>
        /// <value>
        /// The class name of the mobile block to use during rendering on the device
        /// </value>
        public override string MobileBlockType => "Rock.Mobile.Blocks.Communication.SmsConversation";

        /// <summary>
        /// Gets the property values that will be sent to the device in the application bundle.
        /// </summary>
        /// <returns>
        /// A collection of string/object pairs.
        /// </returns>
        public override object GetMobileConfigurationValues()
        {
            return new
            {
            };
        }

        #endregion

        #region Methods

        internal static ConversationMessageBag ToMessageBag( CommunicationRecipientResponse response )
        {
            var bag = new ConversationMessageBag
            {
                ConversationKey = response.ConversationKey,
                MessageKey = response.MessageKey,
                ContactKey = response.ContactKey,
                MessageDateTime = response.CreatedDateTime,
                Message = response.SMSMessage,
                IsRead = response.IsRead,
                PersonAliasGuid = response.RecipientPersonAliasGuid.Value,
                FullName = response.FullName,
                IsNamelessPerson = response.IsNamelessPerson,
                IsOutbound = response.IsOutbound
            };

            if ( response.PhotoId.HasValue )
            {
                var publicUrl = GlobalAttributesCache.Get().GetValue( "PublicApplicationRoot" );

                bag.PhotoUrl = $"{publicUrl}GetImage.ashx?Id={response.PhotoId}&maxwidth=256&maxheight=256";
            }

            return bag;
        }

        #endregion

        #region Action Methods

        [BlockAction]
        public BlockActionResult GetConversationDetails( Guid phoneNumberGuid, Guid? personGuid = null, Guid? personAliasGuid = null )
        {
            var rockPhoneNumber = DefinedValueCache.Get( phoneNumberGuid );

            if ( rockPhoneNumber == null )
            {
                return ActionBadRequest( "Invalid Rock phone number specified." );
            }

            if ( !rockPhoneNumber.IsAuthorized( Rock.Security.Authorization.VIEW, RequestContext.CurrentPerson ) )
            {
                return ActionForbidden( "Not authorized to view messages for this phone number." );
            }

            using ( var rockContext = new RockContext() )
            {
                Person person = null;
                var publicUrl = GlobalAttributesCache.Get().GetValue( "PublicApplicationRoot" );
                string photoUrl = null;

                // Get the person via either their Guid or their alias Guid.
                if ( personGuid.HasValue )
                {
                    person = new PersonService( rockContext ).Queryable()
                        .Include( p => p.PhoneNumbers )
                        .Where( p => p.Guid == personGuid.Value )
                        .FirstOrDefault();
                }
                else if ( personAliasGuid.HasValue )
                {
                    person = new PersonAliasService( rockContext ).Queryable()
                        .Where( pa => pa.Guid == personAliasGuid.Value )
                        .Select( pa => pa.Person )
                        .Include( p => p.PhoneNumbers )
                        .FirstOrDefault();
                }

                // Make sure we actually found a person.
                if ( person == null )
                {
                    return ActionBadRequest( "Individual was not found." );
                }

                if ( person.PhotoId.HasValue )
                {
                    photoUrl = $"{publicUrl}GetImage.ashx?Id={person.PhotoId.Value}&maxwidth=512&maxheight=512";
                }

                var messages = new CommunicationResponseService( rockContext )
                    .GetCommunicationConversationForPerson( person.Id, rockPhoneNumber.Id )
                    .Select( r => ToMessageBag( r ) )
                    .ToList();

                var bag = new ConversationDetailBag
                {
                    FullName = person.FullName,
                    IsNamelessPerson = person.IsNameless(),
                    Messages = messages,
                    PhoneNumber = person.PhoneNumbers.GetFirstSmsNumber(),
                    PhotoUrl = photoUrl
                };

                return ActionOk( bag );
            }
        }

        /// <summary>
        /// Gets all messages for the specified conversation.
        /// </summary>
        /// <param name="phoneNumberGuid">The unique identifier of the phone number to retrieve conversations for.</param>
        /// <param name="personGuid">The unique identifier of the person to be communicated with.</param>
        /// <param name="personAliasGuid">The unique identifier of the person alias to be communicated with.</param>
        /// <returns>A collection of message objects or an HTTP error.</returns>
        [BlockAction]
        public BlockActionResult GetMessages( Guid phoneNumberGuid, Guid? personGuid, Guid? personAliasGuid )
        {
            var rockPhoneNumber = DefinedValueCache.Get( phoneNumberGuid );

            if ( rockPhoneNumber == null )
            {
                return ActionBadRequest( "Invalid Rock phone number specified." );
            }

            if ( !rockPhoneNumber.IsAuthorized( Rock.Security.Authorization.VIEW, RequestContext.CurrentPerson ) )
            {
                return ActionForbidden( "Not authorized to view messages for this phone number." );
            }

            try
            {
                using ( var rockContext = new RockContext() )
                {
                    rockContext.Database.CommandTimeout = GetAttributeValue( AttributeKey.DatabaseTimeoutSeconds ).AsIntegerOrNull() ?? 180;

                    var communicationResponseService = new CommunicationResponseService( rockContext );
                    int? recipientPersonId = null;

                    if ( personGuid.HasValue )
                    {
                        recipientPersonId = new PersonService( rockContext ).GetId( personGuid.Value );
                    }
                    else if ( personAliasGuid.HasValue )
                    {
                        recipientPersonId = new PersonAliasService( rockContext ).GetPersonId( personAliasGuid.Value );
                    }

                    if ( !recipientPersonId.HasValue )
                    {
                        return ActionBadRequest( "Unknown person." );
                    }

                    var messages = communicationResponseService
                        .GetCommunicationConversationForPerson( recipientPersonId.Value, rockPhoneNumber.Id )
                        .Select( r => ToMessageBag( r ) )
                        .ToList();

                    return ActionOk( messages );
                }
            }
            catch ( Exception ex )
            {
                ExceptionLogService.LogException( ex );

                if ( ReportingHelper.FindSqlTimeoutException( ex ) != null )
                {
                    return ActionInternalServerError( "Unable to load SMS messages in a timely manner. You can try again or adjust the timeout setting of this block." );
                }
                else
                {
                    return ActionInternalServerError( "An error occurred when loading SMS messages." );
                }
            }
        }

        #endregion

        #region Support Classes

        private class ConversationDetailBag
        {
            /// <summary>
            /// Gets or sets the full name of the person being communicated with.
            /// </summary>
            /// <value>
            /// The full name of the person being communicated with.
            /// </value>
            public string FullName { get; set; }

            /// <summary>
            /// Gets or sets the photo URL for the person. Value will be <c>null</c>
            /// if no photo is available.
            /// </summary>
            /// <value>The photo URL of the person.</value>
            public string PhotoUrl { get; set; }

            /// <summary>
            /// Gets or sets the phone number for the person.
            /// </summary>
            /// <value>The phone number for the person.</value>
            public string PhoneNumber { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether the recipient is a nameless person.
            /// </summary>
            /// <value><c>true</c> if the recipient is a nameless person; otherwise, <c>false</c>.</value>
            public bool IsNamelessPerson { get; set; }

            /// <summary>
            /// Gets or sets the initial messages to be displayed.
            /// </summary>
            /// <value>The initial messages to be displayed.</value>
            public List<ConversationMessageBag> Messages { get; set; }
        }

        internal class ConversationMessageBag
        {
            /// <summary>
            /// Gets or sets the unique identifier of the conversation that
            /// this message belongs to.
            /// </summary>
            /// <value>
            /// The unique identifier of the conversation.
            /// </value>
            public string ConversationKey { get; set; }

            /// <summary>
            /// Gets or sets the unique identifier for this message. This can
            /// be used to determine if the message has already been seen.
            /// </summary>
            /// <value>
            /// The unique identifier for this message.
            /// </value>
            public string MessageKey { get; set; }

            /// <summary>
            /// Gets or sets the contact key of the recipient. This would contain
            /// a phone number, e-mail address, or other transport specific key
            /// to allow communication.
            /// </summary>
            /// <value>The contact key of the recipient.</value>
            public string ContactKey { get; set; }

            /// <summary>
            /// Gets or sets the created date time of the most recent message.
            /// </summary>
            /// <value>
            /// The created date time of the most recent message.
            /// </value>
            public DateTimeOffset? MessageDateTime { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether the message was sent from Rock.
            /// </summary>
            /// <value>
            ///   <c>true</c> if message was sent from Rock; otherwise, <c>false</c>.
            /// </value>
            public bool IsOutbound { get; set; }

            /// <summary>
            /// Gets or sets the content of the most recent message.
            /// </summary>
            /// <value>The content of the most recent message.</value>
            public string Message { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether the most recent
            /// message has been read.
            /// </summary>
            /// <value>
            ///   <c>true</c> if the most recent message has been read; otherwise, <c>false</c>.
            /// </value>
            public bool IsRead { get; set; }

            /// <summary>
            /// Gets or sets the unique identifier of the person alias being
            /// communicated with.
            /// </summary>
            /// <value>The person alias unique identifier.</value>
            public Guid PersonAliasGuid { get; set; }

            /// <summary>
            /// Gets or sets the full name of the person being communicated with.
            /// </summary>
            /// <value>
            /// The full name of the person being communicated with.
            /// </value>
            public string FullName { get; set; }

            /// <summary>
            /// Gets or sets the photo URL for the person. Value will be <c>null</c>
            /// if no photo is available.
            /// </summary>
            /// <value>The photo URL of the person.</value>
            public string PhotoUrl { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether the recipient is a nameless person.
            /// </summary>
            /// <value><c>true</c> if the recipient is a nameless person; otherwise, <c>false</c>.</value>
            public bool IsNamelessPerson { get; set; }
        }

        #endregion
    }
}
