using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using MapleShark2.Logging;
using MapleShark2.Theme;
using MapleShark2.Tools;
using Microsoft.Win32;
using NLog;
using SharpPcap.LibPcap;

namespace MapleShark2.UI.Child {
    public sealed partial class SplashForm : Form {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private int centerX = 0, centerY = 0;
        private int lastX = -1;
        private int timer = 0;
        private bool lastOrientation = true;

        public SplashForm() {
            InitializeComponent();
            ThemeApplier.ApplyTheme(Config.Instance.Theme, this);

            centerX = this.Size.Width / 2;
            centerY = this.Size.Height / 2;

            centerX -= pictureBox1.Size.Width / 2;
            centerY -= pictureBox1.Size.Height / 2;
        }

        private void frmSplash_Load(object sender, EventArgs e) {
            initialisator.RunWorkerAsync();
        }

        private void timer1_Tick(object sender, EventArgs e) {
            timer += 25;
            var pic = pictureBox1;
            var time = timer++;

            var tmp = time / 360.0f;
            var x = Math.Sin(tmp) * 90;
            x += centerX;

            var y = pic.Location.Y;

            pic.Location = new Point((int) x, (int) y);

            bool goLeft = lastX > (int) x;

            if (goLeft != lastOrientation) {
                Image img = pic.Image.Clone() as Image;
                img.RotateFlip(RotateFlipType.RotateNoneFlipX);
                pic.Image = img;
                lastOrientation = goLeft;
            }

            lastX = (int) x;
        }

        private void initialisator_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void initialisator_DoWork(object sender, DoWorkEventArgs e) {
            Environment.CurrentDirectory = AppDomain.CurrentDomain.BaseDirectory;

            initialisator.ReportProgress(0, "Initializing network devices");
            InitializeNetworkDevices();

            initialisator.ReportProgress(0, "Loading packet definitions");
            DefinitionsContainer.Load();

            initialisator.ReportProgress(0, "Loading + saving config file");
            Config.Instance.Save();

            // Disable this for now.
            //initialisator.ReportProgress(0, "Registering .msb extension");
            //RegisterFileAssociation(".msb", "MapleShark", "MapleShark Binary File", filepath, string.Empty, 0);
        }

        private void InitializeNetworkDevices() {
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
                const string message = "Did you install Npcap first? "
                                       + "If you did, then try to run MapleShark in Administrator Mode."
                                       + "\n\nPress 'No' to go to the install page of Npcap.";
                DialogResult result = MessageBox.Show(this, message, ex.Message, MessageBoxButtons.YesNo,
                    MessageBoxIcon.Error);
                if (result == DialogResult.No) {
                    Process.Start("https://nmap.org/npcap/#download");
                }

                Environment.Exit(2);
            }
        }

        private static void RegisterFileAssociation(string pExtension, string pProgramId, string pDescription,
            string pEXE, string pIconPath, int pIconIndex) {
            try {
                if (pExtension.Length != 0) {
                    if (pExtension[0] != '.') pExtension = "." + pExtension;

                    using (RegistryKey key = Registry.ClassesRoot.OpenSubKey(pExtension))
                        if (key == null)
                            using (RegistryKey extKey = Registry.ClassesRoot.CreateSubKey(pExtension))
                                extKey.SetValue(string.Empty, pProgramId);

                    using (RegistryKey extKey = Registry.ClassesRoot.OpenSubKey(pExtension))
                    using (RegistryKey key = extKey.OpenSubKey(pProgramId)) {
                        if (key == null) {
                            using (RegistryKey progIdKey = Registry.ClassesRoot.CreateSubKey(pProgramId)) {
                                progIdKey.SetValue(string.Empty, pDescription);
                                using (RegistryKey defaultIcon = progIdKey.CreateSubKey("DefaultIcon"))
                                    defaultIcon.SetValue(string.Empty, $"\"{pIconPath}\",{pIconIndex}");

                                using (RegistryKey command = progIdKey.CreateSubKey("shell\\open\\command"))
                                    command.SetValue(string.Empty, $"\"{pEXE}\" \"%1\"");
                            }
                        }
                    }
                }
            } catch (Exception ex) {
                logger.Error(ex, "Error registering file association");
            }
        }

        private void initialisator_ProgressChanged(object sender, ProgressChangedEventArgs e) {
            label2.Text = (string) e.UserState;
        }
    }
}