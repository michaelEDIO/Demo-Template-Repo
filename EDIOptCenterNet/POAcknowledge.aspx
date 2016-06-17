<%@ Page Title="PO Acknowledge Manager" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="POAcknowledge.aspx.cs" Inherits="EDIOptCenterNet.POAcknowledge" %>
<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <div class="RedBottomHead">
        <h2>PO Acknowledge Manager</h2>
    </div>
    <div id="upFrame"></div>
    <div class="RedBottomHead">
        <h2>Templates</h2>
    </div>
    <div id="noTemplateDiv"><p>No templates present.</p></div>
    <div>
        <p>
            <a id="templateLink" style="display:none;"><span class="xls_file_s">Click Here To Download</span></a>
        </p>
    </div>
    <div class="RedBottomHead">
        <h2>Prior Records</h2>
    </div>
    <div id="noRecordDiv">No records present.</div>
    <div id="pastRecordDiv"></div>
    
    <script type="text/javascript"></script>
    <script src="Scripts/POAcknowledge/main.js" type="text/javascript"></script>
</asp:Content>
