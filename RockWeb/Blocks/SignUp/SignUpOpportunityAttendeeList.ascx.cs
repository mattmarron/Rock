using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using Rock;
using Rock.Constants;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.Web.Cache;
using Rock.Web.UI;

namespace RockWeb.Blocks.SignUp
{
    [DisplayName( "Sign-Up Opportunity Attendee List" )]
    [Category( "Sign-Up" )]
    [Description( "Lists all the group members for the selected group, location and schedule." )]

    [Rock.SystemGuid.BlockTypeGuid( "EE652767-5070-4EAB-8BB7-BB254DD01B46" )]
    public partial class SignUpOpportunityAttendeeList : RockBlock
    {
        #region Private Keys

        private static class PageParameterKey
        {
            public const string GroupId = "GroupId";
            public const string LocationId = "LocationId";
            public const string ScheduleId = "ScheduleId";
        }

        private static class ViewStateKey
        {
            public const string GroupId = "GroupId";
            public const string LocationId = "LocationId";
            public const string ScheduleId = "ScheduleId";
        }

        #endregion

        #region Properties

        private int GroupId
        {
            get
            {
                return ViewState[ViewStateKey.GroupId].ToIntSafe();
            }
            set
            {
                ViewState[ViewStateKey.GroupId] = value;
            }
        }

        private int LocationId
        {
            get
            {
                return ViewState[ViewStateKey.LocationId].ToIntSafe();
            }
            set
            {
                ViewState[ViewStateKey.LocationId] = value;
            }
        }

        private int ScheduleId
        {
            get
            {
                return ViewState[ViewStateKey.ScheduleId].ToIntSafe();
            }
            set
            {
                ViewState[ViewStateKey.ScheduleId] = value;
            }
        }

        private int SignUpGroupTypeId
        {
            get
            {
                return GroupTypeCache.Get( Rock.SystemGuid.GroupType.GROUPTYPE_SIGNUP_GROUP )?.Id ?? 0;
            }
        }

        #endregion

        #region Control Life-Cycle Events

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            nbNotAuthorizedToView.Text = EditModeMessage.NotAuthorizedToView( Group.FriendlyTypeName );
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            ShowDetails();
        }

        #endregion

        #region Internal Members

        /// <summary>
        /// Resets the control visibility.
        /// </summary>
        private void ResetControlVisibility()
        {
            nbMissingIds.Visible = false;
            nbNotFoundOrArchived.Visible = false;
            nbNotAuthorizedToView.Visible = false;
            nbInvalidGroupType.Visible = false;
            nbOpportunityNotFound.Visible = false;
            pnlDetails.Visible = true;
        }

        /// <summary>
        /// Shows the missing ids message.
        /// </summary>
        /// <param name="message">The message.</param>
        private void ShowMissingIdsMessage( string message )
        {
            nbMissingIds.Text = message;
            nbMissingIds.Visible = true;
            pnlDetails.Visible = false;
        }

        /// <summary>
        /// Shows the group not found message.
        /// </summary>
        private void ShowGroupNotFoundMessage()
        {
            nbNotFoundOrArchived.Visible = true;
            pnlDetails.Visible = false;
        }

        /// <summary>
        /// Shows the not authorized to view message.
        /// </summary>
        private void ShowNotAuthorizedToViewMessage()
        {
            nbNotAuthorizedToView.Visible = true;
            pnlDetails.Visible = false;
        }

        /// <summary>
        /// Shows the invalid group type message.
        /// </summary>
        private void ShowInvalidGroupTypeMessage()
        {
            nbInvalidGroupType.Visible = true;
            pnlDetails.Visible = false;
        }

        /// <summary>
        /// Shows the opportunity not found message.
        /// </summary>
        private void ShowOpportunityNotFoundMessage()
        {
            nbOpportunityNotFound.Visible = true;
            pnlDetails.Visible = false;
        }

        /// <summary>
        /// Shows the details.
        /// </summary>
        private void ShowDetails()
        {
            ResetControlVisibility();

            if ( !EnsureRequiredIdsAreProvided() )
            {
                return;
            }

            using ( var rockContext = new RockContext() )
            {
                Group group = new GroupService( rockContext )
                    .Queryable()
                    .AsNoTracking()
                    .Include( g => g.Campus )
                    .Include( g => g.GroupType )
                    .Include( g => g.ParentGroup )
                    .FirstOrDefault( g => g.Id == GroupId );

                if ( group == null )
                {
                    ShowGroupNotFoundMessage();
                    return;
                }

                if ( !EnsureGroupIsAllowed( group ) )
                {
                    return;
                }

                var opportunity = GetOpportunity( group, rockContext );
                if ( opportunity == null )
                {
                    ShowOpportunityNotFoundMessage();
                    return;
                }

                InitializeSummary( opportunity );

                BindMembersGrid( opportunity );
            }
        }

        /// <summary>
        /// Ensures the required ids are provided.
        /// </summary>
        /// <returns></returns>
        private bool EnsureRequiredIdsAreProvided()
        {
            GroupId = PageParameter( PageParameterKey.GroupId ).ToIntSafe();
            LocationId = PageParameter( PageParameterKey.LocationId ).ToIntSafe();
            ScheduleId = PageParameter( PageParameterKey.ScheduleId ).ToIntSafe();

            var missingIds = new List<string>();
            if ( GroupId == 0 )
            {
                missingIds.Add( "Group ID" );
            }

            if ( LocationId == 0 )
            {
                missingIds.Add( "Location ID" );
            }

            if ( ScheduleId == 0 )
            {
                missingIds.Add( "Schedule ID" );
            }

            if ( missingIds.Any() )
            {
                ShowMissingIdsMessage( $"The following required ID{( missingIds.Count > 1 ? "s were" : " was" )} not provided: {string.Join( ", ", missingIds )}." );
                return false;
            }

            return true;
        }

        /// <summary>
        /// Ensures the group is allowed.
        /// </summary>
        /// <param name="group">The group.</param>
        /// <returns></returns>
        private bool EnsureGroupIsAllowed( Group group )
        {
            if ( !group.IsAuthorized( Authorization.VIEW, this.CurrentPerson ) )
            {
                ShowNotAuthorizedToViewMessage();
                return false;
            }

            if ( group.GroupTypeId != SignUpGroupTypeId && group.GroupType.InheritedGroupTypeId != SignUpGroupTypeId )
            {
                ShowInvalidGroupTypeMessage();
                return false;
            }

            return true;
        }

        private class Opportunity
        {
            private class BadgeType
            {
                public const string Danger = "danger";
                public const string Success = "success";
                public const string Warning = "warning";
            }

            private int? SlotsMin => Config?.MinimumCapacity;
            private int? SlotsDesired => Config?.DesiredCapacity;
            private int? SlotsMax => Config?.MaximumCapacity;

            public Group Group { get; set; }

            public Location Location { get; set; }

            public Schedule Schedule { get; set; }

            public GroupLocationScheduleConfig Config { get; set; }

            public List<GroupMemberAssignment> Members { get; set; }

            public string Name
            {
                get
                {
                    var title = Group?.Name;

                    if ( !string.IsNullOrWhiteSpace( Config?.ConfigurationName ) )
                    {
                        title = Config.ConfigurationName;
                    }

                    var scheduleName = Schedule?.ScheduleType == ScheduleType.Named
                        && !string.IsNullOrWhiteSpace( Schedule?.Name )
                            ? Schedule.Name
                            : string.Empty;

                    var separator = !string.IsNullOrWhiteSpace( title )
                        && !string.IsNullOrWhiteSpace( scheduleName )
                            ? " - "
                            : string.Empty;

                    return $"{title}{separator}{scheduleName}";
                }
            }

            public string ConfiguredSlots
            {
                get
                {
                    return string.Join( " | ", new List<string>
                    {
                        SlotsMin.GetValueOrDefault().ToString("N0"),
                        SlotsDesired.GetValueOrDefault().ToString("N0"),
                        SlotsMax.GetValueOrDefault().ToString("N0"),
                    } );
                }
            }

            public int SlotsFilled
            {
                get
                {
                    return Members?.Count ?? 0;
                }
            }

            public string SlotsFilledBadgeType
            {
                get
                {
                    if ( SlotsMax.GetValueOrDefault() > 0 )
                    {
                        return SlotsFilled == 0 || SlotsFilled < SlotsMin.GetValueOrDefault()
                            ? BadgeType.Warning
                            : SlotsFilled < SlotsMax.Value
                                ? BadgeType.Success
                                : BadgeType.Danger;
                    }
                    else if ( SlotsMin.GetValueOrDefault() > 0 )
                    {
                        return SlotsFilled < SlotsMin.Value
                            ? BadgeType.Warning
                            : BadgeType.Success;
                    }

                    return SlotsFilled > 0
                        ? BadgeType.Success
                        : BadgeType.Warning;
                }
            }
        }

        /// <summary>
        /// Gets the opportunity.
        /// </summary>
        /// <param name="group">The group.</param>
        /// <param name="rockContext">The rock context.</param>
        /// <returns></returns>
        private Opportunity GetOpportunity( Group group, RockContext rockContext )
        {
            var groupLocation = new GroupLocationService( rockContext )
                .Queryable()
                .AsNoTracking()
                .Include( gl => gl.Location )
                .Include( gl => gl.Schedules )
                .Include( gl => gl.GroupLocationScheduleConfigs )
                .FirstOrDefault( gl => gl.GroupId == GroupId && gl.LocationId == LocationId );

            if ( groupLocation == null
                || groupLocation.Location == null
                || !groupLocation.Schedules.Any( s => s.Id == ScheduleId ) )
            {
                return null;
            }

            var schedule = groupLocation.Schedules.First( s => s.Id == ScheduleId );
            var config = groupLocation.GroupLocationScheduleConfigs.First( c => c.GroupLocationId == groupLocation.Id && c.ScheduleId == ScheduleId );

            return new Opportunity
            {
                Group = group,
                Location = groupLocation.Location,
                Schedule = schedule,
                Config = config,
                Members = new GroupMemberAssignmentService( rockContext )
                    .Queryable()
                    .AsNoTracking()
                    .Include( gma => gma.GroupMember )
                    .Include( gma => gma.GroupMember.GroupRole )
                    .Where( gma => gma.GroupMember.GroupId == GroupId
                        && gma.LocationId == LocationId
                        && gma.ScheduleId == ScheduleId )
                    .ToList()
            };
        }

        /// <summary>
        /// Initializes the summary.
        /// </summary>
        /// <param name="opportunity">The opportunity.</param>
        private void InitializeSummary( Opportunity opportunity )
        {
            lTitle.Text = opportunity.Name;
            lLocation.Text = opportunity.Location.ToString();
            lSchedule.Text = opportunity.Schedule.FriendlyScheduleText ?? "Custom";
            lConfiguredSlots.Text = opportunity.ConfiguredSlots;
            bSlotsFilled.Text = opportunity.SlotsFilled.ToString("N0");
            bSlotsFilled.BadgeType = opportunity.SlotsFilledBadgeType;

            InitializeLabels( opportunity.Group );
        }

        /// <summary>
        /// Initializes the labels.
        /// </summary>
        /// <param name="group">The group.</param>
        /// <returns></returns>
        private void InitializeLabels( Group group )
        {
            hlGroupType.Text = group.GroupType?.Name;

            if ( group.Campus != null )
            {
                hlCampus.Text = group.Campus.Name;
                hlCampus.Visible = true;
            }
            else
            {
                hlCampus.Text = string.Empty;
                hlCampus.Visible = false;
            }

            hlInactive.Visible = !group.IsActive;
        }

        /// <summary>
        /// Binds the members grid.
        /// </summary>
        /// <param name="opportunity">The opportunity.</param>
        private void BindMembersGrid( Opportunity opportunity )
        {
            gMembers.DataSource = opportunity.Members;
            gMembers.DataBind();
        }

        #endregion
    }
}
