<%@ Page Title="Item Distribution" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true"
    CodeBehind="IDM.aspx.cs" Inherits="EDIOptCenterNet.IDM" %>

<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <div class="RedBottomHead">
        <h2>Order Production</h2>
    </div>
    <div class="distributionReadOnly">
        <span>Produce orders on: </span>
        <asp:Label runat="server" ID="distroBox" CssClass="daysText" />
    </div>
    <div class="distributionReadOnly">
        <span>Minimum Dollar Amount: $</span><span runat="server" ID="minDollarLabel" />
    </div>
    <div class="distributionReadOnly">
        <span>Day Range: </span><span runat="server" ID="dayRangeLabel" />
    </div>
    <div class="RedBottomHead">
        <h2>Item Distribution Manager</h2>
    </div>
    <div id='mainField'>
        <div>
            <div class="toolbar clearfix" data-rel-tbl="MainContent_itemGrid">
                <div class="toolbar-edit">
                    <div class="countDiv">
                        <p class="ToolbarHead">
                            &nbsp;</p>
                        <ul>
                            <li class="CollCount tooltip" data-tooltip="Your current collection count">0</li>
                        </ul>
                    </div>
                    <div class="editDiv">
                        <p class="ToolbarHead">
                            Edit</p>
                        <ul class="toolbar-icons">
                            <li class="ToolbarEditHeadBtn tooltip" data-tooltip="Edit Distribution">&nbsp;</li>
                        </ul>
                    </div>
                </div>
            </div>
            <asp:GridView ID="itemGrid" data-rel-tbl="MainContent_itemGrid" CssClass="dataTable prevTable blueBg PaddedTbl"
                runat="server" AutoGenerateColumns="False">
                <HeaderStyle BorderColor="Transparent" HorizontalAlign="Center" />
                <RowStyle BorderColor="Transparent" />
                <Columns>
                    <asp:BoundField DataField="Vendor" HeaderText="Vendor #">
                        <HeaderStyle Width="100px" />
                        <ItemStyle HorizontalAlign="Left" CssClass="VendorID" />
                    </asp:BoundField>
                    <asp:BoundField DataField="ItemUPC" HeaderText="UPC #">
                        <HeaderStyle Width="120px" />
                        <ItemStyle HorizontalAlign="Left" />
                    </asp:BoundField>
                    <asp:BoundField DataField="Description" HeaderText="Description">
                        <ItemStyle HorizontalAlign="Left" />
                    </asp:BoundField>
                    <asp:BoundField DataField="CurMin" HeaderText="Current Min">
                        <HeaderStyle Width="100px" />
                        <ItemStyle HorizontalAlign="Left" CssClass="CurMin" />
                    </asp:BoundField>
                    <asp:BoundField DataField="CurMax" HeaderText="Current Max">
                        <HeaderStyle Width="100px" />
                        <ItemStyle HorizontalAlign="Left" CssClass="CurMax" />
                    </asp:BoundField>
                    <asp:BoundField DataField="CurReorder" HeaderText="Current Reorder">
                        <HeaderStyle Width="100px" />
                        <ItemStyle HorizontalAlign="Left" CssClass="CurReorder" />
                    </asp:BoundField>
                </Columns>
            </asp:GridView>
        </div>
    </div>
    <div id='editField' style='display: none;'>
        <div class="RedBottomHead">
            <h2>Order Production</h2>
        </div>
        <div class="distribution">
            <span>Produce orders on: </span>
            <asp:CheckBox ID="sunCheckbox" runat="server" CssClass="sunCheckbox distroBox" TextAlign="Right"
                Text="Sunday" />
            <asp:CheckBox ID="monCheckbox" runat="server" CssClass="monCheckbox distroBox" TextAlign="Right"
                Text="Monday" />
            <asp:CheckBox ID="tueCheckbox" runat="server" CssClass="tueCheckbox distroBox" TextAlign="Right"
                Text="Tuesday" />
            <asp:CheckBox ID="wedCheckbox" runat="server" CssClass="wedCheckbox distroBox" TextAlign="Right"
                Text="Wednesday" />
            <asp:CheckBox ID="thuCheckbox" runat="server" CssClass="thuCheckbox distroBox" TextAlign="Right"
                Text="Thursday" />
            <asp:CheckBox ID="friCheckbox" runat="server" CssClass="friCheckbox distroBox" TextAlign="Right"
                Text="Friday" />
            <asp:CheckBox ID="satCheckbox" runat="server" CssClass="satCheckbox distroBox" TextAlign="Right"
                Text="Saturday" />
        </div>
        <div class="distribution">
          <span>Minimum Dollar Amount: $</span><input runat="server" type="text" id="minDollarBox" />
        </div>
        <div class="distribution">
           <span>Day Range: </span><input runat="server" type="text" id="dayRangeBox" />
        </div>
        <div class="RedBottomHead">
            <h2>Item Details<h2>
        </div>
        <div>
            <div class="toolbar" data-rel-tbl="MainContent_editGrid">
                <div class="toolbar-edit">
                    <div class="countDiv">
                        <p class="ToolbarHead">
                            &nbsp;</p>
                        <ul>
                            <li class="CollCount tooltip" data-tooltip="Your current collection count">0</li>
                        </ul>
                    </div>
                    <div class="applyDiv">
                        <p class="ToolbarHead">
                            Edit Quantities</p>
                        <input type='text' class="applyBox minApplyBox tooltip" placeholder="Minimum" title="Blank to keep, 'd' for default." />
                        <input type='text' class="applyBox maxApplyBox tooltip" placeholder="Maximum" title="Blank to keep, 'd' for default." />
                        <input type='text' class="applyBox reoApplyBox tooltip" placeholder="Reorder" title="Blank to keep, 'd' for default." />
                        <input type="button" id="applyButton" class="button btnSm tooltip" value="&#9654;"
                            data-tooltip="Apply changes" />
                    </div>
                </div>
            </div>
            <asp:GridView ID="editGrid" data-rel-tbl="MainContent_editGrid" CssClass="dataTable mainTable blueBg PaddedTbl"
                runat="server" AutoGenerateColumns="False">
                <HeaderStyle BorderColor="Transparent" HorizontalAlign="Center" />
                <RowStyle BorderColor="Transparent" />
                <Columns>
                    <asp:TemplateField>
                        <HeaderTemplate>
                            <asp:CheckBox ID="CheckBox1" runat="server" CssClass="CollAllCheckbox" />
                        </HeaderTemplate>
                        <ItemTemplate>
                            <asp:CheckBox ID="CheckBox2" runat="server" CssClass="CollCheckbox" />
                        </ItemTemplate>
                        <HeaderStyle Width="134px" />
                        <ItemStyle HorizontalAlign="Left" />
                    </asp:TemplateField>
                    <asp:BoundField DataField="Vendor" HeaderText="Vendor #">
                        <HeaderStyle Width="150px" />
                        <ItemStyle HorizontalAlign="Left" CssClass="VendorID" />
                    </asp:BoundField>
                    <asp:BoundField DataField="ItemUPC" HeaderText="UPC #">
                        <HeaderStyle Width="200px" />
                        <ItemStyle HorizontalAlign="Left" />
                    </asp:BoundField>
                    <asp:BoundField DataField="Description" HeaderText="Description">
                        <ItemStyle HorizontalAlign="Left" CssClass="shortdesc" />
                    </asp:BoundField>
                    <asp:BoundField DataField="BaseMin" HeaderText="Min">
                        <HeaderStyle Width="200px" />
                        <ItemStyle HorizontalAlign="Left" CssClass="BaseMin" />
                    </asp:BoundField>
                    <asp:BoundField DataField="BaseMax" HeaderText="Max">
                        <HeaderStyle Width="200px" />
                        <ItemStyle HorizontalAlign="Left" CssClass="BaseMax" />
                    </asp:BoundField>
                    <asp:BoundField DataField="BaseReorder" HeaderText="Reorder">
                        <HeaderStyle Width="280px" />
                        <ItemStyle HorizontalAlign="Left" CssClass="BaseReorder" />
                    </asp:BoundField>
                    <asp:TemplateField>
                        <HeaderTemplate>
                            <span>New Min</span>
                        </HeaderTemplate>
                        <ItemTemplate>
                            <asp:TextBox ID="TextBox1" CssClass="CurMin numBox columnCur" runat="server" Text='<%# Eval("CurMin") %>'
                                Width="80px" />
                        </ItemTemplate>
                        <HeaderStyle Width="300px" />
                        <ItemStyle HorizontalAlign="Left" />
                    </asp:TemplateField>
                    <asp:TemplateField>
                        <HeaderTemplate>
                            <span>New Max</span>
                        </HeaderTemplate>
                        <ItemTemplate>
                            <asp:TextBox ID="TextBox2" CssClass="CurMax numBox columnCur" runat="server" Text='<%# Eval("CurMax") %>'
                                Width="80px" />
                        </ItemTemplate>
                        <HeaderStyle Width="300px" />
                        <ItemStyle HorizontalAlign="Left" />
                    </asp:TemplateField>
                    <asp:TemplateField>
                        <HeaderTemplate>
                            <span>New Reorder</span>
                        </HeaderTemplate>
                        <ItemTemplate>
                            <asp:TextBox ID="TextBox3" CssClass="CurReorder numBox columnCur" runat="server"
                                Text='<%# Eval("CurReorder") %>' Width="80px" />
                        </ItemTemplate>
                        <HeaderStyle Width="300px" />
                        <ItemStyle HorizontalAlign="Left" />
                    </asp:TemplateField>
                </Columns>
            </asp:GridView>
            <div class="submitDiv">
                <button type="button" class="button" id="commitButton" style="margin-right: 10px">
                    COMMIT CHANGES</button>
                <button type="button" class="button CloseWindowBtn" id="closeButton">
                    CLOSE WINDOW</button>
            </div>
        </div>
    </div>
    <link href="Css/common.css" type="text/css" rel="Stylesheet" />
    <link href="Css/idm.css" type="text/css" rel="Stylesheet" />
    <script type="text/javascript" src="Scripts/IDM/main.js"></script>
</asp:Content>
