// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using Snap.Hutao.Model.Entity;
using Snap.Hutao.Service.Game;
using Snap.Hutao.Service.Game.Scheme;
using Snap.Hutao.Service.Notification;
using Snap.Hutao.Service.User;
using Snap.Hutao.UI.Xaml.Data;
using Snap.Hutao.UI.Xaml.View.Page;
using Snap.Hutao.ViewModel.User;
using System.Collections.ObjectModel;

namespace Snap.Hutao.ViewModel.Game;

[Injection(InjectAs.Transient)]
[ConstructorGenerated(CallBaseConstructor = true)]
internal sealed partial class LaunchGameViewModelSlim : Abstraction.ViewModelSlim<LaunchGamePage>, IViewModelSupportLaunchExecution
{
    private readonly LaunchGameShared launchGameShared;
    private readonly IInfoBarService infoBarService;
    private readonly IGameService gameService;
    private readonly IUserService userService;
    private readonly ITaskContext taskContext;

    private GameAccountFilter? gameAccountFilter;

    LaunchGameShared IViewModelSupportLaunchExecution.Shared { get => launchGameShared; }

    public partial LaunchStatusOptions LaunchStatusOptions { get; }

    public partial LaunchOptions LaunchOptions { get; }

    [ObservableProperty]
    public partial AdvancedCollectionView<GameAccount>? GameAccountsView { get; set; }

    public GameAccount? SelectedGameAccount { get => GameAccountsView?.CurrentItem; }

    protected override async Task LoadAsync()
    {
        LaunchScheme? scheme = launchGameShared.GetCurrentLaunchSchemeFromConfigFile();
        ObservableCollection<GameAccount> accounts = await gameService.GetGameAccountCollectionAsync().ConfigureAwait(false);

        gameAccountFilter = new(scheme?.GetSchemeType());
        AdvancedCollectionView<GameAccount> accountsView = new(accounts) { Filter = gameAccountFilter.Filter };

        await taskContext.SwitchToMainThreadAsync();
        GameAccountsView = accountsView;

        try
        {
            if (scheme is not null)
            {
                // Try set to the current account.
                await taskContext.SwitchToMainThreadAsync();
                GameAccountsView.CurrentItem ??= gameService.DetectCurrentGameAccount(scheme);
            }
        }
        catch (Exception ex)
        {
            infoBarService.Error(ex);
        }
    }

    [Command("LaunchCommand")]
    private async Task LaunchAsync()
    {
        UserAndUid? userAndUid = await userService.GetCurrentUserAndUidAsync().ConfigureAwait(false);
        await this.LaunchExecutionAsync(launchGameShared.GetCurrentLaunchSchemeFromConfigFile(), userAndUid).ConfigureAwait(false);
    }
}