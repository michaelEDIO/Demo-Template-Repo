﻿<%@ Page Title="" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="PoDropShip.aspx.cs" Inherits="EDIOptCenterNet.PoDropShip" %>
<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <div id="mainDiv"></div>
    <link href="Css/ui-darkness/jquery-ui.css" type="text/css" rel="Stylesheet" />
    <link href="Css/common.css" type="text/css" rel="Stylesheet" />
    <script src="Scripts/pagetable.js?t=<% Response.Write(DateTime.Now.ToString("yyyyMMddHHmmssffff")); %>" type="text/javascript"></script>
    <script src="Scripts/PDS/main.js?t=<% Response.Write(DateTime.Now.ToString("yyyyMMddHHmmssffff")); %>" type="text/javascript"></script>
</asp:Content>
