﻿using Avalonia.Controls;
using Avalonia.Platform.Storage;

using Beutl.Api.Services;
using Beutl.Media.Decoding;
using Beutl.Media.Source;
using Beutl.ViewModels.Editors;

using FluentAvalonia.UI.Controls;

using Microsoft.Extensions.DependencyInjection;

namespace Beutl.Views.Editors;

public partial class SoundSourceEditor : UserControl
{
    public SoundSourceEditor()
    {
        InitializeComponent();
        button.Click += Button_Click;
    }

    private async void Button_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not SoundSourceEditorViewModel vm || VisualRoot is not TopLevel topLevel) return;

        string[] fileExtensions = DecoderRegistry.EnumerateDecoder()
            .SelectMany(x => x.AudioExtensions().Concat(x.VideoExtensions()))
            .Distinct()
            .Select(x =>
            {
                if (x.Contains('*', StringComparison.Ordinal))
                {
                    return x;
                }
                else
                {
                    if (x.StartsWith('.'))
                    {
                        return $"*{x}";
                    }
                    else
                    {
                        return $"*.{x}";
                    }
                }
            })
            .ToArray();

        if (fileExtensions.Length == 0)
        {
            var contentDialog = new ContentDialog()
            {
                Title = "対応している拡張子が見つかりませんでした",
                CloseButtonText = Strings.Close,
                PrimaryButtonText = "ドキュメントを表示"
            };

            if (await contentDialog.ShowAsync() == ContentDialogResult.Primary)
            {
                Process.Start(new ProcessStartInfo("https://github.com/b-editor/beutl-docs")
                {
                    UseShellExecute = true,
                    Verb = "open"
                });
            }
        }
        else
        {
            var options = new FilePickerOpenOptions
            {
                FileTypeFilter = new FilePickerFileType[]
                {
                    new FilePickerFileType("Audio File")
                    {
                        Patterns = fileExtensions
                    }
                }
            };

            IReadOnlyList<IStorageFile> result = await topLevel.StorageProvider.OpenFilePickerAsync(options);
            if (result.Count > 0
                && result[0].TryGetLocalPath() is string localPath
                && MediaSourceManager.Shared.OpenSoundSource(localPath, out ISoundSource? soundSource))
            {
                ISoundSource? oldValue = vm.WrappedProperty.GetValue();
                vm.SetValue(oldValue, soundSource);
                oldValue?.Dispose();
            }
        }
    }
}
