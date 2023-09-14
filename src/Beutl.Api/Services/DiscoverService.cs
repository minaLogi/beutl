﻿using System.Diagnostics;

using Beutl.Api.Objects;

namespace Beutl.Api.Services;

public class DiscoverService : IBeutlApiResource
{
    private readonly BeutlApiApplication _clients;

    public DiscoverService(BeutlApiApplication clients)
    {
        _clients = clients;
    }

    public MyAsyncLock Lock => _clients.Lock;

    public async Task<Package> GetPackage(string name)
    {
        using Activity? activity = _clients.ActivitySource.StartActivity("DiscoverService.GetPackage", ActivityKind.Client);

        PackageResponse package = await _clients.Packages.GetPackageAsync(name).ConfigureAwait(false);
        Profile owner = await GetProfile(package.Owner.Name).ConfigureAwait(false);

        return new Package(owner, package, _clients);
    }

    public async Task<Profile> GetProfile(string name)
    {
        using Activity? activity = _clients.ActivitySource.StartActivity("DiscoverService.GetProfile", ActivityKind.Client);

        ProfileResponse response = await _clients.Users.GetUserAsync(name).ConfigureAwait(false);
        return new Profile(response, _clients);
    }

    public async Task<Package[]> GetDailyRanking(int start = 0, int count = 30)
    {
        using Activity? activity = _clients.ActivitySource.StartActivity("DiscoverService.GetDailyRanking", ActivityKind.Client);
        activity?.SetTag("start", start);
        activity?.SetTag("count", count);

        return await (await _clients.Discover.GetDailyAsync(start, count).ConfigureAwait(false))
            .ToAsyncEnumerable()
            .SelectAwait(async x => await GetPackage(x.Name).ConfigureAwait(false))
            .ToArrayAsync()
            .ConfigureAwait(false);
    }

    public async Task<Package[]> GetWeeklyRanking(int start = 0, int count = 30)
    {
        using Activity? activity = _clients.ActivitySource.StartActivity("DiscoverService.GetWeeklyRanking", ActivityKind.Client);
        activity?.SetTag("start", start);
        activity?.SetTag("count", count);

        return await (await _clients.Discover.GetWeeklyAsync(start, count).ConfigureAwait(false))
            .ToAsyncEnumerable()
            .SelectAwait(async x => await GetPackage(x.Name).ConfigureAwait(false))
            .ToArrayAsync()
            .ConfigureAwait(false);
    }

    public async Task<Package[]> GetOverallRanking(int start = 0, int count = 30)
    {
        using Activity? activity = _clients.ActivitySource.StartActivity("DiscoverService.GetOverallRanking", ActivityKind.Client);
        activity?.SetTag("start", start);
        activity?.SetTag("count", count);

        return await (await _clients.Discover.GetOverallAsync(start, count).ConfigureAwait(false))
            .ToAsyncEnumerable()
            .SelectAwait(async x => await GetPackage(x.Name).ConfigureAwait(false))
            .ToArrayAsync()
            .ConfigureAwait(false);
    }

    public async Task<Package[]> GetRecentlyRanking(int start = 0, int count = 30)
    {
        using Activity? activity = _clients.ActivitySource.StartActivity("DiscoverService.GetRecentlyRanking", ActivityKind.Client);
        activity?.SetTag("start", start);
        activity?.SetTag("count", count);

        return await (await _clients.Discover.GetRecentlyAsync(start, count).ConfigureAwait(false))
            .ToAsyncEnumerable()
            .SelectAwait(async x => await GetPackage(x.Name).ConfigureAwait(false))
            .ToArrayAsync()
            .ConfigureAwait(false);
    }

    public async Task<Package[]> SearchPackages(string query, int start = 0, int count = 30)
    {
        using Activity? activity = _clients.ActivitySource.StartActivity("DiscoverService.SearchPackages", ActivityKind.Client);
        activity?.SetTag("start", start);
        activity?.SetTag("count", count);

        return await (await _clients.Discover.SearchPackagesAsync(query, start, count).ConfigureAwait(false))
            .ToAsyncEnumerable()
            .SelectAwait(async x => await GetPackage(x.Name).ConfigureAwait(false))
            .ToArrayAsync()
            .ConfigureAwait(false);
    }

    public async Task<Profile[]> SearchUsers(string query, int start = 0, int count = 30)
    {
        using Activity? activity = _clients.ActivitySource.StartActivity("DiscoverService.SearchUsers", ActivityKind.Client);
        activity?.SetTag("start", start);
        activity?.SetTag("count", count);

        return await (await _clients.Discover.SearchUsersAsync(query, start, count).ConfigureAwait(false))
            .ToAsyncEnumerable()
            .SelectAwait(async x => await GetProfile(x.Name).ConfigureAwait(false))
            .ToArrayAsync()
            .ConfigureAwait(false);
    }

    public async Task<Package[]> Search(string query, int start = 0, int count = 30)
    {
        return await (await _clients.Discover.SearchAsync(query, start, count).ConfigureAwait(false))
            .ToAsyncEnumerable()
            .SelectAwait(async x => await GetPackage(x.Name).ConfigureAwait(false))
            .ToArrayAsync()
            .ConfigureAwait(false);
    }
}
