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
using System.IO;
using System.Reflection;
using System.Threading;

namespace SvnQuery
{
    static class Program
    {
        const string usage_msg =
            @"          
SvnIndex action index_path repository_url [Options] 
  action := create | update
  Options:
  -r max revision to be included in the index
  -u User
  -p Password
  -f regex filter for items that should be ignored, e.g. "".*/tags/.*""
  -t max number of threads used to query the repository in parallel
  -c commit interval
  -o optimize interval
  -n name of the index (for display in clients e.g. SvnWebQuery)
";

        static void Main(string[] args)
        {
#if !DEBUG
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
#endif

            Mutex mutex = null;
            try
            {
                var indexerArgs = new IndexerArgs(args);

                mutex = new Mutex(false, indexerArgs.IndexPath.ToLowerInvariant().Replace('\\', '_').Replace(':', '_'));
                try
                {
                    mutex.WaitOne();
                }
                catch (AbandonedMutexException)
                {
                    Console.WriteLine("Warning: Mutex was abandoned from another Process");
                }

                Indexer indexer = new Indexer(indexerArgs);
                if (indexerArgs.Command == Indexer.Command.Check)
                {
                    indexer.Check();
                }
                else
                {
                    indexer.Run();
                }
            }
            finally
            {
                if (mutex != null)
                {
                    mutex.ReleaseMutex();
                    mutex.Close();
                }
            }
#if DEBUG
            Console.WriteLine("Press any key");
            Console.ReadKey();
#endif
        }

        public static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception x = e.ExceptionObject as Exception;
            if (x != null)
            {
                Console.Error.WriteLine(x.Message);
                File.AppendAllText(
                    Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "crash.txt"),
                    Environment.CommandLine + Environment.NewLine + x);
            }
            Console.WriteLine(usage_msg);
        }
    }
}