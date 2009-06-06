<%@ Page Language="C#" AutoEventWireup="true" CodeFile="Query.aspx.cs" Inherits="Query" ValidateRequest="false" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Search</title>

    <script type="text/javascript">
        function toggleRevisionRange() {
            var textRevision = document.getElementById('textRevision');
            var optAll = document.getElementById('optAll');
            var optHead = document.getElementById('optHead');
            var toggle = document.getElementById('toggle');     
            if (textRevision.style.display == "none" ) {
                textRevision.style.display = "";
                optGroup.style.display = "none";
                textRevision.value = "";
            }
            else {
                textRevision.style.display = "none";
                textRevision.value = "$hidden$";
                optGroup.style.display = "";
            }
        }                    
    </script>

    <style type="text/css">
        .style1
        {
            text-align: left;
        }
    </style>

</head>
<body style="font-family: arial; font-size: 10pt; background-color: #F0F3FF;">
    <form runat="server" defaultbutton="btnSearch" defaultfocus="txQuery">  
    <table width="100%">
    <tr>
    <td>
    <table style="margin-left: -3px; padding: 0px">
        <tr style="margin-bottom: 0px; padding-bottom: 0px">
            <td style="font-size: 18pt; font-weight: bold; color: #CC6600; font-style: italic;">
                <asp:Label ID="_repositoryLabel" runat="server">Repository</asp:Label>&nbsp;Search</td>
            <td style="font-size: 8pt; height: 20; text-align: right; padding-top: 10px" nowrap="nowrap">
                <asp:Panel ID="_revisionUi" runat="server">
                <input type="text" style="visibility: hidden; width: 10px;" />
                <a id="toggle" href="#" onclick="toggleRevisionRange()">Revision</a>
                <asp:TextBox ID="_textRevision" runat="server" Text="$hidden$" Font-Size="8pt" ToolTip="Enter a revision or a revision range" Width="83px" />
                <span id="_optGroup" runat="server" style="text-align: right">
                    <asp:RadioButton ID="_optHead" runat="server" Text="Head" Font-Size="8pt" GroupName="revisionGroup" Checked="True" ToolTip="Search only in head revision" />
                    <asp:RadioButton ID="_optAll" runat="server" Text="All&nbsp;&nbsp;" Font-Size="8pt" GroupName="revisionGroup" ToolTip="Search in all revisions" />
                </span>
                </asp:Panel>
            
            </td>
            <td>
            </td>
            <td>
            </td>
        </tr>
        <tr style="margin-top: -4px;">
            <td colspan="2">
                <asp:TextBox ID="inputQuery" runat="server" Width="500px" />
            </td>
            <td>
                <asp:Button ID="btnSearch" runat="server" OnClick="OnSearch" Text="Search" />
            </td>
            <td style="font-size: 9pt; padding-left: 12px; ">
                <a href="Help.htm">Help</a>
            </td>
        </tr>
    </table></td>
    <td valign="top" >
    <p style="position: relative; float: right; top: 0px; right: -5px; width: 100px; text-align: center; font-style: italic; color:#909090;">
           <a href="http://svnquery.tigris.org" style="color:#909090"> 
             <span style="font-size: xx-small;font-weight: bold; " >powered by</span><br/>
             <span style="font-size: large; font-weight: bold;" >Svn Query</span></a><br />
             <span style="font-size: xx-small; font-style: normal"><asp:Label ID="SvnQueryVersionLabel" runat="server">0.0.0.0</asp:Label></span> 
</p>
    </td>
    </tr>
    </table>  
    <asp:Label ID="messsageLabel" runat="server" />
    <asp:Panel ID="resultsPanel" runat="server" Visible="False">
        <table width="100%" style="background-color: #FFCC99; border-top-width: 1px; margin-bottom: 8px; border-top-color: #CC6600; border-top-style: solid;">
            <tr>
                <td align="left">
                    <asp:Label ID="hitsLabel" runat="server" Text="<b>123 hits</b> for bli bla blub"/>
                </td>
                <td align="right">
                    <asp:Label ID="statisticsLabel" runat="server" Text="<span style='color:#808080'>(23440 documents searched in 789ms)</span>" />
                </td>
            </tr>
        </table>
        <asp:ListView ID="listView" runat="server" DataSourceID="dataSource">
            <LayoutTemplate>
                <div id="itemPlaceholderContainer" runat="server">
                    <span id="itemPlaceholder" runat="server"></span>
                </div>
                <div style="margin-top: 12px;">
                </div>
            </LayoutTemplate>
            <EmptyDataTemplate>
                -
            </EmptyDataTemplate>
            <ItemTemplate>
                <div style="margin-top: 3px;">
                    <a style="" href='<%# Eval("Link") %>'>
                        <%# Eval("Path") %></a>
                    <br />
                    <span style="font-size: 8pt; color: #707060">
                        <%# Eval("Summary") %>
                    </span>
                </div>
            </ItemTemplate>
        </asp:ListView>
        <p style="word-spacing: 3px" class="style1">
            <asp:DataPager ID="dataPager" PagedControlID="listView" runat="server" PageSize="20">
                <Fields>
                    <asp:NextPreviousPagerField ShowFirstPageButton="True" ShowNextPageButton="False" ShowPreviousPageButton="False" RenderDisabledButtonsAsLabels="True" />
                    <asp:NumericPagerField />
                    <asp:NextPreviousPagerField ShowLastPageButton="True" ShowNextPageButton="False" ShowPreviousPageButton="False" RenderDisabledButtonsAsLabels="True" />
                </Fields>
            </asp:DataPager>                       
        </p>
        <asp:Button ID="DownloadResults" runat="server" Text="Download Results" onclick="DownloadResults_Click" />
        <asp:Button ID="DownloadTargets" runat="server" Text="Download Targets" onclick="DownloadTargets_Click" />
    </asp:Panel>
    <asp:ObjectDataSource ID="dataSource" runat="server" EnablePaging="True" SelectCountMethod="SelectCount" SelectMethod="Select" TypeName="App_Code.QueryApplicationIndex">
        <SelectParameters>
            <asp:ControlParameter Name="query" ControlID="query" PropertyName="Value" />
            <asp:ControlParameter Name="revFirst" ControlID="revFirst" PropertyName="Value" />
            <asp:ControlParameter Name="revLast" ControlID="revLast" PropertyName="Value" />
        </SelectParameters>
    </asp:ObjectDataSource>
    <asp:HiddenField ID="query" runat="server" />
    <asp:HiddenField ID="revFirst" runat="server" />
    <asp:HiddenField ID="revLast" runat="server" />
    </form>
</body>
</html>
