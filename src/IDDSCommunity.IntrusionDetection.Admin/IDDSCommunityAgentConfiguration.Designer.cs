namespace IDDSCommunity.IntrusionDetection.Admin {
    partial class IDDSCommunityAgentConfiguration {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;
        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">若要釋放受控資源則為 true；否則為 false。</param>
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
            this.configurationPanel = new IDDSCommunity.IntrusionDetection.Admin.SmartPanel();
            this.iddscommunitySettingsNavigation = new IDDSCommunity.IntrusionDetection.Admin.IDDSCommunitySettingsNavigation();
            this.SuspendLayout();
            // 
            // configurationPanel
            // 
            this.configurationPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.configurationPanel.AutoScroll = true;
            this.configurationPanel.BorderColor = System.Drawing.SystemColors.ControlText;
            this.configurationPanel.Location = new System.Drawing.Point(435, 16);
            this.configurationPanel.Name = "configurationPanel";
            this.configurationPanel.PaintBorder = false;
            this.configurationPanel.Size = new System.Drawing.Size(533, 515);
            this.configurationPanel.TabIndex = 2;
            // 
            // iddscommunitySettingsNavigation
            // 
            this.iddscommunitySettingsNavigation.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)));
            this.iddscommunitySettingsNavigation.BackColor = System.Drawing.Color.White;
            this.iddscommunitySettingsNavigation.Location = new System.Drawing.Point(12, 16);
            this.iddscommunitySettingsNavigation.Name = "iddscommunitySettingsNavigation";
            this.iddscommunitySettingsNavigation.SeparatorColor = System.Drawing.Color.FromArgb(((int)(((byte)(191)))), ((int)(((byte)(191)))), ((int)(((byte)(191)))));
            this.iddscommunitySettingsNavigation.ShowSeparator = true;
            this.iddscommunitySettingsNavigation.ShowTopMenu = false;
            this.iddscommunitySettingsNavigation.Size = new System.Drawing.Size(415, 515);
            this.iddscommunitySettingsNavigation.TabIndex = 0;
            this.iddscommunitySettingsNavigation.NavigationChanged += new System.EventHandler(this.iddscommunitySettingsNavigation_NavigationChanged);
            // 
            // IDDSCommunityAgentConfiguration
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.configurationPanel);
            this.Controls.Add(this.iddscommunitySettingsNavigation);
            this.Name = "IDDSCommunityAgentConfiguration";
            this.Size = new System.Drawing.Size(978, 549);
            this.ResumeLayout(false);

        }

        #endregion

        private IDDSCommunitySettingsNavigation iddscommunitySettingsNavigation;
        private SmartPanel configurationPanel;
    }
}
