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

using System.ComponentModel;
using Rock.Attribute;
using Rock.ViewModels.Blocks.Engagement.SignUp.SignUpRegister;

namespace Rock.Blocks.Engagement.SignUp
{
    /// <summary>
    /// Block used to register for a sign-up group/project occurrence date time.
    /// </summary>
    /// <seealso cref="Rock.Blocks.RockObsidianBlockType" />

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
