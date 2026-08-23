using System.ComponentModel;
using UDM_10.Client.Models;
using UDM_10.Client.Services;

namespace UDM_10.Client
{
    public partial class MainForm : Form
    {
        private readonly UploadManager _uploadManager = new(new FakeUploader());
        private readonly BindingSource _fileBindingSource = new();

        public MainForm()
        {
            InitializeComponent();

            gridFiles.AutoGenerateColumns = false;

            _fileBindingSource.DataSource = _uploadManager.Files;
            gridFiles.DataSource = _fileBindingSource;

            gridFiles.Columns.Clear();
            gridFiles.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(FileUploadItem.FileName),
                HeaderText = "Tên file",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = 220
            });
            gridFiles.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(FileUploadItem.FileSizeText),
                HeaderText = "Kích thước",
                Width = 100
            });
            gridFiles.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(FileUploadItem.StatusLabel),
                HeaderText = "Trạng thái",
                Width = 120
            });
            gridFiles.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(FileUploadItem.ProgressPercentText),
                HeaderText = "Tiến độ (%)",
                Width = 90
            });
            gridFiles.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(FileUploadItem.ProgressText),
                HeaderText = "Đã gửi",
                Width = 140
            });
            gridFiles.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(FileUploadItem.SpeedText),
                HeaderText = "Tốc độ",
                Width = 90
            });
            _uploadManager.Files.ListChanged += (s, e) =>
            {
                if (e.ListChangedType == ListChangedType.ItemAdded)
                {
                    _uploadManager.Files[e.NewIndex].PropertyChanged += (s2, e2) => gridFiles.Invalidate();
                }
            };
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

        private void gridFiles_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (gridFiles.Columns[e.ColumnIndex].DataPropertyName != nameof(FileUploadItem.StatusLabel)) return;

            if (gridFiles.Rows[e.RowIndex].DataBoundItem is not FileUploadItem item) return;

            e.CellStyle!.BackColor = item.Status switch
            {
                UploadStatus.Waiting => Color.LightYellow,
                UploadStatus.Uploading => Color.LightBlue,
                UploadStatus.Completed => Color.LightGreen,
                UploadStatus.Failed => Color.LightCoral,
                UploadStatus.Cancelled => Color.LightGray,
                _ => Color.White
            };
        }

        private async void btnUploadAll_Click(object sender, EventArgs e)
        {
            btnUploadAll.Enabled = false;
            await _uploadManager.UploadInBatchesAsync();
            btnUploadAll.Enabled = true;
        }
    }
}