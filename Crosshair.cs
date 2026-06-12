using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PubgCrosshair
{
    public class OverlayForm : Form
    {
        private bool showDots = false;
        private bool showMapName = false;
        private int currentMap = 0;
        private Timer mapNameTimer;
        private NotifyIcon trayIcon;
        private ContextMenuStrip trayMenu;
        private bool[] mapEnabled;
        private ToolStripMenuItem[] mapMenuItems;

        // ===== 地图数据 =====
        private class MapData
        {
            public string Name;      // 中文名
            public float ImgW, ImgH; // 图片像素尺寸
            public int[,] Dots;      // 像素坐标 [i,0]=x [i,1]=y
            public byte[] DotTypes;  // null=全部红色, 0=红(密室), 1=紫(撬棍房), 2=绿(熊洞)
        }

        private MapData[] maps = new MapData[] {
            new MapData {
                Name = "艾伦格", ImgW = 1026, ImgH = 1022,
                Dots = new int[,] {
                    { 643, 84 }, { 173, 229 }, { 518, 248 }, { 820, 261 },
                    { 326, 278 }, { 686, 431 }, { 186, 445 }, { 378, 472 },
                    { 585, 556 }, { 848, 616 }, { 344, 648 }, { 158, 695 },
                    { 553, 744 }, { 415, 843 }, { 712, 846 },
                }
            },
            new MapData {
                Name = "米拉玛", ImgW = 1028, ImgH = 1026,
                Dots = new int[,] {
                    { 410, 136 }, { 581, 181 }, { 226, 210 }, { 792, 244 },
                    { 353, 315 }, { 646, 402 }, { 177, 415 }, { 484, 490 },
                    { 778, 535 }, { 337, 626 }, { 555, 654 }, { 166, 668 },
                    { 659, 791 }, { 408, 837 }, { 177, 910 },
                }
            },
            new MapData {
                Name = "泰戈", ImgW = 1027, ImgH = 1026,
                Dots = new int[,] {
                    { 176, 150 }, { 324, 170 }, { 608, 217 }, { 450, 249 },
                    { 869, 262 }, { 159, 340 }, { 893, 424 }, { 129, 429 },
                    { 760, 487 }, { 557, 625 }, { 123, 659 }, { 806, 700 },
                    { 622, 807 }, { 305, 811 }, { 800, 904 },
                }
            },
            new MapData {
                Name = "维寒迪", ImgW = 1025, ImgH = 1021,
                Dots = new int[,] {
                    {387,161}, {682,166}, {359,171}, {178,175}, {663,176},
                    {347,199}, {763,219}, {456,274}, {788,310}, {656,331},
                    {405,367}, {719,368}, {240,387}, {518,404}, {569,447},
                    {593,449}, {670,460}, {176,483}, {836,487}, {862,488},
                    {224,561}, {859,582}, {128,619}, {761,621}, {593,623},
                    {368,666}, {505,693}, {301,708}, {829,735}, {765,739},
                    {785,740}, {660,742}, {402,756}, {478,765}, {667,779},
                    {475,817}, {498,822}, {542,844}, {452,873},
                },
                DotTypes = new byte[] {
                    1,0,2,1,2,
                    0,2,1,0,1,
                    1,2,1,0,1,
                    2,1,0,2,0,
                    1,1,1,1,0,
                    1,1,0,1,0,
                    2,2,1,2,1,
                    2,0,1,1,
                }
            },
            new MapData {
                Name = "帕拉莫", ImgW = 1024, ImgH = 1023,
                Dots = new int[,] {
                    { 407, 324 }, { 284, 366 }, { 639, 502 }, { 837, 587 },
                    { 619, 616 }, { 444, 646 }, { 132, 650 }, { 530, 823 },
                }
            },
            new MapData {
                Name = "帝斯顿", ImgW = 1027, ImgH = 1025,
                Dots = new int[,] {
                    {349,68}, {653,113}, {237,181}, {246,228}, {843,237},
                    {639,248}, {394,340}, {569,345}, {924,397}, {218,427},
                    {413,453}, {574,514}, {778,529}, {829,532}, {480,572},
                    {207,573}, {847,573}, {804,587}, {290,592}, {758,595},
                    {833,622}, {475,716}, {145,792}, {717,807},
                }
            },
            new MapData {
                Name = "荣都", ImgW = 1025, ImgH = 1026,
                Dots = new int[,] {
                    { 731, 116 }, { 383, 119 }, { 186, 173 }, { 634, 261 },
                    { 477, 336 }, { 887, 347 }, { 180, 410 }, { 834, 540 },
                    { 588, 555 }, { 183, 605 }, { 380, 649 }, { 717, 767 },
                    { 159, 820 }, { 427, 882 }, { 622, 930 },
                }
            },
        };

        // ===== 低层键盘钩子 =====
        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll")]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;

        private IntPtr hookId = IntPtr.Zero;
        private LowLevelKeyboardProc hookProc;

        // ===== 分层窗口透明度控制 =====
        [DllImport("user32.dll")]
        private static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);

        private const uint LWA_ALPHA = 0x2;
        private const uint LWA_COLORKEY = 0x1;

        private const uint VK_OEM_3 = 0xC0;
        private const uint VK_F2 = 0x71;
        private const uint VK_LEFT = 0x25;
        private const uint VK_RIGHT = 0x27;

        public OverlayForm()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.TopMost = true;
            this.ShowInTaskbar = false;
            this.StartPosition = FormStartPosition.Manual;

            Rectangle screen = Screen.PrimaryScreen.Bounds;
            this.Bounds = screen;
            this.Location = new Point(0, 0);

            this.BackColor = Color.Black;
            this.TransparencyKey = Color.Black;
            this.AllowTransparency = true;
            this.DoubleBuffered = true;

            SetupTrayIcon();

            mapNameTimer = new Timer();
            mapNameTimer.Interval = 1000;
            mapNameTimer.Tick += (s, e) => {
                showMapName = false;
                mapNameTimer.Stop();
                this.Invalidate();
            };

            InstallHook();

            SetLayeredWindowAttributes(this.Handle, 0, 0, LWA_ALPHA | LWA_COLORKEY);
            showDots = false;
        }

        // ============================================================
        // 低层键盘钩子
        // ============================================================
        private void InstallHook()
        {
            hookProc = HookCallback;
            using (Process p = Process.GetCurrentProcess())
            using (ProcessModule m = p.MainModule)
            {
                hookId = SetWindowsHookEx(WH_KEYBOARD_LL, hookProc,
                    GetModuleHandle(m.ModuleName), 0);
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(lParam);

                if (vkCode == VK_OEM_3 || vkCode == VK_F2)
                {
                    this.Toggle();
                    return (IntPtr)1;
                }

                if (vkCode == VK_LEFT)
                {
                    this.SwitchMap(-1);
                    return (IntPtr)1;
                }

                if (vkCode == VK_RIGHT)
                {
                    this.SwitchMap(1);
                    return (IntPtr)1;
                }
            }
            return CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
        }

        // ============================================================
        // 切换地图
        // ============================================================
        private void SwitchMap(int delta)
        {
            int newMap = currentMap;
            do
            {
                newMap = (newMap + delta + maps.Length) % maps.Length;
            } while (!mapEnabled[newMap] && newMap != currentMap);

            if (newMap == currentMap) return; // 没有可切换的启用地图
            currentMap = newMap;

            // 如果当前是显示状态，刷新画面并显示地图名
            if (showDots)
            {
                showMapName = true;
                mapNameTimer.Stop();
                mapNameTimer.Start();
                trayIcon.Text = maps[currentMap].Name + " - 已显示";
                this.Invalidate();
            }
        }

        // ============================================================
        // 托盘图标
        // ============================================================
        private void SetupTrayIcon()
        {
            trayIcon = new NotifyIcon();
            trayMenu = new ContextMenuStrip();

            // 地图池子菜单
            mapEnabled = new bool[maps.Length];
            mapMenuItems = new ToolStripMenuItem[maps.Length];
            ToolStripMenuItem mapPoolItem = new ToolStripMenuItem("地图池");
            for (int i = 0; i < maps.Length; i++)
            {
                mapEnabled[i] = true;
                int idx = i;
                mapMenuItems[i] = new ToolStripMenuItem(maps[i].Name);
                mapMenuItems[i].Checked = true;
                mapMenuItems[i].Click += (s, e) => {
                    mapEnabled[idx] = !mapEnabled[idx];
                    mapMenuItems[idx].Checked = mapEnabled[idx];
                    // 当前显示的地图被取消勾选时自动跳转
                    if (showDots && idx == currentMap && !mapEnabled[idx])
                    {
                        int next = currentMap;
                        do
                        {
                            next = (next + 1) % maps.Length;
                        } while (!mapEnabled[next] && next != currentMap);
                        if (next != currentMap)
                        {
                            currentMap = next;
                            showMapName = true;
                            mapNameTimer.Stop();
                            mapNameTimer.Start();
                            trayIcon.Text = maps[currentMap].Name + " - 已显示";
                            this.Invalidate();
                        }
                    }
                };
                mapPoolItem.DropDownItems.Add(mapMenuItems[i]);
            }
            // 点击地图池内的选项不关闭子菜单，方便连续勾选
            mapPoolItem.DropDown.Closing += (s, e) => {
                if (e.CloseReason == ToolStripDropDownCloseReason.ItemClicked)
                    e.Cancel = true;
            };
            trayMenu.Items.Add(mapPoolItem);

            trayMenu.Items.Add("显示标记", null, (s, e) => { this.Toggle(); });
            trayMenu.Items.Add("退出", null, (s, e) => { Application.Exit(); });

            using (Bitmap bmp = new Bitmap(16, 16))
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.Clear(Color.Transparent);
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
                    using (SolidBrush brush = new SolidBrush(Color.FromArgb(255, 255, 60, 60)))
                    {
                        g.FillEllipse(brush, 3, 3, 10, 10);
                    }
                }
                trayIcon.Icon = Icon.FromHandle(bmp.GetHicon());
            }

            trayIcon.Text = "PUBG 标记 - 已隐藏 (按 ` 显示)";
            trayIcon.Visible = true;

            // 右键菜单（原生 NotifyIcon 弹出，自动消失）
            trayMenu.Opening += (s, e) => {
                trayMenu.Items[1].Text = showDots ? "隐藏标记" : "显示标记";
            };
            trayIcon.ContextMenuStrip = trayMenu;
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x80000 | 0x20;
                cp.ExStyle |= 0x80;
                return cp;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Graphics g = e.Graphics;
            int sw = this.ClientSize.Width;
            int sh = this.ClientSize.Height;

            MapData map = maps[currentMap];

            // 计算图片在屏幕上的显示区域（居中、等比缩放）
            float scale = Math.Min(sw / map.ImgW, sh / map.ImgH);
            float offX = (sw - map.ImgW * scale) / 2f;
            float offY = (sh - map.ImgH * scale) / 2f;

            // 画标记点
            if (showDots)
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
                Color[] dotColors = new Color[] {
                    Color.FromArgb(255, 255, 40, 40),   // 0:红 - 密室
                    Color.FromArgb(255, 160, 40, 200),  // 1:紫 - 撬棍房
                    Color.FromArgb(255, 40, 200, 40),   // 2:绿 - 熊洞
                };
                for (int i = 0; i < map.Dots.GetLength(0); i++)
                {
                    int type = (map.DotTypes != null && i < map.DotTypes.Length) ? map.DotTypes[i] : 0;
                    if (type < 0 || type >= dotColors.Length) type = 0;
                    using (SolidBrush brush = new SolidBrush(dotColors[type]))
                    {
                        float px = offX + map.Dots[i, 0] * scale;
                        float py = offY + map.Dots[i, 1] * scale;
                        g.FillEllipse(brush, px - 4, py - 4, 8, 8);
                    }
                }
            }

            // 画地图名称提示
            if (showMapName)
            {
                using (Font font = new Font("Microsoft YaHei", 28, FontStyle.Bold))
                {
                    SizeF textSize = g.MeasureString(map.Name, font);
                    float bw = textSize.Width + 60;
                    float bh = textSize.Height + 30;
                    float bx = (sw - bw) / 2;
                    float by = (sh - bh) / 2;

                    using (SolidBrush bgBrush = new SolidBrush(Color.FromArgb(40, 40, 40)))
                    {
                        g.FillRectangle(bgBrush, bx, by, bw, bh);
                    }
                    using (Pen borderPen = new Pen(Color.FromArgb(80, 80, 80), 1))
                    {
                        g.DrawRectangle(borderPen, bx, by, bw, bh);
                    }
                    using (SolidBrush textBrush = new SolidBrush(Color.FromArgb(200, 200, 200)))
                    {
                        g.TextRenderingHint = TextRenderingHint.AntiAlias;
                        g.DrawString(map.Name, font, textBrush,
                            bx + (bw - textSize.Width) / 2,
                            by + (bh - textSize.Height) / 2);
                    }
                }
            }
        }

        public void Toggle()
        {
            showDots = !showDots;
            if (showDots)
            {
                SetLayeredWindowAttributes(this.Handle, 0, 255, LWA_ALPHA | LWA_COLORKEY);
                this.TopMost = true;
                this.BringToFront();
                trayIcon.Text = maps[currentMap].Name + " - 已显示";

                showMapName = true;
                mapNameTimer.Stop();
                mapNameTimer.Start();
            }
            else
            {
                SetLayeredWindowAttributes(this.Handle, 0, 0, LWA_ALPHA | LWA_COLORKEY);
                showMapName = false;
                mapNameTimer.Stop();
                trayIcon.Text = "PUBG 标记 - 已隐藏";
            }
            this.Invalidate();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (hookId != IntPtr.Zero)
                UnhookWindowsHookEx(hookId);
            if (trayIcon != null)
            {
                trayIcon.Visible = false;
                trayIcon.Dispose();
            }
            base.OnFormClosed(e);
        }
    }

    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new OverlayForm());
        }
    }
}
