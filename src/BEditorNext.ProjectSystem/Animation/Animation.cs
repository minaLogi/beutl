﻿using System.Text.Json.Nodes;

using BEditorNext.Animation.Easings;
using BEditorNext.ProjectSystem;

namespace BEditorNext.Animation;

public class Animation : Element, IAnimation
{
    public static readonly PropertyDefine<float> PreviousProperty;
    public static readonly PropertyDefine<float> NextProperty;
    public static readonly PropertyDefine<Easing> EasingProperty;
    public static readonly PropertyDefine<TimeSpan> DurationProperty;
    private float _previous;
    private float _next;
    private Easing _easing;
    private TimeSpan _duration;

    public Animation()
    {
        _easing = EasingProperty.GetDefaultValue() ?? new LinearEasing();
    }

    static Animation()
    {
        PreviousProperty = RegisterProperty<float, Animation>(
            nameof(Previous),
            (owner, obj) => owner.Previous = obj,
            owner => owner.Previous)
            .NotifyPropertyChanged(true)
            .JsonName("prev");

        NextProperty = RegisterProperty<float, Animation>(
            nameof(Next),
            (owner, obj) => owner.Next = obj,
            owner => owner.Next)
            .NotifyPropertyChanged(true)
            .JsonName("next");

        EasingProperty = RegisterProperty<Easing, Animation>(
            nameof(Easing),
            (owner, obj) => owner.Easing = obj,
            owner => owner.Easing)
            .NotifyPropertyChanged(true)
            .DefaultValue(new LinearEasing());

        DurationProperty = RegisterProperty<TimeSpan, Animation>(
            nameof(Duration),
            (owner, obj) => owner.Duration = obj,
            owner => owner.Duration)
            .NotifyPropertyChanged(true)
            .JsonName("duration");
    }

    public float Previous
    {
        get => _previous;
        set => SetAndRaise(PreviousProperty, ref _previous, value);
    }

    public float Next
    {
        get => _next;
        set => SetAndRaise(NextProperty, ref _next, value);
    }

    public Easing Easing
    {
        get => _easing;
        set => SetAndRaise(EasingProperty, ref _easing, value);
    }

    public TimeSpan Duration
    {
        get => _duration;
        set => SetAndRaise(DurationProperty, ref _duration, value);
    }

    public override JsonNode ToJson()
    {
        JsonNode node = base.ToJson();

        if (node is JsonObject jsonObject)
        {
            jsonObject.Remove("name");

            if (Easing is SplineEasing splineEasing)
            {
                jsonObject["easing"] = new JsonObject
                {
                    ["x1"] = splineEasing.X1,
                    ["y1"] = splineEasing.Y1,
                    ["x2"] = splineEasing.X2,
                    ["y2"] = splineEasing.Y2,
                };
            }
            else
            {
                jsonObject["easing"] = JsonValue.Create(SceneLayer.TypeResolver.ToString(Easing.GetType()));
            }
        }

        return node;
    }

    public override void FromJson(JsonNode json)
    {
        base.FromJson(json);

        if (json is JsonObject jsonObject)
        {
            if (jsonObject.TryGetPropertyValue("easing", out JsonNode? easingNode))
            {
                if (easingNode is JsonValue easingTypeValue &&
                    easingTypeValue.TryGetValue(out string? easingType))
                {
                    Type type = SceneLayer.TypeResolver.ToType(easingType) ?? typeof(LinearEasing);

                    if (Activator.CreateInstance(type) is Easing easing)
                    {
                        Easing = easing;
                    }
                }
                else if (easingNode is JsonObject easingObject)
                {
                    float x1 = (float?)easingObject["x1"] ?? 0;
                    float y1 = (float?)easingObject["y1"] ?? 0;
                    float x2 = (float?)easingObject["x2"] ?? 1;
                    float y2 = (float?)easingObject["y2"] ?? 1;

                    Easing = new SplineEasing(x1, y1, x2, y2);
                }
            }
        }
    }
}