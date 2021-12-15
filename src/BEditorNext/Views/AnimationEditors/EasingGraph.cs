﻿using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;

using BEditorNext.Animation.Easings;

namespace BEditorNext.Views.AnimationEditors;

public sealed class EasingGraph : TemplatedControl
{
    public static readonly StyledProperty<Easing?> EasingProperty
        = AvaloniaProperty.Register<EasingGraph, Easing?>("Easing");

    private readonly Pen _pen = new()
    {
        Brush = (IBrush)Application.Current.FindResource("TextControlForeground")!,
        LineJoin = PenLineJoin.Round,
        LineCap = PenLineCap.Round,
        Thickness = 2.5,
    };

    static EasingGraph()
    {
        AffectsRender<EasingGraph>(EasingProperty);
    }

    public Easing? Easing
    {
        get => GetValue(EasingProperty);
        set => SetValue(EasingProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        Easing? easing = Easing;
        Size size = Bounds.Size;

        if (easing is null) return;

        for (int i = 0; i < 100; i++)
        {
            double value = Math.Abs(easing.Ease(i / 100f) - 1);
            double after = Math.Abs(easing.Ease((i + 1) / 100f) - 1);

            context.DrawLine(
                _pen,
                new Point(i / 100d * size.Width, value * size.Height),
                new Point((i + 1) / 100d * size.Width, after * size.Height));
        }
    }
}
