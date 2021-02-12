using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using MapleShark2.Tools;
using MapleShark2.UI;
using MapleShark2.UI.Child;

namespace MapleShark2 {
    internal static class Program {
        [STAThread]
        private static void Main(string[] pArgs) {
            AppDomain.CurrentDomain.UnhandledException += (sender, args) => {
                File.AppendAllText("error.log", args.ExceptionObject.ToString());

                const string message = "Exception occurred. Open error in notepad?";
                if (MessageBox.Show(message, "MapleShark2", MessageBoxButtons.YesNo) == DialogResult.Yes) {
                    Process.Start("notepad", "error.log");
                }
            };

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            using (var splashForm = new SplashForm()) {
                if (splashForm.ShowDialog() != DialogResult.OK) {
                    return;
                }

                if (!Config.Instance.LoadedFromFile) {
                    var setupForm = new SetupForm();
                    if (setupForm.ShowDialog() != DialogResult.OK) {
                        return;
                    }

                    // Since this is the first-time setup we can apply the theme right away.
                    Config.Instance.LoadTheme();
                }

                var mainForm = new MainForm(pArgs);
                Application.Run(mainForm);
            }
        }

        internal static string AssemblyVersion => Assembly.GetExecutingAssembly().GetName().Version.ToString();
    }
}