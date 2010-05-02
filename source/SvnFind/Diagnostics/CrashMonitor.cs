using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Threading;


namespace SvnFind.Diagnostics
{
    /// <summary>
    /// Monitors unhandled exceptions and display a problem dialog with the possibility to
    /// send an error dump and restart the application.
    /// </summary>
    /// <remarks>
    /// Note that exceptions thrwon inside of the System.Timers.Timer.Elapsed event are
    /// are always catched by the Timer class and silently swallowed. You need to catch them
    /// by yourself and delegate the exception to HandleException();
    /// </remarks>
    public static class CrashMonitor
    {
        static Application _app;
        static string _appName;
        static int _isTerminating; // needed for reentry protection in exception handling

        const string RestartedOption = "-restarted";

        public static void Start(Application app, string appName)
        {
            _app = app;
            _appName = appName;

            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            _app.DispatcherUnhandledException += OnDispatcherUnhandledException;

            // note that System.Timers.Timer.Elapsed exceptions are always catched and silently swallowed

            // System.Timers.Timer.Elapsed exceptions are always catched!!! and will not appear
            //            System.Timers.Timer timer;
            //            timer = new System.Timers.Timer(100);
            //            timer.Elapsed += delegate { throw new Exception("Timer"); };
            //            timer.Start();

            KillHangingApp();
        }

        static void KillHangingApp()
        {
            if (Environment.CommandLine.StartsWith(RestartedOption))
            {
                try
                {
                    int pid = int.Parse(Environment.CommandLine.Substring(RestartedOption.Length));
                    Process p = Process.GetProcessById(pid);
                    if (Process.GetCurrentProcess().MainModule.FileName == p.MainModule.FileName && !p.WaitForExit(1000))
                    {
                        p.Kill();
                    }
                }
                catch (Exception x)
                {
                    Debug.Assert(false, x.Message);
                }
            }
        }

        public static void HandleException(Exception x)
        {
            if (_app.Dispatcher.CheckAccess())
            {
                ShowExceptionAndTerminate(x);
            }
            else
            {
                _app.Dispatcher.Invoke(DispatcherPriority.Send, new Action<Exception>(ShowExceptionAndTerminate), x);   
            }
        }

        static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            // because all unhandled dispatcher execptions are handled by the Dispatcher Thread
            // we must be in a none Dispatcher Thread
            // Debug.Assert(_app.Dispatcher.CheckAccess() == false);

            Exception x = e.ExceptionObject as Exception;
            HandleException(x ?? new Exception("Unknown exception type " + e.ExceptionObject.GetType()));
        }

        static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            HandleException(e.Exception);
            e.Handled = true;
        }

        static void ShowExceptionAndTerminate(Exception x)
        {
            // prevent reentry through multiple exceptions on different threads
            if (Interlocked.CompareExchange(ref _isTerminating, 1, 0) == 1) return;

            CrashDialog dlg = new CrashDialog(x);
            //dlg.Owner = (MainWindow != null && MainWindow.IsLoaded && MainWindow.IsVisible) ? MainWindow : null;
            dlg.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            if (dlg.ShowDialog() == true)
            {
                Process p = Process.GetCurrentProcess();
                Process.Start(p.MainModule.FileName, "-restarted " + p.Id);
            }

            _app.Shutdown(0);
        }

    }
}