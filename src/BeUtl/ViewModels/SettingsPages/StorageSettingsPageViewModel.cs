﻿using Avalonia.Collections;

using Beutl.Api;
using Beutl.Api.Objects;

using BeUtl.Configuration;
using BeUtl.Controls.Navigation;

using FluentIcons.Common;

using Reactive.Bindings;

namespace BeUtl.ViewModels.SettingsPages;

public sealed class StorageSettingsPageViewModel : PageContext
{
    private readonly BackupConfig _config;
    private readonly IReadOnlyReactiveProperty<AuthorizedUser?> _user;
    private readonly ReactivePropertySlim<StorageUsageResponse?> _storageUsageResponse = new();

    public StorageSettingsPageViewModel(IReadOnlyReactiveProperty<AuthorizedUser?> user)
    {
        _config = GlobalConfiguration.Instance.BackupConfig;
        BackupSettings = _config.GetObservable(BackupConfig.BackupSettingsProperty).ToReactiveProperty();
        BackupSettings.Subscribe(b => _config.BackupSettings = b);

        SignedIn = user.Select(x => x != null)
            .ToReadOnlyReactivePropertySlim();

        Percent = _storageUsageResponse.Where(x => x != null)
            .Select(x => x!.Size / (double)x.Max_size)
            .ToReadOnlyReactivePropertySlim();

        MaxSize = _storageUsageResponse.Where(x => x != null)
            .Select(x => ToHumanReadableSize(x!.Max_size))
            .ToReadOnlyReactivePropertySlim()!;

        UsedCapacity = _storageUsageResponse.Where(x => x != null)
            .Select(x => ToHumanReadableSize(x!.Size))
            .ToReadOnlyReactivePropertySlim()!;

        RemainingCapacity = _storageUsageResponse.Where(x => x != null)
            .Select(x => ToHumanReadableSize(x!.Max_size - x.Size))
            .ToReadOnlyReactivePropertySlim()!;

        _storageUsageResponse.Where(x => x != null)
            .Subscribe(x =>
            {
                DetailItem ToDetailItem(DetailItem defaultValue, long size)
                {
                    if (size == 0)
                    {
                        return defaultValue;
                    }

                    return new DetailItem(defaultValue.Type, ToHumanReadableSize(size), size, size / (double)x.Size);
                }

                Details.Clear();
                long imageSize = 0;
                long zipSize = 0;
                long bpkgSize = 0;
                long textSize = 0;
                long fontSize = 0;
                long otherSize = 0;

                foreach ((string key, long value) in x!.Details)
                {
                    switch (ToKnownType(key))
                    {
                        case KnownType.Image:
                            imageSize += value;
                            break;
                        case KnownType.Zip:
                            zipSize += value;
                            break;
                        case KnownType.BeutlPackageFile:
                            bpkgSize += value;
                            break;
                        case KnownType.Text:
                            textSize += value;
                            break;
                        case KnownType.Font:
                            fontSize += value;
                            break;
                        case KnownType.Other:
                            otherSize += value;
                            break;
                    }
                }

                Details.OrderedAddDescending(ToDetailItem(DetailItem.Image, imageSize), x => x.Size);
                Details.OrderedAddDescending(ToDetailItem(DetailItem.Zip, zipSize), x => x.Size);
                Details.OrderedAddDescending(ToDetailItem(DetailItem.BeutlPackageFile, bpkgSize), x => x.Size);
                Details.OrderedAddDescending(ToDetailItem(DetailItem.Text, textSize), x => x.Size);
                Details.OrderedAddDescending(ToDetailItem(DetailItem.Font, fontSize), x => x.Size);
                Details.OrderedAddDescending(ToDetailItem(DetailItem.Other, otherSize), x => x.Size);
            });

        _user = user;
        _user.Skip(1).Subscribe(async user =>
        {
            if (Refresh.CanExecute())
            {
                await Refresh.ExecuteAsync();
            }

            if (user == null)
            {
                INavigationProvider nav = await GetNavigation();
                bool goback = nav.CurrentContext is StorageDetailPageViewModel;
                await nav.RemoveAllAsync<StorageDetailPageViewModel>(_ => true, goback);
            }
        });
        Refresh.Subscribe(async () =>
        {
            if (_user.Value == null)
            {
                FillBlankItems();
                return;
            }

            try
            {
                IsBusy.Value = true;
                await _user.Value.RefreshAsync();
                _storageUsageResponse.Value = await _user.Value.StorageUsageAsync();
            }
            catch
            {
                // Todo
            }
            finally
            {
                IsBusy.Value = false;
            }
        });

        NavigateToDetail = new AsyncReactiveCommand<DetailItem>(SignedIn);
        NavigateToDetail.Subscribe(async item =>
        {
            INavigationProvider nav = await GetNavigation();
            await nav.NavigateAsync(
                x => x.Type == item.Type,
                () => new StorageDetailPageViewModel(_user.Value!, item.Type));
        });

        _user.Where(x => x != null)
            .Timeout(TimeSpan.FromSeconds(10))
            .Subscribe(
                _ => Refresh.Execute(),
                ex => { },
                () => { });
    }

    public ReactiveProperty<bool> BackupSettings { get; }

    public ReadOnlyReactivePropertySlim<bool> SignedIn { get; }

    public ReadOnlyReactivePropertySlim<double> Percent { get; }

    public ReadOnlyReactivePropertySlim<string> MaxSize { get; }

    public ReadOnlyReactivePropertySlim<string> UsedCapacity { get; }

    public ReadOnlyReactivePropertySlim<string> RemainingCapacity { get; }

    public AvaloniaList<DetailItem> Details { get; } = new(6);

    public ReactivePropertySlim<bool> IsBusy { get; } = new();

    public AsyncReactiveCommand Refresh { get; } = new();

    public AsyncReactiveCommand<DetailItem> NavigateToDetail { get; }

    public static KnownType ToKnownType(string mediaType)
    {
        if (mediaType.StartsWith("image/"))
            return KnownType.Image;
        else if (mediaType == "application/zip")
            return KnownType.Zip;
        else if (mediaType == "application/x-beutl-package")
            return KnownType.BeutlPackageFile;
        else if (mediaType is "application/json" or "application/xml" || mediaType.StartsWith("text/"))
            return KnownType.Text;
        else if (mediaType.StartsWith("font/"))
            return KnownType.Font;
        else
            return KnownType.Other;
    }

    // https://teratail.com/questions/136799#reply-207332
    // Todo: 移動
    public static string ToHumanReadableSize(double size, int scale = 0, int standard = 1024)
    {
        string[] unit = new[] { "B", "KB", "MB", "GB" };
        if (scale == unit.Length - 1 || size <= standard) { return $"{size:F} {unit[scale]}"; }
        return ToHumanReadableSize(size / standard, scale + 1, standard);
    }

    private void FillBlankItems()
    {
        Details.Clear();
        Details.Add(DetailItem.Image);
        Details.Add(DetailItem.Zip);
        Details.Add(DetailItem.BeutlPackageFile);
        Details.Add(DetailItem.Text);
        Details.Add(DetailItem.Font);
        Details.Add(DetailItem.Other);
    }

    public record DetailItem(KnownType Type, string UsedCapacity, long Size, double Percent)
    {
        public static readonly DetailItem Image = new(KnownType.Image, "0.00 B", 0, 0);
        public static readonly DetailItem Zip = new(KnownType.Zip, "0.00 B", 0, 0);
        public static readonly DetailItem BeutlPackageFile = new(KnownType.BeutlPackageFile, "0.00 B", 0, 0);
        public static readonly DetailItem Text = new(KnownType.Text, "0.00 B", 0, 0);
        public static readonly DetailItem Font = new(KnownType.Font, "0.00 B", 0, 0);
        public static readonly DetailItem Other = new(KnownType.Other, "0.00 B", 0, 0);

        public Symbol IconSymbol => Type switch
        {
            KnownType.Image => Symbol.Image,
            KnownType.Zip => Symbol.FolderZip,
            KnownType.BeutlPackageFile => Symbol.Code,
            KnownType.Text => Symbol.Document,
            KnownType.Font => Symbol.TextFont,
            KnownType.Other => Symbol.Folder,
            _ => Symbol.Folder,
        };

        public string DisplayName => Type.ToString();
    }

    public enum KnownType
    {
        // image/*
        Image,

        // application/zip
        Zip,

        // application/x-beutl-package
        BeutlPackageFile,

        // application/json
        // application/xml
        // text/*
        Text,

        // font/*
        Font,
        Other
    }
}
