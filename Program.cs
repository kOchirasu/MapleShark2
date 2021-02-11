using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using MapleShark2.Tools;
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
                bool zeroFlags = true;
                bool foundDevice = false;
                foreach (LibPcapLiveDevice device in LibPcapLiveDeviceList.Instance) {
                    zeroFlags &= device.Flags == 0;
                    if (!device.IsActive()) continue;

                    // Just need 1 active device
                    foundDevice = true;
                    break;
                }

                if (zeroFlags) throw new ApplicationException("Failed to read Flags from devices.");
                if (!foundDevice) throw new ApplicationException("Unable to find any active devices.");
            } catch (Exception ex) {
                if (MessageBox.Show(null,
                        "Did you install Npcap first? If you did, then try to run MapleShark in Administrator Mode."
                        + "\n\nPress 'No' to go to the install page of Npcap.", ex.Message,
                        MessageBoxButtons.YesNo, MessageBoxIcon.Error)
                    == DialogResult.No) {
                    Process.Start("https://nmap.org/npcap/#download");
                }

                Environment.Exit(2);
                return;
            }

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