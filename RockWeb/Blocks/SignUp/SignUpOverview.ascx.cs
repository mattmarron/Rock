using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using System.Web.UI.WebControls;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace RockWeb.Blocks.SignUp
{
    [DisplayName( "Sign-Up Overview" )]
    [Category( "Sign-Up" )]
    [Description( "Displays an overview of sign-up projects with upcoming and recently-occurred opportunities." )]

    #region Block Attributes

    [LinkedPage( "Project Detail Page",
        Key = AttributeKey.ProjectDetailPage,
        Description = "Page used for viewing details about the scheduled opportunities for a given project group. Clicking a row in the grid will take you to this page.",
        IsRequired = true,
        Order = 0 )]

    [LinkedPage( "Sign-Up Opportunity Attendee List Page",
        Key = AttributeKey.SignUpOpportunityAttendeeListPage,
        Description = "Page used for viewing all the group members for the selected sign-up opportunity. If set, a view attendees button will show for each opportunity.",
        IsRequired = false,
        Order = 1 )]

    #endregion

    [Rock.SystemGuid.BlockTypeGuid( "B539F3B5-01D3-4325-B32A-85AFE2A9D18B" )]
    public partial class SignUpOverview : RockBlock
    {
        #region Private Keys and Fields

        private static class PageParameterKey
        {
            public const string GroupId = "GroupId";
            public const string LocationId = "LocationId";
            public const string ScheduleId = "ScheduleId";
        }

        private static class AttributeKey
        {
            public const string ProjectDetailPage = "ProjectDetailPage";
            public const string SignUpOpportunityAttendeeListPage = "SignUpOpportunityAttendeeListPage";
        }

        private static class GridFilterKey
        {
            public const string ProjectName = "ProjectName";
        }

        private static class DataKeyName
        {
            public const string GroupId = "GroupId";
            public const string LocationId = "LocationId";
            public const string ScheduleId = "ScheduleId";
        }

        private bool _canEdit;

        #endregion

        #region Properties

        private GroupTypeCache SignUpGroupType
        {
            get
            {
                return GroupTypeCache.Get( Rock.SystemGuid.GroupType.GROUPTYPE_SIGNUP_GROUP );
            }
        }


        private int SignUpGroupTypeId
        {
            get
            {
                return SignUpGroupType?.Id ?? 0;
            }
        }

        #endregion

        #region Control Life-Cycle Events

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( System.EventArgs e )
        {
            base.OnInit( e );

            _canEdit = IsUserAuthorized( Authorization.EDIT );

            InitializeGrid();

            // This event gets fired after block settings are updated. It's nice to repaint the screen if these settings would alter it.
            this.BlockUpdated += Block_BlockUpdated;
            this.AddConfigurationUpdateTrigger( upnlSignUpOverview );
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( System.EventArgs e )
        {
            base.OnLoad( e );

            ShowDetails();
        }

        /// <summary>
        /// Handles the BlockUpdated event of the Block control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        /// <exception cref="NotImplementedException"></exception>
        protected void Block_BlockUpdated( object sender, EventArgs e )
        {
            NavigateToCurrentPageReference();
        }

        #endregion

        #region Opportunities Grid Events

        /// <summary>
        /// Displays the gfOpportunities filter values.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The e.</param>
        protected void gfOpportunities_DisplayFilterValue( object sender, Rock.Web.UI.Controls.GridFilter.DisplayFilterValueArgs e )
        {
            if ( e.Key == GridFilterKey.ProjectName )
            {
                return;
            }
            else
            {
                e.Value = string.Empty;
            }
        }

        /// <summary>
        /// Handles the ApplyFilterClick event of the gfOpportunities control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs" /> instance containing the event data.</param>
        protected void gfOpportunities_ApplyFilterClick( object sender, System.EventArgs e )
        {
            gfOpportunities.SaveUserPreference( GridFilterKey.ProjectName, "Project Name", tbProjectName.Text );

            BindOpportunitiesGrid();
        }

        /// <summary>
        /// Handles the ClearFilterClick event of the gfOpportunities control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs" /> instance containing the event data.</param>
        protected void gfOpportunities_ClearFilterClick( object sender, System.EventArgs e )
        {
            gfOpportunities.DeleteUserPreferences();

            SetGridFilters();
        }

        /// <summary>
        /// Handles the DataBinding event of the gOpportunities control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void gOpportunities_DataBinding( object sender, EventArgs e )
        {
            if ( string.IsNullOrWhiteSpace( GetAttributeValue( AttributeKey.SignUpOpportunityAttendeeListPage ) ) )
            {
                var linkButtonField = gOpportunities.ColumnsOfType<LinkButtonField>().FirstOrDefault( c => c.ID == "lbOpportunityDetail" );
                if ( linkButtonField != null )
                {
                    linkButtonField.Visible = false;
                }
            }
        }

        /// <summary>
        /// Handles the RowDataBound event of the gOpportunities control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Web.UI.WebControls.GridViewRowEventArgs" /> instance containing the event data.</param>
        protected void gOpportunities_RowDataBound( object sender, System.Web.UI.WebControls.GridViewRowEventArgs e )
        {
            if ( e.Row.RowType != DataControlRowType.DataRow )
            {
                return;
            }

            var opportunity = e.Row.DataItem as Opportunity;
            if ( opportunity == null )
            {
                return;
            }

            if ( opportunity.ParticipantCount > 0 )
            {
                e.Row.AddCssClass( "js-has-participants" );
            }

            if ( e.Row.FindControl( "lParticipantCountBadgeHtml" ) is Literal lParticipantCountBadgeHtml )
            {
                lParticipantCountBadgeHtml.Text = opportunity.ParticipantCountBadgeHtml;
            }
        }

        /// <summary>
        /// Handles the RowSelected event of the gOpportunities control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="Rock.Web.UI.Controls.RowEventArgs" /> instance containing the event data.</param>
        protected void gOpportunities_RowSelected( object sender, Rock.Web.UI.Controls.RowEventArgs e )
        {
            if ( !string.IsNullOrWhiteSpace( GetAttributeValue( AttributeKey.ProjectDetailPage ) ) )
            {
                var keys = e.RowKeyValues;
                var qryParams = new Dictionary<string, string>
                {
                    { PageParameterKey.GroupId,  keys[DataKeyName.GroupId].ToString() }
                };

                NavigateToLinkedPage( AttributeKey.ProjectDetailPage, qryParams );
            }
        }

        /// <summary>
        /// Handles the Click event of the lbOpportunityDetail control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RowEventArgs"/> instance containing the event data.</param>
        protected void lbOpportunityDetail_Click( object sender, RowEventArgs e )
        {
            var keys = e.RowKeyValues;
            var qryParams = new Dictionary<string, string>
            {
                { PageParameterKey.GroupId,  keys[DataKeyName.GroupId].ToString() },
                { PageParameterKey.LocationId, keys[DataKeyName.LocationId].ToString() },
                { PageParameterKey.ScheduleId, keys[DataKeyName.ScheduleId].ToString() }
            };

            NavigateToLinkedPage( AttributeKey.SignUpOpportunityAttendeeListPage, qryParams );
        }

        /// <summary>
        /// Handles the GridRebind event of the gOpportunities control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="Rock.Web.UI.Controls.GridRebindEventArgs" /> instance containing the event data.</param>
        protected void gOpportunities_GridRebind( object sender, Rock.Web.UI.Controls.GridRebindEventArgs e )
        {
            BindOpportunitiesGrid();
        }

        /// <summary>
        /// Handles the Click event of the dfOpportunities control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RowEventArgs"/> instance containing the event data.</param>
        protected void dfOpportunities_Click( object sender, RowEventArgs e )
        {
            var groupId = e.RowKeyValues[DataKeyName.GroupId].ToIntSafe();
            var locationId = e.RowKeyValues[DataKeyName.LocationId].ToIntSafe();
            var scheduleId = e.RowKeyValues[DataKeyName.ScheduleId].ToIntSafe();

            using ( var rockContext = new RockContext() )
            {
                /*
                 * An Opportunity is a GroupLocationSchedule with possible GroupMemberAssignments (and therefore, GroupMembers).
                 * When deleting an Opportunity we should delete the following:
                 * 
                 * 1) GroupMemberAssignments
                 * 2) GroupMembers (if no more GroupMemberAssignents for a given GroupMember)
                 * 3) Schedule (if non-named and nothing else is using it)
                 * 4) GroupLocationScheduleConfig
                 * 5) GroupLocation (if no more Schedules tied to it)
                 */

                rockContext.SqlLogging( true );

                var groupMemberAssignmentService = new GroupMemberAssignmentService( rockContext );
                var groupMemberAssignments = groupMemberAssignmentService
                    .Queryable()
                    .Include( gma => gma.GroupMember )
                    .Include( gma => gma.GroupMember.GroupMemberAssignments )
                    .Where( gma => gma.GroupMember.GroupId == groupId
                        && gma.LocationId == locationId
                        && gma.ScheduleId == scheduleId
                    )
                    .ToList();

                // Set these aside so we can try to delete them next.
                var groupMembers = groupMemberAssignments
                    .Select( gma => gma.GroupMember )
                    .ToList();

                groupMemberAssignmentService.DeleteRange( groupMemberAssignments );

                var groupMemberService = new GroupMemberService( rockContext );
                foreach ( var groupMember in groupMembers.Where( gm => !gm.GroupMemberAssignments.Any() ) )
                {
                    // We can't delete these in bulk, as the individual Delete call will dynamically archive if necessary (whereas the bulk calls will not).
                    groupMemberService.Delete( groupMember );
                }

                var groupLocationService = new GroupLocationService( rockContext );
                var groupLocation = groupLocationService
                    .Queryable()
                    .Include( gl => gl.Schedules )
                    .Include( gl => gl.GroupLocationScheduleConfigs )
                    .FirstOrDefault( gl => gl.GroupId == groupId && gl.LocationId == locationId );

                // We'll have to delete these last, since we reference the Schedule.Id in the GroupLocationSchedule & GroupLocationScheduleConfig tables.
                var schedulesToDelete = groupLocation.Schedules
                    .Where( s => s.Id == scheduleId )
                    .ToList();

                foreach ( var schedule in schedulesToDelete )
                {
                    groupLocation.Schedules.Remove( schedule );
                }

                foreach ( var config in groupLocation.GroupLocationScheduleConfigs.Where( gls => gls.ScheduleId == scheduleId ).ToList() )
                {
                    groupLocation.GroupLocationScheduleConfigs.Remove( config );
                }

                // If this GroupLocation has no more Schedules, delete it.
                if ( !groupLocation.Schedules.Any() )
                {
                    groupLocationService.Delete( groupLocation );
                }

                rockContext.WrapTransaction( () =>
                {
                    // Initial save to release FK constraints tied to child entities we'll be deleting.
                    rockContext.SaveChanges();

                    var scheduleService = new ScheduleService( rockContext );
                    foreach ( var schedule in schedulesToDelete )
                    {
                        // Remove the schedule if custom (non-named) and nothing else is using it.
                        if ( schedule.ScheduleType != ScheduleType.Named && scheduleService.CanDelete( schedule, out string scheduleErrorMessage ) )
                        {
                            scheduleService.Delete( schedule );
                        }
                    }

                    /*
                     * We cannot safely remove child Locations (even non-named ones):
                     *   1) because of the way we reuse/share Locations across entities (the LocationPicker control auto-searches/matches and saves Locations).
                     *   2) because of the cascade deletes many of the referencing entities have on their LocationId FK constraints (we might accidentally delete a lot of unintended stuff).
                     */

                    // Follow-up save for deleted child entities.
                    rockContext.SaveChanges();
                } );

                rockContext.SqlLogging( false );
            }

            BindOpportunitiesGrid();
        }

        #endregion

        #region Internal Members

        /// <summary>
        /// Initializes the grid.
        /// </summary>
        private void InitializeGrid()
        {
            gOpportunities.ExportFilename = $"{SignUpGroupType.Name} Opportunities";

            // we'll have custom javascript (see SignUpOverview.ascx ) do this instead.
            gOpportunities.ShowConfirmDeleteDialog = false;
            gOpportunities.IsDeleteEnabled = _canEdit;

            gfOpportunities.UserPreferenceKeyPrefix = $"{SignUpGroupTypeId}-";
            SetGridFilters();
        }

        /// <summary>
        /// Sets the grid filters.
        /// </summary>
        private void SetGridFilters()
        {
            tbProjectName.Text = gfOpportunities.GetUserPreference( GridFilterKey.ProjectName );
        }

        /// <summary>
        /// Shows the details.
        /// </summary>
        private void ShowDetails()
        {
            BindOpportunitiesGrid();
        }

        private class Opportunity
        {
            private class BadgeType
            {
                public const string Danger = "danger";
                public const string Success = "success";
                public const string Warning = "warning";
            }

            private string ParticipantCountBadgeType
            {
                get
                {
                    if ( SlotsMax.GetValueOrDefault() > 0 )
                    {
                        return ParticipantCount == 0 || ParticipantCount < SlotsMin.GetValueOrDefault()
                            ? BadgeType.Warning
                            : ParticipantCount < SlotsMax.Value
                                ? BadgeType.Success
                                : BadgeType.Danger;
                    }
                    else if ( SlotsMin.GetValueOrDefault() > 0 )
                    {
                        return ParticipantCount < SlotsMin.Value
                            ? BadgeType.Warning
                            : BadgeType.Success;
                    }

                    return ParticipantCount > 0
                        ? BadgeType.Success
                        : BadgeType.Warning;
                }
            }

            public int GroupId { get; set; }

            public int GroupLocationId { get; set; }

            public int LocationId { get; set; }

            public int ScheduleId { get; set; }

            public string ProjectName { get; set; }

            public DateTime? LastStartDateTime { get; set; }

            public DateTime? NextStartDateTime { get; set; }

            public string FriendlySchedule { get; set; }

            public int? SlotsMin { get; set; }

            public int? SlotsDesired { get; set; }

            public int? SlotsMax { get; set; }

            public int LeaderCount { get; set; }

            public int ParticipantCount { get; set; }

            public string ParticipantCountBadgeHtml
            {
                get
                {
                    return $"<span class='badge badge-{ParticipantCountBadgeType} participant-count-badge'>{ParticipantCount}</span>";
                }
            }
        }

        /// <summary>
        /// Binds the opportunities grid.
        /// </summary>
        private void BindOpportunitiesGrid()
        {
            List<Opportunity> opportunities = null;

            using ( var rockContext = new RockContext() )
            {
                opportunities = GetOpportunities( rockContext );
            }

            gOpportunities.DataSource = opportunities;
            gOpportunities.DataBind();
        }

        /// <summary>
        /// Gets the opportunities.
        /// </summary>
        /// <param name="rockContext">The rock context.</param>
        /// <returns></returns>
        private List<Opportunity> GetOpportunities( RockContext rockContext )
        {
            var qry = new GroupLocationService( rockContext )
                .Queryable()
                .AsNoTracking()
                .Where( gl =>
                    gl.Group.GroupTypeId == SignUpGroupTypeId
                    || gl.Group.GroupType.InheritedGroupTypeId == SignUpGroupTypeId
                );

            // Filter by project name.
            var projectName = tbProjectName.Text;
            if ( !string.IsNullOrWhiteSpace( projectName ) )
            {
                qry = qry.Where( gl => gl.Group.Name.StartsWith( projectName ) );
            }

            // Project our query into a runtime object collection.
            var groupLocationSchedules = qry
                .SelectMany( gl => gl.Schedules, ( gl, s ) => new
                {
                    gl.Group,
                    GroupLocationId = gl.Id,
                    gl.Location,
                    Schedule = s,
                    Config = gl.GroupLocationScheduleConfigs.FirstOrDefault( glsc => glsc.ScheduleId == s.Id )
                } )
                .ToList();

            // Go get all attendees for all opportunities; we'll hook them up to their respective opportunities below.
            var attendees = new GroupMemberAssignmentService( rockContext )
                .Queryable()
                .AsNoTracking()
                .Include( gma => gma.GroupMember.GroupRole )
                .Where( gma =>
                    gma.GroupMember.Group.GroupTypeId == SignUpGroupTypeId
                    || gma.GroupMember.Group.GroupType.InheritedGroupTypeId == SignUpGroupTypeId
                )
                .ToList();

            var opportunities = groupLocationSchedules
                .Select( gls =>
                {
                    var locationId = gls.Location.Id;
                    var scheduleId = gls.Schedule.Id;

                    var participants = attendees
                        .Where( a => a.LocationId == locationId && a.ScheduleId == scheduleId )
                        .ToList();

                    var now = gls.Group.Campus?.CurrentDateTime ?? RockDateTime.Now;
                    var nextStartDateTime = gls.Schedule.GetNextStartDateTime( now );

                    return new Opportunity
                    {
                        GroupId = gls.Group.Id,
                        GroupLocationId = gls.GroupLocationId,
                        LocationId = locationId,
                        ScheduleId = scheduleId,
                        ProjectName = gls.Group.Name,
                        NextStartDateTime = nextStartDateTime,
                        FriendlySchedule = nextStartDateTime.ToRelativeDateString(),
                        SlotsMin = gls.Config?.MinimumCapacity,
                        SlotsDesired = gls.Config?.DesiredCapacity,
                        SlotsMax = gls.Config?.MaximumCapacity,
                        LeaderCount = participants.Count( p => p.GroupMember.GroupRole.IsLeader ),
                        ParticipantCount = participants.Count
                    };
                } )
                .ToList();

            return opportunities;
        }

        #endregion
    }
}
