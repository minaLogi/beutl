﻿using Beutl.Api;
using Beutl.Api.Objects;

using Beutl.ViewModels.Dialogs;

using OpenTelemetry.Trace;

using Reactive.Bindings;

using Serilog;

using static Beutl.ViewModels.SettingsPages.StorageSettingsPageViewModel;

namespace Beutl.ViewModels.ExtensionsPages.DevelopPages;

public sealed class ReleasePageViewModel : BasePageViewModel
{
    private readonly ILogger _logger = Log.ForContext<ReleasePageViewModel>();
    private readonly CompositeDisposable _disposables = new();
    private readonly AuthorizedUser _user;

    public ReleasePageViewModel(AuthorizedUser user, Release release)
    {
        _user = user;
        Release = release;
        ActualAsset = Release.AssetId
            .SelectMany(async id =>
            {
                await _user.RefreshAsync();
                return id.HasValue ? await _user.Profile.GetAssetAsync(id.Value) : null;
            })
            .ToReadOnlyReactivePropertySlim()
            .DisposeWith(_disposables);

        Title = Release.Title
            .CopyToReactiveProperty()
            .DisposeWith(_disposables);
        Body = Release.Body
            .CopyToReactiveProperty()
            .DisposeWith(_disposables);
        Asset = ActualAsset
            .CopyToReactiveProperty()
            .DisposeWith(_disposables);

        IsChanging = Title.EqualTo(Release.Title)
            .AreTrue(
                Body.EqualTo(Release.Body),
                Asset.EqualTo(ActualAsset, (x, y) => x?.Id == y?.Id))
            .Not()
            .ToReadOnlyReactivePropertySlim()
            .DisposeWith(_disposables);

        CanPublish = Release.AssetId
            .Select(x => x.HasValue)
            .ToReadOnlyReactivePropertySlim()
            .DisposeWith(_disposables);

        Save = new AsyncReactiveCommand()
            .WithSubscribe(async () =>
            {
                using Activity? activity = Services.Telemetry.StartActivity("ReleasePage.Save");

                try
                {
                    using (await _user.Lock.LockAsync())
                    {
                        activity?.AddEvent(new("Entered_AsyncLock"));

                        await _user.RefreshAsync();

                        await Release.UpdateAsync(new UpdateReleaseRequest(
                            Asset.Value?.Id,
                            Body.Value,
                            Release.IsPublic.Value,
                            Title.Value));
                    }
                }
                catch (Exception ex)
                {
                    activity?.SetStatus(ActivityStatusCode.Error);
                    activity?.RecordException(ex);
                    ErrorHandle(ex);
                    _logger.Error(ex, "An unexpected error has occurred.");
                }
            })
            .DisposeWith(_disposables);

        DiscardChanges = new AsyncReactiveCommand()
            .WithSubscribe(async () =>
            {
                using Activity? activity = Services.Telemetry.StartActivity("ReleasePage.DiscardChanges");

                try
                {
                    Title.Value = Release.Title.Value;
                    Body.Value = Release.Body.Value;
                    if (Asset.Value?.Id != Release.AssetId.Value)
                    {
                        using (await _user.Lock.LockAsync())
                        {
                            activity?.AddEvent(new("Entered_AsyncLock"));

                            await _user.RefreshAsync();

                            Asset.Value = await Release.GetAssetAsync();
                        }
                    }
                }
                catch (Exception ex)
                {
                    activity?.SetStatus(ActivityStatusCode.Error);
                    activity?.RecordException(ex);
                    ErrorHandle(ex);
                    _logger.Error(ex, "An unexpected error has occurred.");
                }
            })
            .DisposeWith(_disposables);

        Delete = new AsyncReactiveCommand()
            .WithSubscribe(async () =>
            {
                using Activity? activity = Services.Telemetry.StartActivity("ReleasePage.Delete");

                try
                {
                    using (await _user.Lock.LockAsync())
                    {
                        activity?.AddEvent(new("Entered_AsyncLock"));

                        await _user.RefreshAsync();

                        await Release.DeleteAsync();
                    }
                }
                catch (Exception ex)
                {
                    activity?.SetStatus(ActivityStatusCode.Error);
                    activity?.RecordException(ex);
                    ErrorHandle(ex);
                    _logger.Error(ex, "An unexpected error has occurred.");
                }
            })
            .DisposeWith(_disposables);

        MakePublic = new AsyncReactiveCommand()
            .WithSubscribe(async () => await SetVisibility(true))
            .DisposeWith(_disposables);

        MakePrivate = new AsyncReactiveCommand()
            .WithSubscribe(async () => await SetVisibility(false))
            .DisposeWith(_disposables);

        Refresh = new AsyncReactiveCommand()
            .WithSubscribe(async () =>
            {
                using Activity? activity = Services.Telemetry.StartActivity("ReleasePage.Refresh");

                try
                {
                    using (await _user.Lock.LockAsync())
                    {
                        activity?.AddEvent(new("Entered_AsyncLock"));

                        IsBusy.Value = true;
                        await _user.RefreshAsync();

                        await Release.RefreshAsync();
                    }
                }
                catch (Exception ex)
                {
                    activity?.SetStatus(ActivityStatusCode.Error);
                    activity?.RecordException(ex);
                    ErrorHandle(ex);
                    _logger.Error(ex, "An unexpected error has occurred.");
                }
                finally
                {
                    IsBusy.Value = false;
                }
            })
            .DisposeWith(_disposables);
    }

    public Release Release { get; }

    public ReadOnlyReactivePropertySlim<Asset?> ActualAsset { get; }

    public ReactiveProperty<string?> Title { get; }

    public ReactiveProperty<string?> Body { get; }

    public ReactiveProperty<Asset?> Asset { get; }

    public ReadOnlyReactivePropertySlim<bool> IsChanging { get; }

    public AsyncReactiveCommand Save { get; }

    public AsyncReactiveCommand DiscardChanges { get; }

    public AsyncReactiveCommand Delete { get; }

    public ReadOnlyReactivePropertySlim<bool> CanPublish { get; }

    public AsyncReactiveCommand MakePublic { get; }

    public AsyncReactiveCommand MakePrivate { get; }

    public ReactivePropertySlim<bool> IsBusy { get; } = new();

    public AsyncReactiveCommand Refresh { get; }

    public SelectAssetViewModel SelectReleaseAsset()
    {
        return new SelectAssetViewModel(
            _user,
            x => ToKnownType(x) == KnownType.BeutlPackageFile,
            SharedFilePickerOptions.NuGetPackageFileType);
    }

    public override void Dispose()
    {
        _disposables.Dispose();
    }

    private async Task SetVisibility(bool isPublic)
    {
        using Activity? activity = Services.Telemetry.StartActivity("ReleasePage.SetVisibility");

        try
        {
            using (await _user.Lock.LockAsync())
            {
                activity?.AddEvent(new("Entered_AsyncLock"));

                await _user.RefreshAsync();

                await Release.UpdateAsync(isPublic: isPublic);
            }
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error);
            activity?.RecordException(ex);
            ErrorHandle(ex);
            _logger.Error(ex, "An unexpected error has occurred.");
        }
    }
}
