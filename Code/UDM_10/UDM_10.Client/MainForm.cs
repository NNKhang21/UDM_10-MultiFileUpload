using UDM_10.Client.Models;
using UDM_10.Client.Services;

namespace UDM_10.Client
{
    public partial class MainForm : Form
    {
        private readonly UploadManager _uploadManager = new();
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
                DataPropertyName = nameof(FileUploadItem.Status),
                HeaderText = "Trạng thái",
                Width = 120
            });
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
    }
}