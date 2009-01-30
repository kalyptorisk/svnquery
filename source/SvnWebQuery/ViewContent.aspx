<%@ Page Language="C#" AutoEventWireup="true" CodeFile="ViewContent.aspx.cs" Inherits="ViewContent" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Untitled Page</title>
</head>
<body style="font-family: arial; font-size: 10pt; background-color: #F0F3FF;">
    <form id="form1" runat="server">
    <p>
        Path:
        <asp:Label ID="lblPath" runat="server" Text="Label"></asp:Label>
        <br />
        Author:</p>
    
    <pre id="content" runat="server">Content</pre>
    
    </form>
    </body>
</html>
