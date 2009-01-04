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
using System.Diagnostics;
using System.Text;
using System.Xml;

namespace SvnQuery
{
    class SvnPathInfo
    {
        internal string id;
        internal string path;
        internal int revision;
        internal string author;
        internal int size;
        internal DateTime timestamp;
    }

    class SvnPathInfoReader
    {
        readonly Process svn = new Process();
        readonly XmlDocument xml = new XmlDocument();
        readonly StringBuilder arg = new StringBuilder();
        readonly string uriRepository;
        readonly string history;

        public SvnPathInfoReader(string repository)
        {
            uriRepository = "file:///" + repository.Replace("\\", "/");
            history = "history " + repository + " \"";

            svn.StartInfo.RedirectStandardError = true;
            svn.StartInfo.RedirectStandardOutput = true;
            svn.StartInfo.UseShellExecute = false;
            svn.StartInfo.ErrorDialog = false;
            svn.StartInfo.CreateNoWindow = true;
        }

        public SvnPathInfo ReadPathInfo(string path, int revision)
        {
            SvnPathInfo info = new SvnPathInfo();

            svn.StartInfo.FileName = "svnlook";
            svn.StartInfo.Arguments = history + path + "\" -l1 -r" + revision;
            svn.Start();
            svn.StandardOutput.ReadLine();
            svn.StandardOutput.ReadLine();
            string l = svn.StandardOutput.ReadLine();
            svn.StandardOutput.ReadToEnd();
            svn.WaitForExit();
            if (svn.ExitCode != 0) return null;
            if (!int.TryParse(l.Substring(0, 8), out info.revision)) return null;
            info.id = path + "@" + info.revision;
            info.path = path;

            // This code is necessary because there is no way to get the filesize via
            // svnlook. To make things even more difficult, you need to use peg revision (@)
            // with svn list for files because otherwise it seems to ignore -r
            arg.Length = 0;
            arg.Append(path[path.Length - 1] == '/' ? "info" : "list");
            arg.Append(" --xml ");
            arg.Append(uriRepository);
            foreach (string part in path.Split('/')) // Build an escaped Uri in the way svn likes it
            {
                arg.Append(Uri.EscapeDataString(part));
                arg.Append('/');
            }
            arg.Length -= 1;
                // one slash too much, but this is more readable and efficienter than two more if clauses inside the foreach
            arg.Append('@');
            arg.Append(revision);

            svn.StartInfo.FileName = "svn";
            svn.StartInfo.Arguments = arg.ToString();
            svn.Start();
            string result = svn.StandardOutput.ReadToEnd();
            svn.WaitForExit();
            if (svn.ExitCode != 0)
            {
                string error = svn.StandardError.ReadToEnd();
                Debug.WriteLine(error);
                if (!error.Contains("URI-encoded")) return null;
            }

            xml.LoadXml(result);
            info.author = xml.GetElementsByTagName("author")[0].InnerText.ToLowerInvariant();
            info.timestamp =
                XmlConvert.ToDateTime(xml.GetElementsByTagName("date")[0].InnerText, XmlDateTimeSerializationMode.Utc).
                    ToLocalTime();
            XmlNodeList nl = xml.GetElementsByTagName("size");
            info.size = (nl.Count == 0) ? -1 : XmlConvert.ToInt32(nl[0].InnerText);
            return info;
        }
    }
}