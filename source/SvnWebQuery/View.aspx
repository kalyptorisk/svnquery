<%@ Page Language="C#" AutoEventWireup="true" CodeFile="View.aspx.cs" Inherits="View" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <meta http-equiv="Content-Type" content="text/html; charset=UTF-8" />
	<title></title>
	<link type="text/css" rel="stylesheet" href="styles/shCore.css"/>
	<link type="text/css" rel="stylesheet" href="styles/shThemeDefault.css"/>
	<script type="text/javascript" src="scripts/shCore.js"></script>
	<script type="text/javascript" src="scripts/shBrushBash.js"></script>
	<script type="text/javascript" src="scripts/shBrushCpp.js"></script>
	<script type="text/javascript" src="scripts/shBrushCSharp.js"></script>
	<script type="text/javascript" src="scripts/shBrushCss.js"></script>
	<script type="text/javascript" src="scripts/shBrushDelphi.js"></script>
	<script type="text/javascript" src="scripts/shBrushDiff.js"></script>
	<script type="text/javascript" src="scripts/shBrushGroovy.js"></script>
	<script type="text/javascript" src="scripts/shBrushJava.js"></script>
	<script type="text/javascript" src="scripts/shBrushJScript.js"></script>
	<script type="text/javascript" src="scripts/shBrushPhp.js"></script>
	<script type="text/javascript" src="scripts/shBrushPlain.js"></script>
	<script type="text/javascript" src="scripts/shBrushPython.js"></script>
	<script type="text/javascript" src="scripts/shBrushRuby.js"></script>
	<script type="text/javascript" src="scripts/shBrushScala.js"></script>
	<script type="text/javascript" src="scripts/shBrushSql.js"></script>
	<script type="text/javascript" src="scripts/shBrushVb.js"></script>
	<script type="text/javascript" src="scripts/shBrushXml.js"></script>
	
	<script type="text/javascript">
	    SyntaxHighlighter.all();
	    SyntaxHighlighter.defaults.gutter = false;
	</script>
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
