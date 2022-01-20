﻿
using BeUtl.Animation;

namespace BeUtl.Styling;

public interface IStyleInstance : IDisposable
{
    bool IsEnabled { get; set; }

    IStyleInstance? BaseStyle { get; }

    IStyleable Target { get; }

    IStyle Source { get; }

    ReadOnlySpan<ISetterInstance> Setters { get; }

    void Apply(IClock clock);

    void Unapply();
}
