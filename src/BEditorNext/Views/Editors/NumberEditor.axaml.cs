using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

using BEditorNext.ViewModels.Editors;

namespace BEditorNext.Views.Editors;

public partial class NumberEditor : UserControl
{
    public NumberEditor()
    {
        InitializeComponent();
    }
}

public class NumberEditor<T> : NumberEditor
    where T : struct
{
    private T _oldValue;

    public NumberEditor()
    {
        textBox.GotFocus += TextBox_GotFocus;
        textBox.LostFocus += TextBox_LostFocus;
        textBox.KeyDown += TextBox_KeyDown;
        textBox.AddHandler(PointerWheelChangedEvent, TextBox_PointerWheelChanged, RoutingStrategies.Tunnel);
    }

    private void TextBox_GotFocus(object? sender, GotFocusEventArgs e)
    {
        if (DataContext is not BaseNumberEditorViewModel<T> vm) return;

        _oldValue = vm.Setter.Value;
    }

    private void TextBox_LostFocus(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not BaseNumberEditorViewModel<T> vm) return;

        if (vm.TryParse(textBox.Text, out T newValue))
        {
            vm.SetValue(_oldValue, newValue);
        }
    }

    private void TextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not BaseNumberEditorViewModel<T> vm) return;

        if (vm.TryParse(textBox.Text, out T value))
        {
            vm.Setter.Value = vm.Clamp(value, vm.Minimum, vm.Maximum);
        }
    }

    private void TextBox_PointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (DataContext is not BaseNumberEditorViewModel<T> vm) return;

        if (textBox.IsKeyboardFocusWithin && vm.TryParse(textBox.Text, out T value))
        {
            int increment = 10;

            if (e.KeyModifiers == KeyModifiers.Shift)
            {
                increment = 1;
            }

            if (e.Delta.Y < 0)
            {
                value = vm.Decrement(value, increment);
            }
            else
            {
                value = vm.Increment(value, increment);
            }

            vm.Setter.Value = vm.Clamp(value, vm.Minimum, vm.Maximum);

            e.Handled = true;
        }
    }
}