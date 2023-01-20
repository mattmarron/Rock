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
using System.Linq;
using Rock.Attribute;
using Rock.Data;
using Rock.Field.Types;
using Rock.Model;
using Rock.ViewModels.Blocks;
using Rock.ViewModels.Blocks.Engagement.SignUp.SignUpFinder;
using Rock.ViewModels.Cms;
using Rock.ViewModels.Rest.Controls;
using Rock.ViewModels.Utility;
using Rock.Web.Cache;
using Rock.Web.UI.Controls;

namespace Rock.Blocks.Engagement.SignUp
{
    /// <summary>
    /// Block used for finding a sign-up group/project.
    /// </summary>
    /// <seealso cref="Rock.Blocks.RockObsidianBlockType" />
    /// <seealso cref="Rock.Blocks.IHasCustomActions" />

    [DisplayName( "Sign-Up Finder" )]
    [Category( "Engagement > Sign-Up" )]
    [Description( "Block used for finding a sign-up group/project." )]
    [IconCssClass( "fa fa-clipboard-check" )]

    #region Block Attributes

    [GroupTypeField( "Project Types",
        Key = AttributeKey.ProjectTypes,
        Description = "Select the sign-up project group types that should be considered for the search.",
        Category = AttributeCategory.CustomSetting,
        DefaultValue = Rock.SystemGuid.GroupType.GROUPTYPE_SIGNUP_GROUP,
        IsRequired = true )]

    [TextField( "Project Type Filter Label",
        Key = AttributeKey.ProjectTypeFilterLabel,
        Description = "The label to use for the project type filter.",
        Category = AttributeCategory.CustomSetting,
        DefaultValue = "Project Type",
        IsRequired = true )]

    [BooleanField( "Hide Overcapacity Projects",
        Key = AttributeKey.HideOvercapacityProjects,
        Description = "Determines if projects that are full should be shown.",
        Category = AttributeCategory.CustomSetting,
        ControlType = BooleanFieldType.BooleanControlType.Checkbox,
        DefaultBooleanValue = false,
        IsRequired = false )]

    [BooleanField( "Display Campus Filter",
        Key = AttributeKey.DisplayCampusFilter,
        Description = "Determines if the campus filter should be shown.",
        Category = AttributeCategory.CustomSetting,
        ControlType = BooleanFieldType.BooleanControlType.Checkbox,
        DefaultBooleanValue = false,
        IsRequired = false  )]

    [DefinedValueField( "Campus Types",
        Key = AttributeKey.CampusTypes,
        Description = "The types of campuses to include in the campus list.",
        Category = AttributeCategory.CustomSetting,
        DefinedTypeGuid = Rock.SystemGuid.DefinedType.CAMPUS_TYPE,
        AllowMultiple = true,
        IsRequired = false )]

    [DefinedValueField( "Campus Statuses",
        Key = AttributeKey.CampusStatuses,
        Description = "The statuses of the campuses to include in the campus list.",
        Category = AttributeCategory.CustomSetting,
        DefinedTypeGuid = Rock.SystemGuid.DefinedType.CAMPUS_STATUS,
        AllowMultiple = true,
        IsRequired = false )]

    [BooleanField( "Enable Campus Context",
        Key = AttributeKey.EnableCampusContext,
        Description = "If the page has a campus context, its value will be used as a filter.",
        Category = AttributeCategory.CustomSetting,
        ControlType = BooleanFieldType.BooleanControlType.Checkbox,
        DefaultBooleanValue = false,
        IsRequired = false )]

    [CustomDropdownListField( "Display Project Filters As",
        Key = AttributeKey.DisplayProjectFiltersAs,
        Description = "Determines if the project filters should be show as checkboxes or multi-select dropdowns.",
        Category = AttributeCategory.CustomSetting,
        ListSource = "Checkboxes^Checkboxes,MultiSelectDropDown^Multi-Select Dropdown",
        DefaultValue = "Checkboxes",
        IsRequired = true )]

    [CustomDropdownListField( "Filter Columns",
        Key = AttributeKey.FilterColumns,
        Description = "The number of columns the filters should be displayed as.",
        Category = AttributeCategory.CustomSetting,
        ListSource = "1,2,3,4",
        DefaultValue = "1",
        IsRequired = true )]

    [AttributeField( "Display Attribute Filters",
        Key = AttributeKey.DisplayAttributeFilters,
        Description = "The group attributes that should be available for an individual to filter the results by.",
        Category = AttributeCategory.CustomSetting,
        EntityTypeGuid = Rock.SystemGuid.EntityType.GROUP,
        AllowMultiple = true,
        IsRequired = false )]

    [BooleanField( "Display Date Range",
        Key = AttributeKey.DisplayDateRange,
        Description = "When enabled, individuals would be able to filter the results by projects occurring inside the provided date range.",
        Category = AttributeCategory.CustomSetting,
        ControlType = BooleanFieldType.BooleanControlType.Checkbox,
        DefaultBooleanValue = false,
        IsRequired = false )]

    [BooleanField( "Display Location Range Filter",
        Key = AttributeKey.DisplayLocationRangeFilter,
        Description = "When enabled a filter will be shown to limit results to a specified number of miles from the location selected or their mailing address if logged in. If the Location Sort entry is not enabled to be shown and the individual is not logged in then this filter will not be shown, even if enabled, as we will not be able to honor the filter.",
        Category = AttributeCategory.CustomSetting,
        ControlType = BooleanFieldType.BooleanControlType.Checkbox,
        DefaultBooleanValue = false,
        IsRequired = false )]

    [BooleanField( "Display Slots Available Filter",
        Key = AttributeKey.DisplaySlotsAvailableFilter,
        Description = @"When enabled allows the individual to find projects with ""at least"" or ""no more than"" the provided spots available.",
        Category = AttributeCategory.CustomSetting,
        ControlType = BooleanFieldType.BooleanControlType.Checkbox,
        DefaultBooleanValue = false,
        IsRequired = false )]

    [TextField( "Results Lava Template",
        Key = AttributeKey.ResultsLavaTemplate,
        Description = "The Lava template to show with the results of the search.",
        Category = AttributeCategory.CustomSetting,
        DefaultValue = AttributeDefault.ResultsLavaTemplate,
        IsRequired = true )]

    [TextField( "Results Header Lava Template",
        Key = AttributeKey.ResultsHeaderLavaTemplate,
        Description = "The Lava Template to use to show the results header.",
        Category = AttributeCategory.CustomSetting,
        DefaultValue = AttributeDefault.ResultsHeaderLavaTemplate,
        IsRequired = false )]

    [BooleanField( "Load Results on Initial Page Load",
        Key = AttributeKey.LoadResultsOnInitialPageLoad,
        Description = "When enabled the group finder will load with all configured groups (no filters enabled).",
        Category = AttributeCategory.CustomSetting,
        ControlType = BooleanFieldType.BooleanControlType.Checkbox,
        DefaultBooleanValue = false,
        IsRequired = false )]

    [LinkedPage( "Project Detail Page",
        Key = AttributeKey.ProjectDetailPage,
        Description = "The page reference to pass to the Lava template for the details of the project.",
        Category = AttributeCategory.CustomSetting,
        IsRequired = true )]

    [LinkedPage( "Registration Page",
        Key = AttributeKey.RegistrationPage,
        Description = "The page reference to pass to the Lava template for the registration page.",
        Category = AttributeCategory.CustomSetting,
        IsRequired = true )]

    [BooleanField( "Display Location Sort",
        Key = AttributeKey.DisplayLocationSort,
        Description = "Determines if the location sort field should be shown.",
        Category = AttributeCategory.CustomSetting,
        ControlType = BooleanFieldType.BooleanControlType.Checkbox,
        DefaultBooleanValue = false,
        IsRequired = false )]

    [TextField( "Location Sort Label",
        Key = AttributeKey.LocationSortLabel,
        Description = "The label to use for the location sort filter.",
        Category = AttributeCategory.CustomSetting,
        DefaultValue = "Location (City, State or Zip Code)",
        IsRequired = true )]

    [BooleanField( "Display Named Schedule Filter",
        Key = AttributeKey.DisplayNamedScheduleFilter,
        Description = "When enabled a list of named schedules will be show as a filter.",
        Category = AttributeCategory.CustomSetting,
        ControlType = BooleanFieldType.BooleanControlType.Checkbox,
        DefaultBooleanValue = false,
        IsRequired = false )]

    [TextField( "Named Schedule Filter Label",
        Key = AttributeKey.NamedScheduleFilterLabel,
        Description = "The label to use for the named schedule filter.",
        Category = AttributeCategory.CustomSetting,
        DefaultValue = "Schedules",
        IsRequired = true )]

    [ScheduleField( "Root Named Schedule",
        Key = AttributeKey.RootNamedSchedule,
        Description = "When displaying the named schedule filter this will serve to filter which named schedules to show. Only direct descendants of this root schedule will be displayed.",
        Category = AttributeCategory.CustomSetting,
        IsRequired = false )]

    #endregion

    [Rock.SystemGuid.EntityTypeGuid( "BF09747C-786D-4979-BADF-2D0157F4CB21" )]
    [Rock.SystemGuid.BlockTypeGuid( "74A20402-00DF-4A87-98D1-B5A8920F1D32" )]
    public class SignUpFinder : RockObsidianBlockType, IHasCustomActions
    {
        #region Keys

        private static class AttributeKey
        {
            public const string ProjectTypes = "ProjectTypes";
            public const string ProjectTypeFilterLabel = "ProjectTypeFilterLabel";
            public const string HideOvercapacityProjects = "HideOvercapacityProjects";
            public const string DisplayCampusFilter = "DisplayCampusFilter";
            public const string CampusTypes = "CampusTypes";
            public const string CampusStatuses = "CampusStatuses";
            public const string EnableCampusContext = "EnableCampusContext";
            public const string DisplayProjectFiltersAs = "DisplayProjectFiltersAs";
            public const string FilterColumns = "FilterColumns";
            public const string DisplayAttributeFilters = "DisplayAttributeFilters";
            public const string DisplayDateRange = "DisplayDateRange";
            public const string DisplayLocationRangeFilter = "DisplayLocationRangeFilter";
            public const string DisplaySlotsAvailableFilter = "DisplaySlotsAvailableFilter";
            public const string ResultsLavaTemplate = "ResultsLavaTemplate";
            public const string ResultsHeaderLavaTemplate = "ResultsHeaderLavaTemplate";
            public const string LoadResultsOnInitialPageLoad = "LoadResultsOnInitialPageLoad";
            public const string ProjectDetailPage = "ProjectDetailPage";
            public const string RegistrationPage = "RegistrationPage";
            public const string DisplayLocationSort = "DisplayLocationSort";
            public const string LocationSortLabel = "LocationSortLabel";
            public const string DisplayNamedScheduleFilter = "DisplayNamedScheduleFilter";
            public const string NamedScheduleFilterLabel = "NamedScheduleFilterLabel";
            public const string RootNamedSchedule = "RootNamedSchedule";
        }

        private static class AttributeCategory
        {
            public const string CustomSetting = "CustomSetting";
        }

        private static class AttributeDefault
        {
            public const string ResultsLavaTemplate = "";
            public const string ResultsHeaderLavaTemplate = "";
        }

        private static class FilterDisplayType
        {
            public const string Checkboxes = "Checkboxes";
            public const string MultiSelectDropDown = "MultiSelectDropDown";
        }

        #endregion

        public override string BlockFileUrl => $"{base.BlockFileUrl}.obs";

        #region Methods

        /// <inheritdoc/>
        public override object GetObsidianBlockInitialization()
        {
            var box = new SignUpFinderInitializationBox();



            return box;
        }

        /// <summary>
        /// Gets the available sign-up project group type guids for an individual to filter the results by.
        /// </summary>
        /// <returns>A <see cref="List{Guid}"/>.</returns>
        private List<Guid> GetAvailableProjectTypeGuids()
        {
            var groupTypeGuid = Rock.SystemGuid.GroupType.GROUPTYPE_SIGNUP_GROUP.AsGuid();

            return GroupTypeCache
                .All()
                .Where( gt => gt.Guid.Equals( groupTypeGuid ) || gt.InheritedGroupType.Guid.Equals( groupTypeGuid ) )
                .Select( gt => gt.Guid )
                .ToList();
        }

        /// <summary>
        /// Gets the available group attributes for an individual to filter the results by.
        /// </summary>
        /// <param name="selectedGroupTypeGuids"></param>
        /// <returns>A <see cref="List{ListItemBag}"/>.</returns>
        private List<ListItemBag> GetAvailableDisplayAttributeFilters( IEnumerable<Guid> selectedGroupTypeGuids )
        {
            var filters = new List<ListItemBag>();

            foreach ( var groupTypeGuid in selectedGroupTypeGuids )
            {
                var groupTypeCache = GroupTypeCache.Get( groupTypeGuid );
                if ( groupTypeCache == null )
                {
                    continue;
                }

                var group = new Group { GroupTypeId = groupTypeCache.Id };
                group.LoadAttributes();

                foreach ( var attribute in group.Attributes )
                {
                    filters.Add( new ListItemBag
                    {
                        Value = attribute.Value.Guid.ToString(),
                        Text = $"{attribute.Value.Name} ({groupTypeCache.Name})"
                    } );
                }
            }

            return filters;
        }

        /// <inheritdoc/>
        protected override string RenewSecurityGrantToken()
        {
            using ( var rockContext = new RockContext() )
            {
                return GetSecurityGrantToken();
            }
        }

        /// <summary>
        /// Gets the security grant token that will be used by UI controls on
        /// this block to ensure they have the proper permissions.
        /// </summary>
        /// <returns>A string that represents the security grant token.</string>
        private string GetSecurityGrantToken()
        {
            return new Rock.Security.SecurityGrant()
                .ToToken();
        }

        #endregion

        #region IHasCustomActions

        /// <inheritdoc/>
        List<BlockCustomActionBag> IHasCustomActions.GetCustomActions( bool canEdit, bool canAdministrate )
        {
            var actions = new List<BlockCustomActionBag>();

            if ( BlockCache.IsAuthorized( Rock.Security.Authorization.ADMINISTRATE, RequestContext.CurrentPerson ) )
            {
                actions.Add( new BlockCustomActionBag
                {
                    IconCssClass = "fa fa-edit",
                    Tooltip = "Settings",
                    ComponentFileUrl = "/Obsidian/Blocks/Engagement/SignUp/signUpFinderCustomSettings.obs"
                } );
            }

            return actions;
        }

        #endregion

        #region Block Actions

        /// <summary>
        /// Gets the values and all other required details that will be needed to display the custom settings modal.
        /// </summary>
        /// <returns>A box that contains the custom settings values and additional data.</returns>
        [BlockAction]
        public BlockActionResult GetCustomSettings()
        {
            using ( var rockContext = new RockContext() )
            {
                if ( !BlockCache.IsAuthorized( Rock.Security.Authorization.ADMINISTRATE, RequestContext.CurrentPerson ) )
                {
                    return ActionForbidden( "Not authorized to edit block settings." );
                }

                // The available attribute filters are based on the currently-selected group type(s), so get these first.
                var projectTypes = GetAttributeValue( AttributeKey.ProjectTypes ).FromJsonOrNull<List<ListItemBag>>() ?? new List<ListItemBag>();

                var options = new SignUpFinderCustomSettingsOptionsBag
                {
                    AvailableProjectTypeGuids = GetAvailableProjectTypeGuids(),
                    AvailableDisplayAttributeFilters = GetAvailableDisplayAttributeFilters( projectTypes.Select( pt => pt.Value.AsGuid() ) )
                };

                // Ensure we don't try to preselect an attribute filter that was previously saved, but no longer relevant based on the currently-selected group type(s).
                var displayAttributeFilters = GetAttributeValue( AttributeKey.DisplayAttributeFilters )
                    .FromJsonOrNull<List<string>>() ?? new List<string>()
                    .Where( f => options.AvailableDisplayAttributeFilters.Any( af => af.Value == f ) )
                    .ToList();

                var settings = new SignUpFinderCustomSettingsBag
                {
                    ProjectTypes = projectTypes,
                    ProjectTypeFilterLabel = GetAttributeValue( AttributeKey.ProjectTypeFilterLabel ),
                    HideOvercapacityProjects = GetAttributeValue( AttributeKey.HideOvercapacityProjects ).AsBoolean(),
                    DisplayCampusFilter = GetAttributeValue( AttributeKey.DisplayCampusFilter ).AsBoolean(),
                    CampusTypes = GetAttributeValue( AttributeKey.CampusTypes ).FromJsonOrNull<List<ListItemBag>>() ?? new List<ListItemBag>(),
                    CampusStatuses = GetAttributeValue( AttributeKey.CampusStatuses ).FromJsonOrNull<List<ListItemBag>>() ?? new List<ListItemBag>(),
                    EnableCampusContext = GetAttributeValue( AttributeKey.EnableCampusContext ).AsBoolean(),
                    DisplayProjectFiltersAs = GetAttributeValue( AttributeKey.DisplayProjectFiltersAs ),
                    FilterColumns = GetAttributeValue( AttributeKey.FilterColumns ).ToIntSafe(),
                    DisplayAttributeFilters = displayAttributeFilters,
                    DisplayDateRange = GetAttributeValue( AttributeKey.DisplayDateRange ).AsBoolean(),
                    DisplayLocationRangeFilter = GetAttributeValue( AttributeKey.DisplayLocationRangeFilter ).AsBoolean(),
                    DisplaySlotsAvailableFilter = GetAttributeValue( AttributeKey.DisplaySlotsAvailableFilter ).AsBoolean(),
                    ResultsLavaTemplate = GetAttributeValue( AttributeKey.ResultsLavaTemplate ),
                    ResultsHeaderLavaTemplate = GetAttributeValue( AttributeKey.ResultsHeaderLavaTemplate ),
                    LoadResultsOnInitialPageLoad = GetAttributeValue( AttributeKey.LoadResultsOnInitialPageLoad ).AsBoolean(),
                    ProjectDetailPage = GetAttributeValue( AttributeKey.ProjectDetailPage ).FromJsonOrNull<PageRouteValueBag>(),
                    RegistrationPage = GetAttributeValue( AttributeKey.RegistrationPage ).FromJsonOrNull<PageRouteValueBag>(),
                    DisplayLocationSort = GetAttributeValue( AttributeKey.DisplayLocationSort ).AsBoolean(),
                    LocationSortLabel = GetAttributeValue( AttributeKey.LocationSortLabel ),
                    DisplayNamedScheduleFilter = GetAttributeValue( AttributeKey.DisplayNamedScheduleFilter ).AsBoolean(),
                    NamedScheduleFilterLabel = GetAttributeValue( AttributeKey.NamedScheduleFilterLabel ),
                    RootNamedSchedule = GetAttributeValue( AttributeKey.RootNamedSchedule ).FromJsonOrNull<ListItemBag>()
                };

                return ActionOk( new CustomSettingsBox<SignUpFinderCustomSettingsBag, SignUpFinderCustomSettingsOptionsBag>
                {
                    Settings = settings,
                    Options = options,
                    SecurityGrantToken = GetSecurityGrantToken()
                } );
            }
        }

        /// <summary>
        /// Saves the updates to the custom setting values for this block.
        /// </summary>
        /// <param name="box">The box that contains the setting values.</param>
        /// <returns>A response that indicates if the save was successful or not.</returns>
        public BlockActionResult SaveCustomSettings( CustomSettingsBox<SignUpFinderCustomSettingsBag, SignUpFinderCustomSettingsOptionsBag> box )
        {
            using ( var rockContext = new RockContext() )
            {
                if ( !BlockCache.IsAuthorized( Rock.Security.Authorization.ADMINISTRATE, RequestContext.CurrentPerson ) )
                {
                    return ActionForbidden( "Not authorized to edit block settings." );
                }

                var block = new BlockService( rockContext ).Get( BlockId );
                block.LoadAttributes( rockContext );

                box.IfValidProperty( nameof( box.Settings.ProjectTypes ),
                    () => block.SetAttributeValue( AttributeKey.ProjectTypes, box.Settings.ProjectTypes.ToJson() ) );

                box.IfValidProperty( nameof( box.Settings.ProjectTypeFilterLabel ),
                    () => block.SetAttributeValue( AttributeKey.ProjectTypeFilterLabel, box.Settings.ProjectTypeFilterLabel ) );

                box.IfValidProperty( nameof( box.Settings.HideOvercapacityProjects ),
                    () => block.SetAttributeValue( AttributeKey.HideOvercapacityProjects, box.Settings.HideOvercapacityProjects.ToString() ) );

                box.IfValidProperty( nameof( box.Settings.DisplayCampusFilter ),
                    () => block.SetAttributeValue( AttributeKey.DisplayCampusFilter, box.Settings.DisplayCampusFilter.ToString() ) );

                box.IfValidProperty( nameof( box.Settings.CampusTypes ),
                    () => block.SetAttributeValue( AttributeKey.CampusTypes, box.Settings.CampusTypes.ToJson() ) );

                box.IfValidProperty( nameof( box.Settings.CampusStatuses ),
                    () => block.SetAttributeValue( AttributeKey.CampusStatuses, box.Settings.CampusStatuses.ToJson() ) );

                box.IfValidProperty( nameof( box.Settings.EnableCampusContext ),
                    () => block.SetAttributeValue( AttributeKey.EnableCampusContext, box.Settings.EnableCampusContext.ToString() ) );

                box.IfValidProperty( nameof( box.Settings.DisplayProjectFiltersAs ),
                    () => block.SetAttributeValue( AttributeKey.DisplayProjectFiltersAs, box.Settings.DisplayProjectFiltersAs ) );

                box.IfValidProperty( nameof( box.Settings.FilterColumns ),
                    () => block.SetAttributeValue( AttributeKey.FilterColumns, box.Settings.FilterColumns ) );

                box.IfValidProperty( nameof( box.Settings.DisplayAttributeFilters ),
                    () => block.SetAttributeValue( AttributeKey.DisplayAttributeFilters, box.Settings.DisplayAttributeFilters.ToJson() ) );

                box.IfValidProperty( nameof( box.Settings.DisplayDateRange ),
                    () => block.SetAttributeValue( AttributeKey.DisplayDateRange, box.Settings.DisplayDateRange.ToString() ) );

                box.IfValidProperty( nameof( box.Settings.DisplayLocationRangeFilter ),
                    () => block.SetAttributeValue( AttributeKey.DisplayLocationRangeFilter, box.Settings.DisplayLocationRangeFilter.ToString() ) );

                box.IfValidProperty( nameof( box.Settings.DisplaySlotsAvailableFilter ),
                    () => block.SetAttributeValue( AttributeKey.DisplaySlotsAvailableFilter, box.Settings.DisplaySlotsAvailableFilter.ToString() ) );

                box.IfValidProperty( nameof( box.Settings.ResultsLavaTemplate ),
                    () => block.SetAttributeValue( AttributeKey.ResultsLavaTemplate, box.Settings.ResultsLavaTemplate ) );

                box.IfValidProperty( nameof( box.Settings.ResultsHeaderLavaTemplate ),
                    () => block.SetAttributeValue( AttributeKey.ResultsHeaderLavaTemplate, box.Settings.ResultsHeaderLavaTemplate ) );

                box.IfValidProperty( nameof( box.Settings.LoadResultsOnInitialPageLoad ),
                    () => block.SetAttributeValue( AttributeKey.LoadResultsOnInitialPageLoad, box.Settings.LoadResultsOnInitialPageLoad.ToString() ) );

                box.IfValidProperty( nameof( box.Settings.ProjectDetailPage ),
                    () => block.SetAttributeValue( AttributeKey.ProjectDetailPage, box.Settings.ProjectDetailPage.ToJson() ) );

                box.IfValidProperty( nameof( box.Settings.RegistrationPage ),
                    () => block.SetAttributeValue( AttributeKey.RegistrationPage, box.Settings.RegistrationPage.ToJson() ) );

                box.IfValidProperty( nameof( box.Settings.DisplayLocationSort ),
                    () => block.SetAttributeValue( AttributeKey.DisplayLocationSort, box.Settings.DisplayLocationSort.ToString() ) );

                box.IfValidProperty( nameof( box.Settings.LocationSortLabel ),
                    () => block.SetAttributeValue( AttributeKey.LocationSortLabel, box.Settings.LocationSortLabel ) );

                box.IfValidProperty( nameof( box.Settings.DisplayNamedScheduleFilter ),
                    () => block.SetAttributeValue( AttributeKey.DisplayNamedScheduleFilter, box.Settings.DisplayNamedScheduleFilter.ToString() ) );

                box.IfValidProperty( nameof( box.Settings.NamedScheduleFilterLabel ),
                    () => block.SetAttributeValue( AttributeKey.NamedScheduleFilterLabel, box.Settings.NamedScheduleFilterLabel ) );

                box.IfValidProperty( nameof( box.Settings.RootNamedSchedule ),
                    () => block.SetAttributeValue( AttributeKey.RootNamedSchedule, box.Settings.RootNamedSchedule.ToJson() ) );

                return ActionOk();
            }
        }

        #endregion
    }
}
