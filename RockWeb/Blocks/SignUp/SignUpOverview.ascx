<%@ Control Language="C#" AutoEventWireup="true" CodeFile="SignUpOverview.ascx.cs" Inherits="RockWeb.Blocks.SignUp.SignUpOverview" %>

<asp:UpdatePanel ID="upnlSignUpOverview" runat="server">
    <ContentTemplate>

        <asp:Panel ID="pnlDetails" runat="server">
            <div class="panel panel-block">

                <div class="panel-heading">
                    <h1 class="panel-title pull-left">Sign-Up Overview</h1>
                </div>

                <div class="panel-body">
                    <div class="grid grid-panel">
                        <Rock:GridFilter ID="gfOpportunities" runat="server" OnDisplayFilterValue="gfOpportunities_DisplayFilterValue" OnApplyFilterClick="gfOpportunities_ApplyFilterClick" OnClearFilterClick="gfOpportunities_ClearFilterClick">
                            <Rock:RockTextBox ID="tbProjectName" runat="server" Label="Project Name" />
                        </Rock:GridFilter>
                        <Rock:Grid ID="gOpportunities" runat="server" DataKeyNames="GroupId,LocationId,ScheduleId" DisplayType="Full" AllowSorting="true" CssClass="js-grid-opportunities" RowItemText="Opportunity" OnDataBinding="gOpportunities_DataBinding" OnRowDataBound="gOpportunities_RowDataBound" OnGridRebind="gOpportunities_GridRebind" OnRowSelected="gOpportunities_RowSelected" ExportSource="ColumnOutput" ShowConfirmDeleteDialog="true">
                            <Columns>
                                <Rock:SelectField></Rock:SelectField>
                                <Rock:RockBoundField DataField="ProjectName" HeaderText="Project Name" ExcelExportBehavior="AlwaysInclude" />
                                <Rock:RockBoundField DataField="FriendlySchedule" HeaderText="Schedule" ExcelExportBehavior="AlwaysInclude" />
                                <Rock:RockBoundField DataField="LeaderCount" HeaderText="Leader Count" ExcelExportBehavior="AlwaysInclude" />
                                <Rock:RockLiteralField ID="lParticipantCountBadgeHtml" HeaderText="Participant Count" ExcelExportBehavior="NeverInclude" />
                                <Rock:LinkButtonField ID="lbOpportunityDetail" Text="<i class='fa fa-users'></i>" CssClass="btn btn-default btn-sm btn-square" OnClick="lbOpportunityDetail_Click" HeaderStyle-HorizontalAlign="Center" ItemStyle-HorizontalAlign="Center" />
                                <Rock:DeleteField ID="dfOpportunities" OnClick="dfOpportunities_Click" />

                                <%-- Fields that are only shown when exporting --%>
                                <Rock:RockBoundField DataField="ParticipantCount" HeaderText="Participant Count" Visible="False" ExcelExportBehavior="AlwaysInclude" />
                            </Columns>
                        </Rock:Grid>
                    </div>
                </div>

            </div>
        </asp:Panel>

        <script>

            Sys.Application.add_load(function () {
                var $thisBlock = $('#<%= upnlSignUpOverview.ClientID %>');

                // delete prompt
                $thisBlock.find('table.js-grid-opportunities a.grid-delete-button').on('click', function (e) {
                    var $btn = $(this);
                    var $row = $btn.closest('tr');

                    var confirmMessage = 'Are you sure you want to delete this Opportunity?';

                    if ($row.hasClass('js-has-participants')) {
                        var participantCount = parseInt($row.find('.participant-count-badge').html());
                        var participantLabel = participantCount > 1
                            ? 'participants'
                            : 'participant';

                        confirmMessage = 'This Opportunity has ' + participantCount + ' ' + participantLabel + '. Are you sure you want to delete this Opportunity and remove all participants? ';
                    }

                    e.preventDefault();
                    Rock.dialogs.confirm(confirmMessage, function (result) {
                        if (result) {
                            window.location = e.target.href ? e.target.href : e.target.parentElement.href;
                        }
                    });
                });

            });

        </script>

    </ContentTemplate>
</asp:UpdatePanel>