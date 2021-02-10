using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using Be.Windows.Forms;
using Maple2.PacketLib.Tools;
using MapleShark2.Logging;
using MapleShark2.Properties;
using MapleShark2.Tools;
using MapleShark2.UI.Child;
using PacketDotNet;
using SharpPcap;
using SharpPcap.LibPcap;
using WeifenLuo.WinFormsUI.Docking;

namespace MapleShark2.UI {
    public partial class MainForm : Form {
        private const string LAYOUT_FILE = "Layout.xml";

        private bool mClosed;
        private PcapDevice mDevice;

        // DockContent Controls
        private SearchForm mSearchForm;
        private DataForm mDataForm;
        private StructureForm mStructureForm;
        private PropertyForm mPropertyForm;

        private DeserializeDockContent mDeserializeDockContent;
        private readonly string[] startupArguments;

        private List<RawCapture> packetQueue = new List<RawCapture>();

        public MainForm(string[] startupArguments) {
            InitializeComponent();
            CreateStandardControls();

            Text = "MapleShark2 (Build: " + Program.AssemblyVersion + ")";

            mDeserializeDockContent = GetContentFromPersistString;
            this.startupArguments = startupArguments;
        }

        private void CreateStandardControls() {
            mSearchForm = new SearchForm();
            mDataForm = new DataForm();
            mStructureForm = new StructureForm();
            mPropertyForm = new PropertyForm();
        }

        private IDockContent GetContentFromPersistString(string persistString) {
            if (persistString == typeof(SearchForm).ToString()) {
                return mSearchForm;
            }

            if (persistString == typeof(DataForm).ToString()) {
                return mDataForm;
            }

            if (persistString == typeof(StructureForm).ToString()) {
                return mStructureForm;
            }

            if (persistString == typeof(PropertyForm).ToString()) {
                return mPropertyForm;
            }

            throw new ArgumentException("Invalid layout found from config.");
        }

        public SearchForm SearchForm => mSearchForm;
        public DataForm DataForm => mDataForm;
        public StructureForm StructureForm => mStructureForm;
        public PropertyForm PropertyForm => mPropertyForm;
        public byte Locale => ((SessionForm) mDockPanel.ActiveDocument).Locale;

        private SessionForm NewSession() {
            var session = new SessionForm();
            return session;
        }

        public void CopyPacketHex(KeyEventArgs pArgs) {
            if (mDataForm.SelectionLength > 0 && pArgs.Modifiers == Keys.Control && pArgs.KeyCode == Keys.C) {
                Clipboard.SetText(mDataForm.GetHexBoxSelectedBytes().ToHexString(' '));
                pArgs.SuppressKeyPress = true;
            } else if (mDataForm.SelectionLength > 0 && pArgs.Control && pArgs.Shift && pArgs.KeyCode == Keys.C) {
                byte[] buffer = mDataForm.GetHexBoxSelectedBytes();
                mSearchForm.SetHexBoxBytes(buffer);
                pArgs.SuppressKeyPress = true;
            }
        }

        private DialogResult ShowSetupForm() {
            return new SetupForm().ShowDialog(this);
        }

        private void SetupAdapter() {
            if (mDevice != null) {
                mDevice.StopCapture();
                mDevice.Close();
            }

            foreach (LibPcapLiveDevice pcapDevice in LibPcapLiveDeviceList.Instance) {
                if (pcapDevice.Interface.Name == Config.Instance.Interface) {
                    mDevice = pcapDevice;
                    break;
                }
            }

            if (mDevice == null) {
                // Well shit...

                MessageBox.Show("Invalid configuration. Please re-setup your MapleShark configuration.", "MapleShark",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                if (ShowSetupForm() != DialogResult.OK) {
                    Close();
                    return;
                }

                SetupAdapter();
            }

            try {
                mDevice.OnPacketArrival += mDevice_OnPacketArrival;
                mDevice.Open(DeviceMode.Promiscuous, 10);
                UpdatePortRange();
                mDevice.StartCapture();
            } catch {
                MessageBox.Show("Failed to set the device in Promiscuous mode! But that doesn't really matter lol.");
                mDevice.Open();
            }
        }

        public void UpdatePortRange() {
            mDevice.Filter = $"tcp portrange {Config.Instance.LowPort}-{Config.Instance.HighPort}";
        }

        private void mDevice_OnPacketArrival(object sender, CaptureEventArgs e) {
            if (!started) return;

            lock (packetQueue) {
                packetQueue.Add(e.Packet);
            }
        }

        private void MainForm_Load(object pSender, EventArgs pArgs) {
            if (!Config.Instance.LoadedFromFile) {
                if (ShowSetupForm() != DialogResult.OK) {
                    Close();
                    return;
                }
            }

            bool useDefaults = true;
            // TODO: Loading from XML, currently this bugs because SessionForm gets serialized
            /*try {
                mDockPanel.LoadFromXml(LAYOUT_FILE, mDeserializeDockContent);
                useDefaults = false;
            } catch (Exception e) {
                // If we fail to load, it will just use the default layout.
                Console.WriteLine(e);
            }*/

            SetupAdapter();

            mTimer.Enabled = true;

            mSearchForm.Show(mDockPanel);
            mDataForm.Show(mDockPanel);
            mStructureForm.Show(mDockPanel);
            mPropertyForm.Show(mDockPanel);

            if (useDefaults) {
                // Docking can only be done after adding to panel.
                mStructureForm.DockState = DockState.DockRight;
                mPropertyForm.DockState = DockState.DockRight;
            }

            foreach (string arg in startupArguments) {
                SessionForm session = NewSession();
                session.OpenReadOnly(arg);
                session.Show(mDockPanel, DockState.Document);
            }
        }

        private void Shutdown() {
            mTimer.Enabled = false;
            mDevice?.StopCapture();
            mDevice?.Close();
        }

        private void MainForm_FormClosed(object pSender, FormClosedEventArgs pArgs) {
            Shutdown();
            mClosed = true;
        }

        private void mDockPanel_ActiveDocumentChanged(object pSender, EventArgs pArgs) {
            if (!mClosed) {
                mSearchForm.ClearOpcodes();
                if (mDockPanel.ActiveDocument is SessionForm session) {
                    //   session.RefreshPackets();
                    mSearchForm.RefreshOpcodes(false);
                    session.ReselectPacket();
                } else {
                    mDataForm.ClearHexBox();
                    mStructureForm.Tree.Nodes.Clear();
                    mPropertyForm.Properties.SelectedObject = null;
                }
            }
        }

        private void mFileImportMenu_Click(object pSender, EventArgs pArgs) {
            if (mImportDialog.ShowDialog(this) != DialogResult.OK) {
                return;
            }

            PcapDevice device = new CaptureFileReaderDevice(mImportDialog.FileName);
            device.OnPacketArrival += mDevice_OnPacketArrival;
            device.Open();
            device.Capture();
            new Thread(() => ParseImportedFile(device)).Start();
        }

        private static bool InPortRange(ushort port) {
            return port >= Config.Instance.LowPort && port <= Config.Instance.HighPort;
        }

        private void ParseImportedFile(PcapDevice device) {
            this.Invoke((MethodInvoker) delegate {
                SessionForm session = null;
                while (device.GetNextPacket(out RawCapture packet) != 0) {
                    var tcpPacket = Packet.ParsePacket(packet.LinkLayerType, packet.Data).Extract<TcpPacket>();
                    if (tcpPacket == null) continue;
                    if (!InPortRange(tcpPacket.SourcePort) || !InPortRange(tcpPacket.DestinationPort))
                        continue;

                    try {
                        if (tcpPacket.Synchronize && !tcpPacket.Acknowledgment) {
                            session = NewSession();
                        } else if (session == null || !session.MatchTcpPacket(tcpPacket)) {
                            continue;
                        }

                        SessionForm.Results result = session.BufferTcpPacket(tcpPacket, packet.Timeval.Date);
                        if (result == SessionForm.Results.CloseMe) {
                            session.Close();
                            session = null;
                        }
                    } catch (Exception ex) {
                        Console.WriteLine($"Exception while parsing logfile: {ex}");
                        session?.Close();
                        session = null;
                    }
                }

                if (session != null) {
                    session.Show(mDockPanel, DockState.Document);
                    mSearchForm.RefreshOpcodes(false);
                }
            });
        }

        private void mFileOpenMenu_Click(object pSender, EventArgs pArgs) {
            if (mOpenDialog.ShowDialog(this) != DialogResult.OK) {
                return;
            }

            foreach (string path in mOpenDialog.FileNames) {
                SessionForm session = NewSession();
                session.OpenReadOnly(path);
                session.Show(mDockPanel, DockState.Document);
            }

            mSearchForm.RefreshOpcodes(false);
        }

        private void mFileQuit_Click(object pSender, EventArgs pArgs) {
            Close();
        }

        private void mViewMenu_DropDownOpening(object pSender, EventArgs pArgs) {
            mViewSearchMenu.Checked = mSearchForm.Visible;
            mViewDataMenu.Checked = mDataForm.Visible;
            mViewStructureMenu.Checked = mStructureForm.Visible;
            mViewPropertiesMenu.Checked = mPropertyForm.Visible;
        }

        private void mViewSearchMenu_CheckedChanged(object pSender, EventArgs pArgs) {
            if (mViewSearchMenu.Checked) mSearchForm.Show();
            else mSearchForm.Hide();
        }

        private void mViewDataMenu_CheckedChanged(object pSender, EventArgs pArgs) {
            if (mViewDataMenu.Checked) mDataForm.Show();
            else mDataForm.Hide();
        }

        private void mViewStructureMenu_CheckedChanged(object pSender, EventArgs pArgs) {
            if (mViewStructureMenu.Checked) mStructureForm.Show();
            else mStructureForm.Hide();
        }

        private void mViewPropertiesMenu_CheckedChanged(object pSender, EventArgs pArgs) {
            if (mViewPropertiesMenu.Checked) mPropertyForm.Show();
            else mPropertyForm.Hide();
        }

        private readonly List<SessionForm> closes = new List<SessionForm>();

        private void mTimer_Tick(object sender, EventArgs e) {
            try {
                mTimer.Enabled = false;

                DateTime now = DateTime.Now;
                foreach (Form form in MdiChildren) {
                    var ses = (SessionForm) form;
                    if (ses.CloseMe(now))
                        closes.Add(ses);
                }

                closes.ForEach((a) => { a.Close(); });
                closes.Clear();

                ProcessPacketQueue();

                mTimer.Enabled = true;
            } catch (Exception) {
                if (!mDevice.Opened) {
                    mDevice.Open(DeviceMode.Promiscuous, 1);
                }
            }
        }

        private void ProcessPacketQueue() {
            List<RawCapture> curQueue;
            lock (packetQueue) {
                curQueue = packetQueue;
                packetQueue = new List<RawCapture>();
            }

            foreach (RawCapture packet in curQueue) {
                if (!started) {
                    continue;
                }

                var tcpPacket = Packet.ParsePacket(packet.LinkLayerType, packet.Data).Extract<TcpPacket>();
                SessionForm session = null;
                try {
                    SessionForm.Results? result;
                    if (tcpPacket.Synchronize && !tcpPacket.Acknowledgment && InPortRange(tcpPacket.DestinationPort)) {
                        session = NewSession();
                        result = session.BufferTcpPacket(tcpPacket, packet.Timeval.Date);
                    } else {
                        session =
                            Array.Find(MdiChildren,
                                f => ((SessionForm) f).MatchTcpPacket(tcpPacket)) as SessionForm;
                        result = session?.BufferTcpPacket(tcpPacket, packet.Timeval.Date);
                    }

                    switch (result) {
                        case SessionForm.Results.Show:
                            session.Show(mDockPanel, DockState.Document);
                            break;
                        case SessionForm.Results.CloseMe:
                            session.Close();
                            break;
                    }
                } catch (Exception ex) {
                    Console.WriteLine(ex.ToString());
                    File.AppendAllText("MapleShark Error.txt", ex + "\n" + ex.StackTrace);
                    session?.Close();
                }
            }
        }

        private bool started = true;

        private void toolStripButton1_Click(object sender, EventArgs e) {
            if (started) {
                started = false;
                mStopStartButton.Image = Resources.Button_Blank_Green_icon;
                mStopStartButton.Text = "Start sniffing";
            } else {
                started = true;
                mStopStartButton.Image = Resources.Button_Blank_Red_icon;
                mStopStartButton.Text = "Stop sniffing";
            }
        }

        private void helpToolStripButton_Click(object sender, EventArgs e) {
            if (File.Exists("Readme.txt")) {
                Process.Start(Environment.CurrentDirectory + @"\Readme.txt");
            }
        }

        private void saveToolStripButton_Click(object sender, EventArgs e) {
            if (mDockPanel.ActiveDocument is SessionForm session) {
                session.RunSaveCMD();
            }
        }

        private void importJavaPropertiesFileToolStripMenuItem_Click(object sender, EventArgs e) {
            new ImportOpsForm().ShowDialog();
        }

        private void MainForm_DragEnter(object sender, DragEventArgs e) {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) {
                string[] files = (string[]) e.Data.GetData(DataFormats.FileDrop);
                bool okay = false;
                foreach (string file in files) {
                    switch (Path.GetExtension(file)) {
                        case ".msb":
                        case ".pcap":
                            okay = true;
                            continue;
                    }
                }

                e.Effect = okay ? DragDropEffects.Move : DragDropEffects.None;
            } else {
                e.Effect = DragDropEffects.None;
            }
        }

        private void MainForm_DragDrop(object sender, DragEventArgs e) {
            string[] files = (string[]) e.Data.GetData(DataFormats.FileDrop);
            foreach (string file in files) {
                if (!File.Exists(file)) continue;

                switch (Path.GetExtension(file)) {
                    case ".msb": {
                        SessionForm session = NewSession();
                        session.OpenReadOnly(file);
                        session.Show(mDockPanel, DockState.Document);
                        mSearchForm.RefreshOpcodes(false);
                        break;
                    }
                    case ".pcap": {
                        PcapDevice device = new CaptureFileReaderDevice(file);
                        device.Open();
                        ParseImportedFile(device);
                        break;
                    }
                }
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e) {
            // Try to close all sessions
            List<SessionForm> sessionForms = new List<SessionForm>();
            foreach (IDockContent form in mDockPanel.Contents) {
                if (form is SessionForm sessionForm) {
                    sessionForms.Add(sessionForm);
                }
            }

            int sessions = sessionForms.Count;
            bool doSaveQuestioning = true;
            if (sessions > 5) {
                doSaveQuestioning =
                    MessageBox.Show(
                        $"You've got {sessions} sessions open. Say 'Yes' if you want to get a question for each session, 'No' if you want to quit MapleShark.",
                        "MapleShark", MessageBoxButtons.YesNo)
                    == DialogResult.Yes;
            }

            while (doSaveQuestioning && sessionForms.Count > 0) {
                SessionForm ses = sessionForms[0];
                if (!ses.Saved) {
                    ses.Focus();
                    DialogResult result =
                        MessageBox.Show($"Do you want to save the session '{ses.Text}'?", "MapleShark",
                            MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);

                    switch (result) {
                        case DialogResult.Yes:
                            ses.RunSaveCMD();
                            break;
                        case DialogResult.Cancel:
                            e.Cancel = true;
                            return;
                    }
                }

                //mDockPanel.Contents.Remove(ses);
                sessionForms.Remove(ses);
            }

            DefinitionsContainer.Instance.Save();
            //mDockPanel.SaveAsXml(LAYOUT_FILE);
        }

        private void setupToolStripMenuItem_Click(object sender, EventArgs e) {
            if (ShowSetupForm() != DialogResult.OK) {
                return;
            }

            // Restart sniffing
            bool lastTimerState = mTimer.Enabled;
            if (lastTimerState) {
                mTimer.Enabled = false;
            }

            SetupAdapter();

            if (lastTimerState) {
                mTimer.Enabled = true;
            }
        }
    }
}