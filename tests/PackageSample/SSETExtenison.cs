﻿using System.Diagnostics.CodeAnalysis;

using Avalonia.Controls;
using Avalonia.Layout;

using BeUtl;
using BeUtl.Framework;

using Reactive.Bindings;

namespace PackageSample;

// SampleSceneEditorTabExtenison
public sealed class SSETExtenison : ToolTabExtension
{
    public override bool CanMultiple => true;

    public override string Name => "Sample tab";

    public override string DisplayName => "Sample tab";

    public override ResourceReference<string>? Header => "S.SamplePackage.SSETExtension";

    public override bool TryCreateContent(IEditorContext editorContext, [NotNullWhen(true)] out IControl? control)
    {
        control = new TextBlock()
        {
            Text = "Hello world!",
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        return true;
    }

    public override bool TryCreateContext(IEditorContext editorContext, [NotNullWhen(true)] out IToolContext? context)
    {
        context = new Context(this);
        return true;
    }

    private sealed class Context : IToolContext
    {
        public Context(ToolTabExtension extension)
        {
            Extension = extension;
            IsSelected = new ReactiveProperty<bool>();
        }

        public ToolTabExtension Extension { get; }

        public IReactiveProperty<bool> IsSelected { get; }

        public IReadOnlyReactiveProperty<string> Header { get; } = new ReactivePropertySlim<string>("Sample tab");

        public TabPlacement Placement => TabPlacement.Bottom;

        public void Dispose()
        {
        }
    }
}
