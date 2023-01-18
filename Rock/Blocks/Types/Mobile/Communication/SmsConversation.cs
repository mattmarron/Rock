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
using Rock.ViewModels.Communication;
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
                    var attachments = response.CommunicationId.HasValue
                        ? new CommunicationAttachmentService( rockContext ).Queryable()
                            .Where( ca => ca.CommunicationId == response.CommunicationId.Value )
                            .Select( ca => new
                            {
                                ca.BinaryFile.Guid,
                                ca.BinaryFile.FileName
                            } )
                            .ToList()
                        : new CommunicationResponseAttachmentService( rockContext ).Queryable()
                            .Where( cra => cra.CommunicationResponseId == response.CommunicationResponseId.Value )
                            .Select( cra => new
                            {
                                cra.BinaryFile.Guid,
                                cra.BinaryFile.FileName
                            } )
                            .ToList();

                    foreach ( var attachment in attachments )
                    {
                        var ext = System.IO.Path.GetExtension( attachment.FileName ).ToLower();
                        var isImage = ext == ".jpg" || ext == ".png";

                        bag.Attachments.Add( new ConversationAttachmentBag
                        {
                            FileName = attachment.FileName,
                            Url = $"{publicUrl}GetImage.ashx?Guid={attachment.Guid}",
                            ThumbnailUrl = isImage ? $"{publicUrl}GetImage.ashx?Guid={attachment.Guid}&maxwidth=512&maxheight=512" : null
                        } );
                    }
                }
            }

            return bag;
        }

        internal static List<ConversationMessageBag> ToMessageBags( ICollection<CommunicationRecipientResponse> responses )
        {
            var publicUrl = GlobalAttributesCache.Get().GetValue( "PublicApplicationRoot" );
            var attachmentsLookup = new Dictionary<string, List<(Guid Guid, string FileName)>>();

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

                    var results = new CommunicationAttachmentService( rockContext )
                        .Queryable()
                        .Where( ca => communicationIds.Contains( ca.CommunicationId ) )
                        .Select( ca => new
                        {
                            ca.CommunicationId,
                            ca.BinaryFile.Guid,
                            ca.BinaryFile.FileName
                        } )
                        .ToList()
                        .GroupBy( ca => ca.CommunicationId );

                    foreach ( var result in results )
                    {
                        attachmentsLookup.AddOrReplace( communicationIdMap[result.Key], result.Select( r => (r.Guid, r.FileName) ).ToList() );
                    }
                }

                if ( communicationResponseIdMap.Count > 0 )
                {
                    var communicationResponseIds = communicationResponseIdMap.Keys.ToList();

                    var results = new CommunicationResponseAttachmentService( rockContext )
                        .Queryable()
                        .Where( cra => communicationResponseIds.Contains( cra.CommunicationResponseId ) )
                        .Select( cra => new
                        {
                            cra.CommunicationResponseId,
                            cra.BinaryFile.Guid,
                            cra.BinaryFile.FileName
                        } )
                        .ToList()
                        .GroupBy( cra => cra.CommunicationResponseId );

                    foreach ( var result in results )
                    {
                        attachmentsLookup.AddOrReplace( communicationResponseIdMap[result.Key], result.Select( r => (r.Guid, r.FileName ) ).ToList() );
                    }
                }
            }

            return responses
                .Select( response =>
                {
                    var bag = ToMessageBag( response, false );

                    if ( attachmentsLookup.TryGetValue( bag.MessageKey, out var attachments ) )
                    {
                        foreach ( var attachment in attachments )
                        {
                            var ext = System.IO.Path.GetExtension( attachment.FileName ).ToLower();
                            var isImage = ext == ".jpg" || ext == ".png";

                            bag.Attachments.Add( new ConversationAttachmentBag
                            {
                                FileName = attachment.FileName,
                                Url = $"{publicUrl}GetImage.ashx?Guid={attachment.Guid}",
                                ThumbnailUrl = isImage ? $"{publicUrl}GetImage.ashx?Guid={attachment.Guid}&maxwidth=512&maxheight=512" : null
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
        public BlockActionResult SendMessage( Guid rockPhoneNumberGuid, Guid personGuid, string message, List<Guid> attachments )
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

            // If no message and no attachments then fail.
            if ( message.IsNullOrWhiteSpace() && attachments?.Any() != true )
            {
                return ActionBadRequest( "Must provide either message or attachments." );
            }

            using ( var rockContext = new RockContext() )
            {
                try
                {
                    // The sender is the logged in user.
                    var fromPersonAliasId = RequestContext.CurrentPerson.PrimaryAliasId.Value;

                    var responseCode = Rock.Communication.Medium.Sms.GenerateResponseCode( rockContext );
                    var toPrimaryAliasId = new PersonAliasService( rockContext ).GetPrimaryAliasId( personGuid );

                    var attachmentFiles = new List<BinaryFile>();

                    if ( attachments != null )
                    {
                        var binaryFileService = new BinaryFileService( rockContext );
                        foreach ( var attachmentGuid in attachments )
                        {
                            var binaryFile = binaryFileService.Get( attachmentGuid );

                            if ( binaryFile != null )
                            {
                                attachmentFiles.Add( binaryFile );
                            }
                        }
                    }

                    // Create and enqueue the communication
                    var communication = Rock.Communication.Medium.Sms.CreateSmsCommunication( RequestContext.CurrentPerson, toPrimaryAliasId, message, rockPhoneNumber, responseCode, rockContext, attachmentFiles );

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

        #endregion
    }
}
