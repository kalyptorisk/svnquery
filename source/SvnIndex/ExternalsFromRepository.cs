#region Apache License 2.0

// Copyright 2008 Christian Rodemeyer
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//   http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#endregion

using System;
using System.Diagnostics;
using System.Text;

namespace SvnIndex
{
    public class ExternalsFromRepository : ExternalsTokenStream
    {
        readonly Process svnlook = new Process();
        readonly string propget;

        public ExternalsFromRepository(string repository)
        {
            svnlook.StartInfo.RedirectStandardError = true;
            svnlook.StartInfo.RedirectStandardOutput = true;
            svnlook.StartInfo.UseShellExecute = false;
            svnlook.StartInfo.ErrorDialog = false;
            svnlook.StartInfo.CreateNoWindow = true;
            svnlook.StartInfo.FileName = "svnlook";

            propget = string.Format("propget {0} svn:externals -r", repository);
        }

        public bool Reset(string path, string revision)
        {
            if (!path.EndsWith("/")) // only folders are allowed to have externals
            {
                SetReader(null);
                return false;
            }

            if (svnlook.StartInfo.Arguments != "")
            {
                svnlook.StandardOutput.ReadToEnd();
                svnlook.StandardError.ReadToEnd();
            }
            svnlook.StartInfo.Arguments = propget + revision + " \"" + path + "\"";
            svnlook.Start();
            SetReader(svnlook.StandardOutput);
            return true;
        }
    }
}