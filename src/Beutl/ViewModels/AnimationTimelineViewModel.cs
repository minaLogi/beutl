﻿using System.Collections;
using System.Text.Json.Nodes;

using Avalonia;

using Beutl.Animation;
using Beutl.Animation.Easings;
using Beutl.Commands;
using Beutl.Framework;
using Beutl.ProjectSystem;
using Beutl.Services.Editors.Wrappers;
using Beutl.Services.PrimitiveImpls;
using Beutl.ViewModels.Editors;

using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace Beutl.ViewModels;

public sealed class AnimationTimelineViewModel : IDisposable, IToolContext
{
    private readonly CompositeDisposable _disposables = new();
    private readonly ReactivePropertySlim<Layer?> _layerObservable = new();

    public AnimationTimelineViewModel(
        IAbstractAnimatableProperty setter,
        ITimelineOptionsProvider optionsProvider)
    {
        WrappedProperty = setter;
        OptionsProvider = optionsProvider;
        Scene = optionsProvider.Scene;

        BorderMargin = LayerObservable
            .SelectMany(x => x?.GetObservable(Layer.StartProperty) ?? Observable.Return(TimeSpan.Zero))
            .CombineLatest(OptionsProvider.Scale)
            .Select(item => new Thickness(item.First.ToPixel(item.Second), 0, 0, 0))
            .ToReadOnlyReactivePropertySlim()
            .AddTo(_disposables);

        Width = LayerObservable
            .SelectMany(x => x?.GetObservable(Layer.LengthProperty) ?? Observable.Return(TimeSpan.Zero))
            .CombineLatest(OptionsProvider.Scale)
            .Select(item => item.First.ToPixel(item.Second))
            .ToReadOnlyReactivePropertySlim()
            .AddTo(_disposables);

        Color = LayerObservable
            .SelectMany(x => x?.GetObservable(Layer.AccentColorProperty) ?? Observable.Return(default(Media.Color)))
            .Select(c => c.ToAvalonia())
            .ToReadOnlyReactivePropertySlim()
            .AddTo(_disposables);

        PanelWidth = Scene.GetObservable(Scene.DurationProperty)
            .CombineLatest(OptionsProvider.Scale)
            .Select(item => item.First.ToPixel(item.Second))
            .ToReadOnlyReactivePropertySlim()
            .AddTo(_disposables);

        SeekBarMargin = Scene.GetObservable(Scene.CurrentFrameProperty)
            .CombineLatest(OptionsProvider.Scale)
            .Select(item => new Thickness(item.First.ToPixel(item.Second), 0, 0, 0))
            .ToReadOnlyReactivePropertySlim()
            .AddTo(_disposables);

        EndingBarMargin = PanelWidth.Select(p => new Thickness(p, 0, 0, 0))
            .ToReadOnlyReactivePropertySlim()
            .AddTo(_disposables);

        Header = LayerObservable
            .SelectMany(x => x?.GetObservable(CoreObject.NameProperty) ?? Observable.Return(string.Empty))
            .Select(n => string.IsNullOrWhiteSpace(n) ? WrappedProperty.Property.Name : $"{n} / {WrappedProperty.Property.Name}")
            .ToReadOnlyReactivePropertySlim(WrappedProperty.Property.Name)
            .AddTo(_disposables);
    }

    public Scene Scene { get; }

    public Layer? Layer
    {
        get => _layerObservable.Value;
        init => _layerObservable.Value = value;
    }

    public IAbstractAnimatableProperty WrappedProperty { get; }

    public ITimelineOptionsProvider OptionsProvider { get; }

    public IReadOnlyReactiveProperty<Layer?> LayerObservable => _layerObservable;

    public ReadOnlyReactivePropertySlim<Thickness> BorderMargin { get; }

    public ReadOnlyReactivePropertySlim<double> Width { get; }

    public ReadOnlyReactivePropertySlim<Avalonia.Media.Color> Color { get; }

    public ReadOnlyReactivePropertySlim<double> PanelWidth { get; }

    public ReadOnlyReactivePropertySlim<Thickness> SeekBarMargin { get; }

    public ReadOnlyReactivePropertySlim<Thickness> EndingBarMargin { get; }

    public IReactiveProperty<bool> IsSelected { get; } = new ReactivePropertySlim<bool>();

    public ToolTabExtension Extension => AnimationTimelineTabExtension.Instance;

    public IReadOnlyReactiveProperty<string> Header { get; }

    public ToolTabExtension.TabPlacement Placement => ToolTabExtension.TabPlacement.Bottom;

    public void Dispose()
    {
        _layerObservable.Dispose();
        _disposables.Dispose();
    }

    public void AddAnimation(Easing easing)
    {
        CoreProperty? property = WrappedProperty.Property;
        Type type = typeof(AnimationSpan<>).MakeGenericType(property.PropertyType);

        if (WrappedProperty.Animation.Children is IList list
            && Activator.CreateInstance(type) is IAnimationSpan animation)
        {
            animation.Easing = easing;
            animation.Duration = TimeSpan.FromSeconds(2);
            object? value = WrappedProperty.GetValue();

            if (value != null)
            {
                animation.Previous = value;
                animation.Next = value;
            }

            list.BeginRecord()
                .Add(animation)
                .ToCommand()
                .DoAndRecord(CommandRecorder.Default);
        }
    }

    public void ReadFromJson(JsonNode json)
    {
    }

    public void WriteToJson(ref JsonNode json)
    {
    }
}