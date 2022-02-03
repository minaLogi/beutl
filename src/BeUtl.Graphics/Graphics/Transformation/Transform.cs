﻿using System.Diagnostics.CodeAnalysis;

using BeUtl.Styling;

namespace BeUtl.Graphics.Transformation;

public abstract class Transform : Styleable, IMutableTransform
{
    public event EventHandler? Invalidated;

    public static ITransform Identity { get; } = new IdentityTransform();

    public abstract Matrix Value { get; }

    public static bool TryParse(string s, [NotNullWhen(true)] out ITransform? transform)
    {
        return TryParse(s.AsSpan(), out transform);
    }

    public static bool TryParse(ReadOnlySpan<char> s, [NotNullWhen(true)] out ITransform? transform)
    {
        try
        {
            transform = Parse(s);
            return true;
        }
        catch
        {
            transform = null;
            return false;
        }
    }

    public static ITransform Parse(string s)
    {
        return Parse(s.AsSpan());
    }

    public static ITransform Parse(ReadOnlySpan<char> s)
    {
        return TransformParser.Parse(s);
    }

    protected static void AffectsRender<T>(
        CoreProperty? property1 = null,
        CoreProperty? property2 = null,
        CoreProperty? property3 = null,
        CoreProperty? property4 = null)
        where T : Transform
    {
        Action<CorePropertyChangedEventArgs> onNext = e =>
        {
            if (e.Sender is T s)
            {
                s.RaiseInvalidated();
            }
        };

        property1?.Changed.Subscribe(onNext);
        property2?.Changed.Subscribe(onNext);
        property3?.Changed.Subscribe(onNext);
        property4?.Changed.Subscribe(onNext);
    }

    protected static void AffectsRender<T>(params CoreProperty[] properties)
        where T : Transform
    {
        foreach (CoreProperty? item in properties)
        {
            item.Changed.Subscribe(e =>
            {
                if (e.Sender is T s)
                {
                    s.RaiseInvalidated();
                }
            });
        }
    }

    protected void RaiseInvalidated()
    {
        Invalidated?.Invoke(this, EventArgs.Empty);
    }

    private sealed class IdentityTransform : ITransform
    {
        public Matrix Value => Matrix.Identity;

        public event EventHandler? Invalidated
        {
            add { }
            remove { }
        }
    }
}