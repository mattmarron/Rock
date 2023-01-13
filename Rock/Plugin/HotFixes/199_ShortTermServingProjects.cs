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

namespace Rock.Plugin.HotFixes
{
    /// <summary>
    /// Plug-in migration
    /// </summary>
    /// <seealso cref="Rock.Plugin.Migration" />
    [MigrationNumber( 199, "1.15.0" )]
    public class ShortTermServingProjects : Migration
    {
        /// <summary>
        /// Operations to be performed during the upgrade process.
        /// </summary>
        public override void Up()
        {
            ShortTermServingProjects_AddDefinedTypesAndValues();
            ShortTermServingProjects_AddGroups();
            ShortTermServingProjects_AddSystemCommunications();
            ShortTermServingProjects_AddAdminBlockTypes();
            ShortTermServingProjects_AddAdminPagesAndBlocks();
            ShortTermServingProjects_AddPublicBlockTypes();
            ShortTermServingProjects_AddPublicPagesAndBlocks();
        }

        /// <summary>
        /// Operations to be performed during the downgrade process.
        /// </summary>
        public override void Down()
        {
            ShortTermServingProjects_DeletePublicPagesAndBlocks();
            ShortTermServingProjects_DeletePublicBlockTypes();
            ShortTermServingProjects_DeleteAdminPagesAndBlocks();
            ShortTermServingProjects_DeleteAdminBlockTypes();
            ShortTermServingProjects_DeleteSystemCommunications();
            ShortTermServingProjects_DeleteGroups();
            ShortTermServingProjects_DeleteDefinedTypesAndValues();
        }

        /// <summary>
        /// JPH: Add defined types and values needed for Sign-Up Groups.
        /// </summary>
        private void ShortTermServingProjects_AddDefinedTypesAndValues()
        {
            RockMigrationHelper.AddDefinedType( "Group", "Project Type", "List of different types (In-Person, Project Due, etc.) of projects.", SystemGuid.DefinedType.PROJECT_TYPE, string.Empty );

            RockMigrationHelper.UpdateDefinedValue( SystemGuid.DefinedType.PROJECT_TYPE, "In-Person", "The project happens on the configured date/time.", SystemGuid.DefinedValue.PROJECT_TYPE_IN_PERSON );
            RockMigrationHelper.UpdateDefinedValue( SystemGuid.DefinedType.PROJECT_TYPE, "Project Due", "The project is due on the configured date/time.", SystemGuid.DefinedValue.PROJECT_TYPE_PROJECT_DUE );
        }

        /// <summary>
        /// JPH: Add group related records needed for Sign-Up Groups.
        /// </summary>
        private void ShortTermServingProjects_AddGroups()
        {
            RockMigrationHelper.AddGroupType( "Sign-Up Group", "Used to track individuals who have signed up for events such as short term serving projects.", "Group", "Member", false, true, true, "fa fa-clipboard-check", 0, null, 3, null, SystemGuid.GroupType.GROUPTYPE_SIGNUP_GROUP );

            Sql( $"UPDATE [GroupType] SET [AllowedScheduleTypes] = 6 WHERE [Guid] = '{SystemGuid.GroupType.GROUPTYPE_SIGNUP_GROUP}'" );
            Sql( $"UPDATE [GroupType] SET [EnableGroupHistory] = 1 WHERE [Guid] = '{SystemGuid.GroupType.GROUPTYPE_SIGNUP_GROUP}'" );

            RockMigrationHelper.AddGroupTypeRole( SystemGuid.GroupType.GROUPTYPE_SIGNUP_GROUP, "Leader", "Indicates the person is a leader in the group.", 0, null, null, SystemGuid.GroupRole.GROUPROLE_SIGNUP_GROUP_LEADER, true, true, false );
            RockMigrationHelper.AddGroupTypeRole( SystemGuid.GroupType.GROUPTYPE_SIGNUP_GROUP, "Member", "Indicates the person is a member in the group.", 1, null, null, SystemGuid.GroupRole.GROUPROLE_SIGNUP_GROUP_MEMBER, true, false, true );

            RockMigrationHelper.AddGroupTypeAssociation( SystemGuid.GroupType.GROUPTYPE_SIGNUP_GROUP, SystemGuid.GroupType.GROUPTYPE_SIGNUP_GROUP );

            RockMigrationHelper.AddGroupTypeGroupAttribute( SystemGuid.GroupType.GROUPTYPE_SIGNUP_GROUP, SystemGuid.FieldType.DEFINED_VALUE, "Project Type", "The specified project type.", 0, string.Empty, SystemGuid.Attribute.GROUPTYPE_SIGNUP_GROUP_PROJECT_TYPE, true );

            RockMigrationHelper.AddDefinedTypeAttributeQualifier( SystemGuid.Attribute.GROUPTYPE_SIGNUP_GROUP_PROJECT_TYPE, SystemGuid.DefinedType.PROJECT_TYPE, "D49200BC-9E54-4906-9B20-53FD8973A43D" );

            RockMigrationHelper.UpdateGroup( null, SystemGuid.GroupType.GROUPTYPE_SIGNUP_GROUP, "Sign-Up Groups", "Parent group for all sign-up groups.", null, 0, SystemGuid.Group.GROUP_SIGNUP_GROUPS );
        }

        /// <summary>
        /// JPH: Add system communications needed for Sign-Up Groups.
        /// </summary>
        private void ShortTermServingProjects_AddSystemCommunications()
        {
            RockMigrationHelper.UpdateCategory( SystemGuid.EntityType.SYSTEM_COMMUNICATION, "Sign-Up Group Confirmation", "fa fa-clipboard-check", string.Empty, SystemGuid.Category.SYSTEM_COMMUNICATION_SIGNUP_GROUP_CONFIRMATION );

            RockMigrationHelper.UpdateSystemCommunication(
                "Sign-Up Group Confirmation", // category
                "Sign-Up Group Reminder", // title
                string.Empty, // from
                string.Empty, // fromName
                string.Empty, // to
                string.Empty, // cc
                string.Empty, // bcc
                "Sign-Up Group Reminder for {{ OccurrenceTitle }} on {{ Occurrence.OccurrenceDate | Date:'dddd, MMMM d, yyyy' }}", // subject
                "  {{ 'Global' | Attribute:'EmailHeader' }}    <h1>Sign-Up Group Reminder</h1>    <p>Hi {{  Person.NickName  }}!</p>    <p>This is a reminder that you have signed up for {{ OccurrenceTitle }} on {{ Occurrence.OccurrenceDate | Date:'dddd, MMMM d, yyyy' }}.</p>    <p>Thanks!</p>    <p>{{ 'Global' | Attribute:'OrganizationName' }}</p>    {{ 'Global' | Attribute:'EmailFooter' }}  ", // body
                SystemGuid.SystemCommunication.SIGNUP_GROUP_REMINDER, // guid
                true, // isActive
                "This is a reminder from {{ 'Global' | Attribute:'OrganizationName' }} that you have signed up for {{ OccurrenceTitle }} on {{ Occurrence.OccurrenceDate | Date:'dddd, MMMM d, yyyy' }}." // smsMessage
            );
        }

        /// <summary>
        /// JPH: Add admin block types needed for Sign-Up Groups.
        /// </summary>
        private void ShortTermServingProjects_AddAdminBlockTypes()
        {
            RockMigrationHelper.UpdateBlockType( "Sign-Up Overview", "Displays an overview of sign-up projects with upcoming and recently-occurred opportunities.", "~/Blocks/Engagement/SignUp/SignUpOverview.ascx", "Engagement > Sign-Up", "B539F3B5-01D3-4325-B32A-85AFE2A9D18B" );
            RockMigrationHelper.AddOrUpdateBlockTypeAttribute( "B539F3B5-01D3-4325-B32A-85AFE2A9D18B", "BD53F9C9-EBA9-4D3F-82EA-DE5DD34A8108", "Project Detail Page", "ProjectDetailPage", "Project Detail Page", "Page used for viewing details about the scheduled opportunities for a given project group. Clicking a row in the grid will take you to this page.", 0, "", "E306CB42-10FE-428C-A8B9-224BB7B30C6A" );
            Sql( "UPDATE [Attribute] SET [IsRequired] = 1 WHERE [Guid] = 'E306CB42-10FE-428C-A8B9-224BB7B30C6A';" );
            RockMigrationHelper.AddOrUpdateBlockTypeAttribute( "B539F3B5-01D3-4325-B32A-85AFE2A9D18B", "BD53F9C9-EBA9-4D3F-82EA-DE5DD34A8108", "Sign-Up Opportunity Attendee List Page", "SignUpOpportunityAttendeeListPage", "Sign-Up Opportunity Attendee List Page", "Page used for viewing all the group members for the selected sign-up opportunity. If set, a view attendees button will show for each opportunity.", 1, "", "B86F9026-5B63-414C-A069-B5C86B8FEFC2" );

            RockMigrationHelper.UpdateBlockType( "Sign-Up Detail", "Displays details about the scheduled opportunities for a given project group.", "~/Blocks/Engagement/SignUp/SignUpDetail.ascx", "Engagement > Sign-Up", "69F5C6BD-7A22-42FE-8285-7C8E586E746A" );
            RockMigrationHelper.AddOrUpdateBlockTypeAttribute( "69F5C6BD-7A22-42FE-8285-7C8E586E746A", "BD53F9C9-EBA9-4D3F-82EA-DE5DD34A8108", "Sign-Up Opportunity Attendee List Page", "SignUpOpportunityAttendeeListPage", "Sign-Up Opportunity Attendee List Page", "Page used for viewing all the group members for the selected sign-up opportunity. If set, a view attendees button will show for each opportunity.", 0, "", "525A1D90-CF46-4710-ADC3-86552EBB1E9C" );

            RockMigrationHelper.UpdateBlockType( "Sign-Up Opportunity Attendee List", "Lists all the group members for the selected group, location and schedule.", "~/Blocks/Engagement/SignUp/SignUpOpportunityAttendeeList.ascx", "Engagement > Sign-Up", "EE652767-5070-4EAB-8BB7-BB254DD01B46" );
            RockMigrationHelper.AddOrUpdateBlockTypeAttribute( "EE652767-5070-4EAB-8BB7-BB254DD01B46", "BD53F9C9-EBA9-4D3F-82EA-DE5DD34A8108", "Group Member Detail Page", "GroupMemberDetailPage", "Group Member Detail Page", "Page used for viewing an attendee's group member detail for this Sign-Up project. Clicking a row in the grid will take you to this page.", 0, "", "E0908F51-8B7E-4D94-8972-7DDCDB3D37A6" );
            Sql( "UPDATE [Attribute] SET [IsRequired] = 1 WHERE [Guid] = 'E0908F51-8B7E-4D94-8972-7DDCDB3D37A6';" );
            RockMigrationHelper.AddOrUpdateBlockTypeAttribute( "EE652767-5070-4EAB-8BB7-BB254DD01B46", "BD53F9C9-EBA9-4D3F-82EA-DE5DD34A8108", "Person Profile Page", "PersonProfilePage", "Person Profile Page", "Page used for viewing a person's profile. If set, a view profile button will show for each group member.", 1, "", "E1FB0EC5-F0C8-4BBE-BEB1-B2C5D7B1A1C0" );

            RockMigrationHelper.AddOrUpdateBlockTypeAttribute( "2D26A2C4-62DC-4680-8219-A52EB2BC0F65", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Disable Auto-Select First Group", "DisableAutoSelectFirstGroup", "Disable Auto-Select First Group", "Whether to disable the default behavior of auto-selecting the first group (ordered by name) in the tree view.", 10, "False", "AD145399-2D61-40B4-802A-400766574692" );
        }

        /// <summary>
        /// JPH: Add admin pages and blocks needed for Sign-Up Groups.
        /// </summary>
        private void ShortTermServingProjects_AddAdminPagesAndBlocks()
        {
            // [Page]: Engagement > Sign-Up
            RockMigrationHelper.AddPage( true, "48242949-944A-4651-B6CC-60194EDE08A0", "0CB60906-6B74-44FD-AB25-026050EF70EB", "Sign-Up", "", "1941542C-21F2-4341-BDE1-996AA1E0C0A2", "", "2A0C135A-8421-4125-A484-83C8B4FB3D34" );
            RockMigrationHelper.AddPageRoute( "1941542C-21F2-4341-BDE1-996AA1E0C0A2", "people/sign-up", "75332FE3-DB1B-4B83-9287-5EDDD09A1A4E" );
            // [Block]: Sign-Up Overview for [Page]: Engagement > Sign-Up
            RockMigrationHelper.AddBlock( true, "1941542C-21F2-4341-BDE1-996AA1E0C0A2".AsGuidOrNull(), null, null, "B539F3B5-01D3-4325-B32A-85AFE2A9D18B".AsGuidOrNull(), "Sign-Up Overview", "Main", "", "", 0, "4ECCC106-5374-4E33-A8AA-3ADE977FB1A4" );
            // [Attribute Value]: Project Detail Page for [Block]: Sign-Up Overview for [Page]: Engagement > Sign-Up
            RockMigrationHelper.AddBlockAttributeValue( "4ECCC106-5374-4E33-A8AA-3ADE977FB1A4", "E306CB42-10FE-428C-A8B9-224BB7B30C6A", "34212f8e-5f14-4d92-8b19-46748eba2727,d6aefd09-630e-40d7-aa56-77cc904c6595" );
            // [Attribute Value]: Sign-Up Opportunity Attendee List Page for [Block]: Sign-Up Overview for [Page]: Engagement > Sign-Up
            RockMigrationHelper.AddBlockAttributeValue( "4ECCC106-5374-4E33-A8AA-3ADE977FB1A4", "B86F9026-5B63-414C-A069-B5C86B8FEFC2", "aaf11844-ec6c-498b-a9d8-387390206570,db9c7e0d-5ec7-4cc2-ba9e-dd398d4b9714" );
            // [Block]: Sign-Up Groups (Group Tree View) for [Page]: Engagement > Sign-Up
            RockMigrationHelper.AddBlock( true, "1941542C-21F2-4341-BDE1-996AA1E0C0A2".AsGuidOrNull(), null, null, "2D26A2C4-62DC-4680-8219-A52EB2BC0F65".AsGuidOrNull(), "Sign-Up Groups", "Sidebar1", "", "", 0, "B9D4522A-38D7-4F5B-B9CD-E5497B258471" );
            // [Attribute Value]: Detail Page for [Block]: Sign-Up Groups (Group Tree View) for [Page]: Engagement > Sign-Up
            RockMigrationHelper.AddBlockAttributeValue( "B9D4522A-38D7-4F5B-B9CD-E5497B258471", "ADCC4391-8D8B-4A28-80AF-24CD6D3F77E2", "34212f8e-5f14-4d92-8b19-46748eba2727,d6aefd09-630e-40d7-aa56-77cc904c6595" );
            // [Attribute Value]: Disable Auto-Select First Group for [Block]: Sign-Up Groups (Group Tree View) for [Page]: Engagement > Sign-Up
            RockMigrationHelper.AddBlockAttributeValue( "B9D4522A-38D7-4F5B-B9CD-E5497B258471", "AD145399-2D61-40B4-802A-400766574692", "True" );
            // [Attribute Value]: Display Inactive Campuses for [Block]: Sign-Up Groups (Group Tree View) for [Page]: Engagement > Sign-Up
            RockMigrationHelper.AddBlockAttributeValue( "B9D4522A-38D7-4F5B-B9CD-E5497B258471", "22D5915F-D449-4E03-A8AD-0C473A3D4864", "True" );
            // [Attribute Value]: Initial Active Setting for [Block]: Sign-Up Groups (Group Tree View) for [Page]: Engagement > Sign-Up
            RockMigrationHelper.AddBlockAttributeValue( "B9D4522A-38D7-4F5B-B9CD-E5497B258471", "2AD968BA-6721-4B69-A4FE-B57D8FB0ECFB", "1" );
            // [Attribute Value]: Initial Count Setting for [Block]: Sign-Up Groups (Group Tree View) for [Page]: Engagement > Sign-Up
            RockMigrationHelper.AddBlockAttributeValue( "B9D4522A-38D7-4F5B-B9CD-E5497B258471", "36D18581-3874-4C5A-A01B-793A458F9F91", "0" );
            // [Attribute Value]: Limit to Security Role Groups for [Block]: Sign-Up Groups (Group Tree View) for [Page]: Engagement > Sign-Up
            RockMigrationHelper.AddBlockAttributeValue( "B9D4522A-38D7-4F5B-B9CD-E5497B258471", "1688837B-73CF-46C3-8880-74C46605807C", "False" );
            // [Attribute Value]: Root Group for [Block]: Sign-Up Groups (Group Tree View) for [Page]: Engagement > Sign-Up
            RockMigrationHelper.AddBlockAttributeValue( "B9D4522A-38D7-4F5B-B9CD-E5497B258471", "0E1768CD-87CC-4361-8BCD-01981FBFE24B", "d649638a-ef91-42d8-9b38-32172d614a5f" );
            // [Attribute Value]: Show Settings Panel for [Block]: Sign-Up Groups (Group Tree View) for [Page]: Engagement > Sign-Up
            RockMigrationHelper.AddBlockAttributeValue( "B9D4522A-38D7-4F5B-B9CD-E5497B258471", "4633ED5A-7A2C-4A78-B092-6733FED8CFA6", "True" );
            // [Attribute Value]: Treeview Title for [Block]: Sign-Up Groups (Group Tree View) for [Page]: Engagement > Sign-Up
            RockMigrationHelper.AddBlockAttributeValue( "B9D4522A-38D7-4F5B-B9CD-E5497B258471", "D1583306-2504-48D2-98EE-3DE55C2806C7", "Sign-Up Groups" );

            // [Page]: Sign-Up > Sign-Up Detail
            RockMigrationHelper.AddPage( true, "1941542C-21F2-4341-BDE1-996AA1E0C0A2", "0CB60906-6B74-44FD-AB25-026050EF70EB", "Sign-Up Detail", "", "34212F8E-5F14-4D92-8B19-46748EBA2727", "", null );
            RockMigrationHelper.AddPageRoute( "34212F8E-5F14-4D92-8B19-46748EBA2727", "people/sign-up/{GroupId}", "D6AEFD09-630E-40D7-AA56-77CC904C6595" );
            // [Block]: Sign-Up Detail for [Page]: Sign-Up > Sign-Up Detail
            RockMigrationHelper.AddBlock( true, "34212F8E-5F14-4D92-8B19-46748EBA2727".AsGuidOrNull(), null, null, "69F5C6BD-7A22-42FE-8285-7C8E586E746A".AsGuidOrNull(), "Sign-Up Detail", "Main", "", "", 0, "735C2380-5E10-4EDF-91ED-4EDF9BD5C507" );
            // [Attribute Value]: Sign-Up Opportunity Attendee List Page for [Block]: Sign-Up Detail for [Page]: Sign-Up > Sign-Up Detail
            RockMigrationHelper.AddBlockAttributeValue( "735C2380-5E10-4EDF-91ED-4EDF9BD5C507", "525A1D90-CF46-4710-ADC3-86552EBB1E9C", "aaf11844-ec6c-498b-a9d8-387390206570,db9c7e0d-5ec7-4cc2-ba9e-dd398d4b9714" );
            // [Block]: Sign-Up Groups (Group Tree View) for [Page]: Sign-Up > Sign-Up Detail
            RockMigrationHelper.AddBlock( true, "34212F8E-5F14-4D92-8B19-46748EBA2727".AsGuidOrNull(), null, null, "2D26A2C4-62DC-4680-8219-A52EB2BC0F65".AsGuidOrNull(), "Sign-Up Groups", "Sidebar1", "", "", 0, "D493C133-08EE-4E78-A85D-FF8E4FF80158" );
            // [Attribute Value]: Detail Page for [Block]: Sign-Up Groups (Group Tree View) for [Page]: Sign-Up > Sign-Up Detail
            RockMigrationHelper.AddBlockAttributeValue( "D493C133-08EE-4E78-A85D-FF8E4FF80158", "ADCC4391-8D8B-4A28-80AF-24CD6D3F77E2", "34212f8e-5f14-4d92-8b19-46748eba2727,d6aefd09-630e-40d7-aa56-77cc904c6595" );
            // [Attribute Value]: Disable Auto-Select First Group for [Block]: Sign-Up Groups (Group Tree View) for [Page]: Sign-Up > Sign-Up Detail
            RockMigrationHelper.AddBlockAttributeValue( "D493C133-08EE-4E78-A85D-FF8E4FF80158", "AD145399-2D61-40B4-802A-400766574692", "False" );
            // [Attribute Value]: Display Inactive Campuses for [Block]: Sign-Up Groups (Group Tree View) for [Page]: Sign-Up > Sign-Up Detail
            RockMigrationHelper.AddBlockAttributeValue( "D493C133-08EE-4E78-A85D-FF8E4FF80158", "22D5915F-D449-4E03-A8AD-0C473A3D4864", "True" );
            // [Attribute Value]: Initial Active Setting for [Block]: Sign-Up Groups (Group Tree View) for [Page]: Sign-Up > Sign-Up Detail
            RockMigrationHelper.AddBlockAttributeValue( "D493C133-08EE-4E78-A85D-FF8E4FF80158", "2AD968BA-6721-4B69-A4FE-B57D8FB0ECFB", "1" );
            // [Attribute Value]: Initial Count Setting for [Block]: Sign-Up Groups (Group Tree View) for [Page]: Sign-Up > Sign-Up Detail
            RockMigrationHelper.AddBlockAttributeValue( "D493C133-08EE-4E78-A85D-FF8E4FF80158", "36D18581-3874-4C5A-A01B-793A458F9F91", "0" );
            // [Attribute Value]: Limit to Security Role Groups for [Block]: Sign-Up Groups (Group Tree View) for [Page]: Sign-Up > Sign-Up Detail
            RockMigrationHelper.AddBlockAttributeValue( "D493C133-08EE-4E78-A85D-FF8E4FF80158", "1688837B-73CF-46C3-8880-74C46605807C", "False" );
            // [Attribute Value]: Root Group for [Block]: Sign-Up Groups (Group Tree View) for [Page]: Sign-Up > Sign-Up Detail
            RockMigrationHelper.AddBlockAttributeValue( "D493C133-08EE-4E78-A85D-FF8E4FF80158", "0E1768CD-87CC-4361-8BCD-01981FBFE24B", "d649638a-ef91-42d8-9b38-32172d614a5f" );
            // [Attribute Value]: Show Settings Panel for [Block]: Sign-Up Groups (Group Tree View) for [Page]: Sign-Up > Sign-Up Detail
            RockMigrationHelper.AddBlockAttributeValue( "D493C133-08EE-4E78-A85D-FF8E4FF80158", "4633ED5A-7A2C-4A78-B092-6733FED8CFA6", "True" );
            // [Attribute Value]: Treeview Title for [Block]: Sign-Up Groups (Group Tree View) for [Page]: Sign-Up > Sign-Up Detail
            RockMigrationHelper.AddBlockAttributeValue( "D493C133-08EE-4E78-A85D-FF8E4FF80158", "D1583306-2504-48D2-98EE-3DE55C2806C7", "Sign-Up Groups" );

            // [Page]: Sign-Up Detail > Sign-Up Opportunity Attendee List
            RockMigrationHelper.AddPage( true, "34212F8E-5F14-4D92-8B19-46748EBA2727", "D65F783D-87A9-4CC9-8110-E83466A0EADB", "Sign-Up Opportunity Attendee List", "", "AAF11844-EC6C-498B-A9D8-387390206570", "", null );
            RockMigrationHelper.AddPageRoute( "AAF11844-EC6C-498B-A9D8-387390206570", "people/sign-up/{GroupId}/location/{LocationId}/schedule/{ScheduleId}", "DB9C7E0D-5EC7-4CC2-BA9E-DD398D4B9714" );
            // [Block]: Sign-Up Opportunity Attendee List for [Page]: Sign-Up Detail > Sign-Up Opportunity Attendee List
            RockMigrationHelper.AddBlock( true, "AAF11844-EC6C-498B-A9D8-387390206570".AsGuidOrNull(), null, null, "EE652767-5070-4EAB-8BB7-BB254DD01B46".AsGuidOrNull(), "Sign-Up Opportunity Attendee List", "Main", "", "", 0, "54FC3FA7-2D25-4694-8DD4-647222582CEB" );
            // [Attribute Value]: Group Member Detail Page for [Block]: Sign-Up Opportunity Attendee List for [Page]: Sign-Up Detail > Sign-Up Opportunity Attendee List
            RockMigrationHelper.AddBlockAttributeValue( "54FC3FA7-2D25-4694-8DD4-647222582CEB", "E0908F51-8B7E-4D94-8972-7DDCDB3D37A6", "05b79031-183f-4a64-a689-56b5c8e7519f,40566dcd-ac73-4c61-95b3-8f9b2e06528c" );
            // [Attribute Value]: Person Profile Page for [Block]: Sign-Up Opportunity Attendee List for [Page]: Sign-Up Detail > Sign-Up Opportunity Attendee List
            RockMigrationHelper.AddBlockAttributeValue( "54FC3FA7-2D25-4694-8DD4-647222582CEB", "E1FB0EC5-F0C8-4BBE-BEB1-B2C5D7B1A1C0", "08dbd8a5-2c35-4146-b4a8-0f7652348b25,7e97823a-78a8-4e8e-a337-7a20f2da9e52" );

            // [Page]: Sign-Up Opportunity Attendee List > Group Member Detail
            RockMigrationHelper.AddPage( true, "AAF11844-EC6C-498B-A9D8-387390206570", "D65F783D-87A9-4CC9-8110-E83466A0EADB", "Group Member Detail", "", "05B79031-183F-4A64-A689-56B5C8E7519F", "", null );
            RockMigrationHelper.AddPageRoute( "05B79031-183F-4A64-A689-56B5C8E7519F", "people/sign-up/{GroupId}/location/{LocationId}/schedule/{ScheduleId}/member/{GroupMemberId}", "40566DCD-AC73-4C61-95B3-8F9B2E06528C" );
            // [Block]: Group Member Detail for [Page]: Sign-Up Opportunity Attendee List > Group Member Detail
            RockMigrationHelper.AddBlock( true, "05B79031-183F-4A64-A689-56B5C8E7519F".AsGuidOrNull(), null, null, "AAE2E5C3-9279-4AB0-9682-F4D19519D678".AsGuidOrNull(), "Group Member Detail", "Main", "", "", 0, "C4D268FC-17B8-4E55-B3A2-7C55F79015BD" );
            // [Attribute Value]: Allow Selecting 'From' for [Block]: Group Member Detail for [Page]: Sign-Up Opportunity Attendee List > Group Member Detail
            RockMigrationHelper.AddBlockAttributeValue( "C4D268FC-17B8-4E55-B3A2-7C55F79015BD", "65FCFD8F-0BD9-4285-AC2B-6CCB6654EC20", "True" );
            // [Attribute Value]: Append Organization Email Header/Footer for [Block]: Group Member Detail for [Page]: Sign-Up Opportunity Attendee List > Group Member Detail
            RockMigrationHelper.AddBlockAttributeValue( "C4D268FC-17B8-4E55-B3A2-7C55F79015BD", "A0513BD2-3A68-40A4-94F0-063AEF476048", "True" );
            // [Attribute Value]: Are Requirements Publicly Hidden for [Block]: Group Member Detail for [Page]: Sign-Up Opportunity Attendee List > Group Member Detail
            RockMigrationHelper.AddBlockAttributeValue( "C4D268FC-17B8-4E55-B3A2-7C55F79015BD", "924FFC5A-FF18-4EC1-ADE6-E5E9BCD3EBA4", "False" );
            // [Attribute Value]: Are Requirements Refreshed When Block Is Loaded for [Block]: Group Member Detail for [Page]: Sign-Up Opportunity Attendee List > Group Member Detail
            RockMigrationHelper.AddBlockAttributeValue( "C4D268FC-17B8-4E55-B3A2-7C55F79015BD", "78A53B17-B4BA-4345-B984-6172E03F9B0E", "False" );
            // [Attribute Value]: Enable Communications for [Block]: Group Member Detail for [Page]: Sign-Up Opportunity Attendee List > Group Member Detail
            RockMigrationHelper.AddBlockAttributeValue( "C4D268FC-17B8-4E55-B3A2-7C55F79015BD", "9C78478B-A1D9-4F62-BB36-EB9F32AA3035", "True" );
            // [Attribute Value]: Enable SMS for [Block]: Group Member Detail for [Page]: Sign-Up Opportunity Attendee List > Group Member Detail
            RockMigrationHelper.AddBlockAttributeValue( "C4D268FC-17B8-4E55-B3A2-7C55F79015BD", "D657F24D-565F-4F19-B6D8-CB0D9A3F3121", "True" );
            // [Attribute Value]: Is Requirement Summary Hidden for [Block]: Group Member Detail for [Page]: Sign-Up Opportunity Attendee List > Group Member Detail
            RockMigrationHelper.AddBlockAttributeValue( "C4D268FC-17B8-4E55-B3A2-7C55F79015BD", "562D04DB-744C-48CC-8738-BA094F6FEA26", "False" );
            // [Attribute Value]: Registration Page for [Block]: Group Member Detail for [Page]: Sign-Up Opportunity Attendee List > Group Member Detail
            RockMigrationHelper.AddBlockAttributeValue( "C4D268FC-17B8-4E55-B3A2-7C55F79015BD", "2EDA5282-EA3E-446F-9CD6-5B3F323FC245", "aaf11844-ec6c-498b-a9d8-387390206570,db9c7e0d-5ec7-4cc2-ba9e-dd398d4b9714" );
            // [Attribute Value]: Show "Move To Another Group" Button for [Block]: Group Member Detail for [Page]: Sign-Up Opportunity Attendee List > Group Member Detail
            RockMigrationHelper.AddBlockAttributeValue( "C4D268FC-17B8-4E55-B3A2-7C55F79015BD", "260A458D-BC35-4A36-B966-172870AFB24B", "False" );
            // [Attribute Value]: Workflow Entry Page for [Block]: Group Member Detail for [Page]: Sign-Up Opportunity Attendee List > Group Member Detail
            RockMigrationHelper.AddBlockAttributeValue( "C4D268FC-17B8-4E55-B3A2-7C55F79015BD", "75C4FE0F-58E1-4BE2-896B-9ADA0A0D4D4F", "0550d2aa-a705-4400-81ff-ab124fdf83d7" );
        }

        /// <summary>
        /// JPH: Add public block types needed for Sign-Up Groups.
        /// </summary>
        private void ShortTermServingProjects_AddPublicBlockTypes()
        {
            RockMigrationHelper.UpdateMobileBlockType( "Sign-Up Finder", "Block used for finding a sign-up group/project.", "Rock.Blocks.Engagement.SignUp.SignUpFinder", "Engagement > Sign-Up", "74A20402-00DF-4A87-98D1-B5A8920F1D32" );
            RockMigrationHelper.UpdateEntityType( "Rock.Blocks.Engagement.SignUp.SignUpFinder", "Sign Up Finder", "Rock.Blocks.Engagement.SignUp.SignUpFinder, Rock.Blocks, Version=1.15.0.10, Culture=neutral, PublicKeyToken=null", false, false, "BF09747C-786D-4979-BADF-2D0157F4CB21" );

            RockMigrationHelper.UpdateMobileBlockType( "Sign-Up Register", "Block used to register for a sign-up group/project occurrence date time.", "Rock.Blocks.Engagement.SignUp.SignUpRegister", "Engagement > Sign-Up", "161587D9-7B74-4D61-BF8E-3CDB38F16A12" );
            RockMigrationHelper.UpdateEntityType( "Rock.Blocks.Engagement.SignUp.SignUpRegister", "Sign Up Register", "Rock.Blocks.Engagement.SignUp.SignUpRegister, Rock.Blocks, Version=1.15.0.10, Culture=neutral, PublicKeyToken=null", false, false, "ED7A31F2-8D4C-469A-B2D8-7E28B8717FB8" );

            RockMigrationHelper.UpdateMobileBlockType( "Sign-Up Attendance Detail", "Lists the group members for a specific sign-up group/project occurrence date time and allows selecting if they attended or not.", "Rock.Blocks.Engagement.SignUp.SignUpAttendanceDetail", "Engagement > Sign-Up", "96D160D9-5668-46EF-9941-702BD3A577DB" );
            RockMigrationHelper.UpdateEntityType( "Rock.Blocks.Engagement.SignUp.SignUpAttendanceDetail", "Sign Up Attendance Detail", "Rock.Blocks.Engagement.SignUp.SignUpAttendanceDetail, Rock.Blocks, Version=1.15.0.10, Culture=neutral, PublicKeyToken=null", false, false, "747587A0-87E9-437D-A4ED-75431CED55B3" );
        }

        /// <summary>
        /// JPH: Add public pages and blocks needed for Sign-Up Groups.
        /// </summary>
        private void ShortTermServingProjects_AddPublicPagesAndBlocks()
        {
            // [Page]: Connect > Sign-Up Finder
            RockMigrationHelper.AddPage( true, "7625A63E-6650-4886-B605-53C2234FA5E1", "325B7BFD-8B80-44FD-A951-4E4763DA6C0D", "Sign-Up Finder", "", "2DC1906D-C9D5-411D-B961-A2295C9450A4", "", "3B31B9A2-DE35-4407-8E7D-3633F93906CD" );
            RockMigrationHelper.AddPageRoute( "2DC1906D-C9D5-411D-B961-A2295C9450A4", "sign-up/finder", "37B68603-6D07-4C37-A89A-253DB72DBBE3" );
            // [Block]: Sign-Up Finder for [Page]: Connect > Sign-Up Finder
            RockMigrationHelper.AddBlock( true, "2DC1906D-C9D5-411D-B961-A2295C9450A4".AsGuidOrNull(), null, null, "74A20402-00DF-4A87-98D1-B5A8920F1D32".AsGuidOrNull(), "Sign-Up Finder", "Main", "", "", 0, "B9ADC017-782B-4100-AA07-DD1D703EE971" );

            // [Page]: Sign-Up Finder > Sign-Up Register
            RockMigrationHelper.AddPage( true, "2DC1906D-C9D5-411D-B961-A2295C9450A4", "325B7BFD-8B80-44FD-A951-4E4763DA6C0D", "Sign-Up Register", "", "BBB8A41D-E65F-4AFF-8987-BFF3458A46C1", "", null );
            RockMigrationHelper.AddPageRoute( "BBB8A41D-E65F-4AFF-8987-BFF3458A46C1", "sign-up/register/{GroupId}/location/{LocationId}/schedule/{ScheduleId}", "E6685354-09C3-479F-8B6A-C6BD0A18A675" );
            // [Block]: Sign-Up Register for [Page]: Sign-Up Finder > Sign-Up Register
            RockMigrationHelper.AddBlock( true, "BBB8A41D-E65F-4AFF-8987-BFF3458A46C1".AsGuidOrNull(), null, null, "161587D9-7B74-4D61-BF8E-3CDB38F16A12".AsGuidOrNull(), "Sign-Up Register", "Main", "", "", 0, "9D0AF2E9-7BE2-4320-A81C-CF22D0E94BD4" );

            // [Page]: Group Attendance > Sign-Up Attendance Detail
            RockMigrationHelper.AddPage( true, "00D2DCE6-D9C0-47A0-BAE1-4591779AE2E1", "325B7BFD-8B80-44FD-A951-4E4763DA6C0D", "Sign-Up Attendance Detail", "", "73FC6F39-6194-483A-BF0D-7FDD1DD91C91", "", "0C00CD89-BF4C-4B19-9B0D-E1FA2CFF5DD7" );
            RockMigrationHelper.AddPageRoute( "73FC6F39-6194-483A-BF0D-7FDD1DD91C91", "sign-up/attendance/{GroupId}/location/{LocationId}/schedule/{ScheduleId}", "33DE9AF2-456C-413D-8559-A58DEA78D62A" );
            // [Block]: Sign Up Attendance Detail for [Page]: Group Attendance > Sign-Up Attendance Detail
            RockMigrationHelper.AddBlock( true, "73FC6F39-6194-483A-BF0D-7FDD1DD91C91".AsGuidOrNull(), null, null, "96D160D9-5668-46EF-9941-702BD3A577DB".AsGuidOrNull(), "Sign Up Attendance Detail", "Main", "", "", 0, "5ED5DD3E-7280-4617-8BB1-0A6CF2DFED6D" );
        }

        /// <summary>
        /// JPH: Delete public pages and blocks added for Sign-Up Groups.
        /// </summary>
        private void ShortTermServingProjects_DeletePublicPagesAndBlocks()
        {
            // [Block]: Sign Up Attendance Detail for [Page]: Group Attendance > Sign-Up Attendance Detail
            RockMigrationHelper.DeleteBlock( "5ED5DD3E-7280-4617-8BB1-0A6CF2DFED6D" );
            // [Page]: Group Attendance > Sign-Up Attendance Detail
            RockMigrationHelper.DeletePageRoute( "33DE9AF2-456C-413D-8559-A58DEA78D62A" );
            RockMigrationHelper.DeletePage( "73FC6F39-6194-483A-BF0D-7FDD1DD91C91" );

            // [Block]: Sign-Up Register for [Page]: Sign-Up Finder > Sign-Up Register
            RockMigrationHelper.DeleteBlock( "9D0AF2E9-7BE2-4320-A81C-CF22D0E94BD4" );
            // [Page]: Sign-Up Finder > Sign-Up Register
            RockMigrationHelper.DeletePageRoute( "E6685354-09C3-479F-8B6A-C6BD0A18A675" );
            RockMigrationHelper.DeletePage( "BBB8A41D-E65F-4AFF-8987-BFF3458A46C1" );

            // [Block]: Sign-Up Finder for [Page]: Connect > Sign-Up Finder
            RockMigrationHelper.DeleteBlock( "B9ADC017-782B-4100-AA07-DD1D703EE971" );
            // [Page]: Connect > Sign-Up Finder
            RockMigrationHelper.DeletePageRoute( "37B68603-6D07-4C37-A89A-253DB72DBBE3" );
            RockMigrationHelper.DeletePage( "2DC1906D-C9D5-411D-B961-A2295C9450A4" );
        }

        /// <summary>
        /// JPH: Delete public block types added for Sign-Up Groups.
        /// </summary>
        private void ShortTermServingProjects_DeletePublicBlockTypes()
        {
            RockMigrationHelper.DeleteBlockType( "74A20402-00DF-4A87-98D1-B5A8920F1D32" );
            RockMigrationHelper.DeleteEntityType( "BF09747C-786D-4979-BADF-2D0157F4CB21" );

            RockMigrationHelper.DeleteBlockType( "161587D9-7B74-4D61-BF8E-3CDB38F16A12" );
            RockMigrationHelper.DeleteEntityType( "ED7A31F2-8D4C-469A-B2D8-7E28B8717FB8" );

            RockMigrationHelper.DeleteBlockType( "96D160D9-5668-46EF-9941-702BD3A577DB" );
            RockMigrationHelper.DeleteEntityType( "747587A0-87E9-437D-A4ED-75431CED55B3" );
        }

        /// <summary>
        /// JPH: Delete admin pages and blocks added for Sign-Up Groups.
        /// </summary>
        private void ShortTermServingProjects_DeleteAdminPagesAndBlocks()
        {
            // [Attribute Value]: Workflow Entry Page for [Block]: Group Member Detail for [Page]: Sign-Up Opportunity Attendee List > Group Member Detail
            RockMigrationHelper.DeleteBlockAttributeValue( "C4D268FC-17B8-4E55-B3A2-7C55F79015BD", "75C4FE0F-58E1-4BE2-896B-9ADA0A0D4D4F" );
            // [Attribute Value]: Show "Move To Another Group" Button for [Block]: Group Member Detail for [Page]: Sign-Up Opportunity Attendee List > Group Member Detail
            RockMigrationHelper.DeleteBlockAttributeValue( "C4D268FC-17B8-4E55-B3A2-7C55F79015BD", "260A458D-BC35-4A36-B966-172870AFB24B" );
            // [Attribute Value]: Registration Page for [Block]: Group Member Detail for [Page]: Sign-Up Opportunity Attendee List > Group Member Detail
            RockMigrationHelper.DeleteBlockAttributeValue( "C4D268FC-17B8-4E55-B3A2-7C55F79015BD", "2EDA5282-EA3E-446F-9CD6-5B3F323FC245" );
            // [Attribute Value]: Is Requirement Summary Hidden for [Block]: Group Member Detail for [Page]: Sign-Up Opportunity Attendee List > Group Member Detail
            RockMigrationHelper.DeleteBlockAttributeValue( "C4D268FC-17B8-4E55-B3A2-7C55F79015BD", "562D04DB-744C-48CC-8738-BA094F6FEA26" );
            // [Attribute Value]: Enable SMS for [Block]: Group Member Detail for [Page]: Sign-Up Opportunity Attendee List > Group Member Detail
            RockMigrationHelper.DeleteBlockAttributeValue( "C4D268FC-17B8-4E55-B3A2-7C55F79015BD", "D657F24D-565F-4F19-B6D8-CB0D9A3F3121" );
            // [Attribute Value]: Enable Communications for [Block]: Group Member Detail for [Page]: Sign-Up Opportunity Attendee List > Group Member Detail
            RockMigrationHelper.DeleteBlockAttributeValue( "C4D268FC-17B8-4E55-B3A2-7C55F79015BD", "9C78478B-A1D9-4F62-BB36-EB9F32AA3035" );
            // [Attribute Value]: Are Requirements Refreshed When Block Is Loaded for [Block]: Group Member Detail for [Page]: Sign-Up Opportunity Attendee List > Group Member Detail
            RockMigrationHelper.DeleteBlockAttributeValue( "C4D268FC-17B8-4E55-B3A2-7C55F79015BD", "78A53B17-B4BA-4345-B984-6172E03F9B0E" );
            // [Attribute Value]: Are Requirements Publicly Hidden for [Block]: Group Member Detail for [Page]: Sign-Up Opportunity Attendee List > Group Member Detail
            RockMigrationHelper.DeleteBlockAttributeValue( "C4D268FC-17B8-4E55-B3A2-7C55F79015BD", "924FFC5A-FF18-4EC1-ADE6-E5E9BCD3EBA4" );
            // [Attribute Value]: Append Organization Email Header/Footer for [Block]: Group Member Detail for [Page]: Sign-Up Opportunity Attendee List > Group Member Detail
            RockMigrationHelper.DeleteBlockAttributeValue( "C4D268FC-17B8-4E55-B3A2-7C55F79015BD", "A0513BD2-3A68-40A4-94F0-063AEF476048" );
            // [Attribute Value]: Allow Selecting 'From' for [Block]: Group Member Detail for [Page]: Sign-Up Opportunity Attendee List > Group Member Detail
            RockMigrationHelper.DeleteBlockAttributeValue( "C4D268FC-17B8-4E55-B3A2-7C55F79015BD", "65FCFD8F-0BD9-4285-AC2B-6CCB6654EC20" );
            // [Block]: Group Member Detail for [Page]: Sign-Up Opportunity Attendee List > Group Member Detail
            RockMigrationHelper.DeleteBlock( "C4D268FC-17B8-4E55-B3A2-7C55F79015BD" );
            // [Page]: Sign-Up Opportunity Attendee List > Group Member Detail
            RockMigrationHelper.DeletePageRoute( "40566DCD-AC73-4C61-95B3-8F9B2E06528C" );
            RockMigrationHelper.DeletePage( "05B79031-183F-4A64-A689-56B5C8E7519F" );

            // [Attribute Value]: Person Profile Page for [Block]: Sign-Up Opportunity Attendee List for [Page]: Sign-Up Detail > Sign-Up Opportunity Attendee List
            RockMigrationHelper.DeleteBlockAttributeValue( "54FC3FA7-2D25-4694-8DD4-647222582CEB", "E1FB0EC5-F0C8-4BBE-BEB1-B2C5D7B1A1C0" );
            // [Attribute Value]: Group Member Detail Page for [Block]: Sign-Up Opportunity Attendee List for [Page]: Sign-Up Detail > Sign-Up Opportunity Attendee List
            RockMigrationHelper.DeleteBlockAttributeValue( "54FC3FA7-2D25-4694-8DD4-647222582CEB", "E0908F51-8B7E-4D94-8972-7DDCDB3D37A6" );
            // [Block]: Sign-Up Opportunity Attendee List for [Page]: Sign-Up Detail > Sign-Up Opportunity Attendee List
            RockMigrationHelper.DeleteBlock( "54FC3FA7-2D25-4694-8DD4-647222582CEB" );
            // [Page]: Sign-Up Detail > Sign-Up Opportunity Attendee List
            RockMigrationHelper.DeletePageRoute( "DB9C7E0D-5EC7-4CC2-BA9E-DD398D4B9714" );
            RockMigrationHelper.DeletePage( "AAF11844-EC6C-498B-A9D8-387390206570" );

            // [Attribute Value]: Treeview Title for [Block]: Sign-Up Groups (Group Tree View) for [Page]: Sign-Up > Sign-Up Detail
            RockMigrationHelper.DeleteBlockAttributeValue( "D493C133-08EE-4E78-A85D-FF8E4FF80158", "D1583306-2504-48D2-98EE-3DE55C2806C7" );
            // [Attribute Value]: Show Settings Panel for [Block]: Sign-Up Groups (Group Tree View) for [Page]: Sign-Up > Sign-Up Detail
            RockMigrationHelper.DeleteBlockAttributeValue( "D493C133-08EE-4E78-A85D-FF8E4FF80158", "4633ED5A-7A2C-4A78-B092-6733FED8CFA6" );
            // [Attribute Value]: Root Group for [Block]: Sign-Up Groups (Group Tree View) for [Page]: Sign-Up > Sign-Up Detail
            RockMigrationHelper.DeleteBlockAttributeValue( "D493C133-08EE-4E78-A85D-FF8E4FF80158", "0E1768CD-87CC-4361-8BCD-01981FBFE24B" );
            // [Attribute Value]: Limit to Security Role Groups for [Block]: Sign-Up Groups (Group Tree View) for [Page]: Sign-Up > Sign-Up Detail
            RockMigrationHelper.DeleteBlockAttributeValue( "D493C133-08EE-4E78-A85D-FF8E4FF80158", "1688837B-73CF-46C3-8880-74C46605807C" );
            // [Attribute Value]: Initial Count Setting for [Block]: Sign-Up Groups (Group Tree View) for [Page]: Sign-Up > Sign-Up Detail
            RockMigrationHelper.DeleteBlockAttributeValue( "D493C133-08EE-4E78-A85D-FF8E4FF80158", "36D18581-3874-4C5A-A01B-793A458F9F91" );
            // [Attribute Value]: Initial Active Setting for [Block]: Sign-Up Groups (Group Tree View) for [Page]: Sign-Up > Sign-Up Detail
            RockMigrationHelper.DeleteBlockAttributeValue( "D493C133-08EE-4E78-A85D-FF8E4FF80158", "2AD968BA-6721-4B69-A4FE-B57D8FB0ECFB" );
            // [Attribute Value]: Display Inactive Campuses for [Block]: Sign-Up Groups (Group Tree View) for [Page]: Sign-Up > Sign-Up Detail
            RockMigrationHelper.DeleteBlockAttributeValue( "D493C133-08EE-4E78-A85D-FF8E4FF80158", "22D5915F-D449-4E03-A8AD-0C473A3D4864" );
            // [Attribute Value]: Disable Auto-Select First Group for [Block]: Sign-Up Groups (Group Tree View) for [Page]: Sign-Up > Sign-Up Detail
            RockMigrationHelper.DeleteBlockAttributeValue( "D493C133-08EE-4E78-A85D-FF8E4FF80158", "AD145399-2D61-40B4-802A-400766574692" );
            // [Attribute Value]: Detail Page for [Block]: Sign-Up Groups (Group Tree View) for [Page]: Sign-Up > Sign-Up Detail
            RockMigrationHelper.DeleteBlockAttributeValue( "D493C133-08EE-4E78-A85D-FF8E4FF80158", "ADCC4391-8D8B-4A28-80AF-24CD6D3F77E2" );
            // [Block]: Sign-Up Groups (Group Tree View) for [Page]: Sign-Up > Sign-Up Detail
            RockMigrationHelper.DeleteBlock( "D493C133-08EE-4E78-A85D-FF8E4FF80158" );
            // [Attribute Value]: Sign-Up Opportunity Attendee List Page for [Block]: Sign-Up Detail for [Page]: Sign-Up > Sign-Up Detail
            RockMigrationHelper.DeleteBlockAttributeValue( "735C2380-5E10-4EDF-91ED-4EDF9BD5C507", "525A1D90-CF46-4710-ADC3-86552EBB1E9C" );
            // [Block]: Sign-Up Detail for [Page]: Sign-Up > Sign-Up Detail
            RockMigrationHelper.DeleteBlock( "735C2380-5E10-4EDF-91ED-4EDF9BD5C507" );
            // [Page]: Sign-Up > Sign-Up Detail
            RockMigrationHelper.DeletePageRoute( "D6AEFD09-630E-40D7-AA56-77CC904C6595" );
            RockMigrationHelper.DeletePage( "34212F8E-5F14-4D92-8B19-46748EBA2727" );

            // [Attribute Value]: Treeview Title for [Block]: Sign-Up Groups (Group Tree View) for [Page]: Engagement > Sign-Up
            RockMigrationHelper.DeleteBlockAttributeValue( "B9D4522A-38D7-4F5B-B9CD-E5497B258471", "D1583306-2504-48D2-98EE-3DE55C2806C7" );
            // [Attribute Value]: Show Settings Panel for [Block]: Sign-Up Groups (Group Tree View) for [Page]: Engagement > Sign-Up
            RockMigrationHelper.DeleteBlockAttributeValue( "B9D4522A-38D7-4F5B-B9CD-E5497B258471", "4633ED5A-7A2C-4A78-B092-6733FED8CFA6" );
            // [Attribute Value]: Root Group for [Block]: Sign-Up Groups (Group Tree View) for [Page]: Engagement > Sign-Up
            RockMigrationHelper.DeleteBlockAttributeValue( "B9D4522A-38D7-4F5B-B9CD-E5497B258471", "0E1768CD-87CC-4361-8BCD-01981FBFE24B" );
            // [Attribute Value]: Limit to Security Role Groups for [Block]: Sign-Up Groups (Group Tree View) for [Page]: Engagement > Sign-Up
            RockMigrationHelper.DeleteBlockAttributeValue( "B9D4522A-38D7-4F5B-B9CD-E5497B258471", "1688837B-73CF-46C3-8880-74C46605807C" );
            // [Attribute Value]: Initial Count Setting for [Block]: Sign-Up Groups (Group Tree View) for [Page]: Engagement > Sign-Up
            RockMigrationHelper.DeleteBlockAttributeValue( "B9D4522A-38D7-4F5B-B9CD-E5497B258471", "36D18581-3874-4C5A-A01B-793A458F9F91" );
            // [Attribute Value]: Initial Active Setting for [Block]: Sign-Up Groups (Group Tree View) for [Page]: Engagement > Sign-Up
            RockMigrationHelper.DeleteBlockAttributeValue( "B9D4522A-38D7-4F5B-B9CD-E5497B258471", "2AD968BA-6721-4B69-A4FE-B57D8FB0ECFB" );
            // [Attribute Value]: Display Inactive Campuses for [Block]: Sign-Up Groups (Group Tree View) for [Page]: Engagement > Sign-Up
            RockMigrationHelper.DeleteBlockAttributeValue( "B9D4522A-38D7-4F5B-B9CD-E5497B258471", "22D5915F-D449-4E03-A8AD-0C473A3D4864" );
            // [Attribute Value]: Disable Auto-Select First Group for [Block]: Sign-Up Groups (Group Tree View) for [Page]: Engagement > Sign-Up
            RockMigrationHelper.DeleteBlockAttributeValue( "B9D4522A-38D7-4F5B-B9CD-E5497B258471", "AD145399-2D61-40B4-802A-400766574692" );
            // [Attribute Value]: Detail Page for [Block]: Sign-Up Groups (Group Tree View) for [Page]: Engagement > Sign-Up
            RockMigrationHelper.DeleteBlockAttributeValue( "B9D4522A-38D7-4F5B-B9CD-E5497B258471", "ADCC4391-8D8B-4A28-80AF-24CD6D3F77E2" );
            // [Block]: Sign-Up Groups (Group Tree View) for [Page]: Engagement > Sign-Up
            RockMigrationHelper.DeleteBlock( "B9D4522A-38D7-4F5B-B9CD-E5497B258471" );
            // [Attribute Value]: Sign-Up Opportunity Attendee List Page for [Block]: Sign-Up Overview for [Page]: Engagement > Sign-Up
            RockMigrationHelper.DeleteBlockAttributeValue( "4ECCC106-5374-4E33-A8AA-3ADE977FB1A4", "B86F9026-5B63-414C-A069-B5C86B8FEFC2" );
            // [Attribute Value]: Project Detail Page for [Block]: Sign-Up Overview for [Page]: Engagement > Sign-Up
            RockMigrationHelper.DeleteBlockAttributeValue( "4ECCC106-5374-4E33-A8AA-3ADE977FB1A4", "E306CB42-10FE-428C-A8B9-224BB7B30C6A" );
            // [Block]: Sign-Up Overview for [Page]: Engagement > Sign-Up
            RockMigrationHelper.DeleteBlock( "4ECCC106-5374-4E33-A8AA-3ADE977FB1A4" );
            // [Page]: Engagement > Sign-Up
            RockMigrationHelper.DeletePageRoute( "75332FE3-DB1B-4B83-9287-5EDDD09A1A4E" );
            RockMigrationHelper.DeletePage( "1941542C-21F2-4341-BDE1-996AA1E0C0A2" );
        }

        /// <summary>
        /// JPH: Delete admin block types added for Sign-Up Groups.
        /// </summary>
        private void ShortTermServingProjects_DeleteAdminBlockTypes()
        {
            RockMigrationHelper.DeleteBlockAttribute( "E306CB42-10FE-428C-A8B9-224BB7B30C6A" );
            RockMigrationHelper.DeleteBlockAttribute( "B86F9026-5B63-414C-A069-B5C86B8FEFC2" );
            RockMigrationHelper.DeleteBlockType( "B539F3B5-01D3-4325-B32A-85AFE2A9D18B" );

            RockMigrationHelper.DeleteBlockAttribute( "525A1D90-CF46-4710-ADC3-86552EBB1E9C" );
            RockMigrationHelper.DeleteBlockType( "69F5C6BD-7A22-42FE-8285-7C8E586E746A" );

            RockMigrationHelper.DeleteBlockAttribute( "E0908F51-8B7E-4D94-8972-7DDCDB3D37A6" );
            RockMigrationHelper.DeleteBlockAttribute( "E1FB0EC5-F0C8-4BBE-BEB1-B2C5D7B1A1C0" );
            RockMigrationHelper.DeleteBlockType( "EE652767-5070-4EAB-8BB7-BB254DD01B46" );

            RockMigrationHelper.DeleteBlockAttribute( "AD145399-2D61-40B4-802A-400766574692" );
        }

        /// <summary>
        /// JPH: Delete system communications added for Sign-Up Groups.
        /// </summary>
        private void ShortTermServingProjects_DeleteSystemCommunications()
        {
            RockMigrationHelper.DeleteSystemCommunication( SystemGuid.SystemCommunication.SIGNUP_GROUP_REMINDER );

            RockMigrationHelper.DeleteCategory( SystemGuid.Category.SYSTEM_COMMUNICATION_SIGNUP_GROUP_CONFIRMATION );
        }

        /// <summary>
        /// JPH: Delete group related records added for Sign-Up Groups.
        /// </summary>
        private void ShortTermServingProjects_DeleteGroups()
        {
            RockMigrationHelper.DeleteGroup( SystemGuid.Group.GROUP_SIGNUP_GROUPS );
            RockMigrationHelper.DeleteAttributeQualifier( "D49200BC-9E54-4906-9B20-53FD8973A43D" );
            RockMigrationHelper.DeleteAttribute( SystemGuid.Attribute.GROUPTYPE_SIGNUP_GROUP_PROJECT_TYPE );
            RockMigrationHelper.DeleteGroupType( SystemGuid.GroupType.GROUPTYPE_SIGNUP_GROUP );
        }

        /// <summary>
        /// JPH: Deleted defined types and values added for Sign-Up Groups.
        /// </summary>
        private void ShortTermServingProjects_DeleteDefinedTypesAndValues()
        {
            RockMigrationHelper.DeleteDefinedValue( SystemGuid.DefinedValue.PROJECT_TYPE_IN_PERSON );
            RockMigrationHelper.DeleteDefinedValue( SystemGuid.DefinedValue.PROJECT_TYPE_PROJECT_DUE );

            RockMigrationHelper.DeleteDefinedType( SystemGuid.DefinedType.PROJECT_TYPE );
        }
    }
}
