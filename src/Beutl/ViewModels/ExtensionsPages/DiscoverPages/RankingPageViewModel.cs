﻿using Avalonia.Collections;

using Beutl.Api.Objects;
using Beutl.Api.Services;
using Reactive.Bindings;

namespace Beutl.ViewModels.ExtensionsPages.DiscoverPages;

public enum RankingType
{
    Overall,
    Daily,
    Weekly,
    Recently,
}

public record RankingModel(string DisplayName, RankingType Type);

public sealed class RankingPageViewModel : BasePageViewModel
{
    private readonly CompositeDisposable _disposables = new();
    private readonly DiscoverService _discover;

    public RankingPageViewModel(DiscoverService discover, RankingType rankingType)
    {
        Rankings = new RankingModel[]
        {
            new RankingModel(ExtensionsPage.Overall, RankingType.Overall),
            new RankingModel(ExtensionsPage.Daily, RankingType.Daily),
            new RankingModel(ExtensionsPage.Weekly, RankingType.Weekly),
            new RankingModel(ExtensionsPage.Recently, RankingType.Recently),
        };
        SelectedRanking = new ReactivePropertySlim<RankingModel>(Rankings.First(x => x.Type == rankingType));
        _discover = discover;

        Refresh = new AsyncReactiveCommand(IsBusy.Not())
            .WithSubscribe(async () =>
            {
                try
                {
                    IsBusy.Value = true;
                    Items.Clear();
                    Package[] array = await LoadItems(SelectedRanking.Value.Type, 0, 30);
                    Items.AddRange(array);

                    if (array.Length == 30)
                    {
                        Items.Add(null);
                    }
                }
                catch (Exception e)
                {
                    ErrorHandle(e);
                }
                finally
                {
                    IsBusy.Value = false;
                }
            })
            .DisposeWith(_disposables);

        SelectedRanking.Subscribe(_ => Refresh.Execute())
            .DisposeWith(_disposables);

        More = new AsyncReactiveCommand(IsBusy.Not())
            .WithSubscribe(async () =>
            {
                try
                {
                    IsBusy.Value = true;
                    Items.RemoveAt(Items.Count - 1);
                    Package[] array = await LoadItems(SelectedRanking.Value.Type, Items.Count, 30);
                    Items.AddRange(array);

                    if (array.Length == 30)
                    {
                        Items.Add(null);
                    }
                }
                catch (Exception e)
                {
                    ErrorHandle(e);
                }
                finally
                {
                    IsBusy.Value = false;
                }
            })
            .DisposeWith(_disposables);
    }

    public ReactivePropertySlim<RankingModel> SelectedRanking { get; }

    public RankingModel[] Rankings { get; }

    public AvaloniaList<Package?> Items { get; } = new();

    public AsyncReactiveCommand Refresh { get; }

    public AsyncReactiveCommand More { get; }

    public ReactivePropertySlim<bool> IsBusy { get; } = new();

    private async Task<Package[]> LoadItems(RankingType rankingType, int start, int count)
    {
        return rankingType switch
        {
            RankingType.Daily => await _discover.GetDailyRanking(start, count),
            RankingType.Weekly => await _discover.GetWeeklyRanking(start, count),
            RankingType.Recently => await _discover.GetRecentlyRanking(start, count),
            _ => await _discover.GetOverallRanking(start, count),
        };
    }

    public override void Dispose()
    {
        _disposables.Dispose();
    }
}