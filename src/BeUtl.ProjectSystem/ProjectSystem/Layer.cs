﻿using System.Reactive;
using System.Reactive.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

using BeUtl.Collections;
using BeUtl.Commands;
using BeUtl.Media;
using BeUtl.Rendering;
using BeUtl.Streaming;

namespace BeUtl.ProjectSystem;

public class Layer : Element, IStorable, ILogicalElement
{
    public static readonly CoreProperty<TimeSpan> StartProperty;
    public static readonly CoreProperty<TimeSpan> LengthProperty;
    public static readonly CoreProperty<int> ZIndexProperty;
    public static readonly CoreProperty<Color> AccentColorProperty;
    public static readonly CoreProperty<bool> IsEnabledProperty;
    public static readonly CoreProperty<LayerNode> NodeProperty;
    public static readonly CoreProperty<LogicalList<StreamOperator>> OperatorsProperty;
    private TimeSpan _start;
    private TimeSpan _length;
    private int _zIndex;
    private string? _fileName;
    private bool _isEnabled = true;
    private EventHandler? _saved;
    private EventHandler? _restored;
    private IDisposable? _disposable;

    static Layer()
    {
        StartProperty = ConfigureProperty<TimeSpan, Layer>(nameof(Start))
            .Accessor(o => o.Start, (o, v) => o.Start = v)
            .Observability(PropertyObservability.Changed)
            .SerializeName("start")
            .Register();

        LengthProperty = ConfigureProperty<TimeSpan, Layer>(nameof(Length))
            .Accessor(o => o.Length, (o, v) => o.Length = v)
            .Observability(PropertyObservability.Changed)
            .SerializeName("length")
            .Register();

        ZIndexProperty = ConfigureProperty<int, Layer>(nameof(ZIndex))
            .Accessor(o => o.ZIndex, (o, v) => o.ZIndex = v)
            .Observability(PropertyObservability.Changed)
            .SerializeName("zIndex")
            .Register();

        AccentColorProperty = ConfigureProperty<Color, Layer>(nameof(AccentColor))
            .DefaultValue(Colors.Teal)
            .Observability(PropertyObservability.Changed)
            .SerializeName("accentColor")
            .Register();

        IsEnabledProperty = ConfigureProperty<bool, Layer>(nameof(IsEnabled))
            .Accessor(o => o.IsEnabled, (o, v) => o.IsEnabled = v)
            .DefaultValue(true)
            .Observability(PropertyObservability.Changed)
            .SerializeName("isEnabled")
            .Register();

        NodeProperty = ConfigureProperty<LayerNode, Layer>(nameof(Node))
            .Accessor(o => o.Node, null)
            .Observability(PropertyObservability.Changed)
            .Register();

        OperatorsProperty = ConfigureProperty<LogicalList<StreamOperator>, Layer>(nameof(Operators))
            .Accessor(o => o.Operators, null)
            .Register();

        NameProperty.OverrideMetadata<Layer>(new CorePropertyMetadata<string>
        {
            SerializeName = "name"
        });

        ZIndexProperty.Changed.Subscribe(args =>
        {
            if (args.Sender is Layer layer && layer.Parent is Scene { Renderer: { IsDisposed: false } renderer })
            {
                renderer[args.OldValue]?.RemoveNode(layer.Node);
                if (args.NewValue >= 0)
                {
                    ILayerContext? context = renderer[args.NewValue];
                    if (context == null)
                    {
                        context = new LayerContext();
                        renderer[args.NewValue] = context;
                    }
                    context.AddNode(layer.Node);
                }
            }
        });

        //RenderableProperty.Changed.Subscribe(args =>
        //{
        //    if (args.Sender is Layer layer && layer.Parent is Scene { Renderer: { IsDisposed: false } renderer } && layer.ZIndex >= 0)
        //    {
        //        renderer[layer.ZIndex] = args.NewValue;
        //    }
        //});

        IsEnabledProperty.Changed.Subscribe(args =>
        {
            if (args.Sender is Layer layer)
            {
                layer.ForceRender();
            }
        });

        StartProperty.Changed.Subscribe(e =>
        {
            if (e.Sender is Layer layer)
            {
                layer.Node.Start = e.NewValue;
            }
        });

        LengthProperty.Changed.Subscribe(e =>
        {
            if (e.Sender is Layer layer)
            {
                layer.Node.Duration = e.NewValue;
            }
        });
    }

    public Layer()
    {
        Operators = new LogicalList<StreamOperator>(this);
    }

    event EventHandler IStorable.Saved
    {
        add => _saved += value;
        remove => _saved -= value;
    }

    event EventHandler IStorable.Restored
    {
        add => _restored += value;
        remove => _restored -= value;
    }

    // 0以上
    public TimeSpan Start
    {
        get => _start;
        set => SetAndRaise(StartProperty, ref _start, value);
    }

    public TimeSpan Length
    {
        get => _length;
        set => SetAndRaise(LengthProperty, ref _length, value);
    }

    public TimeRange Range => new(Start, Length);

    public int ZIndex
    {
        get => _zIndex;
        set => SetAndRaise(ZIndexProperty, ref _zIndex, value);
    }

    public Color AccentColor
    {
        get => GetValue(AccentColorProperty);
        set => SetValue(AccentColorProperty, value);
    }

    public bool IsEnabled
    {
        get => _isEnabled;
        set => SetAndRaise(IsEnabledProperty, ref _isEnabled, value);
    }

    public LayerNode Node { get; } = new();

    //public Renderable? Renderable
    //{
    //    get => _renderable;
    //    set => SetAndRaise(RenderableProperty, ref _renderable, value);
    //}

    public string FileName
    {
        get => _fileName ?? throw new Exception("The file name is not set.");
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            _fileName = value;
        }
    }

    public DateTime LastSavedTime { get; private set; }

    public LogicalList<StreamOperator> Operators { get; }

    IEnumerable<ILogicalElement> ILogicalElement.LogicalChildren => Operators;

    public void Save(string filename)
    {
        _fileName = filename;
        LastSavedTime = DateTime.Now;
        this.JsonSave(filename);
        _saved?.Invoke(this, EventArgs.Empty);
    }

    public void Restore(string filename)
    {
        _fileName = filename;
        LastSavedTime = DateTime.Now;

        this.JsonRestore(filename);

        _restored?.Invoke(this, EventArgs.Empty);
    }

    public override void ReadFromJson(JsonNode json)
    {
        base.ReadFromJson(json);

        if (json is JsonObject jobject)
        {
            // NOTE: リリース時に削除。互換性を保つためのコードなので
            if (!jobject.ContainsKey("zIndex") && jobject.TryGetPropertyValue("layer", out JsonNode? layerNode)
                && layerNode is JsonValue layerValue
                && layerValue.TryGetValue(out int layer))
            {
                ZIndex = layer;
            }

            if (jobject.TryGetPropertyValue("renderable", out JsonNode? renderableNode)
                && renderableNode is JsonObject renderableObj
                && renderableObj.TryGetPropertyValue("@type", out JsonNode? renderableTypeNode)
                && renderableTypeNode is JsonValue renderableTypeValue
                && renderableTypeValue.TryGetValue(out string? renderableTypeStr)
                && TypeFormat.ToType(renderableTypeStr) is Type renderableType
                && renderableType.IsAssignableTo(typeof(Renderable))
                && Activator.CreateInstance(renderableType) is Renderable renderable)
            {
                renderable.ReadFromJson(renderableObj);
                Node.Value = renderable;
            }

            if (jobject.TryGetPropertyValue("operators", out JsonNode? operatorsNode)
                && operatorsNode is JsonArray operatorsArray)
            {
                foreach (JsonObject operatorJson in operatorsArray.OfType<JsonObject>())
                {
                    if (operatorJson.TryGetPropertyValue("@type", out JsonNode? atTypeNode)
                        && atTypeNode is JsonValue atTypeValue
                        && atTypeValue.TryGetValue(out string? atType))
                    {
                        var type = TypeFormat.ToType(atType);
                        StreamOperator? @operator = null;

                        if (type?.IsAssignableTo(typeof(StreamOperator)) ?? false)
                        {
                            @operator = Activator.CreateInstance(type) as StreamOperator;
                        }

                        @operator ??= new StreamOperator();
                        @operator.ReadFromJson(operatorJson);
                        Operators.Add(@operator);
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
            if (Node.Value is Renderable renderable)
            {
                JsonNode node = new JsonObject();
                renderable.WriteToJson(ref node);
                node["@type"] = TypeFormat.ToString(renderable.GetType());
                jobject["renderable"] = node;
            }

            Span<StreamOperator> operators = Operators.AsSpan();
            if (operators.Length > 0)
            {
                var array = new JsonArray();

                foreach (StreamOperator item in operators)
                {
                    JsonNode node = new JsonObject();
                    item.WriteToJson(ref node);
                    node["@type"] = TypeFormat.ToString(item.GetType());

                    array.Add(node);
                }

                jobject["operators"] = array;
            }
        }
    }

    public IRecordableCommand AddChild(StreamOperator @operator)
    {
        ArgumentNullException.ThrowIfNull(@operator);

        return new AddCommand<StreamOperator>(Operators, @operator, Operators.Count);
    }

    public IRecordableCommand RemoveChild(StreamOperator @operator)
    {
        ArgumentNullException.ThrowIfNull(@operator);

        return new RemoveCommand<StreamOperator>(Operators, @operator);
    }

    public IRecordableCommand InsertChild(int index, StreamOperator @operator)
    {
        ArgumentNullException.ThrowIfNull(@operator);

        return new AddCommand<StreamOperator>(Operators, @operator, index);
    }

    protected override void OnAttachedToLogicalTree(in LogicalTreeAttachmentEventArgs args)
    {
        base.OnAttachedToLogicalTree(args);
        if (args.Parent is Scene { Renderer: { IsDisposed: false } renderer } && ZIndex >= 0)
        {
            ILayerContext? context = renderer[ZIndex];
            if (context == null)
            {
                context = new LayerContext();
                renderer[ZIndex] = context;
            }
            context.AddNode(Node);

            _disposable = SubscribeToLayerNode();
        }
    }

    protected override void OnDetachedFromLogicalTree(in LogicalTreeAttachmentEventArgs args)
    {
        base.OnDetachedFromLogicalTree(args);
        if (args.Parent is Scene { Renderer: { IsDisposed: false } renderer } && ZIndex >= 0)
        {
            renderer[ZIndex]?.RemoveNode(Node);
            _disposable?.Dispose();
            _disposable = null;
        }
    }

    internal bool InRange(TimeSpan ts)
    {
        return Start <= ts && ts < Length + Start;
    }

    private void ForceRender()
    {
        Scene? scene = this.FindLogicalParent<Scene>();
        if (scene != null &&
            Start <= scene.CurrentFrame &&
            scene.CurrentFrame < Start + Length &&
            scene.Renderer is { IsDisposed: false, IsRendering: false })
        {
            scene.Renderer.Invalidate(scene.CurrentFrame);
        }
    }

    private IDisposable SubscribeToLayerNode()
    {
        return Node.GetObservable(LayerNode.ValueProperty)
            .SelectMany(value => value != null
                ? Observable.FromEventPattern(h => value.Invalidated += h, h => value.Invalidated -= h)
                    .Select(_ => Unit.Default)
                    .Publish(Unit.Default)
                    .RefCount()
                : Observable.Return(Unit.Default))
            .Subscribe(_ => ForceRender());
    }

    internal Layer? GetBefore(int zindex, TimeSpan start)
    {
        if (Parent is Scene scene)
        {
            Layer? tmp = null;
            foreach (Layer? item in scene.Children.AsSpan())
            {
                if (item != this && item.ZIndex == zindex && item.Start < start)
                {
                    if (tmp == null || tmp.Start <= item.Start)
                    {
                        tmp = item;
                    }
                }
            }
            return tmp;
        }

        return null;
    }

    internal Layer? GetAfter(int zindex, TimeSpan end)
    {
        if (Parent is Scene scene)
        {
            Layer? tmp = null;
            foreach (Layer? item in scene.Children.AsSpan())
            {
                if (item != this && item.ZIndex == zindex && item.Range.End > end)
                {
                    if (tmp == null || tmp.Range.End >= item.Range.End)
                    {
                        tmp = item;
                    }
                }
            }
            return tmp;
        }

        return null;
    }

    internal (Layer? Before, Layer? After, Layer? Cover) GetBeforeAndAfterAndCover(int zindex, TimeSpan start, TimeSpan end)
    {
        if (Parent is Scene scene)
        {
            Layer? beforeTmp = null;
            Layer? afterTmp = null;
            Layer? coverTmp = null;
            var range = new TimeRange(start, end - start);

            foreach (Layer? item in scene.Children.AsSpan())
            {
                if (item != this && item.ZIndex == zindex)
                {
                    if (item.Start < start
                        && (beforeTmp == null || beforeTmp.Start <= item.Start))
                    {
                        beforeTmp = item;
                    }

                    if (item.Range.End > end
                        && (afterTmp == null || afterTmp.Range.End >= item.Range.End))
                    {
                        afterTmp = item;
                    }

                    if (range.Contains(item.Range))
                    {
                        coverTmp = item;
                    }
                }
            }
            return (beforeTmp, afterTmp, coverTmp);
        }

        return (null, null, null);
    }
}
