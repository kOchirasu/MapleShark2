using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Maple2.PacketLib.Tools;
using MapleShark2.Logging;
using MapleShark2.Properties;
using MapleShark2.Tools;
using MapleShark2.UI.Child;
using NLog;
using PacketDotNet;
using SharpPcap;
using SharpPcap.LibPcap;
using WeifenLuo.WinFormsUI.Docking;

namespace MapleShark2.UI {
    public sealed partial class MainForm : Form {
        private const string LAYOUT_FILE = "Layout.xml";

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private bool closed;
        private bool sniffEnabled = true;
        private PcapDevice device;

        // DockContent Controls
        public LogForm LogForm { get; private set; }
        public SearchForm SearchForm { get; private set; }
        public DataForm DataForm { get; private set; }
        public StructureForm StructureForm { get; private set; }
        public PropertyForm PropertyForm { get; private set; }

        private readonly string[] startupArguments;

        private List<RawCapture> packetQueue = new List<RawCapture>();
        private HashSet<SessionForm> sessions = new HashSet<SessionForm>();

        public byte Locale => ((SessionForm) mDockPanel.ActiveDocument).Locale;

        public MainForm(string[] startupArguments) {
            InitializeComponent();
            CreateStandardControls();

            Text = $"MapleShark2 (Build: {Program.AssemblyVersion})";

            this.startupArguments = startupArguments;
        }

        private void CreateStandardControls() {
            LogForm = new LogForm();
            SearchForm = new SearchForm();
            DataForm = new DataForm();
            StructureForm = new StructureForm();
            PropertyForm = new PropertyForm();
        }

        private IDockContent GetContentFromPersistString(string persistString) {
            if (persistString == typeof(LogForm).ToString()) {
                return LogForm;
            }

            if (persistString == typeof(SearchForm).ToString()) {
                return SearchForm;
            }

            if (persistString == typeof(DataForm).ToString()) {
                return DataForm;
            }

            if (persistString == typeof(StructureForm).ToString()) {
                return StructureForm;
            }

            if (persistString == typeof(PropertyForm).ToString()) {
                return PropertyForm;
            }

            throw new ArgumentException("Invalid layout found from config.");
        }

        public void CopyPacketHex(KeyEventArgs pArgs) {
            if (DataForm.SelectionLength > 0 && pArgs.Modifiers == Keys.Control && pArgs.KeyCode == Keys.C) {
                Clipboard.SetText(DataForm.GetHexBoxSelectedBytes().ToArray().ToHexString(' '));
                pArgs.SuppressKeyPress = true;
            } else if (DataForm.SelectionLength > 0 && pArgs.Control && pArgs.Shift && pArgs.KeyCode == Keys.C) {
                byte[] buffer = DataForm.GetHexBoxSelectedBytes().ToArray();
                SearchForm.SetHexBoxBytes(buffer);
                pArgs.SuppressKeyPress = true;
            }
        }

        private SessionForm NewSession() {
            var session = new SessionForm();
            session.OnTerminated += s => sessions.Remove(s);
            sessions.Add(session);

            return session;
        }

        private DialogResult ShowSetupForm() {
            return new SetupForm().ShowDialog(this);
        }

        private void SetupAdapter() {
            if (device != null) {
                device.StopCapture();
                device.Close();
            }

            foreach (LibPcapLiveDevice pcapDevice in LibPcapLiveDeviceList.Instance) {
                if (pcapDevice.Interface.Name == Config.Instance.Interface) {
                    device = pcapDevice;
                    break;
                }
            }

            if (device == null) {
                // Well shit...

                const string message = "Invalid configuration. Please re-setup your MapleShark configuration.";
                MessageBox.Show(message, "MapleShark2", MessageBoxButtons.OK, MessageBoxIcon.Error);
                if (ShowSetupForm() != DialogResult.OK) {
                    Close();
                    return;
                }

                SetupAdapter();
            }

            try {
                device.Open(DeviceModes.Promiscuous, 10);
            } catch {
                MessageBox.Show("Failed to set the device in Promiscuous mode! But that doesn't really matter lol.");
                device.Open();
            }

            device.OnPacketArrival += device_OnPacketArrival;
            device.Filter = $"tcp portrange {Config.Instance.LowPort}-{Config.Instance.HighPort}";
            device.StartCapture();
        }

        private void device_OnPacketArrival(object sender, PacketCapture e) {
            if (!sniffEnabled) return;

            lock (packetQueue) {
                packetQueue.Add(e.GetPacket());
            }
        }

        private void MainForm_Load(object pSender, EventArgs pArgs) {
            mDockPanel.Theme = Config.Instance.Theme.DockSuiteTheme;
            toolStripExtender.DefaultRenderer = new ToolStripProfessionalRenderer();
            toolStripExtender.SetStyle(mMenu, VisualStudioToolStripExtender.VsVersion.Vs2015, mDockPanel.Theme);
            toolStripExtender.SetStyle(toolStrip, VisualStudioToolStripExtender.VsVersion.Vs2015, mDockPanel.Theme);

            try {
                mDockPanel.LoadFromXml(LAYOUT_FILE, GetContentFromPersistString);
            } catch {
                // If we fail to load, it will just use the default layout.
                logger.Debug("Using default layout");
                LogForm.Show(mDockPanel);
                SearchForm.Show(mDockPanel);
                DataForm.Show(mDockPanel);
                StructureForm.Show(mDockPanel);
                PropertyForm.Show(mDockPanel);

                // Docking can only be done after adding to panel.
                StructureForm.DockState = DockState.DockRight;
                PropertyForm.DockState = DockState.DockRight;
            }

            LogForm.ApplyTheme();
            SearchForm.ApplyTheme();
            DataForm.ApplyTheme();
            StructureForm.ApplyTheme();
            PropertyForm.ApplyTheme();

            SetupAdapter();
            mTimer.Enabled = true;

            foreach (string arg in startupArguments) {
                SessionForm session = NewSession();
                session.OpenReadOnly(arg);
                StructureForm.InitEngine(session.Locale, session.Build);
                session.Show(mDockPanel, DockState.Document);
            }
        }

        private void Shutdown() {
            mTimer.Enabled = false;
            device?.StopCapture();
            device?.Close();
        }

        private void MainForm_FormClosed(object pSender, FormClosedEventArgs pArgs) {
            Shutdown();
            closed = true;
        }

        private void mDockPanel_ActiveDocumentChanged(object pSender, EventArgs pArgs) {
            if (!closed) {
                SearchForm.ClearOpcodes();
                if (mDockPanel.ActiveDocument is SessionForm session) {
                    SearchForm.RefreshOpcodes(false);
                    session.ReselectPacket();
                } else {
                    DataForm.ClearHexBox();
                    StructureForm.Tree.Nodes.Clear();
                    PropertyForm.Properties.SelectedObject = null;
                }
            }
        }

        private void mFileImportMenu_Click(object pSender, EventArgs pArgs) {
            if (mImportDialog.ShowDialog(this) != DialogResult.OK) {
                return;
            }

            LoadPcapFile(mImportDialog.FileName);
        }

        private static bool InPortRange(ushort port) {
            return port >= Config.Instance.LowPort && port <= Config.Instance.HighPort;
        }

        private void LoadPcapFile(string fileName) {
            PcapDevice fileDevice = new CaptureFileReaderDevice(fileName);
            fileDevice.Open();

            SessionForm session = null;
            while (fileDevice.GetNextPacket(out PacketCapture capture) != 0) {
                RawCapture packet = capture.GetPacket();
                if (packet == null) {
                    continue;
                }

                var tcpPacket = Packet.ParsePacket(packet.LinkLayerType, packet.Data).Extract<TcpPacket>();
                if (tcpPacket == null) continue;
                if (!InPortRange(tcpPacket.SourcePort) && !InPortRange(tcpPacket.DestinationPort)) continue;

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
                    logger.Fatal(ex, $"Exception while parsing logfile: {fileName}");
                    session?.Close();
                    session = null;
                }
            }

            session?.Show(mDockPanel, DockState.Document);
        }

        private void mFileOpenMenu_Click(object pSender, EventArgs pArgs) {
            if (mOpenDialog.ShowDialog(this) != DialogResult.OK) {
                return;
            }

            foreach (string path in mOpenDialog.FileNames) {
                SessionForm session = NewSession();
                session.OpenReadOnly(path);
                StructureForm.InitEngine(session.Locale, session.Build);
                session.Show(mDockPanel, DockState.Document);
            }
        }

        private void mFileQuit_Click(object pSender, EventArgs pArgs) {
            Close();
        }

        private void mViewMenu_DropDownOpening(object pSender, EventArgs pArgs) {
            mViewLogMenu.Checked = !LogForm.IsHidden;
            mViewSearchMenu.Checked = !SearchForm.IsHidden;
            mViewDataMenu.Checked = !DataForm.IsHidden;
            mViewStructureMenu.Checked = !StructureForm.IsHidden;
            mViewPropertiesMenu.Checked = !PropertyForm.IsHidden;
        }

        private void mViewLogMenu_CheckedChanged(object pSender, EventArgs pArgs) {
            if (mViewLogMenu.Checked) LogForm.Show();
            else LogForm.Hide();
        }

        private void mViewSearchMenu_CheckedChanged(object pSender, EventArgs pArgs) {
            if (mViewSearchMenu.Checked) SearchForm.Show();
            else SearchForm.Hide();
        }

        private void mViewDataMenu_CheckedChanged(object pSender, EventArgs pArgs) {
            if (mViewDataMenu.Checked) DataForm.Show();
            else DataForm.Hide();
        }

        private void mViewStructureMenu_CheckedChanged(object pSender, EventArgs pArgs) {
            if (mViewStructureMenu.Checked) StructureForm.Show();
            else StructureForm.Hide();
        }

        private void mViewPropertiesMenu_CheckedChanged(object pSender, EventArgs pArgs) {
            if (mViewPropertiesMenu.Checked) PropertyForm.Show();
            else PropertyForm.Hide();
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
                if (!device.Opened) {
                    device.Open(DeviceModes.Promiscuous, 1);
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
                if (!sniffEnabled) {
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
                        session = sessions.FirstOrDefault(s => s.MatchTcpPacket(tcpPacket));
                        if (session == null) {
                            continue;
                        }

                        result = session?.BufferTcpPacket(tcpPacket, packet.Timeval.Date);
                    }

                    switch (result) {
                        case SessionForm.Results.Show:
                            StructureForm.InitEngine(session.Locale, session.Build);
                            session.Show(mDockPanel, DockState.Document);
                            break;
                        case SessionForm.Results.CloseMe:
                            session.Close();
                            break;
                    }
                } catch (Exception ex) {
                    logger.Fatal(ex, "Exception while processing packet queue");
                    session?.Close();
                }
            }
        }

        private void mStopStartButton_Click(object sender, EventArgs e) {
            if (sniffEnabled) {
                sniffEnabled = false;
                mStopStartButton.Image = Resources.Button_Blank_Green_icon;
                mStopStartButton.Text = "Start sniffing";
            } else {
                sniffEnabled = true;
                mStopStartButton.Image = Resources.Button_Blank_Red_icon;
                mStopStartButton.Text = "Stop sniffing";
            }
        }

        private void helpToolStripButton_Click(object sender, EventArgs e) {
            if (File.Exists("README.md")) {
                Process.Start(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"README.md"));
            }
        }

        private void saveToolStripButton_Click(object sender, EventArgs e) {
            if (mDockPanel.ActiveDocument is SessionForm session) {
                session.SavePacketLog();
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
                        StructureForm.InitEngine(session.Locale, session.Build);
                        session.Show(mDockPanel, DockState.Document);
                        break;
                    }
                    case ".pcap": {
                        LoadPcapFile(file);
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
                string message = $"You've got {sessions} sessions open. "
                                 + "Say 'Yes' if you want to get a question for each session, "
                                 + "'No' if you want to quit MapleShark.";
                DialogResult result = MessageBox.Show(message, "MapleShark2", MessageBoxButtons.YesNo);
                doSaveQuestioning = result == DialogResult.Yes;
            }

            foreach (SessionForm session in sessionForms) {
                if (!session.Saved && doSaveQuestioning) {
                    session.Focus();

                    string message = $"Do you want to save the session '{session.Text}'?";
                    DialogResult result = MessageBox.Show(message, "MapleShark2", MessageBoxButtons.YesNoCancel);
                    switch (result) {
                        case DialogResult.Yes:
                            session.SavePacketLog();
                            break;
                        case DialogResult.Cancel:
                            e.Cancel = true;
                            return;
                    }
                }

                session.Close();
            }

            DefinitionsContainer.Instance.Save();
            mDockPanel.SaveAsXml(LAYOUT_FILE);
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