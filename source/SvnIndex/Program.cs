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
using System.IO;
using System.Reflection;
using System.Threading;

namespace SvnQuery
{
    static class Program
    {

        const string usage_msg = "SvnQuery action index_path repository_path [revision] \r\n  action := create | update \r\n";

        static void Main(string[] args)
        {
            Mutex mutex = null;
            try
            {                
                if (args.Length < 3) throw new Exception(usage_msg);
                string action = args[0].ToLowerInvariant();
                string index = Path.GetFullPath(args[1]).ToLowerInvariant();
                string repository = Path.GetFullPath(args[2]);
                string revision = (args.Length < 4) ? null : args[3];

                mutex = new Mutex(false, index.Replace('\\', '_').Replace(':', '_'));
                try
                {                    
                    mutex.WaitOne();
                }
                catch (AbandonedMutexException)
                {
                    Console.WriteLine("Warning: Mutex was abandoned from another Process");
                }

                Indexer indexer = new Indexer(index, repository, revision);
                if (action == "create")
                {
                    indexer.CreateIndex();                    
                }
                else if (action == "update")
                {
                    indexer.UpdateIndex();
                }
#if DEBUG
                else if (action == "debug")
                {
                    
                    //Console.WriteLine("MaxRevision:" + MaxIndexRevision.Get(index));

                    indexer.UpdateIndex(); 
                    Console.WriteLine("press any key");
                    Console.ReadKey();
                }
#endif
                else throw new Exception(usage_msg);                
            }
#if !DEBUG
            catch (Exception x)
            {
                Console.Error.WriteLine(x);
                File.AppendAllText(
                    Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "crash.txt"),
                    Environment.CommandLine + Environment.NewLine + x);
            }
#endif
            finally
            {
                if (mutex != null)
                {
                    mutex.ReleaseMutex();
                    mutex.Close();
                }
            }
        }


    }
}