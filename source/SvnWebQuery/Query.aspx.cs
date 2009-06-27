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
using System.Reflection;
using System.Text.RegularExpressions;
using System.Web.UI;
using System.Linq;
using App_Code;
using SvnQuery;

namespace SvnWebQuery
{
    public partial class Query : Page
    {
        static readonly string Version = Assembly.GetExecutingAssembly().GetName().Version.ToString();

        protected override void OnPreRender(EventArgs e)
        {
            base.OnPreRender(e);
            inputQuery.Text = query.Value;

            if (QueryApplicationIndex.IsSingleRevision)
            {
                _revisionUi.Visible = false;
            }
            else
            {
                if (_textRevision.Text == "$hidden$")
                {
                    _textRevision.Style[HtmlTextWriterStyle.Display] = "none";
                    _optGroup.Style[HtmlTextWriterStyle.Display] = "";
                }
                else
                {
                    _textRevision.Style[HtmlTextWriterStyle.Display] = "";
                    _optGroup.Style[HtmlTextWriterStyle.Display] = "none";
                }            
            }
            _repositoryLabel.Text = QueryApplicationIndex.Name;
            SvnQueryVersionLabel.Text = Version;
            Title = QueryApplicationIndex.Name + " Search";
        }

        protected void OnSearch(object sender, EventArgs e)
        {
            string queryText = inputQuery.Text.ToLowerInvariant().Replace('\\', '/');
            if (string.IsNullOrEmpty(queryText)) return;
            query.Value = queryText;

            try
            {
                Pair p = GetRevisionRange();
                revFirst.Value = p.First.ToString();
                revLast.Value = p.Second.ToString();

                QueryResult r = QueryApplicationIndex.Query(query.Value, revFirst.Value, revLast.Value);
                string htmlQuery = Server.HtmlEncode(query.Value);
                hitsLabel.Text = string.Format("<b>{0}</b> hits for <b>{1}</b>", r.HitCount, htmlQuery);
                statisticsLabel.Text =
                    string.Format("<span style='color:#808080'>{0} documents searched in {1}ms. Index revision {2}</span>",
                                  r.SearchCount, r.SearchTime, r.IndexRevision);

                dataPager.Visible = (dataPager.MaximumRows < r.HitCount);
                // Reset to page 0
                dataPager.SetPageProperties(0, dataPager.MaximumRows, true);
                resultsPanel.Visible = true;
                messsageLabel.Visible = false;
            }
            catch (Exception x)
            {
                messsageLabel.Text =
                    "An error occured. Most probably your query has some wildcards that lead to too many results. Try narrowing down your query.</br></br><b>Details: </b>" +
                    "<pre>" + x + "</pre>";
                resultsPanel.Visible = false;
                messsageLabel.Visible = true;
            }
        }

        Pair GetRevisionRange()
        {
            string text = _textRevision.Text.ToLowerInvariant();
            if (text == "$hidden$")
            {
                return new Pair
                       {
                           First = (_optHead.Checked ? RevisionFilter.Head : RevisionFilter.All),
                           Second = RevisionFilter.Head
                       };
            }

            Match m = Regex.Match(text, @"\d{1,8}");
            int first = -1;
            int last = -1;
            if (m.Success)
            {
                first = int.Parse(m.ToString());
                m = m.NextMatch();
                if (m.Success) last = int.Parse(m.ToString());
            }
            if (text.Contains("all"))
            {
                first = last = RevisionFilter.All;
                _textRevision.Text = "all";
            }
            else if (text.Contains("head") || first < 0)
            {
                last = RevisionFilter.Head;
                if (first < 0)
                {
                    first = last;
                    _textRevision.Text = "head";
                }
                else
                {
                    _textRevision.Text = first + " : head";
                }
            }
            else if (first > 0)
            {
                if (last > 0 && last < first)
                {
                    int swap = first;
                    first = last;
                    last = swap;
                }
                _textRevision.Text = first.ToString();
                if (last > 0) _textRevision.Text += " : " + last;
            }
            return new Pair(first, last > 0 ? last : first);
        }
    
        protected void DownloadResults_Click(object sender, EventArgs e)
        {
            Response.ContentType = "application/x-msdownload";
            string time = DateTime.Now.ToString("s").Replace(':', '-').Replace('T', '-');
            Response.AppendHeader("content-disposition", "attachment; filename=QueryResults_" + time + ".csv");

            Response.Write(Join("Path", "File", "Author", "Modified", "Revision", "Size"));
            foreach (Hit hit in QueryApplicationIndex.Query(query.Value, revFirst.Value, revLast.Value))
            {
                Response.Write(Join(hit.Path, hit.File, hit.Author, hit.LastModification, hit.RevFirst, hit.MaxSize.ToString()));
            }        
            Response.End();
        }

        static string Join(params string[] strings)
        {
            return string.Join(";", strings) + "\n";
        }

        protected void DownloadTargets_Click(object sender, EventArgs e)
        {
            Response.ContentType = "application/x-msdownload";
            string time = DateTime.Now.ToString("s").Replace(':', '-').Replace('T', '-');
            Response.AppendHeader("content-disposition", "attachment; filename=QueryResults_" + time + ".txt");

            foreach (Hit hit in QueryApplicationIndex.Query(query.Value, revFirst.Value, revLast.Value))
            {
                Response.Write(QueryApplicationIndex.ExternalUri + hit.Path + Environment.NewLine);
            }
            Response.End();
        }

    
    }
}