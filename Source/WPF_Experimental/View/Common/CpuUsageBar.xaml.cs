using System.Windows;
using System.Windows.Controls;

namespace WPF_Experimental.View.Common
{
    public partial class CpuUsageBar : UserControl
    {
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(
                nameof(Value),
                typeof(double),
                typeof(CpuUsageBar),
                new PropertyMetadata(0.0, OnValueChanged));

        public double Value
        {
            get => (double)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public CpuUsageBar()
        {
            InitializeComponent();
            UpdateArc(Value);
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is CpuUsageBar control)
            {
                control.UpdateArc((double)e.NewValue);
            }
        }

        private void UpdateArc(double percentage)
        {
            if (ProgressArc == null) return;

            percentage = Math.Max(0, Math.Min(100, percentage));

            if (percentage >= 100) percentage = 99.99;

            double angle = (percentage / 100.0) * 360.0;
            double radians = (angle - 90) * (Math.PI / 180.0);

            double x = 50 + 45 * Math.Cos(radians);
            double y = 50 + 45 * Math.Sin(radians);

            ProgressArc.IsLargeArc = percentage > 50;
            ProgressArc.Point = new Point(x, y);
        }
    }
}
