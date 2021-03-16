namespace MapleShark2.UI
{
    partial class StructureForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code
        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            this.tree = new System.Windows.Forms.TreeView();
            this.SuspendLayout();
            //
            // mTree
            //
            this.tree.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tree.FullRowSelect = true;
            this.tree.HideSelection = false;
            this.tree.Location = new System.Drawing.Point(0, 0);
            this.tree.Name = "tree";
            this.tree.Size = new System.Drawing.Size(288, 364);
            this.tree.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.tree.TabIndex = 3;
            this.tree.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.mTree_AfterSelect);
            this.tree.KeyDown += new System.Windows.Forms.KeyEventHandler(this.mTree_KeyDown);
            //
            // StructureForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(288, 364);
            this.Controls.Add(this.tree);
            this.DockAreas =
                ((WeifenLuo.WinFormsUI.Docking.DockAreas) ((
                    (((WeifenLuo.WinFormsUI.Docking.DockAreas.Float | WeifenLuo.WinFormsUI.Docking.DockAreas.DockLeft) |
                      WeifenLuo.WinFormsUI.Docking.DockAreas.DockRight) |
                     WeifenLuo.WinFormsUI.Docking.DockAreas.DockTop) |
                    WeifenLuo.WinFormsUI.Docking.DockAreas.DockBottom)));
            this.Font = new System.Drawing.Font("Calibri", 9F, System.Drawing.FontStyle.Regular,
                System.Drawing.GraphicsUnit.Point, ((byte) (0)));
            this.HideOnClose = true;
            this.Name = "StructureForm";
            this.ShowHint = WeifenLuo.WinFormsUI.Docking.DockState.DockTop;
            this.Text = "Structure";
            this.ResumeLayout(false);
        }
        #endregion

        private System.Windows.Forms.TreeView tree;
    }
}