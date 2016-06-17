<%@ Page Title="Location Report" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="LocationReport.aspx.cs" Inherits="EDIOptCenterNet.LocationReport" %>
<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
<div id="contentDiv">
    <div class="RedBottomHead">
        <h2>Location Report</h2>
    </div>
    <p class='info'>The following reports will be produced for the active partner only.</p>
    <table id="dateTable" class="VerticalForm">
        <tbody>
            <tr>
                <td class="LeftLabelWide"><span>Report Type: </span></td>
                <td><select id="reportType"></select></td>
            </tr>
            <tr>
                <td class="LeftLabelWide"><span>Start Date: </span></td>
                <td><input type="text" id="startDate" /><label class='error' id="startDateInvalid" style="display: none;">Please enter a valid date.</label></td>
            </tr>
            <tr>
                <td class='LeftLabelWide'><span>End Date: </span></td>
                <td><input type="text" id="endDate" /><label class='error' id="endDateInvalid" style="display: none;">Please enter a valid date.</label></td>
            </tr>
            <tr id="bolFilter" style="display: none;">
                <td>
                    <span class="LeftLabelWide">BOL Number:</span>
                </td>
                <td><input type="text" id="bolBox" /></td>
            </tr>
            <tr>
                <td>
                    <span class="LeftLabelWide">PO Number:</span>
                </td>
                <td><input type="text" id="poBox" /></td>
            </tr>
            <tr>
                <td>
                    <span class="LeftLabelWide">Stores:</span>
                    <div class="tooltip-parent">
                        <img src="images/question-mark.png" width="17" height="16" alt="?" class="tooltip-link">
                        <div class="tooltip-content"><img src="images/tooltip-arrow.png" width="11" height="12" alt="arrow" class="tooltip-arrow">Enter the store ID of the store you'd like to see. You may select as many stores as desired.</div>
                    </div>
                </td>
                <td><button type="button" class="button" id="addSTButton">Add</button></td>
            </tr>
        </tbody>
    </table>
     <div class="BtmMargin"></div>
    <div class="RedBottomHead">
        <h2>Report Attributes</h2>
    </div>
    <div class="FilterContainer">
        <div class="FilterAttributes SmBtmMargin">
            <div id="FilterCriteriaDiv">
            </div>
        </div>
        <button type="button" class="button" id="reportButton">Download</button>
    </div>
    <div class="BtmMargin"></div>
</div>
<link href="Css/ui-darkness/jquery-ui.css" type="text/css" rel="Stylesheet" />
<link href="Css/salesreq.css" type="text/css" rel="Stylesheet" />
<script type="text/javascript" src="Scripts/LOC/main.js?t=<% Response.Write(DateTime.Now.ToString("yyyyMMddHHmmssffff")); %>"></script>
</asp:Content>
