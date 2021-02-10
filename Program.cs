using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using MapleShark2.Theme;
using MapleShark2.UI;
using MapleShark2.UI.Child;
using SharpPcap.LibPcap;

namespace MapleShark2 {
    internal static class Program {
        [STAThread]
        private static void Main(string[] pArgs) {
            AppDomain.CurrentDomain.UnhandledException += (sender, args) => {
                Exception e = (Exception) args.ExceptionObject;

                File.AppendAllText("MapleShark Error.txt", e.ToString());

                if (MessageBox.Show("Exception occurred. Open error in notepad?", "", MessageBoxButtons.YesNo)
                    == DialogResult.Yes) {
                    Process.Start("notepad", "\"MapleShark Error.txt\"");
                }
            };

            try {
                if (LibPcapLiveDeviceList.Instance.Count == 0) throw new Exception();
            } catch {
                if (MessageBox.Show(null,
                        "Did you install WinPcap first? If you did, then try to run MapleShark in Administrator Mode, else press 'No' to go to the install page of WinPcap.",
                        "Interface Error", MessageBoxButtons.YesNo, MessageBoxIcon.Error)
                    == DialogResult.No) {
                    Process.Start("http://www.winpcap.org/install/default.htm");
                }

                Environment.Exit(2);
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            using (var frm = new SplashForm()) {
                if (frm.ShowDialog() == DialogResult.OK)
                    Application.Run(new MainForm(new DarkTheme(), pArgs));
            }
        }

        internal static string AssemblyVersion => Assembly.GetExecutingAssembly().GetName().Version.ToString();
    }
}