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
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SvnFind.Views
{
    /// <summary>
    /// Interaction logic for MainView.xaml
    /// </summary>
    public partial class MainView : Window
    {
        public MainView()
        {
            InitializeComponent();

            ViewModel = new MainViewModel();
        }

        MainViewModel ViewModel
        {
            get { return (MainViewModel) DataContext;}
            set { DataContext = value;}
        }

        private void QueryText_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                QueryText.GetBindingExpression(TextBox.TextProperty).UpdateSource();
                Query_Click(null, null);
            }
        }

        private void Query_Click(object sender, RoutedEventArgs e)
        {
            DoActionWithWaitCursor(ViewModel.Query);            
        }

        private void HitList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            HitList.SelectedItem = null;
        }

        private void Head_Click(object sender, RoutedEventArgs e)
        {            
            RevisionRange.Text = "Head";
        }

        private void All_Click(object sender, RoutedEventArgs e)
        {
            RevisionRange.Text = "All";
        }

        private void SvnQuery_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("http://svnquery.tigris.org/");
        }

        private void Help_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("Help.htm");
        }

        private void HitItemLink_Click(object sender, RoutedEventArgs e)
        {
            DoActionWithWaitCursor(delegate
            {
                HitViewModel hitViewModel = (HitViewModel) ((FrameworkContentElement) e.Source).DataContext;
                ViewModel.QueryResult.OpenHit(hitViewModel);
            });
        }

        void DoActionWithWaitCursor(Action a)
        {
            try 
            {
                Cursor = Cursors.Wait;
                a(); 
            }
            finally
            {
                Cursor = null;
            }
        }

        private void RevisionRange_LostFocus(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke((Action)delegate
            {
                RevisionRange.GetBindingExpression(TextBox.TextProperty).UpdateTarget();
            });
        }

        private void RevisionRange_GotFocus(object sender, RoutedEventArgs e)
        {
            RevisionRange.SelectAll();
        }
    }
}