using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Forms;
using MapleShark2.Logging;
using MapleShark2.Theme;
using MapleShark2.Tools;
using MapleShark2.UI.Child;
using MapleShark2.UI.Control;
using Scripting.SSharp;
using WeifenLuo.WinFormsUI.Docking;

namespace MapleShark2.UI {
    public partial class StructureForm : DockContent {
        private MaplePacket packet;
        private Stack<StructureNode> mSubNodes = new Stack<StructureNode>();

        public StructureForm() {
            InitializeComponent();
        }

        public new void Show(DockPanel panel) {
            base.Show(panel);
            BackColor = Config.Instance.Theme.DockSuiteTheme.ColorPalette.MainWindowActive.Background;
            ThemeApplier.ApplyTheme(Config.Instance.Theme, Controls);
        }

        public MainForm MainForm => ParentForm as MainForm;
        public TreeView Tree => mTree;

        public void ParseMaplePacket(MaplePacket pPacketItem) {
            mTree.Nodes.Clear();
            mSubNodes.Clear();
            pPacketItem.Reset(); // Seek back to beginning
            packet = pPacketItem;

            var scriptPath = Helpers.GetScriptPath(pPacketItem.Locale, pPacketItem.Build, pPacketItem.Outbound,
                pPacketItem.Opcode);
            var commonPath = Helpers.GetCommonScriptPath(pPacketItem.Locale, pPacketItem.Build);

            if (File.Exists(scriptPath)) {
                try {
                    StringBuilder scriptCode = new StringBuilder();
                    scriptCode.Append(File.ReadAllText(scriptPath));
                    if (File.Exists(commonPath)) scriptCode.Append(File.ReadAllText(commonPath));
                    Script script = Script.Compile(scriptCode.ToString());
                    script.Context.SetItem("ScriptAPI", new ScriptAPI(this));
                    script.Execute();
                } catch (Exception exc) {
                    OutputForm output = new OutputForm("Script Error");
                    output.Append(exc.ToString());
                    output.Show(DockPanel, new Rectangle(MainForm.Location, new Size(400, 400)));
                }
            }

            if (packet.Available > 0) {
                APIAddField("Undefined", packet.Available);
            }
        }

        private TreeNodeCollection CurrentNodes => mSubNodes.Count > 0 ? mSubNodes.Peek().Nodes : mTree.Nodes;

        internal string APIGetFiletime() {
            string ret = DateTime.Now.ToFileTime().ToString().Substring(12);
            return ret;
        }

        internal byte APIAddByte(string pName) => ReadToNode<byte>(pName);

        internal sbyte APIAddSByte(string pName) => ReadToNode<sbyte>(pName);

        internal ushort APIAddUShort(string pName) => ReadToNode<ushort>(pName);

        internal short APIAddShort(string pName) => ReadToNode<short>(pName);

        internal uint APIAddUInt(string pName) => ReadToNode<uint>(pName);

        internal int APIAddInt(string pName) => ReadToNode<int>(pName);

        internal float APIAddFloat(string pName) => ReadToNode<float>(pName);

        internal bool APIAddBool(string pName) => ReadToNode<bool>(pName);

        internal long APIAddLong(string pName) => ReadToNode<long>(pName);

        internal double APIAddDouble(string pName) => ReadToNode<double>(pName);

        internal string APIAddString(string pName) {
            APIStartNode(pName);
            short size = APIAddShort("Size");
            string value = APIAddPaddedString("String", size);
            APIEndNode(false);
            return value;
        }

        internal string APIAddUnicodeString(string pName) {
            APIStartNode(pName);
            short size = APIAddShort("Size");
            CurrentNodes.Add(new StructureNode(pName, packet.GetReadSegment(size * 2))); // Unicode is 2-width
            string value = packet.ReadRawUnicodeString(size);
            APIEndNode(false);
            return value;
        }

        internal string APIAddPaddedString(string pName, int pLength) {
            CurrentNodes.Add(new StructureNode(pName, packet.GetReadSegment(pLength)));
            return packet.ReadRawString(pLength);
        }

        internal void APIAddField(string pName, int pLength) {
            CurrentNodes.Add(new StructureNode(pName, packet.GetReadSegment(pLength)));
            packet.Skip(pLength);
        }

        internal void APIAddComment(string pComment) {
            CurrentNodes.Add(new StructureNode(pComment, packet.GetReadSegment(0)));
        }

        internal void APIStartNode(string pName) {
            var node = new StructureNode(pName, packet.GetReadSegment(0));
            if (mSubNodes.Count > 0) mSubNodes.Peek().Nodes.Add(node);
            else mTree.Nodes.Add(node);
            mSubNodes.Push(node);
        }

        internal void APIEndNode(bool pExpand) {
            if (mSubNodes.Count > 0) {
                StructureNode node = mSubNodes.Pop();
                int length = packet.Position - node.Data.Offset;
                node.UpdateData(packet.GetSegment(node.Data.Offset, length));
                if (pExpand) node.Expand();
            }
        }

        internal int APIRemaining() {
            return packet.Available;
        }

        private T ReadToNode<T>(string name) where T : struct {
            int size = Unsafe.SizeOf<T>();
            CurrentNodes.Add(new StructureNode(name, packet.GetReadSegment(size)));
            return packet.Read<T>();
        }

        private void mTree_AfterSelect(object pSender, TreeViewEventArgs pArgs) {
            if (!(pArgs.Node is StructureNode node)) {
                MainForm.DataForm.ClearHexBoxSelection();
                MainForm.PropertyForm.Properties.SelectedObject = null;
                return;
            }

            MainForm.DataForm.SelectHexBoxRange(node.Data);
            MainForm.PropertyForm.Properties.SelectedObject = new StructureSegment(node.Data, MainForm.Locale);
        }

        private void mTree_KeyDown(object pSender, KeyEventArgs pArgs) {
            MainForm.CopyPacketHex(pArgs);
        }
    }
}