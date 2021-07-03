using System.Windows.Forms;

namespace MapleShark2.UI {
    partial class LogForm {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
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
            this.logBox = new System.Windows.Forms.RichTextBox();
            this.logPanel = new System.Windows.Forms.Panel();
            this.SuspendLayout();
            //
            // logBox
            //
            this.logBox.CausesValidation = false;
            this.logBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.logBox.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte) (0)));
            this.logBox.Location = new System.Drawing.Point(0, 0);
            this.logBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.logBox.Multiline = true;
            this.logBox.Name = "logBox";
            this.logBox.ReadOnly = true;
            this.logBox.Size = new System.Drawing.Size(682, 153);
            this.logBox.TabIndex = 0;
            this.logBox.WordWrap = false;
            this.logBox.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.Vertical;
            //
            // logPanel
            //
            this.logPanel.Controls.Add(this.logBox);
            this.logPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.logPanel.Padding = new Padding(3, 2, 0, 0);
            //
            // LogForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(682, 153);
            this.Controls.Add(this.logPanel);
            this.DockAreas = ((WeifenLuo.WinFormsUI.Docking.DockAreas) (((((WeifenLuo.WinFormsUI.Docking.DockAreas.Float | WeifenLuo.WinFormsUI.Docking.DockAreas.DockLeft)
                                                                           | WeifenLuo.WinFormsUI.Docking.DockAreas.DockRight)
                                                                          | WeifenLuo.WinFormsUI.Docking.DockAreas.DockTop)
                                                                         | WeifenLuo.WinFormsUI.Docking.DockAreas.DockBottom)));
            this.HideOnClose = true;
            this.Name = "LogForm";
            this.ShowHint = WeifenLuo.WinFormsUI.Docking.DockState.DockBottom;
            this.Text = "Log";
            this.ResumeLayout(false);
            this.PerformLayout();
        }
        #endregion

        private System.Windows.Forms.Panel logPanel;
        private System.Windows.Forms.RichTextBox logBox;
    }
}