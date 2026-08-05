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
            btnChooseFiles = new Button();
            dropZone = new Panel();
            dropLabel = new Label();
            gridFiles = new DataGridView();
            topPanel.SuspendLayout();
            dropZone.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)gridFiles).BeginInit();
            SuspendLayout();
            // 
            // topPanel
            // 
            topPanel.Controls.Add(btnChooseFiles);
            topPanel.Controls.Add(dropZone);
            topPanel.Dock = DockStyle.Top;
            topPanel.Location = new Point(0, 0);
            topPanel.Name = "topPanel";
            topPanel.Size = new Size(1082, 96);
            topPanel.TabIndex = 0;
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
            // gridFiles
            // 
            gridFiles.AllowUserToAddRows = false;
            gridFiles.AllowUserToDeleteRows = false;
            gridFiles.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            gridFiles.Dock = DockStyle.Fill;
            gridFiles.Location = new Point(0, 96);
            gridFiles.Name = "gridFiles";
            gridFiles.ReadOnly = true;
            gridFiles.RowHeadersVisible = false;
            gridFiles.RowHeadersWidth = 51;
            gridFiles.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            gridFiles.Size = new Size(1082, 577);
            gridFiles.TabIndex = 1;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1082, 673);
            Controls.Add(gridFiles);
            Controls.Add(topPanel);
            MinimumSize = new Size(900, 560);
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "UDM_10-MultiFileUpload";
            topPanel.ResumeLayout(false);
            dropZone.ResumeLayout(false);
            dropZone.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)gridFiles).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private Panel topPanel;
        private Panel dropZone;
        private Button btnChooseFiles;
        private Label dropLabel;
        private DataGridView gridFiles;
    }
}