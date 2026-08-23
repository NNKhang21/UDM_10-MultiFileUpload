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
            dropZone = new Panel();
            dropLabel = new Label();
            btnChooseFiles = new Button();
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
            topPanel.BackColor = Color.FromArgb(142, 229, 238);
            topPanel.Controls.Add(btnUploadAll);
            topPanel.Controls.Add(btnUploadSelected);
            topPanel.Controls.Add(btnTestStatus);
            topPanel.Controls.Add(dropZone);
            topPanel.Dock = DockStyle.Top;
            topPanel.Location = new Point(0, 0);
            topPanel.Name = "topPanel";
            topPanel.Size = new Size(1150, 110);
            topPanel.TabIndex = 0;
            // 
            // btnUploadAll
            // 
            btnUploadAll.Location = new Point(605, 16);
            btnUploadAll.Name = "btnUploadAll";
            btnUploadAll.Padding = new Padding(6, 2, 6, 2);
            btnUploadAll.Size = new Size(143, 42);
            btnUploadAll.TabIndex = 3;
            btnUploadAll.Text = "Upload tất cả";
            btnUploadAll.UseVisualStyleBackColor = true;
            btnUploadAll.Click += btnUploadAll_Click;
            // 
            // btnUploadSelected
            // 
            btnUploadSelected.Location = new Point(605, 60);
            btnUploadSelected.Name = "btnUploadSelected";
            btnUploadSelected.Padding = new Padding(6, 2, 6, 2);
            btnUploadSelected.Size = new Size(143, 42);
            btnUploadSelected.TabIndex = 4;
            btnUploadSelected.Text = "Upload đã chọn";
            btnUploadSelected.UseVisualStyleBackColor = true;
            btnUploadSelected.Click += btnUploadSelected_Click;
            // 
            // btnTestStatus
            // 
            btnTestStatus.Location = new Point(446, 24);
            btnTestStatus.Name = "btnTestStatus";
            btnTestStatus.Size = new Size(143, 32);
            btnTestStatus.TabIndex = 2;
            btnTestStatus.Text = "Test đổi trạng thái";
            btnTestStatus.UseVisualStyleBackColor = true;
            btnTestStatus.Click += btnTestStatus_Click;
            // 
            // dropZone
            // 
            dropZone.AllowDrop = true;
            dropZone.BackColor = Color.White;
            dropZone.Controls.Add(dropLabel);
            dropZone.Controls.Add(btnChooseFiles);
            dropZone.Location = new Point(0, 0);
            dropZone.Name = "dropZone";
            dropZone.Size = new Size(420, 96);
            dropZone.TabIndex = 0;
            dropZone.DragDrop += dropZone_DragDrop;
            dropZone.DragEnter += dropZone_DragEnter;
            dropZone.Paint += dropZone_Paint;
            // 
            // dropLabel
            // 
            dropLabel.Location = new Point(0, 28);
            dropLabel.Name = "dropLabel";
            dropLabel.Size = new Size(420, 24);
            dropLabel.TabIndex = 0;
            dropLabel.Text = "Kéo && thả file vào đây";
            dropLabel.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // btnChooseFiles
            // 
            btnChooseFiles.AutoSize = true;
            btnChooseFiles.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btnChooseFiles.Location = new Point(155, 58);
            btnChooseFiles.Name = "btnChooseFiles";
            btnChooseFiles.Size = new Size(124, 30);
            btnChooseFiles.TabIndex = 2;
            btnChooseFiles.Text = "hoặc Chọn tệp...";
            btnChooseFiles.UseVisualStyleBackColor = true;
            btnChooseFiles.Click += btnChooseFiles_Click;
            // 
            // lblQueueStatus
            // 
            lblQueueStatus.AutoSize = true;
            lblQueueStatus.BackColor = Color.Transparent;
            lblQueueStatus.Location = new Point(152, 24);
            lblQueueStatus.Name = "lblQueueStatus";
            lblQueueStatus.Size = new Size(50, 20);
            lblQueueStatus.TabIndex = 6;
            lblQueueStatus.Text = "label1";
            // 
            // lblTotalFiles
            // 
            lblTotalFiles.AutoSize = true;
            lblTotalFiles.BackColor = Color.Transparent;
            lblTotalFiles.Location = new Point(12, 24);
            lblTotalFiles.Name = "lblTotalFiles";
            lblTotalFiles.Size = new Size(50, 20);
            lblTotalFiles.TabIndex = 5;
            lblTotalFiles.Text = "label1";
            // 
            // lblConcurrencyInfo
            // 
            lblConcurrencyInfo.AutoSize = true;
            lblConcurrencyInfo.BackColor = Color.Transparent;
            lblConcurrencyInfo.Location = new Point(616, 24);
            lblConcurrencyInfo.Name = "lblConcurrencyInfo";
            lblConcurrencyInfo.Size = new Size(50, 20);
            lblConcurrencyInfo.TabIndex = 4;
            lblConcurrencyInfo.Text = "label1";
            // 
            // btnCancelAll
            // 
            btnCancelAll = new Button();
            btnCancelAll.Location = new Point(750, 18);
            btnCancelAll.Name = "btnCancelAll";
            btnCancelAll.AutoSize = true;
            btnCancelAll.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btnCancelAll.TabIndex = 7;
            btnCancelAll.Text = "Hủy tất cả";
            btnCancelAll.UseVisualStyleBackColor = true;
            btnCancelAll.Click += btnCancelAll_Click;
            // 
            // btnRetryAllFailed
            // 
            btnRetryAllFailed = new Button();
            btnRetryAllFailed.Location = new Point(840, 18);
            btnRetryAllFailed.Name = "btnRetryAllFailed";
            btnRetryAllFailed.AutoSize = true;
            btnRetryAllFailed.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btnRetryAllFailed.TabIndex = 8;
            btnRetryAllFailed.Text = "Thử lại lỗi";
            btnRetryAllFailed.UseVisualStyleBackColor = true;
            btnRetryAllFailed.Click += btnRetryAllFailed_Click;
            // 
            // btnDeleteAll
            // 
            btnDeleteAll = new Button();
            btnDeleteAll.Location = new Point(930, 18);
            btnDeleteAll.Name = "btnDeleteAll";
            btnDeleteAll.AutoSize = true;
            btnDeleteAll.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btnDeleteAll.TabIndex = 9;
            btnDeleteAll.Text = "Xóa tất cả";
            btnDeleteAll.UseVisualStyleBackColor = true;
            btnDeleteAll.Click += btnDeleteAll_Click;
            // 
            // gridFiles
            // 
            gridFiles.AllowUserToAddRows = false;
            gridFiles.AllowUserToDeleteRows = false;
            gridFiles.BackgroundColor = Color.AliceBlue;
            gridFiles.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            gridFiles.Dock = DockStyle.Fill;
            gridFiles.Location = new Point(0, 110);
            gridFiles.Name = "gridFiles";
            gridFiles.RowHeadersVisible = false;
            gridFiles.RowHeadersWidth = 51;
            gridFiles.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            gridFiles.Size = new Size(1150, 493);
            gridFiles.TabIndex = 1;
            gridFiles.CellFormatting += gridFiles_CellFormatting;
            // 
            // bottomPanel
            // 
            bottomPanel.BackColor = Color.FromArgb(142, 229, 238);
            bottomPanel.Controls.Add(lblQueueStatus);
            bottomPanel.Controls.Add(lblTotalFiles);
            bottomPanel.Controls.Add(lblConcurrencyInfo);
            bottomPanel.Controls.Add(btnCancelAll);
            bottomPanel.Controls.Add(btnRetryAllFailed);
            bottomPanel.Controls.Add(btnDeleteAll);
            bottomPanel.Dock = DockStyle.Bottom;
            bottomPanel.Location = new Point(0, 603);
            bottomPanel.Name = "bottomPanel";
            bottomPanel.Padding = new Padding(8);
            bottomPanel.Size = new Size(1150, 70);
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
        private DataGridView gridFiles;
        private Button btnTestStatus;
        private Button btnUploadAll;
        private Button btnUploadSelected;
        private Button btnCancelAll;
        private Button btnRetryAllFailed;
        private Button btnDeleteAll;
        private Label lblConcurrencyInfo;
        private Label lblQueueStatus;
        private Label lblTotalFiles;
        private Panel bottomPanel;
        private Panel dropZone;
        private Label dropLabel;
        private Button btnChooseFiles;
    }
}