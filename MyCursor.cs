// MyCursor - 내 PC 전용 마우스 커서 커스터마이저 (YOLO Mouse 스타일)
// 빌드: build.bat  (Windows 기본 내장 .NET Framework 컴파일러 사용, 추가 설치 불필요)
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Serialization;
using Microsoft.Win32;

namespace MyCursor
{
    // ───────────────────────── Win32 ─────────────────────────
    static class Native
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct POINT { public int X, Y; public POINT(int x, int y) { X = x; Y = y; } }
        [StructLayout(LayoutKind.Sequential)]
        public struct SIZE { public int CX, CY; public SIZE(int x, int y) { CX = x; CY = y; } }
        [StructLayout(LayoutKind.Sequential)]
        public struct RECT { public int Left, Top, Right, Bottom; }
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct BLENDFUNCTION { public byte BlendOp, BlendFlags, SourceConstantAlpha, AlphaFormat; }
        [StructLayout(LayoutKind.Sequential)]
        public struct ICONINFO { public bool fIcon; public int xHotspot, yHotspot; public IntPtr hbmMask, hbmColor; }

        [DllImport("user32.dll")] public static extern bool SetSystemCursor(IntPtr hcur, uint id);
        [DllImport("user32.dll")] public static extern bool SystemParametersInfo(uint action, uint param, IntPtr pv, uint winIni);
        [DllImport("user32.dll", EntryPoint = "SystemParametersInfo")] public static extern bool SystemParametersInfoGetInt(uint action, uint param, ref int pv, uint winIni);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern IntPtr LoadCursorFromFile(string file);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern IntPtr LoadImage(IntPtr hinst, string name, uint type, int cx, int cy, uint load);
        [DllImport("user32.dll")] public static extern IntPtr CreateCursor(IntPtr hInst, int x, int y, int w, int h, byte[] andMask, byte[] xorMask);
        [DllImport("user32.dll")] public static extern bool DestroyIcon(IntPtr h);
        [DllImport("user32.dll")] public static extern bool DestroyCursor(IntPtr h);
        [DllImport("user32.dll")] public static extern bool GetIconInfo(IntPtr h, out ICONINFO ii);
        [DllImport("user32.dll")] public static extern IntPtr CreateIconIndirect(ref ICONINFO ii);
        [DllImport("gdi32.dll")] public static extern bool DeleteObject(IntPtr h);
        [DllImport("user32.dll")] public static extern bool GetCursorPos(out POINT p);
        [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr h, IntPtr after, int x, int y, int cx, int cy, uint flags);
        [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int cmd);
        [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern int GetClassName(IntPtr h, StringBuilder sb, int max);
        [DllImport("user32.dll")] public static extern bool RegisterHotKey(IntPtr h, int id, uint mod, uint vk);
        [DllImport("user32.dll")] public static extern bool UnregisterHotKey(IntPtr h, int id);
        [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
        [DllImport("user32.dll")] public static extern short GetAsyncKeyState(int vk);
        [DllImport("user32.dll")] public static extern int GetSystemMetrics(int i);
        [DllImport("user32.dll")] public static extern bool ClipCursor(ref RECT r);
        [DllImport("user32.dll", EntryPoint = "ClipCursor")] public static extern bool ClipCursorFree(IntPtr zero);
        [DllImport("user32.dll")] public static extern bool GetClientRect(IntPtr h, out RECT r);
        [DllImport("user32.dll")] public static extern bool ClientToScreen(IntPtr h, ref POINT p);
        [DllImport("user32.dll")] public static extern bool SetLayeredWindowAttributes(IntPtr h, uint key, byte alpha, uint flags);
        [DllImport("user32.dll")] public static extern bool SetWindowDisplayAffinity(IntPtr h, uint affinity);
        [DllImport("dwmapi.dll")] public static extern int DwmSetWindowAttribute(IntPtr h, int attr, ref int value, int size);
        [DllImport("winmm.dll")] public static extern uint timeBeginPeriod(uint p);
        [DllImport("winmm.dll")] public static extern uint timeEndPeriod(uint p);
        [DllImport("user32.dll")] public static extern bool UpdateLayeredWindow(IntPtr hwnd, IntPtr hdcDst, ref POINT pptDst, ref SIZE psize, IntPtr hdcSrc, ref POINT pprSrc, int crKey, ref BLENDFUNCTION pblend, int flags);
        [DllImport("user32.dll")] public static extern IntPtr GetDC(IntPtr h);
        [DllImport("user32.dll")] public static extern int ReleaseDC(IntPtr h, IntPtr dc);
        [DllImport("gdi32.dll")] public static extern IntPtr CreateCompatibleDC(IntPtr dc);
        [DllImport("gdi32.dll")] public static extern bool DeleteDC(IntPtr dc);
        [DllImport("gdi32.dll")] public static extern IntPtr SelectObject(IntPtr dc, IntPtr obj);

        public const uint SPI_GETMOUSESPEED = 0x70, SPI_SETMOUSESPEED = 0x71;
        public static void SetMouseSpeed(int speed) { SystemParametersInfo(SPI_SETMOUSESPEED, 0, new IntPtr(Math.Max(1, Math.Min(20, speed))), 0); }
    }

    // ───────────────────────── 설정 ─────────────────────────
    public class Profile
    {
        public string Name = "새 프로필";
        public string Process = "";          // 게임 exe 이름 (쉼표로 여러 개)
        public int Mode = 0;                 // 0 = 시스템 커서 교체, 1 = 오버레이
        public int Style = 0;                // CursorRenderer.StyleNames 참고
        public int Size = 32;
        public int Thickness = 2;
        public int Gap = 3;
        public int Outline = 1;
        public int Opacity = 100;
        public int Color = unchecked((int)0xFFFF3B30);
        public int OutlineColor = unchecked((int)0xFF000000);
        public bool Shadow = true;
        public string ImagePath = "";
        public int ImageHotspot = 0;         // 0 좌상단, 1 중앙, 2 파일 값(.cur/.ani)
        public bool AllCursors = true;
        public bool HideSystemCursor = true;
        // 꼬리
        public bool Trail = false;
        public int TrailStyle = 0;           // 0 부드러운 꼬리, 1 점 잔상, 2 커서 잔상, 3 담배 연기
        public int TrailLength = 250;
        public int TrailWidth = 6;
        public int TrailColor = unchecked((int)0xFF4FC3F7);
        public bool TrailRainbow = false;
        // 효과
        public bool Halo = false;
        public int HaloColor = unchecked((int)0xFF4F8CFF);
        public int HaloSize = 44;
        public bool HaloPulse = true;
        public bool Glow = false;
        public int GlowColor = unchecked((int)0xFFFFD60A);
        public int GlowSize = 70;
        public bool ClickFx = false;
        public int ClickColor = unchecked((int)0xFF4F8CFF);
        public bool Crosshair = false;
        public int CrossColor = unchecked((int)0xFF00FF88);
        public int CrossThick = 1;
        public int CrossAlpha = 45;

        public Profile Clone() { return (Profile)MemberwiseClone(); }

        public bool Matches(string processName)
        {
            if (string.IsNullOrWhiteSpace(Process) || processName == null) return false;
            foreach (string raw in Process.Split(',', ';'))
            {
                string n = raw.Trim();
                if (n.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) n = n.Substring(0, n.Length - 4);
                if (n.Length > 0 && string.Equals(n, processName, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }
    }

    public class Settings
    {
        public List<Profile> Profiles = new List<Profile>();
        public string DefaultProfile = "";
        public bool Enabled = true;
        public bool AutoSwitch = true;
        public bool TrayTipShown = false;
        public bool SmokeSampleAdded = false;
        public bool FxSampleAdded = false;
        public bool AnimSampleAdded = false;
        public int ZoomFactor = 2;
        public int ZoomSize = 260;
        public int AltSpeed = 4;

        static string Dir { get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MyCursor"); } }
        static string FilePath { get { return Path.Combine(Dir, "settings.xml"); } }

        public static Settings Load()
        {
            Settings s = null;
            try
            {
                if (File.Exists(FilePath))
                    using (var fs = File.OpenRead(FilePath))
                        s = (Settings)new XmlSerializer(typeof(Settings)).Deserialize(fs);
            }
            catch { s = null; }
            if (s == null) s = new Settings();
            bool save = false;
            if (s.Profiles.Count == 0)
            {
                s.Profiles.Add(new Profile { Name = "빨간 화살표", Style = 0, Size = 32, Color = unchecked((int)0xFFFF3B30) });
                s.Profiles.Add(new Profile { Name = "초록 크로스헤어", Style = 4, Size = 28, Thickness = 2, Gap = 4, Color = unchecked((int)0xFF00FF66), Shadow = false });
                s.Profiles.Add(new Profile { Name = "노란 링 (오버레이)", Mode = 1, Style = 5, Size = 40, Thickness = 3, Color = unchecked((int)0xFFFFD60A) });
                s.Profiles.Add(new Profile { Name = "무지개 꼬리", Style = 0, Size = 32, Color = unchecked((int)0xFFFFFFFF), Trail = true, TrailRainbow = true, TrailWidth = 8, TrailLength = 300 });
                s.DefaultProfile = "빨간 화살표";
                save = true;
            }
            if (!s.SmokeSampleAdded)
            {
                s.SmokeSampleAdded = true;
                if (!s.Profiles.Any(p => p.Name == "말보로 담배"))
                    s.Profiles.Add(new Profile
                    {
                        Name = "말보로 담배", Style = CursorRenderer.StyleCigarette, Size = 56, Outline = 1,
                        Color = unchecked((int)0xFFD0021B), Trail = true, TrailStyle = 3, TrailLength = 400, TrailWidth = 8
                    });
                s.DefaultProfile = "말보로 담배";
                save = true;
            }
            if (!s.FxSampleAdded)
            {
                s.FxSampleAdded = true;
                if (!s.Profiles.Any(p => p.Name == "게이밍 다트"))
                    s.Profiles.Add(new Profile
                    {
                        Name = "게이밍 다트", Style = 9, Size = 30, Color = unchecked((int)0xFF00E5FF),
                        Halo = true, HaloColor = unchecked((int)0xFF00E5FF), HaloSize = 46, ClickFx = true, ClickColor = unchecked((int)0xFF00E5FF)
                    });
                save = true;
            }
            if (!s.AnimSampleAdded)
            {
                s.AnimSampleAdded = true;
                if (!s.Profiles.Any(p => p.Name == "네온 화살표 (애니)"))
                    s.Profiles.Add(new Profile
                    {
                        Name = "네온 화살표 (애니)", Style = 20, Size = 32, Color = unchecked((int)0xFF00E5FF),
                        OutlineColor = unchecked((int)0xFF0A1020), Shadow = false
                    });
                save = true;
            }
            if (save) s.Save();
            return s;
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(Dir);
                string tmp = FilePath + ".tmp";
                using (var fs = File.Create(tmp))
                    new XmlSerializer(typeof(Settings)).Serialize(fs, this);
                if (File.Exists(FilePath)) File.Delete(FilePath);
                File.Move(tmp, FilePath);
            }
            catch { }
        }
    }

    // ───────────────────────── 커서 그리기 ─────────────────────────
    static class CursorRenderer
    {
        public static readonly string[] StyleNames = {
            "클래식 화살표", "점", "링", "십자선", "십자선+점", "링+점", "이미지 파일", "담배",
            "모던 화살표", "날렵한 화살표", "손가락", "별", "하트", "다이아몬드", "과녁", "검", "괄호 조준경", "막대 화살표",
            "로딩 스피너", "무지개 화살표", "네온 화살표", "두근 하트", "반짝 별", "회전 조준경", "레이더 핑", "불타는 담배" };
        // 갤러리에 보여줄 순서
        public static readonly int[] GalleryOrder = { 0, 8, 9, 17, 10, 15, 7, 11, 12, 13, 1, 2, 3, 4, 5, 14, 16, 6 };
        public static readonly int[] AnimatedOrder = { 19, 20, 25, 21, 22, 18, 23, 24 };
        public const int StyleImage = 6, StyleCigarette = 7;

        public static bool UsesThickness(int s) { return (s >= 2 && s <= 5) || s == 14 || s == 16 || s == 23; }
        public static bool UsesGap(int s) { return s == 3 || s == 4; }
        public static bool IsAnimated(int s) { return s >= 18 && s <= 25; }

        // 애니메이션 프레임 수 / 프레임당 시간(1/60초 단위)
        public static int Frames(int s)
        {
            switch (s) { case 18: return 12; case 19: return 24; case 24: return 20; case 25: return 12; }
            return IsAnimated(s) ? 16 : 1;
        }
        public static int Jiffies(int s)
        {
            switch (s) { case 19: return 3; case 23: return 5; case 25: return 5; }
            return 4;
        }
        public static float PhaseNow(int s)
        {
            int period = Math.Max(1, Frames(s) * Jiffies(s) * 1000 / 60);
            return (Environment.TickCount & 0x7FFFFFFF) % period / (float)period;
        }

        public static Bitmap Render(Profile p, out Point hot) { return RenderAt(p, 0.2f, out hot); }

        public static Bitmap RenderAt(Profile p, float t, out Point hot)
        {
            Bitmap b = p.Style == StyleImage ? RenderImage(p, out hot) : RenderShape(p, t, out hot);
            if (p.Shadow) b = AddShadow(b, Math.Max(1, p.Size / 16));
            if (p.Opacity < 100) b = ApplyOpacity(b, Math.Max(5, p.Opacity) / 100f);
            hot = new Point(Clamp(hot.X, 0, b.Width - 1), Clamp(hot.Y, 0, b.Height - 1));
            return b;
        }

        // 모든 프레임을 같은 크기 · 같은 클릭 지점으로 맞춰서 반환
        public static Bitmap[] RenderFrames(Profile p, out Point hot)
        {
            int n = Frames(p.Style);
            var raw = new Bitmap[n]; var hots = new Point[n];
            int left = 0, top = 0, right = 0, bottom = 0;
            for (int i = 0; i < n; i++)
            {
                raw[i] = RenderAt(p, i / (float)n, out hots[i]);
                left = Math.Max(left, hots[i].X); top = Math.Max(top, hots[i].Y);
                right = Math.Max(right, raw[i].Width - hots[i].X); bottom = Math.Max(bottom, raw[i].Height - hots[i].Y);
            }
            var outp = new Bitmap[n];
            for (int i = 0; i < n; i++)
            {
                if (raw[i].Width == left + right && raw[i].Height == top + bottom && hots[i].X == left && hots[i].Y == top) { outp[i] = raw[i]; continue; }
                outp[i] = new Bitmap(left + right, top + bottom, PixelFormat.Format32bppArgb);
                using (var g = Graphics.FromImage(outp[i])) g.DrawImage(raw[i], new Rectangle(left - hots[i].X, top - hots[i].Y, raw[i].Width, raw[i].Height));
                raw[i].Dispose();
            }
            hot = new Point(left, top);
            return outp;
        }

        static Color Hsv(float h)
        {
            h = ((h % 360) + 360) % 360;
            float x = 1 - Math.Abs((h / 60f) % 2 - 1);
            float r, g, b;
            if (h < 60) { r = 1; g = x; b = 0; } else if (h < 120) { r = x; g = 1; b = 0; }
            else if (h < 180) { r = 0; g = 1; b = x; } else if (h < 240) { r = 0; g = x; b = 1; }
            else if (h < 300) { r = x; g = 0; b = 1; } else { r = 1; g = 0; b = x; }
            return Color.FromArgb((int)(r * 255), (int)(g * 255), (int)(b * 255));
        }

        static Bitmap CenterOnCanvas(Bitmap src, Point srcHot, int S, out Point hot)
        {
            var bmp = new Bitmap(S, S, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmp)) g.DrawImage(src, new Rectangle(S / 2 - srcHot.X, S / 2 - srcHot.Y, src.Width, src.Height));
            src.Dispose();
            hot = new Point(S / 2, S / 2);
            return bmp;
        }

        static GraphicsPath HeartPath()
        {
            var gp = new GraphicsPath();
            gp.AddBezier(0.5f, 0.95f, 0.1f, 0.65f, -0.05f, 0.3f, 0.25f, 0.12f);
            gp.AddBezier(0.25f, 0.12f, 0.4f, 0.03f, 0.5f, 0.15f, 0.5f, 0.25f);
            gp.AddBezier(0.5f, 0.25f, 0.5f, 0.15f, 0.6f, 0.03f, 0.75f, 0.12f);
            gp.AddBezier(0.75f, 0.12f, 1.05f, 0.3f, 0.9f, 0.65f, 0.5f, 0.95f);
            gp.CloseFigure();
            return gp;
        }

        static GraphicsPath StarPath(float rotateDeg)
        {
            var pts = new float[20];
            for (int i = 0; i < 10; i++)
            {
                double a = (-90 + i * 36 + rotateDeg) * Math.PI / 180;
                float r = i % 2 == 0 ? 0.5f : 0.21f;
                pts[i * 2] = 0.5f + r * (float)Math.Cos(a);
                pts[i * 2 + 1] = 0.5f + r * (float)Math.Sin(a);
            }
            return Poly(pts);
        }

        static void Sparkle(Graphics g, float x, float y, float r, int alpha)
        {
            if (alpha <= 0 || r < 1) return;
            using (var br = new SolidBrush(Color.FromArgb(Math.Min(255, alpha), 255, 255, 240)))
                g.FillPolygon(br, new[] { new PointF(x, y - r), new PointF(x + r * 0.22f, y - r * 0.22f), new PointF(x + r, y), new PointF(x + r * 0.22f, y + r * 0.22f),
                    new PointF(x, y + r), new PointF(x - r * 0.22f, y + r * 0.22f), new PointF(x - r, y), new PointF(x - r * 0.22f, y - r * 0.22f) });
        }

        static Bitmap RenderAnimated(Profile p, float t, Color fill, Color oc, int ol, out Point hot)
        {
            int size = Math.Max(8, p.Size);
            float ph = t * 2f * (float)Math.PI;
            switch (p.Style)
            {
                case 19: // 무지개 화살표
                    return ClassicArrow(size, ol, Hsv(t * 360), oc, out hot);
                case 20: // 네온 화살표
                    return NeonArrow(size, ol, fill, oc, 0.5f + 0.5f * (float)Math.Sin(ph), out hot);
                case 25: // 불타는 담배
                    {
                        float f = 0.55f + 0.25f * (float)Math.Sin(ph) + 0.2f * (float)Math.Sin(ph * 3 + 1);
                        return RenderCigarette(p, fill, oc, ol, out hot, Math.Max(0.05f, Math.Min(1f, f)));
                    }
                case 21: // 두근 하트 (두 번 뛰는 박동)
                    {
                        float b1 = (float)Math.Exp(-Math.Pow((t - 0.12) / 0.06, 2)), b2 = (float)Math.Exp(-Math.Pow((t - 0.32) / 0.07, 2)) * 0.7f;
                        float sc = 1 + 0.2f * Math.Max(b1, b2);
                        Point h0;
                        var bmp = PathShape((int)(size * sc), ol, fill, oc, true, HeartPath(), new PointF(0.5f, 0.5f), null, out h0);
                        return CenterOnCanvas(bmp, h0, (int)(size * 1.4f) + ol * 2 + 8, out hot);
                    }
                case 22: // 반짝 별 (5각 대칭이라 72°씩 돌면 이어짐)
                    {
                        Point h0;
                        float sc = 1 + 0.06f * (float)Math.Sin(ph * 2);
                        var star = PathShape((int)(size * sc), ol, fill, oc, true, StarPath(t * 72), new PointF(0.5f, 0.52f), null, out h0);
                        int S = (int)(size * 1.5f) + ol * 2 + 8;
                        var bmp = CenterOnCanvas(star, h0, S, out hot);
                        using (var g = Graphics.FromImage(bmp))
                        {
                            g.SmoothingMode = SmoothingMode.AntiAlias;
                            float c = S / 2f, R = size * 0.62f;
                            for (int k = 0; k < 3; k++)
                            {
                                float a = (float)Math.Sin(ph + k * 2.1f);
                                double ang = (k * 120 + 30) * Math.PI / 180;
                                Sparkle(g, c + R * (float)Math.Cos(ang), c + R * (float)Math.Sin(ang), size * 0.14f * Math.Max(0, a), (int)(255 * Math.Max(0, a)));
                            }
                        }
                        return bmp;
                    }
                case 23: // 회전 조준경 (4각 대칭이라 90°씩)
                    {
                        var q = p.Clone(); q.Style = 16;
                        Point h0;
                        var baseBmp = Centered(q, size, ol, Math.Max(1, p.Thickness), fill, oc, out h0);
                        int S = (int)Math.Ceiling(Math.Max(baseBmp.Width, baseBmp.Height) * 1.42f) + 2;
                        var bmp = new Bitmap(S, S, PixelFormat.Format32bppArgb);
                        using (var g = Graphics.FromImage(bmp))
                        {
                            g.SmoothingMode = SmoothingMode.AntiAlias;
                            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                            g.TranslateTransform(S / 2f, S / 2f);
                            g.RotateTransform(t * 90);
                            g.DrawImage(baseBmp, -h0.X, -h0.Y, baseBmp.Width, baseBmp.Height);
                        }
                        baseBmp.Dispose();
                        hot = new Point(S / 2, S / 2);
                        return bmp;
                    }
                case 18: // 로딩 스피너
                    {
                        int S = size + ol * 2 + 6; if (S % 2 == 1) S++;
                        float c = S / 2f, th = Math.Max(2f, size / 7f), r = size / 2f - th / 2f;
                        var bmp = new Bitmap(S, S, PixelFormat.Format32bppArgb);
                        using (var g = Graphics.FromImage(bmp))
                        {
                            g.SmoothingMode = SmoothingMode.AntiAlias;
                            var rc = new RectangleF(c - r, c - r, r * 2, r * 2);
                            using (var pen = new Pen(Color.FromArgb(70, fill), th)) g.DrawEllipse(pen, rc);
                            float start = t * 360, sweep = 120;
                            if (ol > 0) using (var pen = new Pen(Color.FromArgb(170, oc), th + ol * 2) { StartCap = LineCap.Round, EndCap = LineCap.Round }) g.DrawArc(pen, rc, start, sweep);
                            for (int i = 0; i < 12; i++)
                                using (var pen = new Pen(Color.FromArgb(40 + 215 * i / 11, fill), th) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                                    g.DrawArc(pen, rc, start + i * sweep / 12f, sweep / 12f + 1);
                        }
                        hot = new Point(S / 2, S / 2);
                        return bmp;
                    }
                default: // 24 레이더 핑
                    {
                        int S = size + ol * 2 + 6; if (S % 2 == 1) S++;
                        float c = S / 2f, R = size / 2f, pw = Math.Max(1.5f, size / 16f);
                        var bmp = new Bitmap(S, S, PixelFormat.Format32bppArgb);
                        using (var g = Graphics.FromImage(bmp))
                        {
                            g.SmoothingMode = SmoothingMode.AntiAlias;
                            for (int k = 0; k < 2; k++)
                            {
                                float tt = (t + k * 0.5f) % 1f, r = Math.Max(2f, R * tt);
                                int a = (int)(255 * (1 - tt));
                                using (var pen = new Pen(Color.FromArgb(a / 2, oc), pw + 2)) g.DrawEllipse(pen, c - r, c - r, r * 2, r * 2);
                                using (var pen = new Pen(Color.FromArgb(a, fill), pw)) g.DrawEllipse(pen, c - r, c - r, r * 2, r * 2);
                            }
                            float dr = Math.Max(2f, size / 10f);
                            if (ol > 0) using (var br = new SolidBrush(oc)) g.FillEllipse(br, c - dr - ol, c - dr - ol, (dr + ol) * 2, (dr + ol) * 2);
                            using (var br = new SolidBrush(fill)) g.FillEllipse(br, c - dr, c - dr, dr * 2, dr * 2);
                        }
                        hot = new Point(S / 2, S / 2);
                        return bmp;
                    }
            }
        }

        static Bitmap NeonArrow(int size, int ol, Color color, Color dark, float pulse, out Point hot)
        {
            int G = (int)(size * 0.25f) + ol + 2;
            int W = (int)Math.Ceiling(size * 0.64f) + G * 2, H = size + G * 2;
            var bmp = new Bitmap(W, H, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmp))
            using (var path = new GraphicsPath())
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                float[,] u = { { 0, 0 }, { 0, 0.86f }, { 0.22f, 0.67f }, { 0.37f, 1.0f }, { 0.51f, 0.94f }, { 0.37f, 0.62f }, { 0.64f, 0.62f } };
                var pts = new PointF[u.GetLength(0)];
                for (int i = 0; i < pts.Length; i++) pts[i] = new PointF(G + u[i, 0] * size, G + u[i, 1] * size);
                path.AddPolygon(pts);
                for (int k = 4; k >= 1; k--)
                    using (var pen = new Pen(Color.FromArgb((int)(30 + 30 * pulse), color), (2 + k * size / 14f) * (0.6f + 0.4f * pulse)) { LineJoin = LineJoin.Round })
                        g.DrawPath(pen, path);
                using (var br = new SolidBrush(Color.FromArgb(225, dark))) g.FillPath(br, path);
                using (var pen = new Pen(Lighten(color, 0.3f + 0.4f * pulse), Math.Max(1.5f, size / 18f)) { LineJoin = LineJoin.Round }) g.DrawPath(pen, path);
            }
            hot = new Point(G, G);
            return bmp;
        }

        static int Clamp(int v, int lo, int hi) { return v < lo ? lo : (v > hi ? hi : v); }
        static Color Opaque(int argb) { return Color.FromArgb(255, Color.FromArgb(argb)); }
        public static Color Lighten(Color c, float t)
        {
            return Color.FromArgb(c.A, (int)(c.R + (255 - c.R) * t), (int)(c.G + (255 - c.G) * t), (int)(c.B + (255 - c.B) * t));
        }

        static GraphicsPath Poly(params float[] xy)
        {
            var pts = new PointF[xy.Length / 2];
            for (int i = 0; i < pts.Length; i++) pts[i] = new PointF(xy[i * 2], xy[i * 2 + 1]);
            var gp = new GraphicsPath();
            gp.AddPolygon(pts);
            return gp;
        }

        static void AddRound(GraphicsPath gp, float x, float y, float w, float h, float r)
        {
            float d = Math.Min(r * 2, Math.Min(w, h));
            gp.StartFigure();
            gp.AddArc(x, y, d, d, 180, 90);
            gp.AddArc(x + w - d, y, d, d, 270, 90);
            gp.AddArc(x + w - d, y + h - d, d, d, 0, 90);
            gp.AddArc(x, y + h - d, d, d, 90, 90);
            gp.CloseFigure();
        }

        static Bitmap RenderShape(Profile p, float t, out Point hot)
        {
            int size = Math.Max(4, p.Size);
            int ol = Math.Max(0, p.Outline);
            Color fill = Opaque(p.Color), oc = Opaque(p.OutlineColor);
            if (IsAnimated(p.Style)) return RenderAnimated(p, t, fill, oc, ol, out hot);

            switch (p.Style)
            {
                case 0: return ClassicArrow(size, ol, fill, oc, out hot);
                case StyleCigarette: return RenderCigarette(p, fill, oc, ol, out hot);
                case 15: return RenderSword(p, fill, oc, ol, out hot);
                case 8: // 모던 화살표
                    return PathShape(size, ol, fill, oc, true,
                        Poly(0, 0, 0, 0.80f, 0.19f, 0.63f, 0.31f, 0.93f, 0.43f, 0.88f, 0.31f, 0.59f, 0.56f, 0.59f), PointF.Empty, null, out hot);
                case 9: // 날렵한 화살표 (다트)
                    return PathShape(size, ol, fill, oc, true, Poly(0, 0, 0.78f, 0.36f, 0.40f, 0.40f, 0.36f, 0.78f), PointF.Empty, null, out hot);
                case 17: // 막대 화살표
                    {
                        var gp = Poly(0, 0, 0.4f, -0.24f, 0.4f, -0.08f, 1.0f, -0.08f, 1.0f, 0.08f, 0.4f, 0.08f, 0.4f, 0.24f);
                        using (var m = new Matrix()) { m.Rotate(45); gp.Transform(m); }
                        return PathShape(size, ol, fill, oc, true, gp, PointF.Empty, null, out hot);
                    }
                case 10: // 손가락
                    {
                        var gp = new GraphicsPath(FillMode.Winding);
                        AddRound(gp, 0.22f, 0f, 0.18f, 0.58f, 0.09f);
                        AddRound(gp, 0.40f, 0.34f, 0.12f, 0.28f, 0.06f);
                        AddRound(gp, 0.52f, 0.38f, 0.11f, 0.26f, 0.055f);
                        AddRound(gp, 0.63f, 0.42f, 0.10f, 0.24f, 0.05f);
                        AddRound(gp, 0.12f, 0.44f, 0.61f, 0.50f, 0.16f);
                        gp.AddEllipse(0.0f, 0.46f, 0.22f, 0.30f);
                        Color line = Color.FromArgb(110, oc);
                        return PathShape(size, ol, fill, oc, true, gp, new PointF(0.31f, 0), (g, s) =>
                        {
                            using (var pen = new Pen(line, Math.Max(1f, s / 40f)))
                            {
                                g.DrawLine(pen, 0.52f * s, 0.40f * s, 0.52f * s, 0.58f * s);
                                g.DrawLine(pen, 0.63f * s, 0.44f * s, 0.63f * s, 0.60f * s);
                                g.DrawLine(pen, 0.40f * s, 0.36f * s, 0.40f * s, 0.56f * s);
                            }
                        }, out hot);
                    }
                case 11: // 별
                    {
                        var pts = new float[20];
                        for (int i = 0; i < 10; i++)
                        {
                            double a = (-90 + i * 36) * Math.PI / 180;
                            float r = i % 2 == 0 ? 0.5f : 0.21f;
                            pts[i * 2] = 0.5f + r * (float)Math.Cos(a);
                            pts[i * 2 + 1] = 0.5f + r * (float)Math.Sin(a);
                        }
                        return PathShape(size, ol, fill, oc, true, Poly(pts), new PointF(0.5f, 0.52f), null, out hot);
                    }
                case 12: // 하트
                    {
                        var gp = new GraphicsPath();
                        gp.AddBezier(0.5f, 0.95f, 0.1f, 0.65f, -0.05f, 0.3f, 0.25f, 0.12f);
                        gp.AddBezier(0.25f, 0.12f, 0.4f, 0.03f, 0.5f, 0.15f, 0.5f, 0.25f);
                        gp.AddBezier(0.5f, 0.25f, 0.5f, 0.15f, 0.6f, 0.03f, 0.75f, 0.12f);
                        gp.AddBezier(0.75f, 0.12f, 1.05f, 0.3f, 0.9f, 0.65f, 0.5f, 0.95f);
                        gp.CloseFigure();
                        return PathShape(size, ol, fill, oc, true, gp, new PointF(0.5f, 0.5f), null, out hot);
                    }
                case 13: // 다이아몬드(보석)
                    {
                        var gp = Poly(0.25f, 0.08f, 0.75f, 0.08f, 0.95f, 0.35f, 0.5f, 0.95f, 0.05f, 0.35f);
                        return PathShape(size, ol, fill, oc, true, gp, new PointF(0.5f, 0.5f), (g, s) =>
                        {
                            using (var pen = new Pen(Color.FromArgb(120, 255, 255, 255), Math.Max(1f, s / 45f)))
                            {
                                g.DrawLine(pen, 0.05f * s, 0.35f * s, 0.95f * s, 0.35f * s);
                                g.DrawLines(pen, new[] { new PointF(0.25f * s, 0.08f * s), new PointF(0.38f * s, 0.35f * s), new PointF(0.5f * s, 0.95f * s) });
                                g.DrawLines(pen, new[] { new PointF(0.75f * s, 0.08f * s), new PointF(0.62f * s, 0.35f * s), new PointF(0.5f * s, 0.95f * s) });
                                g.DrawLines(pen, new[] { new PointF(0.38f * s, 0.35f * s), new PointF(0.5f * s, 0.08f * s), new PointF(0.62f * s, 0.35f * s) });
                            }
                        }, out hot);
                    }
            }
            return Centered(p, size, ol, Math.Max(1, p.Thickness), fill, oc, out hot);
        }

        static Bitmap PathShape(int size, int ol, Color fill, Color oc, bool gradient, GraphicsPath gp, PointF hotU, Action<Graphics, float> decorate, out Point hot)
        {
            using (gp)
            {
                float s = size;
                using (var m = new Matrix()) { m.Scale(s, s); gp.Transform(m); }
                RectangleF b = gp.GetBounds();
                float pad = ol + 2;
                int W = (int)Math.Ceiling(b.Width + pad * 2) + 1, H = (int)Math.Ceiling(b.Height + pad * 2) + 1;
                var bmp = new Bitmap(W, H, PixelFormat.Format32bppArgb);
                float dx = pad - b.X, dy = pad - b.Y;
                using (var g = Graphics.FromImage(bmp))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.TranslateTransform(dx, dy);
                    if (ol > 0) using (var pen = new Pen(oc, ol * 2) { LineJoin = LineJoin.Round }) g.DrawPath(pen, gp);
                    if (gradient)
                        using (var br = new LinearGradientBrush(new RectangleF(b.X, b.Y - 1, b.Width + 1, b.Height + 2), Lighten(fill, 0.45f), fill, 90f))
                            g.FillPath(br, gp);
                    else using (var br = new SolidBrush(fill)) g.FillPath(br, gp);
                    if (decorate != null) decorate(g, s);
                }
                hot = new Point((int)Math.Round(dx + hotU.X * s), (int)Math.Round(dy + hotU.Y * s));
                return bmp;
            }
        }

        static Bitmap ClassicArrow(int size, int ol, Color fill, Color oc, out Point hot)
        {
            int pad = ol + 2;
            int W = (int)Math.Ceiling(size * 0.64f) + pad * 2, H = size + pad * 2;
            var bmp = new Bitmap(W, H, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmp))
            using (var path = new GraphicsPath())
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                float[,] u = { { 0, 0 }, { 0, 0.86f }, { 0.22f, 0.67f }, { 0.37f, 1.0f }, { 0.51f, 0.94f }, { 0.37f, 0.62f }, { 0.64f, 0.62f } };
                var pts = new PointF[u.GetLength(0)];
                for (int i = 0; i < pts.Length; i++) pts[i] = new PointF(pad + u[i, 0] * size, pad + u[i, 1] * size);
                path.AddPolygon(pts);
                if (ol > 0) using (var pen = new Pen(oc, ol * 2) { LineJoin = LineJoin.Round }) g.DrawPath(pen, path);
                using (var br = new SolidBrush(fill)) g.FillPath(br, path);
            }
            hot = new Point(pad, pad);
            return bmp;
        }

        static Bitmap Centered(Profile p, int size, int ol, int th, Color fill, Color oc, out Point hot)
        {
            int st = p.Style;
            int S = size + ol * 2 + 4;
            if (S % 2 == 1) S++;
            int c = S / 2;
            float R = size / 2f;
            var bm = new Bitmap(S, S, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bm))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                bool cross = st == 3 || st == 4;
                bool ring = st == 2 || st == 5;
                bool dot = st == 1 || st == 4 || st == 5 || st == 14 || st == 16;

                if (ring) DrawRing(g, c, R - th / 2f, th, fill, oc, ol);
                if (st == 14)
                {
                    DrawRing(g, c, R - th / 2f, th, fill, oc, ol);
                    DrawRing(g, c, R * 0.58f - th / 2f, th, fill, oc, ol);
                }
                if (cross)
                {
                    g.SmoothingMode = SmoothingMode.None;
                    int gap = Math.Max(0, p.Gap), len = (int)R;
                    int a = c - th / 2;
                    var arms = new[] {
                        new Rectangle(c + gap, a, Math.Max(1, len - gap), th),
                        new Rectangle(c - len, a, Math.Max(1, len - gap), th),
                        new Rectangle(a, c + gap, th, Math.Max(1, len - gap)),
                        new Rectangle(a, c - len, th, Math.Max(1, len - gap)) };
                    FillRects(g, arms, fill, oc, ol);
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                }
                if (st == 16) // 괄호 조준경
                {
                    g.SmoothingMode = SmoothingMode.None;
                    int r = (int)R, arm = Math.Max(3, (int)(R * 0.42f));
                    int l = c - r, t = c - r, rr = c + r, b = c + r;
                    var rects = new[] {
                        new Rectangle(l, t, arm, th), new Rectangle(l, t, th, arm),
                        new Rectangle(rr - arm, t, arm, th), new Rectangle(rr - th, t, th, arm),
                        new Rectangle(l, b - th, arm, th), new Rectangle(l, b - arm, th, arm),
                        new Rectangle(rr - arm, b - th, arm, th), new Rectangle(rr - th, b - arm, th, arm) };
                    FillRects(g, rects, fill, oc, ol);
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                }
                if (dot)
                {
                    float dr = st == 1 ? R : (st == 14 ? Math.Max(2f, th * 1.1f) : Math.Max(1.5f, th * 0.9f));
                    if (ol > 0) using (var br = new SolidBrush(oc)) g.FillEllipse(br, c - dr - ol, c - dr - ol, (dr + ol) * 2, (dr + ol) * 2);
                    using (var br = new SolidBrush(fill)) g.FillEllipse(br, c - dr, c - dr, dr * 2, dr * 2);
                }
            }
            hot = new Point(c, c);
            return bm;
        }

        static void DrawRing(Graphics g, float c, float r, int th, Color fill, Color oc, int ol)
        {
            if (r < 1) r = 1;
            if (ol > 0) using (var pen = new Pen(oc, th + ol * 2)) g.DrawEllipse(pen, c - r, c - r, r * 2, r * 2);
            using (var pen = new Pen(fill, th)) g.DrawEllipse(pen, c - r, c - r, r * 2, r * 2);
        }

        static void FillRects(Graphics g, Rectangle[] rects, Color fill, Color oc, int ol)
        {
            if (ol > 0) using (var br = new SolidBrush(oc))
                foreach (var rc in rects) { var o = rc; o.Inflate(ol, ol); g.FillRectangle(br, o); }
            using (var br = new SolidBrush(fill))
                foreach (var rc in rects) g.FillRectangle(br, rc);
        }

        // 불붙은 끝이 왼쪽 위(클릭 지점)를 향하도록 45° 기울인 담배. band = 필터 쪽 띠 색
        static Bitmap RenderCigarette(Profile p, Color band, Color oc, int ol, out Point hot, float flicker = 0.6f)
        {
            float L = Math.Max(16, p.Size);
            float T = Math.Max(4f, L * 0.17f);
            const float k = 0.70710678f;
            float glowMax = T * 1.05f;
            float glow = glowMax * (0.7f + 0.3f * flicker);
            float pad = ol + 3 + glowMax * k;
            int S = (int)Math.Ceiling((L + T) * k + pad * 2);
            float ox = pad + T * 0.5f * k, oy = ox;
            var bmp = new Bitmap(S, S, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TranslateTransform(ox, oy);
                g.RotateTransform(45);
                float h = T / 2;

                using (var gp = new GraphicsPath())
                {
                    gp.AddEllipse(-glow, -glow, glow * 2, glow * 2);
                    using (var pg = new PathGradientBrush(gp) { CenterColor = Color.FromArgb((int)(60 + 160 * flicker), 255, 120, 20), SurroundColors = new[] { Color.FromArgb(0, 255, 80, 0) } })
                        g.FillPath(pg, gp);
                }
                if (ol > 0) using (var br = new SolidBrush(oc)) g.FillRectangle(br, -ol * 0.5f, -h - ol, L + ol * 1.5f, T + ol * 2);

                RectangleF paper = new RectangleF(L * 0.1f, -h, L * 0.56f, T);
                RectangleF filter = new RectangleF(L * 0.66f, -h, L * 0.34f, T);
                FillCylinder(g, paper, Color.FromArgb(205, 205, 200), Color.FromArgb(255, 255, 252));
                FillCylinder(g, filter, Color.FromArgb(176, 112, 48), Color.FromArgb(232, 172, 98));
                var rnd = new Random(7);
                using (var sp = new SolidBrush(Color.FromArgb(170, 150, 90, 35)))
                    for (int i = 0; i < (int)(L * 0.35f); i++)
                    {
                        float d = Math.Max(1f, T * 0.1f);
                        g.FillEllipse(sp, filter.X + (float)rnd.NextDouble() * (filter.Width - d), -h + (float)rnd.NextDouble() * (T - d), d, d);
                    }
                using (var gold = new Pen(Color.FromArgb(201, 162, 39), Math.Max(1f, L * 0.015f))) g.DrawLine(gold, L * 0.66f, -h, L * 0.66f, h);
                using (var br = new SolidBrush(band))
                    g.FillPolygon(br, new[] { new PointF(L * 0.655f, -h), new PointF(L * 0.655f, h), new PointF(L * 0.6f, h), new PointF(L * 0.545f, 0), new PointF(L * 0.6f, -h) });

                FillCylinder(g, new RectangleF(L * 0.035f, -h, L * 0.07f, T), Color.FromArgb(90, 90, 90), Color.FromArgb(160, 160, 158));
                var emberHot = Color.FromArgb(255, (int)(190 + 65 * flicker), (int)(90 + 150 * flicker));
                using (var br = new LinearGradientBrush(new RectangleF(-1, -h, L * 0.05f + 2, T), emberHot, Color.FromArgb((int)(170 + 60 * flicker), 40, 0), 0f))
                    g.FillRectangle(br, 0, -h, L * 0.045f, T);
            }
            hot = new Point((int)Math.Round(ox), (int)Math.Round(oy));
            return bmp;
        }

        // 칼끝이 왼쪽 위를 향한 검. accent = 손잡이 가드/폼멜 색
        static Bitmap RenderSword(Profile p, Color accent, Color oc, int ol, out Point hot)
        {
            float L = Math.Max(16, p.Size);
            float T = Math.Max(3f, L * 0.1f);
            float gw = L * 0.16f;
            const float k = 0.70710678f;
            float pad = ol + 3 + T * k;
            int S = (int)Math.Ceiling(pad * 2 + (L + T) * k);
            var bmp = new Bitmap(S, S, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TranslateTransform(pad, pad);
                g.RotateTransform(45);
                var blade = new[] { new PointF(0, 0), new PointF(L * 0.1f, -T / 2), new PointF(L * 0.6f, -T / 2), new PointF(L * 0.6f, T / 2), new PointF(L * 0.1f, T / 2) };
                var guard = new RectangleF(L * 0.6f, -gw, L * 0.05f, gw * 2);
                var grip = new RectangleF(L * 0.65f, -T * 0.35f, L * 0.24f, T * 0.7f);
                float pr = T * 0.6f;
                var pommel = new RectangleF(L * 0.89f, -pr, pr * 2, pr * 2);
                if (ol > 0)
                    using (var gp = new GraphicsPath())
                    using (var pen = new Pen(oc, ol * 2) { LineJoin = LineJoin.Round })
                    {
                        gp.AddPolygon(blade); gp.AddRectangle(guard); gp.AddRectangle(grip); gp.AddEllipse(pommel);
                        g.DrawPath(pen, gp);
                    }
                using (var br = new LinearGradientBrush(new RectangleF(0, -T / 2 - 0.5f, L * 0.6f, T + 1), Color.Gray, Color.Gray, 90f))
                {
                    var edge = Color.FromArgb(142, 152, 163); var mid = Color.FromArgb(244, 247, 250);
                    br.InterpolationColors = new ColorBlend { Colors = new[] { edge, mid, mid, edge }, Positions = new[] { 0f, 0.4f, 0.5f, 1f } };
                    g.FillPolygon(br, blade);
                }
                using (var pen = new Pen(Color.FromArgb(150, 160, 170), Math.Max(1f, T * 0.12f))) g.DrawLine(pen, L * 0.12f, 0, L * 0.6f, 0);
                using (var br = new LinearGradientBrush(new RectangleF(guard.X - 0.5f, guard.Y, guard.Width + 1, guard.Height), Lighten(accent, 0.4f), accent, 0f))
                    g.FillRectangle(br, guard);
                using (var br = new SolidBrush(Color.FromArgb(74, 46, 27))) g.FillRectangle(br, grip);
                using (var pen = new Pen(Color.FromArgb(46, 28, 16), Math.Max(1f, T * 0.1f)))
                    for (float x = grip.X + T * 0.3f; x < grip.Right; x += T * 0.5f)
                        g.DrawLine(pen, x, grip.Top, x + T * 0.25f, grip.Bottom);
                using (var br = new LinearGradientBrush(new RectangleF(pommel.X, pommel.Y - 0.5f, pommel.Width, pommel.Height + 1), Lighten(accent, 0.4f), accent, 90f))
                    g.FillEllipse(br, pommel);
            }
            hot = new Point((int)Math.Round(pad), (int)Math.Round(pad));
            return bmp;
        }

        static void FillCylinder(Graphics g, RectangleF r, Color edge, Color mid)
        {
            using (var br = new LinearGradientBrush(new RectangleF(r.X, r.Y - 0.5f, r.Width, r.Height + 1), edge, edge, 90f))
            {
                br.InterpolationColors = new ColorBlend { Colors = new[] { edge, mid, mid, edge }, Positions = new[] { 0f, 0.35f, 0.55f, 1f } };
                g.FillRectangle(br, r);
            }
        }

        static Bitmap RenderImage(Profile p, out Point hot)
        {
            string path = p.ImagePath;
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) throw new FileNotFoundException("이미지 파일을 찾을 수 없습니다: " + path);
            string ext = Path.GetExtension(path).ToLowerInvariant();
            Bitmap src;
            Point fileHot = Point.Empty;
            bool hasFileHot = false;

            if (ext == ".cur" || ext == ".ani")
            {
                IntPtr h = Native.LoadCursorFromFile(path);
                if (h == IntPtr.Zero) throw new Exception("커서 파일을 읽을 수 없습니다.");
                Native.ICONINFO ii;
                if (Native.GetIconInfo(h, out ii))
                {
                    fileHot = new Point(ii.xHotspot, ii.yHotspot); hasFileHot = true;
                    if (ii.hbmColor != IntPtr.Zero) Native.DeleteObject(ii.hbmColor);
                    if (ii.hbmMask != IntPtr.Zero) Native.DeleteObject(ii.hbmMask);
                }
                using (var ic = Icon.FromHandle(h)) src = ic.ToBitmap();
                Native.DestroyCursor(h);
            }
            else if (ext == ".ico")
            {
                using (var ic = new Icon(path, 256, 256)) src = ic.ToBitmap();
            }
            else
            {
                using (var img = Image.FromFile(path)) src = new Bitmap(img);
            }

            float sc = Math.Min((float)p.Size / src.Width, (float)p.Size / src.Height);
            int w = Math.Max(1, (int)Math.Round(src.Width * sc)), h2 = Math.Max(1, (int)Math.Round(src.Height * sc));
            var dst = new Bitmap(w, h2, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(dst))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.DrawImage(src, 0, 0, w, h2);
            }
            if (p.ImageHotspot == 1) hot = new Point(w / 2, h2 / 2);
            else if (p.ImageHotspot == 2 && hasFileHot) hot = new Point((int)(fileHot.X * sc), (int)(fileHot.Y * sc));
            else hot = Point.Empty;
            src.Dispose();
            return dst;
        }

        static Bitmap AddShadow(Bitmap src, int off)
        {
            var dst = new Bitmap(src.Width + off + 2, src.Height + off + 2, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(dst))
            using (var ia = new ImageAttributes())
            {
                var cm = new ColorMatrix(new float[][] {
                    new float[] { 0, 0, 0, 0, 0 }, new float[] { 0, 0, 0, 0, 0 }, new float[] { 0, 0, 0, 0, 0 },
                    new float[] { 0, 0, 0, 0.1f, 0 }, new float[] { 0, 0, 0, 0, 1 } });
                ia.SetColorMatrix(cm);
                for (int dx = 0; dx <= 2; dx++)
                    for (int dy = 0; dy <= 2; dy++)
                        g.DrawImage(src, new Rectangle(off - 1 + dx, off - 1 + dy, src.Width, src.Height), 0, 0, src.Width, src.Height, GraphicsUnit.Pixel, ia);
                g.DrawImage(src, new Rectangle(0, 0, src.Width, src.Height));
            }
            src.Dispose();
            return dst;
        }

        static Bitmap ApplyOpacity(Bitmap src, float a)
        {
            var dst = new Bitmap(src.Width, src.Height, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(dst))
            using (var ia = new ImageAttributes())
            {
                var cm = new ColorMatrix(); cm.Matrix33 = a;
                ia.SetColorMatrix(cm);
                g.DrawImage(src, new Rectangle(0, 0, src.Width, src.Height), 0, 0, src.Width, src.Height, GraphicsUnit.Pixel, ia);
            }
            src.Dispose();
            return dst;
        }
    }

    // ───────────────────────── 시스템 커서 교체 ─────────────────────────
    static class CursorEngine
    {
        const uint OCR_NORMAL = 32512;
        static readonly uint[] AllIds = { 32512, 32513, 32514, 32515, 32516, 32642, 32643, 32644, 32645, 32646, 32648, 32649, 32650, 32651, 32671, 32672 };

        public static void Restore()
        {
            Native.SystemParametersInfo(0x0057 /*SPI_SETCURSORS*/, 0, IntPtr.Zero, 0);
        }

        static void ApplyEach(Func<IntPtr> make, bool all)
        {
            foreach (uint id in all ? AllIds : new[] { OCR_NORMAL })
            {
                IntPtr h = make();
                if (h != IntPtr.Zero && !Native.SetSystemCursor(h, id)) Native.DestroyCursor(h);
            }
        }

        public static IntPtr MakeCursor(Bitmap bmp, Point hot)
        {
            IntPtr hIcon = bmp.GetHicon();
            Native.ICONINFO ii;
            Native.GetIconInfo(hIcon, out ii);
            ii.fIcon = false; ii.xHotspot = hot.X; ii.yHotspot = hot.Y;
            IntPtr cur = Native.CreateIconIndirect(ref ii);
            if (ii.hbmColor != IntPtr.Zero) Native.DeleteObject(ii.hbmColor);
            if (ii.hbmMask != IntPtr.Zero) Native.DeleteObject(ii.hbmMask);
            Native.DestroyIcon(hIcon);
            return cur;
        }

        public static void ApplySystem(Profile p)
        {
            if (CursorRenderer.IsAnimated(p.Style))
            {
                Point ahot;
                var frames = CursorRenderer.RenderFrames(p, out ahot);
                try
                {
                    string ani = AniWriter.Write(frames, ahot, CursorRenderer.Jiffies(p.Style));
                    ApplyEach(() => Native.LoadImage(IntPtr.Zero, ani, 2 /*IMAGE_CURSOR*/, 0, 0, 0x10 /*LR_LOADFROMFILE*/), p.AllCursors);
                }
                finally { foreach (var f in frames) f.Dispose(); }
                return;
            }
            string ext = p.Style == CursorRenderer.StyleImage ? Path.GetExtension(p.ImagePath ?? "").ToLowerInvariant() : "";
            if ((ext == ".cur" || ext == ".ani") && p.ImageHotspot == 2 && p.Opacity >= 100 && !p.Shadow && File.Exists(p.ImagePath))
            {
                string path = p.ImagePath; int sz = p.Size;
                ApplyEach(() =>
                {
                    IntPtr h = Native.LoadImage(IntPtr.Zero, path, 2 /*IMAGE_CURSOR*/, sz, sz, 0x10 /*LR_LOADFROMFILE*/);
                    return h != IntPtr.Zero ? h : Native.LoadCursorFromFile(path);
                }, p.AllCursors);
                return;
            }
            Point hot;
            using (var bmp = CursorRenderer.Render(p, out hot))
                ApplyEach(() => MakeCursor(bmp, hot), p.AllCursors);
        }

        public static void ApplyBlank()
        {
            var and = Enumerable.Repeat((byte)0xFF, 128).ToArray();
            var xor = new byte[128];
            ApplyEach(() => Native.CreateCursor(IntPtr.Zero, 0, 0, 32, 32, and, xor), true);
        }
    }

    // ───────────────────────── 애니메이션 커서(.ani) 파일 만들기 ─────────────────────────
    static class AniWriter
    {
        static int counter;

        public static string Write(Bitmap[] frames, Point hot, int jiffies)
        {
            // 커서는 최대 256px
            int w = frames[0].Width, h = frames[0].Height;
            float sc = Math.Min(1f, 256f / Math.Max(w, h));
            string dir = Path.Combine(Path.GetTempPath(), "MyCursor");
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, "anim_" + (counter++ % 4) + ".ani");

            var icons = new List<byte[]>();
            foreach (var f in frames)
            {
                if (sc < 1f)
                    using (var s = new Bitmap(Math.Max(1, (int)(w * sc)), Math.Max(1, (int)(h * sc)), PixelFormat.Format32bppArgb))
                    {
                        using (var g = Graphics.FromImage(s)) { g.InterpolationMode = InterpolationMode.HighQualityBicubic; g.DrawImage(f, 0, 0, s.Width, s.Height); }
                        icons.Add(CurBytes(s, new Point((int)(hot.X * sc), (int)(hot.Y * sc))));
                    }
                else icons.Add(CurBytes(f, hot));
            }

            using (var ms = new MemoryStream())
            using (var bw = new BinaryWriter(ms))
            {
                bw.Write(Encoding.ASCII.GetBytes("RIFF")); bw.Write(0); bw.Write(Encoding.ASCII.GetBytes("ACON"));
                bw.Write(Encoding.ASCII.GetBytes("anih")); bw.Write(36);
                bw.Write(36); bw.Write(frames.Length); bw.Write(frames.Length);
                bw.Write(0); bw.Write(0); bw.Write(0); bw.Write(0);
                bw.Write(Math.Max(1, jiffies)); bw.Write(1 /*AF_ICON*/);
                bw.Write(Encoding.ASCII.GetBytes("LIST"));
                long listSizePos = ms.Position; bw.Write(0);
                bw.Write(Encoding.ASCII.GetBytes("fram"));
                foreach (var ic in icons)
                {
                    bw.Write(Encoding.ASCII.GetBytes("icon")); bw.Write(ic.Length); bw.Write(ic);
                    if (ic.Length % 2 == 1) bw.Write((byte)0);
                }
                long end = ms.Position;
                ms.Position = listSizePos; bw.Write((int)(end - listSizePos - 4));
                ms.Position = 4; bw.Write((int)(end - 8));
                bw.Flush();
                File.WriteAllBytes(path, ms.ToArray());
            }
            return path;
        }

        // 32비트 알파 .cur 한 장
        static byte[] CurBytes(Bitmap b, Point hot)
        {
            int w = b.Width, h = b.Height;
            int maskStride = ((w + 31) / 32) * 4;
            int xorSize = w * h * 4, andSize = maskStride * h;
            using (var ms = new MemoryStream())
            using (var bw = new BinaryWriter(ms))
            {
                bw.Write((short)0); bw.Write((short)2); bw.Write((short)1);
                bw.Write((byte)(w >= 256 ? 0 : w)); bw.Write((byte)(h >= 256 ? 0 : h)); bw.Write((byte)0); bw.Write((byte)0);
                bw.Write((short)hot.X); bw.Write((short)hot.Y);
                bw.Write(40 + xorSize + andSize); bw.Write(22);
                bw.Write(40); bw.Write(w); bw.Write(h * 2); bw.Write((short)1); bw.Write((short)32);
                bw.Write(0); bw.Write(xorSize + andSize); bw.Write(0); bw.Write(0); bw.Write(0); bw.Write(0);
                var data = b.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                try
                {
                    var row = new byte[w * 4];
                    for (int y = h - 1; y >= 0; y--)
                    {
                        Marshal.Copy(new IntPtr(data.Scan0.ToInt64() + (long)y * data.Stride), row, 0, row.Length);
                        bw.Write(row); // BGRA
                    }
                }
                finally { b.UnlockBits(data); }
                bw.Write(new byte[andSize]);
                bw.Flush();
                return ms.ToArray();
            }
        }
    }

    // ───────────────────────── 클릭 통과 투명 창 공통 ─────────────────────────
    class ClickThroughForm : Form
    {
        public ClickThroughForm()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
        }

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                // LAYERED | TRANSPARENT(클릭 통과) | TOOLWINDOW | TOPMOST | NOACTIVATE
                cp.ExStyle |= 0x80000 | 0x20 | 0x80 | 0x8 | 0x8000000;
                return cp;
            }
        }

        protected override bool ShowWithoutActivation { get { return true; } }
    }

    static class Layered
    {
        public static void Present(IntPtr hwnd, Bitmap bmp, int left, int top)
        {
            IntPtr screen = Native.GetDC(IntPtr.Zero);
            IntPtr mem = Native.CreateCompatibleDC(screen);
            IntPtr hb = bmp.GetHbitmap(Color.FromArgb(0));
            IntPtr old = Native.SelectObject(mem, hb);
            try
            {
                var size = new Native.SIZE(bmp.Width, bmp.Height);
                var src = new Native.POINT(0, 0);
                var dst = new Native.POINT(left, top);
                var blend = new Native.BLENDFUNCTION { SourceConstantAlpha = 255, AlphaFormat = 1 };
                Native.UpdateLayeredWindow(hwnd, screen, ref dst, ref size, mem, ref src, 0, ref blend, 2 /*ULW_ALPHA*/);
            }
            finally
            {
                Native.SelectObject(mem, old);
                Native.DeleteObject(hb);
                Native.DeleteDC(mem);
                Native.ReleaseDC(IntPtr.Zero, screen);
            }
        }
    }

    // ───────────────────────── 오버레이 커서 ─────────────────────────
    class OverlayForm : ClickThroughForm
    {
        volatile int hx, hy;
        volatile bool running, forceMove;
        Thread thread;
        IntPtr hwnd;
        readonly object lk = new object();
        Bitmap[] frames;
        int frameMs, frameIdx;

        // frames 소유권은 오버레이가 가져감 (다음 교체 때 해제)
        public void SetFrames(Bitmap[] f, Point hot, int msPerFrame)
        {
            hwnd = Handle;
            Bitmap[] old;
            Native.POINT cur; Native.GetCursorPos(out cur);
            lock (lk)
            {
                old = frames; frames = f; frameMs = msPerFrame; frameIdx = 0;
                hx = hot.X; hy = hot.Y;
                Layered.Present(hwnd, f[0], cur.X - hot.X, cur.Y - hot.Y);
            }
            if (old != null) foreach (var b in old) b.Dispose();
            forceMove = true;
        }

        public void Start()
        {
            if (!Visible) Show();
            if (running) return;
            running = true;
            thread = new Thread(Loop) { IsBackground = true, Priority = ThreadPriority.AboveNormal };
            thread.Start();
        }

        public void Stop()
        {
            running = false;
            if (thread != null) { thread.Join(300); thread = null; }
            if (Visible) Hide();
        }

        void Loop()
        {
            Native.timeBeginPeriod(1);
            int lx = int.MinValue, ly = int.MinValue, tick = 0;
            IntPtr TOP = new IntPtr(-1);
            const uint flags = 0x0001 | 0x0010 | 0x4000 | 0x0040; // NOSIZE | NOACTIVATE | ASYNC | SHOWWINDOW
            var sw = Stopwatch.StartNew();
            long lastAnim = 0;
            while (running)
            {
                Native.POINT p;
                if (Native.GetCursorPos(out p))
                {
                    int x = p.X - hx, y = p.Y - hy;
                    bool retop = (++tick % 500) == 0;
                    long now = sw.ElapsedMilliseconds;
                    bool animated = false;
                    lock (lk)
                    {
                        if (frames != null && frames.Length > 1 && frameMs > 0 && now - lastAnim >= frameMs)
                        {
                            lastAnim = now;
                            frameIdx = (frameIdx + 1) % frames.Length;
                            Layered.Present(hwnd, frames[frameIdx], x, y);
                            lx = x; ly = y; animated = true;
                        }
                    }
                    if (animated && !retop) { Thread.Sleep(1); continue; }
                    if (x != lx || y != ly || retop || forceMove)
                    {
                        forceMove = false;
                        Native.SetWindowPos(hwnd, TOP, x, y, 0, 0, flags);
                        lx = x; ly = y;
                    }
                }
                Thread.Sleep(1);
            }
            Native.timeEndPeriod(1);
        }
    }

    // ───────────────────────── 효과 (꼬리 · 헤일로 · 글로우 · 클릭 · 십자선 · 돋보기) ─────────────────────────
    class FxConfig
    {
        public bool Trail; public int TrailStyle, TrailLength, TrailWidth, TrailColor; public bool TrailRainbow;
        public Bitmap CursorBmp; public Point Hot;
        public bool Halo; public int HaloColor, HaloSize; public bool HaloPulse;
        public bool Glow; public int GlowColor, GlowSize;
        public bool Click; public int ClickColor;
        public bool Cross; public int CrossColor, CrossThick, CrossAlpha;
        public bool Zoom; public int ZoomFactor, ZoomSize;
        public bool Any { get { return Trail || Halo || Glow || Click || Cross || Zoom; } }
    }

    // 전용 스레드에서 도는 효과 렌더러. 필요한 영역만 그려서 가볍게 유지한다.
    class FxHost : ClickThroughForm
    {
        static volatile FxConfig cfg;
        static volatile bool quit;
        static Thread thread;

        public static void Configure(FxConfig c)
        {
            if (c != null && !c.Any) c = null;
            cfg = c;
            if (c != null && thread == null)
            {
                quit = false;
                thread = new Thread(() => { using (var f = new FxHost()) f.Run(); }) { IsBackground = true };
                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
            }
        }

        public static void Shutdown()
        {
            cfg = null; quit = true;
            if (thread != null) { thread.Join(500); thread = null; }
        }

        struct TP { public int X, Y; public long T; }
        class Puff { public float X, Y, VX, VY, R0, Life, Phase, Freq, Amp; public long Born; }
        class Ripple { public int X, Y; public long T; public bool Right; }

        readonly List<TP> pts = new List<TP>();
        readonly List<Puff> puffs = new List<Puff>();
        readonly List<Ripple> ripples = new List<Ripple>();
        readonly Random rnd = new Random();
        Bitmap[] sprites;
        bool haveEmit;
        float emitX, emitY;
        long lastEmitT;
        ClickThroughForm lineH, lineV, lens;
        Bitmap lensSrc, lensOut;

        float R(float a, float b) { return a + (float)rnd.NextDouble() * (b - a); }
        static Color Opaque(int argb) { return Color.FromArgb(255, Color.FromArgb(argb)); }

        void Run()
        {
            IntPtr hwnd = Handle;
            lineH = new ClickThroughForm(); lineV = new ClickThroughForm(); lens = new ClickThroughForm();
            IntPtr hH = lineH.Handle, hV = lineV.Handle, hL = lens.Handle;
#if !NOAFFINITY
            try { Native.SetWindowDisplayAffinity(hL, 0x11 /*WDA_EXCLUDEFROMCAPTURE*/); } catch { }
#endif
            Native.timeBeginPeriod(1);
            var sw = Stopwatch.StartNew();
            FxConfig last = null;
            bool shown = false, crossShown = false, lensShown = false, prevL = false, prevR = false, force = true;
            long lastFrame = 0, lastTop = 0;
            int lastX = int.MinValue, lastY = int.MinValue, crossX = int.MinValue, crossY = int.MinValue;
            IntPtr TOP = new IntPtr(-1);

            while (!quit)
            {
                Application.DoEvents();
                var c = cfg;
                long now = sw.ElapsedMilliseconds;
                if (c == null)
                {
                    pts.Clear(); puffs.Clear(); ripples.Clear(); haveEmit = false;
                    if (shown) { Native.ShowWindow(hwnd, 0); shown = false; }
                    if (crossShown) { Native.ShowWindow(hH, 0); Native.ShowWindow(hV, 0); crossShown = false; }
                    if (lensShown) { Native.ShowWindow(hL, 0); lensShown = false; }
                    last = null;
                    Thread.Sleep(30);
                    continue;
                }
                if (c != last)
                {
                    last = c; force = true;
                    if (c.Cross)
                    {
                        var col = Opaque(c.CrossColor);
                        lineH.BackColor = col; lineV.BackColor = col;
                        byte a = (byte)Math.Max(10, Math.Min(255, c.CrossAlpha * 255 / 100));
                        Native.SetLayeredWindowAttributes(hH, 0, a, 2);
                        Native.SetLayeredWindowAttributes(hV, 0, a, 2);
                        crossX = int.MinValue;
                    }
                    if (!c.Trail) { pts.Clear(); puffs.Clear(); haveEmit = false; }
                }
                Native.POINT p;
                Native.GetCursorPos(out p);

                // 꼬리 궤적
                if (c.Trail && c.TrailStyle != 3)
                {
                    if (pts.Count == 0 || pts[pts.Count - 1].X != p.X || pts[pts.Count - 1].Y != p.Y)
                        pts.Add(new TP { X = p.X, Y = p.Y, T = now });
                    int life = Math.Max(30, c.TrailLength);
                    while (pts.Count > 0 && now - pts[0].T > life) pts.RemoveAt(0);
                }
                else if (pts.Count > 0) pts.Clear();

                // 클릭 감지
                if (c.Click)
                {
                    bool l = (Native.GetAsyncKeyState(1) & 0x8000) != 0, r = (Native.GetAsyncKeyState(2) & 0x8000) != 0;
                    if (l && !prevL) ripples.Add(new Ripple { X = p.X, Y = p.Y, T = now });
                    if (r && !prevR) ripples.Add(new Ripple { X = p.X, Y = p.Y, T = now, Right = true });
                    prevL = l; prevR = r;
                }
                ripples.RemoveAll(q => now - q.T > 450);

                // 화면 십자선
                if (c.Cross)
                {
                    if (p.X != crossX || p.Y != crossY || !crossShown)
                    {
                        int vx = Native.GetSystemMetrics(76), vy = Native.GetSystemMetrics(77);
                        int vw = Native.GetSystemMetrics(78), vh = Native.GetSystemMetrics(79);
                        int t = Math.Max(1, c.CrossThick);
                        const uint fl = 0x0010 | 0x0040; // NOACTIVATE | SHOWWINDOW
                        Native.SetWindowPos(hH, TOP, vx, p.Y - t / 2, vw, t, fl);
                        Native.SetWindowPos(hV, TOP, p.X - t / 2, vy, t, vh, fl);
                        crossX = p.X; crossY = p.Y; crossShown = true;
                    }
                }
                else if (crossShown) { Native.ShowWindow(hH, 0); Native.ShowWindow(hV, 0); crossShown = false; }

                if (now - lastFrame >= 12)
                {
                    float dt = Math.Min(0.1f, (now - lastFrame) / 1000f);
                    lastFrame = now;
                    if (c.Trail && c.TrailStyle == 3) SmokeSim(c, now, p, dt);
                    else if (puffs.Count > 0) { puffs.Clear(); haveEmit = false; }

                    bool moved = p.X != lastX || p.Y != lastY;
                    bool animated = pts.Count >= 2 || puffs.Count > 0 || ripples.Count > 0 || (c.Halo && c.HaloPulse);
                    bool steady = c.Halo || c.Glow;
                    if (animated || force || (steady && moved) || (shown && !steady))
                    {
                        bool drew = false;
                        try { drew = RenderMain(hwnd, c, p, now); } catch { }
                        if (drew && !shown) { Native.ShowWindow(hwnd, 4); shown = true; }
                        else if (!drew && shown) { Native.ShowWindow(hwnd, 0); shown = false; }
                    }
                    if (c.Zoom)
                    {
                        try { RenderLens(hL, c, p); } catch { }
                        if (!lensShown) { Native.ShowWindow(hL, 4); lensShown = true; }
                    }
                    else if (lensShown) { Native.ShowWindow(hL, 0); lensShown = false; }

                    if (now - lastTop > 1000)
                    {
                        lastTop = now;
                        const uint f2 = 0x0001 | 0x0002 | 0x0010; // NOSIZE | NOMOVE | NOACTIVATE
                        if (shown) Native.SetWindowPos(hwnd, TOP, 0, 0, 0, 0, f2);
                        if (lensShown) Native.SetWindowPos(hL, TOP, 0, 0, 0, 0, f2);
                        if (crossShown) { Native.SetWindowPos(hH, TOP, 0, 0, 0, 0, f2); Native.SetWindowPos(hV, TOP, 0, 0, 0, 0, f2); }
                    }
                    lastX = p.X; lastY = p.Y; force = false;
                }
                Thread.Sleep(2);
            }
            Native.ShowWindow(hwnd, 0); Native.ShowWindow(hH, 0); Native.ShowWindow(hV, 0); Native.ShowWindow(hL, 0);
            Native.timeEndPeriod(1);
        }

        static Color Hsv(float h)
        {
            h = ((h % 360) + 360) % 360;
            float x = 1 - Math.Abs((h / 60f) % 2 - 1);
            float r, g, b;
            if (h < 60) { r = 1; g = x; b = 0; } else if (h < 120) { r = x; g = 1; b = 0; }
            else if (h < 180) { r = 0; g = 1; b = x; } else if (h < 240) { r = 0; g = x; b = 1; }
            else if (h < 300) { r = x; g = 0; b = 1; } else { r = 1; g = 0; b = x; }
            return Color.FromArgb((int)(r * 255), (int)(g * 255), (int)(b * 255));
        }

        void BuildSprites()
        {
            const int N = 16, D = 48;
            sprites = new Bitmap[N];
            for (int lv = 0; lv < N; lv++)
            {
                var b = new Bitmap(D, D, PixelFormat.Format32bppArgb);
                float maxA = (lv + 1) / (float)N;
                for (int y = 0; y < D; y++)
                    for (int x = 0; x < D; x++)
                    {
                        float dx = (x + 0.5f - D / 2f) / (D / 2f), dy = (y + 0.5f - D / 2f) / (D / 2f);
                        float d = (float)Math.Sqrt(dx * dx + dy * dy);
                        if (d >= 1) continue;
                        float a = (1 - d * d) * (1 - d * d) * maxA;
                        b.SetPixel(x, y, Color.FromArgb((int)(a * 255), 125, 127, 140));
                    }
                sprites[lv] = b;
            }
        }

        void AddPuff(float x, float y, long now, float w, float life)
        {
            if (puffs.Count > 500) return;
            puffs.Add(new Puff
            {
                X = x + R(-1, 1), Y = y + R(-1, 1), VX = R(-8, 8), VY = -R(18, 38),
                R0 = w * 0.75f * R(0.8f, 1.2f), Born = now, Life = life * R(0.7f, 1.2f),
                Phase = R(0, 6.283f), Freq = R(1.5f, 3.5f), Amp = R(5, 14)
            });
        }

        void SmokeSim(FxConfig c, long now, Native.POINT p, float dt)
        {
            float life = Math.Max(600, Math.Min(4000, c.TrailLength * 4));
            float w = Math.Max(2, c.TrailWidth);
            if (!haveEmit) { emitX = p.X; emitY = p.Y; haveEmit = true; lastEmitT = now; }
            float dx = p.X - emitX, dy = p.Y - emitY;
            float dist = (float)Math.Sqrt(dx * dx + dy * dy), spacing = Math.Max(2f, w * 0.6f);
            if (dist >= spacing)
            {
                int n = Math.Min(40, (int)(dist / spacing));
                for (int i = 1; i <= n; i++) AddPuff(emitX + dx * i / n, emitY + dy * i / n, now, w, life);
                emitX = p.X; emitY = p.Y; lastEmitT = now;
            }
            else if (now - lastEmitT >= 70) { AddPuff(p.X, p.Y, now, w * 0.8f, life); lastEmitT = now; }

            for (int i = puffs.Count - 1; i >= 0; i--)
            {
                var q = puffs[i];
                if (now - q.Born > q.Life) { puffs.RemoveAt(i); continue; }
                q.VY -= 10 * dt;
                q.VX *= 1 - 0.8f * dt;
                q.X += q.VX * dt; q.Y += q.VY * dt;
            }
        }

        bool RenderMain(IntPtr hwnd, FxConfig c, Native.POINT p, long now)
        {
            float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
            bool any = false;
            Action<float, float, float> inc = (x, y, r) =>
            {
                any = true;
                if (x - r < minX) minX = x - r; if (x + r > maxX) maxX = x + r;
                if (y - r < minY) minY = y - r; if (y + r > maxY) maxY = y + r;
            };

            if (c.Glow) inc(p.X, p.Y, c.GlowSize / 2f + 2);
            float haloR = c.HaloSize / 2f * (c.HaloPulse ? 1f + 0.07f * (float)Math.Sin(now / 170.0) : 1f);
            if (c.Halo) inc(p.X, p.Y, haloR + 6);
            bool ghost = c.Trail && c.TrailStyle == 2 && c.CursorBmp != null;
            int life = Math.Max(30, c.TrailLength);
            if (pts.Count >= 2)
            {
                float m = ghost ? Math.Max(c.CursorBmp.Width, c.CursorBmp.Height) + 2 : c.TrailWidth + 4;
                foreach (var t in pts) inc(t.X, t.Y, m);
            }
            int n2 = puffs.Count;
            var xs = new float[n2]; var ys = new float[n2]; var rs = new float[n2]; var al = new float[n2];
            for (int i = 0; i < n2; i++)
            {
                var q = puffs[i];
                float age = now - q.Born, t = age / q.Life;
                xs[i] = q.X + (float)Math.Sin(age / 1000f * q.Freq + q.Phase) * q.Amp * t;
                ys[i] = q.Y;
                rs[i] = q.R0 * (1 + 4 * t);
                al[i] = Math.Min(1f, age / 80f) * (float)Math.Pow(1 - t, 0.9) * 0.9f;
                inc(xs[i], ys[i], rs[i] + 1);
            }
            foreach (var r in ripples) inc(r.X, r.Y, 40);
            if (!any) return false;

            int left = (int)Math.Floor(minX) - 1, top = (int)Math.Floor(minY) - 1;
            int w = Math.Max(1, Math.Min(8000, (int)Math.Ceiling(maxX) - left + 1));
            int h = Math.Max(1, Math.Min(8000, (int)Math.Ceiling(maxY) - top + 1));

            using (var bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb))
            {
                using (var g = Graphics.FromImage(bmp))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    float cx = p.X - left, cy = p.Y - top;

                    if (c.Glow)
                    {
                        float gr = c.GlowSize / 2f;
                        var gc = Opaque(c.GlowColor);
                        using (var gp = new GraphicsPath())
                        {
                            gp.AddEllipse(cx - gr, cy - gr, gr * 2, gr * 2);
                            using (var pg = new PathGradientBrush(gp) { CenterPoint = new PointF(cx, cy), CenterColor = Color.FromArgb(170, gc), SurroundColors = new[] { Color.FromArgb(0, gc) } })
                                g.FillPath(pg, gp);
                        }
                    }

                    // 꼬리
                    int n = pts.Count;
                    if (n >= 2)
                    {
                        if (ghost)
                        {
                            long lastT = long.MaxValue;
                            using (var ia = new ImageAttributes())
                                for (int i = n - 2; i >= 0; i--)
                                {
                                    if (lastT - pts[i].T < 25) continue;
                                    lastT = pts[i].T;
                                    float a = 1f - (float)(now - pts[i].T) / life;
                                    if (a <= 0) continue;
                                    var cm = new ColorMatrix(); cm.Matrix33 = a * 0.55f;
                                    ia.SetColorMatrix(cm);
                                    int x = pts[i].X - left - c.Hot.X, y = pts[i].Y - top - c.Hot.Y;
                                    g.DrawImage(c.CursorBmp, new Rectangle(x, y, c.CursorBmp.Width, c.CursorBmp.Height),
                                        0, 0, c.CursorBmp.Width, c.CursorBmp.Height, GraphicsUnit.Pixel, ia);
                                }
                        }
                        else
                        {
                            Color baseColor = Opaque(c.TrailColor);
                            float hue0 = (now / 4) % 360;
                            for (int i = 1; i < n; i++)
                            {
                                float a = 1f - (float)(now - pts[i].T) / life;
                                if (a <= 0) continue;
                                Color col = c.TrailRainbow ? Hsv(hue0 + i * 6) : baseColor;
                                col = Color.FromArgb((int)(230 * a), col);
                                float x0 = pts[i - 1].X - left, y0 = pts[i - 1].Y - top, x1 = pts[i].X - left, y1 = pts[i].Y - top;
                                if (c.TrailStyle == 0)
                                {
                                    using (var pen = new Pen(col, Math.Max(1f, c.TrailWidth * a)) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                                        g.DrawLine(pen, x0, y0, x1, y1);
                                }
                                else
                                {
                                    float r = Math.Max(0.8f, c.TrailWidth / 2f * a);
                                    using (var br = new SolidBrush(col)) g.FillEllipse(br, x1 - r, y1 - r, r * 2, r * 2);
                                }
                            }
                        }
                    }

                    // 연기
                    if (n2 > 0)
                    {
                        if (sprites == null) BuildSprites();
                        g.InterpolationMode = InterpolationMode.Bilinear;
                        for (int i = 0; i < n2; i++)
                        {
                            int lv = (int)(al[i] * sprites.Length);
                            if (lv <= 0) continue;
                            if (lv > sprites.Length) lv = sprites.Length;
                            g.DrawImage(sprites[lv - 1], xs[i] - rs[i] - left, ys[i] - rs[i] - top, rs[i] * 2, rs[i] * 2);
                        }
                    }

                    // 헤일로
                    if (c.Halo)
                    {
                        var hc = Opaque(c.HaloColor);
                        float pw = Math.Max(2f, c.HaloSize / 14f);
                        using (var pen = new Pen(Color.FromArgb(90, 0, 0, 0), pw + 2)) g.DrawEllipse(pen, cx - haloR, cy - haloR, haloR * 2, haloR * 2);
                        using (var pen = new Pen(Color.FromArgb(235, hc), pw)) g.DrawEllipse(pen, cx - haloR, cy - haloR, haloR * 2, haloR * 2);
                    }

                    // 클릭 파문
                    var cc = Opaque(c.ClickColor);
                    foreach (var r in ripples)
                    {
                        float a = (now - r.T) / 450f;
                        float rr = 6 + 30 * a;
                        var col = Color.FromArgb((int)(230 * (1 - a)), cc);
                        float x = r.X - left, y = r.Y - top;
                        using (var pen = new Pen(Color.FromArgb((int)(80 * (1 - a)), 0, 0, 0), 4.5f)) g.DrawEllipse(pen, x - rr, y - rr, rr * 2, rr * 2);
                        using (var pen = new Pen(col, 2.5f)) g.DrawEllipse(pen, x - rr, y - rr, rr * 2, rr * 2);
                        if (r.Right) using (var pen = new Pen(col, 1.5f)) g.DrawEllipse(pen, x - rr * 0.6f, y - rr * 0.6f, rr * 1.2f, rr * 1.2f);
                    }
                }
                Layered.Present(hwnd, bmp, left, top);
            }
            return true;
        }

        void RenderLens(IntPtr hL, FxConfig c, Native.POINT p)
        {
            int S = Math.Max(100, c.ZoomSize);
            float F = Math.Max(1.5f, c.ZoomFactor);
            int src = Math.Max(10, (int)(S / F));
            if (lensSrc == null || lensSrc.Width != src) { if (lensSrc != null) lensSrc.Dispose(); lensSrc = new Bitmap(src, src, PixelFormat.Format32bppArgb); }
            if (lensOut == null || lensOut.Width != S) { if (lensOut != null) lensOut.Dispose(); lensOut = new Bitmap(S, S, PixelFormat.Format32bppArgb); }
            using (var g = Graphics.FromImage(lensSrc)) g.CopyFromScreen(p.X - src / 2, p.Y - src / 2, 0, 0, new Size(src, src));
            using (var g = Graphics.FromImage(lensOut))
            {
                g.Clear(Color.Transparent);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (var gp = new GraphicsPath())
                {
                    gp.AddEllipse(2, 2, S - 4, S - 4);
                    g.SetClip(gp);
                    g.InterpolationMode = InterpolationMode.NearestNeighbor;
                    g.PixelOffsetMode = PixelOffsetMode.Half;
                    g.DrawImage(lensSrc, new Rectangle(0, 0, S, S));
                    g.ResetClip();
                }
                g.PixelOffsetMode = PixelOffsetMode.Default;
                using (var pen = new Pen(Color.FromArgb(230, 20, 20, 24), 5)) g.DrawEllipse(pen, 3, 3, S - 6, S - 6);
                using (var pen = new Pen(Color.FromArgb(255, 79, 140, 255), 2.5f)) g.DrawEllipse(pen, 3, 3, S - 6, S - 6);
                float m = S / 2f;
                using (var pen = new Pen(Color.FromArgb(150, 255, 255, 255), 1)) { g.DrawLine(pen, m - 6, m, m + 6, m); g.DrawLine(pen, m, m - 6, m, m + 6); }
            }
            Layered.Present(hL, lensOut, p.X - S / 2, p.Y - S / 2);
        }
    }

    // ───────────────────────── 다크 테마 UI 컨트롤 ─────────────────────────
    static class Theme
    {
        public static readonly Color Bg = C(0x15161B), Side = C(0x1C1D24), Card = C(0x21222A), Card2 = C(0x2B2C36), Hover = C(0x383947),
            Border = C(0x34353F), Text = C(0xEDEDF2), Sub = C(0x9A9BAA), Dim = C(0x5E5F6C), Accent = C(0x4F8CFF),
            Star = C(0xFFC940), Good = C(0x3DDC97), Danger = C(0xFF5C6C);
        public const string FontName = "맑은 고딕";
        public static readonly Font Normal = new Font(FontName, 9.5f), Small = new Font(FontName, 8.5f), Bold = new Font(FontName, 9.5f, FontStyle.Bold),
            H = new Font(FontName, 11f, FontStyle.Bold), Title = new Font(FontName, 16f, FontStyle.Bold), Big = new Font(FontName, 13f, FontStyle.Bold);

        static Color C(int rgb) { return Color.FromArgb(255, (rgb >> 16) & 255, (rgb >> 8) & 255, rgb & 255); }

        public static GraphicsPath Round(RectangleF r, float rad)
        {
            var gp = new GraphicsPath();
            float d = Math.Max(1f, Math.Min(rad * 2, Math.Min(r.Width, r.Height)));
            gp.AddArc(r.X, r.Y, d, d, 180, 90);
            gp.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            gp.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            gp.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            gp.CloseFigure();
            return gp;
        }

        public static void FillRound(Graphics g, RectangleF r, float rad, Color c)
        {
            using (var gp = Round(r, rad)) using (var br = new SolidBrush(c)) g.FillPath(br, gp);
        }

        public static Color Mix(Color a, Color b, float t)
        {
            return Color.FromArgb(255, (int)(a.R + (b.R - a.R) * t), (int)(a.G + (b.G - a.G) * t), (int)(a.B + (b.B - a.B) * t));
        }
    }

    class UiControl : Control
    {
        protected bool hover, down;
        public UiControl()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            SetStyle(ControlStyles.StandardDoubleClick, false);
            ForeColor = Theme.Text;
            Font = Theme.Normal;
        }
        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; down = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { down = true; Invalidate(); base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { down = false; Invalidate(); base.OnMouseUp(e); }
        protected override void OnEnabledChanged(EventArgs e) { Invalidate(); base.OnEnabledChanged(e); }
        protected Graphics Begin(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(BackColor);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            return g;
        }
        protected const TextFormatFlags LeftMid = TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine;
        protected const TextFormatFlags CenterMid = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine;
    }

    class FlatButton : UiControl
    {
        public bool Primary;
        public FlatButton(string text, bool primary) { Text = text; Primary = primary; Cursor = Cursors.Hand; }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = Begin(e);
            Color bg = Primary ? Theme.Accent : Theme.Card2;
            if (!Enabled) bg = Theme.Card2;
            else if (down) bg = Theme.Mix(bg, Color.Black, 0.2f);
            else if (hover) bg = Theme.Mix(bg, Color.White, 0.1f);
            Theme.FillRound(g, new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f), Math.Min(10, Height * 0.3f), bg);
            TextRenderer.DrawText(g, Text, Primary ? Theme.Bold : Font, ClientRectangle, !Enabled ? Theme.Dim : (Primary ? Color.White : Theme.Text), CenterMid);
        }
    }

    class ToggleSwitch : UiControl
    {
        bool on;
        public event EventHandler CheckedChanged;
        public Font LabelFont;
        public ToggleSwitch(string text) { Text = text; Cursor = Cursors.Hand; }
        public bool Checked
        {
            get { return on; }
            set { if (on == value) return; on = value; Invalidate(); if (CheckedChanged != null) CheckedChanged(this, EventArgs.Empty); }
        }
        protected override void OnClick(EventArgs e) { if (Enabled) Checked = !Checked; base.OnClick(e); }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = Begin(e);
            float h = Math.Min(Height - 6, 22f), w = h * 1.8f, y = (Height - h) / 2f;
            Color track = on ? Theme.Accent : Theme.Hover;
            if (!Enabled) track = on ? Theme.Mix(Theme.Accent, BackColor, 0.6f) : Theme.Card2;
            else if (hover) track = Theme.Mix(track, Color.White, 0.08f);
            Theme.FillRound(g, new RectangleF(1, y, w, h), h / 2, track);
            float kd = h - 6, kx = on ? 1 + w - kd - 3 : 4;
            using (var br = new SolidBrush(Enabled ? Color.White : Theme.Sub)) g.FillEllipse(br, kx, y + 3, kd, kd);
            if (!string.IsNullOrEmpty(Text))
                TextRenderer.DrawText(g, Text, LabelFont ?? Font, new Rectangle((int)(w + 12), 0, Width - (int)(w + 12), Height), Enabled ? Theme.Text : Theme.Dim, LeftMid);
        }
    }

    class Slider : UiControl
    {
        int min, max = 100, val, step = 1;
        bool dragging;
        public string Caption = "", Suffix = "";
        public int CaptionWidth = 70, ValueWidth = 54;
        public event EventHandler ValueChanged;
        public Slider() { Cursor = Cursors.Hand; }
        public int Minimum { get { return min; } set { min = value; if (val < min) val = min; Invalidate(); } }
        public int Maximum { get { return max; } set { max = value; if (val > max) val = max; Invalidate(); } }
        public int Step { get { return step; } set { step = Math.Max(1, value); } }
        public int Value
        {
            get { return val; }
            set
            {
                int v = Math.Max(min, Math.Min(max, value));
                if (step > 1) v = min + (int)Math.Round((v - min) / (double)step) * step;
                v = Math.Max(min, Math.Min(max, v));
                if (v == val) return;
                val = v; Invalidate();
                if (ValueChanged != null) ValueChanged(this, EventArgs.Empty);
            }
        }
        Rectangle Track { get { int x0 = CaptionWidth + 4, x1 = Width - ValueWidth - 4; return new Rectangle(x0, Height / 2 - 2, Math.Max(10, x1 - x0), 4); } }
        void SetFromX(int x)
        {
            var t = Track;
            float f = Math.Max(0, Math.Min(1, (x - t.X) / (float)t.Width));
            Value = min + (int)Math.Round(f * (max - min));
        }
        protected override void OnMouseDown(MouseEventArgs e) { base.OnMouseDown(e); if (!Enabled) return; dragging = true; Capture = true; SetFromX(e.X); }
        protected override void OnMouseMove(MouseEventArgs e) { base.OnMouseMove(e); if (dragging) SetFromX(e.X); }
        protected override void OnMouseUp(MouseEventArgs e) { base.OnMouseUp(e); dragging = false; Capture = false; }
        protected override void OnMouseWheel(MouseEventArgs e) { base.OnMouseWheel(e); if (Enabled) Value += Math.Sign(e.Delta) * step; }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = Begin(e);
            if (CaptionWidth > 0) TextRenderer.DrawText(g, Caption, Font, new Rectangle(0, 0, CaptionWidth, Height), Enabled ? Theme.Sub : Theme.Dim, LeftMid);
            var t = Track;
            Theme.FillRound(g, t, 2, Theme.Hover);
            float f = max > min ? (val - min) / (float)(max - min) : 0;
            float kx = t.X + f * t.Width;
            Color acc = Enabled ? Theme.Accent : Theme.Dim;
            if (kx > t.X + 1) Theme.FillRound(g, new RectangleF(t.X, t.Y, kx - t.X, t.Height), 2, acc);
            float kr = Math.Min(8f, Height * 0.26f) + (hover || dragging ? 1 : 0);
            using (var br = new SolidBrush(Enabled ? Color.White : Theme.Sub)) g.FillEllipse(br, kx - kr, Height / 2f - kr, kr * 2, kr * 2);
            if (Enabled) using (var pen = new Pen(acc, 2)) g.DrawEllipse(pen, kx - kr, Height / 2f - kr, kr * 2, kr * 2);
            TextRenderer.DrawText(g, val + Suffix, Theme.Bold, new Rectangle(Width - ValueWidth, 0, ValueWidth, Height), Enabled ? Theme.Text : Theme.Dim, TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
        }
    }

    class ColorPicker : UiControl
    {
        Color val = Color.Red;
        public string Caption = "";
        public int CaptionWidth = 70;
        public event EventHandler ColorChanged;
        public static readonly Color[] Presets = {
            Color.FromArgb(255, 59, 48), Color.FromArgb(255, 149, 0), Color.FromArgb(255, 214, 10), Color.FromArgb(52, 199, 89),
            Color.FromArgb(0, 229, 255), Color.FromArgb(79, 140, 255), Color.FromArgb(175, 82, 222), Color.FromArgb(255, 45, 146),
            Color.White, Color.Black };
        public ColorPicker() { Cursor = Cursors.Hand; }
        public Color Value
        {
            get { return val; }
            set { var v = Color.FromArgb(255, value); if (v.ToArgb() == val.ToArgb()) return; val = v; Invalidate(); if (ColorChanged != null) ColorChanged(this, EventArgs.Empty); }
        }
        float H { get { return Math.Min(Height - 6, 24f); } }
        RectangleF MainRect { get { return new RectangleF(CaptionWidth, (Height - H) / 2f, 46, H); } }
        RectangleF PresetRect(int i) { float d = H * 0.78f; return new RectangleF(CaptionWidth + 58 + i * (d + 7), (Height - d) / 2f, d, d); }
        int PresetCount { get { float d = H * 0.78f; return Math.Max(0, Math.Min(Presets.Length, (int)((Width - CaptionWidth - 58 - 4) / (d + 7)))); } }
        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            if (!Enabled) return;
            if (MainRect.Contains(e.Location))
            {
                using (var d = new ColorDialog { FullOpen = true, AnyColor = true, Color = val })
                    if (d.ShowDialog(FindForm()) == DialogResult.OK) Value = d.Color;
                return;
            }
            for (int i = 0; i < PresetCount; i++)
                if (PresetRect(i).Contains(e.Location)) { Value = Presets[i]; return; }
        }
        protected override void OnMouseMove(MouseEventArgs e) { base.OnMouseMove(e); Invalidate(); }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = Begin(e);
            if (CaptionWidth > 0) TextRenderer.DrawText(g, Caption, Font, new Rectangle(0, 0, CaptionWidth, Height), Enabled ? Theme.Sub : Theme.Dim, LeftMid);
            var mr = MainRect;
            Color show = Enabled ? val : Theme.Mix(val, BackColor, 0.7f);
            Theme.FillRound(g, mr, 6, show);
            using (var gp = Theme.Round(mr, 6)) using (var pen = new Pen(hover && Enabled ? Theme.Text : Theme.Border, 1.5f)) g.DrawPath(pen, gp);
            var mp = PointToClient(MousePosition);
            for (int i = 0; i < PresetCount; i++)
            {
                var r = PresetRect(i);
                Color pc = Enabled ? Presets[i] : Theme.Mix(Presets[i], BackColor, 0.7f);
                using (var br = new SolidBrush(pc)) g.FillEllipse(br, r);
                bool selected = Presets[i].ToArgb() == val.ToArgb();
                if (selected || (Enabled && r.Contains(mp)))
                    using (var pen = new Pen(selected ? Theme.Text : Theme.Sub, 2)) g.DrawEllipse(pen, r.X - 2.5f, r.Y - 2.5f, r.Width + 5, r.Height + 5);
                else using (var pen = new Pen(Theme.Border, 1)) g.DrawEllipse(pen, r);
            }
        }
    }

    class Segmented : UiControl
    {
        public string[] Items = new string[0];
        int sel;
        public event EventHandler SelectedIndexChanged;
        public Segmented(params string[] items) { Items = items; Cursor = Cursors.Hand; }
        public int SelectedIndex
        {
            get { return sel; }
            set { int v = Math.Max(0, Math.Min(Items.Length - 1, value)); if (v == sel) return; sel = v; Invalidate(); if (SelectedIndexChanged != null) SelectedIndexChanged(this, EventArgs.Empty); }
        }
        protected override void OnMouseClick(MouseEventArgs e) { base.OnMouseClick(e); if (Enabled && Items.Length > 0) SelectedIndex = e.X * Items.Length / Math.Max(1, Width); }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = Begin(e);
            Theme.FillRound(g, new RectangleF(0, 0, Width - 1, Height - 1), 8, Theme.Card2);
            if (Items.Length == 0) return;
            float w = (Width - 6) / (float)Items.Length;
            for (int i = 0; i < Items.Length; i++)
            {
                var r = new RectangleF(3 + i * w, 3, w, Height - 7);
                if (i == sel) Theme.FillRound(g, r, 6, Enabled ? Theme.Accent : Theme.Hover);
                TextRenderer.DrawText(g, Items[i], i == sel ? Theme.Bold : Font, Rectangle.Round(r), !Enabled ? Theme.Dim : (i == sel ? Color.White : Theme.Sub), CenterMid);
            }
        }
    }

    class TabStrip : UiControl
    {
        public string[] Items;
        int sel, hot = -1;
        public event EventHandler SelectedIndexChanged;
        public TabStrip(params string[] items) { Items = items; Cursor = Cursors.Hand; }
        public int SelectedIndex
        {
            get { return sel; }
            set { if (value == sel) return; sel = value; Invalidate(); if (SelectedIndexChanged != null) SelectedIndexChanged(this, EventArgs.Empty); }
        }
        Rectangle TabRect(int i)
        {
            int x = 0;
            for (int k = 0; k < i; k++) x += TextRenderer.MeasureText(Items[k], Theme.Bold).Width + 30;
            return new Rectangle(x, 0, TextRenderer.MeasureText(Items[i], Theme.Bold).Width + 30, Height);
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int h = -1;
            for (int i = 0; i < Items.Length; i++) if (TabRect(i).Contains(e.Location)) h = i;
            if (h != hot) { hot = h; Invalidate(); }
        }
        protected override void OnMouseLeave(EventArgs e) { hot = -1; base.OnMouseLeave(e); }
        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            for (int i = 0; i < Items.Length; i++) if (TabRect(i).Contains(e.Location)) SelectedIndex = i;
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = Begin(e);
            for (int i = 0; i < Items.Length; i++)
            {
                var r = TabRect(i);
                if (i == hot && i != sel) Theme.FillRound(g, new RectangleF(r.X + 3, 4, r.Width - 6, r.Height - 10), 8, Theme.Card);
                TextRenderer.DrawText(g, Items[i], i == sel ? Theme.Bold : Theme.Normal, new Rectangle(r.X, 0, r.Width, r.Height - 4), i == sel ? Theme.Text : Theme.Sub, CenterMid);
                if (i == sel) Theme.FillRound(g, new RectangleF(r.X + 12, r.Height - 4, r.Width - 24, 3), 1.5f, Theme.Accent);
            }
        }
    }

    class TileGallery : UiControl
    {
        public List<int> Ids = new List<int>();
        public Func<int, string> LabelOf;
        public Func<int, Bitmap> ThumbOf;
        public int TileW = 76, TileH = 78, GapPx = 8;
        int sel = -1, hotId = -1;
        public event EventHandler SelectedChanged;
        public TileGallery() { Cursor = Cursors.Hand; }
        public int SelectedId
        {
            get { return sel; }
            set { if (value == sel) return; sel = value; Invalidate(); if (SelectedChanged != null) SelectedChanged(this, EventArgs.Empty); }
        }
        public int Cols { get { return Math.Max(1, (Width + GapPx) / (TileW + GapPx)); } }
        public int NeededHeight { get { int rows = (Ids.Count + Cols - 1) / Cols; return rows * TileH + Math.Max(0, rows - 1) * GapPx; } }
        Rectangle TileRect(int i) { int c = Cols; return new Rectangle((i % c) * (TileW + GapPx), (i / c) * (TileH + GapPx), TileW, TileH); }
        int Hit(Point pt) { for (int i = 0; i < Ids.Count; i++) if (TileRect(i).Contains(pt)) return Ids[i]; return -1; }
        protected override void OnMouseMove(MouseEventArgs e) { base.OnMouseMove(e); int h = Hit(e.Location); if (h != hotId) { hotId = h; Invalidate(); } }
        protected override void OnMouseLeave(EventArgs e) { hotId = -1; base.OnMouseLeave(e); }
        protected override void OnMouseClick(MouseEventArgs e) { base.OnMouseClick(e); if (!Enabled) return; int h = Hit(e.Location); if (h >= 0) SelectedId = h; }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = Begin(e);
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            for (int i = 0; i < Ids.Count; i++)
            {
                int id = Ids[i];
                var r = TileRect(i);
                bool s = id == sel, h = id == hotId && Enabled;
                Theme.FillRound(g, r, 10, s ? Theme.Mix(Theme.Card2, Theme.Accent, 0.18f) : (h ? Theme.Hover : Theme.Card2));
                if (s) using (var gp = Theme.Round(new RectangleF(r.X + 1, r.Y + 1, r.Width - 2, r.Height - 2), 9)) using (var pen = new Pen(Theme.Accent, 2)) g.DrawPath(pen, gp);
                var area = new Rectangle(r.X + 8, r.Y + 6, r.Width - 16, r.Height - 30);
                Bitmap b = null;
                try { b = ThumbOf != null ? ThumbOf(id) : null; } catch { b = null; }
                if (b != null)
                {
                    float sc = Math.Min(1.3f, Math.Min(area.Width / (float)b.Width, area.Height / (float)b.Height));
                    int bw = (int)(b.Width * sc), bh = (int)(b.Height * sc);
                    g.DrawImage(b, area.X + (area.Width - bw) / 2, area.Y + (area.Height - bh) / 2, bw, bh);
                }
                else
                {
                    // 이미지 아이콘
                    var ir = new Rectangle(area.X + area.Width / 2 - 14, area.Y + area.Height / 2 - 11, 28, 22);
                    using (var pen = new Pen(Theme.Sub, 2)) g.DrawRectangle(pen, ir);
                    using (var br = new SolidBrush(Theme.Sub))
                    {
                        g.FillPolygon(br, new[] { new Point(ir.X + 3, ir.Bottom - 3), new Point(ir.X + 11, ir.Y + 9), new Point(ir.X + 18, ir.Bottom - 3) });
                        g.FillEllipse(br, ir.Right - 10, ir.Y + 4, 5, 5);
                    }
                }
                string label = LabelOf != null ? LabelOf(id) : id.ToString();
                TextRenderer.DrawText(g, label, Theme.Small, new Rectangle(r.X + 2, r.Bottom - 24, r.Width - 4, 20), s ? Theme.Text : Theme.Sub, CenterMid);
                if (!Enabled) Theme.FillRound(g, r, 10, Color.FromArgb(150, BackColor));
            }
        }
    }

    class Card : Panel
    {
        public Card() { DoubleBuffered = true; BackColor = Theme.Card; ResizeRedraw = true; }
        protected override void OnPaintBackground(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Parent != null ? Parent.BackColor : Theme.Bg);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Theme.FillRound(g, new RectangleF(0, 0, Width - 1, Height - 1), 14, BackColor);
        }
    }

    class TextField : Panel
    {
        public readonly TextBox Box = new TextBox();
        public TextField()
        {
            DoubleBuffered = true;
            BackColor = Theme.Card2;
            Box.BorderStyle = BorderStyle.None;
            Box.BackColor = Theme.Card2;
            Box.ForeColor = Theme.Text;
            Box.Font = Theme.Normal;
            Controls.Add(Box);
        }
        protected override void OnLayout(LayoutEventArgs e)
        {
            base.OnLayout(e);
            Box.Width = Width - 24;
            Box.Location = new Point(12, (Height - Box.Height) / 2);
        }
        protected override void OnPaintBackground(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Parent != null ? Parent.BackColor : Theme.Card);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Theme.FillRound(g, new RectangleF(0, 0, Width - 1, Height - 1), 8, BackColor);
            using (var gp = Theme.Round(new RectangleF(0.5f, 0.5f, Width - 2, Height - 2), 8))
            using (var pen = new Pen(Box.Focused ? Theme.Accent : Theme.Border)) g.DrawPath(pen, gp);
        }
    }

    class DarkCombo : ComboBox
    {
        public DarkCombo()
        {
            DrawMode = DrawMode.OwnerDrawFixed; DropDownStyle = ComboBoxStyle.DropDownList; FlatStyle = FlatStyle.Flat;
            BackColor = Theme.Card2; ForeColor = Theme.Text; Font = Theme.Normal; ItemHeight = 24;
        }
        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            bool selected = (e.State & DrawItemState.Selected) != 0 && (e.State & DrawItemState.ComboBoxEdit) == 0;
            using (var br = new SolidBrush(selected ? Theme.Accent : Theme.Card2)) e.Graphics.FillRectangle(br, e.Bounds);
            TextRenderer.DrawText(e.Graphics, Items[e.Index].ToString(), Font, new Rectangle(e.Bounds.X + 8, e.Bounds.Y, e.Bounds.Width - 8, e.Bounds.Height), Theme.Text,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        }
    }

    class Chip : UiControl
    {
        public bool On;
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = Begin(e);
            Theme.FillRound(g, new RectangleF(0, 0, Width - 1, Height - 1), Height / 2f, On ? Theme.Mix(Theme.Card2, Theme.Good, 0.25f) : Theme.Card2);
            using (var br = new SolidBrush(On ? Theme.Good : Theme.Dim)) g.FillEllipse(br, 10, Height / 2f - 4, 8, 8);
            TextRenderer.DrawText(g, Text, Theme.Small, new Rectangle(22, 0, Width - 26, Height), On ? Theme.Text : Theme.Sub, LeftMid);
        }
    }

    // ───────────────────────── 빠른 커서 선택 (Ctrl+Alt+C) ─────────────────────────
    class QuickPicker : Form
    {
        public readonly TileGallery Gallery = new TileGallery();
        public readonly Slider SizeSlider = new Slider();
        public readonly FlatButton OpenSettings = new FlatButton("설정 열기", false);

        public QuickPicker(string profileName, Func<int, Bitmap> thumb, int style, int size)
        {
            FormBorderStyle = FormBorderStyle.None; ShowInTaskbar = false; TopMost = true; StartPosition = FormStartPosition.Manual;
            BackColor = Theme.Card; KeyPreview = true; DoubleBuffered = true;
            AutoScaleDimensions = new SizeF(96F, 96F); AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(564, 392);
            var title = new Label { Text = "빠른 커서 선택", Font = Theme.H, ForeColor = Theme.Text, BackColor = Theme.Card, AutoSize = true, Location = new Point(18, 14) };
            var sub = new Label { Text = profileName + "  ·  Esc로 닫기", Font = Theme.Small, ForeColor = Theme.Sub, BackColor = Theme.Card, AutoSize = true, Location = new Point(150, 18) };
            var kind = new Segmented("기본", "애니메이션") { BackColor = Theme.Card, Bounds = new Rectangle(356, 12, 190, 30) };
            Gallery.BackColor = Theme.Card; Gallery.TileW = 80; Gallery.TileH = 80;
            Gallery.Bounds = new Rectangle(18, 50, 528, 260);
            bool anim = CursorRenderer.IsAnimated(style);
            kind.SelectedIndex = anim ? 1 : 0;
            Gallery.Ids.AddRange(anim ? CursorRenderer.AnimatedOrder : CursorRenderer.GalleryOrder);
            kind.SelectedIndexChanged += delegate
            {
                Gallery.Ids.Clear();
                Gallery.Ids.AddRange(kind.SelectedIndex == 1 ? CursorRenderer.AnimatedOrder : CursorRenderer.GalleryOrder);
                Gallery.Invalidate();
            };
            Gallery.LabelOf = id => CursorRenderer.StyleNames[id];
            Gallery.ThumbOf = thumb;
            Gallery.SelectedId = style;
            var anim2 = new System.Windows.Forms.Timer { Interval = 50 };
            anim2.Tick += delegate { if (kind.SelectedIndex == 1) Gallery.Invalidate(); };
            anim2.Start();
            FormClosed += delegate { anim2.Dispose(); };
            Controls.Add(kind);
            SizeSlider.BackColor = Theme.Card; SizeSlider.Caption = "크기"; SizeSlider.Suffix = "px"; SizeSlider.CaptionWidth = 44;
            SizeSlider.Minimum = 8; SizeSlider.Maximum = 128; SizeSlider.Value = size;
            SizeSlider.Bounds = new Rectangle(18, 330, 380, 36);
            OpenSettings.BackColor = Theme.Card;
            OpenSettings.Bounds = new Rectangle(416, 332, 130, 34);
            Controls.AddRange(new Control[] { title, sub, Gallery, SizeSlider, OpenSettings });
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (var pen = new Pen(Theme.Accent, 2)) e.Graphics.DrawRectangle(pen, 1, 1, ClientSize.Width - 2, ClientSize.Height - 2);
        }
        protected override void OnDeactivate(EventArgs e) { base.OnDeactivate(e); Close(); }
        protected override void OnKeyDown(KeyEventArgs e) { base.OnKeyDown(e); if (e.KeyCode == Keys.Escape) Close(); }
    }

    // ───────────────────────── 메인 창 ─────────────────────────
    class MainForm : Form
    {
        const int HK_TOGGLE = 1, HK_NEXT = 2, HK_PICK = 3, HK_ZOOM = 4, HK_LOCK = 5, HK_SPEED = 6;
        const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        const string AppName = "MyCursor";
        public static int OriginalSpeedForCrash = -1;

        readonly Settings S;
        readonly OverlayForm overlay = new OverlayForm();
        readonly NotifyIcon tray = new NotifyIcon();
        readonly System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
        readonly int ownPid = Process.GetCurrentProcess().Id;

        // 헤더 / 사이드바
        ToggleSwitch swEnabled;
        Label lblHeaderStatus;
        ListBox lst;
        TabStrip tabs;
        Panel[] pages;
        // 커서 탭
        TileGallery galStyle;
        Slider slSize, slOpacity, slOutline, slThick, slGap;
        ToggleSwitch swShadow;
        ColorPicker cpColor, cpOutline;
        TextField tfImage;
        FlatButton btnImage;
        Segmented segHot, segKind;
        readonly System.Windows.Forms.Timer animTimer = new System.Windows.Forms.Timer();
        // 꼬리 탭
        ToggleSwitch swTrail, swRainbow;
        TileGallery galTrail;
        Slider slTrailLen, slTrailW;
        ColorPicker cpTrail;
        // 효과 탭
        ToggleSwitch swHalo, swPulse, swGlow, swClick, swCross, swZoom;
        ColorPicker cpHalo, cpGlow, cpClick, cpCross;
        Slider slHalo, slGlow, slCrossT, slCrossA, slZoomF, slZoomS;
        // 게임·적용 탭
        TextField tfName, tfProc;
        FlatButton btnPick, btnMakeDefault;
        Segmented segMode;
        Label lblModeDesc;
        ToggleSwitch swAll, swHide;
        // 편의 기능 탭
        ToggleSwitch swAuto, swLock, swSpeed, swStartup;
        DarkCombo cmbDefault;
        Slider slSpeed;
        Label lblSpeedInfo;
        // 오른쪽
        UiControl preview;
        Segmented segBg, segScale;
        Label lblNow, lblReason;
        Chip chipZoom, chipLock, chipSpeed;

        bool loading, startHidden, reallyExit, dirty, appliedOnce, zoomOn, lockOn, speedOn;
        int version, appliedVersion = -1, originalSpeed = 10;
        Profile applied, pinned;
        string status = "", reasonText = "";
        readonly Dictionary<int, Bitmap> thumbCache = new Dictionary<int, Bitmap>();
        string thumbKey = "";
        readonly Dictionary<Profile, Bitmap> listThumbs = new Dictionary<Profile, Bitmap>();
        int listThumbVersion = -1;
        readonly Bitmap[] trailThumbs = new Bitmap[4];
        Point _dummy;

        public MainForm(bool hidden)
        {
            startHidden = hidden;
            S = Settings.Load();
            Native.SystemParametersInfoGetInt(Native.SPI_GETMOUSESPEED, 0, ref originalSpeed, 0);
            OriginalSpeedForCrash = originalSpeed;

            SuspendLayout();
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            Font = Theme.Normal;
            BackColor = Theme.Bg;
            ForeColor = Theme.Text;
            Text = "MyCursor";
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            ClientSize = new Size(1120, 724);
            StartPosition = FormStartPosition.CenterScreen;
            DoubleBuffered = true;
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }
            if (Icon == null)
                using (var b = CursorRenderer.Render(new Profile { Size = 26 }, out _dummy)) Icon = Icon.FromHandle(b.GetHicon());
            BuildUi();
            ResumeLayout(false);
            PerformLayout();

            tray.Icon = Icon;
            tray.Text = "MyCursor";
            tray.Visible = true;
            tray.DoubleClick += delegate { ShowMain(); };
            var menu = new ContextMenuStrip();
            menu.Items.Add("열기", null, delegate { ShowMain(); });
            menu.Items.Add("켜기 / 끄기   (Ctrl+Alt+F9)", null, delegate { swEnabled.Checked = !swEnabled.Checked; });
            menu.Items.Add("빠른 커서 선택   (Ctrl+Alt+C)", null, delegate { OpenPicker(); });
            menu.Items.Add("다음 평소 커서   (Ctrl+Alt+F10)", null, delegate { NextDefault(); });
            menu.Items.Add("돋보기   (Ctrl+Alt+Z)", null, delegate { ToggleZoom(); });
            menu.Items.Add("커서 가두기   (Ctrl+Alt+L)", null, delegate { ToggleLock(); });
            menu.Items.Add("감도 전환   (Ctrl+Alt+S)", null, delegate { ToggleSpeed(); });
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("종료 (커서 원래대로)", null, delegate { ExitApp(); });
            tray.ContextMenuStrip = menu;

            RefreshList(0);
            RefreshDefaultCombo();
            loading = true;
            swEnabled.Checked = S.Enabled;
            swAuto.Checked = S.AutoSwitch;
            swStartup.Checked = IsStartupEnabled();
            slZoomF.Value = S.ZoomFactor; slZoomS.Value = S.ZoomSize; slSpeed.Value = S.AltSpeed;
            lblSpeedInfo.Text = "윈도우 기본 감도: " + originalSpeed + "  (1~20, 기본값 10)";
            loading = false;

            SystemEvents.SessionEnding += delegate { CursorEngine.Restore(); };
            timer.Interval = 150;
            timer.Tick += delegate { Tick(); };
            timer.Start();

            // 애니메이션 커서 미리보기 / 갤러리 갱신
            animTimer.Interval = 40;
            animTimer.Tick += delegate
            {
                if (!Visible) return;
                var p = Current;
                if (p != null && CursorRenderer.IsAnimated(p.Style)) preview.Invalidate();
                if (pages[0].Visible && segKind.SelectedIndex == 1) galStyle.Invalidate();
            };
            animTimer.Start();
        }

        // ───── UI 구성 도우미 ─────
        static T Add<T>(Control parent, T c, int x, int y, int w, int h) where T : Control
        {
            c.Bounds = new Rectangle(x, y, w, h);
            if (!(c is Card) && !(c is TextField) && !(c is ListBox) && !(c is ComboBox)) c.BackColor = parent.BackColor;
            parent.Controls.Add(c);
            return c;
        }

        static Label Lbl(Control parent, string text, int x, int y, Font f, Color col, int w = 0, int h = 0)
        {
            var l = new Label { Text = text, Font = f, ForeColor = col, BackColor = parent.BackColor, Location = new Point(x, y), UseMnemonic = false };
            if (w > 0) { l.AutoSize = false; l.Size = new Size(w, h); } else l.AutoSize = true;
            parent.Controls.Add(l);
            return l;
        }

        static void Divider(Control parent, int y) { Add(parent, new Label { BackColor = Theme.Border }, 20, y, 500, 1).BackColor = Theme.Border; }

        Slider Sl(Control parent, string caption, int x, int y, int w, int min, int max, string suffix, bool profileField = true)
        {
            var s = new Slider { Caption = caption, Suffix = suffix, Minimum = min, Maximum = max };
            Add(parent, s, x, y, w, 30);
            if (profileField) s.ValueChanged += delegate { OnEdit(); };
            return s;
        }

        ToggleSwitch Sw(Control parent, string text, int x, int y, int w, bool profileField = true)
        {
            var t = new ToggleSwitch(text);
            Add(parent, t, x, y, w, 30);
            if (profileField) t.CheckedChanged += delegate { OnEdit(); };
            return t;
        }

        ColorPicker Cp(Control parent, string caption, int x, int y, int w, int captionWidth = 70)
        {
            var c = new ColorPicker { Caption = caption, CaptionWidth = captionWidth };
            Add(parent, c, x, y, w, 30);
            c.ColorChanged += delegate { OnEdit(); };
            return c;
        }

        void BuildUi()
        {
            // ── 헤더
            var logo = Add(this, new PictureBox { SizeMode = PictureBoxSizeMode.Zoom }, 22, 16, 34, 34);
            try { logo.Image = Icon.ToBitmap(); } catch { }
            Lbl(this, "MyCursor", 64, 12, Theme.Title, Theme.Text);
            Lbl(this, "나만의 커서 · 꼬리 · 효과", 66, 42, Theme.Small, Theme.Sub);
            lblHeaderStatus = Lbl(this, "", 560, 24, Theme.Bold, Theme.Good, 330, 22);
            lblHeaderStatus.TextAlign = ContentAlignment.MiddleRight;
            swEnabled = new ToggleSwitch("커스텀 커서") { LabelFont = Theme.Bold };
            Add(this, swEnabled, 910, 18, 190, 32);
            swEnabled.CheckedChanged += delegate { if (loading) return; S.Enabled = swEnabled.Checked; dirty = true; lst.Invalidate(); };

            // ── 사이드바 (프로필)
            var side = Add(this, new Card(), 16, 72, 250, 636);
            Lbl(side, "프로필", 18, 14, Theme.H, Theme.Text);
            lst = new ListBox { DrawMode = DrawMode.OwnerDrawFixed, ItemHeight = 58, BorderStyle = BorderStyle.None, BackColor = Theme.Card, ForeColor = Theme.Text, IntegralHeight = false };
            Add(side, lst, 8, 46, 234, 488);
            lst.DrawItem += DrawProfileItem;
            lst.SelectedIndexChanged += delegate { if (!loading) LoadProfileToUi(); };
            var bAdd = Add(side, new FlatButton("＋  새 프로필", true), 12, 546, 226, 36);
            var bDup = Add(side, new FlatButton("복제", false), 12, 590, 109, 32);
            var bDel = Add(side, new FlatButton("삭제", false), 129, 590, 109, 32);
            bAdd.Click += delegate { AddProfile(new Profile { Name = UniqueName("새 프로필") }); };
            bDup.Click += delegate { var c = Current; if (c != null) { var n = c.Clone(); n.Name = UniqueName(c.Name + " 복사본"); AddProfile(n); } };
            bDel.Click += delegate { DeleteProfile(); };

            // ── 가운데 (탭)
            tabs = new TabStrip("커서", "꼬리", "효과", "게임 · 적용", "편의 기능");
            Add(this, tabs, 282, 72, 540, 40);
            var content = Add(this, new Card(), 282, 116, 540, 592);
            pages = new Panel[5];
            for (int i = 0; i < 5; i++)
            {
                pages[i] = new Panel { BackColor = Theme.Card, Visible = i == 0 };
                Add(content, pages[i], 0, 6, 540, 580);
            }
            tabs.SelectedIndexChanged += delegate { for (int i = 0; i < 5; i++) pages[i].Visible = i == tabs.SelectedIndex; };
            BuildCursorPage(pages[0]);
            BuildTrailPage(pages[1]);
            BuildFxPage(pages[2]);
            BuildGamePage(pages[3]);
            BuildConveniencePage(pages[4]);

            // ── 오른쪽 (미리보기 / 상태)
            var pv = Add(this, new Card(), 838, 72, 266, 342);
            Lbl(pv, "미리보기", 16, 14, Theme.H, Theme.Text);
            preview = Add(pv, new UiControl(), 14, 46, 238, 240);
            preview.Paint += PaintPreview;
            segBg = Add(pv, new Segmented("체크", "밝게", "어둡게"), 14, 296, 160, 32);
            segScale = Add(pv, new Segmented("1x", "2x"), 182, 296, 70, 32);
            segBg.SelectedIndexChanged += delegate { preview.Invalidate(); };
            segScale.SelectedIndexChanged += delegate { preview.Invalidate(); };

            var st = Add(this, new Card(), 838, 426, 266, 282);
            Lbl(st, "지금 적용 중", 16, 14, Theme.Small, Theme.Sub);
            lblNow = Lbl(st, "-", 16, 34, Theme.Big, Theme.Text, 236, 28);
            lblReason = Lbl(st, "", 16, 64, Theme.Small, Theme.Sub, 236, 20);
            chipZoom = Add(st, new Chip { Text = "돋보기" }, 16, 92, 74, 26);
            chipLock = Add(st, new Chip { Text = "가두기" }, 96, 92, 74, 26);
            chipSpeed = Add(st, new Chip { Text = "감도" }, 176, 92, 74, 26);
            Lbl(st, "단축키", 16, 130, Theme.Small, Theme.Sub);
            string[,] keys = { { "Ctrl+Alt+F9", "켜기 / 끄기" }, { "Ctrl+Alt+C", "빠른 커서 선택" }, { "Ctrl+Alt+F10", "평소 커서 바꾸기" },
                               { "Ctrl+Alt+Z", "돋보기" }, { "Ctrl+Alt+L", "창 안에 가두기" }, { "Ctrl+Alt+S", "감도 전환" } };
            for (int i = 0; i < keys.GetLength(0); i++)
            {
                Lbl(st, keys[i, 0], 16, 152 + i * 20, Theme.Small, Theme.Accent);
                Lbl(st, keys[i, 1], 120, 152 + i * 20, Theme.Small, Theme.Text);
            }
        }

        void BuildCursorPage(Panel pg)
        {
            Lbl(pg, "커서 모양", 20, 10, Theme.H, Theme.Text);
            segKind = Add(pg, new Segmented("기본 18종", "애니메이션 8종"), 300, 6, 224, 30);
            segKind.SelectedIndexChanged += delegate { ShowStyleKind(segKind.SelectedIndex == 1); };
            galStyle = new TileGallery { TileW = 76, TileH = 76 };
            galStyle.Ids.AddRange(CursorRenderer.GalleryOrder);
            galStyle.LabelOf = id => CursorRenderer.StyleNames[id];
            galStyle.ThumbOf = id => StyleThumb(Current, id);
            Add(pg, galStyle, 20, 40, 504, 244);
            galStyle.SelectedChanged += delegate { OnEdit(); };

            slSize = Sl(pg, "크기", 20, 296, 240, 8, 256, "px");
            slOpacity = Sl(pg, "불투명도", 280, 296, 240, 10, 100, "%");
            slOutline = Sl(pg, "외곽선", 20, 332, 240, 0, 6, "px");
            slThick = Sl(pg, "선 두께", 280, 332, 240, 1, 16, "px");
            slGap = Sl(pg, "십자 간격", 20, 368, 240, 0, 30, "px");
            swShadow = Sw(pg, "그림자", 280, 368, 240);
            cpColor = Cp(pg, "색상", 20, 408, 500);
            cpOutline = Cp(pg, "외곽선 색", 20, 444, 500);
            Divider(pg, 486);
            Lbl(pg, "이미지 파일 (png · ico · cur · ani)", 20, 496, Theme.Small, Theme.Sub);
            tfImage = Add(pg, new TextField(), 20, 518, 384, 32);
            tfImage.Box.TextChanged += delegate { OnEdit(); };
            btnImage = Add(pg, new FlatButton("찾아보기", false), 412, 518, 108, 32);
            btnImage.Click += delegate { BrowseImage(); };
            segHot = Add(pg, new Segmented("클릭: 왼쪽 위", "클릭: 정중앙", "클릭: 파일 값"), 20, 556, 500, 22);
            segHot.Height = 24;
            segHot.SelectedIndexChanged += delegate { OnEdit(); };
        }

        void BuildTrailPage(Panel pg)
        {
            swTrail = Sw(pg, "마우스 꼬리 켜기", 20, 12, 300);
            swTrail.LabelFont = Theme.H;
            Lbl(pg, "꼬리 모양", 20, 58, Theme.Small, Theme.Sub);
            galTrail = new TileGallery { TileW = 116, TileH = 88 };
            galTrail.Ids.AddRange(new[] { 0, 1, 2, 3 });
            string[] names = { "부드러운 꼬리", "점 잔상", "커서 잔상", "담배 연기" };
            galTrail.LabelOf = id => names[id];
            galTrail.ThumbOf = id => TrailThumb(id);
            Add(pg, galTrail, 20, 80, 500, 88);
            galTrail.SelectedChanged += delegate { OnEdit(); };
            slTrailLen = Sl(pg, "길이", 20, 186, 500, 50, 1000, "ms");
            slTrailLen.Step = 10;
            slTrailW = Sl(pg, "두께", 20, 222, 500, 1, 30, "px");
            cpTrail = Cp(pg, "색상", 20, 262, 500);
            swRainbow = Sw(pg, "무지개색", 20, 300, 240);
            Lbl(pg, "꼬리는 선택한 프로필에만 적용돼요. 바탕화면에서 보려면\n★(평소 커서) 프로필에서 켜세요. 담배 연기는 멈춰 있어도 피어올라요.", 20, 350, Theme.Small, Theme.Sub, 500, 40);
        }

        void BuildFxPage(Panel pg)
        {
            swHalo = Sw(pg, "헤일로 (커서 주변 링)", 20, 10, 240);
            cpHalo = Cp(pg, "", 270, 10, 250, 0);
            slHalo = Sl(pg, "크기", 48, 44, 250, 16, 140, "px");
            swPulse = Sw(pg, "두근두근", 320, 44, 200);
            Divider(pg, 84);
            swGlow = Sw(pg, "글로우 (커서 주변 빛)", 20, 94, 240);
            cpGlow = Cp(pg, "", 270, 94, 250, 0);
            slGlow = Sl(pg, "크기", 48, 128, 250, 20, 240, "px");
            Divider(pg, 168);
            swClick = Sw(pg, "클릭 효과 (파문)", 20, 178, 240);
            cpClick = Cp(pg, "", 270, 178, 250, 0);
            Divider(pg, 218);
            swCross = Sw(pg, "화면 십자선 (가이드 선)", 20, 228, 240);
            cpCross = Cp(pg, "", 270, 228, 250, 0);
            slCrossT = Sl(pg, "두께", 48, 262, 220, 1, 8, "px");
            slCrossA = Sl(pg, "진하기", 290, 262, 230, 10, 100, "%");
            Divider(pg, 302);
            Lbl(pg, "돋보기", 20, 314, Theme.H, Theme.Text);
            Lbl(pg, "Ctrl+Alt+Z · 모든 프로필 공통", 86, 318, Theme.Small, Theme.Sub);
            swZoom = Sw(pg, "지금 켜기", 380, 312, 140, false);
            swZoom.CheckedChanged += delegate { if (!loading && swZoom.Checked != zoomOn) ToggleZoom(); };
            slZoomF = Sl(pg, "배율", 20, 348, 240, 2, 6, "x", false);
            slZoomS = Sl(pg, "렌즈 크기", 280, 348, 240, 120, 420, "px", false);
            slZoomS.Step = 10;
            slZoomF.ValueChanged += delegate { if (loading) return; S.ZoomFactor = slZoomF.Value; dirty = true; ApplyFx(); };
            slZoomS.ValueChanged += delegate { if (loading) return; S.ZoomSize = slZoomS.Value; dirty = true; ApplyFx(); };
            Lbl(pg, "효과는 화면 위에 덧그려져서 클릭을 방해하지 않아요.\n게임에서는 창 모드 또는 전체 창 모드(테두리 없음)에서 보입니다.", 20, 400, Theme.Small, Theme.Sub, 500, 40);
        }

        void BuildGamePage(Panel pg)
        {
            Lbl(pg, "프로필 이름", 20, 12, Theme.Small, Theme.Sub);
            tfName = Add(pg, new TextField(), 20, 34, 500, 34);
            tfName.Box.TextChanged += delegate { OnEdit(); };
            Lbl(pg, "게임 실행 파일 (exe)", 20, 84, Theme.Small, Theme.Sub);
            tfProc = Add(pg, new TextField(), 20, 106, 370, 34);
            tfProc.Box.TextChanged += delegate { OnEdit(); };
            btnPick = Add(pg, new FlatButton("실행 중에서 선택", false), 398, 106, 122, 34);
            btnPick.Click += delegate { ShowProcessPicker(); };
            Lbl(pg, "예: League of Legends.exe, valorant.exe  ·  쉼표로 여러 개  ·  비우면 일반 프로필", 20, 146, Theme.Small, Theme.Dim);
            Divider(pg, 176);
            Lbl(pg, "적용 방식", 20, 188, Theme.Small, Theme.Sub);
            segMode = Add(pg, new Segmented("시스템 커서 교체", "오버레이"), 20, 210, 500, 36);
            segMode.SelectedIndexChanged += delegate { OnEdit(); };
            lblModeDesc = Lbl(pg, "", 20, 254, Theme.Small, Theme.Sub, 500, 40);
            swAll = Sw(pg, "텍스트 · 손 · 대기 등 모든 커서 종류도 같이 교체", 20, 300, 500);
            swHide = Sw(pg, "오버레이일 때 원래 윈도우 커서 숨기기", 20, 336, 500);
            Divider(pg, 380);
            btnMakeDefault = Add(pg, new FlatButton("★  이 프로필을 평소 커서로 지정", true), 20, 394, 280, 38);
            btnMakeDefault.Click += delegate
            {
                var p = Current; if (p == null) return;
                S.DefaultProfile = p.Name; dirty = true;
                RefreshDefaultCombo(); lst.Invalidate();
            };
        }

        void BuildConveniencePage(Panel pg)
        {
            swAuto = Sw(pg, "게임별 자동 전환", 20, 12, 500, false);
            swAuto.CheckedChanged += delegate { if (loading) return; S.AutoSwitch = swAuto.Checked; dirty = true; };
            Lbl(pg, "게임 exe가 지정된 프로필은 그 게임이 켜지면 자동으로 적용돼요.", 60, 42, Theme.Small, Theme.Sub);
            Lbl(pg, "평소(게임 밖) 커서", 20, 72, Theme.Small, Theme.Sub);
            cmbDefault = new DarkCombo();
            Add(pg, cmbDefault, 20, 94, 300, 30);
            cmbDefault.SelectedIndexChanged += delegate
            {
                if (loading) return;
                S.DefaultProfile = cmbDefault.SelectedIndex <= 0 ? "" : S.Profiles[cmbDefault.SelectedIndex - 1].Name;
                dirty = true; lst.Invalidate();
            };
            Divider(pg, 140);
            swLock = Sw(pg, "창 안에 커서 가두기   (Ctrl+Alt+L)", 20, 152, 500, false);
            swLock.CheckedChanged += delegate { if (!loading && swLock.Checked != lockOn) ToggleLock(); };
            Lbl(pg, "듀얼 모니터에서 게임하다 마우스가 다른 화면으로 나가는 걸 막아요.", 60, 182, Theme.Small, Theme.Sub);
            swSpeed = Sw(pg, "마우스 감도 전환   (Ctrl+Alt+S)", 20, 214, 500, false);
            swSpeed.CheckedChanged += delegate { if (!loading && swSpeed.Checked != speedOn) ToggleSpeed(); };
            slSpeed = Sl(pg, "전환 감도", 60, 246, 460, 1, 20, "", false);
            slSpeed.ValueChanged += delegate { if (loading) return; S.AltSpeed = slSpeed.Value; dirty = true; if (speedOn) Native.SetMouseSpeed(S.AltSpeed); };
            lblSpeedInfo = Lbl(pg, "", 60, 280, Theme.Small, Theme.Sub);
            Divider(pg, 312);
            swStartup = Sw(pg, "윈도우 시작 시 자동 실행", 20, 324, 500, false);
            swStartup.CheckedChanged += delegate { if (!loading) SetStartup(swStartup.Checked); };
            var bOff = Add(pg, new FlatButton("커서를 윈도우 기본으로 되돌리기", false), 20, 372, 260, 36);
            bOff.Click += delegate { swEnabled.Checked = false; };
        }

        // ───── 썸네일 ─────
        Bitmap StyleThumb(Profile p, int style)
        {
            if (p == null) p = new Profile();
            string key = p.Color + "|" + p.OutlineColor + "|" + p.Outline + "|" + p.Shadow + "|" + p.Thickness + "|" + p.Gap + "|" + p.ImagePath + "|" + p.ImageHotspot;
            if (key != thumbKey)
            {
                foreach (var b in thumbCache.Values) if (b != null) b.Dispose();
                thumbCache.Clear(); thumbKey = key;
            }
            // 애니메이션 커서는 현재 시각의 프레임을 보여줌
            int n = CursorRenderer.Frames(style);
            int frame = n > 1 ? (int)(CursorRenderer.PhaseNow(style) * n) % n : 0;
            int cacheId = style * 1000 + frame;
            Bitmap bmp;
            if (thumbCache.TryGetValue(cacheId, out bmp)) return bmp;
            try
            {
                var q = p.Clone(); q.Style = style; q.Opacity = 100;
                q.Size = style == CursorRenderer.StyleCigarette || style == 25 || style == 15 ? 50 : (style == 0 || style == 8 || style == 10 || style == 19 ? 38 : (style == 20 ? 32 : 34));
                q.Thickness = Math.Min(p.Thickness, 4); q.Gap = Math.Min(p.Gap, 6);
                Point hot;
                bmp = CursorRenderer.RenderAt(q, n > 1 ? frame / (float)n : 0.2f, out hot);
            }
            catch { bmp = null; }
            thumbCache[cacheId] = bmp;
            return bmp;
        }

        void ShowStyleKind(bool animated)
        {
            galStyle.Ids.Clear();
            galStyle.Ids.AddRange(animated ? CursorRenderer.AnimatedOrder : CursorRenderer.GalleryOrder);
            galStyle.Invalidate();
        }

        Bitmap ListThumb(Profile p)
        {
            if (listThumbVersion != version)
            {
                foreach (var b in listThumbs.Values) if (b != null) b.Dispose();
                listThumbs.Clear(); listThumbVersion = version;
            }
            Bitmap bmp;
            if (listThumbs.TryGetValue(p, out bmp)) return bmp;
            try { var q = p.Clone(); q.Size = Math.Min(40, Math.Max(24, p.Size)); q.Opacity = 100; Point hot; bmp = CursorRenderer.Render(q, out hot); }
            catch { bmp = null; }
            listThumbs[p] = bmp;
            return bmp;
        }

        Bitmap TrailThumb(int id)
        {
            if (trailThumbs[id] != null) return trailThumbs[id];
            var b = new Bitmap(90, 56, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(b))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                var pts = new List<PointF>();
                for (int i = 0; i <= 20; i++) { float t = i / 20f; pts.Add(new PointF(8 + t * 70, 46 - t * 34 - (float)Math.Sin(t * Math.PI) * 10)); }
                Color c = Color.FromArgb(79, 195, 247);
                if (id == 0)
                    for (int i = 1; i < pts.Count; i++)
                        using (var pen = new Pen(Color.FromArgb((int)(40 + 200 * i / 20f), c), 1 + 5 * i / 20f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                            g.DrawLine(pen, pts[i - 1], pts[i]);
                else if (id == 1)
                    for (int i = 0; i < pts.Count; i += 2)
                    {
                        float r = 1 + 3 * i / 20f;
                        using (var br = new SolidBrush(Color.FromArgb((int)(40 + 200 * i / 20f), c))) g.FillEllipse(br, pts[i].X - r, pts[i].Y - r, r * 2, r * 2);
                    }
                else if (id == 2)
                {
                    Point hot;
                    using (var arrow = CursorRenderer.Render(new Profile { Size = 20, Color = unchecked((int)0xFF4FC3F7), Shadow = false }, out hot))
                    using (var ia = new ImageAttributes())
                        for (int i = 4; i <= 20; i += 5)
                        {
                            var cm = new ColorMatrix(); cm.Matrix33 = 0.2f + 0.8f * i / 20f; ia.SetColorMatrix(cm);
                            g.DrawImage(arrow, new Rectangle((int)pts[i].X, (int)pts[i].Y - 4, arrow.Width, arrow.Height), 0, 0, arrow.Width, arrow.Height, GraphicsUnit.Pixel, ia);
                        }
                }
                else
                {
                    var rnd = new Random(3);
                    for (int i = 0; i < 16; i++)
                    {
                        float t = i / 15f;
                        float x = 45 + (float)Math.Sin(t * 5) * 8 + (float)rnd.NextDouble() * 4, y = 50 - t * 44, r = 3 + t * 9;
                        using (var gp = new GraphicsPath())
                        {
                            gp.AddEllipse(x - r, y - r, r * 2, r * 2);
                            using (var br = new PathGradientBrush(gp) { CenterColor = Color.FromArgb((int)(200 * (1 - t * 0.7f)), 190, 192, 205), SurroundColors = new[] { Color.FromArgb(0, 190, 192, 205) } })
                                g.FillPath(br, gp);
                        }
                    }
                }
            }
            trailThumbs[id] = b;
            return b;
        }

        // ───── 프로필 목록 ─────
        Profile Current { get { int i = lst.SelectedIndex; return i >= 0 && i < S.Profiles.Count ? S.Profiles[i] : null; } }

        void DrawProfileItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= S.Profiles.Count) return;
            var p = S.Profiles[e.Index];
            var g = e.Graphics;
            var r = e.Bounds;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            using (var br = new SolidBrush(Theme.Card)) g.FillRectangle(br, r);
            bool sel = (e.State & DrawItemState.Selected) != 0;
            if (sel)
            {
                Theme.FillRound(g, new RectangleF(r.X + 2, r.Y + 3, r.Width - 4, r.Height - 6), 10, Theme.Card2);
                Theme.FillRound(g, new RectangleF(r.X + 2, r.Y + 14, 4, r.Height - 28), 2, Theme.Accent);
            }
            var tb = new Rectangle(r.X + 14, r.Y + 10, 38, 38);
            Theme.FillRound(g, tb, 8, Theme.Bg);
            var th = ListThumb(p);
            if (th != null)
            {
                float sc = Math.Min(1f, Math.Min((tb.Width - 6) / (float)th.Width, (tb.Height - 6) / (float)th.Height));
                int w = (int)(th.Width * sc), h = (int)(th.Height * sc);
                g.DrawImage(th, tb.X + (tb.Width - w) / 2, tb.Y + (tb.Height - h) / 2, w, h);
            }
            const TextFormatFlags f = TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine;
            TextRenderer.DrawText(g, p.Name, Theme.Bold, new Rectangle(r.X + 62, r.Y + 10, r.Width - 96, 20), Theme.Text, f);
            string sub = string.IsNullOrWhiteSpace(p.Process) ? "일반 프로필" : "게임 · " + p.Process;
            TextRenderer.DrawText(g, sub, Theme.Small, new Rectangle(r.X + 62, r.Y + 31, r.Width - 96, 18), Theme.Sub, f);
            if (p.Name == S.DefaultProfile) TextRenderer.DrawText(g, "★", Theme.H, new Rectangle(r.Right - 32, r.Y + 7, 24, 22), Theme.Star, f);
            if (p == applied && S.Enabled) using (var br = new SolidBrush(Theme.Good)) g.FillEllipse(br, r.Right - 24, r.Y + 36, 8, 8);
        }

        void RefreshList(int select)
        {
            loading = true;
            lst.BeginUpdate();
            lst.Items.Clear();
            foreach (var p in S.Profiles) lst.Items.Add(p.Name);
            lst.EndUpdate();
            if (S.Profiles.Count > 0) lst.SelectedIndex = Math.Max(0, Math.Min(select, S.Profiles.Count - 1));
            loading = false;
            LoadProfileToUi();
        }

        void RefreshDefaultCombo()
        {
            loading = true;
            cmbDefault.Items.Clear();
            cmbDefault.Items.Add("(윈도우 기본 커서)");
            int sel = 0;
            for (int i = 0; i < S.Profiles.Count; i++)
            {
                cmbDefault.Items.Add(S.Profiles[i].Name);
                if (S.Profiles[i].Name == S.DefaultProfile) sel = i + 1;
            }
            cmbDefault.SelectedIndex = sel;
            loading = false;
        }

        string UniqueName(string baseName)
        {
            string n = baseName; int k = 2;
            while (S.Profiles.Any(p => p.Name == n)) n = baseName + " " + (k++);
            return n;
        }

        void AddProfile(Profile p)
        {
            S.Profiles.Add(p);
            dirty = true; version++;
            RefreshList(S.Profiles.Count - 1);
            RefreshDefaultCombo();
        }

        void DeleteProfile()
        {
            var c = Current;
            if (c == null) return;
            if (MessageBox.Show(this, "'" + c.Name + "' 프로필을 삭제할까요?", "삭제", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            int i = lst.SelectedIndex;
            S.Profiles.Remove(c);
            if (S.DefaultProfile == c.Name) S.DefaultProfile = "";
            dirty = true; version++;
            RefreshList(i);
            RefreshDefaultCombo();
        }

        static Color Col(int argb) { return Color.FromArgb(255, Color.FromArgb(argb)); }
        static int Argb(ColorPicker c) { return Color.FromArgb(255, c.Value).ToArgb(); }

        void LoadProfileToUi()
        {
            var p = Current;
            loading = true;
            foreach (var pg in pages.Take(4)) pg.Enabled = p != null;
            if (p != null)
            {
                bool anim = CursorRenderer.IsAnimated(p.Style);
                if ((segKind.SelectedIndex == 1) != anim) { segKind.SelectedIndex = anim ? 1 : 0; ShowStyleKind(anim); }
                galStyle.SelectedId = p.Style;
                slSize.Value = p.Size; slOpacity.Value = p.Opacity; slOutline.Value = p.Outline; slThick.Value = p.Thickness; slGap.Value = p.Gap;
                swShadow.Checked = p.Shadow;
                cpColor.Value = Col(p.Color); cpOutline.Value = Col(p.OutlineColor);
                tfImage.Box.Text = p.ImagePath; segHot.SelectedIndex = p.ImageHotspot;

                swTrail.Checked = p.Trail; galTrail.SelectedId = Math.Max(0, Math.Min(3, p.TrailStyle));
                slTrailLen.Value = p.TrailLength; slTrailW.Value = p.TrailWidth; cpTrail.Value = Col(p.TrailColor); swRainbow.Checked = p.TrailRainbow;

                swHalo.Checked = p.Halo; cpHalo.Value = Col(p.HaloColor); slHalo.Value = p.HaloSize; swPulse.Checked = p.HaloPulse;
                swGlow.Checked = p.Glow; cpGlow.Value = Col(p.GlowColor); slGlow.Value = p.GlowSize;
                swClick.Checked = p.ClickFx; cpClick.Value = Col(p.ClickColor);
                swCross.Checked = p.Crosshair; cpCross.Value = Col(p.CrossColor); slCrossT.Value = p.CrossThick; slCrossA.Value = p.CrossAlpha;

                tfName.Box.Text = p.Name; tfProc.Box.Text = p.Process;
                segMode.SelectedIndex = p.Mode; swAll.Checked = p.AllCursors; swHide.Checked = p.HideSystemCursor;
                UpdateEnables();
            }
            loading = false;
            galStyle.Invalidate();
            preview.Invalidate();
        }

        void UpdateEnables()
        {
            int st = galStyle.SelectedId;
            bool img = st == CursorRenderer.StyleImage;
            slThick.Enabled = CursorRenderer.UsesThickness(st);
            slGap.Enabled = CursorRenderer.UsesGap(st);
            slOutline.Enabled = cpColor.Enabled = cpOutline.Enabled = !img;
            tfImage.Enabled = btnImage.Enabled = segHot.Enabled = img;

            bool tr = swTrail.Checked, ghost = galTrail.SelectedId == 2, smoke = galTrail.SelectedId == 3;
            galTrail.Enabled = slTrailLen.Enabled = tr;
            slTrailW.Enabled = tr && !ghost;
            swRainbow.Enabled = tr && !ghost && !smoke;
            cpTrail.Enabled = tr && !ghost && !smoke && !swRainbow.Checked;

            cpHalo.Enabled = slHalo.Enabled = swPulse.Enabled = swHalo.Checked;
            cpGlow.Enabled = slGlow.Enabled = swGlow.Checked;
            cpClick.Enabled = swClick.Checked;
            cpCross.Enabled = slCrossT.Enabled = slCrossA.Enabled = swCross.Checked;

            swHide.Enabled = segMode.SelectedIndex == 1;
            swAll.Enabled = segMode.SelectedIndex == 0;
            lblModeDesc.Text = segMode.SelectedIndex == 0
                ? "윈도우 커서 자체를 바꿔요. 바탕화면과 대부분의 게임·프로그램에서 동작하고 가장 가볍습니다."
                : "커서를 화면 위에 덧그려요. 게임이 커서를 직접 그려서 안 바뀔 때 쓰세요. (창 모드/전체 창 모드 필요)";
        }

        void OnEdit()
        {
            if (loading) return;
            var p = Current;
            if (p == null) return;
            string oldName = p.Name;
            string nm = tfName.Box.Text.Trim();
            p.Name = nm.Length > 0 ? nm : oldName;
            p.Process = tfProc.Box.Text.Trim();
            p.Mode = segMode.SelectedIndex;
            if (galStyle.SelectedId >= 0) p.Style = galStyle.SelectedId;
            p.Size = slSize.Value; p.Opacity = slOpacity.Value; p.Outline = slOutline.Value; p.Thickness = slThick.Value; p.Gap = slGap.Value;
            p.Shadow = swShadow.Checked;
            p.Color = Argb(cpColor); p.OutlineColor = Argb(cpOutline);
            p.ImagePath = tfImage.Box.Text.Trim(); p.ImageHotspot = segHot.SelectedIndex;
            p.AllCursors = swAll.Checked; p.HideSystemCursor = swHide.Checked;

            p.Trail = swTrail.Checked; if (galTrail.SelectedId >= 0) p.TrailStyle = galTrail.SelectedId;
            p.TrailLength = slTrailLen.Value; p.TrailWidth = slTrailW.Value; p.TrailColor = Argb(cpTrail); p.TrailRainbow = swRainbow.Checked;

            p.Halo = swHalo.Checked; p.HaloColor = Argb(cpHalo); p.HaloSize = slHalo.Value; p.HaloPulse = swPulse.Checked;
            p.Glow = swGlow.Checked; p.GlowColor = Argb(cpGlow); p.GlowSize = slGlow.Value;
            p.ClickFx = swClick.Checked; p.ClickColor = Argb(cpClick);
            p.Crosshair = swCross.Checked; p.CrossColor = Argb(cpCross); p.CrossThick = slCrossT.Value; p.CrossAlpha = slCrossA.Value;

            if (oldName != p.Name && S.DefaultProfile == oldName) S.DefaultProfile = p.Name;
            loading = true;
            UpdateEnables();
            loading = false;
            if (oldName != p.Name) { lst.Items[lst.SelectedIndex] = p.Name; RefreshDefaultCombo(); }
            version++; dirty = true;
            lst.Invalidate();
            galStyle.Invalidate();
            preview.Invalidate();
        }

        void BrowseImage()
        {
            using (var d = new OpenFileDialog { Filter = "커서/이미지|*.png;*.ico;*.cur;*.ani;*.bmp;*.gif;*.jpg;*.jpeg|모든 파일|*.*" })
            {
                if (d.ShowDialog(this) != DialogResult.OK) return;
                tfImage.Box.Text = d.FileName;
                string ext = Path.GetExtension(d.FileName).ToLowerInvariant();
                if (ext == ".cur" || ext == ".ani") segHot.SelectedIndex = 2;
            }
        }

        void ShowProcessPicker()
        {
            var menu = new ContextMenuStrip();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var pr in Process.GetProcesses().OrderBy(x => x.ProcessName))
            {
                try
                {
                    if (pr.Id == ownPid || pr.MainWindowHandle == IntPtr.Zero || string.IsNullOrEmpty(pr.MainWindowTitle)) continue;
                    if (!seen.Add(pr.ProcessName)) continue;
                    string name = pr.ProcessName + ".exe";
                    string t = pr.MainWindowTitle.Length > 40 ? pr.MainWindowTitle.Substring(0, 40) + "…" : pr.MainWindowTitle;
                    menu.Items.Add(name + "   —   " + t, null, delegate
                    {
                        var cur = tfProc.Box.Text.Trim();
                        tfProc.Box.Text = cur.Length == 0 ? name : cur + ", " + name;
                    });
                }
                catch { }
            }
            if (menu.Items.Count == 0) menu.Items.Add("(창이 있는 프로그램이 없습니다)").Enabled = false;
            menu.Show(btnPick, new Point(0, btnPick.Height));
        }

        // ───── 미리보기 ─────
        void PaintPreview(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            int w = preview.Width, h = preview.Height;
            g.Clear(Theme.Card);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var clip = Theme.Round(new RectangleF(0, 0, w - 1, h - 1), 12))
            {
                g.SetClip(clip);
                int mode = segBg.SelectedIndex;
                if (mode == 0)
                {
                    using (var a = new SolidBrush(Color.FromArgb(42, 43, 51))) using (var b = new SolidBrush(Color.FromArgb(52, 53, 62)))
                        for (int y = 0; y < h; y += 12) for (int x = 0; x < w; x += 12) g.FillRectangle(((x + y) / 12) % 2 == 0 ? a : b, x, y, 12, 12);
                }
                else using (var br = new SolidBrush(mode == 1 ? Color.FromArgb(238, 240, 244) : Color.FromArgb(12, 13, 16))) g.FillRectangle(br, 0, 0, w, h);

                var p = Current;
                if (p != null)
                {
                    float sc = segScale.SelectedIndex == 1 ? 2f : 1f;
                    float cx = w / 2f + 10, cy = h / 2f + 6;
                    if (p.Crosshair)
                        using (var pen = new Pen(Color.FromArgb(Math.Max(30, p.CrossAlpha * 255 / 100), Col(p.CrossColor)), p.CrossThick))
                        { g.DrawLine(pen, 0, cy, w, cy); g.DrawLine(pen, cx, 0, cx, h); }
                    if (p.Glow)
                    {
                        float r = p.GlowSize / 2f * sc;
                        using (var gp = new GraphicsPath())
                        {
                            gp.AddEllipse(cx - r, cy - r, r * 2, r * 2);
                            using (var pg = new PathGradientBrush(gp) { CenterPoint = new PointF(cx, cy), CenterColor = Color.FromArgb(170, Col(p.GlowColor)), SurroundColors = new[] { Color.FromArgb(0, Col(p.GlowColor)) } })
                                g.FillPath(pg, gp);
                        }
                    }
                    if (p.Trail) DrawSampleTrail(g, p, cx, cy, sc);
                    if (p.Halo)
                    {
                        float r = p.HaloSize / 2f * sc, pw = Math.Max(2f, p.HaloSize / 14f) * sc;
                        using (var pen = new Pen(Color.FromArgb(90, 0, 0, 0), pw + 2)) g.DrawEllipse(pen, cx - r, cy - r, r * 2, r * 2);
                        using (var pen = new Pen(Col(p.HaloColor), pw)) g.DrawEllipse(pen, cx - r, cy - r, r * 2, r * 2);
                    }
                    if (p.ClickFx)
                        using (var pen = new Pen(Color.FromArgb(120, Col(p.ClickColor)), 2)) g.DrawEllipse(pen, cx - 26 * sc, cy - 26 * sc, 52 * sc, 52 * sc);
                    try
                    {
                        Point hot;
                        float phase = CursorRenderer.IsAnimated(p.Style) ? CursorRenderer.PhaseNow(p.Style) : 0.2f;
                        using (var bmp = CursorRenderer.RenderAt(p, phase, out hot))
                        {
                            g.InterpolationMode = sc > 1 ? InterpolationMode.NearestNeighbor : InterpolationMode.HighQualityBicubic;
                            g.PixelOffsetMode = PixelOffsetMode.Half;
                            g.DrawImage(bmp, cx - hot.X * sc, cy - hot.Y * sc, bmp.Width * sc, bmp.Height * sc);
                            g.PixelOffsetMode = PixelOffsetMode.Default;
                        }
                    }
                    catch (Exception ex)
                    {
                        TextRenderer.DrawText(g, ex.Message, Theme.Small, new Rectangle(10, 10, w - 20, h - 20), Theme.Danger, TextFormatFlags.WordBreak);
                    }
                }
                g.ResetClip();
            }
        }

        void DrawSampleTrail(Graphics g, Profile p, float cx, float cy, float sc)
        {
            var pts = new List<PointF>();
            for (int i = 0; i <= 24; i++)
            {
                float t = i / 24f;
                pts.Add(new PointF(cx - 95 * (1 - t), cy + 70 * (1 - t) - (float)Math.Sin(t * Math.PI) * 30));
            }
            Color c = Col(p.TrailColor);
            int st = p.TrailStyle;
            if (st == 3)
            {
                var rnd = new Random(5);
                for (int i = 0; i < 14; i++)
                {
                    float t = i / 13f;
                    float x = cx + (float)Math.Sin(t * 5) * 8 * t, y = cy - 4 - t * 90, r = (3 + t * 12) * (p.TrailWidth / 8f + 0.4f);
                    using (var gp = new GraphicsPath())
                    {
                        gp.AddEllipse(x - r, y - r, r * 2, r * 2);
                        using (var br = new PathGradientBrush(gp) { CenterColor = Color.FromArgb((int)(210 * (1 - t * 0.75f)), 140, 142, 155), SurroundColors = new[] { Color.FromArgb(0, 140, 142, 155) } })
                            g.FillPath(br, gp);
                    }
                }
                return;
            }
            if (st == 2)
            {
                try
                {
                    Point hot;
                    using (var bmp = CursorRenderer.Render(p, out hot))
                    using (var ia = new ImageAttributes())
                        for (int i = 6; i < 24; i += 6)
                        {
                            var cm = new ColorMatrix(); cm.Matrix33 = 0.15f + 0.45f * i / 24f; ia.SetColorMatrix(cm);
                            g.DrawImage(bmp, new Rectangle((int)(pts[i].X - hot.X * sc), (int)(pts[i].Y - hot.Y * sc), (int)(bmp.Width * sc), (int)(bmp.Height * sc)), 0, 0, bmp.Width, bmp.Height, GraphicsUnit.Pixel, ia);
                        }
                }
                catch { }
                return;
            }
            for (int i = 1; i < pts.Count; i++)
            {
                float a = i / 24f;
                Color col = p.TrailRainbow ? Rainbow(i * 15) : c;
                col = Color.FromArgb((int)(30 + 200 * a), col);
                if (st == 0)
                    using (var pen = new Pen(col, Math.Max(1f, p.TrailWidth * a * sc)) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                        g.DrawLine(pen, pts[i - 1], pts[i]);
                else if (i % 2 == 0)
                {
                    float r = Math.Max(1f, p.TrailWidth / 2f * a * sc);
                    using (var br = new SolidBrush(col)) g.FillEllipse(br, pts[i].X - r, pts[i].Y - r, r * 2, r * 2);
                }
            }
        }

        static Color Rainbow(float h)
        {
            h = h % 360; float x = 1 - Math.Abs((h / 60f) % 2 - 1);
            float r, g, b;
            if (h < 60) { r = 1; g = x; b = 0; } else if (h < 120) { r = x; g = 1; b = 0; } else if (h < 180) { r = 0; g = 1; b = x; }
            else if (h < 240) { r = 0; g = x; b = 1; } else if (h < 300) { r = x; g = 0; b = 1; } else { r = 1; g = 0; b = x; }
            return Color.FromArgb((int)(r * 255), (int)(g * 255), (int)(b * 255));
        }

        // ───── 적용 엔진 ─────
        string ForegroundProcess(out bool own, out IntPtr fg)
        {
            own = false;
            fg = Native.GetForegroundWindow();
            try
            {
                uint pid;
                Native.GetWindowThreadProcessId(fg, out pid);
                if (pid == 0) return null;
                if (pid == ownPid) { own = true; return null; }
                using (var pr = Process.GetProcessById((int)pid)) return pr.ProcessName;
            }
            catch { return null; }
        }

        void Tick()
        {
            Profile target = null;
            string reason = "꺼짐";
            bool own; IntPtr fg;
            string pn = ForegroundProcess(out own, out fg);
            if (S.Enabled)
            {
                if (pinned != null) { target = pinned; reason = "빠른 선택 중"; }
                else if (own && Visible) { target = Current; reason = "편집 중 미리보기"; }
                else
                {
                    if (S.AutoSwitch && pn != null)
                    {
                        target = S.Profiles.FirstOrDefault(p => p.Matches(pn));
                        if (target != null) reason = "게임 감지 · " + pn;
                    }
                    if (target == null)
                    {
                        target = S.Profiles.FirstOrDefault(p => p.Name == S.DefaultProfile);
                        reason = "평소 커서";
                    }
                }
            }
            if (!appliedOnce || target != applied || (target != null && version != appliedVersion))
                Apply(target);
            reasonText = reason;

            // 창 안에 가두기
            if (lockOn)
            {
                bool release = own || fg == IntPtr.Zero;
                if (!release)
                {
                    var sb = new StringBuilder(64);
                    Native.GetClassName(fg, sb, 64);
                    string cls = sb.ToString();
                    release = cls == "Progman" || cls == "WorkerW" || cls == "Shell_TrayWnd";
                }
                if (release) Native.ClipCursorFree(IntPtr.Zero);
                else
                {
                    Native.RECT cr; Native.GetClientRect(fg, out cr);
                    var tl = new Native.POINT(0, 0); Native.ClientToScreen(fg, ref tl);
                    var rc = new Native.RECT { Left = tl.X, Top = tl.Y, Right = tl.X + cr.Right, Bottom = tl.Y + cr.Bottom };
                    if (cr.Right > 50 && cr.Bottom > 50) Native.ClipCursor(ref rc);
                }
            }

            // 상태 표시
            string now = target == null ? "윈도우 기본 커서" : target.Name;
            if (lblNow.Text != now) lblNow.Text = now;
            string rs = status.Length > 0 ? status : (target == null ? (S.Enabled ? "평소 커서가 지정되지 않음" : "커스텀 커서 꺼짐") : reason);
            if (lblReason.Text != rs) { lblReason.Text = rs; lblReason.ForeColor = status.Length > 0 ? Theme.Danger : Theme.Sub; }
            string hs = S.Enabled ? "● 작동 중" : "○ 꺼짐";
            if (lblHeaderStatus.Text != hs) { lblHeaderStatus.Text = hs; lblHeaderStatus.ForeColor = S.Enabled ? Theme.Good : Theme.Sub; }
            SetChip(chipZoom, zoomOn); SetChip(chipLock, lockOn); SetChip(chipSpeed, speedOn);
            if (dirty) { dirty = false; S.Save(); }
        }

        static void SetChip(Chip c, bool on) { if (c.On != on) { c.On = on; c.Invalidate(); } }

        void Apply(Profile t)
        {
            appliedOnce = true; applied = t; appliedVersion = version; status = "";
            overlay.Stop();
            CursorEngine.Restore();
            FxHost.Configure(BuildFx(t));
            lst.Invalidate();
            if (t == null) return;
            try
            {
                if (t.Mode == 0) CursorEngine.ApplySystem(t);
                else
                {
                    Point hot;
                    if (CursorRenderer.IsAnimated(t.Style))
                        overlay.SetFrames(CursorRenderer.RenderFrames(t, out hot), hot, CursorRenderer.Jiffies(t.Style) * 1000 / 60);
                    else
                        overlay.SetFrames(new[] { CursorRenderer.Render(t, out hot) }, hot, 0);
                    overlay.Start();
                    if (t.HideSystemCursor) CursorEngine.ApplyBlank();
                }
            }
            catch (Exception ex)
            {
                overlay.Stop();
                CursorEngine.Restore();
                status = "오류: " + ex.Message;
            }
        }

        FxConfig BuildFx(Profile t)
        {
            var c = new FxConfig { Zoom = zoomOn, ZoomFactor = S.ZoomFactor, ZoomSize = S.ZoomSize };
            if (t != null)
            {
                c.Trail = t.Trail; c.TrailStyle = t.TrailStyle; c.TrailLength = t.TrailLength; c.TrailWidth = t.TrailWidth; c.TrailColor = t.TrailColor; c.TrailRainbow = t.TrailRainbow;
                c.Halo = t.Halo; c.HaloColor = t.HaloColor; c.HaloSize = t.HaloSize; c.HaloPulse = t.HaloPulse;
                c.Glow = t.Glow; c.GlowColor = t.GlowColor; c.GlowSize = t.GlowSize;
                c.Click = t.ClickFx; c.ClickColor = t.ClickColor;
                c.Cross = t.Crosshair; c.CrossColor = t.CrossColor; c.CrossThick = t.CrossThick; c.CrossAlpha = t.CrossAlpha;
                if (c.Trail && c.TrailStyle == 2)
                {
                    try { Point hot; c.CursorBmp = CursorRenderer.Render(t, out hot); c.Hot = hot; }
                    catch { c.TrailStyle = 0; }
                }
            }
            return c;
        }

        void ApplyFx() { FxHost.Configure(BuildFx(applied)); }

        // ───── 단축키 기능 ─────
        void ToggleZoom()
        {
            zoomOn = !zoomOn;
            loading = true; swZoom.Checked = zoomOn; loading = false;
            ApplyFx();
        }

        void ToggleLock()
        {
            lockOn = !lockOn;
            if (!lockOn) Native.ClipCursorFree(IntPtr.Zero);
            loading = true; swLock.Checked = lockOn; loading = false;
            tray.ShowBalloonTip(700, "MyCursor", lockOn ? "커서를 창 안에 가뒀어요" : "커서 가두기 해제", ToolTipIcon.None);
        }

        void ToggleSpeed()
        {
            speedOn = !speedOn;
            Native.SetMouseSpeed(speedOn ? S.AltSpeed : originalSpeed);
            loading = true; swSpeed.Checked = speedOn; loading = false;
            tray.ShowBalloonTip(700, "MyCursor", speedOn ? "감도 " + S.AltSpeed + " 로 전환" : "원래 감도로 복귀", ToolTipIcon.None);
        }

        void OpenPicker()
        {
            Profile target = applied ?? Current ?? S.Profiles.FirstOrDefault();
            if (target == null) return;
            pinned = target;
            var qp = new QuickPicker(target.Name, id => StyleThumb(target, id), target.Style, Math.Min(128, target.Size));
            qp.Gallery.SelectedChanged += delegate
            {
                target.Style = qp.Gallery.SelectedId; version++; dirty = true;
                if (target == Current) LoadProfileToUi();
            };
            qp.SizeSlider.ValueChanged += delegate
            {
                target.Size = qp.SizeSlider.Value; version++; dirty = true;
                if (target == Current) LoadProfileToUi();
            };
            qp.OpenSettings.Click += delegate
            {
                int i = S.Profiles.IndexOf(target);
                qp.Close();
                ShowMain();
                if (i >= 0) lst.SelectedIndex = i;
            };
            qp.FormClosed += delegate { pinned = null; };
            Native.POINT cur; Native.GetCursorPos(out cur);
            var wa = Screen.FromPoint(new Point(cur.X, cur.Y)).WorkingArea;
            qp.Location = new Point(Math.Max(wa.Left, Math.Min(cur.X - 40, wa.Right - qp.Width)), Math.Max(wa.Top, Math.Min(cur.Y - 40, wa.Bottom - qp.Height)));
            qp.Show();
            qp.Activate();
        }

        void NextDefault()
        {
            if (S.Profiles.Count == 0) return;
            int i = S.Profiles.FindIndex(p => p.Name == S.DefaultProfile);
            i = (i + 1) % S.Profiles.Count;
            S.DefaultProfile = S.Profiles[i].Name;
            if (!S.Enabled) swEnabled.Checked = true;
            dirty = true;
            RefreshDefaultCombo();
            lst.Invalidate();
            tray.ShowBalloonTip(1000, "MyCursor", "평소 커서: " + S.DefaultProfile, ToolTipIcon.None);
        }

        // ───── 시작 프로그램 ─────
        bool IsStartupEnabled()
        {
            try { using (var k = Registry.CurrentUser.OpenSubKey(RunKey)) return k != null && k.GetValue(AppName) != null; }
            catch { return false; }
        }

        void SetStartup(bool on)
        {
            try
            {
                using (var k = Registry.CurrentUser.OpenSubKey(RunKey, true))
                {
                    if (on) k.SetValue(AppName, "\"" + Application.ExecutablePath + "\" --tray");
                    else k.DeleteValue(AppName, false);
                }
            }
            catch (Exception ex) { MessageBox.Show(this, "설정 실패: " + ex.Message); }
        }

        // ───── 창 / 트레이 / 단축키 ─────
        protected override void SetVisibleCore(bool value)
        {
            if (startHidden) { startHidden = false; if (!IsHandleCreated) CreateHandle(); value = false; }
            base.SetVisibleCore(value);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            try { int dark = 1; Native.DwmSetWindowAttribute(Handle, 20, ref dark, 4); } catch { }
            const uint MOD_ALT = 1, MOD_CONTROL = 2, MOD_NOREPEAT = 0x4000;
            uint m = MOD_CONTROL | MOD_ALT | MOD_NOREPEAT;
            Native.RegisterHotKey(Handle, HK_TOGGLE, m, (uint)Keys.F9);
            Native.RegisterHotKey(Handle, HK_NEXT, m, (uint)Keys.F10);
            Native.RegisterHotKey(Handle, HK_PICK, m, (uint)Keys.C);
            Native.RegisterHotKey(Handle, HK_ZOOM, m, (uint)Keys.Z);
            Native.RegisterHotKey(Handle, HK_LOCK, m, (uint)Keys.L);
            Native.RegisterHotKey(Handle, HK_SPEED, m, (uint)Keys.S);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x0312) // WM_HOTKEY
            {
                switch (m.WParam.ToInt32())
                {
                    case HK_TOGGLE:
                        swEnabled.Checked = !swEnabled.Checked;
                        tray.ShowBalloonTip(800, "MyCursor", swEnabled.Checked ? "커스텀 커서 켜짐" : "커스텀 커서 꺼짐", ToolTipIcon.None);
                        break;
                    case HK_NEXT: NextDefault(); break;
                    case HK_PICK: OpenPicker(); break;
                    case HK_ZOOM: ToggleZoom(); break;
                    case HK_LOCK: ToggleLock(); break;
                    case HK_SPEED: ToggleSpeed(); break;
                }
            }
            base.WndProc(ref m);
        }

        void ShowMain()
        {
            Show();
            if (WindowState == FormWindowState.Minimized) WindowState = FormWindowState.Normal;
            Activate();
        }

        void ExitApp()
        {
            reallyExit = true;
            Close();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!reallyExit && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
                if (!S.TrayTipShown)
                {
                    S.TrayTipShown = true; dirty = true;
                    tray.ShowBalloonTip(3000, "MyCursor", "트레이에서 계속 실행 중입니다. 완전히 끄려면 트레이 아이콘 → 종료", ToolTipIcon.Info);
                }
                return;
            }
            timer.Stop();
            overlay.Stop();
            FxHost.Shutdown();
            CursorEngine.Restore();
            Native.ClipCursorFree(IntPtr.Zero);
            if (speedOn) Native.SetMouseSpeed(originalSpeed);
            for (int id = 1; id <= 6; id++) Native.UnregisterHotKey(Handle, id);
            S.Save();
            tray.Visible = false;
            base.OnFormClosing(e);
        }
    }

    static class Program
    {
        // 커서 모양 아이콘(.ico) 생성: MyCursor.exe --make-icon app.ico
        static void MakeIcon(string path)
        {
            int[] sizes = { 16, 24, 32, 48, 64, 128, 256 };
            var pngs = new List<byte[]>();
            foreach (int s in sizes)
            {
                Point hot;
                using (var arrow = CursorRenderer.Render(new Profile { Size = (int)(s * 0.86), Outline = Math.Max(1, s / 20), Shadow = false }, out hot))
                using (var canvas = new Bitmap(s, s, PixelFormat.Format32bppArgb))
                using (var ms = new MemoryStream())
                {
                    using (var g = Graphics.FromImage(canvas))
                    {
                        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        float sc = Math.Min(1f, Math.Min((float)s / arrow.Width, (float)s / arrow.Height));
                        int w = (int)(arrow.Width * sc), h = (int)(arrow.Height * sc);
                        g.DrawImage(arrow, (s - w) / 2, (s - h) / 2, w, h);
                    }
                    canvas.Save(ms, ImageFormat.Png);
                    pngs.Add(ms.ToArray());
                }
            }
            using (var bw = new BinaryWriter(File.Create(path)))
            {
                bw.Write((short)0); bw.Write((short)1); bw.Write((short)sizes.Length);
                int offset = 6 + 16 * sizes.Length;
                for (int i = 0; i < sizes.Length; i++)
                {
                    bw.Write((byte)(sizes[i] >= 256 ? 0 : sizes[i])); bw.Write((byte)(sizes[i] >= 256 ? 0 : sizes[i]));
                    bw.Write((byte)0); bw.Write((byte)0); bw.Write((short)1); bw.Write((short)32);
                    bw.Write(pngs[i].Length); bw.Write(offset);
                    offset += pngs[i].Length;
                }
                foreach (var d in pngs) bw.Write(d);
            }
        }

        static void EmergencyRestore()
        {
            CursorEngine.Restore();
            Native.ClipCursorFree(IntPtr.Zero);
            if (MainForm.OriginalSpeedForCrash > 0) Native.SetMouseSpeed(MainForm.OriginalSpeedForCrash);
        }

        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length == 2 && args[0] == "--make-icon") { MakeIcon(args[1]); return; }
            bool created;
            using (var mutex = new Mutex(true, "MyCursor_SingleInstance_5b1e", out created))
            {
                if (!created)
                {
                    MessageBox.Show("MyCursor가 이미 실행 중입니다. 작업 표시줄 트레이 아이콘을 확인하세요.", "MyCursor");
                    return;
                }
                try { Native.SetProcessDPIAware(); } catch { }
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.ThreadException += (s, e) => { EmergencyRestore(); MessageBox.Show(e.Exception.Message, "MyCursor 오류"); };
                AppDomain.CurrentDomain.UnhandledException += (s, e) => EmergencyRestore();

                CursorEngine.Restore(); // 이전 실행이 비정상 종료됐을 때를 대비
                Application.Run(new MainForm(args.Any(a => a == "--tray")));
                EmergencyRestore();
            }
        }
    }
}
