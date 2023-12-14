namespace PopuliQB1
{
    partial class MainForm
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
        private void InitializeComponent()
        {
            this.btPopQBSync = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.tbQBCompanyName = new System.Windows.Forms.TextBox();
            this.lStatus = new System.Windows.Forms.Label();
            this.btConnectQB = new System.Windows.Forms.Button();
            this.rtbStatus = new System.Windows.Forms.RichTextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.dtStartTxn = new System.Windows.Forms.DateTimePicker();
            this.rtbStatistic = new System.Windows.Forms.RichTextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // btPopQBSync
            // 
            this.btPopQBSync.Location = new System.Drawing.Point(69, 139);
            this.btPopQBSync.Name = "btPopQBSync";
            this.btPopQBSync.Size = new System.Drawing.Size(137, 43);
            this.btPopQBSync.TabIndex = 0;
            this.btPopQBSync.Text = "Populi to QB Sync";
            this.btPopQBSync.UseVisualStyleBackColor = true;
            this.btPopQBSync.Click += new System.EventHandler(this.btPopQBSync_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(66, 39);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(157, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "QuickBooks Desktop company:";
            // 
            // tbQBCompanyName
            // 
            this.tbQBCompanyName.Enabled = false;
            this.tbQBCompanyName.Location = new System.Drawing.Point(230, 31);
            this.tbQBCompanyName.Name = "tbQBCompanyName";
            this.tbQBCompanyName.ReadOnly = true;
            this.tbQBCompanyName.ScrollBars = System.Windows.Forms.ScrollBars.Horizontal;
            this.tbQBCompanyName.Size = new System.Drawing.Size(209, 20);
            this.tbQBCompanyName.TabIndex = 2;
            // 
            // lStatus
            // 
            this.lStatus.AutoSize = true;
            this.lStatus.Location = new System.Drawing.Point(295, 199);
            this.lStatus.Name = "lStatus";
            this.lStatus.Size = new System.Drawing.Size(40, 13);
            this.lStatus.TabIndex = 3;
            this.lStatus.Text = "Status:";
            // 
            // btConnectQB
            // 
            this.btConnectQB.Location = new System.Drawing.Point(69, 79);
            this.btConnectQB.Name = "btConnectQB";
            this.btConnectQB.Size = new System.Drawing.Size(137, 43);
            this.btConnectQB.TabIndex = 5;
            this.btConnectQB.Text = "Connect to QB";
            this.btConnectQB.UseVisualStyleBackColor = true;
            this.btConnectQB.Click += new System.EventHandler(this.btConnectQB_Click);
            // 
            // rtbStatus
            // 
            this.rtbStatus.Location = new System.Drawing.Point(298, 220);
            this.rtbStatus.Name = "rtbStatus";
            this.rtbStatus.ReadOnly = true;
            this.rtbStatus.Size = new System.Drawing.Size(291, 96);
            this.rtbStatus.TabIndex = 7;
            this.rtbStatus.Text = "";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(244, 79);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(116, 13);
            this.label2.TabIndex = 8;
            this.label2.Text = "Start transactions date:";
            // 
            // dtStartTxn
            // 
            this.dtStartTxn.Location = new System.Drawing.Point(363, 73);
            this.dtStartTxn.Name = "dtStartTxn";
            this.dtStartTxn.Size = new System.Drawing.Size(141, 20);
            this.dtStartTxn.TabIndex = 10;
            // 
            // rtbStatistic
            // 
            this.rtbStatistic.Location = new System.Drawing.Point(554, 55);
            this.rtbStatistic.Name = "rtbStatistic";
            this.rtbStatistic.Size = new System.Drawing.Size(206, 96);
            this.rtbStatistic.TabIndex = 11;
            this.rtbStatistic.Text = "";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(551, 39);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(47, 13);
            this.label3.TabIndex = 12;
            this.label3.Text = "Statistic:";
            // 
            // MainForm
            // 
            this.AllowDrop = true;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.rtbStatistic);
            this.Controls.Add(this.dtStartTxn);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.rtbStatus);
            this.Controls.Add(this.btConnectQB);
            this.Controls.Add(this.lStatus);
            this.Controls.Add(this.tbQBCompanyName);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.btPopQBSync);
            this.Name = "MainForm";
            this.Text = "Populi To QuickBooks Sync";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btPopQBSync;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox tbQBCompanyName;
        private System.Windows.Forms.Label lStatus;
        private System.Windows.Forms.Button btConnectQB;
        private System.Windows.Forms.RichTextBox rtbStatus;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.DateTimePicker dtStartTxn;
        private System.Windows.Forms.RichTextBox rtbStatistic;
        private System.Windows.Forms.Label label3;
    }
}

