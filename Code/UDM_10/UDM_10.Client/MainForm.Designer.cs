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
            dropZone = new Panel();
            dropLabel = new Label();
            btnChooseFiles = new Button();
            gridFiles = new DataGridView();
            dropZone.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)gridFiles).BeginInit();
            SuspendLayout();
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
            // gridFiles
            // 
            gridFiles.AllowUserToAddRows = false;
            gridFiles.AllowUserToDeleteRows = false;
            gridFiles.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            gridFiles.Dock = DockStyle.Bottom;
            gridFiles.Location = new Point(0, 80);
            gridFiles.MultiSelect = false;
            gridFiles.Name = "gridFiles";
            gridFiles.ReadOnly = true;
            gridFiles.RowHeadersVisible = false;
            gridFiles.RowHeadersWidth = 51;
            gridFiles.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            gridFiles.Size = new Size(1082, 593);
            gridFiles.TabIndex = 2;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1082, 673);
            Controls.Add(btnChooseFiles);
            Controls.Add(dropZone);
            Controls.Add(gridFiles);
            MinimumSize = new Size(900, 560);
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "UDM_10-MultiFileUpload";
            dropZone.ResumeLayout(false);
            dropZone.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)gridFiles).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private Panel dropZone;
        private Label dropLabel;
        private Button btnChooseFiles;
        private DataGridView gridFiles;
    }
}