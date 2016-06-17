<%@ Page Title="Sales Data" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="SalesRequest.aspx.cs" Inherits="EDIOptCenterNet.SalesRequest" EnableSessionState="True" %>
<asp:Content ID="HeaderContent" ContentPlaceHolderID="HeadContent" runat="server">
</asp:Content>
<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <div class="RedBottomHead">
        <h2>Report Options</h2>
    </div>
    <table id="controlTable">
        <tbody>
            <tr>
                <td>
                <span class="LeftLabel tooltip-head" style="width:auto!important">Stores</span>
                <div class="tooltip-parent">
                        <img src="images/question-mark.png" width="17" height="16" alt="?" class="tooltip-link">
                        <div class="tooltip-content"><img src="images/tooltip-arrow.png" width="11" height="12" alt="arrow" class="tooltip-arrow">Enter the store ID of the store you'd like to see. You may select as many stores as desired.</div>
                    </div>
                </td>
                <td><input type="text" id="storeSelect"/></td>
                <td><button type="button" class="button" id="selectStoreButton">Add</button></td>
            </tr>
            <tr>
                <td>
                    <span class="LeftLabel tooltip-head" style="width:auto!important">Retail Weeks</span>
                    <div class="tooltip-parent">
                        <img src="images/question-mark.png" width="17" height="16" alt="?" class="tooltip-link">
                        <div class="tooltip-content"><img src="images/tooltip-arrow.png" width="11" height="12" alt="arrow" class="tooltip-arrow">You may select up to 7 different retail weeks to view. If none are selected, the last 3 retail weeks will be used.</div>
                    </div>
                </td>
                <td><select id="weekSelect"></select></td>
                <td><button type="button" class="button" id="selectWeekButton">Add</button></td>
            </tr>
            <tr>
                <td>
                    <span class="LeftLabel tooltip-head" style="width:auto!important">REPORT TYPE</span>
                    <div class="tooltip-parent">
                        <img src="images/question-mark.png" width="17" height="16" alt="?" class="tooltip-link">
                        <div class="tooltip-content"><img src="images/tooltip-arrow.png" width="11" height="12" alt="arrow" class="tooltip-arrow">Select the type of report to view or request.</div>
                    </div>
                </td>
                <td>
                    <select id="reportSelect">
                        <option value="ESS" selected="selected">Store Sales Analysis</option>
                        <option value="EST">Shipment Tracking</option>
                    </select>
                </td>
            </tr>
            <tr>
                <td>
                    <input type="checkbox" id="sendEmailCheckBox" style="margin-left:0px!important;"/>
                    <label for="sendEmailCheckBox">Send report to email:</label>
                </td>
                <td>
                    <input id="email" type="text" value="<%= partnerEmail %>" />
                </td>
            </tr>
        </tbody>
    </table>
    <div class="BtmMargin"></div>
    <div class="RedBottomHead">
        <h2>Report Attributes</h2>
    </div>
    <div class="FilterContainer">
        <div class="FilterAttributes SmBtmMargin">
            <div class="StoreCriteriaDiv">
            </div>
            <div class="WeekCriteriaDiv">
            </div>
        </div>
        <button type="button" class="button" id="submitRequestButton" title="Submit a request for a spreadsheet report" style="margin-right:5px">SUBMIT</button>
        <button type="button" class="button" id="viewReportButton">VIEW REPORT</button>
    </div>
    <div class="BtmMargin"></div>
    <div class="RedBottomHead">
        <h2>Prior Reports</h2>
    </div>
    <div id="pastRequestDiv">
        <span id="emptyMessageSpan">No report requests found.</span>
        <asp:GridView runat="server" ID="pastOrderGrid" CssClass="dataTable prevTable blueBg PaddedTbl" AutoGenerateColumns="false">
            <HeaderStyle BorderColor="Transparent" HorizontalAlign="Center" />
            <RowStyle BorderColor="Transparent" />
            <Columns>
                <asp:BoundField DataField="RequestDate" HeaderText="Request Date">
                    <HeaderStyle Width="100px" />
                    <ItemStyle HorizontalAlign="Left" />
                </asp:BoundField>
                <asp:BoundField DataField="OutputName" HeaderText="Output File">
                    <HeaderStyle Width="100px" />
                    <ItemStyle HorizontalAlign="Left" />
                </asp:BoundField>
                <asp:BoundField DataField="Email" HeaderText="Email">
                    <HeaderStyle Width="100px" />
                    <ItemStyle HorizontalAlign="Left" />
                </asp:BoundField>
                <asp:BoundField DataField="Status" HeaderText="Status">
                    <HeaderStyle Width="100px" />
                    <ItemStyle HorizontalAlign="Left" CssClass="status" />
                </asp:BoundField>
            </Columns>
        </asp:GridView>
    </div>
    <link href="Css/ui-darkness/jquery-ui.css" type="text/css" rel="Stylesheet" />
    <link href="Css/common.css" type="text/css" rel="Stylesheet" />
    <link href="Css/salesreq.css" type="text/css" rel="Stylesheet" />
    <script type="text/javascript">
        var storeList = <%= storeList %>;
        var retailWeekList = <%= retailWeekList %>;
    </script>
    <script type="text/javascript" src="Scripts/jquery.stickytableheaders.min.js"></script>
    <script type="text/javascript" src="Scripts/SalesRequest/main.js"></script>
</asp:Content>
