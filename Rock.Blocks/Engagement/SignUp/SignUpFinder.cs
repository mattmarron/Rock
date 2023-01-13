using System.ComponentModel;
using Rock.Attribute;
using Rock.ViewModels.Blocks.Engagement.SignUp.Finder;

namespace Rock.Blocks.Engagement.SignUp
{
    [DisplayName( "Sign-Up Finder" )]
    [Category( "Engagement > Sign-Up" )]
    [Description( "Block used for finding a sign-up group/project." )]
    [IconCssClass( "fa fa-clipboard-check" )]

    #region Block Attributes



    #endregion

    [Rock.SystemGuid.EntityTypeGuid( "BF09747C-786D-4979-BADF-2D0157F4CB21" )]
    [Rock.SystemGuid.BlockTypeGuid( "74A20402-00DF-4A87-98D1-B5A8920F1D32" )]
    public class SignUpFinder : RockObsidianBlockType
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
            var box = new SignUpFinderInitializationBox();



            return box;
        }

        #endregion


    }
}
