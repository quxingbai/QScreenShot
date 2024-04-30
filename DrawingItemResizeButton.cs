using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace QScreenShot
{
    public class DrawingItemResizeButton : Border
    {
        public enum ResizeDirection
        {
            LeftTop, TopCenter, RightTop,
            LeftCenter, Center, RightCenter,
            LeftBottom, BottomCenter, RightBottom,
        }


        public ResizeDirection Direction
        {
            get { return (ResizeDirection)GetValue(DirectionProperty); }
            set { SetValue(DirectionProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Direction.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DirectionProperty =
            DependencyProperty.Register("Direction", typeof(ResizeDirection), typeof(DrawingItemResizeButton), new PropertyMetadata(ResizeDirection.LeftTop));

        public ScreenShotControl.DrawingUIItemHost Target
        {
            get { return (ScreenShotControl.DrawingUIItemHost)GetValue(TargetProperty); }
            set { SetValue(TargetProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Target.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TargetProperty =
            DependencyProperty.Register("Target", typeof(ScreenShotControl.DrawingUIItemHost), typeof(DrawingItemResizeButton), new PropertyMetadata(null));




        public object CustomContent
        {
            get { return (object)GetValue(CustomContentProperty); }
            set { SetValue(CustomContentProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CustomContent.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CustomContentProperty =
            DependencyProperty.Register("CustomContent", typeof(object), typeof(DrawingItemResizeButton), new PropertyMetadata(null));


        static DrawingItemResizeButton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(DrawingItemResizeButton), new FrameworkPropertyMetadata(typeof(DrawingItemResizeButton)));
        }
        protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            if (Target == null) return;
            e.Handled = true;
            //var DownPos = e.GetPosition(this);
            ScreenUtils.WinApi.GetCursorPos(out var DownPos);
            var StartSize = Target.Location;
            double X = Target.Location.X1, Y = Target.Location.Y1, X2 = Target.Location.X2, Y2 = Target.Location.Y2;
            ScreenUtils.CreateMouseDownWaitUpTick(p =>
            {
                double MoveX = p.X - DownPos.X, MoveY = p.Y - DownPos.Y;
                switch (this.Direction)
                {
                    case ResizeDirection.LeftTop:
                        {
                            X = StartSize.X1 + p.X - DownPos.X;
                            Y = StartSize.Y1 + p.Y - DownPos.Y;
                        }
                        break;
                    case ResizeDirection.TopCenter:
                        {
                            Y = StartSize.Y1 + p.Y - DownPos.Y;
                        }
                        break;
                    case ResizeDirection.RightTop:
                        {
                            X2 = StartSize.X2 + p.X - DownPos.X;
                            Y = StartSize.Y1 + p.Y - DownPos.Y;
                        }
                        break;
                    case ResizeDirection.LeftCenter:
                        {
                            X = StartSize.X1 + p.X - DownPos.X;
                        }
                        break;
                    case ResizeDirection.Center:
                        {
                            X = StartSize.X1 + p.X - DownPos.X;
                            Y = StartSize.Y1 + p.Y - DownPos.Y;
                            X2 = StartSize.X2 + p.X - DownPos.X;
                            Y2 = StartSize.Y2 + p.Y - DownPos.Y;
                        }
                        break;
                    case ResizeDirection.RightCenter:
                        {
                            X2 = StartSize.X2 + p.X - DownPos.X;
                        }
                        break;
                    case ResizeDirection.LeftBottom:
                        {
                            X = StartSize.X1 + p.X - DownPos.X;
                            Y2 = StartSize.Y2 + p.Y - DownPos.Y;
                        }
                        break;
                    case ResizeDirection.BottomCenter:
                        {
                            Y2 = StartSize.Y2 + p.Y - DownPos.Y;
                        }
                        break;
                    case ResizeDirection.RightBottom:
                        {
                            X2 = StartSize.X2 + p.X - DownPos.X;
                            Y2 = StartSize.Y2 + p.Y - DownPos.Y;
                        }
                        break;
                    default:
                        break;
                }
                Target.SetLocation(new ScreenShotControl.RectangleByPoints(X, Y, X2, Y2));
            }, p =>
            {
                
            });
            base.OnPreviewMouseLeftButtonDown(e);
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (e.Property == CustomContentProperty)
            {
                this.Child = new ContentControl()
                {
                    Content = this.CustomContent
                };
            }
            base.OnPropertyChanged(e);
        }

    }
}
