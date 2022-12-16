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
using System.Data.Entity;
using System.Linq;
using System.Text;

using Rock.Attribute;
using Rock.Common.Mobile.Blocks.Content;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;

namespace Rock.Blocks.Types.Mobile.Events
{
    /// <summary>
    /// Displays a list of SMS conversations that the individual can interact with.
    /// </summary>
    /// <seealso cref="Rock.Blocks.RockMobileBlockType" />

    [DisplayName( "SMS Conversation List" )]
    [Category( "Mobile > Communication" )]
    [Description( "Displays a list of SMS conversations that the individual can interact with." )]
    [IconCssClass( "fa fa-sms" )]

    #region Block Attributes

    #endregion

    [Rock.SystemGuid.EntityTypeGuid( "77701BE2-1335-45F3-93B3-F06466CA391F" )]
    [Rock.SystemGuid.BlockTypeGuid( "E16DC868-101F-4944-BE6C-29D858D9821D" )]
    public class SmsConversationList : RockMobileBlockType
    {
        #region Block Attributes

        /// <summary>
        /// The block setting attribute keys for the <see cref="SmsConversationList"/> block.
        /// </summary>
        public static class AttributeKeys
        {
        }

        #endregion

        #region IRockMobileBlockType Implementation

        /// <summary>
        /// Gets the required mobile application binary interface version required to render this block.
        /// </summary>
        /// <value>
        /// The required mobile application binary interface version required to render this block.
        /// </value>
        public override int RequiredMobileAbiVersion => 5;

        /// <summary>
        /// Gets the class name of the mobile block to use during rendering on the device.
        /// </summary>
        /// <value>
        /// The class name of the mobile block to use during rendering on the device
        /// </value>
        public override string MobileBlockType => "Rock.Mobile.Blocks.Communication.SmsConversationList";

        /// <summary>
        /// Gets the property values that will be sent to the device in the application bundle.
        /// </summary>
        /// <returns>
        /// A collection of string/object pairs.
        /// </returns>
        public override object GetMobileConfigurationValues()
        {
            return new
            {
            };
        }

        #endregion

        #region Methods

        #endregion

        #region Action Methods

        #endregion
    }
}
