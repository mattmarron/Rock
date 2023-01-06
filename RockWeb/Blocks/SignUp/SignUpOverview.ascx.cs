using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using Rock;
using Rock.Constants;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.Web.Cache;
using Rock.Web.UI;

namespace RockWeb.Blocks.SignUp
{
    [DisplayName( "Sign-Up Overview" )]
    [Category( "Sign-Up" )]
    [Description( "Displays an overview of sign-up projects with upcoming and recently-occurred opportunities." )]

    [Rock.SystemGuid.BlockTypeGuid( "B539F3B5-01D3-4325-B32A-85AFE2A9D18B" )]
    public partial class SignUpOverview : RockBlock
    {
        #region Private Keys and Fields

        private static class PageParameterKey
        {

        }

        private static class AttributeKey
        {

        }

        private int _groupId;

        #endregion

        #region Properties

        private int SignUpGroupTypeId
        {
            get
            {
                return GroupTypeCache.Get( Rock.SystemGuid.GroupType.GROUPTYPE_SIGNUP_GROUP )?.Id ?? 0;
            }
        }

        #endregion

        #region Control Life-Cycle Events

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( System.EventArgs e )
        {
            base.OnInit( e );

            nbNotAuthorizedToView.Text = EditModeMessage.NotAuthorizedToView( Group.FriendlyTypeName );

            using ( var rockContext = new RockContext() )
            {
                var group = GetSharedGroup( rockContext );
                if ( group != null )
                {
                    InitializeGrid( group );
                }
            }

            // This event gets fired after block settings are updated. It's nice to repaint the screen if these settings would alter it.
            this.BlockUpdated += Block_BlockUpdated;
            this.AddConfigurationUpdateTrigger( upnlSignUpOverview );
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( System.EventArgs e )
        {
            base.OnLoad( e );

            ShowDetails();
        }

        /// <summary>
        /// Handles the BlockUpdated event of the Block control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        /// <exception cref="NotImplementedException"></exception>
        protected void Block_BlockUpdated( object sender, EventArgs e )
        {
            NavigateToCurrentPageReference();
        }

        #endregion

        #region Opportunities Grid Events

        /// <summary>
        /// Displays the gfOpportunities filter values.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The e.</param>
        protected void gfOpportunities_DisplayFilterValue( object sender, Rock.Web.UI.Controls.GridFilter.DisplayFilterValueArgs e )
        {

        }

        /// <summary>
        /// Handles the ApplyFilterClick event of the gfOpportunities control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs" /> instance containing the event data.</param>
        protected void gfOpportunities_ApplyFilterClick( object sender, System.EventArgs e )
        {

        }

        /// <summary>
        /// Handles the ClearFilterClick event of the gfOpportunities control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs" /> instance containing the event data.</param>
        protected void gfOpportunities_ClearFilterClick( object sender, System.EventArgs e )
        {

        }

        /// <summary>
        /// Handles the RowDataBound event of the gOpportunities control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Web.UI.WebControls.GridViewRowEventArgs" /> instance containing the event data.</param>
        protected void gOpportunities_RowDataBound( object sender, System.Web.UI.WebControls.GridViewRowEventArgs e )
        {

        }

        /// <summary>
        /// Handles the GridRebind event of the gOpportunities control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="Rock.Web.UI.Controls.GridRebindEventArgs" /> instance containing the event data.</param>
        protected void gOpportunities_GridRebind( object sender, Rock.Web.UI.Controls.GridRebindEventArgs e )
        {

        }

        /// <summary>
        /// Handles the RowSelected event of the gOpportunities control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="Rock.Web.UI.Controls.RowEventArgs" /> instance containing the event data.</param>
        protected void gOpportunities_RowSelected( object sender, Rock.Web.UI.Controls.RowEventArgs e )
        {

        }

        #endregion

        #region Internal Members

        /// <summary>
        /// Gets the shared group.
        /// </summary>
        /// <param name="rockContext">The rock context.</param>
        /// <returns></returns>
        private Group GetSharedGroup( RockContext rockContext )
        {
            if ( _groupId <= 0 )
            {
                _groupId = new GroupService( rockContext ).GetId( Rock.SystemGuid.Group.GROUP_SIGNUP_GROUPS.AsGuid() ) ?? 0;
            }

            var key = $"Group:{_groupId}";
            var group = RockPage.GetSharedItem( key ) as Group;

            if ( group == null && _groupId > 0 )
            {
                group = new GroupService( rockContext )
                    .Queryable()
                    .AsNoTracking()
                    .Include( g => g.GroupType )
                    .Include( g => g.ParentGroup )
                    .FirstOrDefault( g => g.Id == _groupId );

                RockPage.SaveSharedItem( key, group );
            }

            return group;
        }

        /// <summary>
        /// Initializes the grid.
        /// </summary>
        /// <param name="group">The group.</param>
        private void InitializeGrid( Group group )
        {
            gOpportunities.ExportFilename = group.Name;
        }

        /// <summary>
        /// Resets the message boxes.
        /// </summary>
        private void ResetMessageBoxes()
        {
            nbNotFoundOrArchived.Visible = false;
            nbNotAuthorizedToView.Visible = false;
            nbInvalidGroupType.Visible = false;
            pnlDetails.Visible = true;
        }

        /// <summary>
        /// Shows the group not found message.
        /// </summary>
        private void ShowGroupNotFoundMessage()
        {
            nbNotFoundOrArchived.Visible = true;
            pnlDetails.Visible = false;
        }

        /// <summary>
        /// Shows the not authorized to view message.
        /// </summary>
        private void ShowNotAuthorizedToViewMessage()
        {
            nbNotAuthorizedToView.Visible = true;
            pnlDetails.Visible = false;
        }

        /// <summary>
        /// Shows the invalid group type message.
        /// </summary>
        private void ShowInvalidGroupTypeMessage()
        {
            nbInvalidGroupType.Visible = true;
            pnlDetails.Visible = false;
        }

        /// <summary>
        /// Shows the details.
        /// </summary>
        private void ShowDetails()
        {
            ResetMessageBoxes();

            using ( var rockContext = new RockContext() )
            {
                var group = GetSharedGroup( rockContext );
                if ( group == null )
                {
                    ShowGroupNotFoundMessage();
                    return;
                }

                if ( !EnsureGroupIsAllowed( group ) )
                {
                    return;
                }

                var opportunities = GetOpportunities( rockContext, group );

                BindOpportunitiesGrid( opportunities );
            }
        }

        /// <summary>
        /// Ensures the group is allowed.
        /// </summary>
        /// <param name="group">The group.</param>
        /// <returns></returns>
        private bool EnsureGroupIsAllowed( Group group )
        {
            if ( !group.IsAuthorized( Authorization.VIEW, this.CurrentPerson ) )
            {
                ShowNotAuthorizedToViewMessage();
                return false;
            }

            if ( group.GroupTypeId != SignUpGroupTypeId && group.GroupType.InheritedGroupTypeId != SignUpGroupTypeId )
            {
                ShowInvalidGroupTypeMessage();
                return false;
            }

            return true;
        }

        private class Opportunity
        {
            public int GroupLocationId { get; set; }

            public int LocationId { get; set; }

            public int ScheduleId { get; set; }

            public string Name { get; set; }

            public string FriendlyDateTime { get; set; }

            public int LeaderCount { get; set; }

            public int ParticipantCount { get; set; }
        }

        /// <summary>
        /// Gets the opportunities.
        /// </summary>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="group">The group.</param>
        /// <returns></returns>
        private List<Opportunity> GetOpportunities( RockContext rockContext, Group group = null )
        {
            group = group ?? GetSharedGroup( rockContext );

            //var opportunities = new GroupLocationService( rockContext )
            //    .Queryable()
            //    .AsNoTracking()
            //    .Where()

            return null;
        }

        /// <summary>
        /// Binds the opportunities grid.
        /// </summary>
        /// <param name="opportunities">The opportunities.</param>
        private void BindOpportunitiesGrid( List<Opportunity> opportunities = null )
        {
            if ( opportunities == null )
            {
                using ( var rockContext = new RockContext() )
                {
                    opportunities = GetOpportunities( rockContext );
                }
            }

            gOpportunities.DataSource = opportunities;
            gOpportunities.DataBind();
        }

        #endregion
    }
}

