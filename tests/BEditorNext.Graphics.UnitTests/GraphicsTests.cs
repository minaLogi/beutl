﻿using BEditorNext.Graphics.Pixel;

using NUnit.Framework;

namespace BEditorNext.Graphics.UnitTests;

public class GraphicsTests
{
    [Test]
    public void DrawText()
    {
        var element = new TextElement
        {
            Color = Colors.White,
            Size = 200,
            Text = "Text",
            Font = TypefaceProvider.CreateTypeface()
        };

        var graphics = new Graphics(500, 500)
        {
            Color = Colors.Gray,
        };

        graphics.Clear(Colors.Black);

        var rect = new Rect(graphics.Size.ToSize(1));
        Size size = element.Measure();
        Rect bounds = rect.CenterRect(new Rect(size));

        graphics.Translate(bounds.Position);
        graphics.FillRect(size);
        graphics.DrawText(element);

        Bitmap<Bgra8888> bmp = graphics.GetBitmap();

        Assert.IsTrue(bmp.Save(Path.Combine(ArtifactProvider.GetArtifactDirectory(), "1.png"), EncodedImageFormat.Png));

        bmp.Dispose();
        graphics.Dispose();
        element.Font.Dispose();
    }
}