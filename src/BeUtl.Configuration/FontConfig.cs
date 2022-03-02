﻿using System.Collections.ObjectModel;
using System.Text.Json.Nodes;

namespace BeUtl.Configuration;

public sealed class FontConfig : ConfigurationBase
{
    public ObservableCollection<string> FontDirectories { get; } = CreateDefaultFontDirectories();

    public override void FromJson(JsonNode json)
    {
        base.FromJson(json);
        if (json is JsonObject jsonObject)
        {
            if (jsonObject.TryGetPropertyValue("directories", out JsonNode? dirsNode) &&
                dirsNode is JsonArray dirsArray)
            {
                string[] array = dirsArray.Select(i => (string?)i).Where(i => i != null).ToArray()!;

                foreach (string item in array.Except(FontDirectories))
                {
                    FontDirectories.Add(item);
                }

                foreach (string item in FontDirectories.Except(array))
                {
                    FontDirectories.Remove(item);
                }
            }
        }
    }

    public override JsonNode ToJson()
    {
        JsonNode obj = base.ToJson();
        obj["directories"] = new JsonArray(FontDirectories.Select(i => JsonValue.Create(i)).ToArray());

        return obj;
    }

    private static ObservableCollection<string> CreateDefaultFontDirectories()
    {
        static IEnumerable<string> Windows()
        {
            string user = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            yield return $"{user}\\AppData\\Local\\Microsoft\\Windows\\Fonts";
            yield return "C:\\Windows\\Fonts";
        }

        static IEnumerable<string> Linux()
        {
            string user = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            yield return "/usr/local/share/fonts";
            yield return "/usr/share/fonts";
            yield return $"{user}/.local/share/fonts/";
        }

        static IEnumerable<string> MacOS()
        {
            string user = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            yield return "/System/Library/Fonts";
            yield return "/Library/Fonts";
            yield return $"{user}/Library/Fonts";
        }

        IEnumerable<string>? e = null;
        if (OperatingSystem.IsWindows())
        {
            e = Windows();
        }
        else if (OperatingSystem.IsLinux())
        {
            e = Linux();
        }
        else if (OperatingSystem.IsMacOS())
        {
            e = MacOS();
        }

        return e != null ?
            new ObservableCollection<string>(e) :
            new ObservableCollection<string>();
    }
}