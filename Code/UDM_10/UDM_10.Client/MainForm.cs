using System.ComponentModel;
using UDM_10.Client.Models;
using UDM_10.Client.Services;

namespace UDM_10.Client
{
    public partial class MainForm : Form
    {
        private readonly UploadManager _uploadManager;
        private readonly BindingSource _fileBindingSource = new();
        private static readonly Color AccentColor = ColorTranslator.FromHtml("#0F766E");
        private static readonly Color AccentDarkColor = ColorTranslator.FromHtml("#0B5A54");
        private static readonly Color AccentSoftColor = ColorTranslator.FromHtml("#E6F4F3");
        private static readonly Color BorderColor = ColorTranslator.FromHtml("#E2E4EA");
        private static readonly Color TextMutedColor = ColorTranslator.FromHtml("#6B7280");
        private static readonly Color TextDarkColor = ColorTranslator.FromHtml("#374151");
        private CheckBox chkSimulateError = new()
        {
            Text = "Giả lập lỗi (debug)",
            AutoSize = true,
            Location = new Point(900, 15),
            Checked = false
        };
        private CheckBox chkSimulateDisconnect = new()
        {
            Text = "Giả lập mất kết nối (debug)",
            AutoSize = true,
            Location = new Point(900, 40),
            Checked = false
        };
        private Label lblLogo = new()
        {
            Text = "UTH",
            Font = new Font("Segoe UI", 20f, FontStyle.Bold),
            ForeColor = ColorTranslator.FromHtml("#0F766E"),
            AutoSize = true,
            Location = new Point(12, 10)
        };
        private Label lblLogoSub = new()
        {
            Text = "Upload Manager",
            Font = new Font("Segoe UI", 8f),
            ForeColor = TextMutedColor,
            AutoSize = true,
            Location = new Point(12, 50)
        };
        private Label lblWaiting = new() { AutoSize = true, Font = new Font("Segoe Fluent Icons", 9.5f, FontStyle.Bold), ForeColor = Color.FromArgb(202, 138, 4) };
        private Label lblUploading = new() { AutoSize = true, Font = new Font("Segoe Fluent Icons", 9.5f, FontStyle.Bold), ForeColor = Color.FromArgb(37, 99, 235) };
        private Label lblCompleted = new() { AutoSize = true, Font = new Font("Segoe Fluent Icons", 9.5f, FontStyle.Bold), ForeColor = Color.FromArgb(22, 163, 74) };
        private Label lblFailed = new() { AutoSize = true, Font = new Font("Segoe Fluent Icons", 9.5f, FontStyle.Bold), ForeColor = Color.FromArgb(185, 28, 28) };
        private Label lblCancelled = new() { AutoSize = true, Font = new Font("Segoe Fluent Icons", 9.5f, FontStyle.Bold), ForeColor = TextMutedColor };
        public MainForm()
        {
            InitializeComponent();
            StylePrimaryButton(btnUploadAll);
            StyleSecondaryButton(btnCancelAll);
            StyleSecondaryButton(btnRetryAllFailed);
            StyleDangerButton(btnDeleteAll);
            StyleSecondaryButton(btnUploadSelected);
            StyleLinkButton(btnChooseFiles);
            StyleSecondaryButton(btnTestStatus);

            topPanel.Controls.Add(chkSimulateError);
            topPanel.Controls.Add(chkSimulateDisconnect);
            topPanel.Controls.Add(lblLogo);
            topPanel.Controls.Add(lblLogoSub);
            bottomPanel.Controls.Add(lblWaiting);
            bottomPanel.Controls.Add(lblUploading);
            bottomPanel.Controls.Add(lblCompleted);
            bottomPanel.Controls.Add(lblFailed);
            bottomPanel.Controls.Add(lblCancelled);
            lblQueueStatus.Visible = false;
            lblLogo.BringToFront();
            lblLogoSub.BringToFront();

            int rowStartX = Math.Max(lblLogo.Right, lblLogoSub.Right) + 20;
            dropZone.Location = new Point(rowStartX, 0);
            btnTestStatus.Location = new Point(dropZone.Right + 16, 24);
            btnUploadAll.Location = new Point(btnTestStatus.Right + 16, 16);
            btnUploadSelected.Location = new Point(btnTestStatus.Right + 16, 60);
            chkSimulateError.Location = new Point(btnUploadAll.Right + 40, 15);
            chkSimulateDisconnect.Location = new Point(btnUploadAll.Right + 40, 40);
            _uploadManager = new UploadManager(new FakeUploader(
                () => chkSimulateError.Checked,
                () => chkSimulateDisconnect.Checked));


            gridFiles.AutoGenerateColumns = false;
            gridFiles.ReadOnly = false;
            gridFiles.EnableHeadersVisualStyles = false;
            gridFiles.GridColor = Color.FromArgb(180, 185, 196);
            gridFiles.CellBorderStyle = DataGridViewCellBorderStyle.Single;
            gridFiles.GridColor = Color.FromArgb(200, 205, 210);
            gridFiles.RowTemplate.Height = 42;
            gridFiles.BackgroundColor = Color.White;
            gridFiles.Font = new Font("Segoe UI", 9.5f);

            gridFiles.ColumnHeadersDefaultCellStyle.BackColor = Color.White;
            gridFiles.ColumnHeadersDefaultCellStyle.ForeColor = TextDarkColor;
            gridFiles.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            gridFiles.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            gridFiles.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.White;   
            gridFiles.ColumnHeadersDefaultCellStyle.SelectionForeColor = TextDarkColor;      
            gridFiles.ColumnHeadersHeight = 40;
            gridFiles.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
            gridFiles.DefaultCellStyle.SelectionBackColor = AccentSoftColor;
            gridFiles.DefaultCellStyle.SelectionForeColor = TextDarkColor;
            gridFiles.DefaultCellStyle.ForeColor = TextDarkColor;

            _fileBindingSource.DataSource = _uploadManager.Files;
            gridFiles.DataSource = _fileBindingSource;

            gridFiles.Columns.Clear();
            gridFiles.Columns.Add(new DataGridViewCheckBoxColumn
            {
                DataPropertyName = nameof(FileUploadItem.IsSelected),
                HeaderText = "Chọn",
                Width = 50
            });
            gridFiles.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(FileUploadItem.FileName),
                HeaderText = "Tên file",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = 220,
                ReadOnly = true
            });
            gridFiles.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(FileUploadItem.FileSizeText),
                HeaderText = "Kích thước",
                Width = 100,
                ReadOnly = true
            });
            gridFiles.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(FileUploadItem.StatusLabel),
                HeaderText = "Trạng thái",
                Width = 120,
                ReadOnly = true
            });
            gridFiles.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(FileUploadItem.ProgressPercentText),
                HeaderText = "Tiến độ (%)",
                Width = 90,
                ReadOnly = true
            });
            gridFiles.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(FileUploadItem.ProgressText),
                HeaderText = "Đã gửi",
                Width = 140,
                ReadOnly = true
            });
            gridFiles.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(FileUploadItem.SpeedText),
                HeaderText = "Tốc độ",
                Width = 90,
                ReadOnly = true
            });
            gridFiles.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(FileUploadItem.ServerFileName),
                HeaderText = "Tên trên Server",
                Width = 150,
                ReadOnly = true
            });
            gridFiles.Columns.Add(new DataGridViewButtonColumn
            {
                Name = "colCancel",
                HeaderText = "Hủy",
                Text = "\uE711",   // icon X
                UseColumnTextForButtonValue = true,
                Width = 60,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Font = new Font("Segoe Fluent Icons", 11f),
                    ForeColor = Color.FromArgb(185, 28, 28),
                    Alignment = DataGridViewContentAlignment.MiddleCenter
                }
            });
            gridFiles.Columns.Add(new DataGridViewButtonColumn
            {
                Name = "colRetry",
                HeaderText = "Thử lại",
                Text = "\uE72C",   // icon refresh/xoay vong
                UseColumnTextForButtonValue = true,
                Width = 60,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Font = new Font("Segoe Fluent Icons", 11f),
                    ForeColor = AccentColor,
                    Alignment = DataGridViewContentAlignment.MiddleCenter
                }
            });
            gridFiles.Columns.Add(new DataGridViewButtonColumn
            {
                Name = "colDelete",
                HeaderText = "Xóa",
                Text = "\uE74D",   // icon thung rac
                UseColumnTextForButtonValue = true,
                Width = 60,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Font = new Font("Segoe Fluent Icons", 11f),
                    ForeColor = Color.FromArgb(185, 28, 28),
                    Alignment = DataGridViewContentAlignment.MiddleCenter
                }
            });
            gridFiles.Columns[0].HeaderCell.Style.BackColor = AccentColor;
            gridFiles.Columns[0].HeaderCell.Style.ForeColor = Color.White;
            gridFiles.Columns[0].HeaderCell.Style.Alignment =DataGridViewContentAlignment.MiddleCenter;
            gridFiles.CellPainting += gridFiles_CellPainting;
            gridFiles.CellContentClick += gridFiles_CellContentClick;
            gridFiles.CellFormatting += gridFiles_CellFormatting;
            gridFiles.CurrentCellDirtyStateChanged += (s, e) =>
            {
                if (gridFiles.IsCurrentCellDirty)
                    gridFiles.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };
            _uploadManager.Files.ListChanged += (s, e) =>
            {
                if (e.ListChangedType == ListChangedType.ItemAdded)
                {
                    _uploadManager.Files[e.NewIndex].PropertyChanged += (s2, e2) =>
                    {
                        gridFiles.Invalidate();
                        UpdateFooter();
                    };
                }
                UpdateFooter();
            };

            lblConcurrencyInfo.Text = $"⚡ Đồng thời tối đa: {UploadManager.MaxConcurrentUploads} file";
            UpdateFooter();
        }

        private void UpdateFooter()
        {
            int total = _uploadManager.Files.Count;
            long totalBytes = _uploadManager.Files.Sum(f => f.FileSizeBytes);
            int waiting = _uploadManager.Files.Count(f => f.Status == UploadStatus.Waiting);
            int uploading = _uploadManager.Files.Count(f => f.Status == UploadStatus.Uploading);
            int completed = _uploadManager.Files.Count(f => f.Status == UploadStatus.Completed);
            int failed = _uploadManager.Files.Count(f => f.Status == UploadStatus.Failed);
            int cancelled = _uploadManager.Files.Count(f => f.Status == UploadStatus.Cancelled);

            lblTotalFiles.Text = $"\uE7C3  {total} file ({FileUploadItem.FormatBytes(totalBytes)})";
            lblTotalFiles.Font = new Font("Segoe Fluent Icons", 9.5f);
            lblTotalFiles.ForeColor = TextDarkColor;

            lblWaiting.Text = $"\uE72C Chờ: {waiting}";
            lblUploading.Text = $"\uE896 Đang tải: {uploading}";
            lblCompleted.Text = $"\uE73E Xong: {completed}";
            lblFailed.Text = $"\uE783 Lỗi: {failed}";
            lblCancelled.Text = $"\uE711 Đã hủy: {cancelled}";

            lblConcurrencyInfo.Text = $"Đồng thời tối đa: {UploadManager.MaxConcurrentUploads} file";

            // CANH LAI TOAN BO THEO MEP PHAI CUA CONTROL TRUOC - noi duoi nhau, khong dung so co dinh
            int y = lblTotalFiles.Top;
            lblWaiting.Location = new Point(lblTotalFiles.Right + 24, y);
            lblUploading.Location = new Point(lblWaiting.Right + 20, y);
            lblCompleted.Location = new Point(lblUploading.Right + 20, y);
            lblFailed.Location = new Point(lblCompleted.Right + 20, y);
            lblCancelled.Location = new Point(lblFailed.Right + 20, y);
            lblConcurrencyInfo.Location = new Point(lblCancelled.Right + 24, y);
            btnCancelAll.Location = new Point(lblConcurrencyInfo.Right + 24, 18);
            btnRetryAllFailed.Location = new Point(btnCancelAll.Right + 10, 18);
            btnDeleteAll.Location = new Point(btnRetryAllFailed.Right + 10, 18);
        }
        private void StylePrimaryButton(Button btn)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.BackColor = AccentColor;
            btn.ForeColor = Color.White;
            btn.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            btn.Cursor = Cursors.Hand;
            btn.MouseEnter += (s, e) => btn.BackColor = AccentDarkColor;
            btn.MouseLeave += (s, e) => btn.BackColor = AccentColor;
        }

        private void StyleSecondaryButton(Button btn)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = BorderColor;
            btn.BackColor = Color.White;
            btn.ForeColor = TextDarkColor;
            btn.Font = new Font("Segoe UI", 9.5f);
            btn.Cursor = Cursors.Hand;
            btn.MouseEnter += (s, e) => btn.BackColor = Color.FromArgb(243, 244, 246);
            btn.MouseLeave += (s, e) => btn.BackColor = Color.White;
        }
        private void StyleDangerButton(Button btn)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = Color.FromArgb(254, 202, 202);
            btn.BackColor = Color.FromArgb(254, 242, 242);
            btn.ForeColor = Color.FromArgb(185, 28, 28);
            btn.Font = new Font("Segoe UI", 9.5f);
            btn.Cursor = Cursors.Hand;
            btn.MouseEnter += (s, e) => btn.BackColor = Color.FromArgb(254, 226, 226);
            btn.MouseLeave += (s, e) => btn.BackColor = Color.FromArgb(254, 242, 242);
        }
        private void StyleLinkButton(Button btn)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.BackColor = Color.White;
            btn.ForeColor = AccentColor;
            btn.Font = new Font("Segoe UI", 9f, FontStyle.Underline);
            btn.Cursor = Cursors.Hand;
        }
        private async void btnUploadSelected_Click(object sender, EventArgs e)
        {
            btnUploadSelected.Enabled = false;
            await _uploadManager.UploadSelectedAsync();
            btnUploadSelected.Enabled = true;
        }
        private void btnCancelAll_Click(object sender, EventArgs e)
        {
            _uploadManager.CancelAll();
        }

        private async void btnRetryAllFailed_Click(object sender, EventArgs e)
        {
            btnRetryAllFailed.Enabled = false;
            await _uploadManager.RetryAllFailedAsync();
            btnRetryAllFailed.Enabled = true;
        }

        private void btnDeleteAll_Click(object sender, EventArgs e)
        {
            var confirm = MessageBox.Show(
                "Xóa toàn bộ file đã Hoàn thành/Lỗi/Đã hủy khỏi danh sách?",
                "Xác nhận xóa tất cả",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm == DialogResult.Yes)
                _uploadManager.RemoveAllEligible();
        }
        private void AddFilesToList(IEnumerable<string> paths)
        {
            var errors = new List<string>();
            foreach (var path in paths)
            {
                if (!_uploadManager.AddFile(path, out var error))
                {
                    errors.Add(error!);
                }
            }

            if (errors.Count > 0)
            {
                MessageBox.Show(string.Join(Environment.NewLine, errors),
                    "Một số file không thể thêm", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void btnChooseFiles_Click(object sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog { Multiselect = true, Title = "Chọn file để upload" };
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                AddFilesToList(dialog.FileNames);
            }
        }

        private void dropZone_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data != null && e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
            else
                e.Effect = DragDropEffects.None;
        }

        private void dropZone_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data?.GetData(DataFormats.FileDrop) is string[] paths)
            {
                BeginInvoke(new Action(() => AddFilesToList(paths)));
            }
        }

        private void btnTestStatus_Click(object sender, EventArgs e)
        {
            if (_uploadManager.Files.Count == 0)
            {
                MessageBox.Show("Chưa có file nào trong danh sách. Kéo-thả hoặc Chọn tệp trước.");
                return;
            }

            var item = _uploadManager.Files[0];

            item.Status = item.Status switch
            {
                UploadStatus.Waiting => UploadStatus.Uploading,
                UploadStatus.Uploading => UploadStatus.Completed,
                UploadStatus.Completed => UploadStatus.Failed,
                UploadStatus.Failed => UploadStatus.Cancelled,
                UploadStatus.Cancelled => UploadStatus.Waiting,
                _ => UploadStatus.Waiting
            };
        }

        private void gridFiles_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (gridFiles.Rows[e.RowIndex].DataBoundItem is not FileUploadItem item) return;

            if (gridFiles.Columns[e.ColumnIndex].DataPropertyName == nameof(FileUploadItem.StatusLabel))
            {
                gridFiles.Rows[e.RowIndex].Cells[e.ColumnIndex].ToolTipText =
                    item.Status == UploadStatus.Failed ? item.ErrorMessage ?? "Lỗi không xác định" : "";
            }
        }

        private void gridFiles_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (gridFiles.Rows[e.RowIndex].DataBoundItem is not FileUploadItem item) return;
            if (e.Graphics is null) return; // bao ve, gan gia tri khong-null ben duoi

            var g = e.Graphics;
            string colName = gridFiles.Columns[e.ColumnIndex].DataPropertyName;

            if (colName == nameof(FileUploadItem.FileName))
            {
                e.PaintBackground(e.CellBounds, true);
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                string ext = System.IO.Path.GetExtension(item.FileName).ToLowerInvariant();
                var (tagColor, tagText) = ext switch
                {
                    ".doc" or ".docx" => (Color.FromArgb(37, 99, 235), "DOC"),
                    ".ppt" or ".pptx" => (Color.FromArgb(220, 38, 38), "PPT"),
                    ".xls" or ".xlsx" => (Color.FromArgb(22, 163, 74), "XLS"),
                    _ => (Color.FromArgb(107, 114, 128), ext.TrimStart('.').ToUpperInvariant())
                };
                if (tagText.Length > 4) tagText = tagText.Substring(0, 4);

                int tagWidth = 40;
                int tagHeight = 20;
                int tagY = e.CellBounds.Y + (e.CellBounds.Height - tagHeight) / 2;
                var tagRect = new Rectangle(e.CellBounds.X + 4, tagY, tagWidth, tagHeight);

                using var tagPath = RoundedRect(tagRect, 4);
                using var tagBrush = new SolidBrush(tagColor);
                g.FillPath(tagBrush, tagPath);
                TextRenderer.DrawText(g, tagText, new Font("Segoe UI", 7.5f, FontStyle.Bold),
                    tagRect, Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

                var textRect = new Rectangle(tagRect.Right + 8, e.CellBounds.Y, e.CellBounds.Width - tagWidth - 16, e.CellBounds.Height);
                TextRenderer.DrawText(g, item.FileName, e.CellStyle!.Font, textRect, TextDarkColor,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);

                e.Handled = true;
                return;
            }

            if (colName == nameof(FileUploadItem.StatusLabel))
            {
                e.PaintBackground(e.CellBounds, true);
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                var (bg, dot) = item.Status switch
                {
                    UploadStatus.Waiting => (Color.FromArgb(254, 249, 195), Color.FromArgb(202, 138, 4)),
                    UploadStatus.Uploading => (Color.FromArgb(219, 234, 254), AccentColor),
                    UploadStatus.Completed => (Color.FromArgb(220, 252, 231), Color.FromArgb(22, 163, 74)),
                    UploadStatus.Failed => (Color.FromArgb(254, 226, 226), Color.FromArgb(185, 28, 28)),
                    UploadStatus.Cancelled => (Color.FromArgb(229, 231, 235), TextMutedColor),
                    _ => (Color.White, Color.Black)
                };

                int badgeHeight = 22;
                int badgeY = e.CellBounds.Y + (e.CellBounds.Height - badgeHeight) / 2;
                int badgeWidth = Math.Min(e.CellBounds.Width - 10, 100);
                var badgeRect = new Rectangle(e.CellBounds.X + 6, badgeY, badgeWidth, badgeHeight);

                using var path = RoundedRect(badgeRect, 10);
                using var badgeBrush = new SolidBrush(bg);
                g.FillPath(badgeBrush, path);

                using var dotBrush = new SolidBrush(dot);
                g.FillEllipse(dotBrush, badgeRect.X + 8, badgeRect.Y + badgeHeight / 2 - 3, 6, 6);

                TextRenderer.DrawText(g, item.StatusLabel, e.CellStyle!.Font,
                    new Rectangle(badgeRect.X + 18, badgeRect.Y, badgeRect.Width - 18, badgeRect.Height),
                    TextDarkColor, TextFormatFlags.VerticalCenter);

                e.Handled = true;
                return;
            }

            if (colName != nameof(FileUploadItem.ProgressPercentText)) return;

            e.PaintBackground(e.CellBounds, true);

            double percent = Math.Clamp(item.ProgressPercent, 0, 100);

            if (item.Status == UploadStatus.Uploading || item.Status == UploadStatus.Completed)
            {
                int barWidth = (int)((e.CellBounds.Width - 4) * (percent / 100.0));
                var barRect = new Rectangle(e.CellBounds.X + 2, e.CellBounds.Y + 3, barWidth, e.CellBounds.Height - 6);

                var barColor = item.Status == UploadStatus.Completed ? Color.MediumSeaGreen : Color.CornflowerBlue;
                using var barBrush = new SolidBrush(barColor);
                g.FillRectangle(barBrush, barRect);

                using var borderPen = new Pen(Color.Gray);
                g.DrawRectangle(borderPen, e.CellBounds.X + 2, e.CellBounds.Y + 3, e.CellBounds.Width - 5, e.CellBounds.Height - 7);
            }

            TextRenderer.DrawText(
                g, item.ProgressPercentText, e.CellStyle!.Font,
                e.CellBounds, Color.Black,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

            e.Handled = true;
        }
        private static System.Drawing.Drawing2D.GraphicsPath RoundedRect(Rectangle bounds, int radius)
        {
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            int d = radius * 2;
            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
        private void dropZone_Paint(object? sender, PaintEventArgs e)
        {
            var rect = new Rectangle(0, 0, dropZone.Width, dropZone.Height);
            using var clipPath = RoundedRect(rect, 12);
            dropZone.Region = new Region(clipPath); 

            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            var borderRect = new Rectangle(1, 1, dropZone.Width - 3, dropZone.Height - 3);
            using var borderPath = RoundedRect(borderRect, 12);
            using var dashPen = new Pen(Color.FromArgb(150, 155, 170), 1.5f)
            { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash };
            e.Graphics.DrawPath(dashPen, borderPath);
        }
        private async void btnUploadAll_Click(object sender, EventArgs e)
        {
            btnUploadAll.Enabled = false;
            await _uploadManager.UploadInBatchesAsync();
            btnUploadAll.Enabled = true;
        }
        private async void gridFiles_CellContentClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (gridFiles.Rows[e.RowIndex].DataBoundItem is not FileUploadItem item) return;

            string columnName = gridFiles.Columns[e.ColumnIndex].Name;

            if (columnName == "colCancel")
            {
                if (item.Status != UploadStatus.Uploading)
                {
                    MessageBox.Show("Chỉ có thể hủy file đang tải lên.", "Không thể hủy",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                _uploadManager.CancelUpload(item);
            }
            else if (columnName == "colRetry")
            {
                if (item.Status != UploadStatus.Failed && item.Status != UploadStatus.Cancelled)
                {
                    MessageBox.Show("Chỉ thử lại được file Failed hoặc Cancelled.", "Không thể thử lại",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                await _uploadManager.RetryUploadAsync(item);
            }
            else if (columnName == "colDelete")
            {
                if (!_uploadManager.RemoveFile(item))
                {
                    MessageBox.Show("Không thể xóa file đang chờ hoặc đang tải lên.", "Không thể xóa",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }
    }
}