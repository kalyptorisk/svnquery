﻿#region Apache License 2.0

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
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.UI;
using System.Linq;
using SvnQuery;
using SvnWebQuery.Code;

namespace SvnWebQuery
{
    public partial class Query : Page
    {
        static readonly string Version = Assembly.GetExecutingAssembly().GetName().Version.ToString();

        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);

            ProcessQueryParameters();
            try
            {
                QueryResult r = QueryApplicationIndex.Query(_query.Value, _revFirst.Value, _revLast.Value);
                string htmlQuery = Server.HtmlEncode(_query.Value);
                _hitsLabel.Text = string.Format("<b>{0}</b> hits for <b>{1}</b>", r.HitCount, htmlQuery);
                _statisticsLabel.Text =
                    string.Format("<span style='color:#808080'>{0} documents searched in {1}ms. Index revision {2}</span>",
                                  r.SearchCount, r.SearchTime, r.IndexRevision);

                _dataPager.Visible = (_dataPager.MaximumRows < r.HitCount);
                // Reset to page 0
                _dataPager.SetPageProperties(0, _dataPager.MaximumRows, true);
                _resultsPanel.Visible = true;
                _messsageLabel.Visible = false;
            }
            catch (Exception x)
            {
                _messsageLabel.Text =
                    "An error occured. Most probably your _query has some wildcards that lead to too many results. Try narrowing down your _query.</br></br><b>Details: </b>" +
                    "<pre>" + x + "</pre>";
                _resultsPanel.Visible = false;
                _messsageLabel.Visible = true;
            }
        }

        void ProcessQueryParameters()
        {
            string query = Context.Request.QueryString["q"] ?? "";
            query = query.ToLowerInvariant().Replace('\\', '/');
            _inputQuery.Text = query;
            _query.Value = query;
            _revFirst.Value = Context.Request.QueryString["f"] ?? RevisionFilter.HeadString;
            _revLast.Value = Context.Request.QueryString["l"] ?? RevisionFilter.HeadString;

            _optAll.Checked = _revFirst.Value == "0" && _revLast.Value == RevisionFilter.HeadString;
            _optHead.Checked = _revFirst.Value == RevisionFilter.HeadString && _revLast.Value == RevisionFilter.HeadString;
                
            if (_optAll.Checked || _optHead.Checked)
            {
                _revision.Style[HtmlTextWriterStyle.Display] = "none";
                _revisionOptions.Style[HtmlTextWriterStyle.Display] = "";
                _revision.Text = "$hidden$";

                _optAll.Checked = _revFirst.Value == "0" && _revLast.Value == RevisionFilter.HeadString;
                _optHead.Checked = _revFirst.Value == RevisionFilter.HeadString && _revLast.Value == RevisionFilter.HeadString;
            }
            else
            {
                _revision.Style[HtmlTextWriterStyle.Display] = "";
                _revisionOptions.Style[HtmlTextWriterStyle.Display] = "none";

                StringBuilder sb = new StringBuilder();
                sb.Append(GetNormalizedRevision(_revFirst.Value));
                if (_revLast.Value != _revFirst.Value)
                {
                    sb.Append(" : ");
                    sb.Append(GetNormalizedRevision(_revLast.Value));                    
                }
                _revision.Text = sb.ToString();
            }
        }

        static string GetNormalizedRevision(string revision)
        {
            return revision == RevisionFilter.HeadString ? "head" : revision;
        }

        protected override void OnPreRender(EventArgs e)
        {
            base.OnPreRender(e);

            revisionContainer.Visible = !QueryApplicationIndex.IsSingleRevision;
            Title = QueryApplicationIndex.Name + " Search";
            _repositoryLabel.Text = Title;
            _version.Text = Version;
        }

        protected void OnSearch(object sender, EventArgs e)
        {
            string queryText = _inputQuery.Text.ToLowerInvariant().Replace('\\', '/');
            if (string.IsNullOrEmpty(queryText)) return;

            RevisionRange rr = GetRevisionRange();
            StringBuilder redirect = new StringBuilder();
            redirect.Append("Query.aspx");
            redirect.Append("?q=");
            redirect.Append(HttpUtility.UrlEncode(queryText));
            if (rr.First != RevisionFilter.Head)
            {
                redirect.Append("&f=");
                redirect.Append(rr.First);
            }
            if (rr.Last != RevisionFilter.Head)
            {
                redirect.Append("&l=");
                redirect.Append(rr.Last);
            }
            Context.Response.Redirect(redirect.ToString());
        }

        struct RevisionRange
        {
            public int First;
            public int Last;
        }

        RevisionRange GetRevisionRange()
        {
            string text = _revision.Text.ToLowerInvariant();
            if (text == "$hidden$")
            {
                return new RevisionRange
                       {
                           First = (_optHead.Checked ? RevisionFilter.Head : RevisionFilter.All),
                           Last = RevisionFilter.Head
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
                _revision.Text = "all";
            }
            else if (text.Contains("head") || first < 0)
            {
                last = RevisionFilter.Head;
                if (first < 0)
                {
                    first = last;
                    _revision.Text = "head";
                }
                else
                {
                    _revision.Text = first + " : head";
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
                _revision.Text = first.ToString();
                if (last > 0) _revision.Text += " : " + last;
            }
            return new RevisionRange{First = first, Last = last > 0 ? last : first};
        }
    
        protected void DownloadResults_Click(object sender, EventArgs e)
        {
            Response.ContentType = "application/x-msdownload";
            string time = DateTime.Now.ToString("s").Replace(':', '-').Replace('T', '-');
            Response.AppendHeader("content-disposition", "attachment; filename=QueryResults_" + time + ".csv");

            Response.Write(Join("Path", "File", "Author", "Modified", "Revision", "Size"));
            foreach (Hit hit in QueryApplicationIndex.Query(_query.Value, _revFirst.Value, _revLast.Value))
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

            foreach (Hit hit in QueryApplicationIndex.Query(_query.Value, _revFirst.Value, _revLast.Value))
            {
                Response.Write(QueryApplicationIndex.ExternalUri + hit.Path + Environment.NewLine);
            }
            Response.End();
        }

    
    }
}