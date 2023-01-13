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
using System.Linq;

using Rock.Attribute;
using Rock.Model;
using Rock.ViewModels.Utility;
using Rock.Web.Cache;

namespace Rock.Blocks.Utility
{
    /// <summary>
    /// Allows interaction with the SMS Test transport.
    /// </summary>
    /// <seealso cref="Rock.Blocks.RockObsidianDetailBlockType" />

    [DisplayName( "SMS Test Transport" )]
    [Category( "Utility" )]
    [Description( "Allows interaction with the SMS Test transport." )]
    [IconCssClass( "fa fa-chat" )]

    #region Block Attributes

    #endregion

    [Rock.SystemGuid.EntityTypeGuid( "803db5ae-0a92-4b6d-a5bd-81845d1202ae" )]
    [Rock.SystemGuid.BlockTypeGuid( "2c2d6bc3-8257-4e23-8fe7-06e744d58ac0" )]
    public class SmsTestTransport : RockObsidianDetailBlockType
    {
        public override string BlockFileUrl => $"{base.BlockFileUrl}.obs";

        #region Methods

        /// <inheritdoc/>
        public override object GetObsidianBlockInitialization()
        {
            var definedType = DefinedTypeCache.Get( SystemGuid.DefinedType.COMMUNICATION_SMS_FROM );

            return new
            {
                PhoneNumbers = definedType.DefinedValues
                    .Select( v => new ListItemBag
                    {
                        Value = v.Value,
                        Text = v.Description.IsNotNullOrWhiteSpace() ? $"{v.Value} ({v.Description})" : v.Value
                    } )
                    .ToList()
            };
        }

        #endregion
    }
}