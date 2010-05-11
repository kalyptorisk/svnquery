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
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using SvnFind.Diagnostics;
using SvnQuery;
using SvnQuery.Svn;
using IOPath = System.IO.Path;
using IOFile = System.IO.File;

namespace SvnFind
{
    public class HitViewModel
    {
        readonly Hit _hit;

        public HitViewModel(Hit hit)
        {
            _hit = hit;
        }

        public string Path
        {
            get { return _hit.Path; }
        }

        public string File
        {
            get { return _hit.File; }
        }

        public string Folder
        {
            get { return _hit.Folder; }
        }

        public string Author
        {
            get { return _hit.Author; }
        }

        public string LastModified
        {
            get { return _hit.LastModification.ToShortDateString() + " " + _hit.LastModification.ToShortTimeString(); }
        }

        public int Revision
        {
            get { return _hit.Revision; }
        }

        public string RevisionRange
        {
            get { return _hit.RevisionFirst + ":" + _hit.RevisionLast; }
        }

        public string Size
        {
            get { return _hit.Size; }
        }

        public int SizeInBytes
        {
            get { return _hit.SizeInBytes; }
        }

        public void ShowContent(ISvnApi svn)
        {
            if (Path[0] == '$')
            {
                ShowLogMessage(svn);
                return;
            }

            try
            {
                string path = GetTempPath();
                IOFile.WriteAllText(path, svn.GetPathContent(Path, Revision, SizeInBytes));
                Process.Start(path);
                Thread.Sleep(500); // starting the viewer application could take a while, therefore we display the wait cursor for at least half a second
            }
            catch (Exception x)
            {
                MessageBox.Show(Dump.ExceptionMessage(x), "Could not open file", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        public void ShowLogMessage(ISvnApi svn)
        {
            try
            {
                MessageBox.Show(svn.GetLogMessage(Revision), "Log Message");
            }
            catch (Exception x)
            {
                MessageBox.Show(Dump.ExceptionMessage(x), "Could not get log message", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        static HitViewModel()
        {
            Temp = IOPath.Combine(IOPath.GetTempPath(), "SvnFind");
            if (!Directory.Exists(Temp)) Directory.CreateDirectory(Temp);
        }

        static readonly string Temp;

        string GetTempPath()
        {
            string foldername = "";
            bool separator = false;
            foreach (char c in Folder)
            {
                if (separator && c != '/')
                {
                    foldername += c;
                    separator = false;
                }
                else if (c == '/') separator = true;
            }
            foldername = IOPath.Combine(Temp, foldername);
            if (!Directory.Exists(foldername)) Directory.CreateDirectory(foldername);
            string filename = IOPath.GetFileNameWithoutExtension(File) + "@" + Revision + IOPath.GetExtension(File);

            return IOPath.Combine(foldername, filename);
        }

        public override string ToString()
        {
            return _hit.ToString();
        }
    }
}