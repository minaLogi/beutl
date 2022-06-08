﻿using Avalonia.Controls;

using BeUtl.ViewModels.Editors;

using FluentAvalonia.UI.Controls;

namespace BeUtl.Views.Editors;

public sealed partial class ColorEditor : UserControl
{
    public ColorEditor()
    {
        InitializeComponent();
        colorPicker.FlyoutConfirmed += ColorPicker_ColorChanged;
    }

    private void ColorPicker_ColorChanged(ColorPickerButton sender, ColorButtonColorChangedEventArgs e)
    {
        Avalonia.Media.Color? newColor = e.NewColor;
        if (DataContext is ColorEditorViewModel vm && newColor.HasValue)
        {
            vm.SetValue(vm.Setter.Value, newColor.Value.ToMedia());
        }
    }
}
