﻿using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

using BeUtl.Pages.ExtensionsPages.DevelopPages;
using BeUtl.ViewModels.ExtensionsPages;
using BeUtl.ViewModels.ExtensionsPages.DevelopPages;

using FluentAvalonia.UI.Controls;

namespace BeUtl.Pages.ExtensionsPages;

public partial class DevelopPage : UserControl
{
    private bool _flag;

    public DevelopPage()
    {
        InitializeComponent();
        PackagesList.AddHandler(PointerPressedEvent, PackagesList_PointerPressed, RoutingStrategies.Tunnel);
        PackagesList.AddHandler(PointerReleasedEvent, PackagesList_PointerReleased, RoutingStrategies.Tunnel);
    }

    private void PackagesList_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_flag)
        {
            if (PackagesList.SelectedItem is PackageDetailsPageViewModel selectedItem)
            {
                Frame frame = this.FindAncestorOfType<Frame>();
                frame.Navigate(typeof(PackageDetailsPage), selectedItem, SharedNavigationTransitionInfo.Instance);
            }
            _flag = false;
        }
    }

    private void PackagesList_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            _flag = true;
        }
    }

    private void Edit_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is StyledElement { DataContext: PackageDetailsPageViewModel item })
        {
            Frame frame = this.FindAncestorOfType<Frame>();
            frame.Navigate(typeof(PackageDetailsPage), item, SharedNavigationTransitionInfo.Instance);
        }
    }

    private void CreateNewPackage_Click(object? sender, RoutedEventArgs e)
    {
        Frame frame = this.FindAncestorOfType<Frame>();
        if (DataContext is DevelopPageViewModel viewModel)
        {
            viewModel.CreateNewPackage.Execute(frame);
        }
    }
}