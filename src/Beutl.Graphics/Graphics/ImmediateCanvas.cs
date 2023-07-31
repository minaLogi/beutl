﻿using Beutl.Graphics.Effects;
using Beutl.Graphics.Rendering;
using Beutl.Media;
using Beutl.Media.Pixel;
using Beutl.Media.Source;
using Beutl.Media.TextFormatting;
using Beutl.Rendering.Cache;
using Beutl.Threading;

using SkiaSharp;

namespace Beutl.Graphics;

public partial class ImmediateCanvas : ICanvas, IImmediateCanvasFactory
{
    private readonly SKCanvas _canvas;
    private readonly SKSurface _surface;
    private readonly Dispatcher? _dispatcher;
    private readonly SKPaint _sharedFillPaint = new();
    private readonly SKPaint _sharedStrokePaint = new();
    private readonly Stack<CanvasPushedState> _states = new();
    private readonly bool _leaveOpen;
    private Matrix _currentTransform;

    public ImmediateCanvas(SKSurface surface, bool leaveOpen)
    {
        _dispatcher = Dispatcher.Current;
        Size = surface.Canvas.DeviceClipBounds.Size.ToGraphicsSize();
        _surface = surface;
        _canvas = _surface.Canvas;
        _currentTransform = _canvas.TotalMatrix.ToMatrix();
        _leaveOpen = leaveOpen;
    }

    public ImmediateCanvas(int width, int height)
        : this(SKSurface.Create(new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Unpremul)), false)
    {
    }

    ~ImmediateCanvas()
    {
        Dispose();
    }

    public bool IsDisposed { get; private set; }

    public BlendMode BlendMode { get; set; } = BlendMode.SrcOver;

    public PixelSize Size { get; }

    public Matrix Transform
    {
        get { return _currentTransform; }
        set
        {
            if (_currentTransform == value)
                return;

            _currentTransform = value;
            _canvas.SetMatrix(_currentTransform.ToSKMatrix());
        }
    }

    internal IImmediateCanvasFactory? Factory { get; set; }

    internal SKCanvas Canvas => _canvas;

    public RenderCacheContext? GetCacheContext()
    {
        return Factory?.GetCacheContext();
    }

    public ImmediateCanvas CreateCanvas(SKSurface surface, bool leaveOpen)
    {
        if (Factory != null)
        {
            return Factory.CreateCanvas(surface, leaveOpen);
        }
        else
        {
            return new ImmediateCanvas(surface, leaveOpen)
            {
                Factory = this
            };
        }
    }

    public SKSurface CreateRenderTarget(int width, int height)
    {
        if (Factory != null)
        {
            return Factory.CreateRenderTarget(width, height);
        }
        else
        {
            return SKSurface.Create(
                new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Unpremul));
        }
    }

    public void Clear()
    {
        VerifyAccess();
        _canvas.Clear();
    }

    public void Clear(Color color)
    {
        VerifyAccess();
        _canvas.Clear(color.ToSKColor());
    }

    public void ClipRect(Rect clip, ClipOperation operation = ClipOperation.Intersect)
    {
        VerifyAccess();
        _canvas.ClipRect(clip.ToSKRect(), operation.ToSKClipOperation());
    }

    public void ClipPath(Geometry geometry, ClipOperation operation = ClipOperation.Intersect)
    {
        VerifyAccess();
        _canvas.ClipPath(geometry.GetNativeObject(), operation.ToSKClipOperation(), true);
    }

    public void Dispose()
    {
        void DisposeCore()
        {
            if (!_leaveOpen)
            {
                _surface.Dispose();
            }

            _sharedFillPaint.Dispose();
            _sharedStrokePaint.Dispose();
            GC.SuppressFinalize(this);
            Factory = null;
            IsDisposed = true;
        }

        if (!IsDisposed)
        {
            if (_dispatcher == null)
            {
                DisposeCore();
            }
            else
            {
                _dispatcher?.Invoke(DisposeCore);
            }
        }
    }

    public void DrawNode(IGraphicNode node)
    {
        if (GetCacheContext() is { } context)
        {
            RenderCache cache = context.GetCache(node);
            if (node is ISupportRenderCache supportCache)
            {
                supportCache.Accepts(cache);

                if (cache.CanCache())
                {
                    if (cache.IsCached)
                    {
                        supportCache.RenderWithCache(this, cache);
                        return;
                    }
                }
            }
            else
            {
                cache.IncrementRenderCount();
                if (cache.IsCached)
                {
                    void AcceptsAll(IGraphicNode node)
                    {
                        RenderCache cache = context!.GetCache(node);
                        (node as ISupportRenderCache)?.Accepts(cache);
                        if (node is ContainerNode c)
                        {
                            foreach (IGraphicNode item in c.Children)
                            {
                                AcceptsAll(item);
                            }
                        }
                    }
                    AcceptsAll(node);

                    if (context.CanCacheRecursive(node))
                    {
                        using (Ref<SKSurface> surface = cache.UseCache(out Rect bounds))
                        {
                            _canvas.DrawSurface(surface.Value, bounds.X, bounds.Y);
                        }

                        return;
                    }
                    else
                    {
                        cache.Invalidate();
                    }
                }
            }
        }

        node.Render(this);
    }

    public void DrawBitmap(IBitmap bmp, IBrush? fill, IPen? pen)
    {
        if (bmp.IsDisposed)
            throw new ObjectDisposedException(nameof(IBitmap));

        if (bmp.ByteCount <= 0)
            return;

        VerifyAccess();
        var size = new Size(bmp.Width, bmp.Height);
        ConfigureFillPaint(size, fill);
        ConfigureStrokePaint(new Rect(size), pen);

        if (bmp is Bitmap<Bgra8888>)
        {
            using var img = SKImage.FromPixels(new SKImageInfo(bmp.Width, bmp.Height, SKColorType.Bgra8888), bmp.Data);

            _canvas.DrawImage(img, SKPoint.Empty, _sharedFillPaint);
        }
        else
        {
            using var skbmp = bmp.ToSKBitmap();
            _canvas.DrawBitmap(skbmp, SKPoint.Empty, _sharedFillPaint);
        }
    }

    public void DrawImageSource(IImageSource source, IBrush? fill, IPen? pen)
    {
        if (source.TryGetRef(out Ref<IBitmap>? bitmap))
        {
            using (bitmap)
            {
                DrawBitmap(bitmap.Value, fill, pen);
            }
        }
    }

    public void DrawEllipse(Rect rect, IBrush? fill, IPen? pen)
    {
        VerifyAccess();
        ConfigureFillPaint(rect.Size, fill);
        _canvas.DrawOval(rect.ToSKRect(), _sharedFillPaint);

        if (pen != null && pen.Thickness != 0)
        {
            if (pen.StrokeAlignment == StrokeAlignment.Center)
            {
                ConfigureStrokePaint(rect, pen);
                _canvas.DrawOval(rect.ToSKRect(), _sharedStrokePaint);
            }
            else
            {
                using (var path = new SKPath())
                {
                    path.AddOval(rect.ToSKRect());
                    DrawSKPath(path, true, fill, pen);
                }
            }
        }
    }

    public void DrawRectangle(Rect rect, IBrush? fill, IPen? pen)
    {
        VerifyAccess();
        ConfigureFillPaint(rect.Size, fill);
        _canvas.DrawRect(rect.ToSKRect(), _sharedFillPaint);

        if (pen != null && pen.Thickness != 0)
        {
            if (pen.StrokeAlignment == StrokeAlignment.Center)
            {
                ConfigureStrokePaint(rect, pen);
                _canvas.DrawRect(rect.ToSKRect(), _sharedStrokePaint);
            }
            else
            {
                using (var path = new SKPath())
                {
                    path.AddRect(rect.ToSKRect());
                    DrawSKPath(path, true, fill, pen);
                }
            }
        }
    }

    public void DrawText(FormattedText text, IBrush? fill, IPen? pen)
    {
        VerifyAccess();
        var typeface = new Typeface(text.Font, text.Style, text.Weight);
        Size size = text.Bounds;
        SKTypeface sktypeface = typeface.ToSkia();
        ConfigureFillPaint(size, fill);
        _sharedFillPaint.TextSize = text.Size;
        _sharedFillPaint.Typeface = sktypeface;

        bool enableStroke = pen != null && pen.Thickness != 0;

        if (enableStroke)
        {
            ConfigureStrokePaint(new Rect(size), pen);
            _sharedStrokePaint.TextSize = text.Size;
            _sharedStrokePaint.Typeface = sktypeface;
        }

        Span<char> sc = stackalloc char[1];
        float prevRight = 0;

        foreach (char item in text.Text.AsSpan())
        {
            sc[0] = item;
            var bounds = default(SKRect);
            float w = _sharedFillPaint.MeasureText(sc, ref bounds);

            _canvas.Save();
            _canvas.Translate(prevRight + bounds.Left, 0);

            SKPath skPath = _sharedFillPaint.GetTextPath(
                sc,
                (bounds.Width / 2) - bounds.MidX,
                0/*-_paint.FontMetrics.Ascent*/);

            _canvas.DrawPath(skPath, _sharedFillPaint);
            if (enableStroke)
            {
                switch (pen!.StrokeAlignment)
                {
                    case StrokeAlignment.Center:
                        _canvas.DrawPath(skPath, _sharedStrokePaint);
                        break;

                    case StrokeAlignment.Inside:
                        _canvas.Save();
                        _canvas.ClipPath(skPath, SKClipOperation.Intersect, true);
                        _canvas.DrawPath(skPath, _sharedStrokePaint);
                        _canvas.Restore();
                        break;

                    case StrokeAlignment.Outside:
                        _canvas.Save();
                        _canvas.ClipPath(skPath, SKClipOperation.Difference, true);
                        _canvas.DrawPath(skPath, _sharedStrokePaint);
                        _canvas.Restore();
                        break;
                }
            }

            skPath.Dispose();

            prevRight += text.Spacing;
            prevRight += w;

            _canvas.Restore();
        }
    }

    internal void DrawSKPath(SKPath skPath, bool strokeOnly, IBrush? fill, IPen? pen)
    {
        Rect rect = skPath.Bounds.ToGraphicsRect();

        if (!strokeOnly)
        {
            ConfigureFillPaint(rect.Size, fill);
            _canvas.DrawPath(skPath, _sharedFillPaint);
        }

        if (pen != null && pen.Thickness != 0)
        {
            ConfigureStrokePaint(rect, pen);
            switch (pen.StrokeAlignment)
            {
                case StrokeAlignment.Center:
                    _canvas.DrawPath(skPath, _sharedStrokePaint);
                    break;

                case StrokeAlignment.Inside:
                    _canvas.Save();
                    _canvas.ClipPath(skPath, SKClipOperation.Intersect, true);
                    _canvas.DrawPath(skPath, _sharedStrokePaint);
                    _canvas.Restore();
                    break;

                case StrokeAlignment.Outside:
                    _canvas.Save();
                    _canvas.ClipPath(skPath, SKClipOperation.Difference, true);
                    _canvas.DrawPath(skPath, _sharedStrokePaint);
                    _canvas.Restore();
                    break;
            }
        }
    }

    public void DrawGeometry(Geometry geometry, IBrush? fill, IPen? pen)
    {
        VerifyAccess();
        SKPath skPath = geometry.GetNativeObject();
        DrawSKPath(skPath, false, fill, pen);
    }

    public unsafe Bitmap<Bgra8888> GetBitmap()
    {
        VerifyAccess();
        var result = new Bitmap<Bgra8888>(Size.Width, Size.Height);

        _surface.ReadPixels(new SKImageInfo(Size.Width, Size.Height, SKColorType.Bgra8888), result.Data, result.Width * sizeof(Bgra8888), 0, 0);

        return result;
    }

    public void Pop(int count = -1)
    {
        VerifyAccess();

        if (count < 0)
        {
            while (count < 0
                && _states.TryPop(out CanvasPushedState? state))
            {
                state.Pop(this);
                count++;
            }
        }
        else
        {
            while (_states.Count >= count
                && _states.TryPop(out CanvasPushedState? state))
            {
                state.Pop(this);
            }
        }
    }

    public PushedState Push()
    {
        VerifyAccess();
        int count = _canvas.Save();

        _states.Push(new CanvasPushedState.SKCanvasPushedState(count));
        return new PushedState(this, _states.Count);
    }

    public PushedState PushClip(Rect clip, ClipOperation operation = ClipOperation.Intersect)
    {
        VerifyAccess();
        int count = _canvas.Save();
        ClipRect(clip, operation);

        _states.Push(new CanvasPushedState.SKCanvasPushedState(count));
        return new PushedState(this, _states.Count);
    }

    public PushedState PushClip(Geometry geometry, ClipOperation operation = ClipOperation.Intersect)
    {
        VerifyAccess();
        int count = _canvas.Save();
        ClipPath(geometry, operation);

        _states.Push(new CanvasPushedState.SKCanvasPushedState(count));
        return new PushedState(this, _states.Count);
    }

    public PushedState PushOpacityMask(IBrush mask, Rect bounds, bool invert = false)
    {
        VerifyAccess();
        var paint = new SKPaint();

        int count = _canvas.SaveLayer(paint);
        new BrushConstructor(bounds.Size, mask, (BlendMode)paint.BlendMode, this).ConfigurePaint(paint);
        _states.Push(new CanvasPushedState.MaskPushedState(count, invert, paint));
        return new PushedState(this, _states.Count);
    }

    public PushedState PushTransform(Matrix matrix, TransformOperator transformOperator = TransformOperator.Prepend)
    {
        VerifyAccess();
        int count = _canvas.Save();

        if (transformOperator == TransformOperator.Prepend)
        {
            Transform = Transform.Prepend(matrix);
        }
        else if (transformOperator == TransformOperator.Append)
        {
            Transform = Transform.Append(matrix);
        }
        else
        {
            Transform = matrix;
        }

        _states.Push(new CanvasPushedState.SKCanvasPushedState(count));
        return new PushedState(this, _states.Count);
    }

    public PushedState PushBlendMode(BlendMode blendMode)
    {
        VerifyAccess();
        _states.Push(new CanvasPushedState.BlendModePushedState(blendMode));
        return new PushedState(this, _states.Count);
    }

    public PushedState PushFilterEffect(FilterEffect effect)
    {
        throw new NotSupportedException("ImmediateCanvasはFilterEffectに対応しません");
    }

    private void VerifyAccess()
    {
        if (IsDisposed)
            throw new ObjectDisposedException(nameof(ImmediateCanvas));

        _dispatcher?.VerifyAccess();
    }

    private void ConfigureStrokePaint(Rect rect, IPen? pen)
    {
        _sharedStrokePaint.Reset();

        if (pen != null && pen.Thickness != 0)
        {
            float thickness = pen.Thickness;
            switch (pen.StrokeAlignment)
            {
                case StrokeAlignment.Center:
                    rect = rect.Inflate(thickness / 2);
                    break;

                case StrokeAlignment.Outside:
                    rect = rect.Inflate(thickness);
                    goto case StrokeAlignment.Inside;

                case StrokeAlignment.Inside:
                    thickness *= 2;
                    break;

                default:
                    break;
            }

            _sharedStrokePaint.IsStroke = true;
            _sharedStrokePaint.StrokeWidth = thickness;
            _sharedStrokePaint.StrokeCap = (SKStrokeCap)pen.StrokeCap;
            _sharedStrokePaint.StrokeJoin = (SKStrokeJoin)pen.StrokeJoin;
            _sharedStrokePaint.StrokeMiter = pen.MiterLimit;
            if (pen.DashArray != null && pen.DashArray.Count > 0)
            {
                IReadOnlyList<float> srcDashes = pen.DashArray;

                int count = srcDashes.Count % 2 == 0 ? srcDashes.Count : srcDashes.Count * 2;

                float[] dashesArray = new float[count];

                for (int i = 0; i < count; ++i)
                {
                    dashesArray[i] = (float)srcDashes[i % srcDashes.Count] * thickness;
                }

                float offset = (float)(pen.DashOffset * thickness);

                var pe = SKPathEffect.CreateDash(dashesArray, offset);

                _sharedStrokePaint.PathEffect = pe;
            }

            new BrushConstructor(rect.Size, pen.Brush, BlendMode, this).ConfigurePaint(_sharedStrokePaint);
        }
    }

    private void ConfigureFillPaint(Size targetSize, IBrush? brush)
    {
        _sharedFillPaint.Reset();
        new BrushConstructor(targetSize, brush, BlendMode, this).ConfigurePaint(_sharedFillPaint);
    }
}
