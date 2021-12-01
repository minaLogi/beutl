﻿using System.Collections;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Generators;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;
using BEditorNext.Controls.Extensions;
using BEditorNext.Controls.Generators;

namespace BEditorNext.Controls;

public partial class DraggableTabView : TabControl
{
    private Button AdderButton;
    internal double lastselectindex = 0;
    private readonly Animation _animation = new()
    {
        Easing = new SplineEasing(0.1, 0.9, 0.2, 1.0),
        Children =
        {
            new KeyFrame
            {
                Setters =
                {
                    new Setter(OpacityProperty, 0.0),
                    new Setter(TranslateTransform.YProperty, 28)
                },
                Cue = new Cue(0d)
            },
            new KeyFrame
            {
                Setters =
                {
                    new Setter(OpacityProperty, 1d),
                    new Setter(TranslateTransform.YProperty, 0.0)
                },
                Cue = new Cue(1d)
            }
        },
        Duration = TimeSpan.FromSeconds(0.67),
        FillMode = FillMode.Forward
    };
    private Border b_;
    private Grid g_;
    private Grid _gridHost;

    static DraggableTabView()
    {
        SelectionModeProperty.OverrideDefaultValue<DraggableTabView>(SelectionMode.Single);
        SelectedItemProperty.Changed.Subscribe(async x =>
        {
            if (x.Sender is DraggableTabView sender && sender.TransitionIsEnabled && sender._gridHost != null)
            {
                await sender._animation.RunAsync(sender._gridHost);
                sender._gridHost.Opacity = 1;
            }
        });
    }

    protected void AdderButtonClicked(object sender, RoutedEventArgs e)
    {
        var e_ = new RoutedEventArgs(ClickOnAddingButtonEvent);
        RaiseEvent(e_);
        e_.Handled = true;
    }

    protected override IItemContainerGenerator CreateItemContainerGenerator()
        => new DraggableTabItemContainerGenerator(this, ContentControl.ContentProperty, ContentControl.ContentTemplateProperty);

    protected override void OnPropertyChanged<T>(AvaloniaPropertyChangedEventArgs<T> change)
    {
        base.OnPropertyChanged(change);

        if (SelectedItem == null)
        {
            double d = ((double)ItemCount / 2);
            if (lastselectindex < d & ItemCount != 0)
            {
                SelectedItem = (Items as IList).OfType<object>().FirstOrDefault();
            }
            else if (lastselectindex >= d & ItemCount != 0)
            {
                SelectedItem = (Items as IList).OfType<object>().LastOrDefault();
            }
        }
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        AdderButton = e.NameScope.Find<Button>("PART_AdderButton");

        AdderButton.Click += AdderButtonClicked;

        b_ = e.NameScope.Find<Border>("PART_InternalBorder");
        g_ = e.NameScope.Find<Grid>("PART_InternalGrid");
        _gridHost = e.NameScope.Find<Grid>("PART_GridHost");

        PropertyChanged += AuraTabView_PropertyChanged;
    }

    private void AuraTabView_PropertyChanged(object sender, AvaloniaPropertyChangedEventArgs e)
    {
        WidthRemainingSpace = g_.Bounds.Width;
        HeightRemainingSpace = g_.Bounds.Height;
    }

    public void AddTab(DraggableTabItem ItemToAdd, bool isSelected = true)
    {
        TabControlExtensions.AddTab(this, ItemToAdd, isSelected);
    }
}