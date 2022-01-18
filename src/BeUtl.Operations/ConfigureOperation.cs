﻿using BeUtl.ProjectSystem;
using BeUtl.Rendering;

namespace BeUtl.Operations;

public abstract class ConfigureOperation : LayerOperation
{
    public override void ApplySetters(in OperationRenderArgs args)
    {
        base.ApplySetters(args);

        for (int i = 0; i < args.Scope.Count; i++)
        {
            IRenderable item = args.Scope[i];
            Configure(args, ref item);
            args.Scope[i] = item;
        }
    }

    public abstract void Configure(in OperationRenderArgs args, ref IRenderable obj);
}

public abstract class ConfigureOperation<T> : ConfigureOperation
    where T : IRenderable
{
    public override void Configure(in OperationRenderArgs args, ref IRenderable obj)
    {
        if (obj is T typed)
        {
            Configure(args, ref typed);
            obj = typed;
        }
    }

    public abstract void Configure(in OperationRenderArgs args, ref T obj);
}
