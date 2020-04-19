using System.Windows.Forms;
using MapleShark.Tools;

namespace MapleShark
{
    public sealed class DoubleBufferedListView : ListView
    {
        public DoubleBufferedListView() {
            DoubleBuffered = true;
        }

        public new int VirtualListSize {
            get => base.VirtualListSize;
            set {
                if (this.SelectedIndices.Count <= 0) {
                    base.VirtualListSize = value;
                } else {
                    this.SetVirtualListSizeWithoutRefresh(value);
                }
            }
        }
    }
}