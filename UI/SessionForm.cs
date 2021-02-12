using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Maple2.PacketLib.Crypto;
using Maple2.PacketLib.Tools;
using MapleShark2.Logging;
using MapleShark2.Theme;
using MapleShark2.Tools;
using MapleShark2.UI.Child;
using MapleShark2.UI.Control;
using PacketDotNet;
using WeifenLuo.WinFormsUI.Docking;

namespace MapleShark2.UI {
    public partial class SessionForm : DockContent {
        public enum Results {
            Show,
            Continue,
            Terminated,
            CloseMe
        }

        private string mFilename = null;
        private bool mTerminated = false;
        private ushort mLocalPort = 0;
        private ushort mRemotePort = 0;
        private ushort mProxyPort = 0;

        private DateTime startTime;
        private MapleCipher.Decryptor outDecryptor;
        private MapleCipher.Decryptor inDecryptor;
        private readonly TcpReassembler tcpReassembler = new TcpReassembler();
        private readonly List<MaplePacket> mPackets = new List<MaplePacket>();

        private string mRemoteEndpoint = "???";
        private string mLocalEndpoint = "???";

        public MainForm MainForm => ParentForm as MainForm;

        public PacketListView ListView { get; private set; }
        public IReadOnlyList<MaplePacket> FilteredPackets => ListView.FilteredPackets;

        public uint Build { get; private set; }
        public byte Locale { get; private set; }
        public List<Opcode> Opcodes { get; private set; } = new List<Opcode>();

        public bool Saved { get; private set; }

        // Used for determining if the session did receive a packet at all, or if it just emptied its buffers
        public bool ClearedPackets { get; private set; }

        private MsbMetadata MsbMetadata => new MsbMetadata {
            LocalEndpoint = mLocalEndpoint,
            LocalPort = mLocalPort,
            RemoteEndpoint = mRemoteEndpoint,
            RemotePort = mRemotePort,
            Locale = Locale,
            Build = Build,
        };

        internal SessionForm() {
            ClearedPackets = false;
            InitializeComponent();
            ScaleColumns();
            Saved = false;

            // Last column fills to ListView width
            ListView.Resize += (sender, e) => {
                ListView.ColumnHeaderCollection columns = ((PacketListView) sender).Columns;
                columns[columns.Count - 1].Width = -2;
            };
            ListView.ColumnWidthChanged += (sender, e) => {
                ListView.ColumnHeaderCollection columns = ((PacketListView) sender).Columns;
                columns[columns.Count - 1].Width = -2;
            };
        }

        public new void Show(DockPanel panel, DockState state) {
            base.Show(panel, state);

            toolStripExtender.DefaultRenderer = new ToolStripProfessionalRenderer();
            toolStripExtender.SetStyle(mMenu, VisualStudioToolStripExtender.VsVersion.Vs2015,
                Config.Instance.Theme.DockSuiteTheme);
            toolStripExtender.SetStyle(mPacketContextMenu, VisualStudioToolStripExtender.VsVersion.Vs2015,
                Config.Instance.Theme.DockSuiteTheme);

            ThemeApplier.ApplyTheme(Config.Instance.Theme, Controls);
            ThemeApplier.ApplyTheme(Config.Instance.Theme, mPacketContextMenu.Controls);
        }

        // Fix column widths when using screen scaling.
        private void ScaleColumns() {
            float scale = CreateGraphics().DpiX / 96;
            mTimestampColumn.Width = (int) (mTimestampColumn.Width * scale);
            mDirectionColumn.Width = (int) (mDirectionColumn.Width * scale);
            mLengthColumn.Width = (int) (mLengthColumn.Width * scale);
            mOpcodeColumn.Width = (int) (mOpcodeColumn.Width * scale);
            mNameColumn.Width = (int) (mNameColumn.Width * scale);
        }

        public void UpdateOpcodeList() {
            Opcodes = Opcodes.OrderBy(a => a.Header).ToList();
        }

        internal bool MatchTcpPacket(TcpPacket tcpPacket) {
            if (mTerminated) return false;
            if (tcpPacket.SourcePort == mLocalPort
                && tcpPacket.DestinationPort == (mProxyPort > 0 ? mProxyPort : mRemotePort)) return true;
            if (tcpPacket.SourcePort == (mProxyPort > 0 ? mProxyPort : mRemotePort)
                && tcpPacket.DestinationPort == mLocalPort) return true;
            return false;
        }

        internal bool CloseMe(DateTime pTime) {
            if (!ClearedPackets && mPackets.Count == 0 && (pTime - startTime).TotalSeconds >= 5) {
                return true;
            }

            return false;
        }

        internal void SetMapleInfo(ushort version, byte locale, string ip, ushort port) {
            if (mPackets.Count > 0) return;
            Build = version;
            Locale = locale;

            mRemoteEndpoint = ip;
            mRemotePort = port;

            mLocalEndpoint = "127.0.0.1";
            mLocalPort = 10000;
        }

        internal Results BufferTcpPacket(TcpPacket pTcpPacket, DateTime pArrivalTime) {
            if (mTerminated) return Results.Terminated;

            if (pTcpPacket.Finished || pTcpPacket.Reset) {
                Terminate();
                return mPackets.Count == 0 ? Results.CloseMe : Results.Terminated;
            }

            if (pTcpPacket.Synchronize) {
                if (!pTcpPacket.Acknowledgment) {
                    mLocalPort = pTcpPacket.SourcePort;
                    mRemotePort = pTcpPacket.DestinationPort;
                    Text = $"Port {mLocalPort} - {mRemotePort}";
                    startTime = DateTime.Now;

                    try {
                        mRemoteEndpoint = $"{((IPv4Packet) pTcpPacket.ParentPacket).SourceAddress}:{mLocalPort}";
                        mLocalEndpoint = $"{((IPv4Packet) pTcpPacket.ParentPacket).DestinationAddress}:{mRemotePort}";
                        Console.WriteLine("[CONNECTION] From {0} to {1}", mRemoteEndpoint, mLocalEndpoint);
                    } catch {
                        return Results.CloseMe;
                    }
                }
            }

            bool isOutbound = pTcpPacket.SourcePort == mLocalPort;
            tcpReassembler.ReassembleStream(pTcpPacket);

            MapleStream packetStream = isOutbound ? tcpReassembler.OutStream : tcpReassembler.InStream;
            int opcodeCount = Opcodes.Count;
            bool show = false;
            try {
                ListView.BeginUpdate();
                while (packetStream.TryRead(out byte[] packet)) {
                    Results result = ProcessPacket(packet, isOutbound, pArrivalTime);
                    switch (result) {
                        case Results.Continue:
                            continue;
                        case Results.Show:
                            show = true;
                            break;
                        default:
                            ListView.EndUpdate();
                            return result;
                    }
                }

                ListView.EndUpdate();

                // This should be called after EndUpdate so VirtualListSize is set properly
                if (ListView.SelectedIndices.Count == 0 && FilteredPackets.Count > 0) {
                    ListView.Items[FilteredPackets.Count - 1]?.EnsureVisible();
                }
            } catch (Exception ex) {
                Console.WriteLine(ex);
                Terminate();
                return Results.Terminated;
            }

            if (DockPanel != null && DockPanel.ActiveDocument == this && opcodeCount != Opcodes.Count) {
                MainForm.SearchForm.RefreshOpcodes(true);
            }

            return show ? Results.Show : Results.Continue;
        }

        private Results ProcessPacket(byte[] bytes, bool isOutbound, DateTime timestamp) {
            if (mTerminated) return Results.Terminated;

            if (Build == 0) {
                var packet = new ByteReader(bytes);
                packet.Read<ushort>(); // rawSeq
                int length = packet.ReadInt();
                if (bytes.Length - 6 < length) {
                    Console.WriteLine("Connection on port {0} did not have a MapleStory2 Handshake", mLocalEndpoint);
                    return Results.CloseMe;
                }

                ushort opcode = packet.Read<ushort>();
                if (opcode != 0x01) {
                    // RequestVersion
                    Console.WriteLine("Connection on port {0} did not have a valid MapleStory2 Connection Header",
                        mLocalEndpoint);
                    return Results.CloseMe;
                }

                uint version = packet.Read<uint>();
                uint siv = packet.Read<uint>();
                uint riv = packet.Read<uint>();
                uint blockIV = packet.Read<uint>();
                byte type = packet.ReadByte();

                Build = version;
                Locale = MapleLocale.UNKNOWN;

                outDecryptor = new MapleCipher.Decryptor(Build, siv, blockIV);
                inDecryptor = new MapleCipher.Decryptor(Build, riv, blockIV);

                inDecryptor.Decrypt(bytes); // Advance the IV

                // Generate HandShake packet
                Definition definition = Config.Instance.GetDefinition(Build, Locale, false, opcode);
                if (definition == null) {
                    definition = new Definition {
                        Outbound = false,
                        Locale = Locale,
                        Opcode = opcode,
                        Name = "RequestVersion",
                        Build = Build
                    };
                    SaveDefinition(definition);
                }

                ArraySegment<byte> segment = new ArraySegment<byte>(packet.Buffer);
                var maplePacket = new MaplePacket(timestamp, isOutbound, Build, opcode, segment);
                // Add to list of not exist (TODO: SortedSet?)
                if (!Opcodes.Exists(op => op.Outbound == maplePacket.Outbound && op.Header == maplePacket.Opcode)) {
                    // Should be false, but w/e
                    Opcodes.Add(new Opcode(maplePacket.Outbound, maplePacket.Opcode));
                }

                AddPacket(maplePacket, true);

                Console.WriteLine("[CONNECTION] MapleStory2 V{0}", Build);

                return Results.Show;
            }

            try {
                MapleCipher.Decryptor decryptor = isOutbound ? outDecryptor : inDecryptor;
                ByteReader packet = decryptor.Decrypt(bytes);
                // It's possible to get an empty packet, just ignore it.
                // Decryption is still necessary to advance sequence number.
                if (packet.Available == 0) {
                    return Results.Continue;
                }

                ushort opcode = packet.Peek<ushort>();
                ArraySegment<byte> segment = new ArraySegment<byte>(packet.Buffer, 2, packet.Length - 2);
                var maplePacket = new MaplePacket(timestamp, isOutbound, Build, opcode, segment);
                AddPacket(maplePacket);

                return Results.Continue;
            } catch (ArgumentException ex) {
                Console.WriteLine(ex);
                return Results.CloseMe;
            }
        }

        public void OpenReadOnly(string pFilename) {
            // mFileSaveMenu.Enabled = false;
            Saved = true;
            mTerminated = true;

            (MsbMetadata metadata, IEnumerable<MaplePacket> packets) = FileLoader.ReadMsbFile(pFilename);
            mLocalEndpoint = metadata.LocalEndpoint;
            mLocalPort = metadata.LocalPort;
            mRemoteEndpoint = metadata.RemoteEndpoint;
            mRemotePort = metadata.RemotePort;
            Locale = metadata.Locale;
            Build = metadata.Build;

            ListView.BeginUpdate();
            foreach (MaplePacket packet in packets) {
                AddPacket(packet);
            }

            ListView.EndUpdate();
            if (ListView.VirtualListSize > 0) ListView.EnsureVisible(0);

            Text = $"{Path.GetFileName(pFilename)} (ReadOnly)";
            Console.WriteLine("Loaded file: {0}", pFilename);
        }

        public void RefreshPackets() {
            ListView.BeginUpdate();

            MaplePacket previous = ListView.SelectedIndices.Count > 0
                ? FilteredPackets[ListView.SelectedIndices[0]]
                : null;
            Opcodes.Clear();
            ListView.Clear();

            MainForm.DataForm.ClearHexBox();
            MainForm.StructureForm.Tree.Nodes.Clear();
            MainForm.PropertyForm.Properties.SelectedObject = null;

            if (!mViewOutboundMenu.Checked && !mViewInboundMenu.Checked) return;
            int previousIndex = -1;
            foreach (MaplePacket packet in mPackets) {
                if (packet.Outbound && !mViewOutboundMenu.Checked) continue;
                if (!packet.Outbound && !mViewInboundMenu.Checked) continue;
                if (!Opcodes.Exists(op => op.Outbound == packet.Outbound && op.Header == packet.Opcode)) {
                    Opcodes.Add(new Opcode(packet.Outbound, packet.Opcode));
                }

                Definition definition = Config.Instance.GetDefinition(packet);
                if (definition != null && !mViewIgnoredMenu.Checked && definition.Ignore) continue;

                int index = ListView.AddPacket(packet);
                if (packet == previous) {
                    previousIndex = index;
                }
            }

            MainForm.SearchForm.RefreshOpcodes(true);

            ListView.EndUpdate();

            // This should be called after EndUpdate so VirtualListSize is set properly
            if (previous != null && previousIndex >= 0) {
                ListView.Items[previousIndex].Selected = true;
                ListView.Items[previousIndex].EnsureVisible();
            }
        }

        public void ReselectPacket() {
            mPacketList_SelectedIndexChanged(null, null);
        }

        public void SavePacketLog(bool legacy = false) {
            if (mFilename == null) {
                mSaveDialog.FileName = $"Port {mLocalPort}";
                if (mSaveDialog.ShowDialog(this) == DialogResult.OK) mFilename = mSaveDialog.FileName;
                else return;
            }

            if (legacy) {
                FileLoader.WriteLegacyMsbFile(mFilename, MsbMetadata, mPackets);
            } else {
                FileLoader.WriteMsbFile(mFilename, MsbMetadata, mPackets);
            }

            if (mTerminated) {
                mFileSaveMenu.Enabled = false;
                Text += " (ReadOnly)";
            }

            Saved = true;
        }

        private void mFileSaveMenu_Click(object pSender, EventArgs pArgs) {
            SavePacketLog();
        }

        private void mFileSaveLegacyMenu_Click(object pSender, EventArgs pArgs) {
            SavePacketLog(true);
        }

        private void mFileExportMenu_Click(object pSender, EventArgs pArgs) {
            mExportDialog.FileName = $"Port {mLocalPort}";
            if (mExportDialog.ShowDialog(this) != DialogResult.OK) return;

            FileLoader.ExportTxtFile(mExportDialog.FileName, MsbMetadata, mPackets);
        }

        private void mViewCommonScriptMenu_Click(object pSender, EventArgs pArgs) {
            string scriptPath = Helpers.GetCommonScriptPath(Locale, Build);
            Directory.CreateDirectory(scriptPath);

            var script = new ScriptForm(scriptPath, null);
            script.FormClosed += CommonScript_FormClosed;
            script.Show(DockPanel, new Rectangle(MainForm.Location, new Size(600, 300)));
        }

        private void CommonScript_FormClosed(object pSender, FormClosedEventArgs pArgs) {
            if (ListView.SelectedIndices.Count == 0) return;
            MaplePacket packet = ListView.Selected;
            MainForm.StructureForm.ParseMaplePacket(packet);
            Activate();
        }

        private void mViewRefreshMenu_Click(object pSender, EventArgs pArgs) {
            RefreshPackets();
        }

        private void mViewOutboundMenu_CheckedChanged(object pSender, EventArgs pArgs) {
            RefreshPackets();
        }

        private void mViewInboundMenu_CheckedChanged(object pSender, EventArgs pArgs) {
            RefreshPackets();
        }

        private void mViewIgnoredMenu_CheckedChanged(object pSender, EventArgs pArgs) {
            RefreshPackets();
        }

        private void mPacketList_SelectedIndexChanged(object pSender, EventArgs pArgs) {
            if (ListView.SelectedIndices.Count == 0) {
                MainForm.DataForm.ClearHexBox();
                MainForm.StructureForm.Tree.Nodes.Clear();
                MainForm.PropertyForm.Properties.SelectedObject = null;
                return;
            }

            MainForm.DataForm.SelectMaplePacket(ListView.Selected);
            MainForm.StructureForm.ParseMaplePacket(ListView.Selected);
        }

        private void mPacketList_ItemActivate(object pSender, EventArgs pArgs) {
            if (ListView.SelectedIndices.Count == 0) return;
            MaplePacket packet = ListView.Selected;

            string scriptPath = Helpers.GetScriptPath(Locale, Build, packet.Outbound, packet.Opcode);
            Directory.CreateDirectory(scriptPath);

            var script = new ScriptForm(scriptPath, packet);
            script.FormClosed += Script_FormClosed;
            script.Show(DockPanel, new Rectangle(MainForm.Location, new Size(600, 300)));
        }

        private void Script_FormClosed(object pSender, FormClosedEventArgs pArgs) {
            ScriptForm script = pSender as ScriptForm;
            //script.Packet.Selected = true;
            MainForm.StructureForm.ParseMaplePacket(script.Packet);
            Activate();
        }

        bool openingContextMenu = false;

        private void mPacketContextMenu_Opening(object pSender, CancelEventArgs pArgs) {
            openingContextMenu = true;
            mPacketContextNameBox.Text = "";
            mPacketContextIgnoreMenu.Checked = false;
            if (ListView.SelectedIndices.Count == 0) {
                pArgs.Cancel = true;
            } else {
                MaplePacket packet = ListView.Selected;
                Definition definition = Config.Instance.GetDefinition(packet);
                if (definition != null) {
                    mPacketContextNameBox.Text = definition.Name;
                    mPacketContextIgnoreMenu.Checked = definition.Ignore;
                }
            }
        }

        private void mPacketContextMenu_Opened(object pSender, EventArgs pArgs) {
            mPacketContextNameBox.Focus();
            mPacketContextNameBox.SelectAll();

            ListView.Items[ListView.SelectedIndices[0]]?.EnsureVisible();
            openingContextMenu = false;
        }

        private void mPacketContextNameBox_KeyDown(object pSender, KeyEventArgs pArgs) {
            if (pArgs.Modifiers == Keys.None && pArgs.KeyCode == Keys.Enter && ListView.SelectedIndices.Count > 0) {
                int index = ListView.SelectedIndices[0];
                MaplePacket packet = ListView.Selected;
                Definition definition = Config.Instance.GetDefinition(packet);
                if (definition == null) {
                    definition = new Definition {
                        Build = Build,
                        Outbound = packet.Outbound,
                        Opcode = packet.Opcode,
                        Locale = Locale
                    };
                }

                definition.Name = mPacketContextNameBox.Text;
                SaveDefinition(definition);

                pArgs.SuppressKeyPress = true;
                mPacketContextMenu.Close();
                RefreshPackets();

                ListView.Items[index]?.EnsureVisible();
            }
        }

        private void mPacketContextIgnoreMenu_CheckedChanged(object pSender, EventArgs pArgs) {
            if (openingContextMenu || ListView.SelectedIndices.Count == 0) return;
            int index = ListView.SelectedIndices[0];
            MaplePacket packetItem = ListView.Selected;
            Definition definition =
                Config.Instance.GetDefinition(Build, Locale, packetItem.Outbound, packetItem.Opcode);
            if (definition == null) {
                definition = new Definition {
                    Locale = Locale,
                    Build = Build,
                    Outbound = packetItem.Outbound,
                    Opcode = packetItem.Opcode,
                };
            }

            definition.Ignore = mPacketContextIgnoreMenu.Checked;
            SaveDefinition(definition);

            // If viewing ignored packets, ignoring a new packet does not change view
            if (mViewIgnoredMenu.Checked) return;

            int newIndex = index - 1;
            for (int i = index - 1; i > 0; i--) {
                MaplePacket pack = ListView.FilteredPackets[i];
                Definition def = Config.Instance.GetDefinition(Build, Locale, pack.Outbound, pack.Opcode);
                if (def == definition) {
                    newIndex--;
                }
            }

            RefreshPackets();

            if (newIndex != 0 && ListView.FilteredPackets[newIndex] != null) {
                ListViewItem listItem = ListView.Items[newIndex];
                listItem.Selected = true;
                listItem.EnsureVisible();
            }
        }

        private void SessionForm_Load(object sender, EventArgs e) { }

        private void mMenu_ItemClicked(object sender, ToolStripItemClickedEventArgs e) { }

        private void sessionInformationToolStripMenuItem_Click(object sender, EventArgs e) {
            var info = new SessionInfoForm {
                txtVersion = {Text = Build.ToString()},
                txtLocale = {Text = Locale.ToString()},
                txtAdditionalInfo = {
                    Text = $"Connection info:\n {mLocalEndpoint} <-> {mRemoteEndpoint}"
                }
            };

            info.Show();
        }

        private void sendPropertiesToolStripMenuItem_Click(object sender, EventArgs e) {
            DefinitionsContainer.Instance.SaveProperties();
            string tmp = Config.GetPropertiesFile(true, (byte) Locale, Build);
            try {
                Process.Start(tmp);
            } catch {
                Process.Start("notepad", tmp);
            }
        }

        private void recvPropertiesToolStripMenuItem_Click(object sender, EventArgs e) {
            DefinitionsContainer.Instance.SaveProperties();
            string tmp = Config.GetPropertiesFile(false, (byte) Locale, Build);
            try {
                Process.Start(tmp);
            } catch {
                Process.Start("notepad", tmp);
            }
        }

        private void removeLoggedPacketsToolStripMenuItem_Click(object sender, EventArgs e) {
            const string message = "Are you sure you want to delete all logged packets?";
            DialogResult result = MessageBox.Show(message, "Warning!", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result == DialogResult.No) {
                return;
            }

            ClearedPackets = true;

            mPackets.Clear();
            ListView.Clear();
            Opcodes.Clear();
            RefreshPackets();
        }

        private void mFileSeparatorMenu_Click(object sender, EventArgs e) { }

        private void thisPacketOnlyToolStripMenuItem_Click(object sender, EventArgs e) {
            if (ListView.SelectedIndices.Count == 0) return;
            int index = ListView.SelectedIndices[0];
            ListViewItem packetItem = ListView.Items[index];
            mPackets.Remove(ListView.FilteredPackets[index]);

            packetItem.Selected = false;
            if (index > 0) {
                index--;
                packetItem = ListView.Items[index];
                packetItem.Selected = true;
            }

            RefreshPackets();
        }

        private void allBeforeThisOneToolStripMenuItem_Click(object sender, EventArgs e) { }

        private void onlyVisibleToolStripMenuItem_Click(object sender, EventArgs e) {
            if (ListView.SelectedIndices.Count == 0) return;
            int index = ListView.SelectedIndices[0];

            for (int i = 0; i < index; i++)
                mPackets.Remove(ListView.FilteredPackets[i]);
            RefreshPackets();
        }

        private void allToolStripMenuItem_Click(object sender, EventArgs e) {
            MaplePacket packet = ListView.Selected;
            if (packet == default) return;

            mPackets.RemoveRange(0, mPackets.FindIndex((p) => p == packet));
            RefreshPackets();
        }

        private void onlyVisibleToolStripMenuItem1_Click(object sender, EventArgs e) {
            if (ListView.SelectedIndices.Count == 0) return;
            int index = ListView.SelectedIndices[0];

            for (int i = index + 1; i < ListView.VirtualListSize; i++)
                mPackets.Remove(ListView.FilteredPackets[i]);
            RefreshPackets();
        }

        private void allToolStripMenuItem1_Click(object sender, EventArgs e) {
            MaplePacket packet = ListView.Selected;
            if (packet == default) return;

            int startIndex = mPackets.FindIndex((p) => p == packet) + 1;
            mPackets.RemoveRange(startIndex, mPackets.Count - startIndex);
            RefreshPackets();
        }

        private void SaveDefinition(Definition definition) {
            DefinitionsContainer.Instance.SaveDefinition(definition);
        }

        private void AddPacket(MaplePacket packetItem, bool forceAdd = false) {
            mPackets.Add(packetItem);

            Definition definition =
                Config.Instance.GetDefinition(Build, Locale, packetItem.Outbound, packetItem.Opcode);
            if (!Opcodes.Exists(op => op.Outbound == packetItem.Outbound && op.Header == packetItem.Opcode)) {
                Opcodes.Add(new Opcode(packetItem.Outbound, packetItem.Opcode));
            }

            if (!forceAdd) {
                if (definition != null && !mViewIgnoredMenu.Checked && definition.Ignore) return;
                if (packetItem.Outbound && !mViewOutboundMenu.Checked) return;
                if (!packetItem.Outbound && !mViewInboundMenu.Checked) return;
            }

            ListView.AddPacket(packetItem);
        }

        private void Terminate() {
            mTerminated = true;
            Text += " (Terminated)";
        }
    }
}