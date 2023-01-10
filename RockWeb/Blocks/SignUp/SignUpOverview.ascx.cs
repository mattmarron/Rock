using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using Newtonsoft.Json;
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
    public partial class SignUpOverview : RockBlock, IPostBackEventHandler
    {
        #region Private Keys

        private static class PageParameterKey
        {
            public const string CommunicationId = "CommunicationId";
            public const string GroupId = "GroupId";
            public const string LocationId = "LocationId";
            public const string ScheduleId = "ScheduleId";
        }

        private static class AttributeKey
        {
            public const string ProjectDetailPage = "ProjectDetailPage";
            public const string SignUpOpportunityAttendeeListPage = "SignUpOpportunityAttendeeListPage";
        }

        private class ViewStateKey
        {
            public const string OpportunitiesState = "OpportunitiesState";
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

        private static class GridAction
        {
            public const string Inactivate = "INACTIVATE";
            public const string EmailLeaders = "EMAIL_LEADERS";
            public const string EmailAll = "EMAIL_ALL";
            public const string None = "";
        }

        private static class PostbackEventArgument
        {
            public const string GridActionChanged = "GridActionChanged";
        }

        private static class MergeFieldKey
        {
            public static string Group = EntityTypeCache.Get( Rock.SystemGuid.EntityType.GROUP ).Name;
            public static string GroupLocation = EntityTypeCache.Get( Rock.SystemGuid.EntityType.GROUP_LOCATION ).Name;
            public static string Location = EntityTypeCache.Get( Rock.SystemGuid.EntityType.LOCATION ).Name;
            public static string Schedule = EntityTypeCache.Get( Rock.SystemGuid.EntityType.SCHEDULE ).Name;
            public const string ProjectName = "ProjectName";
            public const string LeaderCount = "LeaderCount";
            public const string ParticipantCount = "ParticipantCount";
        }

        #endregion

        #region Fields

        private bool _canEdit;
        private RockDropDownList _ddlAction;

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
                return this.SignUpGroupType?.Id ?? 0;
            }
        }

        private List<Opportunity> OpportunitiesState { get; set; }

        #endregion

        #region Control Life-Cycle Events

        /// <summary>
        /// Restores the view-state information from a previous user control request that was saved by the <see cref="M:System.Web.UI.UserControl.SaveViewState" /> method.
        /// </summary>
        /// <param name="savedState">An <see cref="T:System.Object" /> that represents the user control state to be restored.</param>
        protected override void LoadViewState( object savedState )
        {
            base.LoadViewState( savedState );

            var json = ViewState[ViewStateKey.OpportunitiesState] as string;
            if ( string.IsNullOrWhiteSpace( json ) )
            {
                this.OpportunitiesState = new List<Opportunity>();
            }
            else
            {
                this.OpportunitiesState = JsonConvert.DeserializeObject<List<Opportunity>>( json ) ?? new List<Opportunity>();
            }
        }

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

            if ( !this.Page.IsPostBack )
            {
                BindOpportunitiesGrid();
                SetGridFilters();
            }
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

        /// <summary>
        /// When implemented by a class, enables a server control to process an event raised when a form is posted to the server.
        /// </summary>
        /// <param name="eventArgument">A <see cref="T:System.String" /> that represents an optional event argument to be passed to the event handler.</param>
        public void RaisePostBackEvent( string eventArgument )
        {
            if ( eventArgument != PostbackEventArgument.GridActionChanged || hfAction.Value.IsNullOrWhiteSpace() )
            {
                return;
            }

            var selectedGuids = gOpportunities.SelectedKeys.Select( k => ( Guid ) k ).ToList();
            if ( selectedGuids.Any() )
            {
                var selectedOpportunities = this.OpportunitiesState
                    .Where( o => selectedGuids.Contains( o.Guid ) )
                    .ToList();

                switch ( hfAction.Value )
                {
                    case GridAction.Inactivate:
                        InactivateOpportunities( selectedOpportunities );
                        break;
                    case GridAction.EmailLeaders:
                        EmailParticipants( selectedOpportunities, true );
                        break;
                    case GridAction.EmailAll:
                        EmailParticipants( selectedOpportunities );
                        break;
                }
            }

            _ddlAction.SelectedIndex = 0;
            hfAction.Value = string.Empty;
            gOpportunities.SelectedKeys.Clear();
            BindOpportunitiesGrid();
        }

        /// <summary>
        /// Saves any user control view-state changes that have occurred since the last page postback.
        /// </summary>
        /// <returns>
        /// Returns the user control's current view state. If there is no view state associated with the control, it returns <see langword="null" />.
        /// </returns>
        protected override object SaveViewState()
        {
            var jsonSetting = new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                ContractResolver = new Rock.Utility.IgnoreUrlEncodedKeyContractResolver()
            };

            ViewState[ViewStateKey.OpportunitiesState] = JsonConvert.SerializeObject( this.OpportunitiesState, Formatting.None, jsonSetting );

            return base.SaveViewState();
        }

        #endregion

        #region Opportunities Grid Events

        /// <summary>
        /// Displays the gfOpportunities filter values.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The e.</param>
        protected void gfOpportunities_DisplayFilterValue( object sender, GridFilter.DisplayFilterValueArgs e )
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
        /// <param name="e">The <see cref="RowEventArgs" /> instance containing the event data.</param>
        protected void gOpportunities_RowSelected( object sender, RowEventArgs e )
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
        /// <param name="e">The <see cref="GridRebindEventArgs" /> instance containing the event data.</param>
        protected void gOpportunities_GridRebind( object sender, GridRebindEventArgs e )
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

            /*
             * We should consider moving this logic to a service (probably the GroupLocationService), as this code block is identical
             * to that found within the SignUpDetail block's gOpportunities_Delete() method.
             */

            using ( var rockContext = new RockContext() )
            {
                /*
                 * An Opportunity is a GroupLocationSchedule with possible GroupMemberAssignments (and therefore, GroupMembers).
                 * When deleting an Opportunity we should delete the following:
                 * 
                 * 1) GroupMemberAssignments
                 * 2) GroupMembers (if no more GroupMemberAssignents for a given GroupMember)
                 * 3) GroupLocationSchedule & GroupLocationScheduleConfig
                 * 4) GroupLocation (if no more Schedules tied to it)
                 * 5) Schedule (if non-named and nothing else is using it)
                 */

                rockContext.SqlLogging( true );

                var groupMemberAssignmentService = new GroupMemberAssignmentService( rockContext );
                var groupMemberAssignments = groupMemberAssignmentService
                    .Queryable()
                    .Include( gma => gma.GroupMember )
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
                    // We need to delete these one-by-one, as the individual Delete call will dynamically archive if necessary (whereas the bulk delete calls will not).
                    groupMemberService.Delete( groupMember );
                }

                // Now go get the GroupLocation, Schedule & GroupLocationScheduleConfig.
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
                    // Note that if there happen to be any lingering GroupLocationScheduleConfig records that somehow weren't deleted yet, a cascade delete will get rid of them here.
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
            gOpportunities.ExportFilename = $"{this.SignUpGroupType.Name} Opportunities";

            // we'll have custom javascript (see SignUpOverview.ascx ) do this instead.
            gOpportunities.ShowConfirmDeleteDialog = false;
            gOpportunities.IsDeleteEnabled = _canEdit;

            _ddlAction = new RockDropDownList();
            _ddlAction.ID = "ddlAction";
            _ddlAction.CssClass = "pull-left input-width-xl";
            _ddlAction.Items.Add( new ListItem( "-- Select Action --", GridAction.None ) );
            _ddlAction.Items.Add( new ListItem( "Inactivate Selected Projects", GridAction.Inactivate ) );
            _ddlAction.Items.Add( new ListItem( "Email Leaders of Selected Schedules", GridAction.EmailLeaders ) );
            _ddlAction.Items.Add( new ListItem( "Email All Participants of Selected Schedules", GridAction.EmailAll ) );

            gOpportunities.Actions.AddCustomActionControl( _ddlAction );

            gfOpportunities.UserPreferenceKeyPrefix = $"{this.SignUpGroupTypeId}-";
        }

        /// <summary>
        /// Sets the grid filters.
        /// </summary>
        private void SetGridFilters()
        {
            tbProjectName.Text = gfOpportunities.GetUserPreference( GridFilterKey.ProjectName );
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
                        return this.ParticipantCount == 0 || this.ParticipantCount < this.SlotsMin.GetValueOrDefault()
                            ? BadgeType.Warning
                            : this.ParticipantCount < this.SlotsMax.Value
                                ? BadgeType.Success
                                : BadgeType.Danger;
                    }
                    else if ( this.SlotsMin.GetValueOrDefault() > 0 )
                    {
                        return this.ParticipantCount < this.SlotsMin.Value
                            ? BadgeType.Warning
                            : BadgeType.Success;
                    }

                    return this.ParticipantCount > 0
                        ? BadgeType.Success
                        : BadgeType.Warning;
                }
            }

            /// <summary>
            /// This is a runtime Guid, not related to any Entity in particular.
            /// </summary>
            public Guid Guid { get; set; }

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
                    return $"<span class='badge badge-{this.ParticipantCountBadgeType} participant-count-badge'>{this.ParticipantCount}</span>";
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

            RegisterJavaScriptForGridActions();
        }

        /// <summary>
        /// Gets the opportunities.
        /// </summary>
        /// <param name="rockContext">The rock context.</param>
        /// <returns></returns>
        private List<Opportunity> GetOpportunities( RockContext rockContext )
        {
            rockContext.SqlLogging( true );

            var qry = new GroupLocationService( rockContext )
                .Queryable()
                .AsNoTracking()
                .Where( gl =>
                    gl.Group.IsActive
                    && ( gl.Group.GroupTypeId == this.SignUpGroupTypeId || gl.Group.GroupType.InheritedGroupTypeId == this.SignUpGroupTypeId )
                );

            // Filter by project name.
            var projectName = tbProjectName.Text;
            if ( !string.IsNullOrWhiteSpace( projectName ) )
            {
                qry = qry.Where( gl => gl.Group.Name.StartsWith( projectName ) );
            }

            // Get the current opportunities (GroupLocationSchedules).
            var qryGroupLocationSchedules = qry
                .SelectMany( gl => gl.Schedules, ( gl, s ) => new
                {
                    gl.Group,
                    GroupLocationId = gl.Id,
                    gl.Location,
                    Schedule = s,
                    Config = gl.GroupLocationScheduleConfigs.FirstOrDefault( glsc => glsc.ScheduleId == s.Id )
                } )
                .Where( gls => !gls.Schedule.EffectiveEndDate.HasValue || gls.Schedule.EffectiveEndDate >= RockDateTime.Now );

            // Get all attendees for all current opportunities; we'll hook them up to their respective opportunities below.
            var attendees = new GroupMemberAssignmentService( rockContext )
                .Queryable()
                .AsNoTracking()
                .Include( gma => gma.GroupMember.GroupRole )
                .Where( gma =>
                    qryGroupLocationSchedules.Any( gls => gls.Group.Id == gma.GroupMember.GroupId
                        && gls.Location.Id == gma.LocationId
                        && gls.Schedule.Id == gma.ScheduleId )
                )
                .ToList();

            var opportunities = qryGroupLocationSchedules
                .ToList()
                .Select( gls =>
                {
                    var locationId = gls.Location.Id;
                    var scheduleId = gls.Schedule.Id;

                    var participants = attendees
                        .Where( a => a.LocationId == locationId && a.ScheduleId == scheduleId )
                        .ToList();

                    var nextStartDateTime = gls.Schedule.NextStartDateTime;

                    return new Opportunity
                    {
                        Guid = Guid.NewGuid(),
                        GroupId = gls.Group.Id,
                        GroupLocationId = gls.GroupLocationId,
                        LocationId = locationId,
                        ScheduleId = scheduleId,
                        ProjectName = gls.Group.Name,
                        NextStartDateTime = nextStartDateTime,
                        FriendlySchedule = nextStartDateTime?.ToRelativeDateString(),
                        SlotsMin = gls.Config?.MinimumCapacity,
                        SlotsDesired = gls.Config?.DesiredCapacity,
                        SlotsMax = gls.Config?.MaximumCapacity,
                        LeaderCount = participants.Count( p => p.GroupMember.GroupRole.IsLeader ),
                        ParticipantCount = participants.Count
                    };
                } )
                .ToList();

            this.OpportunitiesState = opportunities;

            rockContext.SqlLogging( false );

            return opportunities;
        }

        /// <summary>
        /// Registers the JavaScript for grid actions.
        /// NOTE: This needs to be done after binding the grid.
        /// </summary>
        private void RegisterJavaScriptForGridActions()
        {
            string script = $@"
                $('#{_ddlAction.ClientID}').on('change', function(e){{
                    var $ddl = $(this);
                    var action = $ddl.val();
                    $('#{hfAction.ClientID}').val(action);

                    var count = $(""#{gOpportunities.ClientID} input[id$='_cbSelect_0']:checked"").length;
                    if (action === '{GridAction.None}' || count === 0) {{
                        return;
                    }}

                    if ($ddl.val() === '{GridAction.Inactivate}') {{
                        Rock.dialogs.confirm('Are you sure you want to inactivate the selected projects?', function (result) {{
                            if (!result) {{
                                return;
                            }}

                            window.location = ""javascript:{Page.ClientScript.GetPostBackEventReference( this, PostbackEventArgument.GridActionChanged )}"";
                            $ddl.val('');
                        }});
                    }} else {{
                        window.location = ""javascript:{Page.ClientScript.GetPostBackEventReference( this, PostbackEventArgument.GridActionChanged )}"";
                        $ddl.val('');
                    }}
                }});";

            ScriptManager.RegisterStartupScript( _ddlAction, _ddlAction.GetType(), "ProcessGridActionChange", script, true );
        }

        /// <summary>
        /// Inactivates the opportunities.
        /// </summary>
        /// <param name="opportunities">The opportunities.</param>
        private void InactivateOpportunities( List<Opportunity> opportunities )
        {
            if ( !opportunities.Any() )
            {
                return;
            }

            /*
             * TBD: Waiting to hear back from Nick regarding exactly what we're inactivating here. Options are:
             * 1) Project [Group], which would inactivate all opportunities [GroupLocationSchedules] under this project.
             * 2) Schedule - just this opportunity.
             *      a) This could prove difficult if a named (shared) schedule is used; we'd need a new [IsActive] bit field at the [GroupLocationScheduleConfig] level, I think.
             * 
             * Note that we'll also need to change our query within the GetOpportunities() method of this block and the BindOpportunitiesGrid() method of the SignUpDetail block,
             * to ensure we're only showing active opportunities within their respective grids.
             */
        }


        /// <summary>
        /// Emails the participants.
        /// </summary>
        /// <param name="opportunities">The opportunities.</param>
        /// <param name="shouldOnlyEmailLeaders">if set to <c>true</c> [should only email leaders].</param>
        private void EmailParticipants( List<Opportunity> opportunities, bool shouldOnlyEmailLeaders = false )
        {
            // These lists of selected Group/Location/Schedule IDs should be pretty small; SQL WHERE IN clauses should be safe here.
            var distinctGroupIds = opportunities.Select( o => o.GroupId ).Distinct().ToList();
            var distinctLocationIds = opportunities.Select( o => o.LocationId ).Distinct().ToList();
            var distinctScheduleIds = opportunities.Select( o => o.ScheduleId ).Distinct().ToList();

            using ( var rockContext = new RockContext() )
            using ( var communicationRecipientRockContext = new RockContext() )
            {
                var qry = new GroupMemberAssignmentService( rockContext )
                    .Queryable()
                    .AsNoTracking()
                    .Where(
                        gma => distinctGroupIds.Contains( gma.GroupMember.GroupId )
                                && gma.LocationId.HasValue && distinctLocationIds.Contains( gma.LocationId.Value )
                                && gma.ScheduleId.HasValue && distinctScheduleIds.Contains( gma.ScheduleId.Value )
                    );

                if ( shouldOnlyEmailLeaders )
                {
                    qry = qry.Where( gma => gma.GroupMember.GroupRole.IsLeader );
                }

                var participants = qry
                    .Select( gma => new
                    {
                        gma.GroupMember.PersonId,
                        gma.GroupMember.GroupId,
                        gma.LocationId,
                        gma.ScheduleId
                    } )
                    .ToList();

                var distinctPersonIds = participants
                    .Select( p => p.PersonId )
                    .Distinct()
                    .ToList();

                if ( !distinctPersonIds.Any() )
                {
                    mdSignUpOverview.Show( "Unable to send email, as no recipients were found.", ModalAlertType.Information );
                    return;
                }

                // Get the primary aliases.
                var personAliasService = new PersonAliasService( rockContext );
                var primaryAliasList = new List<PersonAlias>( distinctPersonIds.Count );

                // Get the data in chunks just in case we have a large list of PersonIds (to avoid a SQL Expression limit error).
                var chunkedPersonIds = distinctPersonIds.Take( 1000 );
                var skipCount = 0;
                while ( chunkedPersonIds.Any() )
                {
                    var chunkedPrimaryAliasList = personAliasService
                        .Queryable()
                        .AsNoTracking()
                        .Where( pa => pa.PersonId == pa.AliasPersonId && chunkedPersonIds.Contains( pa.PersonId ) )
                        .ToList();

                    primaryAliasList.AddRange( chunkedPrimaryAliasList );

                    skipCount += 1000;
                    chunkedPersonIds = distinctPersonIds.Skip( skipCount ).Take( 1000 );
                }

                var currentPersonAliasId = this.RockPage.CurrentPersonAliasId;

                // Add custom merge fields.
                var mergeFields = new List<string>
                {
                    MergeFieldKey.Group,
                    MergeFieldKey.Location,
                    MergeFieldKey.Schedule,
                    MergeFieldKey.ProjectName,
                    MergeFieldKey.LeaderCount,
                    MergeFieldKey.ParticipantCount
                };

                // Create communication.
                var communication = new Communication
                {
                    IsBulkCommunication = true,
                    Status = CommunicationStatus.Transient,
                    SenderPersonAliasId = currentPersonAliasId,
                    AdditionalMergeFields = mergeFields
                };

                communication.UrlReferrer = this.RockPage.Request?.UrlProxySafe()?.AbsoluteUri?.TrimForMaxLength( communication, "UrlReferrer" );

                var communicationService = new CommunicationService( rockContext );
                communicationService.Add( communication );

                // Save Communication to get ID.
                rockContext.SaveChanges();

                var now = RockDateTime.Now;

                var communicationRecipientList = primaryAliasList
                    .Select( a =>
                    {
                        var participant = participants.FirstOrDefault( p => p.PersonId == a.PersonId );
                        var opportunity = participant != null
                            ? opportunities.FirstOrDefault( o =>
                                o.GroupId == participant.GroupId
                                && o.LocationId == participant.LocationId.ToIntSafe()
                                && o.ScheduleId == participant.ScheduleId.ToIntSafe()
                            )
                            : null;

                        return new CommunicationRecipient
                        {
                            CommunicationId = communication.Id,
                            PersonAliasId = a.Id,
                            AdditionalMergeValues = new Dictionary<string, object>
                            {
                                { MergeFieldKey.Group, opportunity?.GroupId },
                                { MergeFieldKey.Location, opportunity?.LocationId },
                                { MergeFieldKey.Schedule, opportunity?.ScheduleId },
                                { MergeFieldKey.ProjectName, opportunity?.ProjectName },
                                { MergeFieldKey.LeaderCount, opportunity?.LeaderCount },
                                { MergeFieldKey.ParticipantCount, opportunity?.ParticipantCount },
                            },
                            CreatedByPersonAliasId = currentPersonAliasId,
                            ModifiedByPersonAliasId = currentPersonAliasId,
                            CreatedDateTime = now,
                            ModifiedDateTime = now
                        };
                    } )
                    .ToList();

                // BulkInsert to quickly insert the CommunicationRecipient records. Note: This is much faster, but will bypass EF and Rock processing.
                communicationRecipientRockContext.BulkInsert( communicationRecipientList );

                // Get the URL to the communication page.
                var communicationPageRef = this.RockPage.Site.CommunicationPageReference;
                string communicationUrl;
                if ( communicationPageRef.PageId > 0 )
                {
                    communicationPageRef.Parameters.AddOrReplace( PageParameterKey.CommunicationId, communication.Id.ToString() );
                    communicationUrl = communicationPageRef.BuildUrl();
                }
                else
                {
                    communicationUrl = "~/Communication/{0}";
                }

                this.Page.Response.Redirect( communicationUrl, false );
                this.Context.ApplicationInstance.CompleteRequest();
            }
        }

        #endregion
    }
}
