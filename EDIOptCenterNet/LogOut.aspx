<%@ Page Title="" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="LogOut.aspx.cs" Inherits="EDIOptCenterNet.Scripts.LogOut" %>
<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <div class="gradientBg">
        <div class="leftCol">
            <h2>Thanks for visiting</h2>
            <p class="SmallGreyTxt">You are now successfully logged out.</p>
        </div>
        <div class="rightCol">
            <div class="RedBottomHead">
                <h2>Where would you like to go next?</h2>
            </div>
            <p><a href="Login.aspx">EDI OptCenter Apps home page</a>. <a href="Login.aspx"><img src="images/arrow-right-sm.png" width="10" height="10" border="0" /></a></p>
            <p><a href="http://www.edioptions.com">EDI Options home page</a>. <a href="http://www.edioptions.com"><img src="images/arrow-right-sm.png" width="10" height="10" border="0" /></a></p>
            <p><a href="http://blog.edioptions.com">EDI Options blog</a>. <a href="http://blog.edioptions.com"><img src="images/arrow-right-sm.png" width="10" height="10" border="0" /></a></p>
        </div>
        <div style="clear:both;"></div>
    </div>
</asp:Content>
