using System;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Text;
// 네임스페이스는 라이브러리가 아니라 코드를 논리적으로 묶는 이름 공간(저장소)이다.
// 따라서 코드를 담고 있는 물리적 단위가 아니므로 중복되건 말건 신경쓸 필요가 없다.
// 참고로 using 문으로 지정하는 네임스페이스는 "이 안에 있는 타입들을 사용하겠다."라는 선언과 같다.
// 또한, 어느 폴더에 있건 using 문만 쓰면 알아서 참조되므로 경로 따위를 신경쓸 필요도 없다.

public class MainForm : Form {
    private IntPtr hWnd;
    private Bitmap hBitmap;
    private bool bTrans;

    private int R, L, r, x,y, iWork, iBreak, iRepeat, iMode;
    private float PixelFontSize;
    private Point Origin, WorkOrigin, BreakOrigin, RepeatOrigin, Mouse, A, B, C, D, E, F, G, H, a, b, c, d;
    private Rectangle Work, Break, Repeat;
    private StringBuilder szWork, szBreak, szRepeat, szInput;
    private Size TextSize;

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
    private const int GWL_EXSTYLE           = -20;
    private const int WS_EX_TOOLWINDOW      = 0x0080;
    private const int WS_EX_TRANSPARENT     = 0x0020;
    private const int WS_EX_NOACTIVATE      = 0x0800;
    private const int WS_EX_TOPMOST         = 0x0008;
    private const int WM_NCLBUTTONDBLCLK    = 0x00A3;

    [DllImport("user32.dll")]
    public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    // Constructor
    public MainForm() {
        // Control Property
        this.Text = "Formodoro";
        this.Width = 500;
        this.Height = (int)(this.Width * 1.05f);
        this.FormBorderStyle = FormBorderStyle.None;
        this.MaximizeBox = this.MinimizeBox = this.ControlBox = false;

        // Common Control Property
        this.Font = new Font("궁서", 16);

        // Form Property;
        this.ShowInTaskbar = false;
        this.StartPosition = FormStartPosition.WindowsDefaultLocation;
        this.TopLevel = true;
        this.TopMost = false;
        this.Opacity = 1.0;
        this.AllowTransparency = true;
        this.KeyPreview = false;

        using(GraphicsPath path = new GraphicsPath()) {
            int R = Math.Min(this.Width, this.Height);
            Rectangle crt = new Rectangle(this.Location.X, this.Location.Y, R, R);
            path.AddEllipse(crt);
            this.Region = new Region(path);
        }

        // Event Handler Subs
        this.Load += MainForm_Load;
        this.Paint += MainForm_Paint;
        this.KeyDown += MainForm_KeyDown;
        this.Resize += MainForm_Resize;
        this.KeyPress += MainForm_KeyPress;
        this.MouseClick += MainForm_MouseClick;

        // Field
        hWnd = 0;
        iMode = 0;
        hBitmap = null;
        bTrans = false;
        iWork = 25;
        iBreak = 5;
        iRepeat = 1;
        szWork = new StringBuilder();
        szBreak = new StringBuilder();
        szRepeat = new StringBuilder();
        szInput = new StringBuilder();
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
            using(GraphicsPath path = new GraphicsPath()){
                G.SmoothingMode = SmoothingMode.AntiAlias;

                R = Math.Min(this.Width, this.Height) / 2;
                L = R / 2;
                r = L / 2;

                Origin = new Point(R, R);

                x = Origin.X - R / 2 - L / 2;
                y = Origin.Y;
                Work = new Rectangle(x, y, L, L);
                WorkOrigin = new Point((int)((Work.Left + Work.Right) * 0.5) , (int)((Work.Top + Work.Bottom) * 0.5));
                path.AddEllipse(Work);

                DrawLine(path, (int)((Work.Left + Work.Right) * 0.5), (int)((Work.Top + Work.Bottom) * 0.5), r, 45.0);
                DrawLine(path, (int)((Work.Left + Work.Right) * 0.5), (int)((Work.Top + Work.Bottom) * 0.5), r, 225.0);

                x = Origin.X + R / 2 - L / 2;
                y = Origin.Y;
                Break = new Rectangle(x, y, L, L);
                BreakOrigin = new Point((int)((Break.Left + Break.Right) * 0.5) , (int)((Break.Top + Break.Bottom) * 0.5));
                path.AddEllipse(Break);

                DrawLine(path, (int)((Break.Left + Break.Right) * 0.5), (int)((Break.Top + Break.Bottom) * 0.5), r, 45.0);
                DrawLine(path, (int)((Break.Left + Break.Right) * 0.5), (int)((Break.Top + Break.Bottom) * 0.5), r, 225.0);

                x = Origin.X - L / 2;
                y = Origin.Y - R / 2 - L / 2;
                Repeat = new Rectangle(x, y, L, L);
                RepeatOrigin = new Point((int)((Repeat.Left + Repeat.Right) * 0.5) , (int)((Repeat.Top + Repeat.Bottom) * 0.5));
                path.AddEllipse(Repeat);

                DrawLine(path, (int)((Repeat.Left + Repeat.Right) * 0.5), (int)((Repeat.Top + Repeat.Bottom) * 0.5), r, 45.0);
                DrawLine(path, (int)((Repeat.Left + Repeat.Right) * 0.5), (int)((Repeat.Top + Repeat.Bottom) * 0.5), r, 225.0);

                // 폰트는 pt 단위, path.AddString은 픽셀 단위 
                // pt -> px 변환식 : inch = pt / 72, px = inch * dpi
                // 따라서, px = pt / 72 * dpi
                PixelFontSize = G.DpiY * this.Font.Size / 72f;

                switch(iMode){
                    case 0:
                        szWork.Clear();
                        szWork.Append(iWork);

                        TextSize = TextRenderer.MeasureText(szWork.ToString(), this.Font);
                        x = (int)(WorkOrigin.X - TextSize.Width / 2f);
                        y = (int)(WorkOrigin.Y - TextSize.Height / 2f);
                        path.AddString(szWork.ToString(), this.Font.FontFamily, (int)this.Font.Style, PixelFontSize, new Point(x,y), StringFormat.GenericDefault);

                        szBreak.Clear();
                        szBreak.Append(iBreak);

                        TextSize = TextRenderer.MeasureText(szBreak.ToString(), this.Font);
                        x = (int)(BreakOrigin.X - TextSize.Width / 2f);
                        y = (int)(BreakOrigin.Y - TextSize.Height / 2f);
                        path.AddString(szBreak.ToString(), this.Font.FontFamily, (int)this.Font.Style, PixelFontSize, new Point(x,y), StringFormat.GenericDefault);

                        szRepeat.Clear();
                        szRepeat.Append(iRepeat);

                        TextSize = TextRenderer.MeasureText(szRepeat.ToString(), this.Font);
                        x = (int)(RepeatOrigin.X - TextSize.Width / 2f);
                        y = (int)(RepeatOrigin.Y - TextSize.Height / 2f);
                        path.AddString(szRepeat.ToString(), this.Font.FontFamily, (int)this.Font.Style, PixelFontSize, new Point(x,y), StringFormat.GenericDefault);
                        break;

                    case 1:
                        TextSize = TextRenderer.MeasureText(szInput.ToString(), this.Font);
                        x = (int)(WorkOrigin.X - TextSize.Width / 2f);
                        y = (int)(WorkOrigin.Y - TextSize.Height / 2f);
 
                        path.AddLine(x + TextSize.Width, y, x + TextSize.Width + 1, y + TextSize.Height);
                        path.AddString(szInput.ToString(), this.Font.FontFamily, (int)this.Font.Style, PixelFontSize, new Point(x,y), StringFormat.GenericDefault);

                        szBreak.Clear();
                        szBreak.Append(iBreak);

                        TextSize = TextRenderer.MeasureText(szBreak.ToString(), this.Font);
                        x = (int)(BreakOrigin.X - TextSize.Width / 2f);
                        y = (int)(BreakOrigin.Y - TextSize.Height / 2f);
                        path.AddString(szBreak.ToString(), this.Font.FontFamily, (int)this.Font.Style, PixelFontSize, new Point(x,y), StringFormat.GenericDefault);

                        szRepeat.Clear();
                        szRepeat.Append(iRepeat);

                        TextSize = TextRenderer.MeasureText(szRepeat.ToString(), this.Font);
                        x = (int)(RepeatOrigin.X - TextSize.Width / 2f);
                        y = (int)(RepeatOrigin.Y - TextSize.Height / 2f);
                        path.AddString(szRepeat.ToString(), this.Font.FontFamily, (int)this.Font.Style, PixelFontSize, new Point(x,y), StringFormat.GenericDefault);
                        break;

                    case 2:
                        szWork.Clear();
                        szWork.Append(iWork);

                        TextSize = TextRenderer.MeasureText(szWork.ToString(), this.Font);
                        x = (int)(WorkOrigin.X - TextSize.Width / 2f);
                        y = (int)(WorkOrigin.Y - TextSize.Height / 2f);
                        path.AddString(szWork.ToString(), this.Font.FontFamily, (int)this.Font.Style, PixelFontSize, new Point(x,y), StringFormat.GenericDefault);

                        TextSize = TextRenderer.MeasureText(szInput.ToString(), this.Font);
                        x = (int)(BreakOrigin.X - TextSize.Width / 2f);
                        y = (int)(BreakOrigin.Y - TextSize.Height / 2f);
 
                        path.AddLine(x + TextSize.Width, y, x + TextSize.Width + 1, y + TextSize.Height);
                        path.AddString(szInput.ToString(), this.Font.FontFamily, (int)this.Font.Style, PixelFontSize, new Point(x,y), StringFormat.GenericDefault);

                        szRepeat.Clear();
                        szRepeat.Append(iRepeat);

                        TextSize = TextRenderer.MeasureText(szRepeat.ToString(), this.Font);
                        x = (int)(RepeatOrigin.X - TextSize.Width / 2f);
                        y = (int)(RepeatOrigin.Y - TextSize.Height / 2f);
                        path.AddString(szRepeat.ToString(), this.Font.FontFamily, (int)this.Font.Style, PixelFontSize, new Point(x,y), StringFormat.GenericDefault);
                        break;

                    case 3:
                        szWork.Clear();
                        szWork.Append(iWork);

                        TextSize = TextRenderer.MeasureText(szWork.ToString(), this.Font);
                        x = (int)(WorkOrigin.X - TextSize.Width / 2f);
                        y = (int)(WorkOrigin.Y - TextSize.Height / 2f);
                        path.AddString(szWork.ToString(), this.Font.FontFamily, (int)this.Font.Style, PixelFontSize, new Point(x,y), StringFormat.GenericDefault);

                        szBreak.Clear();
                        szBreak.Append(iBreak);

                        TextSize = TextRenderer.MeasureText(szBreak.ToString(), this.Font);
                        x = (int)(BreakOrigin.X - TextSize.Width / 2f);
                        y = (int)(BreakOrigin.Y - TextSize.Height / 2f);
                        path.AddString(szBreak.ToString(), this.Font.FontFamily, (int)this.Font.Style, PixelFontSize, new Point(x,y), StringFormat.GenericDefault);

                        TextSize = TextRenderer.MeasureText(szInput.ToString(), this.Font);
                        x = (int)(RepeatOrigin.X - TextSize.Width / 2f);
                        y = (int)(RepeatOrigin.Y - TextSize.Height / 2f);
 
                        path.AddLine(x + TextSize.Width, y, x + TextSize.Width + 1, y + TextSize.Height);
                        path.AddString(szInput.ToString(), this.Font.FontFamily, (int)this.Font.Style, PixelFontSize, new Point(x,y), StringFormat.GenericDefault);
                        break;
                }

                G.DrawPath(Pens.Black, path);
            }

            // BitBlt
            e.Graphics.DrawImage(hBitmap, 0, 0);
        }
    }

    private void MainForm_MouseClick(object sender, MouseEventArgs e) {
        double dRadian, cos, sin;
        Mouse = new Point(e.X, e.Y);

        dRadian = (45.0 % 360.0) * Math.PI / 180.0;
        cos = Math.Cos(dRadian);
        sin = Math.Sin(dRadian);
        A = new Point((int)(WorkOrigin.X + r * cos), (int)(WorkOrigin.Y + r * sin));
        E = new Point((int)(BreakOrigin.X + r * cos), (int)(BreakOrigin.Y + r * sin));
        a = new Point((int)(RepeatOrigin.X + r * cos), (int)(RepeatOrigin.Y + r * sin));

        dRadian = (135.0 % 360.0) * Math.PI / 180.0;
        cos = Math.Cos(dRadian);
        sin = Math.Sin(dRadian);
        B = new Point((int)(WorkOrigin.X + r * cos), (int)(WorkOrigin.Y + r * sin));
        F = new Point((int)(BreakOrigin.X + r * cos), (int)(BreakOrigin.Y + r * sin));
        b = new Point((int)(RepeatOrigin.X + r * cos), (int)(RepeatOrigin.Y + r * sin));


        dRadian = (225.0 % 360.0) * Math.PI / 180.0;
        cos = Math.Cos(dRadian);
        sin = Math.Sin(dRadian);
        C = new Point((int)(WorkOrigin.X + r * cos), (int)(WorkOrigin.Y + r * sin));
        G = new Point((int)(BreakOrigin.X + r * cos), (int)(BreakOrigin.Y + r * sin));
        c = new Point((int)(RepeatOrigin.X + r * cos), (int)(RepeatOrigin.Y + r * sin));


        dRadian = (315.0 % 360.0) * Math.PI / 180.0;
        cos = Math.Cos(dRadian);
        sin = Math.Sin(dRadian);
        D = new Point((int)(WorkOrigin.X + r * cos), (int)(WorkOrigin.Y + r * sin));
        H = new Point((int)(BreakOrigin.X + r * cos), (int)(BreakOrigin.Y + r * sin));
        d = new Point((int)(RepeatOrigin.X + r * cos), (int)(RepeatOrigin.Y + r * sin));

        if(IsMouseOnCircle(WorkOrigin, r, Mouse)){
            if(iMode != 1) {
                if(IsMouseOnArc(A, B, Mouse, WorkOrigin, r, L - r)) {
                    iWork = Math.Clamp(iWork - 1, 1, 999);
                }else if(IsMouseOnArc(C, D, Mouse, WorkOrigin, r, L - r)) {
                    iWork = Math.Clamp(iWork + 1, 1, 999);
                }else{
                    szInput.Clear();
                    iMode = 1;
                }
            }
        }

        if(IsMouseOnCircle(BreakOrigin, r, Mouse)){
            if(iMode != 2){
                if(IsMouseOnArc(E, F, Mouse, BreakOrigin, r, L - r)) {
                    iBreak = Math.Clamp(iBreak - 1, 1, 999);
                }else if(IsMouseOnArc(G, H, Mouse, BreakOrigin, r, L - r)) {
                    iBreak = Math.Clamp(iBreak + 1, 1, 999);
                }else{
                    szInput.Clear();
                    iMode = 2;
                }
            }
        }

        if(IsMouseOnCircle(RepeatOrigin, r, Mouse)){
            if(iMode != 3){
                if(IsMouseOnArc(a, b, Mouse, RepeatOrigin, r, L - r)) {
                    iRepeat = Math.Clamp(iRepeat - 1, 1, 999);
                }else if(IsMouseOnArc(c, d, Mouse, RepeatOrigin, r, L - r)) {
                    iRepeat = Math.Clamp(iRepeat + 1, 1, 999);
                }else {
                    szInput.Clear();
                    iMode = 3;
                }
            }
        }

        this.Invalidate();
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

    protected override bool ProcessCmdKey(ref Message msg, Keys KeyData) {
        if(iMode != 0) {
            switch(KeyData) {
                case Keys.Enter:
                    if(int.TryParse(szInput.ToString(), out int Value)){
                        switch(iMode) {
                            case 1:
                                iWork = Math.Clamp(Value, 1, 999);
                                break;

                            case 2:
                                iBreak = Math.Clamp(Value, 1, 999);
                                break;

                            case 3:
                                iRepeat = Math.Clamp(Value, 1, 999);
                                break;
                        }
                    }
                    iMode = 0;
                    Invalidate();
                    return true;

                case Keys.Back:
                    if(szInput.Length > 0) {
                       szInput.Remove(szInput.Length - 1, 1);
                    }
                    Invalidate();
                    return true;
            }
        }

        return base.ProcessCmdKey(ref msg, KeyData);
    }

    private void MainForm_KeyPress(object sender, KeyPressEventArgs e) {
        if(iMode != 0) {
            if(char.IsDigit(e.KeyChar)) {
                if(szInput.Length < 3) {
                    szInput.Append(e.KeyChar);
                    Invalidate();
                }
            }
        }
    }

    protected override void WndProc(ref Message m) {
        int id, Snap, Margin, nHit, ExStyle;
        WINDOWPOS pos;
        Screen screen;
        Rectangle srt;

        switch(m.Msg){
            case WM_NCLBUTTONDBLCLK:
                // HTCAPTION을 반환하는 중이라 작업 영역을 더블클릭해도 NC 영역에 대한 더블클릭으로 처리된다.
                // 최대화를 방지하기 위해 위 메세지에 대한 기본 처리를 막아야 한다.
                return;

            case WM_NCHITTEST:
                base.WndProc(ref m);
                Mouse = new Point((int)m.LParam);
                Mouse = this.PointToClient(Mouse);

                if(!Work.Contains(Mouse) && !Break.Contains(Mouse) && !Repeat.Contains(Mouse)) {
                    nHit = m.Result.ToInt32();
                    if(nHit == HTCLIENT){
                        m.Result = (IntPtr)HTCAPTION;
                        return;
                    }
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
                return;

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


    private void DrawLine(GraphicsPath path, int x, int y, int length, double ang) {
        double dRadian, cos, sin;
        int sx, sy, ex, ey;

        dRadian = (ang % 360.0) * Math.PI / 180.0;
        cos = Math.Cos(dRadian);
        sin = Math.Sin(dRadian);
        ex = (int)(x + length * cos);
        ey = (int)(y + length * sin);

        dRadian = ((ang + 90.0) % 360.0f) * Math.PI / 180.0f;
        cos = Math.Cos(dRadian);
        sin = Math.Sin(dRadian);
        sx = (int)(x + length * cos);
        sy = (int)(y + length * sin);
        path.AddLine(sx, sy, ex, ey);
        path.StartFigure();
    }

    private bool IsMouseOnArc(Point A, Point B, Point Mouse, Point Origin, double radius, double Threshold) {
        Func<double, double, double> hypot = (x,y) => Math.Sqrt(x * x + y * y);
        double dx = B.X - A.X;
        double dy = B.Y - A.Y;

        double k = ((Mouse.X - A.X) * dx + (Mouse.Y - A.Y) * dy) / (dx * dx + dy * dy);

        if(k < 0.0){ k = 0.0; }
        if(k > 1.0){ k = 1.0; }

        // 가장 가까운 점
        double qx = A.X + k * dx;
        double qy = A.Y + k * dy;

        double ToChord = hypot(Mouse.X - qx, Mouse.Y - qy);
        double ToOrigin = hypot(Mouse.X - Origin.X, Mouse.Y - Origin.Y);

        double SideMouse = dx * (Mouse.Y - A.Y) - dy * (Mouse.X - A.X);
        double SideOrigin = dx * (Origin.Y - A.Y) - dy * (Origin.X - A.X);
        bool OppositeSide = SideMouse * SideOrigin < 0.0;

        return OppositeSide && (ToChord <= Threshold) && (ToOrigin <= radius);
    }

    private bool IsMouseOnCircle(Point Origin, double radius, Point Mouse) {
        double dx = Mouse.X - Origin.X;
        double dy = Mouse.Y - Origin.Y;

        return dx * dx + dy * dy <= radius * radius;
    }
}
