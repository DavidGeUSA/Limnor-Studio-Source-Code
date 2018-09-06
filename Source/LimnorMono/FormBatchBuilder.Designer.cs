namespace LimnorVOB
{
    partial class FormBatchBuilder
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
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormBatchBuilder));
			this.label1 = new System.Windows.Forms.Label();
			this.textBoxRootDir = new System.Windows.Forms.TextBox();
			this.buttonDir = new System.Windows.Forms.Button();
			this.buttonStart = new System.Windows.Forms.Button();
			this.splitContainer1 = new System.Windows.Forms.SplitContainer();
			this.treeView1 = new System.Windows.Forms.TreeView();
			this.imageList1 = new System.Windows.Forms.ImageList(this.components);
			this.splitContainer2 = new System.Windows.Forms.SplitContainer();
			this.listBox1 = new System.Windows.Forms.ListBox();
			this.textBoxMsg = new System.Windows.Forms.TextBox();
			this.lblInfo = new System.Windows.Forms.Label();
			this.buttonCancel = new System.Windows.Forms.Button();
			this.label2 = new System.Windows.Forms.Label();
			this.txtBatch = new System.Windows.Forms.TextBox();
			this.btBatchFile = new System.Windows.Forms.Button();
			this.progressBar1 = new System.Windows.Forms.ProgressBar();
			this.lblCounts = new System.Windows.Forms.Label();
			((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
			this.splitContainer1.Panel1.SuspendLayout();
			this.splitContainer1.Panel2.SuspendLayout();
			this.splitContainer1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).BeginInit();
			this.splitContainer2.Panel1.SuspendLayout();
			this.splitContainer2.Panel2.SuspendLayout();
			this.splitContainer2.SuspendLayout();
			this.SuspendLayout();
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(12, 55);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(62, 13);
			this.label1.TabIndex = 0;
			this.label1.Text = "Root folder:";
			// 
			// textBoxRootDir
			// 
			this.textBoxRootDir.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.textBoxRootDir.Location = new System.Drawing.Point(80, 52);
			this.textBoxRootDir.Name = "textBoxRootDir";
			this.textBoxRootDir.Size = new System.Drawing.Size(391, 20);
			this.textBoxRootDir.TabIndex = 1;
			this.textBoxRootDir.Text = "C:\\Samples";
			// 
			// buttonDir
			// 
			this.buttonDir.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonDir.Location = new System.Drawing.Point(477, 52);
			this.buttonDir.Name = "buttonDir";
			this.buttonDir.Size = new System.Drawing.Size(34, 23);
			this.buttonDir.TabIndex = 2;
			this.buttonDir.Text = "...";
			this.buttonDir.UseVisualStyleBackColor = true;
			this.buttonDir.Click += new System.EventHandler(this.buttonDir_Click);
			// 
			// buttonStart
			// 
			this.buttonStart.Location = new System.Drawing.Point(15, 78);
			this.buttonStart.Name = "buttonStart";
			this.buttonStart.Size = new System.Drawing.Size(75, 23);
			this.buttonStart.TabIndex = 3;
			this.buttonStart.Text = "Start";
			this.buttonStart.UseVisualStyleBackColor = true;
			this.buttonStart.Click += new System.EventHandler(this.buttonStart_Click);
			// 
			// splitContainer1
			// 
			this.splitContainer1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.splitContainer1.Location = new System.Drawing.Point(3, 144);
			this.splitContainer1.Name = "splitContainer1";
			// 
			// splitContainer1.Panel1
			// 
			this.splitContainer1.Panel1.Controls.Add(this.treeView1);
			// 
			// splitContainer1.Panel2
			// 
			this.splitContainer1.Panel2.Controls.Add(this.splitContainer2);
			this.splitContainer1.Size = new System.Drawing.Size(508, 205);
			this.splitContainer1.SplitterDistance = 169;
			this.splitContainer1.TabIndex = 4;
			// 
			// treeView1
			// 
			this.treeView1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.treeView1.ImageIndex = 0;
			this.treeView1.ImageList = this.imageList1;
			this.treeView1.Location = new System.Drawing.Point(0, 0);
			this.treeView1.Name = "treeView1";
			this.treeView1.SelectedImageIndex = 0;
			this.treeView1.Size = new System.Drawing.Size(169, 205);
			this.treeView1.TabIndex = 0;
			this.treeView1.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.treeView1_AfterSelect);
			// 
			// imageList1
			// 
			this.imageList1.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList1.ImageStream")));
			this.imageList1.TransparentColor = System.Drawing.Color.Transparent;
			this.imageList1.Images.SetKeyName(0, "_array.ico");
			this.imageList1.Images.SetKeyName(1, "_cancel.ico");
			this.imageList1.Images.SetKeyName(2, "_ok.ico");
			// 
			// splitContainer2
			// 
			this.splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
			this.splitContainer2.Location = new System.Drawing.Point(0, 0);
			this.splitContainer2.Name = "splitContainer2";
			this.splitContainer2.Orientation = System.Windows.Forms.Orientation.Horizontal;
			// 
			// splitContainer2.Panel1
			// 
			this.splitContainer2.Panel1.Controls.Add(this.listBox1);
			// 
			// splitContainer2.Panel2
			// 
			this.splitContainer2.Panel2.Controls.Add(this.textBoxMsg);
			this.splitContainer2.Size = new System.Drawing.Size(335, 205);
			this.splitContainer2.SplitterDistance = 130;
			this.splitContainer2.TabIndex = 0;
			// 
			// listBox1
			// 
			this.listBox1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.listBox1.FormattingEnabled = true;
			this.listBox1.Location = new System.Drawing.Point(0, 0);
			this.listBox1.Name = "listBox1";
			this.listBox1.Size = new System.Drawing.Size(335, 130);
			this.listBox1.TabIndex = 0;
			this.listBox1.SelectedIndexChanged += new System.EventHandler(this.listBox1_SelectedIndexChanged);
			// 
			// textBoxMsg
			// 
			this.textBoxMsg.Dock = System.Windows.Forms.DockStyle.Fill;
			this.textBoxMsg.Location = new System.Drawing.Point(0, 0);
			this.textBoxMsg.Multiline = true;
			this.textBoxMsg.Name = "textBoxMsg";
			this.textBoxMsg.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			this.textBoxMsg.Size = new System.Drawing.Size(335, 71);
			this.textBoxMsg.TabIndex = 0;
			// 
			// lblInfo
			// 
			this.lblInfo.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.lblInfo.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.lblInfo.Location = new System.Drawing.Point(3, 106);
			this.lblInfo.Name = "lblInfo";
			this.lblInfo.Size = new System.Drawing.Size(511, 35);
			this.lblInfo.TabIndex = 5;
			// 
			// buttonCancel
			// 
			this.buttonCancel.Enabled = false;
			this.buttonCancel.Location = new System.Drawing.Point(97, 78);
			this.buttonCancel.Name = "buttonCancel";
			this.buttonCancel.Size = new System.Drawing.Size(75, 23);
			this.buttonCancel.TabIndex = 6;
			this.buttonCancel.Text = "&Cancel";
			this.buttonCancel.UseVisualStyleBackColor = true;
			this.buttonCancel.Click += new System.EventHandler(this.buttonCancel_Click);
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(12, 26);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(54, 13);
			this.label2.TabIndex = 7;
			this.label2.Text = "Btach file:";
			// 
			// txtBatch
			// 
			this.txtBatch.Location = new System.Drawing.Point(80, 23);
			this.txtBatch.Name = "txtBatch";
			this.txtBatch.Size = new System.Drawing.Size(391, 20);
			this.txtBatch.TabIndex = 8;
			// 
			// btBatchFile
			// 
			this.btBatchFile.Location = new System.Drawing.Point(477, 21);
			this.btBatchFile.Name = "btBatchFile";
			this.btBatchFile.Size = new System.Drawing.Size(33, 23);
			this.btBatchFile.TabIndex = 9;
			this.btBatchFile.Text = "...";
			this.btBatchFile.UseVisualStyleBackColor = true;
			this.btBatchFile.Click += new System.EventHandler(this.btBatchFile_Click);
			// 
			// progressBar1
			// 
			this.progressBar1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.progressBar1.Location = new System.Drawing.Point(178, 78);
			this.progressBar1.Name = "progressBar1";
			this.progressBar1.Size = new System.Drawing.Size(233, 23);
			this.progressBar1.TabIndex = 10;
			// 
			// lblCounts
			// 
			this.lblCounts.AutoSize = true;
			this.lblCounts.Location = new System.Drawing.Point(417, 83);
			this.lblCounts.Name = "lblCounts";
			this.lblCounts.Size = new System.Drawing.Size(24, 13);
			this.lblCounts.TabIndex = 11;
			this.lblCounts.Text = "0/0";
			// 
			// FormBatchBuilder
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(514, 348);
			this.Controls.Add(this.lblCounts);
			this.Controls.Add(this.progressBar1);
			this.Controls.Add(this.btBatchFile);
			this.Controls.Add(this.txtBatch);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.buttonCancel);
			this.Controls.Add(this.lblInfo);
			this.Controls.Add(this.splitContainer1);
			this.Controls.Add(this.buttonStart);
			this.Controls.Add(this.buttonDir);
			this.Controls.Add(this.textBoxRootDir);
			this.Controls.Add(this.label1);
			this.MinimizeBox = false;
			this.Name = "FormBatchBuilder";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "Batch Builder";
			this.splitContainer1.Panel1.ResumeLayout(false);
			this.splitContainer1.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
			this.splitContainer1.ResumeLayout(false);
			this.splitContainer2.Panel1.ResumeLayout(false);
			this.splitContainer2.Panel2.ResumeLayout(false);
			this.splitContainer2.Panel2.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).EndInit();
			this.splitContainer2.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox textBoxRootDir;
        private System.Windows.Forms.Button buttonDir;
        private System.Windows.Forms.Button buttonStart;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.TreeView treeView1;
        private System.Windows.Forms.SplitContainer splitContainer2;
        private System.Windows.Forms.ListBox listBox1;
        private System.Windows.Forms.TextBox textBoxMsg;
        private System.Windows.Forms.Label lblInfo;
        private System.Windows.Forms.ImageList imageList1;
        private System.Windows.Forms.Button buttonCancel;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.TextBox txtBatch;
		private System.Windows.Forms.Button btBatchFile;
		private System.Windows.Forms.ProgressBar progressBar1;
		private System.Windows.Forms.Label lblCounts;
    }
}