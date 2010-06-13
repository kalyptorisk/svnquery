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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Forms;
using SvnFind.Diagnostics;
using SvnFind.Properties;
using SvnQuery;
using MessageBox=System.Windows.MessageBox;

namespace SvnFind
{
    public class MainViewModel : ViewModelBase
    {
        public MainViewModel() : this(RepositoriesFromAppConfig)
        {
            Settings.Default.Repositories.Clear();
            foreach (var index in Indices)
            {
                Settings.Default.Repositories.Add(index.Path);
            }
            Settings.Default.Save();
        }

        static IEnumerable<string> RepositoriesFromAppConfig
        {
            get
            {
                foreach (string index in Settings.Default.Repositories)
                    yield return index;
            }
        }

        MainViewModel(IEnumerable<string> indexPathList)
        {
            var indices = indexPathList.Select(path => OpenIndexWithoutException(path)).Where(i => i != null);
            Indices = new ObservableCollection<Index>(indices);
            if (Indices.Count > 0) SelectedIndex = Indices[0];
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
                                      @"D:\Repositories\SvnQuery\index",
                                  };

                    var model = new MainViewModel(indices);
                    //model.RevisionRange = "All";
                    //model.QueryText = "bla";
                    //model.Query();
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
                if (value == _selectedIndex) return;
                _selectedIndex = value;
                QueryResult = null;

                int i = Indices.IndexOf(value);
                if (i > 0) UpdateMostRecentUsedIndex(i);

                OnPropertyChanged(() => SelectedIndex);
                OnPropertyChanged(() => RevisionRangeVisibility);
                OnPropertyChanged(() => CanQuery);
                OnPropertyChanged(() => Title);
            }
        }

        Index _selectedIndex;

        public string Title
        {
            get { return "SvnFind" + (SelectedIndex == null ? "" : (" - " + SelectedIndex.Name)); }
        }

        public string Version
        {
            get { return Assembly.GetExecutingAssembly().GetName().Version.ToString(); }
        }

        public Visibility RevisionRangeVisibility
        {
            get
            {
                return SelectedIndex != null && SelectedIndex.IsSingleRevision
                           ? Visibility.Hidden
                           : Visibility.Visible;
            }
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

        public bool CanQuery
        {
            get { return SelectedIndex != null; }
        }

        public void Query()
        {
            try
            {
                string first, last;
                GetRevisionRange(RevisionRange, out first, out last);
                QueryResult = new ResultViewModel(SelectedIndex.Query(QueryText, first, last));
            }
            catch (Exception x)
            {
                MessageBox.Show(Dump.ExceptionMessage(x), "Search failed", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
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

        public void OpenIndex()
        {
            var dlg = new FolderBrowserDialog();
            dlg.RootFolder = Environment.SpecialFolder.Desktop; // to enable network browsing
            dlg.ShowNewFolderButton = false;
            dlg.Description = "Select the folder that contains the index for an repository.";

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                string path = dlg.SelectedPath;
                if (TryOpenIndex(path) || TryOpenIndex(Path.Combine(path, "index")))
                {
                    SelectedIndex = Indices[0];
                }
                else
                {
                    MessageBox.Show("Could not open index: \n\"" + path + "\"", "SvnFind", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        bool TryOpenIndex(string path)
        {
            int i = Settings.Default.Repositories.IndexOf(path);
            if (i < 0)
            {
                var index = OpenIndexWithoutException(path);
                if (index == null) return false;
                Settings.Default.Repositories.Insert(0, path);
                Settings.Default.Save();
                Indices.Insert(0, index);
            }
            else if (i > 0)
            {
                UpdateMostRecentUsedIndex(i);
            }
            return true;
        }

        void UpdateMostRecentUsedIndex(int i)
        {
            string path = Settings.Default.Repositories[i];
            Settings.Default.Repositories.RemoveAt(i);     
            Settings.Default.Repositories.Insert(0, path);
            Settings.Default.Save();
            Indices.Move(i, 0);   
        }

        static Index OpenIndexWithoutException(string path)
        {
            try
            {
                return new Index(path);
            }
            catch (Exception x)
            {
                Debug.WriteLine(Dump.ExceptionMessage(x));
                return null;
            }
        }
    }
}