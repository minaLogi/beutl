﻿using BeUtl.ProjectSystem;

namespace BeUtl.Services.Editors;

public sealed class ByteEditorService : INumberEditorService<byte>
{
    public byte GetMaximum(PropertyInstance<byte> property)
    {
        return property.GetMaximumOrDefault(byte.MaxValue);
    }

    public byte GetMinimum(PropertyInstance<byte> property)
    {
        return property.GetMinimumOrDefault(byte.MinValue);
    }

    public byte Clamp(byte value, byte min, byte max)
    {
        return Math.Clamp(value, min, max);
    }

    public byte Decrement(byte value, int increment)
    {
        return (byte)(value - increment);
    }

    public byte Increment(byte value, int increment)
    {
        return (byte)(value + increment);
    }

    public bool TryParse(string? s, out byte result)
    {
        return byte.TryParse(s, out result);
    }
}
