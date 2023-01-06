<%@ Control Language="C#" AutoEventWireup="true" CodeFile="SignUpOverview.ascx.cs" Inherits="RockWeb.Blocks.SignUp.SignUpOverview" %>

<asp:UpdatePanel ID="upnlSignUpOverview" runat="server">
    <ContentTemplate>
        <Rock:NotificationBox ID="nbNotFoundOrArchived" runat="server" NotificationBoxType="Warning" Visible="false" Text="The selected group does not exist or it has been archived." />
        <Rock:NotificationBox ID="nbNotAuthorizedToView" runat="server" NotificationBoxType="Warning" />
        <Rock:NotificationBox ID="nbInvalidGroupType" runat="server" NotificationBoxType="Warning" Visible="false" Text="The selected group is not of a type that can be edited as a sign-up group." />

        <asp:Panel ID="pnlDetails" runat="server">
            <div class="panel panel-block">

                <div class="panel-heading">
                    <h1 class="panel-title pull-left">Sign-Up Overview</h1>
                </div>

                <div class="panel-body">
                    <div class="grid grid-panel">
                        <Rock:GridFilter ID="gfOpportunities" runat="server" OnDisplayFilterValue="gfOpportunities_DisplayFilterValue" OnApplyFilterClick="gfOpportunities_ApplyFilterClick" OnClearFilterClick="gfOpportunities_ClearFilterClick">

                        </Rock:GridFilter>
                        <Rock:Grid ID="gOpportunities" runat="server" DisplayType="Full" AllowSorting="true" RowItemText="Opportunity" OnRowDataBound="gOpportunities_RowDataBound" OnGridRebind="gOpportunities_GridRebind" OnRowSelected="gOpportunities_RowSelected" ExportSource="ColumnOutput" ShowConfirmDeleteDialog="true">
                            <Columns>
                                <Rock:SelectField></Rock:SelectField>
                                <Rock:RockBoundField DataField="Name" ExcelExportBehavior="AlwaysInclude" />
                                <Rock:RockBoundField DataField="FriendlySchedule" ExcelExportBehavior="AlwaysInclude" />
                                <Rock:RockBoundField DataField="LeaderCount" ExcelExportBehavior="AlwaysInclude" />
                                <Rock:BadgeField DataField="ParticipantCount" ExcelExportBehavior="AlwaysInclude" />
                            </Columns>
                        </Rock:Grid>
                    </div>
                </div>

            </div>
        </asp:Panel>

    </ContentTemplate>
</asp:UpdatePanel>