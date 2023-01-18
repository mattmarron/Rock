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
using System.Web;

namespace Rock.Web.Utilities
{
    /// <summary>
    /// Class PathHelper.
    /// </summary>
    internal static class PathHelper
    {
        /// <summary>
        /// The rock theme
        /// </summary>
        private const string RockTheme = "Rock";

        /// <summary>
        /// Gets the root path.
        /// </summary>
        /// <returns>System.String.</returns>
        public static string GetRootPath()
        {
            var uri = new Uri( HttpContext.Current.Request.UrlProxySafe().ToString() );
            return uri.Scheme + "://" + uri.GetComponents( UriComponents.HostAndPort, UriFormat.UriEscaped ) + ResolveRockUrl( "~" );
        }

        /// <summary>
        /// Resolves the rock URL.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <returns>System.String.</returns>
        public static string ResolveRockUrl( string input )
        {
            if ( input.IsNullOrWhiteSpace() )
            {
                return input;
            }

            return VirtualPathUtility.ToAbsolute( input );
        }

        /// <summary>
        /// Resolves the rock URL.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <param name="theme">The theme.</param>
        /// <returns>System.String.</returns>
        public static string ResolveRockUrl( string input, string theme )
        {
            if ( input.StartsWith( "~~" ) )
            {
                if ( theme.IsNullOrWhiteSpace() )
                {
                    theme = RockTheme;
                }

                input = "~/Themes/" + theme + ( input.Length > 2 ? input.Substring( 2 ) : string.Empty );
            }

            return ResolveRockUrl( input );
        }
    }
}
