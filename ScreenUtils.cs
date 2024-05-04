using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using gdi = System.Drawing;

namespace QScreenShot
{
    public static class ScreenUtils
    {
        public class WinApi
        {
            public static bool IsMouseLeftButtonDown => GetAsyncKeyState(VK_LBUTTON);
            [DllImport("user32.dll")]
            static extern bool GetAsyncKeyState(int vKey);

            // 虚拟键码：鼠标左键  
            private const int VK_LBUTTON = 0x01;


            [DllImport("user32.dll")]
            private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

            [DllImport("user32.dll")]
            private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

            [DllImport("user32.dll")]
            private static extern int GetWindowTextLength(IntPtr hWnd);

            [DllImport("user32.dll")]
            private static extern bool IsWindowVisible(IntPtr hWnd);
            [DllImport("user32.dll")]
            private static extern bool IsWindow(IntPtr hWnd);

            [DllImport("user32.dll")]
            private static extern IntPtr GetAncestor(IntPtr hWnd, uint gaFlags);

            private const uint GA_ROOT = 2;

            // 委托定义，用于EnumWindows回调  
            private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

            [DllImport("user32.dll", SetLastError = true)]
            static extern bool EnumChildWindows(IntPtr hWndParent, EnumWindowsProc lpEnumFunc, IntPtr lParam);


            //获取所有在显示的window
            public static List<IntPtr> GetTotalTopWindows()
            {
                List<IntPtr> windowHandles = new List<IntPtr>();

                // 调用EnumWindows，传入自定义的回调函数  
                EnumWindows(new EnumWindowsProc(EnumTheWindows), IntPtr.Zero);

                // 回调函数，用于EnumWindows  
                bool EnumTheWindows(IntPtr hWnd, IntPtr lParam)
                {
                    // 获取窗口的根窗口句柄（对于非顶级窗口，返回其父窗口，直到顶级窗口）  
                    //IntPtr root = GetAncestor(hWnd, GA_ROOT);
                    //if (root == hWnd)

                    //{
                    //    if (IsWindowVisible(hWnd)&&IsWindow(hWnd))
                    //    {
                    //        windowHandles.Add(hWnd);
                    //    }
                    //}

                    if (IsWindowVisible(hWnd) && IsWindow(hWnd))
                    {
                        EnumerateChildWindows(hWnd);
                        windowHandles.Add(hWnd);
                    }

                    // 继续枚举其他窗口  
                    return true;
                }
                // 枚举指定窗口的所有子窗口  
                void EnumerateChildWindows(IntPtr parentHandle)
                {
                    //windowHandles.Clear(); // 清空列表  
                    //GCHandle gch = GCHandle.Alloc(windowHandles);
                    try
                    {
                        EnumChildWindows(parentHandle, new EnumWindowsProc(EnumTheWindows), IntPtr.Zero);
                    }
                    finally
                    {
                        //gch.Free();
                    }
                }
                return windowHandles;
            }

            [DllImport("user32.dll")]
            static extern int GetWindowLong(IntPtr hWnd, int nIndex);

            [DllImport("user32.dll")]
            static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

            // 扩展窗口样式  
            private const int GWL_EXSTYLE = -20;
            // 分层窗口样式标志  
            private const int WS_EX_LAYERED = 0x80000;


            // 调用WinAPI的WindowFromPoint函数  
            [DllImport("user32.dll")]
            public static extern IntPtr WindowFromPoint(gdi.Point p);

            // 调用WinAPI的GetWindowRect函数  

            [DllImport("user32.dll")]
            public static extern bool GetWindowRect(IntPtr hWnd, out gdi.Rectangle lpRect);

            [DllImport("user32.dll")]
            public static extern bool GetCursorPos(out gdi.Point lpPoint);

            [DllImport("user32.dll", CharSet = CharSet.Auto)]
            public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, int wParam, int lParam);

            const int WM_GETICON = 0x007F;
            const int ICON_SMALL = 0;
            const int ICON_BIG = 1;
            public class WindowEntity
            {
                public Lazy<gdi.Rectangle> Rectangle { get; set; }
                public Lazy<string> Title { get; set; }
                public Lazy<bool> IsTransparent { get; set; }
                public Lazy<BitmapImage> Icon { get; set; }
                public IntPtr Handler { get; private set; }
                public WindowEntity(IntPtr handler)
                {
                    this.Handler = handler;
                    Title = new Lazy<string>(() =>
                    {
                        StringBuilder sb = new StringBuilder(256);
                        GetWindowText(handler, sb, sb.Capacity);
                        return sb.ToString();
                    });
                    Rectangle = new Lazy<gdi.Rectangle>(() =>
                    {
                        GetWindowRect(Handler, out var rec);
                        return new gdi.Rectangle(rec.X, rec.Y, rec.Width - rec.X, rec.Height - rec.Y);
                    });
                    IsTransparent = new Lazy<bool>(() =>
                    {
                        int exStyle = GetWindowLong(handler, GWL_EXSTYLE);
                        return (exStyle & WS_EX_LAYERED) != 0;
                    });
                    Icon = new Lazy<BitmapImage>(() =>
                    {
                        IntPtr hIcon = SendMessage(handler, WM_GETICON, ICON_BIG, 0);
                        if (hIcon == IntPtr.Zero) return null;
                        var ic = System.Drawing.Icon.FromHandle(hIcon);
                        MemoryStream ms = new MemoryStream();
                        ic.Save(ms);
                        ic.Dispose();

                        BitmapImage img = new BitmapImage();
                        img.BeginInit();
                        img.StreamSource = ms;
                        img.EndInit();
                        return img;
                    });
                }

            }
        }
        public class ScreenImageMemory : IDisposable
        {
            private MemoryStream Stream { get; set; }
            public gdi.Bitmap BitmapSource { get; set; }
            public Lazy<BitmapImage> Image = new Lazy<BitmapImage>();
            public List<WinApi.WindowEntity> ImageWindowEntitys = new List<WinApi.WindowEntity>();
            public double Width { get; private set; }
            public double Height { get; private set; }
            /// <summary>
            /// 判断位置位于哪个窗口
            /// </summary>
            public WinApi.WindowEntity IsInWindowPoint(int x, int y, Func<WinApi.WindowEntity, bool> CanReturn)
            {
                var dd = ImageWindowEntitys.Select(s => s.Rectangle.Value).ToArray();
                foreach (var i in ImageWindowEntitys)
                {
                    var b = i.Rectangle.Value.Contains(x, y);
                    if (b && CanReturn(i))
                    {
                        return i;
                    }
                }
                return null;
            }
            //刷新窗体数据快照
            public void ReloadInWindowPointSource()
            {
                ImageWindowEntitys.Clear();
                foreach (var i in WinApi.GetTotalTopWindows())
                {
                    var w = new WinApi.WindowEntity(i);
                    if (!w.IsTransparent.Value) ImageWindowEntitys.Add(w);
                }
                //var dd = ImageWindowEntitys.Select(s=>new { T=s.Title.Value ,R=s.Rectangle.Value}).ToArray();
                //int itt = 1;
            }
            public ScreenImageMemory()
            {
                gdi.Bitmap map = new gdi.Bitmap((int)SystemParameters.PrimaryScreenWidth, (int)SystemParameters.PrimaryScreenHeight);
                gdi.Graphics g = gdi.Graphics.FromImage(map);
                g.CopyFromScreen(0, 0, 0, 0, new gdi.Size(map.Width, map.Height));
                Width = map.Width;
                Height = map.Height;
                g.Dispose();
                MemoryStream ms = new MemoryStream();
                map.Save(ms, gdi.Imaging.ImageFormat.Png);
                BitmapImage img = new BitmapImage();
                img.BeginInit();
                img.StreamSource = ms;
                img.EndInit();
                Image = new Lazy<BitmapImage>(() => img);
                Stream = ms;
                ReloadInWindowPointSource();
                this.BitmapSource = map;
            }
            public ScreenImageMemory(MemoryStream ms)
            {
                gdi.Bitmap map = new gdi.Bitmap(gdi.Bitmap.FromStream(ms));
                Width = map.Width;
                Height = map.Height;
                BitmapImage img = new BitmapImage();
                img.BeginInit();
                img.StreamSource = ms;
                img.EndInit();
                Image = new Lazy<BitmapImage>(() => img);
                Stream = ms;
                ReloadInWindowPointSource();
                this.BitmapSource = map;
            }
            public void Dispose()
            {
                this.BitmapSource.Dispose();
                Stream?.Dispose();
                Image = null;
                Stream = null;
            }
            public ScreenImageMemory GetPointImage(int x, int y, int Size = 50)
            {
                gdi.Bitmap map = new gdi.Bitmap(Size, Size);
                gdi.Graphics g = gdi.Graphics.FromImage(map);
                g.FillRectangle(gdi.Brushes.Black, 0, 0, Size, Size);
                g.DrawImage(this.BitmapSource, new gdi.Rectangle(0, 0, Size, Size), x - Size / 2, y - Size / 2, Size, Size, gdi.GraphicsUnit.Pixel);
                g.Dispose();
                MemoryStream ms = new MemoryStream();
                map.Save(ms, gdi.Imaging.ImageFormat.Png);
                map.Dispose();
                BitmapImage img = new BitmapImage();
                img.BeginInit();
                img.StreamSource = ms;
                img.EndInit();
                return new ScreenImageMemory(ms);
            }
            public BitmapImage GetPointImageBMI(int x, int y, int Size = 50)
            {
                gdi.Bitmap map = new gdi.Bitmap(Size, Size);
                gdi.Graphics g = gdi.Graphics.FromImage(map);
                g.FillRectangle(gdi.Brushes.Black, 0, 0, Size, Size);
                g.DrawImage(this.BitmapSource, new gdi.Rectangle(0, 0, Size, Size), x - Size / 2, y - Size / 2, Size, Size, gdi.GraphicsUnit.Pixel);
                g.Dispose();
                MemoryStream ms = new MemoryStream();
                map.Save(ms, gdi.Imaging.ImageFormat.Png);
                map.Dispose();
                BitmapImage img = new BitmapImage();
                img.BeginInit();
                img.StreamSource = ms;
                img.EndInit();
                return img;
            }
            public Color GetPointColor(int x, int y)
            {
                var c = this.BitmapSource.GetPixel(x, y);
                return new Color() { R = c.R, G = c.G, B = c.B };
            }
            public ScreenImageMemory Clip(int X, int Y, int Width, int Height)
            {
                gdi.Bitmap map = new gdi.Bitmap(Width, Height);
                MemoryStream ms = new MemoryStream();

                gdi.Graphics g = gdi.Graphics.FromImage(map);
                g.DrawImage(this.BitmapSource, new gdi.Rectangle(0, 0, Width, Height), X, Y, Width, Height, gdi.GraphicsUnit.Pixel);
                g.Dispose();
                map.Save(ms, gdi.Imaging.ImageFormat.Png);
                map.Dispose();
                return new ScreenImageMemory(ms);
            }
        }
        /// <summary>
        /// 获取屏幕的图像 包含流以内的一些操作方法
        /// </summary>
        public static ScreenImageMemory GetScreenImage()
        {
            return new ScreenImageMemory();
        }
        /// <summary>
        /// 截图
        /// </summary>
        /// <param name="Print">点击确认按钮</param>
        /// <param name="InitControl">初始化组件 可为Null</param>
        /// <returns></returns>
        public static Window Show(Action<ScreenImageMemory> Print, Action<ScreenShotControl> InitControl = null, Func<ScreenShotControl, bool> Cancel = null)
        {
            ScreenShotControl c = new ScreenShotControl();
            c.Shot();
            InitControl?.Invoke(c);
            Window w = new Window()
            {
                Content = c,
                WindowState = WindowState.Normal,
                WindowStyle = WindowStyle.None,
                Topmost = false,
                AllowsTransparency = true,
                Background = null,
                Width = SystemParameters.PrimaryScreenWidth,
                Height = SystemParameters.PrimaryScreenHeight,
                Left = 0,
                Top = 0,
                Title = "截图"
            };
            ControlUtils.ClickEventSetter<Window> wc = new ControlUtils.ClickEventSetter<Window>(w, true);
            wc.Click += (ss, ee) =>
            {
                if (ee.ChangedButton == MouseButton.Right)
                {
                    if (c.Step == ScreenShotControl.ScreenImageShotStep.SelectRange)
                    {
                        if (Cancel?.Invoke(c) ?? true)
                        {
                            w.Close();
                            c.Dispose();
                        }
                    }
                }
            };
            w.PreviewKeyDown += (ss, ee) =>
            {
                if (ee.Key == Key.A && Keyboard.IsKeyDown(Key.LeftCtrl))
                {
                    c.SelectedRangeToMax();
                }
                else if (ee.Key == Key.Delete && c.EditDrawingItem != null)
                {
                    c.RemoveSelectEditDrawingItem();
                }
                else if (ee.Key == Key.Escape)
                {
                    if (Cancel?.Invoke(c) ?? true)
                    {
                        w.Close();
                        c.Dispose();
                    }
                }
            };
            c.Print += (ss, ee) =>
            {
                Print.Invoke(ee);
                w.Close();
                c.Dispose();
            };
            w.Show();
            return w;
        }
        public static gdi.Bitmap SaveVisualToPng(FrameworkElement visual, int dpi = 96)
        {
            // 获取视觉对象的大小  
            //Rect bounds = VisualTreeHelper.GetDescendantBounds(visual);
            Rect bounds = new Rect(0, 0, visual.ActualWidth, visual.ActualHeight);

            // 创建与视觉对象大小相匹配的RenderTargetBitmap  
            RenderTargetBitmap rtb = new RenderTargetBitmap(
                (int)(bounds.Width * dpi / 96.0),
                (int)(bounds.Height * dpi / 96.0),
                dpi,
                dpi,
                PixelFormats.Pbgra32);

            // 将视觉对象渲染到RenderTargetBitmap中  
            rtb.Render(visual);

            // 创建PngBitmapEncoder对象  
            PngBitmapEncoder encoder = new PngBitmapEncoder();

            // 将RenderTargetBitmap添加到encoder的帧中  
            encoder.Frames.Add(BitmapFrame.Create(rtb));

            MemoryStream ms = new MemoryStream();
            encoder.Save(ms);
            return gdi.Bitmap.FromStream(ms) as gdi.Bitmap;
        }
        public static Task CreateMouseDownWaitUpTask(Action MouseMove, Action MouseUp)
        {
            return Task.Run(() =>
            {
                while (WinApi.IsMouseLeftButtonDown)
                {
                    MouseMove();
                    Thread.Sleep(10);
                }
                MouseUp();
            });
        }
        public static void CreateMouseDownWaitUpTick(Action<Point> MouseMove, Action<Point> MouseUp, Action<Point>? Click = null)
        {
            WinApi.GetCursorPos(out var DownPos);
            EventHandler hd = null;
            hd = new EventHandler((ss, ee) =>
            {
                WinApi.GetCursorPos(out var NowPos);
                var PNowPos = new Point(NowPos.X, NowPos.Y);
                if (WinApi.IsMouseLeftButtonDown)
                {
                    if (NowPos != DownPos)
                    {
                        MouseMove(PNowPos);
                    }
                }
                else
                {
                    MouseUp(PNowPos);
                    if (NowPos == DownPos)
                    {
                        Click?.Invoke(PNowPos);
                    }
                    CompositionTarget.Rendering -= hd;
                }
            });
            CompositionTarget.Rendering += hd;
        }

    }
}
