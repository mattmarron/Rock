using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using Rock;
using Rock.Attribute;
using Rock.Constants;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace RockWeb.Blocks.SignUp
{
    [DisplayName( "Sign-Up Opportunity Attendee List" )]
    [Category( "Sign-Up" )]
    [Description( "Lists all the group members for the selected group, location and schedule." )]

    #region Block Attributes

    [LinkedPage( "Group Member Detail Page",
        Key = AttributeKey.GroupMemberDetailPage,
        Description = "Page used for viewing an attendee's group member detail for this Sign-Up project. Clicking a row in the grid will take you to this page.",
        IsRequired = true,
        Order = 0 )]

    [LinkedPage( "Person Profile Page",
        Key = AttributeKey.PersonProfilePage,
        Description = "Page used for viewing a person's profile. If set, a view profile button will show for each group member.",
        IsRequired = false,
        Order = 1 )]

    #endregion

    [Rock.SystemGuid.BlockTypeGuid( "EE652767-5070-4EAB-8BB7-BB254DD01B46" )]
    public partial class SignUpOpportunityAttendeeList : RockBlock
    {
        #region Private Members

        private static class PageParameterKey
        {
            public const string GroupId = "GroupId";
            public const string GroupMemberId = "GroupMemberId";
            public const string LocationId = "LocationId";
            public const string ScheduleId = "ScheduleId";
        }

        private static class AttributeKey
        {
            public const string GroupMemberDetailPage = "GroupMemberDetailPage";
            public const string PersonProfilePage = "PersonProfilePage";
        }

        private static class GridFilterKey
        {
            public const string FirstName = "FirstName";
            public const string LastName = "LastName";
            public const string Role = "Role";
            public const string Status = "Status";
            public const string Campus = "Campus";
            public const string Gender = "Gender";
        }

        private int _groupId;
        private int _locationId;
        private int _scheduleId;

        private GroupTypeCache _groupTypeCache;

        private readonly string _photoFormat = "<div class=\"photo-icon photo-round photo-round-xs pull-left margin-r-sm js-person-popover\" personid=\"{0}\" data-original=\"{1}&w=50\" style=\"background-image: url( '{2}' ); background-size: cover; background-repeat: no-repeat;\"></div>";

        private bool _isCommunicating = false;
        private bool _isExporting = false;

        private Dictionary<int, Location> _personIdHomeLocationLookup = null;
        private Dictionary<int, Dictionary<int, string>> _personIdPhoneNumberTypePhoneNumberLookup = null;

        private HashSet<int> _groupMembersWithGroupMemberHistory = null;
        private HashSet<int> _groupTypeRoleIdsWithGroupSync = null;

        private DeleteField _deleteField = null;
        private int? _deleteFieldColumnIndex = null;

        #endregion

        #region Properties

        private int SignUpGroupTypeId
        {
            get
            {
                return GroupTypeCache.Get( Rock.SystemGuid.GroupType.GROUPTYPE_SIGNUP_GROUP )?.Id ?? 0;
            }
        }

        private int? InactiveRecordStatusValueId
        {
            get
            {
                return DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_INACTIVE )?.Id;
            }
        }

        private int? FamilyGroupTypeId
        {
            get
            {
                return GroupTypeCache.Get( Rock.SystemGuid.GroupType.GROUPTYPE_FAMILY )?.Id;
            }
        }

        private DefinedValueCache HomeAddressDefinedValue
        {
            get
            {
                return DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.GROUP_LOCATION_TYPE_HOME );
            }
        }

        private int? HomePhoneTypeId
        {
            get
            {
                return DefinedValueCache.GetId( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_HOME.AsGuid() );
            }
        }

        private int? CellPhoneTypeId
        {
            get
            {
                return DefinedValueCache.GetId( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_MOBILE.AsGuid() );
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

            _groupId = PageParameter( PageParameterKey.GroupId ).ToIntSafe();
            _locationId = PageParameter( PageParameterKey.LocationId ).ToIntSafe();
            _scheduleId = PageParameter( PageParameterKey.ScheduleId ).ToIntSafe();

            using ( var rockContext = new RockContext() )
            {
                var group = GetSharedGroup( rockContext );
                if ( group != null )
                {
                    InitializeGrid( group );
                }
            }

            // Add lazyload so that person-link-popover javascript works
            RockPage.AddScriptLink( "~/Scripts/jquery.lazyload.min.js" );

            // This event gets fired after block settings are updated. It's nice to repaint the screen if these settings would alter it.
            this.BlockUpdated += Block_BlockUpdated;
            this.AddConfigurationUpdateTrigger( upnlSignUpOpportunityAttendeeList );
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

        #region Attendees Grid Events

        /// <summary>
        /// Displays the gfAttendees filter values.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The e.</param>
        protected void gfAttendees_DisplayFilterValue( object sender, GridFilter.DisplayFilterValueArgs e )
        {
            if ( e.Key == GridFilterKey.FirstName )
            {
                return;
            }
            else if ( e.Key == GridFilterKey.LastName )
            {
                return;
            }
            else if ( e.Key == GridFilterKey.Role )
            {
                e.Value = ResolveCheckBoxListValues( e.Value, cblRole );
            }
            else if ( e.Key == GridFilterKey.Status )
            {
                e.Value = ResolveCheckBoxListValues( e.Value, cblGroupMemberStatus );
            }
            else if ( e.Key == GridFilterKey.Campus )
            {
                var campusId = e.Value.AsIntegerOrNull();
                if ( campusId.HasValue )
                {
                    var campusCache = CampusCache.Get( campusId.Value );
                    if ( campusCache != null )
                    {
                        e.Value = campusCache.Name;
                    }
                    else
                    {
                        e.Value = string.Empty;
                    }
                }
                else
                {
                    e.Value = string.Empty;
                }
            }
            else if ( e.Key == GridFilterKey.Gender )
            {
                e.Value = ResolveCheckBoxListValues( e.Value, cblGenderFilter );
            }
            else
            {
                e.Value = string.Empty;
            }
        }

        /// <summary>
        /// Handles the ApplyFilterClick event of the gfAttendees control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void gfAttendees_ApplyFilterClick( object sender, EventArgs e )
        {
            gfAttendees.SaveUserPreference( GridFilterKey.FirstName, "First Name", tbFirstName.Text );
            gfAttendees.SaveUserPreference( GridFilterKey.LastName, "Last Name", tbLastName.Text );
            gfAttendees.SaveUserPreference( GridFilterKey.Role, "Role", cblRole.SelectedValues.AsDelimited( ";" ) );
            gfAttendees.SaveUserPreference( GridFilterKey.Status, "Status", cblGroupMemberStatus.SelectedValues.AsDelimited( ";" ) );
            gfAttendees.SaveUserPreference( GridFilterKey.Campus, "Campus", cpCampusFilter.SelectedCampusId.ToString() );
            gfAttendees.SaveUserPreference( GridFilterKey.Gender, "Gender", cblGenderFilter.SelectedValues.AsDelimited( ";" ) );

            BindAttendeesGrid();
        }

        /// <summary>
        /// Handles the ClearFilterClick event of the gfAttendees control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void gfAttendees_ClearFilterClick( object sender, EventArgs e )
        {
            gfAttendees.DeleteUserPreferences();

            using ( var rockContext = new RockContext() )
            {
                SetGridFilters( GetSharedGroup( rockContext ) );
            }
        }

        /// <summary>
        /// Handles the RowDataBound event of the gAttendees control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Web.UI.WebControls.GridViewRowEventArgs"/> instance containing the event data.</param>
        protected void gAttendees_RowDataBound( object sender, System.Web.UI.WebControls.GridViewRowEventArgs e )
        {
            if ( e.Row.RowType != DataControlRowType.DataRow )
            {
                return;
            }

            var groupMemberAssignment = e.Row.DataItem as GroupMemberAssignment;
            GroupMember groupMember = groupMemberAssignment?.GroupMember;
            if ( groupMemberAssignment == null || groupMember == null || groupMember.Person == null )
            {
                return;
            }

            if ( e.Row.FindControl( "lNameWithHtml" ) is Literal lNameWithHtml )
            {
                var nameHtml = new StringBuilder();
                nameHtml.AppendFormat( _photoFormat, groupMember.PersonId, groupMember.Person.PhotoUrl, ResolveUrl( "~/Assets/Images/person-no-photo-unknown.svg" ) );
                nameHtml.Append( groupMember.Person.FullName );
                if ( groupMember.Person.TopSignalColor.IsNotNullOrWhiteSpace() )
                {
                    nameHtml.Append( " " + groupMember.Person.GetSignalMarkup() );
                }

                if ( groupMember.Note.IsNotNullOrWhiteSpace() )
                {
                    nameHtml.Append( " <span class='js-group-member-note' data-toggle='tooltip' data-placement='top' title='" + groupMember.Note.EncodeHtml() + "'><i class='fa fa-file-text-o text-info'></i></span>" );
                }

                lNameWithHtml.Text = nameHtml.ToString();
            }

            if ( _isExporting )
            {
                if ( e.Row.FindControl( "lExportFullName" ) is Literal lExportFullName )
                {
                    lExportFullName.Text = groupMember.Person.FullNameReversed;
                }

                var personPhoneNumbers = _personIdPhoneNumberTypePhoneNumberLookup.GetValueOrNull( groupMember.PersonId );
                if ( personPhoneNumbers != null )
                {
                    if ( HomePhoneTypeId.HasValue && e.Row.FindControl( "lExportHomePhone" ) is Literal lExportHomePhone )
                    {
                        lExportHomePhone.Text = personPhoneNumbers.GetValueOrNull( HomePhoneTypeId.Value );
                    }

                    if ( CellPhoneTypeId.HasValue && e.Row.FindControl( "lExportCellPhone" ) is Literal lExportCellPhone )
                    {
                        lExportCellPhone.Text = personPhoneNumbers.GetValueOrNull( CellPhoneTypeId.Value );
                    }
                }

                var homeLocation = _personIdHomeLocationLookup.GetValueOrNull( groupMember.PersonId );
                if ( homeLocation != null )
                {
                    if ( e.Row.FindControl( "lExportHomeAddress" ) is Literal lExportHomeAddress )
                    {
                        lExportHomeAddress.Text = homeLocation.FormattedAddress;
                    }

                    if ( e.Row.FindControl( "lExportLatitude" ) is Literal lExportLatitude )
                    {
                        lExportLatitude.Text = homeLocation.Latitude.ToString();
                    }

                    if ( e.Row.FindControl( "lExportLongitude" ) is Literal lExportLongitude )
                    {
                        lExportLongitude.Text = homeLocation.Longitude.ToString();
                    }
                }
            }

            if ( _deleteField?.Visible == true )
            {
                LinkButton deleteButton = null;
                HtmlGenericControl buttonIcon = null;

                if ( !_deleteFieldColumnIndex.HasValue )
                {
                    _deleteFieldColumnIndex = gAttendees.GetColumnIndex( gAttendees.Columns.OfType<DeleteField>().First() );
                }

                if ( _deleteFieldColumnIndex.HasValue && _deleteFieldColumnIndex > -1 )
                {
                    deleteButton = e.Row.Cells[_deleteFieldColumnIndex.Value].ControlsOfTypeRecursive<LinkButton>().FirstOrDefault();
                }

                if ( deleteButton != null )
                {
                    buttonIcon = deleteButton.ControlsOfTypeRecursive<HtmlGenericControl>().FirstOrDefault();
                }

                if ( buttonIcon != null )
                {
                    if ( _groupTypeRoleIdsWithGroupSync.Contains( groupMember.GroupRoleId ) )
                    {
                        deleteButton.Enabled = false;
                        buttonIcon.Attributes["class"] = "fa fa-exchange";
                        var groupTypeRole = _groupTypeCache.Roles.FirstOrDefault( a => a.Id == groupMember.GroupRoleId );
                        deleteButton.ToolTip = string.Format( "Managed by group sync for role \"{0}\".", groupTypeRole );
                    }
                    else if ( _groupTypeCache.EnableGroupHistory == true && _groupMembersWithGroupMemberHistory.Contains( groupMember.Id ) )
                    {
                        buttonIcon.Attributes["class"] = "fa fa-archive";
                        deleteButton.AddCssClass( "btn-danger" );
                        deleteButton.ToolTip = "Archive";
                        e.Row.AddCssClass( "js-has-grouphistory" );
                    }
                }
            }

            if ( groupMember.Person.IsDeceased )
            {
                e.Row.AddCssClass( "is-deceased" );
            }

            if ( InactiveRecordStatusValueId.HasValue && groupMember.Person.RecordStatusValueId == InactiveRecordStatusValueId.Value )
            {
                e.Row.AddCssClass( "is-inactive-person" );
            }

            if ( groupMember.GroupMemberStatus == GroupMemberStatus.Inactive )
            {
                e.Row.AddCssClass( "is-inactive" );
            }
        }

        /// <summary>
        /// Handles the RowSelected event of the gAttendees control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RowEventArgs"/> instance containing the event data.</param>
        protected void gAttendees_RowSelected( object sender, RowEventArgs e )
        {
            NavigateToGroupMemberDetailPage( e.RowKeyValues[nameof( GroupMemberAssignment.GroupMemberId )].ToIntSafe() );
        }

        /// <summary>
        /// Handles the GridRebind event of the gAttendees control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="Rock.Web.UI.Controls.GridRebindEventArgs"/> instance containing the event data.</param>
        protected void gAttendees_GridRebind( object sender, Rock.Web.UI.Controls.GridRebindEventArgs e )
        {
            BindAttendeesGrid( isCommunicating: e.IsCommunication, isExporting: e.IsExporting );
        }

        /// <summary>
        /// Handles the GetRecipientMergeFields event of the gAttendees control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="GetRecipientMergeFieldsEventArgs"/> instance containing the event data.</param>
        protected void gAttendees_GetRecipientMergeFields( object sender, GetRecipientMergeFieldsEventArgs e )
        {
            var groupMemberAssignment = e.DataItem as GroupMemberAssignment;

            if ( groupMemberAssignment == null )
            {
                return;
            }

            var entityTypeMergeField = MergeFieldPicker.EntityTypeInfo.GetMergeFieldId<GroupMemberAssignment>(
                new MergeFieldPicker.EntityTypeInfo.EntityTypeQualifier[]
                {
                    new MergeFieldPicker.EntityTypeInfo.EntityTypeQualifier( "GroupId", _groupId.ToString() ),
                    new MergeFieldPicker.EntityTypeInfo.EntityTypeQualifier( "GroupTypeId", _groupTypeCache.Id.ToString() ),
                    new MergeFieldPicker.EntityTypeInfo.EntityTypeQualifier( "LocationId", groupMemberAssignment.LocationId.ToString() ),
                    new MergeFieldPicker.EntityTypeInfo.EntityTypeQualifier( "ScheduleId", groupMemberAssignment.ScheduleId.ToString() )
                } );

            e.MergeValues.Add( entityTypeMergeField, groupMemberAssignment.Id );
        }

        /// <summary>
        /// Handles the AddClick event of the gAttendees control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        
        protected void gAttendees_AddClick( object sender, EventArgs e )
        {
            NavigateToGroupMemberDetailPage();
        }

        /// <summary>
        /// Handles the Click event of the DeleteOrArchiveGroupMember control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="Rock.Web.UI.Controls.RowEventArgs" /> instance containing the event data.</param>
        protected void DeleteOrArchiveGroupMember_Click( object sender, Rock.Web.UI.Controls.RowEventArgs e )
        {

        }

        #endregion

        #region Internal Members

        /// <summary>
        /// Gets the shared group.
        /// </summary>
        /// <param name="rockContext">The rock context.</param>
        /// <returns></returns>
        private Group GetSharedGroup( RockContext rockContext )
        {
            var key = $"Group:{_groupId}";
            var group = RockPage.GetSharedItem( key ) as Group;

            if ( group == null && _groupId > 0 )
            {
                group = new GroupService( rockContext )
                    .Queryable()
                    .AsNoTracking()
                    .Include( g => g.Campus )
                    .Include( g => g.GroupSyncs )
                    .Include( g => g.GroupType )
                    .Include( g => g.ParentGroup )
                    .FirstOrDefault( g => g.Id == _groupId );

                RockPage.SaveSharedItem( key, group );
            }

            _groupTypeCache = GroupTypeCache.Get( group.GroupTypeId );
            _groupMembersWithGroupMemberHistory = new HashSet<int>(
                new GroupMemberHistoricalService( rockContext )
                    .Queryable()
                    .Where( a => a.GroupId == _groupId )
                    .Select( a => a.GroupMemberId )
                    .ToList()
            );

            _groupTypeRoleIdsWithGroupSync = new HashSet<int>(
                group.GroupSyncs
                    .Select( a => a.GroupTypeRoleId )
                    .ToList()
            );

            return group;
        }

        /// <summary>
        /// Initializes the grid.
        /// </summary>
        /// <param name="group">The group.</param>
        private void InitializeGrid( Group group )
        {
            gfAttendees.UserPreferenceKeyPrefix = $"{_groupId}-{_locationId}-{_scheduleId}";

            gAttendees.PersonIdField = "PersonId";
            gAttendees.ExportFilename = group.Name;
            gAttendees.GetRecipientMergeFields += gAttendees_GetRecipientMergeFields;
            gAttendees.Actions.AddClick += gAttendees_AddClick;

            var canEdit = IsUserAuthorized( Authorization.EDIT ) && group.IsAuthorized( Authorization.EDIT, this.CurrentPerson );
            gAttendees.Actions.ShowAdd = canEdit;

            AddGridRowButtons();
            SetGridFilters( group );
        }

        /// <summary>
        /// Adds the grid row buttons.
        /// </summary>
        private void AddGridRowButtons()
        {
            var personProfileLinkField = new PersonProfileLinkField();
            personProfileLinkField.LinkedPageAttributeKey = AttributeKey.PersonProfilePage;
            gAttendees.Columns.Add( personProfileLinkField );

            _deleteField = new DeleteField();
            _deleteField.Click += DeleteOrArchiveGroupMember_Click;
            gAttendees.Columns.Add( _deleteField );
        }

        /// <summary>
        /// Resets the message boxes.
        /// </summary>
        private void ResetMessageBoxes()
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
            ResetMessageBoxes();

            if ( !EnsureRequiredIdsAreProvided() )
            {
                return;
            }

            using ( var rockContext = new RockContext() )
            {
                var group = GetSharedGroup( rockContext );
                if ( group == null )
                {
                    ShowGroupNotFoundMessage();
                    return;
                }

                if ( !EnsureGroupIsAllowed( group ) )
                {
                    return;
                }

                var opportunity = GetOpportunity( rockContext, group );
                if ( opportunity == null )
                {
                    ShowOpportunityNotFoundMessage();
                    return;
                }

                InitializeSummary( opportunity );

                BindAttendeesGrid( opportunity );
            }
        }

        /// <summary>
        /// Ensures the required ids are provided.
        /// </summary>
        /// <returns></returns>
        private bool EnsureRequiredIdsAreProvided()
        {
            var missingIds = new List<string>();
            if ( _groupId <= 0 )
            {
                missingIds.Add( "Group ID" );
            }

            if ( _locationId <= 0 )
            {
                missingIds.Add( "Location ID" );
            }

            if ( _scheduleId <= 0 )
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

            public List<GroupMemberAssignment> Attendees { get; set; }

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
                    return Attendees?.Count ?? 0;
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
        /// <param name="rockContext">The rock context.</param>
        /// <param name="group">The group.</param>
        /// <returns></returns>
        private Opportunity GetOpportunity( RockContext rockContext, Group group = null )
        {
            var groupLocation = new GroupLocationService( rockContext )
                .Queryable()
                .AsNoTracking()
                .Include( gl => gl.Location )
                .Include( gl => gl.Schedules )
                .Include( gl => gl.GroupLocationScheduleConfigs )
                .FirstOrDefault( gl => gl.GroupId == _groupId && gl.LocationId == _locationId );

            if ( groupLocation == null
                || groupLocation.Location == null
                || !groupLocation.Schedules.Any( s => s.Id == _scheduleId ) )
            {
                return null;
            }

            var schedule = groupLocation.Schedules.First( s => s.Id == _scheduleId );
            var config = groupLocation.GroupLocationScheduleConfigs.First( c => c.GroupLocationId == groupLocation.Id && c.ScheduleId == _scheduleId );

            var qry = new GroupMemberAssignmentService( rockContext )
                .Queryable()
                .AsNoTracking()
                .Include( gma => gma.GroupMember )
                .Include( gma => gma.GroupMember.GroupRole )
                .Include( gma => gma.GroupMember.Person )
                .Where( gma => gma.GroupMember.GroupId == _groupId
                    && gma.LocationId == _locationId
                    && gma.ScheduleId == _scheduleId );

            if ( _isCommunicating )
            {
                qry = qry.Where( gma => gma.GroupMember.GroupMemberStatus != GroupMemberStatus.Inactive );
            }

            // Filter by first name.
            var firstName = tbFirstName.Text;
            if ( !string.IsNullOrWhiteSpace( firstName ) )
            {
                qry = qry.Where( gma =>
                    gma.GroupMember.Person.FirstName.StartsWith( firstName ) ||
                    gma.GroupMember.Person.NickName.StartsWith( firstName ) );
            }

            // Filter by last name.
            var lastName = tbLastName.Text;
            if ( !string.IsNullOrWhiteSpace( lastName ) )
            {
                qry = qry.Where( gma => gma.GroupMember.Person.LastName.StartsWith( lastName ) );
            }

            // Filter by role.
            var validGroupTypeRoles = _groupTypeCache.Roles.Select( r => r.Id ).ToList();
            var roles = new List<int>();
            foreach ( var roleId in cblRole.SelectedValues.AsIntegerList() )
            {
                if ( validGroupTypeRoles.Contains( roleId ) )
                {
                    roles.Add( roleId );
                }
            }

            if ( roles.Any() )
            {
                qry = qry.Where( gma => roles.Contains( gma.GroupMember.GroupRoleId ) );
            }

            // Filter by GroupMemberStatus.
            var statuses = new List<GroupMemberStatus>();
            foreach ( var status in cblGroupMemberStatus.SelectedValues )
            {
                if ( !string.IsNullOrWhiteSpace( status ) )
                {
                    statuses.Add( status.ConvertToEnum<GroupMemberStatus>() );
                }
            }

            if ( statuses.Any() )
            {
                qry = qry.Where( gma => statuses.Contains( gma.GroupMember.GroupMemberStatus ) );
            }

            // Filter by Campus.
            if ( cpCampusFilter.SelectedCampusId.HasValue )
            {
                var campusId = cpCampusFilter.SelectedCampusId.Value;
                var qryCampusMembers = new GroupMemberService( rockContext )
                    .Queryable()
                    .AsNoTracking()
                    .Where( gm => gm.Group.GroupTypeId == FamilyGroupTypeId && gm.Group.CampusId == campusId );

                qry = qry.Where( gma => qryCampusMembers.Any( cm => cm.PersonId == gma.GroupMember.PersonId ) );
            }

            // Filter by gender.
            var genders = new List<Gender>();
            foreach ( var selectedGender in cblGenderFilter.SelectedValues )
            {
                var gender = selectedGender.ConvertToEnum<Gender>();
                genders.Add( gender );
            }

            if ( genders.Any() )
            {
                qry = qry.Where( gma => genders.Contains( gma.GroupMember.Person.Gender ) );
            }

            if ( _isExporting )
            {
                if ( !FamilyGroupTypeId.HasValue )
                {
                    _personIdPhoneNumberTypePhoneNumberLookup = new Dictionary<int, Dictionary<int, string>>();
                    _personIdHomeLocationLookup = new Dictionary<int, Location>();
                }
                else
                {
                    var personFamily = new GroupMemberService( rockContext )
                        .Queryable()
                        .AsNoTracking()
                        .Where( gm => qry.Any( gma => gma.GroupMember.PersonId == gm.PersonId ) )
                        .Where( gm => gm.Group.GroupTypeId == FamilyGroupTypeId )
                        .Select( gm => new
                        {
                            gm.PersonId,
                            gm.Group
                        } );

                    // Preload all phone numbers for members in the query.
                    _personIdPhoneNumberTypePhoneNumberLookup = new PhoneNumberService( rockContext )
                        .Queryable()
                        .AsNoTracking()
                        .Where( n => personFamily.Any( pf => pf.PersonId == n.PersonId ) && n.NumberTypeValueId.HasValue )
                        .GroupBy( n => new
                        {
                            n.PersonId,
                            n.NumberTypeValueId
                        } )
                        .Select( a => new
                        {
                            a.Key.PersonId,
                            a.Key.NumberTypeValueId,
                            NumberFormatted = a.Select( n => n.NumberFormatted ).FirstOrDefault()
                        } )
                        .GroupBy( a => a.PersonId )
                        .ToDictionary( k => k.Key, v => v.ToDictionary( xk => xk.NumberTypeValueId.Value, xv => xv.NumberFormatted ) );

                    if ( HomeAddressDefinedValue == null )
                    {
                        _personIdHomeLocationLookup = new Dictionary<int, Location>();
                    }
                    else
                    {
                        // Preload all mapped home locations for members in the query.
                        var locationsQry = personFamily
                            .Select( pf => new
                            {
                                HomeLocation = pf.Group.GroupLocations
                                    .Where( l => l.GroupLocationTypeValueId == HomeAddressDefinedValue.Id && l.IsMappedLocation )
                                    .Select( l => l.Location ).FirstOrDefault(),
                                GroupOrder = pf.Group.Order,
                                pf.PersonId
                            } );

                        _personIdHomeLocationLookup = locationsQry
                            .GroupBy( a => a.PersonId )
                            .ToDictionary( k => k.Key, v => v.OrderBy( a => a.GroupOrder )
                                .Select( x => x.HomeLocation ).FirstOrDefault() );
                    }
                }
            }

            if ( gAttendees.SortProperty != null )
            {
                qry = qry.Sort( gAttendees.SortProperty );
            }
            else
            {
                qry = qry
                    .OrderBy( gma => gma.GroupMember.Person.LastName )
                    .ThenBy( gma => gma.GroupMember.Person.AgeClassification )
                    .ThenBy( gma => gma.GroupMember.Person.Gender );
            }

            return new Opportunity
            {
                Group = group ?? GetSharedGroup( rockContext ),
                Location = groupLocation.Location,
                Schedule = schedule,
                Config = config,
                Attendees = qry.ToList()
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
            bSlotsFilled.Text = opportunity.SlotsFilled.ToString( "N0" );
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
        /// Resolves the CheckBox list values.
        /// </summary>
        /// <param name="values">The values.</param>
        /// <param name="checkBoxList">The check box list.</param>
        /// <returns></returns>
        private string ResolveCheckBoxListValues( string values, System.Web.UI.WebControls.CheckBoxList checkBoxList )
        {
            var resolvedValues = new List<string>();

            foreach ( var value in values.Split( ';' ) )
            {
                var item = checkBoxList.Items.FindByValue( value );
                if ( item != null )
                {
                    resolvedValues.Add( item.Text );
                }
            }

            return resolvedValues.AsDelimited( ", " );
        }

        /// <summary>
        /// Sets the grid filters.
        /// </summary>
        /// <param name="group">The group.</param>
        private void SetGridFilters( Group group )
        {
            tbFirstName.Text = gfAttendees.GetUserPreference( GridFilterKey.FirstName );
            tbLastName.Text = gfAttendees.GetUserPreference( GridFilterKey.LastName );

            if ( group != null )
            {
                cblRole.DataSource = group.GroupType.Roles.OrderBy( r => r.Order ).ToList();
                cblRole.DataBind();
            }

            var roleValue = gfAttendees.GetUserPreference( GridFilterKey.Role );
            if ( !string.IsNullOrWhiteSpace( roleValue ) )
            {
                cblRole.SetValues( roleValue.Split( ';' ).ToList() );
            }

            cblGroupMemberStatus.BindToEnum<GroupMemberStatus>();

            var statusValue = gfAttendees.GetUserPreference( GridFilterKey.Status );
            if ( !string.IsNullOrWhiteSpace( statusValue ) )
            {
                cblGroupMemberStatus.SetValues( statusValue.Split( ';' ).ToList() );
            }

            cpCampusFilter.Campuses = CampusCache.All();
            cpCampusFilter.SelectedCampusId = gfAttendees.GetUserPreference( "Campus" ).AsIntegerOrNull();

            string genderValue = gfAttendees.GetUserPreference( GridFilterKey.Gender );
            if ( !string.IsNullOrWhiteSpace( genderValue ) )
            {
                cblGenderFilter.SetValues( genderValue.Split( ';' ).ToList() );
            }
            else
            {
                cblGenderFilter.ClearSelection();
            }
        }

        /// <summary>
        /// Binds the attendees grid.
        /// </summary>
        /// <param name="opportunity">The opportunity.</param>
        /// <param name="isCommunicating">if set to <c>true</c> [is communicating].</param>
        /// <param name="isExporting">if set to <c>true</c> [is exporting].</param>
        private void BindAttendeesGrid( Opportunity opportunity = null, bool isCommunicating = false, bool isExporting = false )
        {
            _isCommunicating = isCommunicating;
            _isExporting = isExporting;

            if ( opportunity == null )
            {
                using ( var rockContext = new RockContext() )
                {
                    opportunity = GetOpportunity( rockContext );
                }
            }

            gAttendees.DataSource = opportunity.Attendees;
            gAttendees.DataBind();
        }

        /// <summary>
        /// Navigates to group member detail page.
        /// </summary>
        /// <param name="groupMemberId">The group member identifier.</param>
        private void NavigateToGroupMemberDetailPage( int? groupMemberId = null )
        {
            if ( !string.IsNullOrWhiteSpace( GetAttributeValue( AttributeKey.GroupMemberDetailPage ) ) )
            {
                var qryParams = new Dictionary<string, string>
                {
                    { PageParameterKey.GroupId, _groupId.ToString() },
                    { PageParameterKey.LocationId, _locationId.ToString() },
                    { PageParameterKey.ScheduleId, _scheduleId.ToString() },
                    { PageParameterKey.GroupMemberId, groupMemberId.GetValueOrDefault().ToString() },
                };

                NavigateToLinkedPage( AttributeKey.GroupMemberDetailPage, qryParams );
            }
        }

        #endregion
    }
}
