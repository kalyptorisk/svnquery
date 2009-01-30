using System;
using System.Collections;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Xml.Linq;
using SharpSvn;
using System.IO;
using SvnQuery;

public partial class ViewContent : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        string path = Context.Request.QueryString["path"];
        lblPath.Text = path;
        content.InnerText = "<b>hullebulle</b>";

        ISvnApi svn = new SharpSvnApi("file:///C:/Workspaces/_external/SvnQuery/test_repository/");
        
        content.InnerText = svn.GetPathContent("Folder/text.txt", 21, 1000);

        
    }
}
