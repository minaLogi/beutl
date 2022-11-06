﻿using ResourcesGenerator;

using System.Text;
using System.Xml.Linq;

const string RootNamespace = "Beutl.Language";
string dir = Environment.CurrentDirectory;
string[] files = Directory.GetFiles(dir, "*.axaml", SearchOption.AllDirectories);

var dict = new Dictionary<string, List<IResourceItem>>();
foreach (string file in files)
{
    Console.WriteLine(file);
    using var stream = new FileStream(file, FileMode.Open);
    var xdoc = XDocument.Load(stream);
    var ns = XNamespace.Get("https://github.com/avaloniaui");
    var x = XNamespace.Get("http://schemas.microsoft.com/winfx/2006/xaml");

    foreach (XElement item in xdoc.Root!.Element(XName.Get("Styles.Resources", ns.NamespaceName))!.Elements())
    {
        XAttribute? keyAtt = item.Attribute(XName.Get("Key", x.NamespaceName));

        string? key1 = keyAtt?.Value;
        if (key1 == null)
            continue;

        string key2 = key1.Split('.')[^1];
        string content = item.Value.ReplaceLineEndings("\n")
            .Replace("&#xa;", "\n")
            .Replace("\"", "\"\"");
        //if (content.Length > 0 && content[0] == '\n')
        //{
        //    var builder = new StringBuilder(content.Length);
        //    for (int i = 0; i < content.Length; i++)
        //    {
        //        char c1 = content[i];
        //        if (c1 is '\n')
        //        {
        //            if (i != 0)
        //            {
        //                builder.Append("\\n");
        //            }

        //            i++;
        //            for (int ii = i; ii < content.Length; ii++)
        //            {
        //                char c2 = content[ii];
        //                if (c2 is ' ')
        //                {
        //                    i++;
        //                }
        //                else
        //                {
        //                    i--;
        //                    break;
        //                }
        //            }
        //        }
        //        else if (c1 is '"')
        //        {
        //            builder.Append("\\\"");
        //        }
        //        else
        //        {
        //            builder.Append(c1);
        //        }
        //    }

        //    content = builder.ToString();
        //}

        if (item.Name.LocalName == "String")
        {
            var res = new StringResource(key1, key2, content);
            List<IResourceItem> list = GetOrCreate(res.GetPath(), dict);
            list.Add(res);
        }
        else if (item.Name.LocalName == "StaticResource")
        {
            XAttribute? resKeyAtt = item.Attribute(XName.Get("ResourceKey"));
            string resKey1 = resKeyAtt!.Value!;

            var res = new StaticResource(key1, key2, resKey1);
            List<IResourceItem> list = GetOrCreate(res.GetPath(), dict);
            list.Add(res);
        }
    }
}

List<IResourceItem> GetOrCreate(string key, Dictionary<string, List<IResourceItem>> dict)
{
    if (dict.TryGetValue(key, out List<IResourceItem>? value))
    {
        return value;
    }
    else
    {
        value = new List<IResourceItem>();
        dict[key] = value;
        return value;
    }
}

// 階層化
var hierarchizedLists = new Dictionary<string, HierarchizedList>();
var redirects = new Dictionary<string, string>();
foreach ((string key, List<IResourceItem> value) in dict)
{
    AddChild(hierarchizedLists, key, value);
}

void AddChild(Dictionary<string, HierarchizedList> hierarchizedLists, string key, List<IResourceItem> value)
{
    string[] splitted = key.Split('.');
    string curKey = splitted[0];
    if (!hierarchizedLists.TryGetValue(curKey, out HierarchizedList? current))
    {
        hierarchizedLists[curKey] = current = new HierarchizedList();
    }

    for (int i = 1; i < splitted.Length; i++)
    {
        curKey = splitted[i];

        if (current.Items.FirstOrDefault(x => x.Key == curKey) is { } match2)
        {
            redirects[match2.RawKey] = $"{match2.GetPath()}.Index{match2.Key}";
            match2.Key = $"Index{match2.Key}";
        }

        if (!current.Children.ContainsKey(curKey))
        {
            current.Children[curKey] = new HierarchizedList();
        }

        current = current.Children[curKey];

        if (i == splitted.Length - 1)
        {
            current.Items = value;
        }
    }
}

// C#コードを書き込み
var sb = new StringBuilder();
sb.AppendLine("// <auto-generated />");
sb.AppendLine($"namespace {RootNamespace};");
sb.AppendLine("#nullable enable");

sb.AppendLine($"public static class Resources {{");
int indent = 4;
foreach (KeyValuePair<string, HierarchizedList> item in hierarchizedLists)
{
    Write(sb, item.Key, item.Value, indent);
}

sb.AppendLine($"}}");

void Write(StringBuilder sb, string key, HierarchizedList value, int indent)
{
    Span<char> indentStr = stackalloc char[indent + 4];
    indentStr.Fill(' ');

    sb.AppendLine($"{indentStr[0..^4]}public static class {key} {{");
    foreach (IResourceItem item in value.Items)
    {
        item.GenerateGetValueCode(indentStr, sb, RootNamespace, redirects);
        item.GenerateGetObservableCode(indentStr, sb, RootNamespace, redirects);
    }

    foreach (KeyValuePair<string, HierarchizedList> item in value.Children)
    {
        Write(sb, item.Key, item.Value, indent + 4);
    }

    sb.AppendLine($"{indentStr[0..^4]}}}");
}

string str = sb.ToString();
Console.Write(str);

File.WriteAllText("Resources.g.cs", str);

namespace ResourcesGenerator
{
    public class HierarchizedList
    {
        public List<IResourceItem> Items { get; set; } = new();

        public Dictionary<string, HierarchizedList> Children { get; } = new();
    }

    public interface IResourceItem
    {
        string RawKey { get; set; }

        string Key { get; set; }

        string GetPath();

        void GenerateGetValueCode(Span<char> indentStr, StringBuilder sb, string rootNamespace, Dictionary<string, string> redirects);

        void GenerateGetObservableCode(Span<char> indentStr, StringBuilder sb, string rootNamespace, Dictionary<string, string> redirects);
    }

    public class StaticResource : IResourceItem
    {
        public StaticResource(string rawKey, string key, string target)
        {
            RawKey = rawKey;
            Key = key;
            Target = target;
        }

        public string RawKey { get; set; }

        public string Key { get; set; }

        public string Target { get; set; }

        public void GenerateGetObservableCode(Span<char> indentStr, StringBuilder sb, string rootNamespace, Dictionary<string, string> redirects)
        {
            if (redirects.TryGetValue(Target, out string? rawKey1))
            {
                sb.AppendLine($"{indentStr}public static IObservable<string> {Key}Observable => global::{rootNamespace}.Resources.{rawKey1}Observable;");
            }
            else
            {
                sb.AppendLine($"{indentStr}public static IObservable<string> {Key}Observable => global::{rootNamespace}.Resources.{Target}Observable;");
            }
        }

        public void GenerateGetValueCode(Span<char> indentStr, StringBuilder sb, string rootNamespace, Dictionary<string, string> redirects)
        {
            if (redirects.TryGetValue(Target, out string? rawKey1))
            {
                sb.AppendLine($"{indentStr}public static string {Key} => global::{rootNamespace}.Resources.{rawKey1};");
            }
            else
            {
                sb.AppendLine($"{indentStr}public static string {Key} => global::{rootNamespace}.Resources.{Target};");
            }
        }

        public string GetPath()
        {
            return RawKey.Substring(0, RawKey.Length - Key.Length - 1);
        }
    }

    public class StringResource : IResourceItem
    {
        public StringResource(string rawKey, string key, string value)
        {
            RawKey = rawKey;
            Key = key;
            Value = value;
        }

        public string RawKey { get; set; }

        public string Key { get; set; }

        public string Value { get; set; }

        public void GenerateGetObservableCode(Span<char> indentStr, StringBuilder sb, string rootNamespace, Dictionary<string, string> redirects)
        {
            sb.AppendLine($"{indentStr}private static IObservable<string>? _{Key}Observable;");

            if (redirects.TryGetValue(RawKey, out var rawKey1))
            {
                sb.AppendLine($"{indentStr}public static IObservable<string> {Key}Observable => _{Key}Observable ??= \"{RawKey}\".GetStringObservable(global::{rootNamespace}.Resources.{rawKey1});");
            }
            else
            {
                sb.AppendLine($"{indentStr}public static IObservable<string> {Key}Observable => _{Key}Observable ??= \"{RawKey}\".GetStringObservable(global::{rootNamespace}.Resources.{RawKey});");
            }
        }

        public void GenerateGetValueCode(Span<char> indentStr, StringBuilder sb, string rootNamespace, Dictionary<string, string> redirects)
        {
            sb.AppendLine($"{indentStr}public static string {Key} => \"{RawKey}\".GetStringResource(@\"{Value}\");");
        }

        public string GetPath()
        {
            return RawKey.Substring(0, RawKey.Length - Key.Length - 1);
        }
    }
}