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
using System.Linq;
using System.Windows.Input;
using SvnQuery;
using System.Windows;

namespace SvnFind
{
    public class MainViewModel: ViewModelBase
    {
        public MainViewModel()
        {
            Indices = new ObservableCollection<Index>();
            Indices.Add(new Index(@"\\moria\DavidIndex"));
            Indices.Add(new Index(@"\\moria\SodaIndex"));
            SelectedIndex = Indices[0];
            QueryText = "";
        }

        public string QueryText { get; set; }

        public ICommand QueryCommand { get; private set; }

        public ObservableCollection<Index> Indices { get; private set;}

        public Index SelectedIndex
        {
            get { return _selectedIndex;} 
            set 
            { 
                _selectedIndex = value; 
                OnPropertyChanged(() => SelectedIndex); 
                OnPropertyChanged(() => RevisionRangeVisibility);
            }
        }
        Index _selectedIndex;

        public Visibility RevisionRangeVisibility
        {
            get { return SelectedIndex.IsSingleRevision ? Visibility.Visible : Visibility.Hidden; }
        }

        public ResultViewModel QueryResult
        {
            get { return _queryResult; }
            set { _queryResult = value; OnPropertyChanged(() => QueryResult); }
        }
        ResultViewModel _queryResult;

        public void Query()
        {            
            QueryResult = new ResultViewModel(SelectedIndex.Query(QueryText));
        }


    }
}