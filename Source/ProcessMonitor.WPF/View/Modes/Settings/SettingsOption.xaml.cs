using System.Windows;
using System.Windows.Controls;

namespace ProcessMonitor.WPF.View.Modes.Settings;

public partial class SettingsOption : UserControl
{
    public SettingsOption()
    {
        InitializeComponent();
    }

    public static readonly DependencyProperty PropertyNameProperty =
        DependencyProperty.Register("PropertyName", typeof(string), typeof(SettingsOption), new PropertyMetadata());

    public static readonly DependencyProperty PropertyValueProperty =
        DependencyProperty.Register("PropertyValue", typeof(string), typeof(SettingsOption), new PropertyMetadata());

    public string PropertyName
    {
        get => (string)GetValue(PropertyNameProperty);
        set => SetValue(PropertyNameProperty, value);
    }

    public string PropertyValue
    {
        get => (string)GetValue(PropertyValueProperty);
        set => SetValue(PropertyValueProperty, value);
    }
}