#region Apache License 2.0

// Copyright 2008-2010 Christian Rodemeyer
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
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Xml.Linq;
using SvnQuery;

namespace SvnFind
{
    public class MainViewModel : ViewModelBase
    {
        public MainViewModel() : this(RepositoriesFromAppConfig)
        {
            QueryText = "$Revision 8710";
        }

        static IEnumerable<Index> RepositoriesFromAppConfig
        {
            get
            {
                XElement repositories = XmlConfiguration.GetSection("repositories");
                return from r in repositories.Elements("repository")
                       select new Index(r.Attribute("index").Value);
            }
        }

        MainViewModel(IEnumerable<Index> indices)
        {
            Indices = new ObservableCollection<Index>(indices);
            if (Indices.Count == 0)
            {
                MessageBox.Show("No repository configured, please check your config file.", "SvnFind Configuration Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(-1);
            }
            SelectedIndex = Indices[0];
            QueryText = "";
            RevisionRange = "Head";
        }

        public static MainViewModel Instance
        {
            get
            {
#if DEBUG
                if (IsDesignTime)
                {
                    var indices = new[]
                                  {
                                      new Index(@"C:\_Entwicklung\SvnQuery\SvnQueryDemos\SvnQueryDemo_Subversion\IndexData"),
                                  };

                    var model = new MainViewModel(indices);
                    model.RevisionRange = "All";
                    model.QueryText = "bla";
                    model.Query();
                    return model;
                }
#endif
                return new MainViewModel();
            }
        }

        public string QueryText { get; set; }

        public string RevisionRange
        {
            get { return _revisionRange; }
            set
            {
                string first, last;
                _revisionRange = GetRevisionRange(value, out first, out last);
                OnPropertyChanged(() => RevisionRange);
            }
        }

        string _revisionRange;

        public ObservableCollection<Index> Indices { get; private set; }

        public Index SelectedIndex
        {
            get { return _selectedIndex; }
            set
            {
                Debug.Assert(value != null);
                _selectedIndex = value;
                OnPropertyChanged(() => SelectedIndex);
                OnPropertyChanged(() => RevisionRangeVisibility);
            }
        }

        Index _selectedIndex;

        public Visibility RevisionRangeVisibility
        {
            get { return SelectedIndex.IsSingleRevision ? Visibility.Hidden : Visibility.Visible; }
        }

        public ResultViewModel QueryResult
        {
            get { return _queryResult; }
            set
            {
                _queryResult = value;
                OnPropertyChanged(() => QueryResult);
            }
        }

        ResultViewModel _queryResult;

        public void Query()
        {
            string first, last;
            GetRevisionRange(RevisionRange, out first, out last);
            QueryResult = new ResultViewModel(SelectedIndex.Query(QueryText, first, last));
        }

        /// <summary>
        /// Validates and normalized the RevisionRange representation. As a side effect also
        /// deliverst the first and last revision of the range in a form suitable for the query api.
        /// </summary>
        static string GetRevisionRange(string revisionRange, out string first, out string last)
        {
            string text = revisionRange.ToLowerInvariant();

            if (text.Contains("all"))
            {
                first = last = Revision.AllString;
                return "All";
            }

            Match m = Regex.Match(text, @"\d{1,8}");
            if (m.Success)
            {
                int nfirst = int.Parse(m.ToString());
                m = m.NextMatch();
                if (m.Success)
                {
                    int nlast = int.Parse(m.ToString());
                    if (nfirst < nlast)
                    {
                        first = nfirst.ToString();
                        last = nlast.ToString();
                    }
                    else
                    {
                        first = nlast.ToString();
                        last = nfirst.ToString();
                    }
                    return first + " : " + last;
                }
                if (text.Contains("head"))
                {
                    first = nfirst.ToString();
                    last = Revision.HeadString;
                    return first + " : Head";
                }
                return first = last = nfirst.ToString();
            }
            first = last = Revision.HeadString;
            return "Head";
        }
    }
}