﻿using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;

using BeUtl.ProjectSystem;

namespace BeUtl;

internal static class Helper
{
    public static readonly double SecondWidth;
    public static readonly double LayerHeight;

    static Helper()
    {
        SecondWidth = (double)(Application.Current?.FindResource("SecondWidth") ?? 150);
        LayerHeight = (double)(Application.Current?.FindResource("LayerHeight") ?? 25);
    }

    public static Color ToAvalonia(this in Media.Color color)
    {
        return Color.FromArgb(color.A, color.R, color.G, color.B);
    }

    public static Media.Color ToMedia(this in Color color)
    {
        return Media.Color.FromArgb(color.A, color.R, color.G, color.B);
    }

    public static double ToPixel(this TimeSpan time)
    {
        return time.TotalSeconds * SecondWidth;
    }

    public static TimeSpan ToTimeSpan(this double pixel)
    {
        return TimeSpan.FromSeconds(pixel / SecondWidth);
    }

    public static double ToPixel(this TimeSpan time, float scale)
    {
        return time.TotalSeconds * SecondWidth * scale;
    }

    public static TimeSpan ToTimeSpan(this double pixel, float scale)
    {
        return TimeSpan.FromSeconds(pixel / (SecondWidth * scale));
    }

    public static int ToLayerNumber(this double pixel)
    {
        return (int)Math.Floor(pixel / LayerHeight);
    }

    public static int ToLayerNumber(this Thickness thickness)
    {
        return (int)Math.Floor((thickness.Top + (LayerHeight / 2)) / LayerHeight);
    }

    public static double ToLayerPixel(this int layer)
    {
        return layer * LayerHeight;
    }

    public static string RandomLayerFileName(string baseDir, string ext)
    {
        string filename = Path.Combine(baseDir, $"{RandomString()}.{ext}");
        while (File.Exists(filename))
        {
            filename = Path.Combine(baseDir, $"{RandomString()}.{ext}");
        }

        return filename;
    }

    public static T? GetMaximumOrDefault<T>(this IPropertyInstance setter, T defaultValue)
    {
        OperationPropertyMetadata<T> metadata
            = setter.Property.GetMetadata<OperationPropertyMetadata<T>>(setter.Parent.GetType());
        return metadata.HasMaximum ? metadata.Maximum : defaultValue;
    }

    public static T? GetMinimumOrDefault<T>(this IPropertyInstance setter, T defaultValue)
    {
        OperationPropertyMetadata<T> metadata
            = setter.Property.GetMetadata<OperationPropertyMetadata<T>>(setter.Parent.GetType());
        return metadata.HasMinimum ? metadata.Minimum : defaultValue;
    }
    
    public static object? GetDefaultValue(this IPropertyInstance setter)
    {
        return setter.Property.GetMetadata<ICorePropertyMetadata>(setter.Parent.GetType()).GetDefaultValue();
    }

    private static string RandomString()
    {
        const string characters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        Span<char> Charsarr = stackalloc char[8];
        var random = new Random();

        for (int i = 0; i < Charsarr.Length; i++)
        {
            Charsarr[i] = characters[random.Next(characters.Length)];
        }

        return new string(Charsarr);
    }
}