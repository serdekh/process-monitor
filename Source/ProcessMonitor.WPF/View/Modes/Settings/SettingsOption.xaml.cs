using System.Windows;
using System.Windows.Controls;

namespace ProcessMonitor.WPF.View.Modes.Settings;

public partial class SettingsOption : UserControl
{
    public SettingsOption()
    {
        InitializeComponent();
    }

    public static readonly RoutedEvent PropertyValueChangedEvent = EventManager.RegisterRoutedEvent(
        "PropertyValueChanged", 
        RoutingStrategy.Bubble,
        typeof(RoutedEventHandler), 
        typeof(SettingsOption));

    public event RoutedEventHandler PropertyValueChanged
    {
        add => AddHandler(PropertyValueChangedEvent, value);
        remove => RemoveHandler(PropertyValueChangedEvent, value);
    }

    public static readonly DependencyProperty PropertyNameProperty =
        DependencyProperty.Register("PropertyName", typeof(string), typeof(SettingsOption), 
            new PropertyMetadata("Option"));

    public static readonly DependencyProperty PropertyValueProperty =
        DependencyProperty.Register("PropertyValue", typeof(string), typeof(SettingsOption), 
            new PropertyMetadata(string.Empty, OnPropertyValueChanged));

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

    private static void OnPropertyValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SettingsOption control)
        {
            RoutedEventArgs args = new RoutedEventArgs(PropertyValueChangedEvent);
            control.RaiseEvent(args);
        }
    }
}