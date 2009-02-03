<%@ Page Language="C#" AutoEventWireup="true" CodeFile="View.aspx.cs" Inherits="View" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
     <title></title>
</head>
<body style="font-family: arial; font-size: 10pt; background-color: #F0F3FF;">
    <h2 id="header" runat="server">
        Path/to/file</h2>
    <table id="properties" runat="server">
        <tr>
            <td>Author:</td><td id="author" runat="server" ></td>
        </tr>
        <tr>
            <td>Modified:</td><td id="modified" runat="server" ></td>
        </tr>
        <tr>
            <td>Revisions:</td><td id="revisions" runat="server" ></td>
        </tr>
        <tr id="sizeRow" runat="server">
            <td>File Size:</td><td id="size" runat="server" ></td>
        </tr>
        <tr>
            <td valign="top">Message:</td><td id="message" runat="server" style="white-space: pre"></td>
        </tr>
    </table>
    <p id="contentWarning" runat="server" style="font-style:italic; color:Gray"></p>
    <pre id="content" runat="server"></pre>
</body>
</html>
