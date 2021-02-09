using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Be.Windows.Forms;
using MapleShark2.Logging;
using MapleShark2.Packet;
using MapleShark2.Tools;
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
        private uint mBuild = 0;
        private byte mLocale = 0;
        private MapleStream mOutboundStream = null;
        private MapleStream mInboundStream = null;
        private TcpReassembler tcpReassembler = new TcpReassembler();
        private List<MaplePacket> mPackets = new List<MaplePacket>();

        private List<Opcode> mOpcodes = new List<Opcode>();
        private int socks5 = 0;

        private string mRemoteEndpoint = "???";
        private string mLocalEndpoint = "???";
        private string mProxyEndpoint = "???";

        // Used for determining if the session did receive a packet at all, or if it just emptied its buffers
        public bool ClearedPackets { get; private set; }

        internal SessionForm() {
            ClearedPackets = false;
            InitializeComponent();
            ScaleColumns();
            Saved = false;
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

        public MainForm MainForm => ParentForm as MainForm;
        public PacketListView ListView => mPacketList;
        public IReadOnlyList<MaplePacket> FilteredPackets => mPacketList.FilteredPackets;
        public uint Build => mBuild;
        public byte Locale => mLocale;
        public List<Opcode> Opcodes => mOpcodes;

        public bool Saved { get; private set; }

        private DateTime startTime;

        public void UpdateOpcodeList() {
            mOpcodes = mOpcodes.OrderBy(a => a.Header).ToList();
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
            mBuild = version;
            mLocale = locale;

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

                tcpReassembler.ReassembleStream(pTcpPacket);
                return Results.Continue;
            }

            bool isOutbound = pTcpPacket.SourcePort == mLocalPort;
            tcpReassembler.ReassembleStream(pTcpPacket);
            Queue<byte[]> queue = isOutbound ? tcpReassembler.OutQueue : tcpReassembler.InQueue;
            while (queue.Count > 0) {
                Results result = ProcessTcpPacket(queue.Dequeue(), isOutbound, pArrivalTime);
                if (result != Results.Continue) {
                    return result;
                }
            }

            return Results.Continue;
        }

        // TcpPacket ordering is guaranteed at this point
        private Results ProcessTcpPacket(byte[] tcpData, bool isOutbound, DateTime pArrivalTime) {
            if (mTerminated) return Results.Terminated;

            if (mBuild == 0) {
                byte[] headerData = new byte[tcpData.Length];
                Buffer.BlockCopy(tcpData, 0, headerData, 0, tcpData.Length);

                PacketReader pr = new PacketReader(headerData);
                ushort rawSeq = pr.ReadUShort();
                int length = pr.ReadInt();
                if (headerData.Length - 6 < length) {
                    Console.WriteLine("Connection on port {0} did not have a MapleStory2 Handshake", mLocalEndpoint);
                    return Results.CloseMe;
                }

                ushort header = pr.ReadUShort();
                if (header != 1) //RequestVersion
                {
                    Console.WriteLine("Connection on port {0} did not have a valid MapleStory2 Connection Header",
                        mLocalEndpoint);
                    return Results.CloseMe;
                }

                uint version = pr.ReadUInt();
                uint localIV = pr.ReadUInt();
                uint remoteIV = pr.ReadUInt();
                uint blockIV = pr.ReadUInt();
                byte ignored = pr.ReadByte();

                mBuild = version;
                mLocale = 0; //TODO: Handle regions somehow since handshake doesn't contain it

                mOutboundStream = new MapleStream(true, mBuild, localIV, blockIV);
                mInboundStream = new MapleStream(false, mBuild, remoteIV, blockIV);

                if (mInboundStream.DecodeSeqBase(rawSeq) != version) {
                    Console.WriteLine("Connection on port {0} has invalid header sequence number", mLocalEndpoint);
                    return Results.CloseMe;
                }

                // Another packet was sent with handshake...
                if (pr.Remaining > 0) {
                    // Buffer it since it is encrypted
                    mInboundStream.Append(pr.ReadBytes(pr.Remaining));
                }

                // Generate HandShake packet
                Definition definition = Config.Instance.GetDefinition(mBuild, mLocale, false, header);
                if (definition == null) {
                    definition = new Definition {
                        Outbound = false,
                        Locale = mLocale,
                        Opcode = header,
                        Name = "RequestVersion",
                        Build = mBuild
                    };
                    SaveDefinition(definition);
                }

                {
                    var filename = Helpers.GetScriptPath(mLocale, mBuild, false, header);
                    Helpers.MakeSureFileDirectoryExists(filename);

                    // Create main script
                    if (!File.Exists(filename)) {
                        string contents = @"using (ScriptAPI) {
                                                AddShort(""Raw Sequence"");
                                                AddField(""Packet Length"", 4);
                                                AddShort(""Opcode"");
                                                AddField(""MapleStory2 Version"", 4);
                                                AddField(""Local Initializing Vector (IV)"", 4);
                                                AddField(""Remote Initializing Vector (IV)"", 4);
                                                AddField(""Block Initializing Vector (IV)"", 4);
                                            }";
                        File.WriteAllText(filename, contents);
                    }
                }

                // Initial TCP packet may not be split up properly, copy only handshake portion
                byte[] handshakePacketData = new byte[6 + length];
                Buffer.BlockCopy(tcpData, 0, handshakePacketData, 0, length + 6);

                MaplePacket packetItem =
                    new MaplePacket(pArrivalTime, false, mBuild, header, handshakePacketData, 0, remoteIV);
                if (!mOpcodes.Exists(op => op.Outbound == packetItem.Outbound && op.Header == packetItem.Opcode)
                ) // Should be false, but w/e
                {
                    mOpcodes.Add(new Opcode(packetItem.Outbound, packetItem.Opcode));
                }

                AddPacket(packetItem, true);

                Console.WriteLine("[CONNECTION] MapleStory2 V{0}", mBuild);

                ProcessPacketBuffer(mInboundStream, pArrivalTime);
                return Results.Show;
            }

            MapleStream stream = isOutbound ? mOutboundStream : mInboundStream;
            stream.Append(tcpData);
            ProcessPacketBuffer(stream, pArrivalTime);
            return Results.Continue;
        }

        private void ProcessPacketBuffer(MapleStream pStream, DateTime pArrivalDate) {
            if (mTerminated) return;

            int opcodeCount = mOpcodes.Count;
            try {
                mPacketList.BeginUpdate();
                while (pStream.TryRead(pArrivalDate, out MaplePacket packetItem)) {
                    AddPacket(packetItem);
                }

                mPacketList.EndUpdate();

                // This should be called after EndUpdate so VirtualListSize is set properly
                if (mPacketList.SelectedIndices.Count == 0 && FilteredPackets.Count > 0) {
                    mPacketList.Items[FilteredPackets.Count - 1]?.EnsureVisible();
                }
            } catch (Exception ex) {
                Console.WriteLine(ex);
                Terminate();
                return;
            }

            if (DockPanel != null && DockPanel.ActiveDocument == this && opcodeCount != mOpcodes.Count) {
                MainForm.SearchForm.RefreshOpcodes(true);
            }
        }

        public void OpenReadOnly(string pFilename) {
            // mFileSaveMenu.Enabled = false;
            Saved = true;

            mTerminated = true;
            using (FileStream stream = new FileStream(pFilename, FileMode.Open, FileAccess.Read)) {
                BinaryReader reader = new BinaryReader(stream);
                ushort MapleSharkVersion = reader.ReadUInt16();
                mBuild = MapleSharkVersion;
                if (MapleSharkVersion < 0x2000) {
                    mLocalPort = reader.ReadUInt16();
                    // Old version
                    frmLocale loc = new frmLocale();
                    var res = loc.ShowDialog();
                    if (res == DialogResult.OK) {
                        mLocale = loc.ChosenLocale;
                    }
                } else {
                    byte v1 = (byte) ((MapleSharkVersion >> 12) & 0xF),
                        v2 = (byte) ((MapleSharkVersion >> 8) & 0xF),
                        v3 = (byte) ((MapleSharkVersion >> 4) & 0xF),
                        v4 = (byte) ((MapleSharkVersion >> 0) & 0xF);
                    Console.WriteLine("Loading MSB file, saved by MapleShark V{0}.{1}.{2}.{3}", v1, v2, v3, v4);

                    if (MapleSharkVersion == 0x2012) {
                        mLocale = (byte) reader.ReadUInt16();
                        mBuild = reader.ReadUInt16();
                        mLocalPort = reader.ReadUInt16();
                    } else if (MapleSharkVersion == 0x2014) {
                        mLocalEndpoint = reader.ReadString();
                        mLocalPort = reader.ReadUInt16();
                        mRemoteEndpoint = reader.ReadString();
                        mRemotePort = reader.ReadUInt16();

                        mLocale = (byte) reader.ReadUInt16();
                        mBuild = reader.ReadUInt16();
                    } else if (MapleSharkVersion == 0x2015 || MapleSharkVersion >= 0x2020) {
                        mLocalEndpoint = reader.ReadString();
                        mLocalPort = reader.ReadUInt16();
                        mRemoteEndpoint = reader.ReadString();
                        mRemotePort = reader.ReadUInt16();

                        mLocale = reader.ReadByte();
                        mBuild = reader.ReadUInt32();
                    } else {
                        MessageBox.Show("I have no idea how to open this MSB file. It looks to me as a version "
                                        + $"{v1}.{v2}.{v3}.{v4}"
                                        + " MapleShark MSB file... O.o?!");
                        return;
                    }
                }

                mPacketList.BeginUpdate();
                while (stream.Position < stream.Length) {
                    long timestamp = reader.ReadInt64();
                    int size = MapleSharkVersion < 0x2027 ? reader.ReadUInt16() : reader.ReadInt32();
                    ushort opcode = reader.ReadUInt16();
                    bool outbound;

                    if (MapleSharkVersion >= 0x2020) {
                        outbound = reader.ReadBoolean();
                    } else {
                        outbound = (size & 0x8000) != 0;
                        size = (ushort) (size & 0x7FFF);
                    }

                    byte[] buffer = reader.ReadBytes(size);

                    uint preDecodeIV = 0, postDecodeIV = 0;
                    if (MapleSharkVersion >= 0x2025) {
                        preDecodeIV = reader.ReadUInt32();
                        postDecodeIV = reader.ReadUInt32();
                    }

                    var packet = new MaplePacket(new DateTime(timestamp), outbound, mBuild, opcode, buffer, preDecodeIV,
                        postDecodeIV);
                    AddPacket(packet);
                }

                mPacketList.EndUpdate();
                if (mPacketList.VirtualListSize > 0) mPacketList.EnsureVisible(0);
            }

            Text = $"{Path.GetFileName(pFilename)} (ReadOnly)";
            Console.WriteLine("Loaded file: {0}", pFilename);
        }

        public void RefreshPackets() {
            mPacketList.BeginUpdate();

            MaplePacket previous = mPacketList.SelectedIndices.Count > 0
                ? FilteredPackets[mPacketList.SelectedIndices[0]]
                : null;
            mOpcodes.Clear();
            mPacketList.Clear();

            MainForm.DataForm.HexBox.ByteProvider = null;
            MainForm.StructureForm.Tree.Nodes.Clear();
            MainForm.PropertyForm.Properties.SelectedObject = null;

            if (!mViewOutboundMenu.Checked && !mViewInboundMenu.Checked) return;
            int previousIndex = -1;
            foreach (MaplePacket packet in mPackets) {
                if (packet.Outbound && !mViewOutboundMenu.Checked) continue;
                if (!packet.Outbound && !mViewInboundMenu.Checked) continue;
                if (!mOpcodes.Exists(op => op.Outbound == packet.Outbound && op.Header == packet.Opcode)) {
                    mOpcodes.Add(new Opcode(packet.Outbound, packet.Opcode));
                }

                Definition definition = Config.Instance.GetDefinition(packet);
                if (definition != null && !mViewIgnoredMenu.Checked && definition.Ignore) continue;

                int index = mPacketList.AddPacket(packet);
                if (packet == previous) {
                    previousIndex = index;
                }
            }

            MainForm.SearchForm.RefreshOpcodes(true);

            mPacketList.EndUpdate();

            // This should be called after EndUpdate so VirtualListSize is set properly
            if (previous != null && previousIndex >= 0) {
                mPacketList.Items[previousIndex].Selected = true;
                mPacketList.Items[previousIndex].EnsureVisible();
            }
        }

        public void ReselectPacket() {
            mPacketList_SelectedIndexChanged(null, null);
        }

        private static Regex _packetRegex = new Regex(@"\[(.{1,2}):(.{1,2}):(.{1,2})\]\[(\d+)\] (Recv|Send):  (.+)");

        internal void ParseMSnifferLine(string packetLine) {
            var match = _packetRegex.Match(packetLine);
            if (match.Captures.Count == 0) return;
            DateTime date = new DateTime(
                2012,
                10,
                10,
                int.Parse(match.Groups[1].Value),
                int.Parse(match.Groups[2].Value),
                int.Parse(match.Groups[3].Value)
            );
            int packetLength = int.Parse(match.Groups[4].Value);
            byte[] buffer = new byte[packetLength - 2];
            bool outbound = match.Groups[5].Value == "Send";
            string[] bytesText = match.Groups[6].Value.Split(new char[] {' '}, StringSplitOptions.RemoveEmptyEntries);

            ushort opcode = (ushort) (byte.Parse(bytesText[0], NumberStyles.HexNumber)
                                      | byte.Parse(bytesText[1], NumberStyles.HexNumber) << 8);

            for (var i = 2; i < packetLength; i++) {
                buffer[i - 2] = byte.Parse(bytesText[i], NumberStyles.HexNumber);
            }

            var packet = new MaplePacket(date, outbound, mBuild, opcode, buffer, 0, 0);
            AddPacket(packet);
        }

        public void RunSaveCMD() {
            mFileSaveMenu.PerformClick();
        }

        private void mFileSaveMenu_Click(object pSender, EventArgs pArgs) {
            if (mFilename == null) {
                mSaveDialog.FileName = $"Port {mLocalPort}";
                if (mSaveDialog.ShowDialog(this) == DialogResult.OK) mFilename = mSaveDialog.FileName;
                else return;
            }

            using (FileStream stream = new FileStream(mFilename, FileMode.Create, FileAccess.Write)) {
                var version = (ushort) 0x2027;

                BinaryWriter writer = new BinaryWriter(stream);
                writer.Write(version);
                writer.Write(mLocalEndpoint);
                writer.Write(mLocalPort);
                writer.Write(mRemoteEndpoint);
                writer.Write(mRemotePort);
                writer.Write(mLocale);
                writer.Write(mBuild);

                mPackets.ForEach(p => {
                    writer.Write((ulong) p.Timestamp.Ticks);
                    writer.Write((int) p.Length);
                    writer.Write((ushort) p.Opcode);
                    writer.Write((byte) (p.Outbound ? 1 : 0));
                    writer.Write(p.Buffer);
                    writer.Write(p.PreDecodeIV);
                    writer.Write(p.PostDecodeIV);
                });

                stream.Flush();
            }

            if (mTerminated) {
                mFileSaveMenu.Enabled = false;
                Text += " (ReadOnly)";
            }

            Saved = true;
        }

        private void mFileExportMenu_Click(object pSender, EventArgs pArgs) {
            mExportDialog.FileName = $"Port {mLocalPort}";
            if (mExportDialog.ShowDialog(this) != DialogResult.OK) return;

            bool includeNames =
                MessageBox.Show("Export opcode names? (slow + generates big files!!!)", "-", MessageBoxButtons.YesNo)
                == DialogResult.Yes;

            string tmp = "";
            tmp += $"=== MapleStory2 Version: {mBuild}; Region: {mLocale} ===\r\n";
            tmp += $"Endpoint From: {mLocalEndpoint}\r\n";
            tmp += $"Endpoint To: {mRemoteEndpoint}\r\n";
            tmp += $"- Packets: {mPackets.Count}\r\n";

            long dataSize = 0;
            foreach (var packet in mPackets)
                dataSize += 2 + packet.Buffer.Length;

            tmp += $"- Data: {dataSize:N0} bytes\r\n";
            tmp += string.Format("================================================\r\n");
            File.WriteAllText(mExportDialog.FileName, tmp);

            tmp = "";

            int outboundCount = 0;
            int inboundCount = 0;
            int i = 0;
            foreach (var packet in mPackets) {
                if (packet.Outbound) ++outboundCount;
                else ++inboundCount;

                Definition definition = Config.Instance.GetDefinition(packet);

                tmp += string.Format("[{0:yyyy-MM-dd HH:mm:ss.fff}][{1}] [{2:X4}{4}] {3}\r\n", packet.Timestamp,
                    (packet.Outbound ? "OUT" : "IN "),
                    packet.Opcode,
                    BitConverter.ToString(packet.Buffer).Replace('-', ' '),
                    includeNames ? " | " + (definition == null ? "N/A" : definition.Name) : "");
                i++;
                if (i % 1000 == 0) {
                    File.AppendAllText(mExportDialog.FileName, tmp);
                    tmp = "";
                }
            }

            File.AppendAllText(mExportDialog.FileName, tmp);
        }

        private void mViewCommonScriptMenu_Click(object pSender, EventArgs pArgs) {
            var scriptPath = Helpers.GetCommonScriptPath(mLocale, mBuild);
            Helpers.MakeSureFileDirectoryExists(scriptPath);

            ScriptForm script = new ScriptForm(scriptPath, null);
            script.FormClosed += CommonScript_FormClosed;
            script.Show(DockPanel, new Rectangle(MainForm.Location, new Size(600, 300)));
        }

        private void CommonScript_FormClosed(object pSender, FormClosedEventArgs pArgs) {
            if (mPacketList.SelectedIndices.Count == 0) return;
            MaplePacket packet = mPacketList.Selected;
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
            if (mPacketList.SelectedIndices.Count == 0) {
                MainForm.DataForm.HexBox.ByteProvider = null;
                MainForm.StructureForm.Tree.Nodes.Clear();
                MainForm.PropertyForm.Properties.SelectedObject = null;
                return;
            }

            MainForm.DataForm.HexBox.ByteProvider = new DynamicByteProvider(mPacketList.Selected.Buffer);
            MainForm.StructureForm.ParseMaplePacket(mPacketList.Selected);
        }

        private void mPacketList_ItemActivate(object pSender, EventArgs pArgs) {
            if (mPacketList.SelectedIndices.Count == 0) return;
            MaplePacket packet = mPacketList.Selected;

            var scriptPath = Helpers.GetScriptPath(mLocale, mBuild, packet.Outbound, packet.Opcode);
            Helpers.MakeSureFileDirectoryExists(scriptPath);

            ScriptForm script = new ScriptForm(scriptPath, packet);
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
            if (mPacketList.SelectedIndices.Count == 0) {
                pArgs.Cancel = true;
            } else {
                MaplePacket packet = mPacketList.Selected;
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

            mPacketList.Items[mPacketList.SelectedIndices[0]]?.EnsureVisible();
            openingContextMenu = false;
        }

        private void mPacketContextNameBox_KeyDown(object pSender, KeyEventArgs pArgs) {
            if (pArgs.Modifiers == Keys.None && pArgs.KeyCode == Keys.Enter && mPacketList.SelectedIndices.Count > 0) {
                int index = mPacketList.SelectedIndices[0];
                MaplePacket packet = mPacketList.Selected;
                Definition definition = Config.Instance.GetDefinition(packet);
                if (definition == null) {
                    definition = new Definition {
                        Build = mBuild,
                        Outbound = packet.Outbound,
                        Opcode = packet.Opcode,
                        Locale = mLocale
                    };
                }

                definition.Name = mPacketContextNameBox.Text;
                SaveDefinition(definition);

                pArgs.SuppressKeyPress = true;
                mPacketContextMenu.Close();
                RefreshPackets();

                mPacketList.Items[index]?.EnsureVisible();
            }
        }

        private void mPacketContextIgnoreMenu_CheckedChanged(object pSender, EventArgs pArgs) {
            if (openingContextMenu || mPacketList.SelectedIndices.Count == 0) return;
            int index = mPacketList.SelectedIndices[0];
            MaplePacket packetItem = mPacketList.Selected;
            Definition definition =
                Config.Instance.GetDefinition(mBuild, mLocale, packetItem.Outbound, packetItem.Opcode);
            if (definition == null) {
                definition = new Definition {
                    Locale = mLocale,
                    Build = mBuild,
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
                MaplePacket pack = mPacketList.FilteredPackets[i];
                Definition def = Config.Instance.GetDefinition(mBuild, mLocale, pack.Outbound, pack.Opcode);
                if (def == definition) {
                    newIndex--;
                }
            }

            RefreshPackets();

            if (newIndex != 0 && mPacketList.FilteredPackets[newIndex] != null) {
                ListViewItem listItem = mPacketList.Items[newIndex];
                listItem.Selected = true;
                listItem.EnsureVisible();
            }
        }

        private void SessionForm_Load(object sender, EventArgs e) { }

        private void mMenu_ItemClicked(object sender, ToolStripItemClickedEventArgs e) { }

        private void sessionInformationToolStripMenuItem_Click(object sender, EventArgs e) {
            var info = new SessionInformation {
                txtVersion = {Text = mBuild.ToString()},
                txtLocale = {Text = mLocale.ToString()},
                txtAdditionalInfo = {
                    Text = "Connection info:\r\n"
                           + mLocalEndpoint
                           + " <-> "
                           + mRemoteEndpoint
                           + (mProxyEndpoint != "???" ? "\r\nProxy:" + mProxyEndpoint : "")
                }
            };

            info.Show();
        }

        private void sendPropertiesToolStripMenuItem_Click(object sender, EventArgs e) {
            DefinitionsContainer.Instance.SaveProperties();
            string tmp = Config.GetPropertiesFile(true, (byte) mLocale, mBuild);
            Process.Start(tmp);
        }

        private void recvPropertiesToolStripMenuItem_Click(object sender, EventArgs e) {
            DefinitionsContainer.Instance.SaveProperties();
            string tmp = Config.GetPropertiesFile(false, (byte) mLocale, mBuild);
            Process.Start(tmp);
        }

        private void removeLoggedPacketsToolStripMenuItem_Click(object sender, EventArgs e) {
            DialogResult result = MessageBox.Show("Are you sure you want to delete all logged packets?", "!!",
                MessageBoxButtons.YesNo);
            if (result == DialogResult.No) {
                return;
            }

            ClearedPackets = true;

            mPackets.Clear();
            mPacketList.Clear();
            mOpcodes.Clear();
            RefreshPackets();
        }

        private void mFileSeparatorMenu_Click(object sender, EventArgs e) { }

        private void thisPacketOnlyToolStripMenuItem_Click(object sender, EventArgs e) {
            if (mPacketList.SelectedIndices.Count == 0) return;
            int index = mPacketList.SelectedIndices[0];
            ListViewItem packetItem = mPacketList.Items[index];
            mPackets.Remove(mPacketList.FilteredPackets[index]);

            packetItem.Selected = false;
            if (index > 0) {
                index--;
                packetItem = mPacketList.Items[index];
                packetItem.Selected = true;
            }

            RefreshPackets();
        }

        private void allBeforeThisOneToolStripMenuItem_Click(object sender, EventArgs e) { }

        private void onlyVisibleToolStripMenuItem_Click(object sender, EventArgs e) {
            if (mPacketList.SelectedIndices.Count == 0) return;
            int index = mPacketList.SelectedIndices[0];

            for (int i = 0; i < index; i++)
                mPackets.Remove(mPacketList.FilteredPackets[i]);
            RefreshPackets();
        }

        private void allToolStripMenuItem_Click(object sender, EventArgs e) {
            MaplePacket packet = mPacketList.Selected;
            if (packet == default) return;

            mPackets.RemoveRange(0, mPackets.FindIndex((p) => p == packet));
            RefreshPackets();
        }

        private void onlyVisibleToolStripMenuItem1_Click(object sender, EventArgs e) {
            if (mPacketList.SelectedIndices.Count == 0) return;
            int index = mPacketList.SelectedIndices[0];

            for (int i = index + 1; i < mPacketList.VirtualListSize; i++)
                mPackets.Remove(mPacketList.FilteredPackets[i]);
            RefreshPackets();
        }

        private void allToolStripMenuItem1_Click(object sender, EventArgs e) {
            MaplePacket packet = mPacketList.Selected;
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
                Config.Instance.GetDefinition(mBuild, mLocale, packetItem.Outbound, packetItem.Opcode);
            if (!mOpcodes.Exists(op => op.Outbound == packetItem.Outbound && op.Header == packetItem.Opcode)) {
                mOpcodes.Add(new Opcode(packetItem.Outbound, packetItem.Opcode));
            }

            if (!forceAdd) {
                if (definition != null && !mViewIgnoredMenu.Checked && definition.Ignore) return;
                if (packetItem.Outbound && !mViewOutboundMenu.Checked) return;
                if (!packetItem.Outbound && !mViewInboundMenu.Checked) return;
            }

            mPacketList.AddPacket(packetItem);
        }

        private void Terminate() {
            mTerminated = true;
            Text += " (Terminated)";
        }
    }
}