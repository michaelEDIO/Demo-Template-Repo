<%@ Page Title="GLC vs MIL Cost Report" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="GVMCostReport.aspx.cs" Inherits="EDIOptCenterNet.GVMCostReport" %>
<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
<div id="contentDiv">
    <div class="RedBottomHead">
        <h2>GLC vs MIL Cost Report</h2>
    </div>
    <p class='info'>The following report will include all partners.</p>
    <table id="dateTable" class="VerticalForm">
        <tbody>
            <tr>
                <td class="LeftLabel"><span>Start Date: </span></td>
                <td><input type="text" id="startDate" /><label class='error' id="startDateInvalid" style="display: none;">Please enter a valid date.</label></td>
            </tr>
            <tr>
                <td class='LeftLabel'><span>End Date: </span></td>
                <td><input type="text" id="endDate" /><label class='error' id="endDateInvalid" style="display: none;">Please enter a valid date.</label></td>
            </tr>
            <tr>
                <td><button type="button" class="button" id="reportButton">Download</button></td>
            </tr>
        </tbody>
    </table>
</div>
<link href="Css/ui-darkness/jquery-ui.css" type="text/css" rel="Stylesheet" />
<script type="text/javascript" src="Scripts/GVM/main.js"></script>
</asp:Content>
