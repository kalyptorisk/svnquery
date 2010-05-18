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
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using System.Xml;

namespace SvnWebQueryDemo
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                string webserver = FindWebServer();
                string currentDir = GetCurrentDir(args);
                StartSvnWebQuery(webserver, currentDir);
            }
            catch (Exception x)
            {
                MessageBox.Show(Dump.Exception(x), "SvnWebQueryDemo", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        static string GetCurrentDir(string[] args)
        {
            if (args.Length == 1 && Directory.Exists(args[0])) 
                return args[0];

            return Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
        }

        static void StartSvnWebQuery(string webserver, string currentDir)
        {
            // Set the 'IndexPath' appsetting to the absolute path to the subversion index
            string webconfig = Path.Combine(currentDir, @"SvnWebQuery\web.config");
            XmlDocument xml = new XmlDocument();
            xml.Load(webconfig);
            var n = xml.SelectSingleNode("//appSettings/add[@key='IndexPath']");
            n.Attributes["value"].Value = Path.Combine(currentDir, "IndexData");
            xml.Save(webconfig);

            if (IsWebServerRunning(webserver))
            {
                throw new Exception("ASP.NET Development Server already started. Please stop it and try again.");
            }

            int port = 9000 + currentDir.GetHashCode() % 1000;
            Process p = Process.Start(webserver, "/port:" + port + " /path:\"" + Path.Combine(currentDir, "SvnWebQuery") + '"');
            if (p != null)
            {
                p.WaitForInputIdle();
            }
            Thread.Sleep(500); // give the web server a chance to finish startup phase
            Process.Start("http://localhost:" + port + "/Query.aspx");
        }

        static string FindWebServer()
        {
            const string webserver = "WebDev.WebServer.exe";
            const string webserver20 = "WebDev.WebServer20.exe";
            string path;

            // Try VS2005 version
            path = Path.GetDirectoryName(RuntimeEnvironment.GetRuntimeDirectory());
            path = Path.Combine(path, webserver);
            if (File.Exists(path)) return path;

            // Try VS2008 version
            path = Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFiles);
            path = Path.Combine(path, @"microsoft shared\DevServer\9.0\" + webserver);
            if (File.Exists(path)) return path;

            // Try VS2010 version
            path = Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFiles);
            path = Path.Combine(path, @"microsoft shared\DevServer\10.0\" + webserver20);
            if (File.Exists(path)) return path;

            throw new Exception("ASP.NET Development Server (WebDev.WebServer.exe) not found");
        }

        static bool IsWebServerRunning(string webserver)
        {
            foreach (var p in Process.GetProcessesByName(Path.GetFileNameWithoutExtension(webserver)))
            {
                if (Path.GetDirectoryName(p.MainModule.FileName).Equals(webserver, StringComparison.CurrentCultureIgnoreCase))
                    return true;
            }
            return false;
        }
    }
}