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

namespace QScreenShot.DrawingItemControsl
{
    /// <summary>
    /// KLine.xaml 的交互逻辑
    /// </summary>
    public partial class KLine : UserControl
    {


        public Brush LineColor
        {
            get { return (Brush)GetValue(LineColorProperty); }
            set { SetValue(LineColorProperty, value); }
        }

        // Using a DependencyProperty as the backing store for LineColor.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LineColorProperty =
            DependencyProperty.Register("LineColor", typeof(Brush), typeof(KLine), new PropertyMetadata(Brushes.Red));



        public double Min
        {
            get { return (double)GetValue(MinProperty); }
            set { SetValue(MinProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Min.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MinProperty =
            DependencyProperty.Register("Min", typeof(double), typeof(KLine), new PropertyMetadata(0.0));



        public double Max
        {
            get { return (double)GetValue(MaxProperty); }
            set { SetValue(MaxProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Max.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MaxProperty =
            DependencyProperty.Register("Max", typeof(double), typeof(KLine), new PropertyMetadata(10.0));



        public double Now
        {
            get { return (double)GetValue(NowProperty); }
            set { SetValue(NowProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Now.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty NowProperty =
            DependencyProperty.Register("Now", typeof(double), typeof(KLine), new PropertyMetadata(6.0));



        public double Open
        {
            get { return (double)GetValue(OpenProperty); }
            set { SetValue(OpenProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Open.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty OpenProperty =
            DependencyProperty.Register("Open", typeof(double), typeof(KLine), new PropertyMetadata(5.0));






        public double LineSize
        {
            get { return (double)GetValue(LineSizeProperty); }
            set { SetValue(LineSizeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for LineSize.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LineSizeProperty =
            DependencyProperty.Register("LineSize", typeof(double), typeof(KLine), new PropertyMetadata(5.0));


        public bool IsAutoLineKLineColor
        {
            get { return (bool)GetValue(IsAutoLineKLineColorProperty); }
            set { SetValue(IsAutoLineKLineColorProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsAutoLineKLineColor.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsAutoLineKLineColorProperty =
            DependencyProperty.Register("IsAutoLineKLineColor", typeof(bool), typeof(KLine), new PropertyMetadata(true));




        public KLine()
        {
            InitializeComponent();
            Loaded += KLine_Loaded;
        }

        private void KLine_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateUI();
        }
        public void UpdateUI()
        {
            double top = ValueToPertiont(Max), now = ValueToPertiont(Now), open = ValueToPertiont(Open), min = ValueToPertiont(Min);
            double useBt = Math.Min(open, now), useTp = Math.Max(now, open);
            double t = (top - useTp) * RenderSize.Height, b = useBt * RenderSize.Height, c = RenderSize.Height - t - b;
            ROW_Top.Height = new GridLength(t);
            ROW_Bottom.Height = new GridLength(b);
            ROW_Center.Height = new GridLength(c);

            if (IsAutoLineKLineColor)
            {
                this.LineColor = this.Open > Now ? Brushes.Green : Brushes.Red;
                BD_Center.Background = Open > Now ? LineColor : null;
            }

            BD_Top.Width = LineSize;
            BD_Center.BorderThickness = new Thickness(LineSize);
            BD_Bottom.Width = LineSize;
        }
        double ValueToPertiont(double Value)
        {
            var range = Max - Min;
            return (Value - Min) / range;
        }


        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (e.Property == OpenProperty || e.Property == NowProperty || e.Property == MaxProperty || e.Property == MinProperty || e.Property == LineSizeProperty)
            {
                if (IsLoaded)
                {
                    UpdateUI();
                }
            }
            base.OnPropertyChanged(e);
        }

    }
}
