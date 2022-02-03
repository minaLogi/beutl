﻿using System.Reactive.Disposables;
using System.Reactive.Linq;

using Avalonia;
using Avalonia.Controls;

using BeUtl.ProjectSystem;

using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace BeUtl.ViewModels.Editors;

public abstract class BaseEditorViewModel : IDisposable
{
    protected CompositeDisposable Disposables = new();
    private bool _disposedValue;

    protected BaseEditorViewModel(IPropertyInstance setter)
    {
        Setter = setter;

        IOperationPropertyMetadata metadata = setter.Property.GetMetadata<IOperationPropertyMetadata>(setter.Parent.GetType());
        ResourceReference<string> reference = metadata.Header;

        if (reference.Key != null)
        {
            Header = Application.Current!.GetResourceObservable(reference.Key)
                .Select(i => (string?)i ?? Setter.Property.Name)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
        }
        else
        {
            Header = Observable.Return(Setter.Property.Name)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
        }
    }

    ~BaseEditorViewModel()
    {
        if (!_disposedValue)
            Dispose(false);
    }

    public IPropertyInstance Setter { get; }

    public bool CanReset => Setter.GetDefaultValue() != null;

    public ReadOnlyReactivePropertySlim<string?> Header { get; }

    public bool IsAnimatable => Setter is IAnimatablePropertyInstance;

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
    protected BaseEditorViewModel(PropertyInstance<T> setter)
        : base(setter)
    {
    }

    public new PropertyInstance<T> Setter => (PropertyInstance<T>)base.Setter;

    public void Reset()
    {
        object? defaultValue = Setter.GetDefaultValue();
        if (defaultValue != null)
        {
            SetValue(Setter.Value, (T?)defaultValue);
        }
    }

    public void SetValue(T? oldValue, T? newValue)
    {
        if (!EqualityComparer<T>.Default.Equals(oldValue, newValue))
        {
            CommandRecorder.Default.DoAndPush(new SetCommand(Setter, oldValue, newValue));
        }
    }

    private sealed class SetCommand : IRecordableCommand
    {
        private readonly PropertyInstance<T> _setter;
        private readonly T? _oldValue;
        private readonly T? _newValue;

        public SetCommand(PropertyInstance<T> setter, T? oldValue, T? newValue)
        {
            _setter = setter;
            _oldValue = oldValue;
            _newValue = newValue;
        }

        public void Do()
        {
            _setter.Value = _newValue;
        }

        public void Redo()
        {
            Do();
        }

        public void Undo()
        {
            _setter.Value = _oldValue;
        }
    }
}