namespace IDDSCommunity.IntrusionDetection.Admin {
    partial class PanelSafeNetworks {
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
            this.checkBoxConfigureSafeNetworks = new System.Windows.Forms.CheckBox();
            this.pictureBoxEdit = new IDDSCommunity.IntrusionDetection.Admin.AccessiblePictureBoxButton();
            this.listBoxSafeNetworks = new System.Windows.Forms.ListBox();
            this.pictureBoxSave = new IDDSCommunity.IntrusionDetection.Admin.AccessiblePictureBoxButton();
            this.smartPanelAdd = new IDDSCommunity.IntrusionDetection.Admin.SmartPanel();
            this.smartLabelInvalidNetwork = new IDDSCommunity.IntrusionDetection.Admin.SmartLabel();
            this.textBoxAddNetwork = new System.Windows.Forms.TextBox();
            this.smartLabel2 = new IDDSCommunity.IntrusionDetection.Admin.SmartLabel();
            this.smartLabel1 = new IDDSCommunity.IntrusionDetection.Admin.SmartLabel();
            this.button1 = new System.Windows.Forms.Button();
            this.buttonAddNetwork = new System.Windows.Forms.Button();
            this.smartPanel1 = new IDDSCommunity.IntrusionDetection.Admin.SmartPanel();
            this.pictureBoxAdd = new IDDSCommunity.IntrusionDetection.Admin.AccessiblePictureBoxButton();
            this.pictureBoxDelete = new IDDSCommunity.IntrusionDetection.Admin.AccessiblePictureBoxButton();
            this.smartLabel5 = new IDDSCommunity.IntrusionDetection.Admin.SmartLabel();
            this.buttonSave = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxEdit)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxSave)).BeginInit();
            this.smartPanelAdd.SuspendLayout();
            this.smartPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxAdd)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxDelete)).BeginInit();
            this.SuspendLayout();
            //
            // checkBoxConfigureSafeNetworks
            //
            this.checkBoxConfigureSafeNetworks.AutoSize = true;
            this.checkBoxConfigureSafeNetworks.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.checkBoxConfigureSafeNetworks.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(102)))), ((int)(((byte)(102)))), ((int)(((byte)(102)))));
            this.checkBoxConfigureSafeNetworks.Location = new System.Drawing.Point(27, 47);
            this.checkBoxConfigureSafeNetworks.Name = "checkBoxConfigureSafeNetworks";
            this.checkBoxConfigureSafeNetworks.Size = new System.Drawing.Size(209, 17);
            this.checkBoxConfigureSafeNetworks.TabIndex = 1;
            this.checkBoxConfigureSafeNetworks.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Configure safe networks (white list)");
            this.checkBoxConfigureSafeNetworks.UseVisualStyleBackColor = true;
            //
            // pictureBoxEdit
            //
            this.pictureBoxEdit.AccessibleName = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Edit");
            this.pictureBoxEdit.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.pictureBoxEdit.Image = global::IDDSCommunity.IntrusionDetection.Admin.Properties.Resources.button25px_edit;
            this.pictureBoxEdit.Location = new System.Drawing.Point(397, 0);
            this.pictureBoxEdit.Name = "pictureBoxEdit";
            this.pictureBoxEdit.Size = new System.Drawing.Size(25, 25);
            this.pictureBoxEdit.TabIndex = 2;
            this.pictureBoxEdit.Visible = false;
            this.pictureBoxEdit.Click += new System.EventHandler(this.pictureBoxEdit_Click);
            this.pictureBoxEdit.MouseDown += new System.Windows.Forms.MouseEventHandler(this.pictureBox_MouseDown);
            this.pictureBoxEdit.MouseUp += new System.Windows.Forms.MouseEventHandler(this.pictureBox_MouseUp);
            //
            // listBoxSafeNetworks
            //
            this.listBoxSafeNetworks.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.listBoxSafeNetworks.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.listBoxSafeNetworks.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.listBoxSafeNetworks.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(102)))), ((int)(((byte)(102)))), ((int)(((byte)(102)))));
            this.listBoxSafeNetworks.FormattingEnabled = true;
            this.listBoxSafeNetworks.Location = new System.Drawing.Point(27, 106);
            this.listBoxSafeNetworks.MinimumSize = new System.Drawing.Size(370, 200);
            this.listBoxSafeNetworks.Name = "listBoxSafeNetworks";
            this.listBoxSafeNetworks.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
            this.listBoxSafeNetworks.Size = new System.Drawing.Size(370, 236);
            this.listBoxSafeNetworks.TabIndex = 7;
            this.listBoxSafeNetworks.DoubleClick += new System.EventHandler(this.listBoxSafeNetworks_DoubleClick);
            this.listBoxSafeNetworks.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.listBoxSafeNetworks_KeyPress);
            //
            // pictureBoxSave
            //
            this.pictureBoxSave.AccessibleName = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Save");
            this.pictureBoxSave.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.pictureBoxSave.Image = global::IDDSCommunity.IntrusionDetection.Admin.Properties.Resources.button25px_save;
            this.pictureBoxSave.Location = new System.Drawing.Point(366, 0);
            this.pictureBoxSave.Name = "pictureBoxSave";
            this.pictureBoxSave.Size = new System.Drawing.Size(25, 25);
            this.pictureBoxSave.TabIndex = 3;
            this.pictureBoxSave.Visible = false;
            this.pictureBoxSave.Click += new System.EventHandler(this.pictureBoxSave_Click);
            this.pictureBoxSave.MouseDown += new System.Windows.Forms.MouseEventHandler(this.pictureBox_MouseDown);
            this.pictureBoxSave.MouseUp += new System.Windows.Forms.MouseEventHandler(this.pictureBox_MouseUp);
            //
            // smartPanelAdd
            //
            this.smartPanelAdd.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.smartPanelAdd.BackColor = System.Drawing.Color.White;
            this.smartPanelAdd.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(191)))), ((int)(((byte)(191)))), ((int)(((byte)(191)))));
            this.smartPanelAdd.Controls.Add(this.smartLabelInvalidNetwork);
            this.smartPanelAdd.Controls.Add(this.textBoxAddNetwork);
            this.smartPanelAdd.Controls.Add(this.smartLabel2);
            this.smartPanelAdd.Controls.Add(this.smartLabel1);
            this.smartPanelAdd.Controls.Add(this.button1);
            this.smartPanelAdd.Controls.Add(this.buttonAddNetwork);
            this.smartPanelAdd.Location = new System.Drawing.Point(42, 119);
            this.smartPanelAdd.Name = "smartPanelAdd";
            this.smartPanelAdd.PaintBorder = true;
            this.smartPanelAdd.Size = new System.Drawing.Size(336, 216);
            this.smartPanelAdd.TabIndex = 8;
            this.smartPanelAdd.Visible = false;
            //
            // smartLabelInvalidNetwork
            //
            this.smartLabelInvalidNetwork.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.smartLabelInvalidNetwork.ForeColor = System.Drawing.Color.Red;
            this.smartLabelInvalidNetwork.Location = new System.Drawing.Point(14, 133);
            this.smartLabelInvalidNetwork.Name = "smartLabelInvalidNetwork";
            this.smartLabelInvalidNetwork.Selected = false;
            this.smartLabelInvalidNetwork.SelectedColor = System.Drawing.Color.Empty;
            this.smartLabelInvalidNetwork.Size = new System.Drawing.Size(307, 41);
            this.smartLabelInvalidNetwork.TabIndex = 12;
            this.smartLabelInvalidNetwork.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("err");
            this.smartLabelInvalidNetwork.Visible = false;
            //
            // textBoxAddNetwork
            //
            this.textBoxAddNetwork.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBoxAddNetwork.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBoxAddNetwork.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(102)))), ((int)(((byte)(102)))), ((int)(((byte)(102)))));
            this.textBoxAddNetwork.Location = new System.Drawing.Point(17, 31);
            this.textBoxAddNetwork.Name = "textBoxAddNetwork";
            this.textBoxAddNetwork.Size = new System.Drawing.Size(263, 22);
            this.textBoxAddNetwork.TabIndex = 10;
            this.textBoxAddNetwork.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.textBoxAddNetwork_KeyPress);
            //
            // smartLabel2
            //
            this.smartLabel2.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.smartLabel2.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(102)))), ((int)(((byte)(102)))), ((int)(((byte)(102)))));
            this.smartLabel2.Location = new System.Drawing.Point(14, 68);
            this.smartLabel2.Name = "smartLabel2";
            this.smartLabel2.Selected = false;
            this.smartLabel2.SelectedColor = System.Drawing.Color.Empty;
            this.smartLabel2.Size = new System.Drawing.Size(283, 55);
            this.smartLabel2.TabIndex = 11;
            this.smartLabel2.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Examples:\r\n192.168.0.17\r\n192.168.0.0/24\r\n192.168.0.0/255.255.255.0");
            //
            // smartLabel1
            //
            this.smartLabel1.AutoSize = true;
            this.smartLabel1.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.smartLabel1.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(102)))), ((int)(((byte)(102)))), ((int)(((byte)(102)))));
            this.smartLabel1.Location = new System.Drawing.Point(14, 15);
            this.smartLabel1.Name = "smartLabel1";
            this.smartLabel1.Selected = false;
            this.smartLabel1.SelectedColor = System.Drawing.Color.Empty;
            this.smartLabel1.Size = new System.Drawing.Size(119, 13);
            this.smartLabel1.TabIndex = 9;
            this.smartLabel1.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("IP address or network");
            //
            // button1
            //
            this.button1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.button1.BackColor = System.Drawing.Color.White;
            this.button1.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button1.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button1.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(102)))), ((int)(((byte)(102)))), ((int)(((byte)(102)))));
            this.button1.Location = new System.Drawing.Point(178, 177);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(102, 26);
            this.button1.TabIndex = 14;
            this.button1.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Cancel");
            this.button1.UseVisualStyleBackColor = false;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            //
            // buttonAddNetwork
            //
            this.buttonAddNetwork.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonAddNetwork.BackColor = System.Drawing.Color.White;
            this.buttonAddNetwork.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonAddNetwork.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonAddNetwork.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(102)))), ((int)(((byte)(102)))), ((int)(((byte)(102)))));
            this.buttonAddNetwork.Location = new System.Drawing.Point(67, 177);
            this.buttonAddNetwork.Name = "buttonAddNetwork";
            this.buttonAddNetwork.Size = new System.Drawing.Size(102, 26);
            this.buttonAddNetwork.TabIndex = 13;
            this.buttonAddNetwork.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Add");
            this.buttonAddNetwork.UseVisualStyleBackColor = false;
            this.buttonAddNetwork.Click += new System.EventHandler(this.buttonAddNetwork_Click);
            //
            // smartPanel1
            //
            this.smartPanel1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.smartPanel1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(243)))), ((int)(((byte)(246)))), ((int)(((byte)(248)))));
            this.smartPanel1.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(191)))), ((int)(((byte)(191)))), ((int)(((byte)(191)))));
            this.smartPanel1.Controls.Add(this.pictureBoxAdd);
            this.smartPanel1.Controls.Add(this.pictureBoxDelete);
            this.smartPanel1.Location = new System.Drawing.Point(27, 76);
            this.smartPanel1.Name = "smartPanel1";
            this.smartPanel1.PaintBorder = true;
            this.smartPanel1.Size = new System.Drawing.Size(370, 30);
            this.smartPanel1.TabIndex = 4;
            //
            // pictureBoxAdd
            //
            this.pictureBoxAdd.AccessibleName = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Add");
            this.pictureBoxAdd.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.pictureBoxAdd.Image = global::IDDSCommunity.IntrusionDetection.Admin.Properties.Resources.button25px_add;
            this.pictureBoxAdd.Location = new System.Drawing.Point(312, 3);
            this.pictureBoxAdd.Name = "pictureBoxAdd";
            this.pictureBoxAdd.Size = new System.Drawing.Size(24, 24);
            this.pictureBoxAdd.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBoxAdd.TabIndex = 5;
            this.pictureBoxAdd.Click += new System.EventHandler(this.pictureBoxAdd_Click);
            this.pictureBoxAdd.MouseDown += new System.Windows.Forms.MouseEventHandler(this.pictureBox_MouseDown);
            this.pictureBoxAdd.MouseUp += new System.Windows.Forms.MouseEventHandler(this.pictureBox_MouseUp);
            //
            // pictureBoxDelete
            //
            this.pictureBoxDelete.AccessibleName = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Delete");
            this.pictureBoxDelete.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.pictureBoxDelete.Image = global::IDDSCommunity.IntrusionDetection.Admin.Properties.Resources.button25px_delete;
            this.pictureBoxDelete.Location = new System.Drawing.Point(342, 3);
            this.pictureBoxDelete.Name = "pictureBoxDelete";
            this.pictureBoxDelete.Size = new System.Drawing.Size(24, 24);
            this.pictureBoxDelete.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBoxDelete.TabIndex = 6;
            this.pictureBoxDelete.Click += new System.EventHandler(this.pictureBoxDelete_Click);
            this.pictureBoxDelete.MouseDown += new System.Windows.Forms.MouseEventHandler(this.pictureBox_MouseDown);
            this.pictureBoxDelete.MouseUp += new System.Windows.Forms.MouseEventHandler(this.pictureBox_MouseUp);
            //
            // smartLabel5
            //
            this.smartLabel5.AutoSize = true;
            this.smartLabel5.Font = new System.Drawing.Font("Segoe UI", 11F);
            this.smartLabel5.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(15)))), ((int)(((byte)(118)))), ((int)(((byte)(110)))));
            this.smartLabel5.Location = new System.Drawing.Point(23, 0);
            this.smartLabel5.Margin = new System.Windows.Forms.Padding(0);
            this.smartLabel5.Name = "smartLabel5";
            this.smartLabel5.Selected = false;
            this.smartLabel5.SelectedColor = System.Drawing.Color.Empty;
            this.smartLabel5.Size = new System.Drawing.Size(174, 20);
            this.smartLabel5.TabIndex = 0;
            this.smartLabel5.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("Safe networks (white list)");
            this.smartLabel5.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // buttonSave
            //
            this.buttonSave.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.buttonSave.BackColor = System.Drawing.Color.FromArgb(15, 118, 110);
            this.buttonSave.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonSave.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonSave.ForeColor = System.Drawing.Color.White;
            this.buttonSave.Location = new System.Drawing.Point(109, 356);
            this.buttonSave.Name = "buttonSave";
            this.buttonSave.Size = new System.Drawing.Size(120, 32);
            this.buttonSave.TabIndex = 15;
            this.buttonSave.Text = global::IDDSCommunity.IntrusionDetection.Shared.Localization.Strings.Get("&Save");
            this.buttonSave.UseVisualStyleBackColor = false;
            this.buttonSave.Click += new System.EventHandler(this.buttonSave_Click);
            //
            // PanelSafeNetworks
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoScroll = true;
            this.Controls.Add(this.buttonSave);
            this.Controls.Add(this.pictureBoxSave);
            this.Controls.Add(this.smartPanelAdd);
            this.Controls.Add(this.smartPanel1);
            this.Controls.Add(this.listBoxSafeNetworks);
            this.Controls.Add(this.smartLabel5);
            this.Controls.Add(this.pictureBoxEdit);
            this.Controls.Add(this.checkBoxConfigureSafeNetworks);
            this.MinimumSize = new System.Drawing.Size(423, 385);
            this.Name = "PanelSafeNetworks";
            this.Size = new System.Drawing.Size(423, 385);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxEdit)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxSave)).EndInit();
            this.smartPanelAdd.ResumeLayout(false);
            this.smartPanelAdd.PerformLayout();
            this.smartPanel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxAdd)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxDelete)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private SmartLabel smartLabel5;
        private AccessiblePictureBoxButton pictureBoxEdit;
        private System.Windows.Forms.CheckBox checkBoxConfigureSafeNetworks;
        private System.Windows.Forms.ListBox listBoxSafeNetworks;
        private SmartPanel smartPanel1;
        private AccessiblePictureBoxButton pictureBoxAdd;
        private AccessiblePictureBoxButton pictureBoxDelete;
        private SmartPanel smartPanelAdd;
        private SmartLabel smartLabel1;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button buttonAddNetwork;
        private System.Windows.Forms.TextBox textBoxAddNetwork;
        private SmartLabel smartLabel2;
        private AccessiblePictureBoxButton pictureBoxSave;
        private SmartLabel smartLabelInvalidNetwork;
        private System.Windows.Forms.Button buttonSave;
    }
}
