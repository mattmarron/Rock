﻿// <copyright>
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
using System.Linq;
using DotLiquid;
using Quartz;
using Rock.Attribute;
using Rock.Communication;
using Rock.Data;
using Rock.Lava;
using Rock.Logging;
using Rock.Model;
using Rock.Web.Cache;

namespace Rock.Jobs
{
    /// <summary>
    /// A job which processes reminders, including creating appropriate notifications and updating the reminder count value
    /// for people with active reminders.
    /// </summary>
    [DisplayName( "Process Reminders" )]
    [Description( "A job which processes reminders, including creating appropriate notifications and updating the reminder count value for people with active reminders." )]

    #region Job Attributes

    [IntegerField(
        "Command Timeout",
        Key = AttributeKey.CommandTimeout,
        Description = "Maximum amount of time (in seconds) to wait for any SQL based operations to complete. Leave blank to use the default for this job (300). Note, some metrics do not use SQL so this timeout will only apply to metrics that are SQL based.",
        IsRequired = false,
        DefaultIntegerValue = 60 * 5,
        Category = "General",
        Order = 1 )]

    [SystemCommunicationField(
        "Reminder Notification",
        Key = AttributeKey.ReminderNotification,
        Description = "The System Communication template used to generate reminder notifications.  If not provided, the job will not generate reminder communications.",
        IsRequired = false,
        DefaultSystemCommunicationGuid = Rock.SystemGuid.SystemCommunication.REMINDER_NOTIFICATION,
        Category = "General",
        Order = 2 )]

    [IntegerField(
        "Max Reminders Per Entity Type",
        Key = AttributeKey.MaxRemindersPerEntityType,
        Description = "The maximum number of reminders (per entity type) to include in communication notifications (default: 20).",
        IsRequired = true,
        DefaultIntegerValue = 20,
        Category = "General",
        Order = 3 )]

    #endregion Job Attributes

    [DisallowConcurrentExecution]
    public class ProcessReminders : RockJob
    {
        /// <summary>
        /// Keys to use for Attributes
        /// </summary>
        private static class AttributeKey
        {
            /// <summary>
            /// The SQL command timeout.
            /// </summary>
            public const string CommandTimeout = "CommandTimeout";

            /// <summary>
            /// The reminder notification.
            /// </summary>
            public const string ReminderNotification = "ReminderNotification";

            /// <summary>
            /// The max reminders per entity type.
            /// </summary>
            public const string MaxRemindersPerEntityType = "MaxRemindersPerEntityType";
        }

        /// <summary> 
        /// Empty constructor for job initialization
        /// <para>
        /// Jobs require a public empty constructor so that the
        /// scheduler can instantiate the class whenever it needs.
        /// </para>
        /// </summary>
        public ProcessReminders()
        {
        }

        #region Private Fields

        /// <summary>
        /// Errors collection for job status tracking.
        /// </summary>
        private List<string> _jobErrors;

        /// <summary>
        /// Total processed reminders for job status tracking.
        /// </summary>
        private int _totalProcessedReminders;

        #endregion Private Fields

        /// <inheritdoc cref="RockJob.Execute()"/>
        public override void Execute()
        {
            _jobErrors = new List<string>();
            _totalProcessedReminders = 0;
            var currentDate = RockDateTime.Now;
            WriteLog( $"ProcessReminders job started at {currentDate}." );

            using ( var rockContext = new RockContext() )
            {
                var commandTimeout = GetAttributeValue( AttributeKey.CommandTimeout ).AsIntegerOrNull() ?? 300;
                rockContext.Database.CommandTimeout = commandTimeout;

                var notificationSystemCommunication = GetNotificationSytemCommunicaton( rockContext );

                var reminderService = new ReminderService( rockContext );
                var activeReminders = reminderService.GetActiveReminders( currentDate );
                ProcessWorkflowNotifications( activeReminders, rockContext );
                ProcessCommunicationNotifications( notificationSystemCommunication, activeReminders, rockContext );

                // Some reminders may have been auto-completed by notification processing and are therefore no longer active,
                // so we need to refresh our query before we update reminder counts.
                activeReminders = reminderService.GetActiveReminders( currentDate );
                UpdateReminderCounts( activeReminders, rockContext );
            }

            WriteLog( $"ProcessReminders job completed at {RockDateTime.Now}." );

            if ( _jobErrors.Any() )
            {
                var sbResultOutput = new System.Text.StringBuilder( "Process Reminders job completed with errors." );
                sbResultOutput.AppendLine();

                int errorCount = 0;
                foreach( var jobError in _jobErrors )
                {
                    if ( errorCount == 5 )
                    {
                        sbResultOutput.AppendLine( $"Additional errors were truncated, to see the full "
                            + $"list of {_jobErrors.Count} errors please enable debug logging for the "
                            + $"\"Jobs\" logging domain." );
                        break;
                    }

                    errorCount++;
                    sbResultOutput.AppendLine( jobError );
                }

                this.Result = StandardFilters.NewlineToBr( sbResultOutput.ToString() );
            }
            else
            {
                this.Result = $"Process Reminders job completed successfully.  " +
                    $"{_totalProcessedReminders} {"reminder".PluralizeIf( _totalProcessedReminders != 1 )} processed.";
            }
        }

        #region Job Logic Methods

        /// <summary>
        /// Gets the SystemCommunication for notifications.
        /// </summary>
        /// <returns></returns>
        private SystemCommunication GetNotificationSytemCommunicaton( RockContext rockContext )
        {
            var notificationSystemCommunicationGuid = GetAttributeValue( AttributeKey.ReminderNotification ).AsGuidOrNull();
            SystemCommunication notificationSystemCommunication = null;
            if ( notificationSystemCommunicationGuid.HasValue )
            {
                notificationSystemCommunication = new SystemCommunicationService( rockContext ).Get( notificationSystemCommunicationGuid.Value );
            }

            return notificationSystemCommunication;
        }

        /// <summary>
        /// Process active reminders configured for workflow notifications.
        /// </summary>
        /// <param name="activeReminders"></param>
        /// <param name="rockContext"></param>
        private void ProcessWorkflowNotifications( IQueryable<Reminder> activeReminders, RockContext rockContext )
        {
            WriteLog( $"ProcessReminders job:  Initiated workflow notification processing." );
            var workflowReminderList = activeReminders
                .Where( r => r.ReminderType.NotificationType == ReminderNotificationType.Workflow )
                .ToList();

            WriteLog( $"ProcessReminders job:  Processing {workflowReminderList.Count} reminders for notification by workflow." );

            foreach ( var workflowReminder in workflowReminderList )
            {
                // Create a notification workflow.
                if ( !workflowReminder.ReminderType.NotificationWorkflowTypeId.HasValue )
                {
                    WriteError( $"Notification workflow for reminder {workflowReminder.Id} aborted:  The reminder type is incorrectly configured." );
                    continue;
                }

                InitiateNotificationWorkflow( workflowReminder, rockContext );
            }
        }

        /// <summary>
        /// Processes active reminders configured for SystemCommunication notifications.
        /// </summary>
        /// <param name="notificationSystemCommunication"></param>
        /// <param name="activeReminders"></param>
        /// <param name="rockContext"></param>
        private void ProcessCommunicationNotifications( SystemCommunication notificationSystemCommunication, IQueryable<Reminder> activeReminders, RockContext rockContext )
        {
            if ( notificationSystemCommunication == null )
            {
                WriteError( $"ProcessReminders job:  Aborted SystemCommunication notification for Reminders.  No SystemCommunication was specified." );
                return;
            }

            WriteLog( $"ProcessReminders job:  Initiated communication notification processing." );

            var remindersPerEntityType = GetAttributeValue( AttributeKey.MaxRemindersPerEntityType ).AsIntegerOrNull() ?? 20;

            var communicationReminders = activeReminders.Where( r => r.ReminderType.NotificationType == ReminderNotificationType.Communication );

            var communicationReminderRecipientList = communicationReminders
                .Select( r => r.PersonAlias.Person )
                .Distinct()
                .ToList();

            WriteLog( $"ProcessReminders job:  Processing reminder notifications for {communicationReminderRecipientList.Count} recipients." );

            foreach ( var reminderRecipient in communicationReminderRecipientList )
            {
                WriteLog( $"ProcessReminders job:  Processing reminder notifications for recipient {reminderRecipient.Id}." );

                var communicationRemindersForRecipient = communicationReminders
                    .Where( r => r.PersonAlias.PersonId == reminderRecipient.Id );

                SendReminderCommunication( reminderRecipient, communicationRemindersForRecipient, notificationSystemCommunication, rockContext, remindersPerEntityType );
            }
        }

        /// <summary>
        /// Creates the notification workflow for a specific reminder.
        /// </summary>
        /// <param name="reminder"></param>
        /// <param name="rockContext"></param>
        /// <returns></returns>
        private void InitiateNotificationWorkflow( Reminder reminder, RockContext rockContext )
        {
            WriteLog( $"ProcessReminders job:  Creating notification workflow for reminder {reminder.Id}." );

            try
            {
                var entityTypeService = new EntityTypeService( rockContext );
                var entity = entityTypeService.GetEntity( reminder.ReminderType.EntityTypeId, reminder.EntityId );

                var workflowParameters = new Dictionary<string, string>
                {
                    { "Reminder", reminder.Guid.ToString() },
                    { "ReminderType", reminder.ReminderType.Guid.ToString() },
                    { "PersonAlias", reminder.PersonAlias.Guid.ToString() },
                    { "EntityType", reminder.ReminderType.EntityType.Guid.ToString() },
                    { "Entity", entity.Guid.ToString() },
                };

                reminder.LaunchWorkflow( reminder.ReminderType.NotificationWorkflowTypeId, reminder.ToString(), workflowParameters, null );
                _totalProcessedReminders++;

                if ( reminder.ReminderType.ShouldAutoCompleteWhenNotified )
                {
                    // Mark the reminder as complete.
                    reminder.CompleteReminder();
                    rockContext.SaveChanges();
                }
            }
            catch ( Exception ex )
            {
                WriteError( $"Failed to create notification workflow for reminder {reminder.Id}: {ex.Message}", ex );
            }
        }

        /// <summary>
        /// Create SystemCommunication notifiocation for a recipient.
        /// </summary>
        /// <param name="recipient"></param>
        /// <param name="reminders"></param>
        /// <param name="notificationSystemCommunication"></param>
        /// <param name="rockContext"></param>
        /// <param name="remindersPerEntityType"></param>
        private void SendReminderCommunication( Person recipient, IQueryable<Reminder> reminders, SystemCommunication notificationSystemCommunication, RockContext rockContext, int remindersPerEntityType )
        {
            WriteLog( $"ProcessReminders job:  Creating SystemCommunication for recipient {recipient.Id}." );

            var baseUrl = GlobalAttributesCache.Value( "PublicApplicationRoot" );

            var personEntityTypeId = EntityTypeCache.GetId( typeof( Rock.Model.Person ) );
            var personReminderList = reminders
                .Where( r => r.ReminderType.EntityTypeId == personEntityTypeId )
                .OrderByDescending( r => r.ReminderDate )
                .Take( remindersPerEntityType )
                .ToList();

            var groupEntityTypeId = EntityTypeCache.GetId( typeof( Rock.Model.Group ) );
            var groupReminderList = reminders
                .Where( r => r.ReminderType.EntityTypeId == groupEntityTypeId )
                .OrderByDescending( r => r.ReminderDate )
                .Take( remindersPerEntityType )
                .ToList();

            var otherReminders = reminders
                .Where( r => r.ReminderType.EntityTypeId != personEntityTypeId
                        && r.ReminderType.EntityTypeId != groupEntityTypeId );

            var otherReminderList = new List<Reminder>();
            var otherReminderEntityList = otherReminders
                .Select( r => r.ReminderType.EntityType )
                .Distinct()
                .OrderBy( t => t.FriendlyName) // Sort other reminders by friendly name.
                .ToList();

            foreach ( var entityType in otherReminderEntityList )
            {
                var entityReminderList = otherReminders
                    .Where( r => r.ReminderType.EntityTypeId == entityType.Id )
                    .OrderByDescending( r => r.ReminderDate )
                    .Take( remindersPerEntityType )
                    .ToList();

                otherReminderList.AddRange( entityReminderList );
            }

            WriteLog( $"ProcessReminders job:  Creating SystemCommunication for {personReminderList.Count} Person Reminders, " +
                $"{groupReminderList.Count} Group Reminders, and {otherReminderList.Count} other reminders for {otherReminderEntityList.Count} entity types." );

            var reminderDataObjects = new List<ReminderDTO>();

            var personService = new PersonService( rockContext );
            foreach ( var reminder in personReminderList )
            {
                var person = personService.Get( reminder.EntityId );
                var photoUrl = person.PhotoUrl.Replace( "~/", baseUrl.EnsureTrailingForwardslash() );;
                var reminderData = new ReminderDTO( reminder, person, photoUrl );
                reminderDataObjects.Add( reminderData );
            }

            var groupService = new GroupService( rockContext );
            foreach ( var reminder in groupReminderList )
            {
                var group = groupService.Get( reminder.EntityId );
                var reminderData = new ReminderDTO( reminder, group );
                reminderDataObjects.Add( reminderData );
            }

            var entityTypeService = new EntityTypeService( rockContext );
            foreach ( var reminder in otherReminderList )
            {
                var entity = entityTypeService.GetEntity( reminder.ReminderType.EntityTypeId, reminder.EntityId );
                var reminderData = new ReminderDTO( reminder, entity );
                reminderDataObjects.Add( reminderData );
            }

            try
            {
                var mergeFields = LavaHelper.GetCommonMergeFields( null );
                mergeFields.Add( "Reminders", reminderDataObjects );
                mergeFields.Add( "Person", recipient );

                var mediumType = ( int ) CommunicationType.Email;
                var result = CommunicationHelper.SendMessage( recipient, mediumType, notificationSystemCommunication, mergeFields );

                if ( result.MessagesSent > 0 )
                {
                    var processedReminderList = new List<Reminder>();
                    processedReminderList.AddRange( personReminderList );
                    processedReminderList.AddRange( groupReminderList );
                    processedReminderList.AddRange( otherReminderList );

                    _totalProcessedReminders += processedReminderList.Count;

                    var autoCompleteReminderList = processedReminderList
                        .Where( r => r.ReminderType.ShouldAutoCompleteWhenNotified )
                        .ToList();

                    WriteLog( $"ProcessReminders job:  Notification sent for {processedReminderList.Count} reminders.  Auto-completing {autoCompleteReminderList.Count} reminders." );

                    foreach ( var autoCompleteReminder in autoCompleteReminderList )
                    {
                        // Mark the reminder as complete.
                        autoCompleteReminder.CompleteReminder();
                        rockContext.SaveChanges();
                    }
                }
                else
                {
                    var messageErrors = new System.Text.StringBuilder( string.Empty );

                    if ( result.Errors.Any() )
                    {
                        result.Errors.ForEach( e => messageErrors.AppendLine( e ) );
                    }
                    else if ( result.Warnings.Any() )
                    {
                        result.Warnings.ForEach( w => messageErrors.AppendLine( w ) );
                    }
                    else
                    {
                        result.Exceptions.ForEach( e => messageErrors.AppendLine( e.Message ) );
                    }

                    var errorOutput = messageErrors.ToString();
                    if ( errorOutput.IsNullOrWhiteSpace() )
                    {
                        errorOutput = "Unknown error.";
                    }

                    WriteError( $"Failed to send SystemCommunication for for Reminders for recipient {recipient.Id}: { errorOutput }" );
                }
            }
            catch ( Exception ex )
            {
                WriteError( $"Failed to create SystemCommunication for Reminders for recipient {recipient.Id}: {ex.Message}", ex );
            }
        }

        /// <summary>
        /// Updates the reminder counts for people with reminders.
        /// </summary>
        /// <param name="activeReminders"></param>
        /// <param name="rockContext"></param>
        private void UpdateReminderCounts( IQueryable<Reminder> activeReminders, RockContext rockContext )
        {
            var personService = new PersonService( rockContext );

            // Locate people who currently have a reminder count greater than zero but shouldn't.
            var peopleWithNoReminders = personService.Queryable()
                .Where( p => p.ReminderCount != null
                            && p.ReminderCount > 0
                            && !activeReminders.Select( r => r.PersonAlias.PersonId ).Contains( p.Id ) );

            int zeroedCount = peopleWithNoReminders.Count();
            WriteLog( $"ProcessReminders job:  Resetting reminder counts to 0 for {zeroedCount} people." );

            rockContext.BulkUpdate( peopleWithNoReminders, p => new Person { ReminderCount = 0 } );
            rockContext.SaveChanges();

            // Update individual reminder counts with correct values.
            var reminderCounts = activeReminders.GroupBy( r => r.PersonAlias.PersonId )
                .ToDictionary( a => a.Key, a => a.Count() );

            int updatedCount = 0;
            foreach ( var personId in reminderCounts.Keys )
            {
                var person = personService.Get( personId );
                if ( person.ReminderCount != reminderCounts[personId])
                {
                    updatedCount++;
                    person.ReminderCount = reminderCounts[personId];
                    rockContext.SaveChanges();
                }
            }

            WriteLog( $"ProcessReminders job:  Updated reminder counts for {updatedCount} people." );
        }

        #endregion Job Logic Methods

        #region Log Utility Methods

        /// <summary>
        /// Writes a message to the job log.
        /// </summary>
        /// <param name="logMessage"></param>
        private void WriteLog( string logMessage )
        {
            RockLogger.Log.Debug( RockLogDomains.Jobs, logMessage);
        }

        /// <summary>
        /// Writes an error to the job log and reports it to the final job status.
        /// </summary>
        /// <param name="errorMessage"></param>
        /// <param name="ex"></param>
        private void WriteError( string errorMessage, Exception ex = null )
        {
            _jobErrors.Add( errorMessage );
            WriteLog( $"ProcessReminders job:  {errorMessage}" );

            if ( ex != null )
            {
                ExceptionLogService.LogException( ex );
            }
        }

        #endregion Log Utility Methods
    }
}
