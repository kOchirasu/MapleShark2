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
			this.lblAdditionalInfo = new System.Windows.Forms.Label();
			this.closeButton = new System.Windows.Forms.Button();
			this.SuspendLayout();
			//
			// lblVersion
			//
			this.lblVersion.AutoSize = true;
			this.lblVersion.Location = new System.Drawing.Point(16, 21);
			this.lblVersion.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.lblVersion.Name = "lblVersion";
			this.lblVersion.Size = new System.Drawing.Size(128, 16);
			this.lblVersion.TabIndex = 0;
			this.lblVersion.Text = "MapleStory Version:";
			//
			// txtVersion
			//
			this.txtVersion.Location = new System.Drawing.Point(225, 15);
			this.txtVersion.Margin = new System.Windows.Forms.Padding(4);
			this.txtVersion.Name = "txtVersion";
			this.txtVersion.ReadOnly = true;
			this.txtVersion.Size = new System.Drawing.Size(132, 22);
			this.txtVersion.TabIndex = 2;
			//
			// txtLocale
			//
			this.txtLocale.Location = new System.Drawing.Point(225, 48);
			this.txtLocale.Margin = new System.Windows.Forms.Padding(4);
			this.txtLocale.Name = "txtLocale";
			this.txtLocale.ReadOnly = true;
			this.txtLocale.Size = new System.Drawing.Size(132, 22);
			this.txtLocale.TabIndex = 5;
			//
			// lblLocale
			//
			this.lblLocale.AutoSize = true;
			this.lblLocale.Location = new System.Drawing.Point(16, 54);
			this.lblLocale.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.lblLocale.Name = "lblLocale";
			this.lblLocale.Size = new System.Drawing.Size(123, 16);
			this.lblLocale.TabIndex = 4;
			this.lblLocale.Text = "MapleStory Locale:";
			//
			// txtAdditionalInfo
			//
			this.txtAdditionalInfo.Location = new System.Drawing.Point(20, 106);
			this.txtAdditionalInfo.Margin = new System.Windows.Forms.Padding(4);
			this.txtAdditionalInfo.Multiline = true;
			this.txtAdditionalInfo.Name = "txtAdditionalInfo";
			this.txtAdditionalInfo.ReadOnly = true;
			this.txtAdditionalInfo.ScrollBars = System.Windows.Forms.ScrollBars.Both;
			this.txtAdditionalInfo.Size = new System.Drawing.Size(337, 158);
			this.txtAdditionalInfo.TabIndex = 7;
			//
			// lblAdditionalInfo
			//
			this.lblAdditionalInfo.AutoSize = true;
			this.lblAdditionalInfo.Location = new System.Drawing.Point(16, 86);
			this.lblAdditionalInfo.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.lblAdditionalInfo.Name = "lblAdditionalInfo";
			this.lblAdditionalInfo.Size = new System.Drawing.Size(138, 16);
			this.lblAdditionalInfo.TabIndex = 6;
			this.lblAdditionalInfo.Text = "Additional Information:";
			//
			// closeButton
			//
			this.closeButton.Location = new System.Drawing.Point(20, 272);
			this.closeButton.Margin = new System.Windows.Forms.Padding(4);
			this.closeButton.Name = "closeButton";
			this.closeButton.Size = new System.Drawing.Size(339, 37);
			this.closeButton.TabIndex = 8;
			this.closeButton.Text = "Close";
			this.closeButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.closeButton.UseVisualStyleBackColor = true;
			this.closeButton.Click += new System.EventHandler(this.closeButton_Click);
			//
			// SessionInformation
			//
			this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(368, 324);
			this.Controls.Add(this.closeButton);
			this.Controls.Add(this.txtAdditionalInfo);
			this.Controls.Add(this.lblAdditionalInfo);
			this.Controls.Add(this.txtLocale);
			this.Controls.Add(this.lblLocale);
			this.Controls.Add(this.txtVersion);
			this.Controls.Add(this.lblVersion);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
			this.Margin = new System.Windows.Forms.Padding(4);
			this.Name = "SessionInfoForm";
			this.Text = "Session Information";
			this.Load += new System.EventHandler(this.SessionInformation_Load);
			this.ResumeLayout(false);
			this.PerformLayout();
		}
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