using Microsoft.VisualBasic.Logging;
using System.Diagnostics.Eventing.Reader;

namespace UDM_10.Client
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
            /* Test UploadManager.AddFile() voi cac tinh huong khac nhau
            var manager = new UDM_10.Client.Services.UploadManager();
            bool ok1 = manager.AddFile(@"C:\Windows\System32\notepad.exe", out var err1);
            System.Diagnostics.Debug.WriteLine($"[TEST 1] Add file hop le: ok ={ ok1},loi ={ err1 ?? "(khong co)"}");
            bool ok2 = manager.AddFile(@"C:\Windows\System32\notepad.exe", out var err2);
            System.Diagnostics.Debug.WriteLine($"[TEST 2] Add lai file trung: ok={ok2}, loi={err2 ?? "(khong co)"}");
            bool ok3 = manager.AddFile(@"C:\khong-ton-tai-123.txt", out var err3);
            System.Diagnostics.Debug.WriteLine($"[TEST 3] Add file khong ton tai: ok={ok3}, loi={err3 ?? "(khong co)"}");
            bool ok4 = manager.AddFile(@"C:\Windows\System32", out var err4);
            System.Diagnostics.Debug.WriteLine($"[TEST 4] Add mot thu muc: ok={ok4}, loi={err4 ?? "(khong co)"}");
            System.Diagnostics.Debug.WriteLine($"[TEST 5] Tong so file trong danh sach: {manager.Files.Count}");
            -> Ket qua mong doi: 1 file hop le, 1 file trung, 1 file khong ton tai, 1 thu muc => chi co 1 file hop le duoc them vao danh sach*/
        }
    }
}
