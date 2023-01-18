using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using Rock.Attribute;
using Rock.Constants;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.ViewModels.Blocks.Engagement.SignUp.SignUpAttendanceDetail;
using Rock.Web.UI.Controls;

namespace Rock.Blocks.Engagement.SignUp
{
    [DisplayName( "Sign-Up Attendance Detail" )]
    [Category( "Engagement > Sign-Up" )]
    [Description( "Lists the group members for a specific sign-up group/project occurrence date time and allows selecting if they attended or not." )]
    [IconCssClass( "fa fa-clipboard-check" )]

    #region Block Attributes

    [CodeEditorField( "Header Lava Template",
        Key = AttributeKey.HeaderLavaTemplate,
        Description = "The Lava template to show at the top of the page.",
        EditorMode = CodeEditorMode.Lava,
        EditorTheme = CodeEditorTheme.Rock,
        EditorHeight = 400,
        IsRequired = true,
        DefaultValue = "",
        Order = 0 )]

    #endregion

    [Rock.SystemGuid.EntityTypeGuid( "747587A0-87E9-437D-A4ED-75431CED55B3" )]
    [Rock.SystemGuid.BlockTypeGuid( "96D160D9-5668-46EF-9941-702BD3A577DB" )]
    public class SignUpAttendanceDetail : RockObsidianBlockType
    {
        #region Keys

        private static class PageParameterKey
        {
            public const string GroupGuid = "GroupGuid";
            public const string LocationId = "LocationId";
            public const string ScheduleId = "ScheduleId";
            public const string AttendanceDate = "AttendanceDate";
        }

        private static class AttributeKey
        {
            public const string HeaderLavaTemplate = "HeaderLavaTemplate";
        }

        #endregion

        public override string BlockFileUrl => $"{base.BlockFileUrl}.obs";

        #region Methods

        /// <inheritdoc/>
        public override object GetObsidianBlockInitialization()
        {
            using ( var rockContext = new RockContext() )
            {
                var box = new SignUpAttendanceDetailInitializationBox();

                SetBoxInitialState( box, rockContext );

                return box;
            }
        }

        /// <summary>
        /// Sets the initial state of the box.
        /// </summary>
        /// <param name="box">The box.</param>
        /// <param name="rockContext">The rock context.</param>
        private void SetBoxInitialState( SignUpAttendanceDetailInitializationBox box, RockContext rockContext )
        {
            var attendanceData = GetAttendanceData( rockContext );

            if ( !attendanceData.CanTakeAttendance )
            {
                box.ErrorMessage = attendanceData.ErrorMessage ?? "Unable to take attendance for this occurrence.";
                return;
            }

            box.AttendanceDate = attendanceData.AttendanceDate;
            box.LocationName = attendanceData.LocationName;
            box.ScheduleName = attendanceData.ScheduleName;
            box.Attendees = attendanceData.Attendees;
        }

        /// <summary>
        /// A runtime object to represent a <see cref="Group"/>, <see cref="Location"/> & <see cref="Schedule"/> combination,
        /// along with it's <see cref="GroupMember"/> collection, against which attendance should be saved.
        /// </summary>
        private class AttendanceData
        {
            public string ErrorMessage { get; set; }

            public Group Group { get; set; }

            public Location Location { get; set; }

            public Schedule Schedule { get; set; }

            public DateTime AttendanceDate { get; set; }

            public List<GroupMember> GroupMembers { get; set; }

            public AttendanceOccurrence ExistingOccurrence { get; set; }

            public bool CanTakeAttendance
            {
                get
                {
                    return string.IsNullOrEmpty(this.ErrorMessage)
                        && this.Group != null
                        && this.Location != null
                        && this.Schedule != null;
                }
            }

            public string LocationName
            {
                get
                {
                    var namedLocation = this.Location?.Name;

                    return !string.IsNullOrEmpty( namedLocation )
                        ? namedLocation
                        : this.Location?.FormattedAddress;
                }
            }

            public string ScheduleName
            {
                get
                {
                    var namedSchedule = this.Schedule?.Name;

                    return !string.IsNullOrWhiteSpace( namedSchedule )
                       ? namedSchedule
                       : this.Schedule?.FriendlyScheduleText;
                }
            }

            public List<SignUpAttendeeBag> Attendees
            {
                get
                {
                    var attendees = new List<SignUpAttendeeBag>();

                    foreach ( var groupMember in GroupMembers )
                    {
                        var person = groupMember.Person;
                        var didAttend = this.ExistingOccurrence != null
                            && this.ExistingOccurrence.Attendees.Any( a => person.Aliases.Any( pa => pa.Id == a.PersonAliasId ) );

                        attendees.Add( new SignUpAttendeeBag
                        {
                            PersonAliasId = person.PrimaryAliasId.GetValueOrDefault(),
                            Name = person.FullName,
                            DidAttend = didAttend
                        } );
                    }

                    return attendees;
                }
            }
        }

        /// <summary>
        /// Gets the attendance data.
        /// </summary>
        /// <param name="rockContext">The rock context.</param>
        /// <returns></returns>
        private AttendanceData GetAttendanceData( RockContext rockContext )
        {
            var attendanceData = new AttendanceData();

            var groupGuid = PageParameter( PageParameterKey.GroupGuid ).AsGuidOrNull();
            if ( !groupGuid.HasValue )
            {
                attendanceData.ErrorMessage = "Group key was not provided.";
                return attendanceData;
            }

            var group = GetGroup( rockContext, groupGuid.Value );
            if ( group == null )
            {
                attendanceData.ErrorMessage = "Group was not found.";
                return attendanceData;
            }

            attendanceData.Group = group;

            var currentPerson = RequestContext.CurrentPerson;
            if ( !group.IsAuthorized( Authorization.VIEW, currentPerson ) )
            {
                attendanceData.ErrorMessage = EditModeMessage.NotAuthorizedToView( Group.FriendlyTypeName );
                return attendanceData;
            }

            if ( !group.IsAuthorized( Authorization.MANAGE_MEMBERS, currentPerson ) && !group.IsAuthorized( Authorization.EDIT, currentPerson ) )
            {
                attendanceData.ErrorMessage = $"You're not authorized to update the attendance for the selected {Group.FriendlyTypeName}.";
                return attendanceData;
            }

            var locationId = PageParameter( PageParameterKey.LocationId ).AsIntegerOrNull();
            var scheduleId = PageParameter( PageParameterKey.ScheduleId ).AsIntegerOrNull();
            var attendanceDate = PageParameter( PageParameterKey.AttendanceDate ).AsDateTime();

            if ( !TryGetGroupLocationSchedule( attendanceData, group, locationId, scheduleId, attendanceDate ) )
            {
                // An error message will have been added.
                return attendanceData;
            }

            if ( !TryGetGroupMembers( rockContext, attendanceData ) )
            {
                // An error message will have been added.
                return attendanceData;
            }

            GetExistingOccurrence( rockContext, attendanceData );

            return attendanceData;
        }

        /// <summary>
        /// Gets the group.
        /// </summary>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="groupGuid">The group unique identifier.</param>
        /// <returns></returns>
        private Group GetGroup( RockContext rockContext, Guid groupGuid )
        {
            return new GroupService( rockContext )
                .Queryable()
                .AsNoTracking()
                .Include( g => g.GroupLocations )
                .Include( g => g.GroupLocations.Select( gl => gl.Location ) )
                .Include( g => g.GroupLocations.Select( gl => gl.Schedules ) )
                .FirstOrDefault( g => g.Guid == groupGuid );
        }

        /// <summary>
        /// Tries to get the <see cref="Location"/> and <see cref="Schedule"/> instances for this occurrence.
        /// </summary>
        /// <param name="attendanceData">The attendance data.</param>
        /// <param name="group">The group.</param>
        /// <param name="locationId">The location identifier.</param>
        /// <param name="scheduleId">The schedule identifier.</param>
        /// <param name="attendanceDate">The attendance date.</param>
        /// <returns></returns>
        private bool TryGetGroupLocationSchedule( AttendanceData attendanceData, Group group, int? locationId, int? scheduleId, DateTime? attendanceDate )
        {
            GroupLocation groupLocation = null;
            if ( locationId.HasValue )
            {
                groupLocation = group.GroupLocations.FirstOrDefault( gl => gl.LocationId == locationId.Value );
            }
            else if ( group.GroupLocations.Count == 1 )
            {
                groupLocation = group.GroupLocations.First();
            }

            Schedule schedule = null;
            if ( groupLocation != null )
            {
                if ( scheduleId.HasValue )
                {
                    schedule = groupLocation.Schedules.FirstOrDefault( s => s.Id == scheduleId.Value );
                }
                else if ( groupLocation.Schedules.Count == 1 )
                {
                    schedule = groupLocation.Schedules.First();
                }
            }

            if ( groupLocation?.Location == null || schedule == null )
            {
                attendanceData.ErrorMessage = "The configuration provided does not provide enough information to take attendance. Please provide the schedule and location for this occurrence.";
                return false;
            }

            if ( !attendanceDate.HasValue )
            {
                attendanceDate = RockDateTime.Today;
            }

            // Ensure the specified attendance date matches an occurrence of the selected schedule.
            var date = attendanceDate.Value.Date;
            if ( !schedule.GetScheduledStartTimes( date.StartOfDay(), date.EndOfDay() ).Any() )
            {
                attendanceData.ErrorMessage = $"The attendance date of {date.ToMonthDayString()} does not match the schedule of the project.";
                return false;
            }

            attendanceData.Location = groupLocation.Location;
            attendanceData.Schedule = schedule;
            attendanceData.AttendanceDate = date;

            return true;
        }

        /// <summary>
        /// Tries to get the group members.
        /// </summary>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="attendanceData">The attendance data.</param>
        /// <returns></returns>
        private bool TryGetGroupMembers( RockContext rockContext, AttendanceData attendanceData )
        {
            var groupMembers = new GroupMemberAssignmentService( rockContext )
                .Queryable()
                .AsNoTracking()
                .Include( gma => gma.GroupMember )
                .Include( gma => gma.GroupMember.Person )
                .Include( gma => gma.GroupMember.Person.Aliases )
                .Where( gma =>
                    !gma.GroupMember.IsArchived
                    && gma.GroupMember.GroupId == attendanceData.Group.Id
                    && gma.LocationId == attendanceData.Location.Id
                    && gma.ScheduleId == attendanceData.Schedule.Id
                )
                .Select( gma => gma.GroupMember )
                .ToList();

            if ( !groupMembers.Any() )
            {
                attendanceData.ErrorMessage = "No attendees found for this occurrence.";
                return false;
            }

            attendanceData.GroupMembers = groupMembers;

            return true;
        }

        /// <summary>
        /// Gets the existing [Attendance]Occurrence, if one exists.
        /// </summary>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="attendanceData">The attendance data.</param>
        private void GetExistingOccurrence( RockContext rockContext, AttendanceData attendanceData )
        {
            attendanceData.ExistingOccurrence = new AttendanceOccurrenceService( rockContext )
                .Queryable()
                .AsNoTracking()
                .Include( ao => ao.Attendees )
                .FirstOrDefault( ao =>
                    ao.GroupId == attendanceData.Group.Id
                    && ao.LocationId == attendanceData.Location.Id
                    && ao.ScheduleId == attendanceData.Schedule.Id
                    && ao.OccurrenceDate == attendanceData.AttendanceDate
                );
        }

        /// <summary>
        /// Saves the attendance.
        /// </summary>
        private void SaveAttendance()
        {
            /*
             * To save attendance, we need to:
             * 1) Create an [AttendanceOccurrence] record (if one does not already exist);
             *     a) If one already exists.. show an error message?
             * 2) Create an [Attendance] record for each [GroupMember] who attended.
             */
        }

        #endregion

        #region Block Actions

        /// <summary>
        /// Saves this instance.
        /// </summary>
        /// <returns></returns>
        [BlockAction]
        public BlockActionResult Save()
        {
            return ActionOk();
        }

        #endregion
    }
}
