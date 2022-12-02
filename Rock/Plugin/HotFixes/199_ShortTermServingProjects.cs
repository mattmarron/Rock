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
            AddDefinedTypesAndValues();
            AddGroups();
            AddSystemCommunications();
            AddBlocks();
        }

        /// <summary>
        /// Operations to be performed during the downgrade process.
        /// </summary>
        public override void Down()
        {
            DeleteBlocks();
            DeleteSystemCommunications();
            DeleteGroups();
            DeleteDefinedTypesAndValues();
        }

        /// <summary>
        /// JPH: Add defined types and values needed for Sign-Up Groups.
        /// </summary>
        private void AddDefinedTypesAndValues()
        {
            RockMigrationHelper.AddDefinedType( "Group", "Project Type", "List of different types (In-Person, Project Due, etc.) of projects.", SystemGuid.DefinedType.PROJECT_TYPE, string.Empty );

            RockMigrationHelper.UpdateDefinedValue( SystemGuid.DefinedType.PROJECT_TYPE, "In-Person", "The project happens on the configured date/time.", SystemGuid.DefinedValue.PROJECT_TYPE_IN_PERSON );
            RockMigrationHelper.UpdateDefinedValue( SystemGuid.DefinedType.PROJECT_TYPE, "Project Due", "The project is due on the configured date/time.", SystemGuid.DefinedValue.PROJECT_TYPE_PROJECT_DUE );
        }

        /// <summary>
        /// JPH: Add group related records needed for Sign-Up Groups.
        /// </summary>
        private void AddGroups()
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
        private void AddSystemCommunications()
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
                // subject
                "Sign-Up Group Reminder for {{ OccurrenceTitle }} on {{ Occurrence.OccurrenceDate | Date:'dddd, MMMM d, yyyy' }}",
                // body
                "  {{ 'Global' | Attribute:'EmailHeader' }}    <h1>Sign-Up Group Reminder</h1>    <p>Hi {{  Person.NickName  }}!</p>    <p>This is a reminder that you have signed up for {{ OccurrenceTitle }} on {{ Occurrence.OccurrenceDate | Date:'dddd, MMMM d, yyyy' }}.</p>    <p>Thanks!</p>    <p>{{ 'Global' | Attribute:'OrganizationName' }}</p>    {{ 'Global' | Attribute:'EmailFooter' }}  ",
                SystemGuid.SystemCommunication.SIGNUP_GROUP_REMINDER, // guid
                true, // isActive
                "This is a reminder from {{ 'Global' | Attribute:'OrganizationName' }} that you have signed up for {{ OccurrenceTitle }} on {{ Occurrence.OccurrenceDate | Date:'dddd, MMMM d, yyyy' }}." // smsMessage
            );
        }

        /// <summary>
        /// JPH: Add blocks needed for Sign-Up Groups.
        /// </summary>
        private void AddBlocks()
        {
            RockMigrationHelper.UpdateBlockType( "Sign-Up Detail", "Displays details about the scheduled opportunities for a given project group.", "~/Blocks/SignUp/SignUpDetail.ascx", "Groups", "69F5C6BD-7A22-42FE-8285-7C8E586E746A" );
        }

        /// <summary>
        /// JPH: Delete blocks added for Sign-Up Groups.
        /// </summary>
        private void DeleteBlocks()
        {
            RockMigrationHelper.DeleteBlock( "3460B380-1AF6-4B22-9C1C-0F7E3054F30" );
        }

        /// <summary>
        /// JPH: Delete system communications added for Sign-Up Groups.
        /// </summary>
        private void DeleteSystemCommunications()
        {
            RockMigrationHelper.DeleteSystemCommunication( SystemGuid.SystemCommunication.SIGNUP_GROUP_REMINDER );

            RockMigrationHelper.DeleteCategory( SystemGuid.Category.SYSTEM_COMMUNICATION_SIGNUP_GROUP_CONFIRMATION );
        }

        /// <summary>
        /// JPH: Delete group related records added for Sign-Up Groups.
        /// </summary>
        private void DeleteGroups()
        {
            RockMigrationHelper.DeleteGroup( SystemGuid.Group.GROUP_SIGNUP_GROUPS );
            RockMigrationHelper.DeleteAttributeQualifier( "D49200BC-9E54-4906-9B20-53FD8973A43D" );
            RockMigrationHelper.DeleteAttribute( SystemGuid.Attribute.GROUPTYPE_SIGNUP_GROUP_PROJECT_TYPE );
            RockMigrationHelper.DeleteGroupType( SystemGuid.GroupType.GROUPTYPE_SIGNUP_GROUP );
        }

        /// <summary>
        /// JPH: Deleted defined types and values added for Sign-Up Groups.
        /// </summary>
        private void DeleteDefinedTypesAndValues()
        {
            RockMigrationHelper.DeleteDefinedValue( SystemGuid.DefinedValue.PROJECT_TYPE_IN_PERSON );
            RockMigrationHelper.DeleteDefinedValue( SystemGuid.DefinedValue.PROJECT_TYPE_PROJECT_DUE );
            RockMigrationHelper.DeleteDefinedType( SystemGuid.DefinedType.PROJECT_TYPE );
        }
    }
}
