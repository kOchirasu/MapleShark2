using System;
using System.Reflection;
using System.Windows.Forms;
using MapleShark2.Tools;
using MapleShark2.UI;
using MapleShark2.UI.Child;
using NLog;

namespace MapleShark2 {
    internal static class Program {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        [STAThread]
        private static void Main(string[] pArgs) {
            AppDomain.CurrentDomain.UnhandledException += HandleUnhandledException;

            Application.SetHighDpiMode(HighDpiMode.SystemAware);
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

        private static void HandleUnhandledException(object sender, UnhandledExceptionEventArgs args) {
            var ex = (Exception) args.ExceptionObject;
            logger.Fatal(ex, "Unhandled Exception");

            string message = $"{ex.Message}";
            MessageBox.Show(message, "MapleShark2 Exception", MessageBoxButtons.OK);
        }

        internal static string AssemblyVersion => Assembly.GetExecutingAssembly().GetName().Version.ToString();
    }
}