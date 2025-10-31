using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace ComicReader.Views.Controls
{
    public partial class StatCard : UserControl
    {
        public StatCard()
        {
            InitializeComponent();
            // Respect user's reduce-motion preference
            if (SystemParameters.ClientAreaAnimation)
            {
                RootBorder.MouseEnter += RootBorder_MouseEnter;
                RootBorder.MouseLeave += RootBorder_MouseLeave;
            }
        }

        private void RootBorder_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            var daX = new DoubleAnimation(1.0, TimeSpan.FromMilliseconds(180)) { EasingFunction = new QuadraticEase() };
            var daY = new DoubleAnimation(1.0, TimeSpan.FromMilliseconds(180)) { EasingFunction = new QuadraticEase() };
            CardScale.BeginAnimation(ScaleTransform.ScaleXProperty, daX);
            CardScale.BeginAnimation(ScaleTransform.ScaleYProperty, daY);
        }

        private void RootBorder_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            var daX = new DoubleAnimation(1.03, TimeSpan.FromMilliseconds(180)) { EasingFunction = new QuadraticEase() };
            var daY = new DoubleAnimation(1.03, TimeSpan.FromMilliseconds(180)) { EasingFunction = new QuadraticEase() };
            CardScale.BeginAnimation(ScaleTransform.ScaleXProperty, daX);
            CardScale.BeginAnimation(ScaleTransform.ScaleYProperty, daY);
        }

        public static readonly DependencyProperty IconProperty = DependencyProperty.Register(
            nameof(Icon), typeof(ImageSource), typeof(StatCard), new PropertyMetadata(null));

        public ImageSource Icon
        {
            get => (ImageSource)GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
            nameof(Value), typeof(string), typeof(StatCard), new PropertyMetadata(string.Empty));

        public string Value
        {
            get => (string)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(
            nameof(Label), typeof(string), typeof(StatCard), new PropertyMetadata(string.Empty));

        public string Label
        {
            get => (string)GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }
    }
}
