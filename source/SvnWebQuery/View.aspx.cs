using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web.UI.HtmlControls;
using App_Code;
using SvnQuery;
using System.Text.RegularExpressions;
                                
public partial class View : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        Hit hit = QueryApplicationIndex.QueryId(Context.Request.QueryString["id"]);
        string path = hit.Path;
        int revision = hit.Revision;
        
        // getting properties from the index
        Title = path.Substring(path.LastIndexOf('/') + 1);
        header.InnerText = path;
        author.InnerText = hit.Author;
        modified.InnerText = hit.LastModification;
        if (hit.MaxSize > 0) size.InnerText = hit.Size;
        else sizeRow.Visible = false;
        revisions.InnerText = hit.RevFirst + " - " + hit.RevLast;

        // getting properties from subversion
        ISvnApi svn = (ISvnApi)Application["SvnApi"];
       
        message.InnerText = svn.GetLogMessage(revision);

        if (path[0] == '$') return; // Revision Log

        bool binary = InitProperties(svn.GetPathProperties(path, revision));

        if (hit.MaxSize > 1024 * 1024)
            contentWarning.InnerText = "Content size is too big to display";
        else if (binary)
            contentWarning.InnerText = "Content type is binary";
        else if (hit.MaxSize > 0)
        {
            content.Attributes.Add("class", GetClass(path));
            content.InnerText = svn.GetPathContent(path, revision, hit.MaxSize);
        }
    }

    static readonly string[,] types = {
                                            {"(.*ml)|(.*proj)|(targets)", "xml"},
                                            {"cs", "csharp"},
                                            {"(h)|(hpp)|(cpp)|(inl)", "cpp"},
                                            {"js", "js"},
                                            {"py", "python"}
                                       };


    /// <summary>
    /// gets the syntax highlighting class fromt the extension of the file
    /// </summary>
    static string GetClass(string path)
    {
        string ext = path.Substring(path.LastIndexOf('.') + 1);

        for (int i = 0; i < types.GetUpperBound(0); ++i)
        {
            if (Regex.IsMatch(ext, "^" +types[i, 0] + "$", RegexOptions.IgnoreCase))
                return "brush: " + types[i, 1] + ";";
        }
        return "";
    }

    /// <summary>
    /// returns true if the mime type is not binary
    /// </summary>
    /// <param name="props"></param>
    /// <returns></returns>
    bool InitProperties(IDictionary<string, string> props)
    {
        bool binary = false;
        foreach (var item in props)
        {
            var key = new HtmlTableCell();
            key.VAlign = "top";
            key.InnerText = item.Key + ":";

            var value = new HtmlTableCell();
            value.InnerText = item.Value;

            var row = new HtmlTableRow();
            row.Cells.Add(key);
            row.Cells.Add(value);

            properties.Rows.Add(row);

            if (item.Key == "svn:mime-type" && !item.Value.StartsWith("text"))
                binary = true;
        }
        return binary;
    }

}
