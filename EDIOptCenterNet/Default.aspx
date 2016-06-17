<%@ Page Title="Home Page" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true"
    CodeBehind="Default.aspx.cs" Inherits="EDIOptCenterNet._Default" EnableSessionState="True" %>
<asp:Content ID="HeaderContent" runat="server" ContentPlaceHolderID="HeadContent">
</asp:Content>
<asp:Content ID="BodyContent" runat="server" ContentPlaceHolderID="MainContent">
    <div class="RedBottomHead">
        <h2>Apps Center</h2>
    </div>
    <span><%= WelcomeMsg %></span>
    <script type="text/javascript" src="Scripts/Welcome/main.js"></script>
    <link href="Css/common.css" type="text/css" rel="Stylesheet" />
</asp:Content>
