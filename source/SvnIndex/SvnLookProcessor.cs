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
using System.Threading;
using System.IO;

namespace SvnIndex
{
    public class SvnLookProcessor
    {
        readonly ProcessStartInfo psi;

        public SvnLookProcessor()
        {
            psi = new ProcessStartInfo();
            psi.RedirectStandardError = true;
            psi.RedirectStandardOutput = true;
            psi.UseShellExecute = false;
            psi.ErrorDialog = false;
            psi.CreateNoWindow = true;
            psi.FileName = "svnlook";
        }

        static Exception ProcessOutput(TextReader output, Action<string> process_line)
        {
            while (true)
            {
                string line = output.ReadLine();
                if (line == null) break;
                process_line(line);
            }
            return null;
        }

        /// <summary>
        /// Executes the given command synchronously
        /// </summary>
        public void Run(string arguments, Action<string> lineDelegate)
        {
            psi.Arguments = arguments;

            using (Process p = Process.Start(psi))
            {
                if (p == null) throw new Exception("cannot start svnlook");

                Exception exception = null;
                ManualResetEvent finished = new ManualResetEvent(false);
                ThreadPool.QueueUserWorkItem(delegate
                    {
                        exception = ProcessOutput(p.StandardOutput, lineDelegate);
                        finished.Set();
                    });

                string error = p.StandardError.ReadToEnd();
                finished.WaitOne();
                finished.Close();

                if (exception != null)
                    throw new Exception("exception while processing svnlook output", exception);

                if (p.ExitCode != 0)
                    throw new InvalidOperationException("svnlook " + arguments + " failed: " + error);
            }
        }
    }
}