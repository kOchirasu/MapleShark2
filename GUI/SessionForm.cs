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
using MapleLib.PacketLib;
using PacketDotNet;
using WeifenLuo.WinFormsUI.Docking;

namespace MapleShark
{
    public partial class SessionForm : DockContent
    {
        public enum Results
        {
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
        private uint mOutboundSequence = 0;
        private uint mInboundSequence = 0;
        private uint mBuild = 0;
        private byte mLocale = 0;
        private string mPatchLocation = "";
        private SortedSet<TcpPacket> outboundTcpBuffer = new SortedSet<TcpPacket>(new TcpSequenceComparer());
        private SortedSet<TcpPacket> inboundTcpBuffer = new SortedSet<TcpPacket>(new TcpSequenceComparer());
        private MapleStream mOutboundStream = null;
        private MapleStream mInboundStream = null;
        private List<MaplePacket> mPackets = new List<MaplePacket>();
        private List<Opcode> mOpcodes = new List<Opcode>();
        private int socks5 = 0;

        private string mRemoteEndpoint = "???";
        private string mLocalEndpoint = "???";
        private string mProxyEndpoint = "???";

        // Used for determining if the session did receive a packet at all, or if it just emptied its buffers
        public bool ClearedPackets { get; private set; }

        internal SessionForm()
        {
            ClearedPackets = false;
            InitializeComponent();
            Saved = false;
        }

        public MainForm MainForm => ParentForm as MainForm;
        public ListView ListView => mPacketList;
        public uint Build => mBuild;
        public byte Locale => mLocale;
        public List<Opcode> Opcodes => mOpcodes;

        public bool Saved { get; private set; }

        private DateTime startTime;

        public void UpdateOpcodeList()
        {
            mOpcodes = mOpcodes.OrderBy(a => a.Header).ToList();
        }


        internal bool MatchTCPPacket(TcpPacket pTCPPacket)
        {
            if (mTerminated) return false;
            if (pTCPPacket.SourcePort == mLocalPort && pTCPPacket.DestinationPort == (mProxyPort > 0 ? mProxyPort : mRemotePort)) return true;
            if (pTCPPacket.SourcePort == (mProxyPort > 0 ? mProxyPort : mRemotePort) && pTCPPacket.DestinationPort == mLocalPort) return true;
            return false;
        }

        internal bool CloseMe(DateTime pTime)
        {
            if (!ClearedPackets && mPackets.Count == 0 && (pTime - startTime).TotalSeconds >= 5)
            {
                return true;
            }
            return false;
        }

        internal void SetMapleInfo(ushort version, string patchLocation, byte locale, string ip, ushort port)
        {
            if (mPackets.Count > 0) return;
            mBuild = version;
            mPatchLocation = patchLocation;
            mLocale = locale;

            mRemoteEndpoint = ip;
            mRemotePort = port;

            mLocalEndpoint = "127.0.0.1";
            mLocalPort = 10000;
        }

        internal Results BufferTcpPacket(TcpPacket pTcpPacket, DateTime pArrivalTime) {
            if (pTcpPacket.Fin || pTcpPacket.Rst) {
                Terminate();

                return mPackets.Count == 0 ? Results.CloseMe : Results.Terminated;
            }

            if (pTcpPacket.Syn && !pTcpPacket.Ack)
            {
                mLocalPort = (ushort)pTcpPacket.SourcePort;
                mRemotePort = (ushort)pTcpPacket.DestinationPort;
                mOutboundSequence = (uint)(pTcpPacket.SequenceNumber + 1);
                Text = "Port " + mLocalPort + " - " + mRemotePort;
                startTime = DateTime.Now;

                try
                {
                    mRemoteEndpoint = ((IPv4Packet)pTcpPacket.ParentPacket).SourceAddress + ":" + pTcpPacket.SourcePort;
                    mLocalEndpoint = ((IPv4Packet)pTcpPacket.ParentPacket).DestinationAddress + ":" + pTcpPacket.DestinationPort;
                    Console.WriteLine("[CONNECTION] From {0} to {1}", mRemoteEndpoint, mLocalEndpoint);

                    return Results.Continue;
                }
                catch
                {
                    return Results.CloseMe;
                }
            }
            if (pTcpPacket.Syn && pTcpPacket.Ack) { mInboundSequence = (uint)(pTcpPacket.SequenceNumber + 1); return Results.Continue; }
            if (pTcpPacket.PayloadData.Length == 0) return Results.Continue;

            bool isOutbound = pTcpPacket.SourcePort == mLocalPort;
            uint expectedSequence = isOutbound ? mOutboundSequence : mInboundSequence;
            // Optimization to avoid buffering packets when not necessary
            if (pTcpPacket.SequenceNumber == expectedSequence) {
                if (isOutbound) mOutboundSequence += (uint) pTcpPacket.PayloadData.Length;
                else mInboundSequence += (uint) pTcpPacket.PayloadData.Length;

                return ProcessTcpPacket(pTcpPacket, pArrivalTime);
            }

            // Buffer packet to guarantee ordering
            var tcpBuffer = isOutbound ? outboundTcpBuffer : inboundTcpBuffer;
            lock (tcpBuffer) {
                tcpBuffer.Add(pTcpPacket);

                TcpPacket bufferedPacket;
                // Remove any retransmitted packets that were already processed.
                while (tcpBuffer.Count > 0 && (bufferedPacket = tcpBuffer.Min()).SequenceNumber < expectedSequence) {
                    tcpBuffer.Remove(bufferedPacket);
                }
                // Process all buffered packets.
                while (tcpBuffer.Count > 0 && (bufferedPacket = tcpBuffer.Min()).SequenceNumber == expectedSequence) {
                    tcpBuffer.Remove(bufferedPacket);
                    if (isOutbound) mOutboundSequence += (uint) bufferedPacket.PayloadData.Length;
                    else mInboundSequence += (uint) bufferedPacket.PayloadData.Length;
                    expectedSequence += (uint) bufferedPacket.PayloadData.Length;

                    Results result = ProcessTcpPacket(bufferedPacket, pArrivalTime);
                    if (result != Results.Continue) {
                        return result;
                    }
                }
            }
            return Results.Continue;
        }

        // TcpPacket ordering is guaranteed at this point
        private Results ProcessTcpPacket(TcpPacket pTcpPacket, DateTime pArrivalTime)
        {
            if (!Config.Instance.Maple2) {
                throw new ArgumentException("Only Config.Maple2 is supported.");
            }

            if (mBuild == 0)
            {
                byte[] tcpData = pTcpPacket.PayloadData;

                byte[] headerData = new byte[tcpData.Length];
                Buffer.BlockCopy(tcpData, 0, headerData, 0, tcpData.Length);

                PacketReader pr = new PacketReader(headerData);
                ushort rawSeq = pr.ReadUShort();
                int length = pr.ReadInt();
                if (headerData.Length - 6 < length)
                {
                    Console.WriteLine("Connection on port {0} did not have a MapleStory2 Handshake", mLocalEndpoint);
                    return Results.CloseMe;
                }

                ushort header = pr.ReadUShort();
                if (header != 1)//RequestVersion
                {
                    Console.WriteLine("Connection on port {0} did not have a valid MapleStory2 Connection Header", mLocalEndpoint);
                    return Results.CloseMe;
                }
                uint version = pr.ReadUInt();
                uint localIV = pr.ReadUInt();
                uint remoteIV = pr.ReadUInt();
                uint blockIV = pr.ReadUInt();
                byte ignored = pr.ReadByte();

                mBuild = version;
                mLocale = 0;//TODO: Handle regions somehow since handshake doesn't contain it
                mPatchLocation = "MST";

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
                if (definition == null)
                {
                    definition = new Definition();
                    definition.Outbound = false;
                    definition.Locale = mLocale;
                    definition.Opcode = header;
                    definition.Name = "RequestVersion";
                    definition.Build = mBuild;
                    SaveDefinition(definition);
                }

                {
                    var filename = Helpers.GetScriptPath(mLocale, mBuild, false, header);
                    Helpers.MakeSureFileDirectoryExists(filename);

                    // Create main script
                    if (!File.Exists(filename))
                    {
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

                MaplePacket packet = new MaplePacket(pArrivalTime, false, mBuild, mLocale, header, definition == null ? "" : definition.Name, handshakePacketData, (uint)0, remoteIV);
                if (!mOpcodes.Exists(op => op.Outbound == packet.Outbound && op.Header == packet.Opcode)) // Should be false, but w/e
                {
                    mOpcodes.Add(new Opcode(packet.Outbound, packet.Opcode));
                }

                mPacketList.Items.Add(packet);
                AddPacket(packet);

                Console.WriteLine("[CONNECTION] MapleStory2 V{2}", mLocalEndpoint, mRemoteEndpoint, mBuild);

                ProcessPacketBuffer(mInboundStream, pArrivalTime);
                return Results.Show;
            }

            if (pTcpPacket.SourcePort == mLocalPort) {
                mOutboundStream.Append(pTcpPacket.PayloadData);
                ProcessPacketBuffer(mOutboundStream, pArrivalTime);
            } else {
                mInboundStream.Append(pTcpPacket.PayloadData);
                ProcessPacketBuffer(mInboundStream, pArrivalTime);
            }
            return Results.Continue;
        }

        private void ProcessPacketBuffer(MapleStream pStream, DateTime pArrivalDate) {
            bool refreshOpcodes = false;
            try
            {
                mPacketList.BeginUpdate();

                MaplePacket packet;
                while ((packet = pStream.Read(pArrivalDate)) != null)
                {
                    AddPacket(packet);
                    Definition definition = Config.Instance.GetDefinition(mBuild, mLocale, packet.Outbound, packet.Opcode);
                    if (!mOpcodes.Exists(op => op.Outbound == packet.Outbound && op.Header == packet.Opcode))
                    {
                        mOpcodes.Add(new Opcode(packet.Outbound, packet.Opcode));
                        refreshOpcodes = true;
                    }
                    if (definition != null && !mViewIgnoredMenu.Checked && definition.Ignore) continue;
                    if (packet.Outbound && !mViewOutboundMenu.Checked) continue;
                    if (!packet.Outbound && !mViewInboundMenu.Checked) continue;

                    var item = mPacketList.Items.Add(packet);
                    if (packet.Outbound) {
                        item.BackColor = Color.AliceBlue;
                    }
                    if (mPacketList.SelectedItems.Count == 0) packet.EnsureVisible();
                }

                mPacketList.EndUpdate();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Terminate();
                return;
            }

            if (DockPanel != null && DockPanel.ActiveDocument == this && refreshOpcodes) {
                MainForm.SearchForm.RefreshOpcodes(true);
            }
        }

        public void OpenReadOnly(string pFilename)
        {
            // mFileSaveMenu.Enabled = false;
            Saved = true;

            mTerminated = true;
            using (FileStream stream = new FileStream(pFilename, FileMode.Open, FileAccess.Read))
            {
                BinaryReader reader = new BinaryReader(stream);
                ushort MapleSharkVersion = reader.ReadUInt16();
                mBuild = MapleSharkVersion;
                if (MapleSharkVersion < 0x2000)
                {

                    mLocalPort = reader.ReadUInt16();
                    // Old version
                    frmLocale loc = new frmLocale();
                    var res = loc.ShowDialog();
                    if (res == DialogResult.OK)
                    {
                        mLocale = loc.ChosenLocale;
                    }
                }
                else
                {
                    byte v1 = (byte)((MapleSharkVersion >> 12) & 0xF),
                           v2 = (byte)((MapleSharkVersion >> 8) & 0xF),
                           v3 = (byte)((MapleSharkVersion >> 4) & 0xF),
                           v4 = (byte)((MapleSharkVersion >> 0) & 0xF);
                    Console.WriteLine("Loading MSB file, saved by MapleShark V{0}.{1}.{2}.{3}", v1, v2, v3, v4);

                    if (MapleSharkVersion == 0x2012)
                    {
                        mLocale = (byte)reader.ReadUInt16();
                        mBuild = reader.ReadUInt16();
                        mLocalPort = reader.ReadUInt16();
                    }
                    else if (MapleSharkVersion == 0x2014)
                    {
                        mLocalEndpoint = reader.ReadString();
                        mLocalPort = reader.ReadUInt16();
                        mRemoteEndpoint = reader.ReadString();
                        mRemotePort = reader.ReadUInt16();

                        mLocale = (byte)reader.ReadUInt16();
                        mBuild = reader.ReadUInt16();
                    }
                    else if (MapleSharkVersion == 0x2015 || MapleSharkVersion >= 0x2020)
                    {
                        mLocalEndpoint = reader.ReadString();
                        mLocalPort = reader.ReadUInt16();
                        mRemoteEndpoint = reader.ReadString();
                        mRemotePort = reader.ReadUInt16();

                        mLocale = reader.ReadByte();
                        mBuild = reader.ReadUInt32();

                        if (MapleSharkVersion >= 0x2021 && !Config.Instance.Maple2)
                        {
                            mPatchLocation = reader.ReadString();
                        }
                    }
                    else
                    {
                        MessageBox.Show("I have no idea how to open this MSB file. It looks to me as a version " + string.Format("{0}.{1}.{2}.{3}", v1, v2, v3, v4) + " MapleShark MSB file... O.o?!");
                        return;
                    }
                }

                mPacketList.BeginUpdate();
                while (stream.Position < stream.Length)
                {
                    long timestamp = reader.ReadInt64();
                    int size = MapleSharkVersion < 0x2027 ? reader.ReadUInt16() : reader.ReadInt32();
                    ushort opcode = reader.ReadUInt16();
                    bool outbound;

                    if (MapleSharkVersion >= 0x2020)
                    {
                        outbound = reader.ReadBoolean();
                    }
                    else
                    {
                        outbound = (size & 0x8000) != 0;
                        size = (ushort)(size & 0x7FFF);
                    }

                    byte[] buffer = reader.ReadBytes(size);

                    uint preDecodeIV = 0, postDecodeIV = 0;
                    if (MapleSharkVersion >= 0x2025)
                    {
                        preDecodeIV = reader.ReadUInt32();
                        postDecodeIV = reader.ReadUInt32();
                    }

                    Definition definition = Config.Instance.GetDefinition(mBuild, mLocale, outbound, opcode);
                    MaplePacket packet = new MaplePacket(new DateTime(timestamp), outbound, mBuild, mLocale, opcode, definition == null ? "" : definition.Name, buffer, preDecodeIV, postDecodeIV);
                    AddPacket(packet);
                    if (!mOpcodes.Exists(op => op.Outbound == packet.Outbound && op.Header == packet.Opcode)) mOpcodes.Add(new Opcode(packet.Outbound, packet.Opcode));
                    if (definition != null && definition.Ignore) continue;
                    mPacketList.Items.Add(packet);
                }
                mPacketList.EndUpdate();
                if (mPacketList.Items.Count > 0) mPacketList.EnsureVisible(0);
            }

            Text = string.Format("{0} (ReadOnly)", Path.GetFileName(pFilename));
            Console.WriteLine("Loaded file: {0}", pFilename);
        }

        public void RefreshPackets()
        {

            Opcode search = (MainForm.SearchForm.ComboBox.SelectedIndex >= 0 ? mOpcodes[MainForm.SearchForm.ComboBox.SelectedIndex] : null);
            MaplePacket previous = mPacketList.SelectedItems.Count > 0 ? mPacketList.SelectedItems[0] as MaplePacket : null;
            mOpcodes.Clear();
            mPacketList.Items.Clear();

            MainForm.DataForm.HexBox.ByteProvider = null;
            MainForm.StructureForm.Tree.Nodes.Clear();
            MainForm.PropertyForm.Properties.SelectedObject = null;

            if (!mViewOutboundMenu.Checked && !mViewInboundMenu.Checked) return;
            mPacketList.BeginUpdate();
            for (int index = 0; index < mPackets.Count; ++index)
            {
                MaplePacket packet = mPackets[index];
                if (packet.Outbound && !mViewOutboundMenu.Checked) continue;
                if (!packet.Outbound && !mViewInboundMenu.Checked) continue;

                Definition definition = Config.Instance.GetDefinition(mBuild, mLocale, packet.Outbound, packet.Opcode);
                packet.Name = definition == null ? "" : definition.Name;
                if (!mOpcodes.Exists(op => op.Outbound == packet.Outbound && op.Header == packet.Opcode)) mOpcodes.Add(new Opcode(packet.Outbound, packet.Opcode));
                if (definition != null && !mViewIgnoredMenu.Checked && definition.Ignore) continue;
                mPacketList.Items.Add(packet);

                if (packet == previous) packet.Selected = true;
            }
            mPacketList.EndUpdate();
            MainForm.SearchForm.RefreshOpcodes(true);

            if (previous != null) previous.EnsureVisible();
        }

        public void ReselectPacket()
        {
            mPacketList_SelectedIndexChanged(null, null);
        }

        private static Regex _packetRegex = new Regex(@"\[(.{1,2}):(.{1,2}):(.{1,2})\]\[(\d+)\] (Recv|Send):  (.+)");
        internal void ParseMSnifferLine(string packetLine)
        {
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
            string[] bytesText = match.Groups[6].Value.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            ushort opcode = (ushort)(byte.Parse(bytesText[0], NumberStyles.HexNumber) | byte.Parse(bytesText[1], NumberStyles.HexNumber) << 8);

            for (var i = 2; i < packetLength; i++)
            {
                buffer[i - 2] = byte.Parse(bytesText[i], NumberStyles.HexNumber);
            }

            Definition definition = Config.Instance.GetDefinition(mBuild, mLocale, outbound, opcode);
            MaplePacket packet = new MaplePacket(date, outbound, mBuild, mLocale, opcode, definition == null ? "" : definition.Name, buffer, 0, 0);
            AddPacket(packet);
            if (!mOpcodes.Exists(op => op.Outbound == packet.Outbound && op.Header == packet.Opcode)) mOpcodes.Add(new Opcode(packet.Outbound, packet.Opcode));
            if (definition != null && definition.Ignore) return;
            mPacketList.Items.Add(packet);
        }

        public void RunSaveCMD()
        {
            mFileSaveMenu.PerformClick();
        }

        private void mFileSaveMenu_Click(object pSender, EventArgs pArgs)
        {
            if (mFilename == null)
            {
                mSaveDialog.FileName = string.Format("Port {0}", mLocalPort);
                if (mSaveDialog.ShowDialog(this) == DialogResult.OK) mFilename = mSaveDialog.FileName;
                else return;
            }
            using (FileStream stream = new FileStream(mFilename, FileMode.Create, FileAccess.Write))
            {
                var version = (ushort)0x2027;

                BinaryWriter writer = new BinaryWriter(stream);
                writer.Write(version);
                writer.Write(mLocalEndpoint);
                writer.Write(mLocalPort);
                writer.Write(mRemoteEndpoint);
                writer.Write(mRemotePort);
                writer.Write(mLocale);
                writer.Write(mBuild);
                if (!Config.Instance.Maple2)
                {
                    writer.Write(mPatchLocation);
                }

                mPackets.ForEach(p =>
                {
                    writer.Write((ulong)p.Timestamp.Ticks);
                    writer.Write((int)p.Length);
                    writer.Write((ushort)p.Opcode);
                    writer.Write((byte)(p.Outbound ? 1 : 0));
                    writer.Write(p.Buffer);
                    writer.Write(p.PreDecodeIV);
                    writer.Write(p.PostDecodeIV);
                });

                stream.Flush();
            }

            if (mTerminated)
            {
                mFileSaveMenu.Enabled = false;
                Text += " (ReadOnly)";
            }

            Saved = true;
        }

        private void mFileExportMenu_Click(object pSender, EventArgs pArgs)
        {
            mExportDialog.FileName = string.Format("Port {0}", mLocalPort);
            if (mExportDialog.ShowDialog(this) != DialogResult.OK) return;

            bool includeNames = MessageBox.Show("Export opcode names? (slow + generates big files!!!)", "-", MessageBoxButtons.YesNo) == DialogResult.Yes;

            string tmp = "";
            tmp += string.Format("=== MapleStory2 Version: {0}; Region: {1} ===\r\n", mBuild, mLocale);
            tmp += string.Format("Endpoint From: {0}\r\n", mLocalEndpoint);
            tmp += string.Format("Endpoint To: {0}\r\n", mRemoteEndpoint);
            tmp += string.Format("- Packets: {0}\r\n", mPackets.Count);

            long dataSize = 0;
            foreach (var packet in mPackets)
                dataSize += 2 + packet.Buffer.Length;

            tmp += string.Format("- Data: {0:N0} bytes\r\n", dataSize);
            tmp += string.Format("================================================\r\n");
            File.WriteAllText(mExportDialog.FileName, tmp);

            tmp = "";

            int outboundCount = 0;
            int inboundCount = 0;
            int i = 0;
            foreach (var packet in mPackets)
            {
                if (packet.Outbound) ++outboundCount;
                else ++inboundCount;

                Definition definition = Config.Instance.GetDefinition(mBuild, mLocale, packet.Outbound, packet.Opcode);

                tmp += string.Format("[{0}][{2}] [{3:X4}{5}] {4}\r\n",
                    packet.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                    (packet.Outbound ? outboundCount : inboundCount),
                    (packet.Outbound ? "Outbound" : "Inbound "),
                    packet.Opcode,
                    BitConverter.ToString(packet.Buffer).Replace('-', ' '),
                    includeNames ? " | " + (definition == null ? "N/A" : definition.Name) : "");
                i++;
                if (i % 1000 == 0)
                {
                    File.AppendAllText(mExportDialog.FileName, tmp);
                    tmp = "";
                }
            }
            File.AppendAllText(mExportDialog.FileName, tmp);
        }

        private void mViewCommonScriptMenu_Click(object pSender, EventArgs pArgs)
        {
            var scriptPath = Helpers.GetCommonScriptPath(mLocale, mBuild);
            Helpers.MakeSureFileDirectoryExists(scriptPath);

            ScriptForm script = new ScriptForm(scriptPath, null);
            script.FormClosed += CommonScript_FormClosed;
            script.Show(DockPanel, new Rectangle(MainForm.Location, new Size(600, 300)));
        }

        private void CommonScript_FormClosed(object pSender, FormClosedEventArgs pArgs)
        {
            if (mPacketList.SelectedIndices.Count == 0) return;
            MaplePacket packet = mPacketList.SelectedItems[0] as MaplePacket;
            MainForm.StructureForm.ParseMaplePacket(packet);
            Activate();
        }

        private void mViewRefreshMenu_Click(object pSender, EventArgs pArgs) { RefreshPackets(); }
        private void mViewOutboundMenu_CheckedChanged(object pSender, EventArgs pArgs) { RefreshPackets(); }
        private void mViewInboundMenu_CheckedChanged(object pSender, EventArgs pArgs) { RefreshPackets(); }
        private void mViewIgnoredMenu_CheckedChanged(object pSender, EventArgs pArgs) { RefreshPackets(); }

        private void mPacketList_SelectedIndexChanged(object pSender, EventArgs pArgs)
        {
            if (mPacketList.SelectedItems.Count == 0)
            {
                MainForm.DataForm.HexBox.ByteProvider = null;
                MainForm.StructureForm.Tree.Nodes.Clear();
                MainForm.PropertyForm.Properties.SelectedObject = null;
                return;
            }
            MainForm.DataForm.HexBox.ByteProvider = new DynamicByteProvider((mPacketList.SelectedItems[0] as MaplePacket).Buffer);
            MainForm.StructureForm.ParseMaplePacket(mPacketList.SelectedItems[0] as MaplePacket);
        }

        private void mPacketList_ItemActivate(object pSender, EventArgs pArgs)
        {
            if (mPacketList.SelectedIndices.Count == 0) return;
            MaplePacket packet = mPacketList.SelectedItems[0] as MaplePacket;

            var scriptPath = Helpers.GetScriptPath(mLocale, mBuild, packet.Outbound, packet.Opcode);
            Helpers.MakeSureFileDirectoryExists(scriptPath);

            ScriptForm script = new ScriptForm(scriptPath, packet);
            script.FormClosed += Script_FormClosed;
            script.Show(DockPanel, new Rectangle(MainForm.Location, new Size(600, 300)));
        }

        private void Script_FormClosed(object pSender, FormClosedEventArgs pArgs)
        {
            ScriptForm script = pSender as ScriptForm;
            script.Packet.Selected = true;
            MainForm.StructureForm.ParseMaplePacket(script.Packet);
            Activate();
        }

        bool openingContextMenu = false;
        private void mPacketContextMenu_Opening(object pSender, CancelEventArgs pArgs)
        {
            openingContextMenu = true;
            mPacketContextNameBox.Text = "";
            mPacketContextIgnoreMenu.Checked = false;
            if (mPacketList.SelectedItems.Count == 0) pArgs.Cancel = true;
            else
            {
                MaplePacket packet = mPacketList.SelectedItems[0] as MaplePacket;
                Definition definition = Config.Instance.GetDefinition(mBuild, mLocale, packet.Outbound, packet.Opcode);
                if (definition != null)
                {
                    mPacketContextNameBox.Text = definition.Name;
                    mPacketContextIgnoreMenu.Checked = definition.Ignore;
                }
            }
        }

        private void mPacketContextMenu_Opened(object pSender, EventArgs pArgs)
        {
            mPacketContextNameBox.Focus();
            mPacketContextNameBox.SelectAll();

            mPacketList.SelectedItems[0].EnsureVisible();
            openingContextMenu = false;
        }

        private void mPacketContextNameBox_KeyDown(object pSender, KeyEventArgs pArgs)
        {
            if (pArgs.Modifiers == Keys.None && pArgs.KeyCode == Keys.Enter)
            {
                MaplePacket packet = mPacketList.SelectedItems[0] as MaplePacket;
                Definition definition = Config.Instance.GetDefinition(mBuild, mLocale, packet.Outbound, packet.Opcode);
                if (definition == null)
                {
                    definition = new Definition();
                    definition.Build = mBuild;
                    definition.Outbound = packet.Outbound;
                    definition.Opcode = packet.Opcode;
                    definition.Locale = mLocale;
                }
                definition.Name = mPacketContextNameBox.Text;
                SaveDefinition(definition);

                pArgs.SuppressKeyPress = true;
                mPacketContextMenu.Close();
                RefreshPackets();

                packet.EnsureVisible();
            }
        }

        private void mPacketContextIgnoreMenu_CheckedChanged(object pSender, EventArgs pArgs)
        {
            if (openingContextMenu) return;
            MaplePacket packet = mPacketList.SelectedItems[0] as MaplePacket;
            Definition definition = Config.Instance.GetDefinition(mBuild, mLocale, packet.Outbound, packet.Opcode);
            if (definition == null)
            {
                definition = new Definition();
                definition.Locale = mLocale;
                definition.Build = mBuild;
                definition.Outbound = packet.Outbound;
                definition.Opcode = packet.Opcode;
                definition.Locale = mLocale;
            }
            definition.Ignore = mPacketContextIgnoreMenu.Checked;
            SaveDefinition(definition);

            int newIndex = packet.Index - 1;
            for (var i = packet.Index - 1; i > 0; i--)
            {
                var pack = mPacketList.Items[i] as MaplePacket;
                var def = Config.Instance.GetDefinition(mBuild, mLocale, pack.Outbound, pack.Opcode);
                if (def == definition)
                {
                    newIndex--;
                }
            }

            RefreshPackets();


            if (newIndex != 0 && mPacketList.Items[newIndex] != null)
            {
                packet = mPacketList.Items[newIndex] as MaplePacket;
                packet.Selected = true;
                packet.EnsureVisible();
            }
        }

        private void SessionForm_Load(object sender, EventArgs e)
        {

        }

        private void mMenu_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }

        private void sessionInformationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SessionInformation si = new SessionInformation();
            si.txtVersion.Text = mBuild.ToString();
            si.txtPatchLocation.Text = mPatchLocation;
            si.txtLocale.Text = mLocale.ToString();
            si.txtAdditionalInfo.Text = "Connection info:\r\n" + mLocalEndpoint + " <-> " + mRemoteEndpoint + (mProxyEndpoint != "???" ? "\r\nProxy:" + mProxyEndpoint : "");

            if (mLocale == 1 || mLocale == 2)
            {
                si.txtAdditionalInfo.Text += "\r\nRecording session of a MapleStory Korea" + (mLocale == 2 ? " Test" : "") + " server.\r\nAdditional KMS info:\r\n";

                try
                {
                    int test = int.Parse(mPatchLocation);
                    ushort maplerVersion = (ushort)(test & 0x7FFF);
                    int extraOption = (test >> 15) & 1;
                    int subVersion = (test >> 16) & 0xFF;
                    si.txtAdditionalInfo.Text += "Real Version: " + maplerVersion + "\r\nSubversion: " + subVersion + "\r\nExtra flag: " + extraOption;
                }
                catch { }
            }

            si.Show();
        }

        private void sendpropertiesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DefinitionsContainer.Instance.SaveProperties();
            string tmp = Config.GetPropertiesFile(true, (byte)mLocale, mBuild);
            Process.Start(tmp);
        }

        private void recvpropertiesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DefinitionsContainer.Instance.SaveProperties();
            string tmp = Config.GetPropertiesFile(false, (byte)mLocale, mBuild);
            Process.Start(tmp);
        }

        private void removeLoggedPacketsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to delete all logged packets?", "!!", MessageBoxButtons.YesNo) == DialogResult.No) return;

            ClearedPackets = true;

            mPackets.Clear();
            ListView.Items.Clear();
            mOpcodes.Clear();
            RefreshPackets();
        }

        private void mFileSeparatorMenu_Click(object sender, EventArgs e)
        {

        }

        private void thisPacketOnlyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (mPacketList.SelectedItems.Count == 0) return;
            var packet = mPacketList.SelectedItems[0] as MaplePacket;
            var index = packet.Index;
            mPackets.Remove(packet);

            packet.Selected = false;
            if (index > 0)
            {
                index--;
                mPackets[index].Selected = true;
            }
            RefreshPackets();
        }

        private void allBeforeThisOneToolStripMenuItem_Click(object sender, EventArgs e)
        {
        }

        private void onlyVisibleToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (mPacketList.SelectedItems.Count == 0) return;
            var packet = mPacketList.SelectedItems[0] as MaplePacket;

            for (int i = 0; i < packet.Index; i++)
                mPackets.Remove(mPacketList.Items[i] as MaplePacket);
            RefreshPackets();
        }

        private void allToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (mPacketList.SelectedItems.Count == 0) return;
            var packet = mPacketList.SelectedItems[0] as MaplePacket;

            mPackets.RemoveRange(0, mPackets.FindIndex((p) => { return p == packet; }));
            RefreshPackets();
        }

        private void onlyVisibleToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (mPacketList.SelectedItems.Count == 0) return;
            var packet = mPacketList.SelectedItems[0] as MaplePacket;

            for (int i = packet.Index + 1; i < mPacketList.Items.Count; i++)
                mPackets.Remove(mPacketList.Items[i] as MaplePacket);
            RefreshPackets();
        }

        private void allToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (mPacketList.SelectedItems.Count == 0) return;
            var packet = mPacketList.SelectedItems[0] as MaplePacket;
            var startIndex = mPackets.FindIndex((p) => { return p == packet; }) + 1;
            mPackets.RemoveRange(startIndex, mPackets.Count - startIndex);
            RefreshPackets();
        }


        private void SaveDefinition(Definition definition)
        {
            DefinitionsContainer.Instance.SaveDefinition(definition);
        }


        private void AddPacket(MaplePacket packet)
        {
            mPackets.Add(packet);
        }

        private void Terminate() {
            mTerminated = true;
            Text += " (Terminated)";
        }
    }

    internal class TcpSequenceComparer : IComparer<TcpPacket>
    {
        public int Compare(TcpPacket x, TcpPacket y) {
            if (x == y) {
                return 0;
            }
            if (x == null) {
                return -1;
            }
            if (y == null) {
                return 1;
            }
            return x.SequenceNumber.CompareTo(y.SequenceNumber);
        }
    }
}
