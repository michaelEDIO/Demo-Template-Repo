<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Upload.aspx.cs" Inherits="EDIOptCenterNet.Upload" %>
<!DOCTYPE html>
<html>
    <head runat="server">
        <title></title>
        <link href="~/Css/screen.css" rel="stylesheet" type="text/css" />
        <script src="Scripts/jquery.js" type="text/javascript"></script>
    </head>
    <body style="background-image: none;">
        <form id="uploadForm" runat="server" enctype="multipart/form-data">
            <div>
                <asp:FileUpload runat="server" ID="fileUpload" uploaded="false" />
                <button id="submitButton" class="button" runat="server" type="submit">Upload</button>
            </div>
        </form>
    </body>
</html>
