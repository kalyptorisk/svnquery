using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Microsoft.Win32;

namespace SvnFind.Diagnostics
{
    /// <summary>
    /// Provides static methods for dumping various kinds of information which
    /// might help in debugging application crashes.
    /// </summary>
    public static class Dump
    {
        public static void All(TextWriter w, Exception x)
        {
            Exception(w, x);
            Environment(w);
            Assemblies(w);
            Dlls(w);
        }

        public static string All(Exception x)
        {
            return ToString(w => All(w, x));
        }

        public static void Exception(TextWriter w, Exception x)
        {
            if (x == null) return;
            Exception(w, x.InnerException);

            w.WriteLine("--- " + x.GetType().FullName + " ---");
            w.WriteLine(x.Message);
            w.WriteLine(x.StackTrace);
            w.WriteLine("==================");
            w.WriteLine();
        }

        public static string Exception(Exception x)
        {
            return ToString(w => Exception(w, x));
        }

        public static string ExceptionMessage(Exception x)
        {
            if (x.InnerException == null)
                return x.Message;
            return ExceptionMessage(x.InnerException) + System.Environment.NewLine + x.Message;
        }

        public static void Assemblies(TextWriter w)
        {
            // Sorted list of running assemblies
            WriteHeader(w, "Loaded assemblies:");
            List<string> assemblyNames = new List<string>();
            foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
            {
                assemblyNames.Add(a.FullName);
            }
            assemblyNames.Sort();
            List<string[]> assemblies = new List<string[]>();
            foreach (string fullName in assemblyNames)
            {
                assemblies.Add(fullName.Split(','));
            }
            WriteTable(w, assemblies);
        }

        public static string Assemblies()
        {
            return ToString(Assemblies);
        }

        public static void Dlls(TextWriter w)
        {
            WriteHeader(w, "Loaded dlls:");
            List<string> modules = new List<string>();
            foreach (ProcessModule module in Process.GetCurrentProcess().Modules)
            {
                modules.Add(module.FileName + " (" + module.FileVersionInfo.FileVersion + ")");
            }
            modules.Sort();
            foreach (string s in modules)
            {
                w.WriteLine(s);
            }
        }

        public static string Dlls()
        {
            return ToString(Dlls);
        }

        public static void Processes(TextWriter w)
        {
            WriteHeader(w, "Running processes:");
            foreach (Process p in Process.GetProcesses())
            {
                w.WriteLine(p.ProcessName + " from " + p.MainModule.FileName + "(" + p.MainModule.FileVersionInfo + ")");
                p.Dispose();
            }
        }

        public static string Processes()
        {
            return ToString(Processes);
        }

        public static void Environment(TextWriter w)
        {
            WriteHeader(w, "Environment:");
            w.WriteLine("User@Domain:     " + System.Environment.UserName + "@" + System.Environment.UserDomainName);
            w.WriteLine("Workstation:     " + System.Environment.MachineName);
            w.WriteLine("Timestamp:       " + DateTime.UtcNow.ToString("s"));
            w.WriteLine("Used Memory:     " + (System.Environment.WorkingSet / (1024 * 1024)) + "MB");
            w.WriteLine("OS Version:      " + System.Environment.OSVersion);
            w.WriteLine(".NET Runtime:    " + System.Environment.Version);
            w.Write(".NET Frameworks: "); Frameworks(w); w.WriteLine();
        }

        public static string Environment()
        {
            return ToString(Environment);
        }

        public static string Frameworks()
        {
            return ToString(Frameworks);
        }

        public static void Frameworks(TextWriter w)
        {
            var registry = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\NET Framework Setup\NDP", false);
            if (registry == null) return;
            bool isFirst = true;
            foreach (var key in registry.GetSubKeyNames())
            {
                if (isFirst) isFirst = false;
                else w.Write("; ");
                w.Write(key);
                var framework = registry.OpenSubKey(key);
                object servicePack = framework.GetValue("SP");
                if (servicePack != null) w.Write(" (SP" + servicePack + ")");
            }
        }

        static string ToString(Action<TextWriter> action)
        {
            StringWriter sw = new StringWriter();
            action(sw);
            return sw.ToString();
        }

        static void WriteHeader(TextWriter w, string header)
        {
            w.WriteLine();
            w.WriteLine(header);
            w.WriteLine(new string('-', header.Length));
        }

        static void WriteTable(TextWriter w, List<string[]> list)
        {
            List<int> width = new List<int>();
            foreach (var item in list)
            {
                for (int i = 0; i < item.Length; ++i)
                {
                    if (i == width.Count) 
                        width.Add(item[i].Length);
                    else if (item[i].Length > width[i]) 
                        width[i] = item[i].Length;
                }                    
            }
            foreach (var item in list)
            {
                for (int i = 0; i < item.Length; ++i)
                {
                    w.Write(item[i]);
                    for (int n = width[i] - item[i].Length + 1; --n > 0;)
                    {
                        w.Write(' ');
                    }
                }    
                w.WriteLine();
            }
        }
    }
}