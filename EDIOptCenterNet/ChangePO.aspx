<%@ Page Title="PO Change" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="ChangePO.aspx.cs" Inherits="EDIOptCenterNet.ChangePO" %>
<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <div class='RedBottomHead'>
        <h2>Purchase Order Changes</h2>
    </div>
    <div id="contentDiv">
    </div>
    <script type="text/javascript">
        var CPODetails = <%= CPODetails %>;
    </script>
    <script type="text/javascript" src="Scripts/jquery.stickytableheaders.min.js"></script>
    <script type="text/javascript" src="Scripts/ChangePO/main.js"></script>
</asp:Content>
