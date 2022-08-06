﻿using BeUtl.Services.Editors.Wrappers;

using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace BeUtl.ViewModels.Editors;

public abstract class BaseEditorViewModel : IDisposable
{
    protected CompositeDisposable Disposables = new();
    private bool _disposedValue;

    protected BaseEditorViewModel(IWrappedProperty property)
    {
        WrappedProperty = property;

        Header = WrappedProperty.Header
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);

        IObservable<bool> hasAnimation = property is IWrappedProperty.IAnimatable anm
            ? anm.HasAnimation
            : Observable.Return(false);

        IObservable<bool>? observable;
        if (property.AssociatedProperty is IStaticProperty { CanWrite: false })
        {
            observable = Observable.Return(false);
        }
        else
        {
            observable = hasAnimation.Select(x => !x);
        }

        CanEdit = observable
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);

        HasAnimation = hasAnimation
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);
    }

    ~BaseEditorViewModel()
    {
        if (!_disposedValue)
            Dispose(false);
    }

    public IWrappedProperty WrappedProperty { get; }

    public bool CanReset => WrappedProperty.GetDefaultValue() != null;

    public ReadOnlyReactivePropertySlim<string?> Header { get; }

    public ReadOnlyReactivePropertySlim<bool> CanEdit { get; }

    public ReadOnlyReactivePropertySlim<bool> HasAnimation { get; }

    public bool IsAnimatable => WrappedProperty.GetMetadataExt<CorePropertyMetadata>()?.PropertyFlags.HasFlag(PropertyFlags.Animatable) == true;

    public bool IsStylingSetter => WrappedProperty is IStylingSetterWrapper;

    public void Dispose()
    {
        if (!_disposedValue)
        {
            Dispose(true);
            _disposedValue = true;
            GC.SuppressFinalize(this);
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        Disposables.Dispose();
    }
}

public abstract class BaseEditorViewModel<T> : BaseEditorViewModel
{
    protected BaseEditorViewModel(IWrappedProperty<T> property)
        : base(property)
    {
    }

    public new IWrappedProperty<T> WrappedProperty => (IWrappedProperty<T>)base.WrappedProperty;

    public void Reset()
    {
        object? defaultValue = WrappedProperty.GetDefaultValue();
        if (defaultValue != null)
        {
            SetValue(WrappedProperty.GetValue(), (T?)defaultValue);
        }
    }

    public void SetValue(T? oldValue, T? newValue)
    {
        if (!EqualityComparer<T>.Default.Equals(oldValue, newValue))
        {
            CommandRecorder.Default.DoAndPush(new SetCommand(WrappedProperty, oldValue, newValue));
        }
    }

    private sealed class SetCommand : IRecordableCommand
    {
        private readonly IWrappedProperty<T> _setter;
        private readonly T? _oldValue;
        private readonly T? _newValue;

        public SetCommand(IWrappedProperty<T> setter, T? oldValue, T? newValue)
        {
            _setter = setter;
            _oldValue = oldValue;
            _newValue = newValue;
        }

        public void Do()
        {
            _setter.SetValue(_newValue);
        }

        public void Redo()
        {
            Do();
        }

        public void Undo()
        {
            _setter.SetValue(_oldValue);
        }
    }
}
