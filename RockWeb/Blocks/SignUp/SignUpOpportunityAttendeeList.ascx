<%@ Control Language="C#" AutoEventWireup="true" CodeFile="SignUpOpportunityAttendeeList.ascx.cs" Inherits="RockWeb.Blocks.SignUp.SignUpOpportunityAttendeeList" %>

<asp:UpdatePanel ID="upnlSignUpOpportunityAttendeeList" runat="server">
    <ContentTemplate>
        <Rock:NotificationBox ID="nbMissingIds" runat="server" NotificationBoxType="Warning" />
        <Rock:NotificationBox ID="nbNotFoundOrArchived" runat="server" NotificationBoxType="Warning" Visible="false" Text="The selected group does not exist or it has been archived." />
        <Rock:NotificationBox ID="nbNotAuthorizedToView" runat="server" NotificationBoxType="Warning" />
        <Rock:NotificationBox ID="nbInvalidGroupType" runat="server" NotificationBoxType="Warning" Visible="false" Text="The selected group is not of a type that can be edited as a sign-up group." />
        <Rock:NotificationBox ID="nbOpportunityNotFound" runat="server" NotificationBoxType="Warning" Text="The selected sign-up opportunity does not exist." />

        <asp:Panel ID="pnlDetails" runat="server">
            <div class="panel panel-block">

                <div class="panel-heading">
                    <h1 class="panel-title pull-left">
                        <asp:Literal ID="lTitle" runat="server" />
                    </h1>

                    <div class="panel-labels">
                        <Rock:HighlightLabel ID="hlGroupType" runat="server" LabelType="Default" />
                        <Rock:HighlightLabel ID="hlCampus" runat="server" LabelType="Campus" />
                        <Rock:HighlightLabel ID="hlInactive" runat="server" LabelType="Danger" Text="Inactive" />
                    </div>
                </div>

                <div class="panel-body">

                    <div class="row">
                        <div class="col-md-6">
                            <Rock:RockControlWrapper ID="rcwLocation" runat="server" Label="Location">
                                <asp:Literal ID="lLocation" runat="server" />
                            </Rock:RockControlWrapper>
                        </div>
                        <div class="col-md-6">
                            <Rock:RockControlWrapper ID="rcwSchedule" runat="server" Label="Schedule">
                                <asp:Literal ID="lSchedule" runat="server" />
                            </Rock:RockControlWrapper>
                        </div>
                    </div>

                    <div class="row">
                        <div class="col-md-6">
                            <Rock:RockControlWrapper ID="rcwConfiguredSlots" runat="server" Label="Configured Slots">
                                <asp:Literal ID="lConfiguredSlots" runat="server" />
                            </Rock:RockControlWrapper>
                        </div>
                        <div class="col-md-6">
                            <Rock:RockControlWrapper ID="rcwSlotsFilled" runat="server" Label="Slots Filled">
                                <Rock:Badge ID="bSlotsFilled" runat="server" BadgeType="W" />
                            </Rock:RockControlWrapper>
                        </div>
                    </div>

                </div>

            </div>

            <div class="panel panel-block">

                <div class="panel-heading">
                    <h1 class="panel-title pull-left">Attendees</h1>
                </div>

                <div class="panel-body">
                    <div class="grid grid-panel">
                        <Rock:GridFilter ID="gfMembers" runat="server">
                            <Rock:RockTextBox ID="tbFirstName" runat="server" Label="First Name" />
                            <Rock:RockTextBox ID="tbLastName" runat="server" Label="Last Name" />
                        </Rock:GridFilter>
                        <Rock:Grid ID="gMembers" runat="server"  DisplayType="Full" AllowSorting="true" RowItemText="Attendee" ShowConfirmDeleteDialog="true">
                            <Columns>
                                <Rock:SelectField></Rock:SelectField>
                                <Rock:RockLiteralField ID="lExportFullName" HeaderText="Name" Visible="false" ExcelExportBehavior="AlwaysInclude" />
                                <Rock:RockLiteralField ID="lNameWithHtml" HeaderText="Name" SortExpression="Person.LastName,Person.NickName" ExcelExportBehavior="NeverInclude" />
                                <Rock:RockBoundField DataField="GroupMember.GroupRole.Name" HeaderText="Role" SortExpression="GroupRole.Name" />
                                <Rock:RockBoundField DataField="GroupMember.GroupMemberStatus" HeaderText="Member Status" SortExpression="GroupMemberStatus" />

                                <%-- Fields that are only shown when exporting --%>
                                <Rock:RockBoundField DataField="Person.NickName" HeaderText="Nick Name" Visible="false" ExcelExportBehavior="AlwaysInclude" />
                                <Rock:RockBoundField DataField="Person.LastName" HeaderText="Last Name" Visible="false" ExcelExportBehavior="AlwaysInclude" />
                                <Rock:RockBoundField DataField="Person.BirthDate" HeaderText="Birth Date" Visible="false" ExcelExportBehavior="AlwaysInclude" />
                                <Rock:RockBoundField DataField="Person.Age" HeaderText="Age" Visible="false" ExcelExportBehavior="AlwaysInclude" />
                                <Rock:RockBoundField DataField="Person.Email" HeaderText="Email" Visible="false" ExcelExportBehavior="AlwaysInclude" />
                                <Rock:RockBoundField DataField="Person.RecordStatusValueId" HeaderText="RecordStatusValueId" Visible="false" ExcelExportBehavior="AlwaysInclude" />
                                <Rock:DefinedValueField DataField="Person.RecordStatusValueId" HeaderText="Record Status" Visible="false" ExcelExportBehavior="AlwaysInclude" />
                                <Rock:RockBoundField DataField="Person.Gender" HeaderText="Gender" Visible="false" ExcelExportBehavior="AlwaysInclude" />
                                <Rock:RockBoundField DataField="Person.IsDeceased" HeaderText="Is Deceased" Visible="false" ExcelExportBehavior="AlwaysInclude" />

                                <Rock:RockLiteralField ID="lExportHomePhone" HeaderText="Home Phone" Visible="false" ExcelExportBehavior="AlwaysInclude" />
                                <Rock:RockLiteralField ID="lExportCellPhone" HeaderText="Cell Phone" Visible="false" ExcelExportBehavior="AlwaysInclude" />
                                <Rock:RockLiteralField ID="lExportHomeAddress" HeaderText="Home Address" Visible="false" ExcelExportBehavior="AlwaysInclude" />
                                <Rock:RockLiteralField ID="lExportLatitude" HeaderText="Latitude" Visible="false" ExcelExportBehavior="AlwaysInclude" />
                                <Rock:RockLiteralField ID="lExportLongitude" HeaderText="Longitude" Visible="false" ExcelExportBehavior="AlwaysInclude" />
                            </Columns>
                        </Rock:Grid>
                    </div>
                </div>

            </div>

        </asp:Panel>

    </ContentTemplate>
</asp:UpdatePanel>
