namespace IDDSCommunity.IntrusionDetection.Admin {
    partial class IDDSCommunityApplicationSettings {
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

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            this.iddscommunitySettingsNavigation = new IDDSCommunity.IntrusionDetection.Admin.IDDSCommunitySettingsNavigation();
            this.configurationPanel = new IDDSCommunity.IntrusionDetection.Admin.SmartPanel();
            this.SuspendLayout();
            //
            // iddscommunitySettingsNavigation
            //
            this.iddscommunitySettingsNavigation.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)));
            this.iddscommunitySettingsNavigation.BackColor = System.Drawing.Color.White;
            this.iddscommunitySettingsNavigation.Location = new System.Drawing.Point(24, 28);
            this.iddscommunitySettingsNavigation.Name = "iddscommunitySettingsNavigation";
            this.iddscommunitySettingsNavigation.SeparatorColor = System.Drawing.Color.FromArgb(((int)(((byte)(191)))), ((int)(((byte)(191)))), ((int)(((byte)(191)))));
            this.iddscommunitySettingsNavigation.ShowSeparator = true;
            this.iddscommunitySettingsNavigation.ShowTopMenu = false;
            this.iddscommunitySettingsNavigation.Size = new System.Drawing.Size(292, 584);
            this.iddscommunitySettingsNavigation.TabIndex = 0;
            //
            // configurationPanel
            //
            this.configurationPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.configurationPanel.AutoScroll = true;
            this.configurationPanel.BorderColor = System.Drawing.SystemColors.ControlText;
            this.configurationPanel.Location = new System.Drawing.Point(322, 28);
            this.configurationPanel.Name = "configurationPanel";
            this.configurationPanel.PaintBorder = false;
            this.configurationPanel.Size = new System.Drawing.Size(488, 584);
            this.configurationPanel.TabIndex = 1;
            //
            // IDDSCommunityApplicationSettings
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.configurationPanel);
            this.Controls.Add(this.iddscommunitySettingsNavigation);
            this.Name = "IDDSCommunityApplicationSettings";
            this.Size = new System.Drawing.Size(844, 634);
            this.ResumeLayout(false);

        }

        #endregion

        private IDDSCommunitySettingsNavigation iddscommunitySettingsNavigation;
        private SmartPanel configurationPanel;
    }
}
