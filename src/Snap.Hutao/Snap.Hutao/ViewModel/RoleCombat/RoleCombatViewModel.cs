﻿// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.Messaging;
using Snap.Hutao.Factory.ContentDialog;
using Snap.Hutao.Service.Hutao;
using Snap.Hutao.Service.Navigation;
using Snap.Hutao.Service.Notification;
using Snap.Hutao.Service.RoleCombat;
using Snap.Hutao.Service.User;
using Snap.Hutao.UI.Xaml.Data;
using Snap.Hutao.ViewModel.Complex;
using Snap.Hutao.ViewModel.User;
using System.Collections.ObjectModel;

namespace Snap.Hutao.ViewModel.RoleCombat;

[ConstructorGenerated]
[Injection(InjectAs.Scoped)]
internal sealed partial class RoleCombatViewModel : Abstraction.ViewModel, IRecipient<UserAndUidChangedMessage>
{
    private readonly IRoleCombatService roleCombatService;
    private readonly IContentDialogFactory contentDialogFactory;
    private readonly INavigationService navigationService;
    private readonly IServiceProvider serviceProvider;
    private readonly IInfoBarService infoBarService;
    private readonly ITaskContext taskContext;
    private readonly IUserService userService;
    private readonly HutaoDatabaseViewModel hutaoDatabaseViewModel;
    private readonly HutaoUserOptions hutaoUserOptions;

    private AdvancedCollectionView<RoleCombatView>? roleCombatEntries;

    public AdvancedCollectionView<RoleCombatView>? RoleCombatEntries { get => roleCombatEntries; set => SetProperty(ref roleCombatEntries, value); }

    public HutaoDatabaseViewModel HutaoDatabaseViewModel { get => hutaoDatabaseViewModel; }

    public void Receive(UserAndUidChangedMessage message)
    {
        if (message.UserAndUid is { } userAndUid)
        {
            _ = UpdateRoleCombatCollectionAsync(userAndUid);
        }
        else
        {
            RoleCombatEntries?.MoveCurrentTo(default);
        }
    }

    protected override async ValueTask<bool> LoadOverrideAsync()
    {
        if (await roleCombatService.InitializeAsync().ConfigureAwait(false))
        {
            if (await userService.GetCurrentUserAndUidAsync().ConfigureAwait(false) is { } userAndUid)
            {
                await UpdateRoleCombatCollectionAsync(userAndUid).ConfigureAwait(false);
            }
            else
            {
                infoBarService.Warning(SH.MustSelectUserAndUid);
            }
        }

        return true;
    }

    [SuppressMessage("", "SH003")]
    private async Task UpdateRoleCombatCollectionAsync(UserAndUid userAndUid)
    {
        try
        {
            ObservableCollection<RoleCombatView> collection;
            using (await EnterCriticalSectionAsync().ConfigureAwait(false))
            {
                collection = await roleCombatService
                    .GetRoleCombatViewCollectionAsync(userAndUid)
                    .ConfigureAwait(false);
            }

            AdvancedCollectionView<RoleCombatView> roleCombatEntries = collection.ToAdvancedCollectionView();

            await taskContext.SwitchToMainThreadAsync();
            RoleCombatEntries = roleCombatEntries;
            RoleCombatEntries.MoveCurrentTo(RoleCombatEntries.SourceCollection.FirstOrDefault(s => s.Engaged));
        }
        catch (OperationCanceledException)
        {
        }
    }

    [Command("RefreshCommand")]
    private async Task RefreshAsync()
    {
        if (RoleCombatEntries is not null)
        {
            if (await userService.GetCurrentUserAndUidAsync().ConfigureAwait(false) is { } userAndUid)
            {
                try
                {
                    using (await EnterCriticalSectionAsync().ConfigureAwait(false))
                    {
                        await roleCombatService
                            .RefreshRoleCombatAsync(userAndUid)
                            .ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException)
                {
                }

                await taskContext.SwitchToMainThreadAsync();
                RoleCombatEntries.MoveCurrentTo(RoleCombatEntries.SourceCollection.FirstOrDefault(s => s.Engaged));
            }
        }
    }

    // [Command("UploadRoleCombatRecordCommand")]
    // private async Task UploadRoleCombatRecordAsync()
    // {
    //     if (await userService.GetCurrentUserAndUidAsync().ConfigureAwait(false) is { } userAndUid)
    //     {
    //         if (!hutaoUserOptions.IsLoggedIn)
    //         {
    //             SpiralAbyssUploadRecordHomaNotLoginDialog dialog = await contentDialogFactory
    //                 .CreateInstanceAsync<SpiralAbyssUploadRecordHomaNotLoginDialog>()
    //                 .ConfigureAwait(false);
    //
    //             await taskContext.SwitchToMainThreadAsync();
    //             ContentDialogResult result = await contentDialogFactory.EnqueueAndShowAsync(dialog).ShowTask.ConfigureAwait(false);
    //
    //             switch (result)
    //             {
    //                 case ContentDialogResult.Primary:
    //                     await navigationService.NavigateAsync<SettingPage>(INavigationAwaiter.Default, true).ConfigureAwait(false);
    //                     return;
    //
    //                 case ContentDialogResult.Secondary:
    //                     break;
    //
    //                 case ContentDialogResult.None:
    //                     return;
    //             }
    //         }
    //
    //         using (IServiceScope scope = serviceProvider.CreateScope())
    //         {
    //             HutaoSpiralAbyssClient spiralAbyssClient = scope.ServiceProvider.GetRequiredService<HutaoSpiralAbyssClient>();
    //             if (await spiralAbyssClient.GetPlayerRecordAsync(userAndUid).ConfigureAwait(false) is { } record)
    //             {
    //                 Web.Response.Response response = await spiralAbyssClient.UploadRecordAsync(record).ConfigureAwait(false);
    //
    //                 if (response is ILocalizableResponse localizableResponse)
    //                 {
    //                     infoBarService.PrepareInfoBarAndShow(builder =>
    //                     {
    //                         builder
    //                         .SetSeverity(response is { ReturnCode: 0 } ? InfoBarSeverity.Success : InfoBarSeverity.Warning)
    //                         .SetMessage(localizableResponse.GetLocalizationMessage());
    //                     });
    //                 }
    //             }
    //         }
    //     }
    //     else
    //     {
    //         infoBarService.Warning(SH.MustSelectUserAndUid);
    //     }
    // }
}