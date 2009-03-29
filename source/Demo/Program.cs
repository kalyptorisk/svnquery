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
using System.IO;
using System.Threading;
using System.Xml;

namespace SvnQueryDemo
{
    static class Program
    {
        static readonly string currentDir = Environment.CurrentDirectory;
        static readonly string webserverFolder = Path.GetDirectoryName(System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory());
        const string webserver = "WebDev.WebServer";

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Set the 'IndexPath' appsetting to the absolute path to the subversion index
            const string webconfig = @"SvnWebQuery\web.config";
            XmlDocument xml = new XmlDocument();
            xml.Load(webconfig);
            var n = xml.SelectSingleNode("//appSettings/add[@key='IndexPath']");
            n.Attributes["value"].Value = Path.Combine(currentDir, "IndexData");
            xml.Save(webconfig);

            int port = 9000 + currentDir.GetHashCode() % 1000;

            if (!IsWebServerRunning())
            {
                string webserverPath = Path.Combine(webserverFolder, webserver + ".exe");
                Process p = Process.Start(webserverPath, "/port:" + port + " /path:\"" + Path.Combine(currentDir, "SvnWebQuery") + '"');
                if (p != null)
                {
                    p.WaitForInputIdle();
                    if (p.HasExited) return;
                }
            }
            Thread.Sleep(500); // give the web server a chance to finish startup phase
            if (IsWebServerRunning())
               Process.Start("http://localhost:" + port + "/Query.aspx");
        }

        static bool IsWebServerRunning()
        {
            foreach (var p in Process.GetProcessesByName(webserver))
            {
               if (Path.GetDirectoryName(p.MainModule.FileName) == webserverFolder)
                   return true;
            }
            return false;
        }
    }
}