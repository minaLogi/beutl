﻿using System.Reactive.Linq;

using BEditorNext.ProjectSystem;

using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace BEditorNext.ViewModels.Editors;

public sealed class StringEditorViewModel : BaseEditorViewModel<string>
{
    public StringEditorViewModel(Setter<string> setter)
        : base(setter)
    {
        Value = setter.GetObservable()
            .Select(x => x ?? string.Empty)
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables)!;
    }

    public ReadOnlyReactivePropertySlim<string> Value { get; }
}