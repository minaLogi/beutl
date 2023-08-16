﻿using Avalonia.Platform.Storage;

using Beutl.Api;
using Beutl.Api.Objects;
using Beutl.Api.Services;

using Reactive.Bindings;

using Serilog;

namespace Beutl.ViewModels.ExtensionsPages.DevelopPages.Dialogs;

public sealed class UpdatePackageDialogViewModel
{
    private readonly ILogger _logger = Log.ForContext<UpdatePackageDialogViewModel>();
    private readonly AuthorizedUser _user;
    private readonly DiscoverService _discoverService;

    public UpdatePackageDialogViewModel(AuthorizedUser user, DiscoverService discoverService)
    {
        _user = user;
        _discoverService = discoverService;
        SelectedFile.Subscribe(async file =>
        {
            try
            {
                IsFileLoading.Value = true;
                LocalPackage.Value = null;
                if (file?.TryGetLocalPath() is string localPath)
                {
                    string ext = Path.GetExtension(localPath);
                    bool nuspec = ext is ".nuspec";

                    using (Stream stream = await file.OpenReadAsync().ConfigureAwait(false))
                    {
                        LocalPackage.Value = nuspec
                            ? Helper.ReadLocalPackageFromNuspecFile(stream)
                            : Helper.ReadLocalPackageFromNupkgFile(stream);
                    }
                }
            }
            catch (Exception ex)
            {
                Error.Value = Message.AnUnexpectedErrorHasOccurred;
                _logger.Error(ex, "An unexpected error has occurred.");
            }
            finally
            {
                IsFileLoading.Value = false;
            }
        });

        IsValid = LocalPackage
            .Select(x => x != null)
            .ToReadOnlyReactivePropertySlim();
    }

    public ReactiveProperty<IStorageFile?> SelectedFile { get; } = new();

    public ReactivePropertySlim<LocalPackage?> LocalPackage { get; } = new();

    public ReactiveProperty<bool> IsFileLoading { get; } = new();

    public ReadOnlyReactivePropertySlim<bool> IsValid { get; }

    public ReactivePropertySlim<string?> Error { get; } = new();

    public Release? Result { get; private set; }

    public async Task<Release?> UpdateAsync()
    {
        try
        {
            LocalPackage? localPackage = LocalPackage.Value;
            if (!IsValid.Value || localPackage == null)
            {
                return null;
            }

            await _user.RefreshAsync();

            var request = new UpdatePackageRequest(
                description: localPackage.Description,
                display_name: localPackage.DisplayName,
                logo_image_id: null,
                @public: null,
                screenshots: null,
                short_description: localPackage.ShortDescription,
                tags: localPackage.Tags,
                website: localPackage.WebSite);

            Package? package;
            try
            {
                package = await _discoverService.GetPackage(localPackage.Name);
            }
            catch
            {
                package = await _user.Profile.AddPackageAsync(localPackage.Name);
            }

            await package.UpdateAsync(
                description: localPackage.Description,
                displayName: localPackage.DisplayName,
                shortDescription: localPackage.ShortDescription,
                tags: localPackage.Tags,
                website: localPackage.WebSite);

            return Result = await package.AddReleaseAsync(
                localPackage.Version, new CreateReleaseRequest("", localPackage.Version));
        }
        catch (BeutlApiException<ApiErrorResponse> e)
        {
            Error.Value = e.Result.Message;
            _logger.Error(e, "API error occurred.");
            return null;
        }
        catch (Exception e)
        {
            Error.Value = Message.AnUnexpectedErrorHasOccurred;
            _logger.Error(e, "An unexpected error has occurred.");
            return null;
        }
    }
}
