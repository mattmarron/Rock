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
using Rock.Communication;
using Rock.Data;
using Rock.Model;
using Rock.Reporting;
using Rock.ViewModels.Utility;
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

    [CustomDropdownListField( "Snippet Type",
        Description = "The type of snippets to make available when sending a message.",
        IsRequired = false,
        ListSource = "SELECT [Guid] AS [Value], [Name] AS [Text] FROM [SnippetType] ORDER BY [Name]",
        Key = AttributeKey.SnippetType,
        Order = 0 )]

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
            public const string SnippetType = "SnippetType";
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

        internal static ConversationMessageBag ToMessageBag( CommunicationRecipientResponse response, bool loadAttachments )
        {
            var publicUrl = GlobalAttributesCache.Get().GetValue( "PublicApplicationRoot" );

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
                IsOutbound = response.IsOutbound,
                OutboundSenderFullName = response.OutboundSenderFullName,
                Attachments = new List<ConversationAttachmentBag>()
            };

            if ( response.PhotoId.HasValue )
            {
                bag.PhotoUrl = $"{publicUrl}GetImage.ashx?Id={response.PhotoId}&maxwidth=256&maxheight=256";
            }

            if ( loadAttachments )
            {
                using ( var rockContext = new RockContext() )
                {
                    var attachmentGuids = response.GetBinaryFileGuids( rockContext );

                    foreach ( var guid in attachmentGuids )
                    {
                        bag.Attachments.Add( new ConversationAttachmentBag
                        {
                            Url = $"{publicUrl}GetImage.ashx?Guid={guid}",
                            ThumbnailUrl = $"{publicUrl}GetImage.ashx?Guid={guid}&maxwidth=512&maxheight=512"
                        } );
                    }
                }
            }

            return bag;
        }

        internal static List<ConversationMessageBag> ToMessageBags( ICollection<CommunicationRecipientResponse> responses )
        {
            var publicUrl = GlobalAttributesCache.Get().GetValue( "PublicApplicationRoot" );
            var attachments = new Dictionary<string, List<Guid>>();

            // Load the attachments for all responses in two queries rather
            // than executing a query for every single response.
            using ( var rockContext = new RockContext() )
            {
                var communicationIdMap = responses
                    .Where( r => r.CommunicationId.HasValue )
                    .ToDictionary( r => r.CommunicationId.Value, r => r.MessageKey );

                var communicationResponseIdMap = responses
                    .Where( r => !r.CommunicationId.HasValue && r.CommunicationResponseId.HasValue )
                    .ToDictionary( r => r.CommunicationResponseId.Value, r => r.MessageKey );

                if ( communicationIdMap.Count > 0 )
                {
                    var communicationIds = communicationIdMap.Keys.ToList();

                    var results = new CommunicationService( rockContext )
                        .Queryable()
                        .Where( c => communicationIds.Contains( c.Id ) )
                        .Select( c => new
                        {
                            c.Id,
                            AttachmentGuids = c.Attachments.Select( a => a.BinaryFile.Guid ).ToList()
                        } )
                        .ToList();

                    foreach ( var result in results )
                    {
                        attachments.AddOrReplace( communicationIdMap[result.Id], result.AttachmentGuids );
                    }
                }

                if ( communicationResponseIdMap.Count > 0 )
                {
                    var communicationResponseIds = communicationResponseIdMap.Keys.ToList();

                    var results = new CommunicationResponseService( rockContext )
                        .Queryable()
                        .Where( c => communicationResponseIds.Contains( c.Id ) )
                        .Select( c => new
                        {
                            c.Id,
                            AttachmentGuids = c.Attachments.Select( a => a.BinaryFile.Guid ).ToList()
                        } )
                        .ToList();

                    foreach ( var result in results )
                    {
                        attachments.AddOrReplace( communicationResponseIdMap[result.Id], result.AttachmentGuids );
                    }
                }
            }

            return responses
                .Select( response =>
                {
                    var bag = ToMessageBag( response, false );

                    if ( attachments.TryGetValue( bag.MessageKey, out var attachmentGuids ) )
                    {
                        foreach ( var guid in attachmentGuids )
                        {
                            bag.Attachments.Add( new ConversationAttachmentBag
                            {
                                Url = $"{publicUrl}GetImage.ashx?Guid={guid}",
                                ThumbnailUrl = $"{publicUrl}GetImage.ashx?Guid={guid}&maxwidth=512&maxheight=512"
                            } );
                        }
                    }

                    return bag;
                } )
                .ToList();
        }

        /// <summary>
        /// Gets the list item bags that represent the snippets which can be
        /// used by the individual.
        /// </summary>
        /// <param name="rockContext">The rock context to use when accessing the database.</param>
        /// <returns>A collection of bags that represent the snippets.</returns>
        private List<ListItemBag> GetSnippetBags( RockContext rockContext )
        {
            var snippetTypeGuid = GetAttributeValue( AttributeKey.SnippetType ).AsGuidOrNull();
            var currentPersonId = RequestContext.CurrentPerson?.Id;

            if ( !snippetTypeGuid.HasValue )
            {
                return new List<ListItemBag>();
            }

            return new SnippetService( rockContext )
                .GetAuthorizedSnippets( RequestContext.CurrentPerson,
                    s => s.SnippetType.Guid == snippetTypeGuid.Value )
                .OrderBy( s => s.Order )
                .ThenBy( s => s.Name )
                .Select( s =>
                {
                    var bag = s.ToListItemBag();

                    bag.Category = s.OwnerPersonAliasId.HasValue ? "Personal" : "Shared";

                    return bag;
                } )
                .ToList();
        }

        #endregion

        #region Action Methods

        [BlockAction]
        public BlockActionResult GetConversationDetails( Guid rockPhoneNumberGuid, Guid? personGuid = null, Guid? personAliasGuid = null )
        {
            var rockPhoneNumber = DefinedValueCache.Get( rockPhoneNumberGuid );

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

                var responses = new CommunicationResponseService( rockContext )
                    .GetCommunicationConversationForPerson( person.Id, rockPhoneNumber.Id );

                var messages = ToMessageBags( responses );
                var snippets = GetSnippetBags( rockContext );

                var bag = new ConversationDetailBag
                {
                    ConversationKey = $"SMS:{rockPhoneNumber.Guid}:{person.PrimaryAlias.Guid}",
                    FullName = person.FullName,
                    IsNamelessPerson = person.IsNameless(),
                    Messages = messages,
                    PersonGuid = person.Guid,
                    PhoneNumber = person.PhoneNumbers.GetFirstSmsNumber(),
                    PhotoUrl = photoUrl,
                    Snippets = snippets
                };

                return ActionOk( bag );
            }
        }

        /// <summary>
        /// Gets all messages for the specified conversation.
        /// </summary>
        /// <param name="rockPhoneNumberGuid">The unique identifier of the phone number to retrieve conversations for.</param>
        /// <param name="personGuid">The unique identifier of the person to be communicated with.</param>
        /// <param name="personAliasGuid">The unique identifier of the person alias to be communicated with.</param>
        /// <returns>A collection of message objects or an HTTP error.</returns>
        [BlockAction]
        public BlockActionResult GetMessages( Guid rockPhoneNumberGuid, Guid? personGuid, Guid? personAliasGuid )
        {
            var rockPhoneNumber = DefinedValueCache.Get( rockPhoneNumberGuid );

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

                    var responses = communicationResponseService
                        .GetCommunicationConversationForPerson( recipientPersonId.Value, rockPhoneNumber.Id );

                    var messages = ToMessageBags( responses );

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

        /// <summary>
        /// Resolves the snippet text and returns it.
        /// </summary>
        /// <param name="snippetGuid">The unique identifier of the snippet to be resolved.</param>
        /// <param name="personGuid">The unique identifier of the person who will receive the snippet.</param>
        /// <returns>A string that represents the snippet text.</returns>
        [BlockAction]
        public BlockActionResult ResolveSnippetText( Guid snippetGuid, Guid personGuid )
        {
            var snippetTypeGuid = GetAttributeValue( AttributeKey.SnippetType ).AsGuidOrNull();
            var currentPersonId = RequestContext.CurrentPerson?.Id;

            if ( !snippetTypeGuid.HasValue )
            {
                return ActionNotFound( "Snippet could not be found." );
            }

            using ( var rockContext = new RockContext() )
            {
                var snippet = new SnippetService( rockContext )
                    .GetAuthorizedSnippets( RequestContext.CurrentPerson,
                        s => s.Guid == snippetGuid && s.SnippetType.Guid == snippetTypeGuid.Value )
                    .FirstOrDefault();

                if ( snippet == null )
                {
                    return ActionNotFound( "Snippet was not found." );
                }

                var person = new PersonService( rockContext ).Get( personGuid );

                if ( person == null )
                {
                    return ActionNotFound( "Person could not be found." );
                }

                var mergeFields = RequestContext.GetCommonMergeFields();

                mergeFields.Add( "Person", person );

                var text = snippet.Content.ResolveMergeFields( mergeFields );

                return ActionOk( text );
            }
        }

        [BlockAction]
        public BlockActionResult SendMessage( Guid rockPhoneNumberGuid, Guid personGuid, string message, Guid? attachmentGuid = null )
        {
            var rockPhoneNumber = DefinedValueCache.Get( rockPhoneNumberGuid );

            if ( rockPhoneNumber == null )
            {
                return ActionNotFound( "Rock phone number was not found." );
            }

            if ( RequestContext.CurrentPerson == null )
            {
                return ActionBadRequest( "Must be logged in to send messages." );
            }

            using ( var rockContext = new RockContext() )
            {
                try
                {
                    // The sender is the logged in user.
                    var fromPersonAliasId = RequestContext.CurrentPerson.PrimaryAliasId.Value;

                    var responseCode = Rock.Communication.Medium.Sms.GenerateResponseCode( rockContext );
                    var toPrimaryAliasId = new PersonAliasService( rockContext ).GetPrimaryAliasId( personGuid );

                    List<BinaryFile> attachments = null;

                    if ( attachmentGuid.HasValue )
                    {
                        var binaryFile = new BinaryFileService( rockContext )
                            .Get( attachmentGuid.Value );

                        if ( binaryFile != null )
                        {
                            attachments = new List<BinaryFile>
                            {
                                binaryFile
                            };
                        }
                    }

                    // Create and enqueue the communication
                    var communication = Rock.Communication.Medium.Sms.CreateSmsCommunication( RequestContext.CurrentPerson, toPrimaryAliasId, message, rockPhoneNumber, responseCode, rockContext, attachments );

                    if ( communication.Recipients.Count == 0 )
                    {
                        return ActionInternalServerError( "Unable to determine recipient of message." );
                    }

                    // Must use a new context in order to get an object that
                    // has valid navigation properties.
                    using ( var rockContext2 = new RockContext() )
                    {
                        var recipientId = communication.Recipients.First().Id;

                        var messageBag = new CommunicationRecipientService( rockContext2 )
                            .GetConversationMessageBag( recipientId );

                        return ActionOk( messageBag );
                    }
                }
                catch ( Exception ex )
                {
                    ExceptionLogService.LogException( ex );

                    return ActionInternalServerError( "Unexpected error encountered when trying to send message." );
                }
            }
        }

        #endregion

        #region Support Classes

        private class ConversationDetailBag
        {
            /// <summary>
            /// Gets or sets the conversation key.
            /// </summary>
            /// <value>The conversation key.</value>
            public string ConversationKey { get; set; }

            /// <summary>
            /// Gets or sets the person unique identifier.
            /// </summary>
            /// <value>The person unique identifier.</value>
            public Guid PersonGuid { get; set; }

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

            /// <summary>
            /// Gets or sets the snippets available to use when sending a message.
            /// </summary>
            /// <value>The snippets available to use when sending a message.</value>
            public List<ListItemBag> Snippets { get; set; }
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

            /// <summary>
            /// Gets or sets the full name of the person that send the message from
            /// Rock. This is only valid if <see cref="IsOutbound"/> is true.
            /// </summary>
            /// <value>
            /// The full name of the person that sent the message from Rock.
            /// </value>
            public string OutboundSenderFullName { get; set; }

            /// <summary>
            /// Gets or sets the attachments to this message.
            /// </summary>
            /// <value>
            /// The attachments to this message.
            /// </value>
            public List<ConversationAttachmentBag> Attachments { get; set; }
        }

        /// <summary>
        /// A single attachment to a conversation message.
        /// </summary>
        internal class ConversationAttachmentBag
        {
            /// <summary>
            /// Gets or sets the thumbnail URL.
            /// </summary>
            /// <value>The thumbnail URL.</value>
            public string ThumbnailUrl { get; set; }

            /// <summary>
            /// Gets or sets the URL.
            /// </summary>
            /// <value>The URL.</value>
            public string Url { get; set; }
        }

        #endregion
    }
}
