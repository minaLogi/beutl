﻿using System.Text.Json.Nodes;

namespace Beutl.Configuration;

public sealed class GlobalConfiguration
{
    public static readonly GlobalConfiguration Instance = new();
    private string? _filePath;

    public static string DefaultFilePath
    {
        get
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".beutl", "settings.json");
        }
    }

    private GlobalConfiguration()
    {
        AddHandlers();
    }

    public event EventHandler<ConfigurationBase>? ConfigurationChanged;

    public GraphicsConfig GraphicsConfig { get; } = new();

    public FontConfig FontConfig { get; } = new();

    public ViewConfig ViewConfig { get; } = new();

    public ExtensionConfig ExtensionConfig { get; } = new();

    public BackupConfig BackupConfig { get; } = new();

    public void Save(string file)
    {
        try
        {
            _filePath = file;
            RemoveHandlers();
            string dir = Path.GetDirectoryName(file)!;
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var json = new JsonObject();

            var fontNode = new JsonObject();
            FontConfig.WriteToJson(fontNode);
            json["Font"] = fontNode;

            var viewNode = new JsonObject();
            ViewConfig.WriteToJson(viewNode);
            json["View"] = viewNode;

            var extensionNode = new JsonObject();
            ExtensionConfig.WriteToJson(extensionNode);
            json["Extension"] = extensionNode;

            var backupNode = new JsonObject();
            BackupConfig.WriteToJson(backupNode);
            json["Backup"] = backupNode;

            json.JsonSave(file);
        }
        finally
        {
            AddHandlers();
        }
    }

    public void Restore(string file)
    {
        try
        {
            RemoveHandlers();
            if (JsonHelper.JsonRestore(file) is JsonObject json)
            {
                JsonNode? GetNode(string name1, string name2)
                {
                    if (json[name1] is JsonNode node1)
                        return node1;
                    else if (json[name2] is JsonNode node2)
                        return node2;
                    else
                        return null;
                }

                if (GetNode("font", "Font") is JsonObject font)
                    FontConfig.ReadFromJson(font);
                
                if (GetNode("view", "View") is JsonObject view)
                    ViewConfig.ReadFromJson(view);
                
                if (GetNode("extension", "Extension") is JsonObject extension)
                    ExtensionConfig.ReadFromJson(extension);
                
                if (GetNode("backup", "Backup") is JsonObject backup)
                    BackupConfig.ReadFromJson(backup);
            }
        }
        finally
        {
            AddHandlers();
        }
    }

    private void AddHandlers()
    {
        GraphicsConfig.ConfigurationChanged += OnConfigurationChanged;
        FontConfig.ConfigurationChanged += OnConfigurationChanged;
        ViewConfig.ConfigurationChanged += OnConfigurationChanged;
        ExtensionConfig.ConfigurationChanged += OnConfigurationChanged;
    }

    private void RemoveHandlers()
    {
        GraphicsConfig.ConfigurationChanged -= OnConfigurationChanged;
        FontConfig.ConfigurationChanged -= OnConfigurationChanged;
        ViewConfig.ConfigurationChanged -= OnConfigurationChanged;
        ExtensionConfig.ConfigurationChanged -= OnConfigurationChanged;
    }

    private void OnConfigurationChanged(object? sender, EventArgs e)
    {
        if (sender is ConfigurationBase config)
        {
            ConfigurationChanged?.Invoke(this, config);
        }

        Save(_filePath ?? DefaultFilePath);
    }
}
