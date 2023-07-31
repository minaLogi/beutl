﻿using System.ComponentModel.DataAnnotations;

using Beutl.Language;
using Beutl.Media;
using Beutl.Media.Pixel;

using OpenCvSharp;

using SkiaSharp;

namespace Beutl.Graphics.Effects.OpenCv;

public class Blur : FilterEffect
{
    public static readonly CoreProperty<PixelSize> KernelSizeProperty;
    public static readonly CoreProperty<bool> FixImageSizeProperty;
    private PixelSize _kernelSize;
    private bool _fixImageSize;

    static Blur()
    {
        KernelSizeProperty = ConfigureProperty<PixelSize, Blur>(nameof(KernelSize))
            .Accessor(o => o.KernelSize, (o, v) => o.KernelSize = v)
            .DefaultValue(PixelSize.Empty)
            .Register();

        FixImageSizeProperty = ConfigureProperty<bool, Blur>(nameof(FixImageSize))
            .Accessor(o => o.FixImageSize, (o, v) => o.FixImageSize = v)
            .DefaultValue(false)
            .Register();

        AffectsRender<Blur>(KernelSizeProperty, FixImageSizeProperty);
    }

    [Display(Name = nameof(Strings.KernelSize), ResourceType = typeof(Strings))]
    [Range(typeof(PixelSize), "1,1", "max,max")]
    public PixelSize KernelSize
    {
        get => _kernelSize;
        set => SetAndRaise(KernelSizeProperty, ref _kernelSize, value);
    }

    [Display(Name = nameof(Strings.FixImageSize), ResourceType = typeof(Strings))]
    public bool FixImageSize
    {
        get => _fixImageSize;
        set => SetAndRaise(FixImageSizeProperty, ref _fixImageSize, value);
    }

    public override void ApplyTo(FilterEffectContext context)
    {
        context.Custom((KernelSize, FixImageSize), Apply, TransformBounds);
    }

    private static Rect TransformBounds((PixelSize KernelSize, bool FixImageSize) data, Rect rect)
    {
        if (!data.FixImageSize)
        {
            rect = rect.Inflate(new Thickness(data.KernelSize.Width / 2, data.KernelSize.Height / 2, 0, 0));
            //rect = rect.Inflate(new Thickness(0, 0, data.KernelSize.Width, data.KernelSize.Height));
        }
        else
        {
            //rect.Inflate()
        }
        return rect;
    }

    private static void Apply((PixelSize KernelSize, bool FixImageSize) data, FilterEffectCustomOperationContext context)
    {
        if (context.Target.Surface is { } surface)
        {
            int kwidth = data.KernelSize.Width;
            int kheight = data.KernelSize.Height;
            if (kwidth % 2 == 0)
                kwidth++;
            if (kheight % 2 == 0)
                kheight++;

            Bitmap<Bgra8888>? dst = null;

            try
            {
                using (SKImage skimage = surface.Value.Snapshot())
                using (var src = skimage.ToBitmap())
                {
                    if (data.FixImageSize)
                    {
                        dst = src.Clone();
                    }
                    else
                    {
                        dst = src.MakeBorder(src.Width + kwidth, src.Height + kheight);
                    }
                }

                using var mat = dst.ToMat();
                Cv2.Blur(mat, mat, new(kwidth, kheight));

                using EffectTarget target = context.CreateTarget(dst.Width, dst.Height);
                target.Surface!.Value.Canvas.DrawBitmap(dst.ToSKBitmap(), 0, 0);
                context.ReplaceTarget(target);
            }
            finally
            {
                dst?.Dispose();
            }
        }
    }

    public override Rect TransformBounds(Rect rect)
    {
        if (!_fixImageSize)
        {
            rect = rect.Inflate(new Thickness(_kernelSize.Width / 2, _kernelSize.Height / 2, 0, 0));
        }

        return rect;
    }
}