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
using System.Threading;

namespace SvnIndex
{
    static class Program
    {

        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            Mutex mutex = null;
            try
            {
                IndexerArgs indexerArgs = new IndexerArgs(args);
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
                indexer.Run();
            }
            catch (IndexerArgsException x)
            {
                Console.WriteLine(x.Message);
            }
            finally
            {
                if (mutex != null)
                {
                    mutex.ReleaseMutex();
                    mutex.Close();
                }
            }            
            AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;
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
                Console.WriteLine("ERROR");
                Console.Error.WriteLine(x);  
            }
            Environment.Exit(-1);
        }
    }
}