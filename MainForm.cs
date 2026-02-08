using System;
using System.Text;
using System.Media;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
// 네임스페이스는 라이브러리가 아니라 코드를 논리적으로 묶는 이름 공간(저장소)이다.
// 따라서 코드를 담고 있는 물리적 단위가 아니므로 중복되건 말건 신경쓸 필요가 없다.
// 참고로 using 문으로 지정하는 네임스페이스는 "이 안에 있는 타입들을 사용하겠다."라는 선언과 같다.
// 또한, 어느 폴더에 있건 using 문만 쓰면 알아서 참조되므로 경로 따위를 신경쓸 필요도 없다.

public class MainForm : Form {
    private IntPtr hWnd;
    private Bitmap hBitmap;
    private System.Windows.Forms.Timer timer;

    private bool bTrans, bStart, bBreak;
    private int R, L, r, x,y, iWork, iBreak, iRepeat, iCount, iMode, iPadding;
    private double dRadian, cos, sin;
    private StringBuilder szWork, szBreak, szRepeat, szInput, szRemain, lpszWork, lpszBreak, lpszRepeat;
    private DateTime EndTime;
    private TimeSpan Remain;

    private Size TextSize;
    private Font Font1, Font2;
    private Rectangle Work, Break, Repeat, Start, Stop, Pause, Next, ltPause, rtPause, NextBar;
    private Point Origin, WorkOrigin, BreakOrigin, RepeatOrigin, Mouse, A, B, C, D, E, F, G, H, a, b, c, d, p1,p2,p3;

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
        this.Width = Screen.PrimaryScreen.Bounds.Width / 5;
        this.Height = (int)(this.Width * 1.05f);
        this.FormBorderStyle = FormBorderStyle.None;
        this.MaximizeBox = this.MinimizeBox = this.ControlBox = false;

        // Common Control Property
        // this.Font = new Font("궁서", 16);
        // WinForms는 DPI 변경시 컨트롤과 폰트 크기를 자동으로 조정한다.
        // 직접 그리는 도형(path 등)에 대해서는 스케일링을 수행하지 않는데
        // 컨트롤의 크기가 조정될 때 도형의 크기도 자연스럽게 따라 변하므로 따로 신경쓸 필요는 없다.
        Font1 = new Font("궁서", 16);
        Font2 = new Font("궁서", 24);

        // Form Property;
        this.ShowInTaskbar = false;
        this.StartPosition = FormStartPosition.WindowsDefaultLocation;
        this.TopLevel = true;
        this.TopMost = false;
        this.Opacity = 1.0;
        this.AllowTransparency = true;
        this.KeyPreview = false;

        using(GraphicsPath path = new GraphicsPath()) {
            R = Math.Min(this.Width, this.Height);
            Rectangle crt = new Rectangle(this.Location.X, this.Location.Y, R, R);
            path.AddEllipse(crt);
            this.Region = new Region(path);
        }

        Init();

        // Event Handler Subs
        this.Load += MainForm_Load;
        this.Paint += MainForm_Paint;
        this.KeyDown += MainForm_KeyDown;
        this.Resize += MainForm_Resize;
        this.KeyPress += MainForm_KeyPress;
        this.MouseClick += MainForm_MouseClick;
        timer = new System.Windows.Forms.Timer();
        timer.Tick += MainForm_Tick;

        // Field
        hWnd = 0;
        iMode = 0;
        hBitmap = null;
        bTrans = false;
        bStart = false;
        bBreak = false;
        iWork = 25;
        iBreak = 5;
        iRepeat = 1;
        iCount = 1;
        iPadding = 5;
        szWork = new StringBuilder();
        szBreak = new StringBuilder();
        szRepeat = new StringBuilder();
        szInput = new StringBuilder();
        szRemain = new StringBuilder();
        lpszWork = new StringBuilder("작업");
        lpszBreak = new StringBuilder("휴식");
        lpszRepeat = new StringBuilder("반복");
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
            G.SmoothingMode = SmoothingMode.AntiAlias;

            using(GraphicsPath path = new GraphicsPath()){
                if(bStart) {
                    TextSize = TextRenderer.MeasureText(lpszRepeat.ToString(), Font1);
                    DrawPathString(G, path, new Point(RepeatOrigin.X, RepeatOrigin.Y - r - TextSize.Height / 2), lpszRepeat.ToString(), Font1); 
                    path.AddEllipse(Repeat);

                    szRepeat.Clear();
                    if(bBreak){
                        szRepeat.Append(string.Format($"{iCount - 1}/{iRepeat}"));
                    }else{
                        szRepeat.Append(string.Format($"{iCount}/{iRepeat}"));
                    }
                    DrawPathString(G, path, RepeatOrigin, szRepeat.ToString(), Font1);

                    // this.Font = new Font("궁서", 24);
                    // 그리는 중에 this.Font를 변경하게 되면 PerformLayout이 호출되면서 컨트롤 전체가 다시 레이아웃된다.
                    // 즉, Invaldiate(true)가 발생하고 폼 전체가 무효화되며 OnPaint가 다시 호출되는데 이게 반복되면서 이전 프레임(시간 문자열)이 사라진다.
                    // 이 문제를 해결하기 위해 this.Font에 폰트를 대입해서 사용하던 구조를 수정했다.
                    // DrawPathString의 시그니처를 변경하여 폰트 객체를 전달받고 함수 내부에서 폰트 크기와 문자열 길이를 고려하여 화면에 출력한다.
                    {   // 시간
                        DrawPathString(G, path, Origin, szRemain.ToString(), Font2);
                    }

                    {   // 중단 버튼
                        path.AddRectangle(Stop);
                    }

                    {   // 일시정지 버튼
                        if(timer.Enabled){
                            ltPause = new Rectangle(Pause.Left, Pause.Top, (Pause.Right - Pause.Left) / 2 - iPadding, Pause.Bottom - Pause.Top);
                            rtPause = new Rectangle((Pause.Left + Pause.Right) / 2 + iPadding, Pause.Top, (Pause.Right - Pause.Left) / 2 - iPadding, Pause.Bottom - Pause.Top);

                            path.AddRectangle(ltPause);
                            path.AddRectangle(rtPause);
                        }else{
                            p1 = new Point(Pause.Left, Pause.Top);
                            p2 = new Point(Pause.Left, Pause.Bottom);
                            p3 = new Point(Pause.Right, (Pause.Top + Pause.Bottom) / 2);

                            path.StartFigure();
                            path.AddLine(p1, p2);
                            path.AddLine(p2, p3);
                            path.AddLine(p3, p1);
                            path.CloseFigure();
                        }
                    }

                    {   // 다음 버튼
                        p1 = new Point(Next.Left, Next.Top);
                        p2 = new Point(Next.Left, Next.Bottom);
                        p3 = new Point((int)(Next.Left + r * 0.7), (Next.Top + Next.Bottom) / 2);

                        path.StartFigure();
                        path.AddLine(p1, p2);
                        path.AddLine(p2, p3);
                        path.AddLine(p3, p1);
                        path.CloseFigure();

                        NextBar = new Rectangle((int)(Next.Left + r * 0.7 + r * 0.05), Next.Top, (int)(r * 0.1), Next.Height);
                        path.AddRectangle(NextBar);
                    }

                    {   // 상태
                        if(bBreak){
                            TextSize = TextRenderer.MeasureText("휴식중", Font1);
                            DrawPathString(G, path, new Point((Pause.Left + Pause.Right) / 2, (Pause.Top + Pause.Bottom) / 2 + TextSize.Height * 2), "휴식중", Font1);
                        }else{
                            TextSize = TextRenderer.MeasureText("작업중", Font1);
                            DrawPathString(G, path, new Point((Pause.Left + Pause.Right) / 2, (Pause.Top + Pause.Bottom) / 2 + TextSize.Height * 2), "작업중", Font1);
                        }
                    }
                }else {
                    TextSize = TextRenderer.MeasureText(lpszWork.ToString(), Font1);
                    DrawPathString(G, path, new Point(WorkOrigin.X, WorkOrigin.Y - r - TextSize.Height / 2), lpszWork.ToString(), Font1); 
                    TextSize = TextRenderer.MeasureText(lpszBreak.ToString(), Font1);
                    DrawPathString(G, path, new Point(BreakOrigin.X, BreakOrigin.Y - r - TextSize.Height / 2), lpszBreak.ToString(), Font1); 
                    TextSize = TextRenderer.MeasureText(lpszRepeat.ToString(), Font1);
                    DrawPathString(G, path, new Point(RepeatOrigin.X, RepeatOrigin.Y - r - TextSize.Height / 2), lpszRepeat.ToString(), Font1); 

                    path.AddEllipse(Work);
                    DrawChord(path, (int)((Work.Left + Work.Right) * 0.5), (int)((Work.Top + Work.Bottom) * 0.5), r, 45.0);
                    DrawChord(path, (int)((Work.Left + Work.Right) * 0.5), (int)((Work.Top + Work.Bottom) * 0.5), r, 225.0);

                    path.AddEllipse(Break);
                    DrawChord(path, (int)((Break.Left + Break.Right) * 0.5), (int)((Break.Top + Break.Bottom) * 0.5), r, 45.0);
                    DrawChord(path, (int)((Break.Left + Break.Right) * 0.5), (int)((Break.Top + Break.Bottom) * 0.5), r, 225.0);

                    path.AddEllipse(Repeat);
                    DrawChord(path, (int)((Repeat.Left + Repeat.Right) * 0.5), (int)((Repeat.Top + Repeat.Bottom) * 0.5), r, 45.0);
                    DrawChord(path, (int)((Repeat.Left + Repeat.Right) * 0.5), (int)((Repeat.Top + Repeat.Bottom) * 0.5), r, 225.0);

                    szWork.Clear();
                    szBreak.Clear();
                    szRepeat.Clear();

                    szWork.Append(iWork);
                    szBreak.Append(iBreak);
                    szRepeat.Append(iRepeat);

                    switch(iMode){
                        case 0:
                            DrawPathString(G, path, WorkOrigin, szWork.ToString(), Font1);
                            DrawPathString(G, path, BreakOrigin, szBreak.ToString(), Font1);
                            DrawPathString(G, path, RepeatOrigin, szRepeat.ToString(), Font1);
                            break;

                        case 1:
                            DrawPathString(G, path, BreakOrigin, szBreak.ToString(), Font1);
                            DrawPathString(G, path, RepeatOrigin, szRepeat.ToString(), Font1);
                            DrawInputPathString(G, path, WorkOrigin, szInput, Font1);
                            break;

                        case 2:
                            DrawPathString(G, path, WorkOrigin, szWork.ToString(), Font1);
                            DrawPathString(G, path, RepeatOrigin, szRepeat.ToString(), Font1);
                            DrawInputPathString(G, path, BreakOrigin, szInput, Font1);
                            break;

                        case 3:
                            DrawPathString(G, path, WorkOrigin, szWork.ToString(), Font1);
                            DrawPathString(G, path, BreakOrigin, szBreak.ToString(), Font1);
                            DrawInputPathString(G, path, RepeatOrigin, szInput, Font1);
                            break;
                    }

                    Start = new Rectangle(R - r, R + L , r * 2, r);
                    path.AddRectangle(Start);
                    DrawPathString(G, path, new Point((Start.Right + Start.Left) / 2, (Start.Top + Start.Bottom) / 2), "시작", Font1);
                }

                G.DrawPath(Pens.Black, path);
            }

            // BitBlt
            e.Graphics.DrawImage(hBitmap, 0, 0);
        }
    }

    private void MainForm_MouseClick(object sender, MouseEventArgs e) {
        Mouse = new Point(e.X, e.Y);

        if(bStart){
            if(IsMouseOnRect(Stop, Mouse)){
                SystemSounds.Asterisk.Play();
                Finish();
            }

            if(IsMouseOnRect(Pause, Mouse)){
                if(!timer.Enabled){
                    EndTime = DateTime.Now.Add(Remain);
                }
                timer.Enabled = !timer.Enabled;
            }

            if(IsMouseOnRect(Next, Mouse)){
                if(bBreak){
                    bBreak = false;
                    SystemSounds.Asterisk.Play();
                    EndTime = DateTime.Now.AddMinutes(iWork);
                }else{
                    iCount++;

                    if(iCount > iRepeat){
                        SystemSounds.Asterisk.Play();
                        Finish();
                    }else{
                        SystemSounds.Asterisk.Play();
                        bBreak = true;
                        EndTime = DateTime.Now.AddMinutes(iBreak);
                    }
                }

                timer.Enabled = true;
            }
        }else {
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

            if(IsMouseOnRect(Start, Mouse)){
                // 타이머 설정 및 화면 그리기 모드 변경
                bStart = true;
                EndTime = DateTime.Now.AddMinutes(iWork);
                timer.Interval = 200;
                timer.Start();
            }
        }

        this.Invalidate();
    }

    private void MainForm_Tick(object sender, EventArgs e) {
        if(bStart){
            Remain = EndTime - DateTime.Now;

            if(Remain <= TimeSpan.Zero){
                if(bBreak){
                    SystemSounds.Asterisk.Play();
                    bBreak = false;
                    EndTime = DateTime.Now.AddMinutes(iWork);
                }else{
                    iCount++;
                    if(iCount > iRepeat){
                        SystemSounds.Asterisk.Play();
                        Finish();
                    }else{
                        SystemSounds.Asterisk.Play();
                        bBreak = true;
                        EndTime = DateTime.Now.AddMinutes(iBreak);
                    }
                }
            }else{
                szRemain.Clear();
                szRemain.Append(Remain.ToString(@"hh\:mm\:ss"));
            }
        }

        this.Invalidate();
    }

    private void MainForm_KeyDown(object sender, KeyEventArgs e) {
        switch(e.KeyCode) {
            case Keys.Escape:
                if(iMode == 0){
                    if(DialogResult.Yes == MessageBox.Show("프로그램을 종료하시겠습니까?", "Formodoro", MessageBoxButtons.YesNo, MessageBoxIcon.Question)){
                        this.Close();
                    }
                }else{
                    iMode = 0;
                    this.Invalidate();
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
                    this.Invalidate();
                    return true;

                case Keys.Back:
                    if(szInput.Length > 0) {
                       szInput.Remove(szInput.Length - 1, 1);
                    }
                    this.Invalidate();
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
                    this.Invalidate();
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

                if(bStart){
                    if(!Stop.Contains(Mouse) && !Pause.Contains(Mouse) && !Next.Contains(Mouse)) {
                        nHit = m.Result.ToInt32();
                        if(nHit == HTCLIENT){
                            m.Result = (IntPtr)HTCAPTION;
                            return;
                        }
                    }
                }else {
                    if(!Work.Contains(Mouse) && !Break.Contains(Mouse) && !Repeat.Contains(Mouse) && !Start.Contains(Mouse)) {
                        nHit = m.Result.ToInt32();
                        if(nHit == HTCLIENT){
                            m.Result = (IntPtr)HTCAPTION;
                            return;
                        }
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


    private void DrawChord(GraphicsPath path, int x, int y, int length, double ang) {
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

    private void DrawPathString(Graphics G, GraphicsPath path, Point Origin, String szText, Font cFont){
        // 폰트는 pt 단위, path.AddString은 픽셀 단위 
        // pt -> px 변환식 : inch = pt / 72, px = inch * dpi
        // 따라서, px = pt / 72 * dpi
        float pxFontSize = G.DpiY * cFont.Size / 72f;
        TextSize = TextRenderer.MeasureText(szText, cFont);

        x = (int)(Origin.X - TextSize.Width / 2f);
        y = (int)(Origin.Y - TextSize.Height / 2f);
        path.AddString(szText, cFont.FontFamily, (int)cFont.Style, pxFontSize, new Point(x,y), StringFormat.GenericDefault);
    }

    private void DrawInputPathString(Graphics G, GraphicsPath path, Point Origin, StringBuilder szText, Font cFont){
        bool bNone = (szText.Length == 0);
        float pxFontSize = G.DpiY * cFont.Size / 72f;
        TextSize = TextRenderer.MeasureText(bNone ? "8" : szText.ToString(), cFont);

        x = (int)(Origin.X - TextSize.Width / 2f);
        y = (int)(Origin.Y - TextSize.Height / 2f);
        path.AddLine(bNone ? x : x + TextSize.Width, y, bNone ? x + 1 : x + TextSize.Width + 1, y + TextSize.Height);
        path.AddString(szText.ToString(), cFont.FontFamily, (int)cFont.Style, pxFontSize, new Point(x,y), StringFormat.GenericDefault);
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

    private bool IsMouseOnRect(Rectangle rect, Point Mouse) {
        if(rect.Contains(Mouse)){ return true; }
        return false;
    }

    private void Finish(){
        timer.Stop();
        bStart = false;
        iMode = 0;
        iCount = 1;
    }

    private void Init() {
        R = Math.Min(this.Width, this.Height) / 2;
        L = R / 2;
        r = L / 2;

        Origin = new Point(R, R);

        x = Origin.X - R / 2 - L / 2;
        y = Origin.Y;
        Work = new Rectangle(x, y, L, L);
        WorkOrigin = new Point((int)((Work.Left + Work.Right) * 0.5) , (int)((Work.Top + Work.Bottom) * 0.5));

        x = Origin.X + R / 2 - L / 2;
        y = Origin.Y;
        Break = new Rectangle(x, y, L, L);
        BreakOrigin = new Point((int)((Break.Left + Break.Right) * 0.5) , (int)((Break.Top + Break.Bottom) * 0.5));

        x = Origin.X - L / 2;
        y = Origin.Y - R / 2 - L / 2;
        Repeat = new Rectangle(x, y, L, L);
        RepeatOrigin = new Point((int)((Repeat.Left + Repeat.Right) * 0.5) , (int)((Repeat.Top + Repeat.Bottom) * 0.5));

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

        Stop = new Rectangle(R - r * 2 - r/2, R + r, r, r);
        Pause = new Rectangle(R - r/2, R + r, r, r);
        Next = new Rectangle(R + r * 2 - r/2, R + r, r, r);
    }
}
