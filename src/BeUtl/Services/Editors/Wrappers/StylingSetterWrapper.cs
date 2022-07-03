﻿using BeUtl.Animation;
using BeUtl.Styling;

namespace BeUtl.Services.Editors.Wrappers;

public interface IStylingSetterWrapper : IWrappedProperty
{

}

public sealed class StylingSetterWrapper<T> : IWrappedProperty<T>.IAnimatable, IStylingSetterWrapper
{
    public StylingSetterWrapper(Setter<T> setter)
    {
        AssociatedProperty = setter.Property;
        Tag = setter;

        Header = Observable.Return(setter.Property.Name);
    }

    public CoreProperty<T> AssociatedProperty { get; }

    public object Tag { get; }

    public IObservable<string> Header { get; }

    public IObservableList<Animation<T>> Animations => ((Setter<T>)Tag).Animations;

    IReadOnlyList<IAnimation> IWrappedProperty.IAnimatable.Animations => ((ISetter)Tag).Animations;

    public IObservable<T?> GetObservable()
    {
        return (Setter<T>)Tag;
    }

    public T? GetValue()
    {
        return ((Setter<T>)Tag).Value;
    }

    public void SetValue(T? value)
    {
        ((Setter<T>)Tag).Value = value;
    }
}