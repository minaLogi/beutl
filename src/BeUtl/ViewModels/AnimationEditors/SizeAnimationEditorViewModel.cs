﻿using BeUtl.Animation;
using BeUtl.Graphics;
using BeUtl.ViewModels.Editors;

namespace BeUtl.ViewModels.AnimationEditors;

public sealed class SizeAnimationEditorViewModel : AnimationEditorViewModel<Size>
{
    public SizeAnimationEditorViewModel(Animation<Size> animation, BaseEditorViewModel<Size> editorViewModel)
        : base(animation, editorViewModel)
    {
    }

    public Size Maximum => Setter.GetMaximumOrDefault(new Size(float.MaxValue, float.MaxValue));

    public Size Minimum => Setter.GetMinimumOrDefault(new Size(float.MinValue, float.MinValue));
}