using System.ComponentModel;
using Rock.Attribute;
using Rock.ViewModels.Blocks.Engagement.SignUp.SignUpRegister;

namespace Rock.Blocks.Engagement.SignUp
{
    [DisplayName( "Sign-Up Register" )]
    [Category( "Engagement > Sign-Up" )]
    [Description( "Block used to register for a sign-up group/project occurrence date time." )]
    [IconCssClass( "fa fa-clipboard-check" )]

    #region Block Attributes



    #endregion

    [Rock.SystemGuid.EntityTypeGuid( "ED7A31F2-8D4C-469A-B2D8-7E28B8717FB8" )]
    [Rock.SystemGuid.BlockTypeGuid( "161587D9-7B74-4D61-BF8E-3CDB38F16A12" )]
    public class SignUpRegister : RockObsidianBlockType
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
            var box = new SignUpRegisterInitializationBox();



            return box;
        }

        #endregion


    }
}
