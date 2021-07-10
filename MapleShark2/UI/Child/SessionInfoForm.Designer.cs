namespace MapleShark2.UI.Child {
	partial class SessionInfoForm {
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
			this.lblVersion = new System.Windows.Forms.Label();
			this.txtVersion = new System.Windows.Forms.TextBox();
			this.txtLocale = new System.Windows.Forms.TextBox();
			this.lblLocale = new System.Windows.Forms.Label();
			this.txtAdditionalInfo = new System.Windows.Forms.TextBox();
			this.panel1 = new System.Windows.Forms.Panel();
			this.closeButton = new System.Windows.Forms.Button();
			this.groupAdditionalInfo = new System.Windows.Forms.GroupBox();
			this.panel1.SuspendLayout();
			this.groupAdditionalInfo.SuspendLayout();
			this.SuspendLayout();
			// 
			// lblVersion
			// 
			this.lblVersion.AutoSize = true;
			this.lblVersion.Location = new System.Drawing.Point(13, 16);
			this.lblVersion.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.lblVersion.Name = "lblVersion";
			this.lblVersion.Size = new System.Drawing.Size(135, 17);
			this.lblVersion.TabIndex = 0;
			this.lblVersion.Text = "MapleStory Version:";
			// 
			// txtVersion
			// 
			this.txtVersion.Location = new System.Drawing.Point(223, 13);
			this.txtVersion.Margin = new System.Windows.Forms.Padding(4);
			this.txtVersion.Name = "txtVersion";
			this.txtVersion.ReadOnly = true;
			this.txtVersion.Size = new System.Drawing.Size(132, 22);
			this.txtVersion.TabIndex = 2;
			// 
			// txtLocale
			// 
			this.txtLocale.Location = new System.Drawing.Point(223, 43);
			this.txtLocale.Margin = new System.Windows.Forms.Padding(4);
			this.txtLocale.Name = "txtLocale";
			this.txtLocale.ReadOnly = true;
			this.txtLocale.Size = new System.Drawing.Size(132, 22);
			this.txtLocale.TabIndex = 5;
			// 
			// lblLocale
			// 
			this.lblLocale.AutoSize = true;
			this.lblLocale.Location = new System.Drawing.Point(13, 46);
			this.lblLocale.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.lblLocale.Name = "lblLocale";
			this.lblLocale.Size = new System.Drawing.Size(129, 17);
			this.lblLocale.TabIndex = 4;
			this.lblLocale.Text = "MapleStory Locale:";
			// 
			// txtAdditionalInfo
			// 
			this.txtAdditionalInfo.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.txtAdditionalInfo.Dock = System.Windows.Forms.DockStyle.Fill;
			this.txtAdditionalInfo.Location = new System.Drawing.Point(3, 18);
			this.txtAdditionalInfo.Margin = new System.Windows.Forms.Padding(4);
			this.txtAdditionalInfo.Multiline = true;
			this.txtAdditionalInfo.Name = "txtAdditionalInfo";
			this.txtAdditionalInfo.ReadOnly = true;
			this.txtAdditionalInfo.ScrollBars = System.Windows.Forms.ScrollBars.Both;
			this.txtAdditionalInfo.Size = new System.Drawing.Size(362, 194);
			this.txtAdditionalInfo.TabIndex = 7;
			// 
			// panel1
			// 
			this.panel1.Controls.Add(this.lblVersion);
			this.panel1.Controls.Add(this.lblLocale);
			this.panel1.Controls.Add(this.txtLocale);
			this.panel1.Controls.Add(this.txtVersion);
			this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
			this.panel1.Location = new System.Drawing.Point(0, 0);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(368, 77);
			this.panel1.TabIndex = 9;
			// 
			// closeButton
			// 
			this.closeButton.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.closeButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.closeButton.Location = new System.Drawing.Point(0, 292);
			this.closeButton.Margin = new System.Windows.Forms.Padding(4);
			this.closeButton.Name = "closeButton";
			this.closeButton.Size = new System.Drawing.Size(368, 32);
			this.closeButton.TabIndex = 8;
			this.closeButton.Text = "Close";
			this.closeButton.UseVisualStyleBackColor = true;
			this.closeButton.Click += new System.EventHandler(this.closeButton_Click);
			// 
			// groupAdditionalInfo
			// 
			this.groupAdditionalInfo.Controls.Add(this.txtAdditionalInfo);
			this.groupAdditionalInfo.Dock = System.Windows.Forms.DockStyle.Fill;
			this.groupAdditionalInfo.Location = new System.Drawing.Point(0, 77);
			this.groupAdditionalInfo.Name = "groupAdditionalInfo";
			this.groupAdditionalInfo.Size = new System.Drawing.Size(368, 215);
			this.groupAdditionalInfo.TabIndex = 10;
			this.groupAdditionalInfo.TabStop = false;
			this.groupAdditionalInfo.Text = "Additional Info";
			// 
			// SessionInfoForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(368, 324);
			this.Controls.Add(this.groupAdditionalInfo);
			this.Controls.Add(this.panel1);
			this.Controls.Add(this.closeButton);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
			this.Margin = new System.Windows.Forms.Padding(4);
			this.Name = "SessionInfoForm";
			this.Text = "Session Information";
			this.Load += new System.EventHandler(this.SessionInformation_Load);
			this.panel1.ResumeLayout(false);
			this.panel1.PerformLayout();
			this.groupAdditionalInfo.ResumeLayout(false);
			this.groupAdditionalInfo.PerformLayout();
			this.ResumeLayout(false);
		}

		private System.Windows.Forms.GroupBox groupAdditionalInfo;

		private System.Windows.Forms.GroupBox groupBox1;

		private System.Windows.Forms.Panel panel1;

		#endregion

		private System.Windows.Forms.Label lblVersion;
		private System.Windows.Forms.Label lblLocale;
		private System.Windows.Forms.Label lblAdditionalInfo;
		private System.Windows.Forms.Button closeButton;
		public System.Windows.Forms.TextBox txtVersion;
		public System.Windows.Forms.TextBox txtLocale;
		public System.Windows.Forms.TextBox txtAdditionalInfo;
	}
}