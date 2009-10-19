<%@ Page Language="C#" AutoEventWireup="true" Inherits="SvnWebQuery.Query" ValidateRequest="false" Codebehind="Query.aspx.cs" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.1//EN" "http://www.w3.org/TR/xhtml11/DTD/xhtml11.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
<title>Repository Search</title>
<script type="text/javascript" src="Query.js"></script>
<link href="styles.css" rel="stylesheet" type="text/css" />
</head>

<body>
    <form runat="server" defaultbutton="_btnSearch" defaultfocus="txQuery">  
    <div id="headerContainer">
	  <div id="title">	     
       <asp:Label ID="_repositoryLabel" runat="server">Repository Search</asp:Label> 
    </div> 
	  <div id="queryContainer"> 
	    <asp:TextBox ID="_inputQuery" runat="server" />
  	  <asp:Button ID="_btnSearch" runat="server" OnClick="OnSearch" Text="Search" />
  	  <a href="Help.htm">Help</a>
	  </div>	  
  	<div id="revisionContainer" runat="server">
  	    <div id="revisionToggle"> 
  	      <a href="javascript:toggleRevisionRange()" >Revision:</a>
  	      </div>  	
        <div id="_revisionRange">
          <asp:TextBox id="_revision" runat="server" Text="$hidden$"  ToolTip="Enter a revision or a revision range" />
          </div>
        <div id="_revisionOptions" runat="server">
          <asp:RadioButton ID="_optHead" runat="server" Text="Head" GroupName="revOptions" Checked="True" ToolTip="Search only in head revision" />
          <asp:RadioButton ID="_optAll" runat="server" Text="All"  GroupName="revOptions" ToolTip="Search in all revisions" />
        </div>
	  </div>
	</div>

	<div id="poweredByContainer">	  
	  <div id="powered">powered by</div>
	  <div id="svnquery"><a href="http://svnquery.tigris.org" style="color:inherit">SvnQuery</a></div>
	  <div id="version"><asp:Label ID="_version" runat="server">0.0.0.0</asp:Label></div>
  </div>

  <div id="content">
    <asp:Label ID="_messsageLabel" runat="server" />
    <asp:Panel ID="_resultsPanel" runat="server" Visible="False">
        <div id="resultsSummary" >
                <div style="float:left;">
                    <asp:Label ID="hitsLabel" runat="server" Text="<b>123 hits</b> for bli bla blub"/>
                </div>
                <div style="text-align:right;float:right;color:#808080">
                    <asp:Label ID="statisticsLabel" runat="server" Text="(23440 documents searched in 789ms)" />
                </div>
        </div>
        <asp:ListView ID="listView" runat="server" DataSourceID="dataSource">
            <LayoutTemplate>
                <div id="itemPlaceholderContainer" runat="server">
                   <div id="itemPlaceholder" runat="server"></div>
                </div>               
            </LayoutTemplate>
            <EmptyDataTemplate>
                -
            </EmptyDataTemplate>
            <ItemTemplate>
                <p class="hit">
                    <a href='<%# Eval("Link") %>'>
                        <%# Eval("Path") %></a>
                    <br />
                    <span class="summary">
                        <%# Eval("Summary") %>
                    </span>
                </p>
            </ItemTemplate>
        </asp:ListView>
        <p>
            <asp:DataPager ID="_dataPager" PagedControlID="listView" runat="server" PageSize="20">
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
    </div>
    </form>
</body>
</html>
