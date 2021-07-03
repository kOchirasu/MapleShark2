using System.Windows.Forms;

namespace MapleShark2.UI
{
    partial class SearchForm
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
            this.dropdownOpcode = new System.Windows.Forms.ComboBox();
            this.btnNextOpcode = new System.Windows.Forms.Button();
            this.btnPrevOpcode = new System.Windows.Forms.Button();
            this.hexInput = new Be.Windows.Forms.HexBox();
            this.btnPrevSequence = new System.Windows.Forms.Button();
            this.btnNextSequence = new System.Windows.Forms.Button();
            this.SuspendLayout();
            //
            // dropdownOpcode
            //
            this.dropdownOpcode.Anchor = ((System.Windows.Forms.AnchorStyles) ((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.dropdownOpcode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.dropdownOpcode.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte) (0)));
            this.dropdownOpcode.FormattingEnabled = true;
            this.dropdownOpcode.Location = new System.Drawing.Point(3, 3);
            this.dropdownOpcode.Name = "dropdownOpcode";
            this.dropdownOpcode.Size = new System.Drawing.Size(152, 23);
            this.dropdownOpcode.TabIndex = 4;
            this.dropdownOpcode.SelectedIndexChanged += new System.EventHandler(this.dropdownOpcode_SelectedIndexChanged);
            //this.dropdownOpcode.DrawMode = DrawMode.OwnerDrawFixed;
            //this.dropdownOpcode.DrawItem += this.dropdownOpcode_DrawItem;
            //
            // btnNextOpcode
            //
            this.btnNextOpcode.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.btnNextOpcode.Enabled = false;
            this.btnNextOpcode.Location = new System.Drawing.Point(232, 3);
            this.btnNextOpcode.Name = "btnNextOpcode";
            this.btnNextOpcode.Size = new System.Drawing.Size(65, 25);
            this.btnNextOpcode.TabIndex = 5;
            this.btnNextOpcode.Text = "Next";
            this.btnNextOpcode.UseVisualStyleBackColor = true;
            this.btnNextOpcode.FlatStyle = FlatStyle.Flat;
            this.btnNextOpcode.Click += new System.EventHandler(this.btnNextOpcode_Click);
            //
            // btnPrevOpcode
            //
            this.btnPrevOpcode.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.btnPrevOpcode.Enabled = false;
            this.btnPrevOpcode.Location = new System.Drawing.Point(161, 3);
            this.btnPrevOpcode.Name = "btnPrevOpcode";
            this.btnPrevOpcode.Size = new System.Drawing.Size(65, 25);
            this.btnPrevOpcode.TabIndex = 9;
            this.btnPrevOpcode.Text = "Prev";
            this.btnPrevOpcode.UseVisualStyleBackColor = true;
            this.btnPrevOpcode.FlatStyle = FlatStyle.Flat;
            this.btnPrevOpcode.Click += new System.EventHandler(this.btnPrevOpcode_Click);
            //
            // hexInput
            //
            this.hexInput.Anchor = ((System.Windows.Forms.AnchorStyles) ((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.hexInput.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte) (0)));
            this.hexInput.InfoForeColor = System.Drawing.Color.Empty;
            this.hexInput.Location = new System.Drawing.Point(3, 32);
            this.hexInput.Name = "hexInput";
            this.hexInput.ShadowSelectionColor = System.Drawing.Color.FromArgb(((int) (((byte) (100)))), ((int) (((byte) (60)))), ((int) (((byte) (188)))), ((int) (((byte) (255)))));
            this.hexInput.Size = new System.Drawing.Size(152, 25);
            this.hexInput.TabIndex = 6;
            this.hexInput.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.hexInput_KeyPress);
            //
            // btnPrevSequence
            //
            this.btnPrevSequence.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.btnPrevSequence.Enabled = false;
            this.btnPrevSequence.Location = new System.Drawing.Point(161, 32);
            this.btnPrevSequence.Name = "btnPrevSequence";
            this.btnPrevSequence.Size = new System.Drawing.Size(65, 25);
            this.btnPrevSequence.TabIndex = 8;
            this.btnPrevSequence.Text = "Prev";
            this.btnPrevSequence.FlatStyle = FlatStyle.Flat;
            this.btnPrevSequence.UseVisualStyleBackColor = true;
            //
            // btnNextSequence
            //
            this.btnNextSequence.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.btnNextSequence.Enabled = false;
            this.btnNextSequence.Location = new System.Drawing.Point(232, 32);
            this.btnNextSequence.Name = "btnNextSequence";
            this.btnNextSequence.Size = new System.Drawing.Size(65, 25);
            this.btnNextSequence.TabIndex = 7;
            this.btnNextSequence.Text = "Next";
            this.btnNextSequence.UseVisualStyleBackColor = true;
            this.btnNextSequence.FlatStyle = FlatStyle.Flat;
            this.btnNextSequence.Click += new System.EventHandler(this.btnNextSequence_Click);
            //
            // SearchForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 14F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(300, 60);
            this.Controls.Add(this.btnNextSequence);
            this.Controls.Add(this.btnNextOpcode);
            this.Controls.Add(this.btnPrevSequence);
            this.Controls.Add(this.btnPrevOpcode);
            this.Controls.Add(this.hexInput);
            this.Controls.Add(this.dropdownOpcode);
            this.DockAreas = ((WeifenLuo.WinFormsUI.Docking.DockAreas) (((((WeifenLuo.WinFormsUI.Docking.DockAreas.Float | WeifenLuo.WinFormsUI.Docking.DockAreas.DockLeft)
                                                                           | WeifenLuo.WinFormsUI.Docking.DockAreas.DockRight)
                                                                          | WeifenLuo.WinFormsUI.Docking.DockAreas.DockTop)
                                                                         | WeifenLuo.WinFormsUI.Docking.DockAreas.DockBottom)));
            this.Font = new System.Drawing.Font("Consolas", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte) (0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.HideOnClose = true;
            this.MinimumSize = new System.Drawing.Size(300, 60);
            this.Name = "SearchForm";
            this.ShowHint = WeifenLuo.WinFormsUI.Docking.DockState.DockTop;
            this.Text = "Search";
            this.Load += new System.EventHandler(this.SearchForm_Load);
            this.ResumeLayout(false);
        }

        private System.Windows.Forms.Button btnNextOpcode;
        private System.Windows.Forms.Button btnNextSequence;
        private System.Windows.Forms.ComboBox dropdownOpcode;
        private System.Windows.Forms.Button btnPrevOpcode;
        private System.Windows.Forms.Button btnPrevSequence;
        private Be.Windows.Forms.HexBox hexInput;
        #endregion
    }
}