using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace QScreenShot
{
    public class ControlUtils
    {
        public class ClickEventSetter<T> :IDisposable where T : FrameworkElement
        {
            public event Action<T, System.Windows.Input.MouseButtonEventArgs> Click;
            private Point? LastMouseDownPoint = null;
            private T Element = null;
            private int MaxIfRange = 1;
            private Action DisposeAct = null;
            /// <summary>
            /// 创建一个 点击事件
            /// </summary>
            /// <param name="element">目标</param>
            /// <param name="IsUsePreviewEvent">是否使用Preview类型事件</param>
            /// <param name="MaxIfRange">最大的偏移判定范围</param>
            public ClickEventSetter(T element, bool IsUsePreviewEvent=false, int MaxIfRange = 1)
            {
                this.MaxIfRange = MaxIfRange;
                Element = element;
                if (IsUsePreviewEvent)
                {
                    element.PreviewMouseDown += Element_MouseDown;
                    element.PreviewMouseUp += Element_MouseUp;
                    DisposeAct = () =>
                    {
                        element.PreviewMouseDown -= Element_MouseDown;
                        element.PreviewMouseUp -= Element_MouseUp;
                    };
                }
                else
                {
                    element.MouseDown += Element_MouseDown;
                    element.MouseUp += Element_MouseUp;
                    DisposeAct = () =>
                    {
                        element.MouseDown -= Element_MouseDown;
                        element.MouseUp -= Element_MouseUp;
                    };
                }
            }

            private void Element_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
            {
                if (LastMouseDownPoint != null)
                {
                    Point p = LastMouseDownPoint.Value;
                    var n = e.GetPosition(Element);
                    p.Offset(-n.X, -n.Y);
                    if (Math.Abs(p.X) <= MaxIfRange && Math.Abs(p.Y) <= MaxIfRange)
                    {
                        this.Click?.Invoke(Element, e);
                    }
                }
            }

            private void Element_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
            {
                LastMouseDownPoint = e.GetPosition(Element);
            }

            public void Dispose()
            {
                DisposeAct.Invoke();
            }
        }
    }
}
