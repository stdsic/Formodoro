using System;
using System.Windows.Forms;
using System.Drawing;
using System.Runtime.InteropServices;
// 네임스페이스는 라이브러리가 아니라 코드를 논리적으로 묶는 이름 공간(저장소)이다.
// 따라서 코드를 담고 있는 물리적 단위가 아니므로 중복되건 말건 신경쓸 필요가 없다.
// 참고로 using 문으로 지정하는 네임스페이스는 "이 안에 있는 타입들을 사용하겠다."라는 선언과 같다.
// 또한, 어느 폴더에 있건 using 문만 쓰면 알아서 참조되므로 경로 따위를 신경쓸 필요도 없다.

public class MainForm : Form {
    private IntPtr hWnd;
    private Bitmap hBitmap;
    private bool bTrans;

    // Hotkey
    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private const uint MOD_ALT      = 0x0001;
    private const uint MOD_CONTROL  = 0x0002;
    private const uint MOD_SHIFT    = 0x0004;
    private const uint MOD_WIN      = 0x0008;
    private const int WM_HOTKEY     = 0x0312;

    // Snap
    [StructLayout(LayoutKind.Sequential)]
    public struct WINDOWPOS{
        public IntPtr hWnd;
        public IntPtr hWndInsertAfter;
        public int x;
        public int y;
        public int cx;
        public int cy;
        public uint flags;
    }
    private const int WM_WINDOWPOSCHANGING = 0x0046; 

    // Hit
    private const int HTNOWHERE         = 0;
    private const int HTCLIENT          = 1;
    private const int HTCAPTION         = 2;
    private const int HTLEFT            = 10;
    private const int HTRIGHT           = 11;
    private const int HTTOP             = 12;
    private const int HTTOPLEFT         = 13;
    private const int HTTOPRIGHT        = 14;
    private const int HTBOTTOM          = 15;
    private const int HTBOTTOMLEFT      = 16;
    private const int HTBOTTOMRIGHT     = 17;
    private const int WM_NCHITTEST      = 0x0084;

    // Transparency
    private const int GWL_EXSTYLE       = -20;
    private const int WS_EX_TOOLWINDOW  = 0x0080;
    private const int WS_EX_TRANSPARENT = 0x0020;
    private const int WS_EX_NOACTIVATE  = 0x0800;
    private const int WS_EX_TOPMOST     = 0x0008;

    [DllImport("user32.dll")]
    public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    // Constructor
    public MainForm() {
        // Control Property
        this.Text = "Formodoro";
        this.Width = 800;
        this.Height = 650;
        this.FormBorderStyle = FormBorderStyle.None;
        this.MaximizeBox = this.MinimizeBox = this.ControlBox = false;

        // Form Property;
        this.Font = new Font("궁서", 16);
        this.ShowInTaskbar = false;
        this.StartPosition = FormStartPosition.WindowsDefaultLocation;
        this.TopLevel = true;
        this.TopMost = false;
        this.Opacity = 1.0;
        this.AllowTransparency = true;
        this.KeyPreview = false;

        // Event Handler Subs
        this.Load += MainForm_Load;
        this.Paint += MainForm_Paint;
        this.KeyDown += MainForm_KeyDown;
        this.Resize += MainForm_Resize;

        // Field
        hWnd = 0;
        hBitmap = null;
        bTrans = false;
    }

    protected override void OnHandleCreated(EventArgs e) {
        // WM_CREATE와 대응되는 이벤트이며 핸들이 생성된 직후 호출된다.
        // 정확히는 WM_CREATE 직후에 발생하는 이벤트이다.
        base.OnHandleCreated(e);
    }

    private void MainForm_Load(object sender, EventArgs e) {
        // 윈도우 핸들이 생성된 직후 화면에 나타나기 직전에 호출된다.   
        // WM_CREATE, WM_NCCREATE, WM_SHOWWINDOW 이후 시점이다.

        // 제일 처음 대응되는 이벤트 핸들러가 Form_Load인데
        // 이 시점에서는 이미 DPI 적용과 Layout 계산이 모두 끝난 후이다.
        // 때문에, 폼 클래스의 생성자에서 외형을 미리 만들어두는 것이 좋다.
        // Form_Load에서는 이미 만들어진 윈도우의 외형을 뜯어 고치는 것이기 때문에
        // 불필요한 시간이 소요될 수 있다.

        hWnd = this.Handle;
        RegisterHotKey(hWnd, 0x0000, MOD_ALT | MOD_SHIFT, (uint)Keys.E);
    }

    private void MainForm_Resize(object sender, EventArgs e) {
        hBitmap?.Dispose();
        hBitmap = new Bitmap(ClientSize.Width, ClientSize.Height);
    }

    protected override void OnPaintBackground(PaintEventArgs e) {
        // 다시 그릴 배경색 지정
        // base.OnPaintBackground(e);
    }

    private void MainForm_Paint(object sender, PaintEventArgs e) {
        if(hBitmap != null){
            Graphics G = Graphics.FromImage(hBitmap);
            G.Clear(SystemColors.Window);

            // Draw
            G.DrawString("Hello World", this.Font, Brushes.Black, 100, 100);

            // BitBlt
            e.Graphics.DrawImage(hBitmap, 0, 0);
        }
    }

    private void MainForm_KeyDown(object sender, KeyEventArgs e) {
        switch(e.KeyCode) {
            case Keys.Escape:
                if(DialogResult.Yes == MessageBox.Show("프로그램을 종료하시겠습니까?", "Formodoro", MessageBoxButtons.YesNo, MessageBoxIcon.Question)){
                    this.Close();
                }
                break;
        }
    }

    protected override void OnFormClosed(FormClosedEventArgs e) {
        // 폼이 닫힌 직후에 호출됨
        hBitmap?.Dispose();
        UnregisterHotKey(hWnd, 0);
        base.OnFormClosed(e);
    }

    protected override void WndProc(ref Message m) {
        int id, Snap, Margin, nHit, ExStyle;
        WINDOWPOS pos;
        Screen screen;
        Rectangle srt;

        switch(m.Msg){
            case WM_NCHITTEST:
                base.WndProc(ref m);
                nHit = m.Result.ToInt32();
                if(nHit == HTCLIENT){
                    m.Result = (IntPtr)HTCAPTION;
                    return;
                }
                break;

            case WM_WINDOWPOSCHANGING:
                pos = Marshal.PtrToStructure<WINDOWPOS>(m.LParam);
                screen = Screen.FromControl(this);
                srt = screen.Bounds;
                Snap = 15;
                Margin = 15;

                if(Math.Abs(srt.Left - pos.x) < (Snap + Margin)){ pos.x = srt.Left + Margin; }
                if(Math.Abs(srt.Top - pos.y) < (Snap + Margin)){ pos.y = srt.Top + Margin; }
                if(Math.Abs(srt.Right - (pos.x + pos.cx)) < (Snap + Margin)){ pos.x = srt.Right - (pos.cx + Margin); }
                if(Math.Abs(srt.Bottom - (pos.y + pos.cy)) < (Snap + Margin)){ pos.y = srt.Bottom - (pos.cy + Margin); }
                Marshal.StructureToPtr(pos, m.LParam, true);
                break;

            case WM_HOTKEY:
                id = m.WParam.ToInt32();
                switch(id) {
                    case 0:
                        this.Activate();
                        if(bTrans){
                            this.Opacity = 1.0;
                            this.TopMost = false;
                            ExStyle = GetWindowLong(hWnd, GWL_EXSTYLE);
                            ExStyle &= ~(WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE);
                            SetWindowLong(hWnd, GWL_EXSTYLE, ExStyle);
                        }else{
                            this.Opacity = 0.7;
                            this.TopMost = true;
                            ExStyle = GetWindowLong(hWnd, GWL_EXSTYLE);
                            ExStyle |= (WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE);
                            SetWindowLong(hWnd, GWL_EXSTYLE, ExStyle);
                        }
                        bTrans = !bTrans;
                        break;
                }
                break;
        }

        base.WndProc(ref m);
    }
}
