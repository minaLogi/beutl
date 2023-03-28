﻿using System.ComponentModel.DataAnnotations;
using System.Numerics;

using Beutl.Language;
using Beutl.Media;
using Beutl.Media.Pixel;

using OpenCvSharp;

namespace Beutl.Graphics.Effects.OpenCv;

public class Blur : BitmapEffect
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

    public Blur()
    {
        Processor = new _(this);
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

    public override IBitmapProcessor Processor { get; }

    public override Rect TransformBounds(Rect rect)
    {
        if (!_fixImageSize)
        {
            rect = rect.Inflate(new Thickness(0, 0, _kernelSize.Width, _kernelSize.Height));
        }

        return rect;
    }

    private sealed class _ : IBitmapProcessor
    {
        private readonly Blur _blur;

        public _(Blur blur)
        {
            _blur = blur;
        }

        public void Process(in Bitmap<Bgra8888> src, out Bitmap<Bgra8888> dst)
        {
            int width = _blur._kernelSize.Width;
            int height = _blur._kernelSize.Height;
            Bitmap<Bgra8888>? image;
            if (_blur.FixImageSize)
            {
                image = src.Clone();
            }
            else
            {
                image = src.MakeBorder(src.Width + width, src.Height + height);
            }

            using var mat = image.ToMat();
            if (width % 2 == 0)
            {
                width++;
            }

            if (height % 2 == 0)
            {
                height++;
            }

            Cv2.Blur(mat, mat, new(width, height));

            dst = image;
        }
    }
}
