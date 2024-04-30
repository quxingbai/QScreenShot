using System;
using System.Collections.Generic;
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
using static QScreenShot.ScreenShotControl.ImageDrawingPenBase;

namespace QScreenShot
{
    public class DrawingItemPropertyStateSetter : ContentControl
    {



        public DrawingItemPropertyData PropertyData
        {
            get { return (DrawingItemPropertyData)GetValue(PropertyDataProperty); }
            set { SetValue(PropertyDataProperty, value); }
        }

        // Using a DependencyProperty as the backing store for PropertyData.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PropertyDataProperty =
            DependencyProperty.Register("PropertyData", typeof(DrawingItemPropertyData), typeof(DrawingItemPropertyStateSetter), new PropertyMetadata(null));



        public IEnumerable<Brush> CanSelectColors => new Brush[] {
            Brushes.Red,
            Brushes.Black,
            Brushes.White,
            Brushes.Yellow,
            Brushes.Blue,
            Brushes.Green,
            Brushes.Aqua,
            Brushes.MidnightBlue,
            Brushes.Salmon,
        };
        public DrawingItemPropertyStateSetter()
        {
        }
        static DrawingItemPropertyStateSetter()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(DrawingItemPropertyStateSetter), new FrameworkPropertyMetadata(typeof(DrawingItemPropertyStateSetter)));
        }
        public override void OnApplyTemplate()
        {
            LoadControl();
            base.OnApplyTemplate();
        }
        private Slider FontSizeSlider => (GetTemplateChild("VALUE_FontSize") as Slider);
        private Slider FontSizeBigSlider => (GetTemplateChild("VALUE_FontSizeBig") as Slider);
        private ListBox ColorListBox => (GetTemplateChild("VALUE_Color") as ListBox);
        private void LoadControl()
        {

            Change();

            void Change()
            {
                sSetBinding(FontSizeBigSlider, Slider.ValueProperty);
                sSetBinding(FontSizeSlider, Slider.ValueProperty);
                sSetBinding(ColorListBox, ListBox.SelectedItemProperty);
            }
            void sSetBinding(FrameworkElement ele,DependencyProperty valProp)
            {
                ele.DataContext = this;
                ele.SetBinding(valProp, new Binding("PropertyData.Value"));
            }
        }


        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (e.Property == PropertyDataProperty)
            {
            }
            base.OnPropertyChanged(e);
        }
        protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseDown(e);
        }
    }
}
