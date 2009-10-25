#region Apache License 2.0

// Copyright 2008-2009 Christian Rodemeyer
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using App_Code;
using SvnQuery;

namespace SvnWebQuery
{
    public partial class View : Page
    {
        static readonly ISvnApi Svn = new SharpSvnApi(QueryApplicationIndex.LocalUri);

        protected void Page_Load(object sender, EventArgs e)
        {
            Hit hit = QueryApplicationIndex.QueryId(Context.Request.QueryString["id"]);
            string path = hit.Path;
            int revision = hit.Revision;

            // getting _properties from the index
            Title = path.Substring(path.LastIndexOf('/') + 1);
            _header.InnerText = path;
            _author.InnerText = hit.Author;
            _modified.InnerText = hit.LastModification;
            if (hit.MaxSize > 0) _size.InnerText = hit.Size;
            else _sizeRow.Visible = false;
            _revisions.InnerText = hit.RevFirst + " - " + hit.RevLast;

            // getting _properties from subversion
            _message.InnerText = Svn.GetLogMessage(revision);

            if (path[0] == '$') return; // Revision Log

            bool binary = InitProperties(Svn.GetPathProperties(path, revision));

            if (hit.MaxSize > 512 * 1024)
            {
                _contentWarning.InnerText = "Content size is too big to display";
            }
            else if (binary)
            {
                _contentWarning.InnerText = "Content type is binary";
            }
            else if (hit.MaxSize > 0)
            {
                _content.InnerText = Svn.GetPathContent(path, revision, hit.MaxSize);

                var syntaxHighlighter = new SyntaxHighlightBrushMapper(path);
                if (syntaxHighlighter.IsAvailable)
                {
                    AddScriptInclude(syntaxHighlighter.GetScript());
                    AddStartupScript();
                   _content.Attributes.Add("class", syntaxHighlighter.GetClass());
                }
            }
        }

        void AddScriptInclude(string src)
        {
            var script = new HtmlGenericControl("script");
            script.Attributes.Add("type", "text/javascript");
            script.Attributes.Add("src", "scripts/" + src);
            Header.Controls.Add(script);
        }

        void AddStartupScript()
        {
            StringBuilder code = new StringBuilder();
            code.Append("SyntaxHighlighter.config.clipboardSwf = 'scripts/clipboard.swf';");
            code.Append("SyntaxHighlighter.defaults['gutter'] = true;");
            code.Append("SyntaxHighlighter.defaults['wrap-lines'] = false;");
            code.Append("SyntaxHighlighter.defaults['auto-links'] = false;");
            code.Append("SyntaxHighlighter.all();");

            var script = new HtmlGenericControl("script");
            script.Attributes.Add("type", "text/javascript");            
            script.InnerHtml = code.ToString();            
            Header.Controls.Add(script);
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

                _properties.Rows.Add(row);

                if (item.Key == "svn:mime-type" && !item.Value.StartsWith("text"))
                    binary = true;
            }
            return binary;
        }
    }
}