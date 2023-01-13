using System.ComponentModel;
using Rock.Attribute;
using Rock.ViewModels.Blocks.Engagement.SignUp.SignUpAttendanceDetail;

namespace Rock.Blocks.Engagement.SignUp
{
    [DisplayName( "Sign-Up Attendance Detail" )]
    [Category( "Engagement > Sign-Up" )]
    [Description( "Lists the group members for a specific sign-up group/project occurrence date time and allows selecting if they attended or not." )]
    [IconCssClass( "fa fa-clipboard-check" )]

    #region Block Attributes



    #endregion

    [Rock.SystemGuid.EntityTypeGuid( "747587A0-87E9-437D-A4ED-75431CED55B3" )]
    [Rock.SystemGuid.BlockTypeGuid( "96D160D9-5668-46EF-9941-702BD3A577DB" )]
    public class SignUpAttendanceDetail : RockObsidianBlockType
    {
        #region Keys

        private static class AttributeKey
        {

        }

        #endregion

        public override string BlockFileUrl => $"{base.BlockFileUrl}.obs";

        #region Methods

        /// <inheritdoc/>
        public override object GetObsidianBlockInitialization()
        {
            var box = new SignUpAttendanceDetailInitializationBox();



            return box;
        }

        #endregion


    }
}
