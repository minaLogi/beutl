﻿using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;

using BeUtl.Pages.ExtensionsPages.DevelopPages.Dialogs;
using BeUtl.ViewModels.ExtensionsPages.DevelopPages;
using BeUtl.ViewModels.ExtensionsPages.DevelopPages.Dialogs;

using FluentAvalonia.UI.Controls;

using Button = Avalonia.Controls.Button;
using S = BeUtl.Language.StringResources;

namespace BeUtl.Pages.ExtensionsPages.DevelopPages;

public partial class PackageSettingsPage : UserControl
{
    public PackageSettingsPage()
    {
        InitializeComponent();
        ScreenshotsScrollViewer.AddHandler(PointerWheelChangedEvent, ScreenshotsScrollViewer_PointerWheelChanged, RoutingStrategies.Tunnel);
    }

    private void ScreenshotsScrollViewer_PointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        Avalonia.Vector offset = ScreenshotsScrollViewer.Offset;

        // オフセット(X) をスクロール
        ScreenshotsScrollViewer.Offset = offset.WithX(offset.X - (e.Delta.Y * 50));

        e.Handled = true;
    }

    private void NavigatePackageDetailsPage_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is PackageSettingsPageViewModel viewModel)
        {
            Frame frame = this.FindAncestorOfType<Frame>();
            frame.Navigate(typeof(PackageDetailsPage), viewModel.Parent, SharedNavigationTransitionInfo.Instance);
        }
    }

    private async void AddResource_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is PackageSettingsPageViewModel viewModel)
        {
            var dialog = new AddResourceDialog
            {
                DataContext = new AddResourceDialogViewModel(viewModel.Parent.Package.Value)
            };
            await dialog.ShowAsync();
        }
    }

    private void EditResource_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: ResourcePageViewModel itemViewModel })
        {
            Frame frame = this.FindAncestorOfType<Frame>();
            frame.Navigate(typeof(ResourcePage), itemViewModel, SharedNavigationTransitionInfo.Instance);
        }
    }

    private async void DeleteResource_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: ResourcePageViewModel itemViewModel })
        {
            Frame frame = this.FindAncestorOfType<Frame>();
            var dialog = new ContentDialog
            {
                Title = S.DevelopPage.DeleteResource.Title,
                Content = S.DevelopPage.DeleteResource.Content,
                PrimaryButtonText = S.Common.Yes,
                CloseButtonText = S.Common.No,
                DefaultButton = ContentDialogButton.Primary
            };

            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                string resourceId = itemViewModel.Resource.Value.Snapshot.Id;
                string packageId = itemViewModel.Parent.Reference.Id;
                frame.RemoveAllStack(item => item is ResourcePageViewModel p
                    && p.Resource.Value.Snapshot.Id == resourceId
                    && p.Parent.Reference.Id == packageId);

                itemViewModel.Delete.Execute();
            }
        }
    }

    private async void DeletePackage_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is PackageSettingsPageViewModel viewModel)
        {
            Frame frame = this.FindAncestorOfType<Frame>();
            var dialog = new ContentDialog
            {
                Title = S.DevelopPage.DeletePackage.Title,
                Content = S.DevelopPage.DeletePackage.Content,
                PrimaryButtonText = S.Common.Yes,
                CloseButtonText = S.Common.No,
                DefaultButton = ContentDialogButton.Primary
            };

            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                string packageId = viewModel.Reference.Id;
                frame.RemoveAllStack(
                    item => (item is PackageDetailsPageViewModel p1 && p1.Package.Value.Snapshot.Id == packageId)
                         || (item is PackageSettingsPageViewModel p2 && p2.Reference.Id == packageId)
                         || (item is ResourcePageViewModel p3 && p3.Parent.Reference.Id == packageId)
                         || (item is ReleasePageViewModel p4 && p4.Parent.Parent.Package.Value.Snapshot.Id == packageId)
                         || (item is PackageReleasesPageViewModel p5 && p5.Parent.Package.Value.Snapshot.Id == packageId));

                viewModel.Delete.Execute();

                frame.GoBack();
            }
        }
    }

    private async void MakePublic_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is PackageSettingsPageViewModel viewModel)
        {
            var dialog = new ContentDialog
            {
                Title = S.DevelopPage.MakePublicPackage.Title,
                Content = S.DevelopPage.MakePublicPackage.Content,
                PrimaryButtonText = S.Common.Yes,
                CloseButtonText = S.Common.No,
                DefaultButton = ContentDialogButton.Primary
            };

            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                viewModel.MakePublic.Execute();
            }
        }
    }

    private async void MakePrivate_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is PackageSettingsPageViewModel viewModel)
        {
            var dialog = new ContentDialog
            {
                Title = S.DevelopPage.MakePrivatePackage.Title,
                Content = S.DevelopPage.MakePrivatePackage.Content,
                PrimaryButtonText = S.Common.Yes,
                CloseButtonText = S.Common.No,
                DefaultButton = ContentDialogButton.Primary
            };

            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                viewModel.MakePrivate.Execute();
            }
        }
    }

    private async void OpenLogoFile_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is PackageSettingsPageViewModel viewModel)
        {
            Window? window = this.FindLogicalAncestorOfType<Window>();
            var dialog = new OpenFileDialog
            {
                AllowMultiple = false,
                Filters = new()
                {
                    new FileDialogFilter()
                    {
                        Extensions = { "jpg", "jpeg", "png" }
                    }
                }
            };
            if ((await dialog.ShowAsync(window)) is string[] items && items.Length > 0)
            {
                viewModel.SetLogo.Execute(items[0]);
            }
        }
    }

    private async void AddScreenshotFile_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is PackageSettingsPageViewModel viewModel)
        {
            Window? window = this.FindLogicalAncestorOfType<Window>();
            var dialog = new OpenFileDialog
            {
                AllowMultiple = true,
                Filters = new()
                {
                    new FileDialogFilter()
                    {
                        Extensions = { "jpg", "jpeg", "png" }
                    }
                }
            };
            if ((await dialog.ShowAsync(window)) is string[] items && items.Length > 0)
            {
                foreach (string item in items)
                {
                    viewModel.AddScreenshot.Execute(item);
                }
            }
        }
    }
}