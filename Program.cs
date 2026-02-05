using System;
using System.Windows.Forms;

internal static class Program {
    [STAThread]
    static void Main() {
    //
    // 적용 순서 중요 : DPI 모드 설정 -> 테마 적용 -> 렌더링 엔진 설정 -> 메세지 루프 시작
    //

        // DPI 인식 모드 설정: PerMonitorV2(최신 Windows DPI 처리 방식)
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);

        // xp 이후 최신 테마(비주얼 스타일) 사용하도록 설정
        Application.EnableVisualStyles();

        // 텍스트 렌더링 엔진 선택: GDI+(false), GDI(true)
        Application.SetCompatibleTextRenderingDefault(false);

        // 위 모든 설정을 알아서 수행하지만, .NET6 Template일 때에만 동작한다.
        // 즉, 지금과 같은 Console 기반 수동 WinForms 구성에서는 효과가 없다.
        // ApplicationConfiguration.Initialize();

        Application.Run(new MainForm());
    }
}

