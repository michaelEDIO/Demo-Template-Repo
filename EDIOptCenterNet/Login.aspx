<%@ Page Title="Login" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Login.aspx.cs" Inherits="EDIOptCenterNet.Login" EnableSessionState="True" ValidateRequest="true" %>
<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <div class="gradientBg">
        <div class="leftCol">
            <img src="images/simplify-your-edi.png" width="253" height="176" alt="Simplify your EDI">
        </div>
        <div class="rightCol" style="width:540px;">
            <div class="RedBottomHead">
                <h2>EDI OptCenter Apps Login</h2>
            </div>
            <asp:Login ID="loginControl" runat="server">
                <LayoutTemplate>
                    <table class="VerticalForm">
                        <tr id="userNameRow">
                            <td class="LeftLabel"><asp:Label runat="server" AssociatedControlID="UserName" Text="Username:"></asp:Label></td>
                            <td>
                                <asp:TextBox runat="server" ID="UserName" MaxLength="50"></asp:TextBox>
                                <asp:requiredfieldvalidator id="UserNameRequired" style='display:none' runat="server" ControlToValidate="UserName" Text="Please enter your username" SetFocusOnError="true" ForeColor="#FF0000"></asp:requiredfieldvalidator>
                            </td>
                        </tr>
                        <tr>
                            <td class="LeftLabel"><asp:Label runat="server" AssociatedControlID="Password" Text="Password:"></asp:Label></td>
                            <td>
                            <asp:TextBox runat="server" ID="Password" MaxLength="50" TextMode="Password"></asp:TextBox>
                            <asp:requiredfieldvalidator id="PasswordRequired" style='display:none' runat="server" ControlToValidate="Password" Text="Please enter your password" SetFocusOnError="true" ForeColor="#FF0000"></asp:requiredfieldvalidator>
                            </td>
                        </tr>
                        <tr>
                            <td style="padding:0;">&nbsp;</td>
                            <td style="padding:0;">
                                <asp:Button runat="server" Text="Submit" CssClass="button" CommandName="Login" />
                                <input type="reset" value="Reset" class="button" id="ResetBtn" style="margin-left:15px"></td>
                        </tr>
                    </table>
                </LayoutTemplate>
            </asp:Login>
        </div>
    </div>
    <script type="text/javascript" src="Scripts/Login/main.js"></script>
</asp:Content>
