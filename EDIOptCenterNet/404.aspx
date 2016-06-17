<%@ Page Title="" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="404.aspx.cs" Inherits="EDIOptCenterNet._404" %>
<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <div class="gradientBg" style="min-height:355px">
        <div class="leftCol"><img src="/images/404.png" width="296" height="121" alt="404"></div>
        <div class="rightCol">
            <div class="RedBottomHead">
                <h2>HTTP Error 404: File Not Found</h2>
            </div>
            <img src="/images/missing-page.png" width="37" height="43" alt="missing page" style="float:left;">
            <p class="SmallGreyTxt BtmMargin" style="margin-left:50px;">The page or file you are looking for might have been removed, had its name changed, or is temporarily unavailable.</p>
            <img src="/images/bad-url.png" width="29" height="43" alt="bad URL" style="float:left;">
            <p class="SmallGreyTxt BtmMargin" style="margin-left:50px;">If you typed in the URL, please check the spelling. Otherwise you can click the <a href="javascript: history.go(-1)" title="Go back">back button</a> and try another link.</p>
            <img src="/images/speech-bubble.png" width="34" height="25" alt="speech bubble" style="float:left;">
            <p class="SmallGreyTxt BtmMargin" style="margin-left:50px;">If you need additional assistance please contact our support team with the tab on the right.</p>
        </div>
        <div style="clear: both"></div>
    </div>
</asp:Content>
