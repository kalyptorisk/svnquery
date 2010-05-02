using System;
using System.Diagnostics;
using System.IO;
using System.Media;
using System.Windows;
using System.Windows.Documents;

namespace SvnFind.Diagnostics
{
    /// <summary>
    /// A dialog that displays a problem report (with hidden details) and
    /// allows the user to 
    ///  - mail the problem report (handled by this dialog)
    ///  - restart the application (ShowDialog() will return true in this case)
    ///  - view the problem details in notepad
    /// It is expected that the application terminates after showing this dialog.
    /// </summary>
    public partial class CrashDialog     
    {
        readonly string _details;
        readonly string _detailsFile;

        public CrashDialog(Exception x)
        {
            InitializeComponent();

            txbProblem.Text = Dump.ExceptionMessage(x);

            _details = Dump.All(x);
            _detailsFile = Path.Combine(Path.GetTempPath(), "SvnFind_Problem_Report.txt");
            File.WriteAllText(_detailsFile, _details);

            lnkReport.Inlines.Add(new Run(x.GetType().Name));
            lnkReport.Click += delegate { Process.Start("notepad", _detailsFile); };
            btnCopy.Click += delegate { Clipboard.SetText(_details); };
            btnRestart.Click += delegate { DialogResult = true; };

            string sound = Environment.ExpandEnvironmentVariables("%windir%\\media\\Windows Critical Stop.wav");
            Loaded += delegate { new SoundPlayer(sound).Play(); };
        }

       
    }
}