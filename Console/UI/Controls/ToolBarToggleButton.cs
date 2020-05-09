using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace Wokhan.WindowsFirewallNotifier.Console.UI.Controls
{
    public class ToolBarToggleButton : ToggleButton
    {
        public bool IsToggle { get; } = true;

        [Bindable(true)]
        public string Text { get; set; }
        public static DependencyProperty TextProperty = DependencyProperty.Register("Text", typeof(string), typeof(ToolBarToggleButton), new PropertyMetadata(null));

        [Bindable(true)]
        public object Image { get; set; }
        public static DependencyProperty ImageProperty = DependencyProperty.Register("Image", typeof(object), typeof(ToolBarToggleButton), new PropertyMetadata(null));

        [Bindable(true)]
        public Brush ImageBrush { get; set; }
        public static DependencyProperty ImageBrushProperty = DependencyProperty.Register(nameof(ImageBrush), typeof(Brush), typeof(ToolBarToggleButton), new PropertyMetadata(null));

        [Bindable(true)]
        public Orientation Orientation { get; set; }

        static ToolBarToggleButton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ToolBarToggleButton), new FrameworkPropertyMetadata(typeof(ToolBarToggleButton)));
        }
    }
}
