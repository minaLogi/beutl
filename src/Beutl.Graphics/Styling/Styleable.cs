﻿using System.Collections;
using System.Collections.Specialized;
using System.Text.Json.Nodes;

using Beutl.Animation;
using Beutl.Collections;

namespace Beutl.Styling;

public abstract class Styleable : Animatable, IStyleable, IModifiableHierarchical
{
    public static readonly CoreProperty<Styles> StylesProperty;
    private readonly Styles _styles;
    private IStyleInstance? _styleInstance;

    static Styleable()
    {
        StylesProperty = ConfigureProperty<Styles, Styleable>(nameof(Styles))
            .Accessor(o => o.Styles, (o, v) => o.Styles = v)
            .Register();

        HierarchicalParentProperty = ConfigureProperty<IHierarchical?, Styleable>(nameof(HierarchicalParent))
            .Accessor(o => o.HierarchicalParent, (o, v) => o.HierarchicalParent = v)
            .Register();
    }

    protected Styleable()
    {
        _styles = new();
        _styles.Attached += item =>
        {
            item.Invalidated += Style_Invalidated;
        };
        _styles.Detached += item =>
        {
            item.Invalidated -= Style_Invalidated;
        };
        _styles.CollectionChanged += Style_Invalidated;

        _root = this as IHierarchicalRoot;
        _hierarchicalChildren = new CoreList<IHierarchical>()
        {
            ResetBehavior = ResetBehavior.Remove
        };
        _hierarchicalChildren.CollectionChanged += HierarchicalChildrenCollectionChanged;
    }

    private void Style_Invalidated(object? sender, EventArgs e)
    {
        _styleInstance = null;
    }

    [ShouldSerialize(false)]
    public Styles Styles
    {
        get => _styles;
        set
        {
            if (_styles != value)
            {
                _styles.Replace(value);
            }
        }
    }

    public void InvalidateStyles()
    {
        if (_styleInstance != null)
        {
            _styleInstance.Dispose();
            _styleInstance = null;
        }
    }

    public virtual void ApplyStyling(IClock clock)
    {
        _styleInstance ??= Styles.Instance(this);

        if (_styleInstance != null)
        {
            _styleInstance.Begin();
            _styleInstance.Apply(clock);
            _styleInstance.End();
        }
    }

    IStyleInstance? IStyleable.GetStyleInstance(IStyle style)
    {
        IStyleInstance? styleInstance = _styleInstance;
        while (styleInstance != null)
        {
            if (styleInstance.Source == style)
            {
                return styleInstance;
            }
            else
            {
                styleInstance = styleInstance.BaseStyle;
            }
        }

        return null;
    }

    void IStyleable.StyleApplied(IStyleInstance instance)
    {
        _styleInstance = instance;
    }

    public override void ReadFromJson(JsonNode json)
    {
        base.ReadFromJson(json);
        if (json is JsonObject jobject)
        {
            if (jobject.TryGetPropertyValue("styles", out JsonNode? stylesNode)
                && stylesNode is JsonArray stylesArray)
            {
                Styles.Clear();
                Styles.EnsureCapacity(stylesArray.Count);

                foreach (JsonNode? styleNode in stylesArray)
                {
                    if (styleNode is JsonObject styleObject
                        && styleObject.ToStyle() is Style style)
                    {
                        Styles.Add(style);
                    }
                }
            }
        }
    }

    public override void WriteToJson(ref JsonNode json)
    {
        base.WriteToJson(ref json);
        if (json is JsonObject jobject)
        {
            if (Styles.Count > 0)
            {
                var styles = new JsonArray();

                foreach (IStyle style in Styles.GetMarshal().Value)
                {
                    styles.Add(style.ToJson());
                }

                jobject["styles"] = styles;
            }
        }
    }

    #region IHierarchical

    public static readonly CoreProperty<IHierarchical?> HierarchicalParentProperty;
    private readonly CoreList<IHierarchical> _hierarchicalChildren;
    private IHierarchical? _parent;
    private IHierarchicalRoot? _root;

    [ShouldSerialize(false)]
    public IHierarchical? HierarchicalParent
    {
        get => _parent;
        private set => SetAndRaise(HierarchicalParentProperty, ref _parent, value);
    }

    protected ICoreList<IHierarchical> HierarchicalChildren => _hierarchicalChildren;

    IHierarchical? IHierarchical.HierarchicalParent => _parent;

    IHierarchicalRoot? IHierarchical.HierarchicalRoot => _root;

    ICoreReadOnlyList<IHierarchical> IHierarchical.HierarchicalChildren => HierarchicalChildren;

    public event EventHandler<HierarchyAttachmentEventArgs>? AttachedToHierarchy;

    public event EventHandler<HierarchyAttachmentEventArgs>? DetachedFromHierarchy;

    protected virtual void HierarchicalChildrenCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        void SetParent(IList children)
        {
            int count = children.Count;

            for (int i = 0; i < count; i++)
            {
                var logical = (IHierarchical)children[i]!;

                if (logical.HierarchicalParent is null)
                {
                    (logical as IModifiableHierarchical)?.SetParent(this);
                }
            }
        }

        void ClearParent(IList children)
        {
            int count = children.Count;

            for (int i = 0; i < count; i++)
            {
                var logical = (IHierarchical)children[i]!;

                if (logical.HierarchicalParent == this)
                {
                    (logical as IModifiableHierarchical)?.SetParent(null);
                }
            }
        }

        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                SetParent(e.NewItems!);
                break;

            case NotifyCollectionChangedAction.Remove:
                ClearParent(e.OldItems!);
                break;

            case NotifyCollectionChangedAction.Replace:
                ClearParent(e.OldItems!);
                SetParent(e.NewItems!);
                break;

            case NotifyCollectionChangedAction.Reset:
                break;
        }
    }

    protected virtual void OnAttachedToHierarchy(in HierarchyAttachmentEventArgs args)
    {
    }

    protected virtual void OnDetachedFromHierarchy(in HierarchyAttachmentEventArgs args)
    {
    }

    private void OnAttachedToHierarchyCore(in HierarchyAttachmentEventArgs e)
    {
        if (_parent == null && this is not IHierarchicalRoot)
        {
            throw new InvalidOperationException(
                $"AttachedToLogicalTreeCore called for '{GetType().Name}' but element has no logical parent.");
        }

        if (_root == null)
        {
            _root = e.Root;
            OnAttachedToHierarchy(e);
            AttachedToHierarchy?.Invoke(this, e);
        }

        _ = HierarchicalChildren;
        foreach (IHierarchical item in _hierarchicalChildren!.GetMarshal().Value)
        {
            (item as IModifiableHierarchical)?.NotifyAttachedToHierarchy(e);
        }
    }

    private void OnDetachedFromHierarchyCore(in HierarchyAttachmentEventArgs e)
    {
        if (_root != null)
        {
            _root = null;
            OnDetachedFromHierarchy(e);
            DetachedFromHierarchy?.Invoke(this, e);

            _ = HierarchicalChildren;
            foreach (IHierarchical item in _hierarchicalChildren!.GetMarshal().Value)
            {
                (item as IModifiableHierarchical)?.NotifyDetachedFromHierarchy(e);
            }
        }
    }

    void IModifiableHierarchical.NotifyAttachedToHierarchy(in HierarchyAttachmentEventArgs e)
    {
        OnAttachedToHierarchyCore(e);
    }

    void IModifiableHierarchical.NotifyDetachedFromHierarchy(in HierarchyAttachmentEventArgs e)
    {
        OnDetachedFromHierarchyCore(e);
    }

    void IModifiableHierarchical.SetParent(IHierarchical? parent)
    {
        IHierarchical? old = _parent;

        if (parent != old)
        {
            if (old != null && parent != null)
            {
                throw new InvalidOperationException("This logical element already has a parent.");
            }

            if (_root != null)
            {
                var e = new HierarchyAttachmentEventArgs(_root, old);
                OnDetachedFromHierarchyCore(e);
            }

            _parent = parent;
            IHierarchicalRoot? newRoot = this.FindHierarchicalRoot();

            if (newRoot != null)
            {
                var e = new HierarchyAttachmentEventArgs(newRoot, parent);
                OnAttachedToHierarchyCore(e);
            }

            // Raise PropertyChanged
            _parent = old;
            HierarchicalParent = parent;
        }
    }

    void IModifiableHierarchical.AddChild(IHierarchical child)
    {
        HierarchicalChildren.Add(child);
    }

    void IModifiableHierarchical.RemoveChild(IHierarchical child)
    {
        HierarchicalChildren.Remove(child);
    }
    #endregion
}
