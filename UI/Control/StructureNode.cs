using System;
using System.Windows.Forms;

namespace MapleShark2.UI.Control {
    public class StructureNode : TreeNode {
        public ArraySegment<byte> Data { get; private set; }

        public StructureNode(string name, ArraySegment<byte> data) : base(name) {
            Data = data;
        }

        // Used to update the Array segment for node groups
        public void UpdateData(ArraySegment<byte> data) {
            Data = data;
        }
    }
}