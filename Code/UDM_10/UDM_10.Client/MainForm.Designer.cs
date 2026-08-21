namespace UDM_10.Client
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            topPanel = new Panel();
            btnUploadAll = new Button();
            btnUploadSelected = new Button();
            btnTestStatus = new Button();
            btnChooseFiles = new Button();
            dropZone = new Panel();
            dropLabel = new Label();
            lblQueueStatus = new Label();
            lblTotalFiles = new Label();
            lblConcurrencyInfo = new Label();
            gridFiles = new DataGridView();
            bottomPanel = new Panel();
            topPanel.SuspendLayout();
            dropZone.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)gridFiles).BeginInit();
            bottomPanel.SuspendLayout();
            SuspendLayout();
            // 
            // topPanel
            // 
            topPanel.BackColor = Color.AliceBlue;
            topPanel.Controls.Add(btnUploadAll);
            topPanel.Controls.Add(btnUploadSelected);
            topPanel.Controls.Add(btnTestStatus);
            topPanel.Controls.Add(btnChooseFiles);
            topPanel.Controls.Add(dropZone);
            topPanel.Dock = DockStyle.Top;
            topPanel.Location = new Point(0, 0);
            topPanel.Name = "topPanel";
            topPanel.Size = new Size(1150, 110);
            topPanel.TabIndex = 0;
            // 
            // btnUploadAll
            // 
            btnUploadAll.Location = new Point(721, 16);
            btnUploadAll.Name = "btnUploadAll";
            btnUploadAll.AutoSize = true;
            btnUploadAll.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btnUploadAll.Padding = new Padding(14, 6, 14, 6);
            btnUploadAll.TabIndex = 3;
            btnUploadAll.Text = "Upload tất cả";
            btnUploadAll.UseVisualStyleBackColor = true;
            btnUploadAll.Click += btnUploadAll_Click;
            // 
            // btnUploadSelected
            // 
            btnUploadSelected.Location = new Point(721, 62);
            btnUploadSelected.Name = "btnUploadSelected";
            btnUploadSelected.AutoSize = true;
            btnUploadSelected.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btnUploadSelected.Padding = new Padding(14, 6, 14, 6);
            btnUploadSelected.TabIndex = 4;
            btnUploadSelected.Text = "Upload đã chọn";
            btnUploadSelected.UseVisualStyleBackColor = true;
            btnUploadSelected.Click += btnUploadSelected_Click;
            // 
            // btnTestStatus
            // 
            btnTestStatus.Location = new Point(572, 24);
            btnTestStatus.Name = "btnTestStatus";
            btnTestStatus.Size = new Size(143, 32);
            btnTestStatus.TabIndex = 2;
            btnTestStatus.Text = "Test đổi trạng thái";
            btnTestStatus.UseVisualStyleBackColor = true;
            btnTestStatus.Click += btnTestStatus_Click;
            // 
            // btnChooseFiles
            // 
            btnChooseFiles.Location = new Point(436, 24);
            btnChooseFiles.Name = "btnChooseFiles";
            btnChooseFiles.Size = new Size(130, 32);
            btnChooseFiles.TabIndex = 1;
            btnChooseFiles.Text = "Chọn tệp...";
            btnChooseFiles.UseVisualStyleBackColor = true;
            btnChooseFiles.Click += btnChooseFiles_Click;
            // 
            // dropZone
            // 
            dropZone.AllowDrop = true;
            dropZone.BackColor = Color.WhiteSmoke;
            dropZone.BorderStyle = BorderStyle.FixedSingle;
            dropZone.Controls.Add(dropLabel);
            dropZone.Location = new Point(0, 0);
            dropZone.Name = "dropZone";
            dropZone.Size = new Size(420, 80);
            dropZone.TabIndex = 0;
            dropZone.DragDrop += dropZone_DragDrop;
            dropZone.DragEnter += dropZone_DragEnter;
            // 
            // dropLabel
            // 
            dropLabel.AutoSize = true;
            dropLabel.Dock = DockStyle.Fill;
            dropLabel.Location = new Point(0, 0);
            dropLabel.Name = "dropLabel";
            dropLabel.Size = new Size(157, 20);
            dropLabel.TabIndex = 0;
            dropLabel.Text = "Kéo && thả file vào đây";
            dropLabel.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblQueueStatus
            // 
            lblQueueStatus.AutoSize = true;
            lblQueueStatus.BackColor = SystemColors.HighlightText;
            lblQueueStatus.Location = new Point(152, 24);
            lblQueueStatus.Name = "lblQueueStatus";
            lblQueueStatus.Size = new Size(50, 20);
            lblQueueStatus.TabIndex = 6;
            lblQueueStatus.Text = "label1";
            // 
            // lblTotalFiles
            // 
            lblTotalFiles.AutoSize = true;
            lblTotalFiles.BackColor = SystemColors.HighlightText;
            lblTotalFiles.Location = new Point(12, 24);
            lblTotalFiles.Name = "lblTotalFiles";
            lblTotalFiles.Size = new Size(50, 20);
            lblTotalFiles.TabIndex = 5;
            lblTotalFiles.Text = "label1";
            // 
            // lblConcurrencyInfo
            // 
            lblConcurrencyInfo.AutoSize = true;
            lblConcurrencyInfo.BackColor = SystemColors.HighlightText;
            lblConcurrencyInfo.Location = new Point(500, 24);
            lblConcurrencyInfo.Name = "lblConcurrencyInfo";
            lblConcurrencyInfo.Size = new Size(50, 20);
            lblConcurrencyInfo.TabIndex = 4;
            lblConcurrencyInfo.Text = "label1";
            // 
            // gridFiles
            // 
            gridFiles.AllowUserToAddRows = false;
            gridFiles.AllowUserToDeleteRows = false;
            gridFiles.BackgroundColor = Color.AliceBlue;
            gridFiles.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            gridFiles.Dock = DockStyle.Fill;
            gridFiles.Location = new Point(0, 96);
            gridFiles.Name = "gridFiles";
            gridFiles.RowHeadersVisible = false;
            gridFiles.RowHeadersWidth = 51;
            gridFiles.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            gridFiles.Size = new Size(1082, 507);
            gridFiles.TabIndex = 1;
            gridFiles.CellFormatting += gridFiles_CellFormatting;
            // 
            // bottomPanel
            // 
            bottomPanel.BackColor = Color.AliceBlue;
            bottomPanel.Controls.Add(lblQueueStatus);
            bottomPanel.Controls.Add(lblTotalFiles);
            bottomPanel.Controls.Add(lblConcurrencyInfo);
            bottomPanel.Dock = DockStyle.Bottom;
            bottomPanel.Location = new Point(0, 603);
            bottomPanel.Name = "bottomPanel";
            bottomPanel.Padding = new Padding(8);
            bottomPanel.Size = new Size(1082, 70);
            bottomPanel.TabIndex = 2;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1150, 673);
            Controls.Add(gridFiles);
            Controls.Add(bottomPanel);
            Controls.Add(topPanel);
            MinimumSize = new Size(900, 560);
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "UDM_10-MultiFileUpload";
            topPanel.ResumeLayout(false);
            dropZone.ResumeLayout(false);
            dropZone.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)gridFiles).EndInit();
            bottomPanel.ResumeLayout(false);
            bottomPanel.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private Panel topPanel;
        private Panel dropZone;
        private Button btnChooseFiles;
        private Label dropLabel;
        private DataGridView gridFiles;
        private Button btnTestStatus;
        private Button btnUploadAll;
        private Button btnUploadSelected;
        private Label lblConcurrencyInfo;
        private Label lblQueueStatus;
        private Label lblTotalFiles;
        private Panel bottomPanel;
    }
}