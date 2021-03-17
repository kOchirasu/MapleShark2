namespace MapleShark2.UI.Child
{
    partial class ScriptForm
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources =
                new System.ComponentModel.ComponentResourceManager(typeof(ScriptForm));
            this.mSaveButton = new System.Windows.Forms.Button();
            this.mScriptEditor = new ScintillaNET.Scintilla();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.mImportButton = new System.Windows.Forms.Button();
            this.FileImporter = new System.Windows.Forms.OpenFileDialog();
            ((System.ComponentModel.ISupportInitialize) (this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.SuspendLayout();
            //
            // mSaveButton
            //
            this.mSaveButton.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mSaveButton.Enabled = false;
            this.mSaveButton.Location = new System.Drawing.Point(0, 0);
            this.mSaveButton.Name = "mSaveButton";
            this.mSaveButton.Size = new System.Drawing.Size(392, 25);
            this.mSaveButton.TabIndex = 5;
            this.mSaveButton.Text = "&Save script";
            this.mSaveButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.mSaveButton.UseVisualStyleBackColor = true;
            this.mSaveButton.Click += new System.EventHandler(this.mSaveButton_Click);
            //
            // mScriptEditor
            //
            this.mScriptEditor.StyleResetDefault();
            this.mScriptEditor.Styles[ScintillaNET.Style.Default].Font = "Consolas";
            this.mScriptEditor.Styles[ScintillaNET.Style.Default].Size = 10;
            this.mScriptEditor.Styles[ScintillaNET.Style.Default].ForeColor =
                System.Drawing.Color.FromArgb(0xD4, 0xD4, 0xD4);
            this.mScriptEditor.Styles[ScintillaNET.Style.Default].BackColor =
                System.Drawing.Color.FromArgb(0x1E, 0x1E, 0x1E);
            this.mScriptEditor.SetSelectionBackColor(true, System.Drawing.Color.FromArgb(0x14, 0x44, 0x6A));
            this.mScriptEditor.StyleClearAll();

            this.mScriptEditor.Lexer = ScintillaNET.Lexer.Python;
            // Set the styles
            this.mScriptEditor.Styles[ScintillaNET.Style.LineNumber].ForeColor =
                System.Drawing.Color.FromArgb(0xB4, 0xB4, 0xB4);
            this.mScriptEditor.Styles[ScintillaNET.Style.LineNumber].BackColor =
                System.Drawing.Color.FromArgb(0x30, 0x30, 0x30);
            this.mScriptEditor.Styles[ScintillaNET.Style.Python.Default].ForeColor =
                System.Drawing.Color.FromArgb(0xD4, 0xD4, 0xD4);
            this.mScriptEditor.Styles[ScintillaNET.Style.Python.CommentLine].ForeColor =
                System.Drawing.Color.FromArgb(0x60, 0x8B, 0x4E);
            this.mScriptEditor.Styles[ScintillaNET.Style.Python.Number].ForeColor =
                System.Drawing.Color.FromArgb(0xB5, 0xCE, 0xA8);
            this.mScriptEditor.Styles[ScintillaNET.Style.Python.String].ForeColor =
                System.Drawing.Color.FromArgb(0xCE, 0x91, 0x78);
            this.mScriptEditor.Styles[ScintillaNET.Style.Python.Character].ForeColor =
                System.Drawing.Color.FromArgb(0xCE, 0x91, 0x78);
            this.mScriptEditor.Styles[ScintillaNET.Style.Python.Word].ForeColor =
                System.Drawing.Color.FromArgb(0xC5, 0x86, 0xC0);
            this.mScriptEditor.Styles[ScintillaNET.Style.Python.Word2].ForeColor =
                System.Drawing.Color.FromArgb(0x56, 0x9C, 0xD6);
            this.mScriptEditor.Styles[ScintillaNET.Style.Python.Triple].ForeColor =
                System.Drawing.Color.FromArgb(0xCE, 0x91, 0x78);
            this.mScriptEditor.Styles[ScintillaNET.Style.Python.TripleDouble].ForeColor =
                System.Drawing.Color.FromArgb(0xCE, 0x91, 0x78);
            this.mScriptEditor.Styles[ScintillaNET.Style.Python.ClassName].ForeColor =
                System.Drawing.Color.FromArgb(0x56, 0x9C, 0xD6);
            this.mScriptEditor.Styles[ScintillaNET.Style.Python.ClassName].Bold = true;
            this.mScriptEditor.Styles[ScintillaNET.Style.Python.DefName].ForeColor =
                System.Drawing.Color.FromArgb(0xDC, 0xDC, 0xAA);
            this.mScriptEditor.Styles[ScintillaNET.Style.Python.DefName].Bold = true;
            this.mScriptEditor.Styles[ScintillaNET.Style.Python.Operator].Bold = true;
            /*this.mScriptEditor.Styles[ScintillaNET.Style.Python.Identifier].ForeColor =
                System.Drawing.Color.FromArgb(0xDC, 0xDC, 0xAA);*/
            this.mScriptEditor.Styles[ScintillaNET.Style.Python.CommentBlock].ForeColor =
                System.Drawing.Color.FromArgb(0x60, 0x8B, 0x4E);
            this.mScriptEditor.Styles[ScintillaNET.Style.Python.CommentBlock].Italic = true;
            /*this.mScriptEditor.Styles[ScintillaNET.Style.Python.StringEol].ForeColor =
                System.Drawing.Color.FromArgb(0x00, 0x00, 0x00, 0x00);
            this.mScriptEditor.Styles[ScintillaNET.Style.Python.StringEol].BackColor =
                System.Drawing.Color.FromArgb(0x00, 0x00, 0x00, 0x00);
            this.mScriptEditor.Styles[ScintillaNET.Style.Python.StringEol].FillLine = true;*/
            this.mScriptEditor.Styles[ScintillaNET.Style.Python.Decorator].ForeColor =
                System.Drawing.Color.FromArgb(0xFE, 0xC2, 0xE5);
            this.mScriptEditor.CaretForeColor = System.Drawing.Color.FromArgb(0xD4, 0xD4, 0xD4);
            this.mScriptEditor.ViewWhitespace = ScintillaNET.WhitespaceMode.VisibleOnlyIndent;
            // python2: exec print, python 3: nonlocal
            this.mScriptEditor.SetKeywords(0, "as assert break continue del elif else except finally for from global "
                                              + "if import in pass raise return try while with yield");
            this.mScriptEditor.SetKeywords(1, "False None True and class def is lambda not or, exec print");
            this.mScriptEditor.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.mScriptEditor.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mScriptEditor.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.mScriptEditor.Location = new System.Drawing.Point(0, 0);
            this.mScriptEditor.Name = "mScriptEditor";
            this.mScriptEditor.Size = new System.Drawing.Size(598, 370);
            this.mScriptEditor.TabIndex = 0;
            this.mScriptEditor.TextChanged += this.mScriptEditor_TextChanged;
            this.mScriptEditor.InsertCheck += this.mScriptEditor_InsertCheck;
            //
            // splitContainer1
            //
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.splitContainer1.IsSplitterFixed = true;
            this.splitContainer1.Location = new System.Drawing.Point(0, 370);
            this.splitContainer1.Name = "splitContainer1";
            //
            // splitContainer1.Panel1
            //
            this.splitContainer1.Panel1.Controls.Add(this.mSaveButton);
            //
            // splitContainer1.Panel2
            //
            this.splitContainer1.Panel2.Controls.Add(this.mImportButton);
            this.splitContainer1.Size = new System.Drawing.Size(598, 25);
            this.splitContainer1.SplitterDistance = 392;
            this.splitContainer1.TabIndex = 6;
            //
            // mImportButton
            //
            this.mImportButton.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mImportButton.Location = new System.Drawing.Point(0, 0);
            this.mImportButton.Name = "mImportButton";
            this.mImportButton.Size = new System.Drawing.Size(202, 25);
            this.mImportButton.TabIndex = 0;
            this.mImportButton.Text = "Import script...";
            this.mImportButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.mImportButton.UseVisualStyleBackColor = true;
            this.mImportButton.Click += new System.EventHandler(this.mImportButton_Click);
            //
            // FileImporter
            //
            this.FileImporter.FileName = "*.*";
            //
            // ScriptForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(598, 395);
            this.Controls.Add(this.mScriptEditor);
            this.Controls.Add(this.splitContainer1);
            this.DockAreas =
                ((WeifenLuo.WinFormsUI.Docking.DockAreas) ((
                    (((WeifenLuo.WinFormsUI.Docking.DockAreas.Float | WeifenLuo.WinFormsUI.Docking.DockAreas.DockLeft) |
                      WeifenLuo.WinFormsUI.Docking.DockAreas.DockRight) |
                     WeifenLuo.WinFormsUI.Docking.DockAreas.DockTop) |
                    WeifenLuo.WinFormsUI.Docking.DockAreas.DockBottom)));
            this.Font = new System.Drawing.Font("Calibri", 9F, System.Drawing.FontStyle.Regular,
                System.Drawing.GraphicsUnit.Point, ((byte) (0)));
            this.Icon = ((System.Drawing.Icon) (resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ScriptForm";
            this.ShowHint = WeifenLuo.WinFormsUI.Docking.DockState.Float;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Script";
            this.Load += new System.EventHandler(this.ScriptForm_Load);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize) (this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.ResumeLayout(false);
        }
        #endregion

        private ScintillaNET.Scintilla mScriptEditor;
        private System.Windows.Forms.Button mSaveButton;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.Button mImportButton;
        private System.Windows.Forms.OpenFileDialog FileImporter;

    }
}