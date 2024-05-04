using QScreenShot.DrawingItemControsl;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Documents.DocumentStructures;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using static QScreenShot.ScreenShotControl;
using gdi = System.Drawing;

namespace QScreenShot
{
    /// <summary>
    /// ScreenShotControl.xaml 的交互逻辑
    /// </summary>
    public partial class ScreenShotControl : UserControl, IDisposable
    {
        public enum ScreenImageShotStep
        {
            None, SelectRange, Drawing, End
        }
        #region 画笔
        public class DrawingUIItemHost : INotifyPropertyChanged
        {
            public enum ResizeMode
            {
                None, StartEnd, RecCorner, All, Center
            }
            public Func<ResizeMode> GetCanResizeMode { get; set; } = null;
            public ResizeMode CanResizeMode { get => GetCanResizeMode?.Invoke() ?? ResizeMode.None; }
            public class DrawingUIMenuSource
            {
                public enum MenuType
                {
                    Color, Size
                }
                public ObservableCollection<KeyValuePair<MenuType, string>> Menus = new ObservableCollection<KeyValuePair<MenuType, string>>();

            }
            public String DrawingName { get; set; } = "Default";//绘制的图形名称
            public ImageDrawingPenBase DrawingPenBase { get; set; }
            public UIElement UIElement { get; set; }
            public RectangleByPoints Location { get; set; }
            public bool IsFirstDrawing { get; set; } = true;//是否是第一次创建的初始化绘制
            public event PropertyChangedEventHandler? PropertyChanged;
            public void SetLocation(RectangleByPoints NewPos)
            {
                this.Location = NewPos;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Location"));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CanResizeMode"));
            }
        }
        public class RectangleByPoints
        {
            public override string ToString()
            {
                return X1 + "," + Y1 + " , " + X2 + "," + Y2;
            }
            public double X1 { get; set; }
            public double Y1 { get; set; }
            public double X2 { get; set; }
            public double Y2 { get; set; }

            // 构造函数，用于初始化矩形的对角点  
            public RectangleByPoints(double x1, double y1, double x2, double y2)
            {
                X1 = x1;
                Y1 = y1;
                X2 = x2;
                Y2 = y2;
            }
            /// <summary>
            /// 获取坐标中的原点位置 因为矩形的左面有可能是X2 顶上也有可能是Y2
            /// </summary>
            /// <returns></returns>
            public Point GetStartPoint()
            {
                Point p = new Point();
                p.X = X1 == Left ? 0 : X1 - X2;
                p.Y = Y1 == Top ? 0 : Y1 - Y2;
                return p;
            }
            public Point GetEndPoint()
            {
                Point p = new Point();
                p.X = X1 == Left ? X2 - X1 : 0;
                p.Y = Y1 == Top ? Y2 - Y1 : 0;
                return p;
            }
            public double Left => X1 > X2 ? X2 : X1;
            public double Top => Y1 < Y2 ? Y1 : Y2;
            public double Right => X1 < X2 ? X2 : X1;
            public double Bottom => Y1 > Y2 ? Y1 : Y2;
            public double CenterX => Width / 2;
            public double CenterY => Height / 2;

            public double RX1 => GetStartPoint().X;
            public double RY1 => GetStartPoint().Y;
            public double RX2 => GetEndPoint().X;
            public double RY2 => GetEndPoint().Y;


            // 计算矩形的宽度  
            public double Width
            {
                get
                {
                    double width = Math.Abs(X2 - X1);
                    return width;
                }
            }

            // 计算矩形的高度  
            public double Height
            {
                get
                {
                    double height = Math.Abs(Y2 - Y1);
                    return height;
                }
            }

            // 可以添加其他方法或属性，例如中心点、面积等  
            public Point CenterPoint
            {
                get
                {
                    double centerX = (X1 + X2) / 2;
                    double centerY = (Y1 + Y2) / 2;
                    return new Point(centerX, centerY);
                }
            }

            public double Area
            {
                get
                {
                    return Width * Height;
                }
            }
        }
        public abstract class ImageDrawingPenBase
        {
            public enum DrawingState
            {
                Start, Move, End
            }
            public enum DrawingItemProperty
            {
                None, Color, Size, SizeBig, Value0To100_1,Value0To100_2
            }
            public event Action<ImageDrawingPenBase, DrawingItemPropertyData> DrawingProertyChanged;
            public class DrawingItemPropertyData : INotifyPropertyChanged
            {
                public DrawingItemProperty PropertyType { get; set; }
                private object _Value;
                public object Value { get => _Value; set { _Value = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Value")); } }

                public event PropertyChangedEventHandler? PropertyChanged;
                public static DrawingItemPropertyData[] Creates(params DrawingItemProperty[] types)
                {
                    return types.Select(s => new DrawingItemPropertyData() { PropertyType = s }).ToArray();
                }
                public static DrawingItemPropertyData[] CreateColorAndSize(Brush Color = null, double? Size = null)
                {
                    return new DrawingItemPropertyData[]
                    {
                         new DrawingItemPropertyData(){PropertyType= DrawingItemProperty.Color,Value=Color??Brushes.Black},
                         new DrawingItemPropertyData(){PropertyType= DrawingItemProperty.Size,Value= Size??6}
                    };
                }
                public static DrawingItemPropertyData[] CreateColorAndSizeBig(Brush Color = null, double? Size = null)
                {
                    return new DrawingItemPropertyData[]
                    {
                         new DrawingItemPropertyData(){PropertyType= DrawingItemProperty.Color,Value=Color??Brushes.Black},
                         new DrawingItemPropertyData(){PropertyType= DrawingItemProperty.SizeBig,Value= Size??6}
                    };
                }
                public static DrawingItemPropertyData[] CreateKline(Brush Color = null, double? Open = null, double? Now = null)
                {
                    return new DrawingItemPropertyData[]
                    {
                         new DrawingItemPropertyData(){PropertyType= DrawingItemProperty.Value0To100_1,Value= Open??20},
                         new DrawingItemPropertyData(){PropertyType= DrawingItemProperty.Value0To100_2,Value= Now??100}
                    };
                }
                //public void Update()
                //{
                //    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Value"));
                //}
            }
            public abstract String PenName { get; }
            public abstract ImageSource Icon { get; }
            public abstract void DrawingStart(double X, double Y);
            public abstract void DrawingNow(double X, double Y);
            public abstract void DrawingEnd(double X, double Y);
            public abstract void DrawingStateChange(DrawingUIItemHost Target, DrawingItemPropertyData Data);
            public abstract DrawingItemPropertyData[] CanDrawingItemPropertys { get; }
            public void OnDrawingProertyChanged(DrawingItemPropertyData d)
            {
                DrawingProertyChanged?.Invoke(this, d);
            }
        }
        public class ImageDrawingPenCustom : ImageDrawingPenBase
        {
            private Func<string> _PenName;
            private Func<ImageSource> _Icon;
            private Action<double, double, DrawingState> _Drawing;
            private Action<DrawingUIItemHost, DrawingItemPropertyData> _DrawingTargetState;
            private Func<DrawingItemPropertyData[]> _CanDrawingItemPropertys;

            public override string PenName => _PenName();
            public override ImageSource Icon => _Icon();
            public override DrawingItemPropertyData[] CanDrawingItemPropertys => _CanDrawingItemPropertys?.Invoke();

            public ImageDrawingPenCustom(Func<string> GetPenName, Func<ImageSource> GetIcon, Action<double, double, DrawingState> ToDrawing)
            {
                this._PenName = GetPenName;
                this._Icon = GetIcon;
                this._Drawing = ToDrawing;
            }
            /// <summary>
            /// 设置可以更改哪些状态
            /// </summary>
            /// <param name="CanDrawingItems">可以更改的绘制项状态</param>
            /// <param name="DrawingTargetState">被要求更改目标触发</param>
            /// <returns></returns>
            public ImageDrawingPenCustom SetDrawingPropertys(DrawingItemPropertyData[] CanDrawingItems, Action<DrawingUIItemHost, DrawingItemPropertyData> DrawingTargetState = null)
            {
                if (this.CanDrawingItemPropertys != null)
                {
                    foreach (var i in CanDrawingItemPropertys)
                    {
                        i.PropertyChanged -= I_PropertyChanged;
                    }
                }
                foreach (var i in CanDrawingItems)
                {
                    i.PropertyChanged += I_PropertyChanged;
                }
                this._CanDrawingItemPropertys = () => CanDrawingItems;
                this._DrawingTargetState = DrawingTargetState;
                return this;
            }

            private void I_PropertyChanged(object? sender, PropertyChangedEventArgs e)
            {
                if (e.PropertyName == "Value")
                {
                    OnDrawingProertyChanged(sender as dynamic);
                }
            }

            public override void DrawingEnd(double X, double Y)
            {
                _Drawing.Invoke(X, Y, DrawingState.End);
            }

            public override void DrawingNow(double X, double Y)
            {
                _Drawing.Invoke(X, Y, DrawingState.Move);
            }

            public override void DrawingStart(double X, double Y)
            {
                _Drawing.Invoke(X, Y, DrawingState.Start);
            }

            public override void DrawingStateChange(DrawingUIItemHost Target, DrawingItemPropertyData Data)
            {
                _DrawingTargetState?.Invoke(Target, Data);
            }
        }
        #endregion

        public class ScreenShotControlSetting
        {
            public bool IsShowPointImage { get; set; } = true;
            public int PointImageSize { get; set; } = 45;

        }





        public ImageDrawingPenBase InHandDrawingPen
        {
            get { return (ImageDrawingPenBase)GetValue(InHandDrawingPenProperty); }
            set { SetValue(InHandDrawingPenProperty, value); }
        }

        // Using a DependencyProperty as the backing store for InHandDrawingPen.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty InHandDrawingPenProperty =
            DependencyProperty.Register("InHandDrawingPen", typeof(ImageDrawingPenBase), typeof(ScreenShotControl), new PropertyMetadata(null));



        public DrawingUIItemHost EditDrawingItem
        {
            get { return (DrawingUIItemHost)GetValue(EditDrawingItemProperty); }
            set { SetValue(EditDrawingItemProperty, value); }
        }

        // Using a DependencyProperty as the backing store for EditDrawingItem.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty EditDrawingItemProperty =
            DependencyProperty.Register("EditDrawingItem", typeof(DrawingUIItemHost), typeof(ScreenShotControl), new PropertyMetadata(null));



        public ScreenShotControlSetting Setting
        {
            get { return (ScreenShotControlSetting)GetValue(SettingProperty); }
            set { SetValue(SettingProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Setting.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SettingProperty =
            DependencyProperty.Register("Setting", typeof(ScreenShotControlSetting), typeof(ScreenShotControl), new PropertyMetadata(new ScreenShotControlSetting()));


        public BitmapImage ImageSource
        {
            get { return (BitmapImage)GetValue(ImageSourceProperty); }
            set { SetValue(ImageSourceProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ImageSource.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ImageSourceProperty =
            DependencyProperty.Register("ImageSource", typeof(BitmapImage), typeof(ScreenShotControl), new PropertyMetadata(null));



        public ImageSource PointImageSource
        {
            get { return (ImageSource)GetValue(PointImageSourceProperty); }
            set { SetValue(PointImageSourceProperty, value); }
        }

        // Using a DependencyProperty as the backing store for PointImageSource.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PointImageSourceProperty =
            DependencyProperty.Register("PointImageSource", typeof(ImageSource), typeof(ScreenShotControl), new PropertyMetadata(null));



        public ScreenImageShotStep Step
        {
            get { return (ScreenImageShotStep)GetValue(StepProperty); }
            set { SetValue(StepProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Step.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty StepProperty =
            DependencyProperty.Register("Step", typeof(ScreenImageShotStep), typeof(ScreenShotControl), new PropertyMetadata(ScreenImageShotStep.SelectRange));




        protected ScreenUtils.ScreenImageMemory ImageSourceMemory = null;
        protected IntPtr? ParentHandler = null;
        protected ControlUtils.ClickEventSetter<ScreenShotControl> ClickSetter = null;
        public event Action<ScreenShotControl, ScreenUtils.ScreenImageMemory> Print;
        //仅仅是样式 对截图数据毫无更改
        #region 选中区域样式
        /// <summary>
        /// 选中区域 边框尺寸
        /// </summary>
        public Thickness SelectRangeBorderThickness { get => BD_SelectRangeBorder.BorderThickness; set => BD_SelectRangeBorder.BorderThickness = value; }
        /// <summary>
        /// 选中区域 边框圆角
        /// </summary>
        public CornerRadius SelectRangeBorderCornerRadius { get => BD_SelectRangeBorder.CornerRadius; set => BD_SelectRangeBorder.CornerRadius = value; }
        /// <summary>
        /// 选中区域 遮罩
        /// </summary>
        public Brush SelectRangeBackgroundMask { get => BD_SelectRangeBorder.Background; set => BD_SelectRangeBorder.Background = value; }
        /// <summary>
        /// 选中区域 边框颜色
        /// </summary>
        public Brush SelectRangeBorderBrush { get => BD_SelectRangeBorder.BorderBrush; set => BD_SelectRangeBorder.BorderBrush = value; }
        #endregion
        public ScreenShotControl()
        {
            InitializeComponent();
            Loaded += ScreenShotControl_Loaded;
            CompositionTarget.Rendering += CompositionTarget_Rendering;
        }

        private void CompositionTarget_Rendering(object? sender, EventArgs e)
        {
            if (Cursor == Cursors.Arrow && this.InHandDrawingPen != null)
            {
                Cursor = Cursors.Pen;
            }
        }

        private void ScreenShotControl_Loaded(object sender, RoutedEventArgs e)
        {
            Mouse.Capture(this, CaptureMode.Element);
            var p = Window.GetWindow(this);
            if (p != null)
            {
                var hhand = new WindowInteropHelper(p);
                ParentHandler = hhand?.Handle;
            }
            if (Step != ScreenImageShotStep.None && ImageSourceMemory != null)
            {
                OnPropertyChanged(new DependencyPropertyChangedEventArgs(StepProperty, ScreenImageShotStep.None, Step));
            }
            ClickSetter = new ControlUtils.ClickEventSetter<ScreenShotControl>(this, true, 1);
            ClickSetter.Click += ClickSetter_Click; ;
            LoadDrawingPens();
        }

        private void ClickSetter_Click(ScreenShotControl arg1, MouseButtonEventArgs arg2)
        {
            if (arg2.ChangedButton == MouseButton.Right)
            {
                StepBack();
            }
        }

        public ScreenUtils.ScreenImageMemory Shot()
        {
            ImageSourceMemory = ScreenUtils.GetScreenImage(); ;
            ImageSource = ImageSourceMemory.Image.Value;
            return ImageSourceMemory;
        }
        public void Dispose()
        {
            ImageSourceMemory.Dispose();
            ImageSource = null;
        }
        /// <summary>
        /// 将一个矩形范围转换为组件内空间位置
        /// </summary>
        public Rect RecToControlRecPoint(Rect rec)
        {
            var Rec = rec;
            var cw = RenderSize.Width;
            var ch = RenderSize.Height;
            Rec.X = Rec.Left < 0 ? 0 : Rec.Right >= cw ? cw - Rec.Width : Rec.X;
            Rec.Y = Rec.Top < 0 ? 0 : Rec.Bottom >= ch ? ch - Rec.Height : Rec.Y;
            return Rec;
        }
        //把组件的Point转换为相对其中图像的Point
        protected Point ControlPointToImagePoint(Point point)
        {
            double WidthEnlarge = (double)(ImageSourceMemory.Width / RenderSize.Width), HeightEnlarge = (double)(ImageSourceMemory.Height / RenderSize.Height);
            var tx = point.X * WidthEnlarge;
            var ty = point.Y * HeightEnlarge;
            tx = tx < 0 ? 0 : tx > ImageSourceMemory.Width ? ImageSourceMemory.Width : tx > ImageSourceMemory.Width - 1 ? ImageSourceMemory.Width : tx;
            ty = ty < 0 ? 0 : ty > ImageSourceMemory.Height ? ImageSourceMemory.Height : ty > ImageSourceMemory.Height - 1 ? ImageSourceMemory.Height : ty;
            return new Point(tx, ty);
        }
        protected Rect ControlRecToImageRec(Rect point)
        {
            var imgRec = new Rect(0, 0, this.ImageSourceMemory.Width, this.ImageSourceMemory.Height);
            double WidthEnlarge = (double)(ImageSourceMemory.Width / ActualWidth), HeightEnlarge = (double)(ImageSourceMemory.Height / ActualHeight);
            var tx = point.X * WidthEnlarge;
            var ty = point.Y * HeightEnlarge;
            tx = tx < 0 ? 0 : tx > ImageSourceMemory.Width ? ImageSourceMemory.Width : tx > ImageSourceMemory.Width - 1 ? ImageSourceMemory.Width : tx;
            ty = ty < 0 ? 0 : ty > ImageSourceMemory.Height ? ImageSourceMemory.Height : ty > ImageSourceMemory.Height - 1 ? ImageSourceMemory.Height : ty;
            //return new Rect(tx, ty, point.Width * WidthEnlarge, point.Height * HeightEnlarge);
            var res = new Rect((int)tx, (int)ty, (int)(point.Width * WidthEnlarge), (int)(point.Height * HeightEnlarge));
            var maxOffset = 5;
            return (res.X == 0 && res.Y == 0 && res.Width >= RenderSize.Width - maxOffset && res.Height >= RenderSize.Height - maxOffset) ? imgRec : res;
        }
        protected Point ImagePointToControlPoint(Point point)
        {
            double WidthEnlarge = (double)(ImageSourceMemory.Width / ActualWidth), HeightEnlarge = (double)(ImageSourceMemory.Height / ActualHeight);
            var tx = point.X / WidthEnlarge;
            var ty = point.Y / HeightEnlarge;
            tx = tx < 0 ? 0 : tx > ImageSourceMemory.Width ? ImageSourceMemory.Width : tx > ImageSourceMemory.Width - 1 ? ImageSourceMemory.Width : tx;
            ty = ty < 0 ? 0 : ty > ImageSourceMemory.Height ? ImageSourceMemory.Height : ty > ImageSourceMemory.Height - 1 ? ImageSourceMemory.Height : ty;
            return new Point(tx, ty);
        }
        protected Rect ImageRecToControlRec(Rect point)
        {
            double WidthEnlarge = (double)(ImageSourceMemory.Width / ActualWidth), HeightEnlarge = (double)(ImageSourceMemory.Height / ActualHeight);
            var tx = point.X / WidthEnlarge;
            var ty = point.Y / HeightEnlarge;
            tx = tx < 0 ? 0 : tx > ImageSourceMemory.Width ? ImageSourceMemory.Width : tx > ImageSourceMemory.Width - 1 ? ImageSourceMemory.Width : tx;
            ty = ty < 0 ? 0 : ty > ImageSourceMemory.Height ? ImageSourceMemory.Height : ty > ImageSourceMemory.Height - 1 ? ImageSourceMemory.Height : ty;
            var res = new Rect(tx, ty, point.Width / WidthEnlarge, point.Height / HeightEnlarge);
            return res;

        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (e.Property == StepProperty)
            {
                var oldStep = (ScreenImageShotStep)e.OldValue;
                var newStep = (ScreenImageShotStep)e.NewValue;
                //------------
                {
                    switch (newStep)
                    {
                        case ScreenImageShotStep.None:
                            break;
                        case ScreenImageShotStep.SelectRange:
                            UnSelectRec();
                            PreviewMouseMove += ScreenShotControl_WindowRecSelectMouseMove;
                            PreviewMouseDown += ScreenShotControl_WindowRecSelectMouseDown;
                            break;
                        case ScreenImageShotStep.Drawing:
                            ShowDrawingUtilBox();
                            ShowDrawingSelectInfo();
                            MouseDown += ScreenShotControl_DrawingMouseDown;
                            PreviewMouseMove += ScreenShotControl_DrawingMouseMove;
                            PreviewMouseUp += ScreenShotControl_DrawingMouseUp;
                            break;
                        case ScreenImageShotStep.End:
                            break;
                        default:
                            break;
                    }
                }
                //------------
                {
                    switch (oldStep)
                    {
                        case ScreenImageShotStep.None:
                            break;
                        case ScreenImageShotStep.SelectRange:
                            PreviewMouseMove -= ScreenShotControl_WindowRecSelectMouseMove;
                            PreviewMouseDown -= ScreenShotControl_WindowRecSelectMouseDown;
                            HidePointimageHost();
                            HideSelectRangeToolTip();
                            break;
                        case ScreenImageShotStep.Drawing:
                            HideDrawingUtilBox();
                            HideDrawingSelectInfo();
                            this.Cursor = Cursors.Arrow;
                            this.EditDrawingItem = null;
                            InHandDrawingPen = null;
                            LIST_DrawingItems.Items.Clear();
                            MouseDown -= ScreenShotControl_DrawingMouseDown;
                            PreviewMouseMove -= ScreenShotControl_DrawingMouseMove;
                            PreviewMouseUp -= ScreenShotControl_DrawingMouseUp;
                            break;
                        case ScreenImageShotStep.End:
                            break;
                        default:
                            break;
                    }
                }
            }
            else if (e.Property == InHandDrawingPenProperty)
            {
                if (InHandDrawingPen != null)
                {
                    BD_PenDrawingPropertyHost.DataContext = this.InHandDrawingPen;
                    ShowDrawingPropertyHost();
                }
                else
                {
                    HideDrawingPropertyHost();
                }
            }
            else if (e.Property == EditDrawingItemProperty)
            {
                if (EditDrawingItem != null)
                {
                    ShowDrawingPropertyHost();
                }
                else
                {
                    HideDrawingPropertyHost();
                }
            }
            base.OnPropertyChanged(e);
        }

        private void ScreenShotControl_DrawingMouseUp(object sender, MouseButtonEventArgs e)
        {
        }

        private void ScreenShotControl_DrawingMouseMove(object sender, MouseEventArgs e)
        {
            //Cursor = this.InHandDrawingPen != null ? Cursors.Pen : Cursors.Arrow;
            //if (Cursor == Cursors.Arrow && this.InHandDrawingPen != null)
            //{
            //    Cursor = Cursors.Pen;
            //}
        }

        private void ScreenShotControl_DrawingMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (InHandDrawingPen != null && e.ChangedButton == MouseButton.Left)
            {
                var item = InHandDrawingPen;
                var p = Mouse.GetPosition(this);
                item.DrawingStart(p.X, p.Y);
                ScreenUtils.CreateMouseDownWaitUpTick(p =>
                {
                    var pos = Mouse.GetPosition(this);
                    item.DrawingNow(pos.X, pos.Y);
                }, p =>
                {
                    var pos = Mouse.GetPosition(this);
                    item.DrawingEnd(pos.X, pos.Y);
                    //LIST_DrawingPens.SelectedItem = null;
                });
            }
        }

        private Rect? StepData_Start_SelectedRect = null;
        private ScreenUtils.ScreenImageMemory StepData_SelectRange_PointImageSource = null;
        private void ScreenShotControl_WindowRecSelectMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && e.ChangedButton == MouseButton.Left)
            {
                Point? StepData_Start_MouseDownPos = e.GetPosition(this);
                Mouse.Capture(this, CaptureMode.Element);

                GRID_SelectRecMask.BeginAnimation(Canvas.LeftProperty, null);
                GRID_SelectRecMask.BeginAnimation(Canvas.TopProperty, null);
                GRID_SelectRecMask.BeginAnimation(WidthProperty, null);
                GRID_SelectRecMask.BeginAnimation(HeightProperty, null);

                ScreenUtils.CreateMouseDownWaitUpTick((pos) =>
                {
                    Mouse.Capture(this, CaptureMode.Element);
                    var nowPos = Mouse.GetPosition(this);
                    var nowPosInt = new gdi.Point((int)nowPos.X, (int)nowPos.Y);
                    //如果是鼠标按下后移动选择尺寸
                    if (ScreenUtils.WinApi.IsMouseLeftButtonDown && StepData_Start_MouseDownPos != null)
                    {
                        Rect select = new Rect(StepData_Start_MouseDownPos.Value, nowPos);
                        SelectRec((select));
                        ShowSelectRangeTooltip(new
                        {
                            SelectRange = ControlRecToImageRec(StepData_Start_SelectedRect.Value),
                            MouseAbsPos = pos,
                            MouseRealtPos = nowPos,
                        }, false);
                    }
                }, (pos) =>
                {
                    var upPos = Mouse.GetPosition(this);
                    SelectedRec((this.StepData_Start_SelectedRect.Value));
                    StepData_Start_MouseDownPos = null;
                    Mouse.Capture(this, CaptureMode.None);
                }, (pos) =>
                {
                });
            }

        }
        //如果是开始的范围选择 就会调用MouseMove以下事件
        private void ScreenShotControl_WindowRecSelectMouseMove(object sender, MouseEventArgs e)
        {
            if (this.PointImageSource != null)
            {
                ShowPointImageHost();
            }
            var nowPos = e.GetPosition(this);
            var nowPosInt = new gdi.Point((int)nowPos.X, (int)nowPos.Y);
            //如果是鼠标按下后移动选择尺寸
            if (e.LeftButton == MouseButtonState.Pressed)
            {
            }
            //否则就推荐窗体大小选择尺寸
            else
            {
                ScreenUtils.WinApi.GetCursorPos(out var screenPos);
                var imagePos = ControlPointToImagePoint(e.GetPosition(this));
                var d = ImageSourceMemory.IsInWindowPoint((int)imagePos.X, (int)imagePos.Y, i => (this.ParentHandler == null ? true : i.Handler != ParentHandler.Value));
                var wrec = d == null ? new gdi.Rectangle(0, 0, (int)RenderSize.Width, (int)RenderSize.Height) : d.Rectangle.Value;
                SelectRec(ImageRecToControlRec(new Rect(wrec.X, wrec.Y, wrec.Width, wrec.Height)), true);
                ShowSelectRangeTooltip(new
                {
                    Window = d,
                    SelectRange = wrec,
                    MouseAbsPos = screenPos,
                    MouseRealtPos = imagePos,
                }, true);
            }
            if (Step == ScreenImageShotStep.SelectRange && Setting.IsShowPointImage)
            {
                var pp = ControlPointToImagePoint(nowPos);
                PointImageSource = this.ImageSourceMemory.GetPointImageBMI((int)pp.X, (int)pp.Y, 45);
                var prw = BD_PointImageHost.RenderSize.Width;
                var prh = BD_PointImageHost.RenderSize.Height;
                var marginBorder = 150;
                Canvas.SetLeft(BD_PointImageHost, nowPos.X <= 0 ? marginBorder : nowPos.X + prw >= RenderSize.Width ? RenderSize.Width - marginBorder : nowPos.X);
                Canvas.SetTop(BD_PointImageHost, nowPos.Y <= 0 ? marginBorder : nowPos.Y + prh >= RenderSize.Height ? RenderSize.Height - marginBorder : nowPos.Y);
            }

        }
        #region SelectRec
        //选择一块图片的区域但不确认
        public void SelectRec(Rect rec, bool IsUseAnima = false)
        {
            var rsw = RenderSize.Width;
            var rsh = RenderSize.Height;
            if (rec == StepData_Start_SelectedRect) return;
            var ww = rec.Right > rsw ? rsw - rec.X : rec.Left < 0 ? rec.Right : rec.Width;
            var hh = rec.Bottom > rsh ? rsh - rec.Y : rec.Top < 0 ? rec.Bottom : rec.Height;
            ww = ww < 0 ? 0 : ww;
            hh = hh < 0 ? 0 : hh;
            rec.Height = hh;
            rec.Width = ww;
            rec.X = rec.X < 0 ? 0 : rec.X > rsw ? rsw : rec.X;
            rec.Y = rec.Y < 0 ? 0 : rec.Y > rsh ? rsh : rec.Y;
            StepData_Start_SelectedRect = rec;
            Duration AnimaDuration = new Duration(TimeSpan.Parse("0:0:0.1"));
            if (IsUseAnima)
            {
                DoubleAnimation LeftAnm = new DoubleAnimation(rec.Left, AnimaDuration);
                DoubleAnimation TopAnm = new DoubleAnimation(rec.Top, AnimaDuration);
                DoubleAnimation WidthAnm = new DoubleAnimation(rec.Width, AnimaDuration);
                DoubleAnimation HeightAnm = new DoubleAnimation(rec.Height, AnimaDuration);

                Storyboard sb = new Storyboard();

                sb.Completed += (ss, ee) =>
                {
                    if (rec != StepData_Start_SelectedRect) return;
                    GRID_SelectRecMask.BeginAnimation(Canvas.LeftProperty, null);
                    GRID_SelectRecMask.BeginAnimation(Canvas.TopProperty, null);
                    GRID_SelectRecMask.BeginAnimation(WidthProperty, null);
                    GRID_SelectRecMask.BeginAnimation(HeightProperty, null);

                    Canvas.SetLeft(GRID_SelectRecMask, rec.Left);
                    Canvas.SetTop(GRID_SelectRecMask, rec.Top);
                    GRID_SelectRecMask.Width = rec.Width;
                    GRID_SelectRecMask.Height = rec.Height;
                };
                sb.Children.Add(LeftAnm);
                sb.Children.Add(TopAnm);
                sb.Children.Add(WidthAnm);
                sb.Children.Add(HeightAnm);
                Storyboard.SetTargetProperty(LeftAnm, new("(Canvas.Left)"));
                Storyboard.SetTargetProperty(TopAnm, new("(Canvas.Top)"));
                Storyboard.SetTargetProperty(WidthAnm, new("Width"));
                Storyboard.SetTargetProperty(HeightAnm, new("Height"));
                Storyboard.SetTarget(LeftAnm, GRID_SelectRecMask);
                Storyboard.SetTarget(TopAnm, GRID_SelectRecMask);
                Storyboard.SetTarget(WidthAnm, GRID_SelectRecMask);
                Storyboard.SetTarget(HeightAnm, GRID_SelectRecMask);
                sb.Begin();
            }
            else
            {
                Canvas.SetLeft(GRID_SelectRecMask, rec.Left);
                Canvas.SetTop(GRID_SelectRecMask, rec.Top);
                GRID_SelectRecMask.Width = rec.Width;
                GRID_SelectRecMask.Height = rec.Height;
            }
        }
        /// <summary>
        /// 取消已经选择的区域 
        /// </summary>
        public void UnSelectRec()
        {
            ScreenUtils.WinApi.GetCursorPos(out var screenPos);
            var imagePos = ControlPointToImagePoint(Mouse.GetPosition(this));
            var d = ImageSourceMemory.IsInWindowPoint((int)imagePos.X, (int)imagePos.Y, i => (this.ParentHandler == null ? true : i.Handler != ParentHandler.Value));
            var wrec = d == null ? new gdi.Rectangle(0, 0, (int)RenderSize.Width, (int)RenderSize.Height) : d.Rectangle.Value;
            SelectRec(ImageRecToControlRec(new Rect(wrec.X, wrec.Y, wrec.Width, wrec.Height)), true);
        }
        //选择一块图片的区域但 并且确认 然后进行绘制
        public void SelectedRec(Rect rec)
        {
            SelectRec(rec);
            Step = ScreenImageShotStep.Drawing;
        }
        //选中全图
        public void SelectedRangeToMax()
        {
            SelectRec(new Rect(0, 0, ActualWidth, ActualHeight));
            //SelectedRec(new Rect(0,0,ActualWidth,ActualHeight));
        }
        //返回上一步操作
        public void StepBack()
        {
            switch (this.Step)
            {
                case ScreenImageShotStep.None:
                    break;
                case ScreenImageShotStep.SelectRange:
                    break;
                case ScreenImageShotStep.Drawing:

                    if (EditDrawingItem != null|| InHandDrawingPen!=null)
                    {
                        EditDrawingItem = null;
                        InHandDrawingPen = null;
                        return;
                    }
                    if (LIST_DrawingItems.Items.Count != 0)
                    {
                        LIST_DrawingItems.Items.Clear();
                        return;
                    }
                    Step = ScreenImageShotStep.SelectRange;
                    break;
                case ScreenImageShotStep.End:
                    Step = ScreenImageShotStep.SelectRange;
                    break;
                default:
                    break;
            }
        }
        public void ShowPointImageHost()
        {
            BD_PointImageHost.Visibility = Visibility.Visible;
        }
        public void HidePointimageHost()
        {
            BD_PointImageHost.Visibility = Visibility.Collapsed;
        }
        //显示在绘制步骤时的选中信息
        public void ShowDrawingSelectInfo()
        {
            BD_DrawingShowSelectInfo.DataContext = new
            {
                SelectControlRec = DoubleRectToIntRect(this.StepData_Start_SelectedRect.Value),
                SelectImageRec = ControlRecToImageRec(StepData_Start_SelectedRect.Value)
            };
            BD_DrawingShowSelectInfo.Visibility = Visibility.Visible;
            Dispatcher.BeginInvoke(() =>
            {
                BD_DrawingShowSelectInfo.Measure(this.RenderSize);
                Canvas.SetLeft(BD_DrawingShowSelectInfo, (RenderSize.Width - BD_DrawingShowSelectInfo.DesiredSize.Width) / 2);
            });
        }
        public Rect DoubleRectToIntRect(Rect rect)
        {
            return new Rect((int)rect.X, (int)rect.Y, (int)(rect.Width), (int)(rect.Height));
        }
        public void HideDrawingSelectInfo()
        {
            BD_DrawingShowSelectInfo.Visibility = Visibility.Collapsed;
        }
        /// <summary>
        /// 删除选中的绘制项
        /// </summary>
        public bool RemoveSelectEditDrawingItem()
        {
            if (EditDrawingItem != null)
            {
                LIST_DrawingItems.Items.Remove(EditDrawingItem);
                EditDrawingItem = null;
                return true;
            }
            return false;
        }
        public void ShowDrawingUtilBox()
        {
            BD_DrawingItemsHost.Visibility = Visibility.Visible;
            BD_DrawingItemsHost.Measure(RenderSize);
            Dispatcher.BeginInvoke(() =>
            {
                var rr = this.StepData_Start_SelectedRect.Value;
                var rec = RecToControlRecPoint(new Rect(rr.Right - BD_DrawingItemsHost.RenderSize.Width, 7 + rr.Bottom, BD_DrawingItemsHost.RenderSize.Width, BD_DrawingItemsHost.RenderSize.Height));
                Canvas.SetLeft(BD_DrawingItemsHost, rec.X);
                Canvas.SetTop(BD_DrawingItemsHost, rec.Y);
            });
        }
        public void HideDrawingUtilBox()
        {
            BD_DrawingItemsHost.Visibility = Visibility.Collapsed;
        }
        public void ShowDrawingPropertyHost()
        {
            BD_PenDrawingPropertyHost.Measure(this.RenderSize);
            BD_PenDrawingPropertyHost.Dispatcher.BeginInvoke(() =>
            {
                Double X = Canvas.GetLeft(BD_DrawingItemsHost), Y = Canvas.GetTop(BD_DrawingItemsHost);
                var ps = BD_PenDrawingPropertyHost.DesiredSize;
                var ds = BD_DrawingItemsHost.DesiredSize;
                Y += ds.Height;
                Y = Y < 0 ? ds.Height + ps.Height : Y + ps.Height > RenderSize.Height ? Y - ps.Height - ds.Height : Y;
                Canvas.SetLeft(BD_PenDrawingPropertyHost, X);
                Canvas.SetTop(BD_PenDrawingPropertyHost, Y);
            });
            BD_PenDrawingPropertyHost.Visibility = Visibility.Visible;
        }
        public void HideDrawingPropertyHost()
        {
            BD_PenDrawingPropertyHost.Visibility = Visibility.Collapsed;
        }
        private void HideSelectRangeToolTip()
        {
            BD_SelectRangeBorder_Tooltips.Visibility = Visibility.Collapsed;
        }
        //显示选中范围等信息
        private void ShowSelectRangeTooltip(object Data, bool IsUseAnima = false)
        {
            BD_SelectRangeBorder_Tooltips.Visibility = Visibility.Visible;
            BD_SelectRangeBorder_Tooltips.DataContext = Data;
            var wrec = StepData_Start_SelectedRect.Value;
            var toolTipPos = RecToControlRecPoint(new Rect(wrec.X, wrec.Y - BD_SelectRangeBorder_Tooltips.RenderSize.Height, BD_SelectRangeBorder_Tooltips.RenderSize.Width, BD_SelectRangeBorder_Tooltips.RenderSize.Height));
            if (toolTipPos.X == Canvas.GetLeft(BD_SelectRangeBorder_Tooltips) && toolTipPos.Y == Canvas.GetTop(BD_SelectRangeBorder_Tooltips)) return;
            Duration AnimaDuration = new Duration(TimeSpan.Parse("0:0:0.1"));
            var rec = StepData_Start_SelectedRect;
            if (IsUseAnima)
            {
                DoubleAnimation LeftAnm = new DoubleAnimation(wrec.Left, AnimaDuration);
                DoubleAnimation TopAnm = new DoubleAnimation(wrec.Top, AnimaDuration);

                Storyboard.SetTargetProperty(LeftAnm, new("(Canvas.Left)"));
                Storyboard.SetTargetProperty(TopAnm, new("(Canvas.Top)"));
                Storyboard sb = new Storyboard();
                sb.Completed += (ss, ee) =>
                {
                    if (StepData_Start_SelectedRect == rec)
                    {
                        BD_SelectRangeBorder_Tooltips.BeginAnimation(Canvas.LeftProperty, null);
                        BD_SelectRangeBorder_Tooltips.BeginAnimation(Canvas.TopProperty, null);
                        Canvas.SetLeft(BD_SelectRangeBorder_Tooltips, toolTipPos.X);
                        Canvas.SetTop(BD_SelectRangeBorder_Tooltips, toolTipPos.Y);
                    }
                };
                sb.Children.Add(LeftAnm);
                sb.Children.Add(TopAnm);
                Storyboard.SetTarget(sb, BD_SelectRangeBorder_Tooltips);
                sb.Begin();
            }
            else
            {
                BD_SelectRangeBorder_Tooltips.BeginAnimation(Canvas.LeftProperty, null);
                BD_SelectRangeBorder_Tooltips.BeginAnimation(Canvas.TopProperty, null);
                Canvas.SetLeft(BD_SelectRangeBorder_Tooltips, toolTipPos.X);
                Canvas.SetTop(BD_SelectRangeBorder_Tooltips, toolTipPos.Y);
            }
        }

        #endregion

        //private void ListBoxItem_Loaded(object sender, RoutedEventArgs e)
        //{
        //    var lb = (sender as ListBoxItem);
        //    lb.SetBinding(Canvas.LeftProperty, new Binding("X1"));
        //    lb.SetBinding(Canvas.TopProperty, new Binding("Y1"));

        //}
        #region 绘制项
        private void LoadDrawingPens()
        {
            CreatePen<KLine>("K线", null, () => new KLine() { Max=100,Min=0,Open=0,Now=0}, (sender, element, rec) =>
            {
                var start = rec.GetStartPoint();
                var end = rec.GetEndPoint();
                element.Height = rec.Height;
                element.Width = rec.Width;
                element.UpdateUI();
            }).SetDrawingPropertys(ImageDrawingPenBase.DrawingItemPropertyData.CreateKline(), (sender, data) =>
            {
                sender.GetCanResizeMode = sender.CanResizeMode == DrawingUIItemHost.ResizeMode.None ? () => DrawingUIItemHost.ResizeMode.StartEnd : sender.GetCanResizeMode;
                switch (data.PropertyType)
                {
                    case ImageDrawingPenBase.DrawingItemProperty.Value0To100_1:
                        {
                            var l = (sender.UIElement as KLine);
                            l.Open = (double.Parse(data.Value.ToString()));
                        }
                        break;
                    case ImageDrawingPenBase.DrawingItemProperty.Value0To100_2:
                        {
                            var l = (sender.UIElement as KLine);
                            l.Now = (double.Parse(data.Value.ToString()));
                        }
                        break;
                    default:
                        break;
                }
            });

            CreatePen<Line>("线段", null, () => new Line(), (sender, element, rec) =>
            {
                var start = rec.GetStartPoint();
                var end = rec.GetEndPoint();
                element.X1 = start.X;
                element.Y1 = start.Y;
                element.X2 = end.X;
                element.Y2 = end.Y;
            }).SetDrawingPropertys(ImageDrawingPenBase.DrawingItemPropertyData.CreateColorAndSize(), (sender, data) =>
            {
                sender.GetCanResizeMode = sender.CanResizeMode == DrawingUIItemHost.ResizeMode.None ? () => DrawingUIItemHost.ResizeMode.StartEnd : sender.GetCanResizeMode;
                switch (data.PropertyType)
                {
                    case ImageDrawingPenBase.DrawingItemProperty.Color:
                        {
                            (sender.UIElement as Line).Stroke = (data.Value as Brush);
                        }
                        break;
                    case ImageDrawingPenBase.DrawingItemProperty.Size:
                        {
                            (sender.UIElement as Line).StrokeThickness = double.Parse(data.Value.ToString());
                        }
                        break;
                    default:
                        break;
                }
            });

            CreatePen<Rectangle>("矩形", null, () => new Rectangle(), (sender, element, rec) =>
            {
                var start = rec.GetStartPoint();
                var end = rec.GetEndPoint();
                element.Width = rec.Width;
                element.Height = rec.Height;
                sender.GetCanResizeMode = sender.CanResizeMode == DrawingUIItemHost.ResizeMode.None ? () => DrawingUIItemHost.ResizeMode.RecCorner : sender.GetCanResizeMode;
            }).SetDrawingPropertys(ImageDrawingPenBase.DrawingItemPropertyData.CreateColorAndSize(), (sender, data) =>
            {
                switch (data.PropertyType)
                {
                    case ImageDrawingPenBase.DrawingItemProperty.Color:
                        {
                            (sender.UIElement as Rectangle).Stroke = (data.Value as Brush);
                        }
                        break;
                    case ImageDrawingPenBase.DrawingItemProperty.Size:
                        {
                            (sender.UIElement as Rectangle).StrokeThickness = double.Parse(data.Value.ToString());
                        }
                        break;
                    default:
                        break;
                }
            });

            CreatePen<Ellipse>("圆形", null, () => new Ellipse(), (sender, element, rec) =>
            {
                var start = rec.GetStartPoint();
                var end = rec.GetEndPoint();
                element.Width = rec.Width;
                element.Height = rec.Height;
                sender.GetCanResizeMode = sender.CanResizeMode == DrawingUIItemHost.ResizeMode.None ? () => DrawingUIItemHost.ResizeMode.StartEnd : sender.GetCanResizeMode;
            }).SetDrawingPropertys(ImageDrawingPenBase.DrawingItemPropertyData.CreateColorAndSize(), (sender, data) =>
            {
                switch (data.PropertyType)
                {
                    case ImageDrawingPenBase.DrawingItemProperty.Color:
                        {
                            (sender.UIElement as Ellipse).Stroke = (data.Value as Brush);
                        }
                        break;
                    case ImageDrawingPenBase.DrawingItemProperty.Size:
                        {
                            (sender.UIElement as Ellipse).StrokeThickness = double.Parse(data.Value.ToString());
                        }
                        break;
                    default:
                        break;
                }
            });

            CreatePenMaxRange<Polyline>("线", null, () => new Polyline(), (sender, element, rec) =>
            {
                var start = rec.GetStartPoint();
                var end = rec.GetEndPoint();
                var ms = Mouse.GetPosition(this);
                if (sender.IsFirstDrawing)
                {
                    element.Points.Add(ms);
                }
                sender.GetCanResizeMode = sender.CanResizeMode == DrawingUIItemHost.ResizeMode.None ? () => DrawingUIItemHost.ResizeMode.Center : sender.GetCanResizeMode;
            }).SetDrawingPropertys(ImageDrawingPenBase.DrawingItemPropertyData.CreateColorAndSize(), (sender, data) =>
            {
                switch (data.PropertyType)
                {
                    case ImageDrawingPenBase.DrawingItemProperty.Color:
                        {
                            (sender.UIElement as Polyline).Stroke = (data.Value as Brush);
                        }
                        break;
                    case ImageDrawingPenBase.DrawingItemProperty.Size:
                        {
                            (sender.UIElement as Polyline).StrokeThickness = double.Parse(data.Value.ToString());
                        }
                        break;
                    default:
                        break;
                }
            });

            CreatePen<TextBox>("文本", null, () => new TextBox() { Background = Brushes.Transparent, BorderBrush = null, AcceptsReturn = true, }, (sender, element, rec) =>
            {
                var start = rec.GetStartPoint();
                var end = rec.GetEndPoint();
                element.Width = rec.Width;
                element.Height = rec.Height;
                sender.GetCanResizeMode = sender.CanResizeMode == DrawingUIItemHost.ResizeMode.None ? () => DrawingUIItemHost.ResizeMode.RecCorner : sender.GetCanResizeMode;
            }).SetDrawingPropertys(ImageDrawingPenBase.DrawingItemPropertyData.CreateColorAndSizeBig(Brushes.White, 12), (sender, data) =>
            {
                switch (data.PropertyType)
                {
                    case ImageDrawingPenBase.DrawingItemProperty.Color:
                        {
                            (sender.UIElement as TextBox).Foreground = (data.Value as Brush);
                        }
                        break;
                    case ImageDrawingPenBase.DrawingItemProperty.SizeBig:
                        {
                            (sender.UIElement as TextBox).FontSize = double.Parse(data.Value.ToString());
                        }
                        break;
                    default:
                        break;
                }
            });

            ImageDrawingPenCustom CreatePen<T>(string Title, ImageSource Icon, Func<T> DrawingItemCreate, Action<DrawingUIItemHost, T, RectangleByPoints> SetDrawingItemXY) where T : UIElement
            {
                DrawingUIItemHost target = null;
                ImageDrawingPenCustom c = null;
                c = new ImageDrawingPenCustom(() => Title, () => Icon, (X, Y, State) =>
                {
                    switch (State)
                    {
                        case ImageDrawingPenCustom.DrawingState.Start:
                            {
                                target = new DrawingUIItemHost();
                                target.IsFirstDrawing = true;
                                var t = target;
                                target.DrawingName = Title;
                                target.DrawingPenBase = c;
                                T DrawingItem = DrawingItemCreate();
                                target.PropertyChanged += (ss, ee) =>
                                {
                                    if (ee.PropertyName == "Location")
                                    {
                                        SetDrawingItemXY?.Invoke(t, DrawingItem, t.Location);
                                    }
                                };
                                target.SetLocation(new RectangleByPoints(X, Y, X, Y));
                                target.UIElement = DrawingItem;
                                foreach (var i in c.CanDrawingItemPropertys)
                                {
                                    c.DrawingStateChange(target, i);
                                }
                                LIST_DrawingItems.Items.Add(target);
                                EditDrawingItem = target;
                            }
                            break;
                        case ImageDrawingPenCustom.DrawingState.Move:
                            {
                                target?.SetLocation(new RectangleByPoints(target.Location.X1, target.Location.Y1, X, Y));
                            }
                            break;
                        case ImageDrawingPenCustom.DrawingState.End:
                            {
                                target.IsFirstDrawing = false;
                                target = null;
                            }
                            break;
                    }

                });
                c.DrawingProertyChanged += (ss, ee) =>
                {
                    if (this.EditDrawingItem != null)
                    {
                        foreach (var i in c.CanDrawingItemPropertys)
                        {
                            c.DrawingStateChange(EditDrawingItem, i);
                        }
                    }
                };
                LIST_DrawingPens.Items.Add(c);
                return c;
            }
            ImageDrawingPenCustom CreatePenMaxRange<T>(string Title, ImageSource Icon, Func<T> DrawingItemCreate, Action<DrawingUIItemHost, T, RectangleByPoints> SetDrawingItemXY) where T : UIElement
            {
                DrawingUIItemHost target = null;
                ImageDrawingPenCustom c = null;
                c = new ImageDrawingPenCustom(() => Title, () => Icon, (X, Y, State) =>
                {
                    switch (State)
                    {
                        case ImageDrawingPenCustom.DrawingState.Start:
                            {
                                target = new DrawingUIItemHost();
                                target.IsFirstDrawing = true;
                                var t = target;
                                target.DrawingName = Title;
                                target.DrawingPenBase = c;
                                T DrawingItem = DrawingItemCreate();
                                target.PropertyChanged += (ss, ee) =>
                                {
                                    if (ee.PropertyName == "Location")
                                    {
                                        SetDrawingItemXY?.Invoke(t, DrawingItem, t.Location);
                                    }
                                };
                                target.SetLocation(new RectangleByPoints(0, 0, RenderSize.Width, RenderSize.Height));
                                target.UIElement = DrawingItem;
                                foreach (var i in c.CanDrawingItemPropertys)
                                {
                                    c.DrawingStateChange(target, i);
                                }
                                LIST_DrawingItems.Items.Add(target);
                                EditDrawingItem = target;
                            }
                            break;
                        case ImageDrawingPenCustom.DrawingState.Move:
                            {
                                target?.SetLocation(new RectangleByPoints(target.Location.X1, target.Location.Y1, target.Location.X2, target.Location.Y2));
                            }
                            break;
                        case ImageDrawingPenCustom.DrawingState.End:
                            {
                                target.IsFirstDrawing = false;
                                target = null;
                            }
                            break;
                    }

                });
                c.DrawingProertyChanged += (ss, ee) =>
                {
                    if (this.EditDrawingItem != null)
                    {
                        foreach (var i in c.CanDrawingItemPropertys)
                        {
                            c.DrawingStateChange(EditDrawingItem, i);
                        }
                    }
                };
                LIST_DrawingPens.Items.Add(c);
                return c;
            }

        }
        #endregion

        private void ListBoxItem_DrawingItem_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            var data = (sender as ListBoxItem).DataContext as DrawingUIItemHost;
            ShowDrawingItemEdit(data);
        }
        private void ShowDrawingItemEdit(DrawingUIItemHost target)
        {
            //foreach (ImageDrawingPenCustom i in LIST_DrawingPens.Items)
            //{
            //    if (i.PenName == target.DrawingName)
            //    {
            //        LIST_DrawingPens.SelectedItem = i;
            //    }
            //}
            EditDrawingItem = target;
            foreach (var i in LIST_DrawingPens.Items)
            {
                if (i == EditDrawingItem.DrawingPenBase)
                {
                    LIST_DrawingPens.SelectedItem = i;
                    SelectPen(LIST_DrawingPens.SelectedItem as ImageDrawingPenBase);
                    break;
                }
            }
        }

        private void LIST_DrawingPens_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        private void LIST_DrawingPensItemMouseDown(object sender, MouseButtonEventArgs e)
        {
            var pen = (sender as ListBoxItem).DataContext as ImageDrawingPenBase;
            SelectPen(pen);
        }
        private void SelectPen(ImageDrawingPenBase pen)
        {
            this.InHandDrawingPen = pen;
            if (EditDrawingItem != null && EditDrawingItem.DrawingPenBase != InHandDrawingPen)
            {
                EditDrawingItem = null;
            }
            ShowDrawingPropertyHost();
        }

        private void BT_Print_Click(object sender, RoutedEventArgs e)
        {
            OnPrint();
        }
        public void OnPrint()
        {
            var range = ControlRecToImageRec(this.StepData_Start_SelectedRect.Value);
            var source = this.ImageSourceMemory;
            var g = gdi.Graphics.FromImage(source.BitmapSource);
            var drawingVisuals = ScreenUtils.SaveVisualToPng(LIST_DrawingItems);
            g.DrawImage(drawingVisuals, 0, 0, source.BitmapSource.Width, source.BitmapSource.Height);
            drawingVisuals.Dispose();
            g.Dispose();
            var s = source.Clip((int)range.X, (int)range.Y, (int)range.Width, (int)range.Height);
            Print?.Invoke(this, s);
            Dispose();
        }
    }
}
