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
            ShortTermServingProjects_AddBlockTypes();
            ShortTermServingProjects_AddPagesAndBlocks();
        }

        /// <summary>
        /// Operations to be performed during the downgrade process.
        /// </summary>
        public override void Down()
        {
            ShortTermServingProjects_DeletePagesAndBlocks();
            ShortTermServingProjects_DeleteBlockTypes();
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
            RockMigrationHelper.AddGroupType( "Sign-Up Group", "Used to track individuals who have signed up for events such as short term serving projects.", "Group", "Member", true, true, true, "fa fa-clipboard-check", 0, null, 3, null, SystemGuid.GroupType.GROUPTYPE_SIGNUP_GROUP );

            RockMigrationHelper.AddGroupTypeRole( SystemGuid.GroupType.GROUPTYPE_SIGNUP_GROUP, "Leader", "Indicates the person is a leader in the group.", 0, null, null, SystemGuid.GroupRole.GROUPROLE_SIGNUP_GROUP_LEADER, true, true, false );
            RockMigrationHelper.AddGroupTypeRole( SystemGuid.GroupType.GROUPTYPE_SIGNUP_GROUP, "Member", "Indicates the person is a member in the group.", 1, null, null, SystemGuid.GroupRole.GROUPROLE_SIGNUP_GROUP_MEMBER, true, false, true );

            RockMigrationHelper.AddGroupTypeAssociation( SystemGuid.GroupType.GROUPTYPE_SIGNUP_GROUP, SystemGuid.GroupType.GROUPTYPE_SIGNUP_GROUP );

            Sql( $"UPDATE [GroupType] SET [IsSchedulingEnabled] = 1 WHERE [Guid] = '{SystemGuid.GroupType.GROUPTYPE_SIGNUP_GROUP}'" );

            Sql( $"UPDATE [GroupType] SET [AllowedScheduleTypes] = 6 WHERE [Guid] = '{SystemGuid.GroupType.GROUPTYPE_SIGNUP_GROUP}'" );

            Sql( $"UPDATE [GroupType] SET [EnableLocationSchedules] = 1 WHERE [Guid] = '{SystemGuid.GroupType.GROUPTYPE_SIGNUP_GROUP}'" );

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
        /// JPH: Add block types needed for Sign-Up Groups.
        /// </summary>
        private void ShortTermServingProjects_AddBlockTypes()
        {
            RockMigrationHelper.UpdateBlockType( "Sign-Up Overview", "Displays an overview of sign-up projects with upcoming and recently-occurred opportunities.", "~/Blocks/SignUp/SignUpOverview.ascx", "Sign-Up", "B539F3B5-01D3-4325-B32A-85AFE2A9D18B" );
            RockMigrationHelper.UpdateBlockType( "Sign-Up Detail", "Displays details about the scheduled opportunities for a given project group.", "~/Blocks/SignUp/SignUpDetail.ascx", "Sign-Up", "69F5C6BD-7A22-42FE-8285-7C8E586E746A" );
            RockMigrationHelper.UpdateBlockType( "Sign-Up Opportunity Attendee List", "Lists all the group members for the selected group, location and schedule.", "~/Blocks/SignUp/SignUpOpportunityAttendeeList.ascx", "Sign-Up", "EE652767-5070-4EAB-8BB7-BB254DD01B46" );
        }

        /// <summary>
        /// JPH: Add pages and blocks needed for Sign-Up Groups.
        /// </summary>
        private void ShortTermServingProjects_AddPagesAndBlocks()
        {
            // [Page]: Engagement > Sign-Up
            RockMigrationHelper.AddPage( true, "48242949-944A-4651-B6CC-60194EDE08A0", "0CB60906-6B74-44FD-AB25-026050EF70EB", "Sign-Up", "", "1941542C-21F2-4341-BDE1-996AA1E0C0A2", "", "2A0C135A-8421-4125-A484-83C8B4FB3D34" );
            RockMigrationHelper.AddPageRoute( "1941542C-21F2-4341-BDE1-996AA1E0C0A2", "people/sign-up", "75332FE3-DB1B-4B83-9287-5EDDD09A1A4E" );
            // [Block]: Sign-Up Overview for [Page]: Engagement > Sign-Up
            RockMigrationHelper.AddBlock( true, "1941542C-21F2-4341-BDE1-996AA1E0C0A2".AsGuidOrNull(), null, null, "B539F3B5-01D3-4325-B32A-85AFE2A9D18B".AsGuidOrNull(), "Sign-Up Overview", "Main", "", "", 0, "4ECCC106-5374-4E33-A8AA-3ADE977FB1A4" );
            // [Block]: Sign-Up Groups (Group Tree View) for [Page]: Engagement > Sign-Up
            RockMigrationHelper.AddBlock( true, "1941542C-21F2-4341-BDE1-996AA1E0C0A2".AsGuidOrNull(), null, null, "2D26A2C4-62DC-4680-8219-A52EB2BC0F65".AsGuidOrNull(), "Sign-Up Groups", "Sidebar1", "", "", 0, "B9D4522A-38D7-4F5B-B9CD-E5497B258471" );
            // [Attribute Value]: Detail Page for [Block]: Sign-Up Groups (Group Tree View) for [Page]: Engagement > Sign-Up
            RockMigrationHelper.AddBlockAttributeValue( "B9D4522A-38D7-4F5B-B9CD-E5497B258471", "ADCC4391-8D8B-4A28-80AF-24CD6D3F77E2", "34212f8e-5f14-4d92-8b19-46748eba2727,d6aefd09-630e-40d7-aa56-77cc904c6595" );
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
            RockMigrationHelper.AddPageRoute( "34212F8E-5F14-4D92-8B19-46748EBA2727", "people/sign-up/detail", "D6AEFD09-630E-40D7-AA56-77CC904C6595" );
            // [Block]: Sign-Up Detail for [Page]: Sign-Up > Sign-Up Detail
            RockMigrationHelper.AddBlock( true, "34212F8E-5F14-4D92-8B19-46748EBA2727".AsGuidOrNull(), null, null, "69F5C6BD-7A22-42FE-8285-7C8E586E746A".AsGuidOrNull(), "Sign-Up Detail", "Main", "", "", 0, "735C2380-5E10-4EDF-91ED-4EDF9BD5C507" );
            // [Block]: Sign-Up Groups (Group Tree View) for [Page]: Sign-Up > Sign-Up Detail
            RockMigrationHelper.AddBlock( true, "34212F8E-5F14-4D92-8B19-46748EBA2727".AsGuidOrNull(), null, null, "2D26A2C4-62DC-4680-8219-A52EB2BC0F65".AsGuidOrNull(), "Sign-Up Groups", "Sidebar1", "", "", 0, "D493C133-08EE-4E78-A85D-FF8E4FF80158" );
            // [Attribute Value]: Detail Page for [Block]: Sign-Up Groups (Group Tree View) for [Page]: Sign-Up > Sign-Up Detail
            RockMigrationHelper.AddBlockAttributeValue( "D493C133-08EE-4E78-A85D-FF8E4FF80158", "ADCC4391-8D8B-4A28-80AF-24CD6D3F77E2", "34212f8e-5f14-4d92-8b19-46748eba2727,d6aefd09-630e-40d7-aa56-77cc904c6595" );
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
            RockMigrationHelper.AddPageRoute( "AAF11844-EC6C-498B-A9D8-387390206570", "people/sign-up/opportunity-attendee-list", "DDD0A160-38BD-4D8A-9CBA-ED6F15622E45" );
            // [Block]: Sign-Up Opportunity Attendee List for [Page]: Sign-Up Detail > Sign-Up Opportunity Attendee List
            RockMigrationHelper.AddBlock( true, "AAF11844-EC6C-498B-A9D8-387390206570".AsGuidOrNull(), null, null, "EE652767-5070-4EAB-8BB7-BB254DD01B46".AsGuidOrNull(), "Sign-Up Opportunity Attendee List", "Main", "", "", 0, "54FC3FA7-2D25-4694-8DD4-647222582CEB" );
        }

        /// <summary>
        /// JPH: Delete pages and blocks added for Sign-Up Groups.
        /// </summary>
        private void ShortTermServingProjects_DeletePagesAndBlocks()
        {
            // [Block]: Sign-Up Opportunity Attendee List for [Page]: Sign-Up Detail > Sign-Up Opportunity Attendee List
            RockMigrationHelper.DeleteBlock( "54FC3FA7-2D25-4694-8DD4-647222582CEB" );
            // [Page]: Sign-Up Detail > Sign-Up Opportunity Attendee List
            RockMigrationHelper.DeletePageRoute( "DDD0A160-38BD-4D8A-9CBA-ED6F15622E45" );
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
            // [Attribute Value]: Detail Page for [Block]: Sign-Up Groups (Group Tree View) for [Page]: Sign-Up > Sign-Up Detail
            RockMigrationHelper.DeleteBlockAttributeValue( "D493C133-08EE-4E78-A85D-FF8E4FF80158", "ADCC4391-8D8B-4A28-80AF-24CD6D3F77E2" );
            // [Block]: Sign-Up Groups (Group Tree View) for [Page]: Sign-Up > Sign-Up Detail
            RockMigrationHelper.DeleteBlock( "D493C133-08EE-4E78-A85D-FF8E4FF80158" );
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
            // [Attribute Value]: Detail Page for [Block]: Sign-Up Groups (Group Tree View) for [Page]: Engagement > Sign-Up
            RockMigrationHelper.DeleteBlockAttributeValue( "B9D4522A-38D7-4F5B-B9CD-E5497B258471", "ADCC4391-8D8B-4A28-80AF-24CD6D3F77E2" );
            // [Block]: Sign-Up Groups (Group Tree View) for [Page]: Engagement > Sign-Up
            RockMigrationHelper.DeleteBlock( "B9D4522A-38D7-4F5B-B9CD-E5497B258471" );
            // [Block]: Sign-Up Overview for [Page]: Engagement > Sign-Up
            RockMigrationHelper.DeleteBlock( "4ECCC106-5374-4E33-A8AA-3ADE977FB1A4" );
            // [Page]: Engagement > Sign-Up
            RockMigrationHelper.DeletePageRoute( "75332FE3-DB1B-4B83-9287-5EDDD09A1A4E" );
            RockMigrationHelper.DeletePage( "1941542C-21F2-4341-BDE1-996AA1E0C0A2" );
        }

        /// <summary>
        /// JPH: Delete block types added for Sign-Up Groups.
        /// </summary>
        private void ShortTermServingProjects_DeleteBlockTypes()
        {
            RockMigrationHelper.DeleteBlockType( "B539F3B5-01D3-4325-B32A-85AFE2A9D18B" );
            RockMigrationHelper.DeleteBlockType( "69F5C6BD-7A22-42FE-8285-7C8E586E746A" );
            RockMigrationHelper.DeleteBlockType( "EE652767-5070-4EAB-8BB7-BB254DD01B46" );
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
