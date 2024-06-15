﻿// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Extensions.Caching.Memory;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppNotifications;
using Snap.Hutao.Core.LifeCycle.InterProcess;
using Snap.Hutao.Core.Setting;
using Snap.Hutao.Core.Shell;
using Snap.Hutao.Core.Windowing;
using Snap.Hutao.Core.Windowing.HotKey;
using Snap.Hutao.Core.Windowing.NotifyIcon;
using Snap.Hutao.Service;
using Snap.Hutao.Service.DailyNote;
using Snap.Hutao.Service.Discord;
using Snap.Hutao.Service.Hutao;
using Snap.Hutao.Service.Job;
using Snap.Hutao.Service.Metadata;
using Snap.Hutao.Service.Navigation;
using Snap.Hutao.ViewModel.Guide;
using System.Diagnostics;

namespace Snap.Hutao.Core.LifeCycle;

/// <summary>
/// 激活
/// </summary>
[HighQuality]
[ConstructorGenerated]
[Injection(InjectAs.Singleton, typeof(IAppActivation))]
[SuppressMessage("", "CA1001")]
internal sealed partial class AppActivation : IAppActivation, IAppActivationActionHandlersAccess, IDisposable
{
    public const string Action = nameof(Action);
    public const string Uid = nameof(Uid);
    public const string LaunchGame = nameof(LaunchGame);
    public const string ImportUIAFFromClipboard = nameof(ImportUIAFFromClipboard);

    private const string CategoryAchievement = "ACHIEVEMENT";
    private const string CategoryDailyNote = "DAILYNOTE";
    private const string UrlActionImport = "/IMPORT";
    private const string UrlActionRefresh = "/REFRESH";

    private readonly ICurrentXamlWindowReference currentWindowReference;
    private readonly IServiceProvider serviceProvider;
    private readonly ITaskContext taskContext;

    private readonly SemaphoreSlim activateSemaphore = new(1);

    /// <inheritdoc/>
    public void Activate(HutaoActivationArguments args)
    {
        HandleActivationAsync(args).SafeForget();
    }

    public void NotificationActivate(AppNotificationManager manager, AppNotificationActivatedEventArgs args)
    {
        if (args.Arguments.TryGetValue(Action, out string? action))
        {
            if (action == LaunchGame)
            {
                _ = args.Arguments.TryGetValue(Uid, out string? uid);
                HandleLaunchGameActionAsync(uid).SafeForget();
            }
        }
    }

    /// <inheritdoc/>
    public void PostInitialization()
    {
        RunPostInitializationAsync().SafeForget();

        async ValueTask RunPostInitializationAsync()
        {
            await taskContext.SwitchToBackgroundAsync();

            serviceProvider.GetRequiredService<PrivateNamedPipeServer>().RunAsync().SafeForget();

            using (await activateSemaphore.EnterAsync().ConfigureAwait(false))
            {
                // TODO: Introduced in 1.10.2, remove in later version
                serviceProvider.GetRequiredService<IJumpListInterop>().ClearAsync().SafeForget();
                serviceProvider.GetRequiredService<IScheduleTaskInterop>().UnregisterAllTasks();

                if (UnsafeLocalSetting.Get(SettingKeys.Major1Minor10Revision0GuideState, GuideState.Language) < GuideState.Completed)
                {
                    return;
                }

                await taskContext.SwitchToMainThreadAsync();
                serviceProvider.GetRequiredService<HotKeyOptions>().RegisterAll();

                if (serviceProvider.GetRequiredService<AppOptions>().IsNotifyIconEnabled)
                {
                    XamlLifetime.ApplicationLaunchedWithNotifyIcon = true;

                    await taskContext.SwitchToMainThreadAsync();
                    serviceProvider.GetRequiredService<App>().DispatcherShutdownMode = DispatcherShutdownMode.OnExplicitShutdown;
                    _ = serviceProvider.GetRequiredService<NotifyIconController>();
                }

                serviceProvider.GetRequiredService<IQuartzService>().StartAsync(default).SafeForget();
            }
        }
    }

    public void Dispose()
    {
        activateSemaphore.Dispose();
    }

    public async ValueTask HandleLaunchGameActionAsync(string? uid = null)
    {
        serviceProvider
            .GetRequiredService<IMemoryCache>()
            .Set(ViewModel.Game.LaunchGameViewModel.DesiredUid, uid);

        await taskContext.SwitchToMainThreadAsync();

        switch (currentWindowReference.Window)
        {
            case null:
                LaunchGameWindow launchGameWindow = serviceProvider.GetRequiredService<LaunchGameWindow>();
                currentWindowReference.Window = launchGameWindow;

                launchGameWindow.SwitchTo();
                launchGameWindow.BringToForeground();
                return;

            case MainWindow:
                await serviceProvider
                    .GetRequiredService<INavigationService>()
                    .NavigateAsync<View.Page.LaunchGamePage>(INavigationAwaiter.Default, true)
                    .ConfigureAwait(false);
                return;

            case LaunchGameWindow currentLaunchGameWindow:
                currentLaunchGameWindow.SwitchTo();
                currentLaunchGameWindow.BringToForeground();
                return;

            default:
                Process.GetCurrentProcess().Kill();
                return;
        }
    }

    private async ValueTask HandleActivationAsync(HutaoActivationArguments args)
    {
        await taskContext.SwitchToBackgroundAsync();

        if (activateSemaphore.CurrentCount > 0)
        {
            using (await activateSemaphore.EnterAsync().ConfigureAwait(false))
            {
                await HandleActivationCoreAsync(args).ConfigureAwait(false);
            }
        }
    }

    private async ValueTask HandleActivationCoreAsync(HutaoActivationArguments args)
    {
        switch (args.Kind)
        {
            case HutaoActivationKind.Protocol:
                {
                    ArgumentNullException.ThrowIfNull(args.ProtocolActivatedUri);
                    await HandleUrlActivationAsync(args.ProtocolActivatedUri, args.IsRedirectTo).ConfigureAwait(false);
                    break;
                }

            case HutaoActivationKind.Launch:
                {
                    ArgumentNullException.ThrowIfNull(args.LaunchActivatedArguments);
                    switch (args.LaunchActivatedArguments)
                    {
                        default:
                            {
                                await HandleNormalLaunchActionAsync(args.IsRedirectTo).ConfigureAwait(false);
                                break;
                            }
                    }

                    break;
                }

            case HutaoActivationKind.Toast:
                {
                    break;
                }
        }
    }

    private async ValueTask HandleNormalLaunchActionAsync(bool isRedirectTo)
    {
        if (!isRedirectTo)
        {
            // Increase launch times
            LocalSetting.Update(SettingKeys.LaunchTimes, 0, x => unchecked(x + 1));

            // If the guide is completed, we check if there's any unfulfilled resource category present.
            if (UnsafeLocalSetting.Get(SettingKeys.Major1Minor10Revision0GuideState, GuideState.Language) >= GuideState.StaticResourceBegin)
            {
                if (StaticResource.IsAnyUnfulfilledCategoryPresent())
                {
                    UnsafeLocalSetting.Set(SettingKeys.Major1Minor10Revision0GuideState, GuideState.StaticResourceBegin);
                }
            }

            if (UnsafeLocalSetting.Get(SettingKeys.Major1Minor10Revision0GuideState, GuideState.Language) < GuideState.Completed)
            {
                await taskContext.SwitchToMainThreadAsync();

                GuideWindow guideWindow = serviceProvider.GetRequiredService<GuideWindow>();
                currentWindowReference.Window = guideWindow;

                guideWindow.SwitchTo();
                guideWindow.BringToForeground();
                return;
            }
        }

        await WaitMainWindowOrCurrentAsync().ConfigureAwait(false);
    }

    private async ValueTask WaitMainWindowOrCurrentAsync()
    {
        if (currentWindowReference.Window is { } window)
        {
            window.SwitchTo();
            window.BringToForeground();
            return;
        }

        await taskContext.SwitchToMainThreadAsync();

        MainWindow mainWindow = serviceProvider.GetRequiredService<MainWindow>();
        currentWindowReference.Window = mainWindow;

        mainWindow.SwitchTo();
        mainWindow.BringToForeground();

        await taskContext.SwitchToBackgroundAsync();

        if (serviceProvider.GetRequiredService<IMetadataService>() is IMetadataServiceInitialization metadataServiceInitialization)
        {
            metadataServiceInitialization.InitializeInternalAsync().SafeForget();
        }

        if (serviceProvider.GetRequiredService<IHutaoUserService>() is IHutaoUserServiceInitialization hutaoUserServiceInitialization)
        {
            hutaoUserServiceInitialization.InitializeInternalAsync().SafeForget();
        }

        serviceProvider.GetRequiredService<IDiscordService>().SetNormalActivityAsync().SafeForget();
    }

    private async ValueTask HandleUrlActivationAsync(Uri uri, bool isRedirectTo)
    {
        UriBuilder builder = new(uri);

        string category = builder.Host.ToUpperInvariant();
        string action = builder.Path.ToUpperInvariant();
        string parameter = builder.Query.ToUpperInvariant();

        switch (category)
        {
            case CategoryAchievement:
                {
                    await WaitMainWindowOrCurrentAsync().ConfigureAwait(false);
                    await HandleAchievementActionAsync(action, parameter, isRedirectTo).ConfigureAwait(false);
                    break;
                }

            case CategoryDailyNote:
                {
                    await HandleDailyNoteActionAsync(action, parameter, isRedirectTo).ConfigureAwait(false);
                    break;
                }

            default:
                {
                    await HandleNormalLaunchActionAsync(isRedirectTo).ConfigureAwait(false);
                    break;
                }
        }
    }

    private async ValueTask HandleAchievementActionAsync(string action, string parameter, bool isRedirectTo)
    {
        _ = parameter;
        _ = isRedirectTo;
        switch (action)
        {
            case UrlActionImport:
                {
                    await taskContext.SwitchToMainThreadAsync();

                    INavigationAwaiter navigationAwaiter = new NavigationExtra(ImportUIAFFromClipboard);
                    await serviceProvider
                        .GetRequiredService<INavigationService>()
                        .NavigateAsync<View.Page.AchievementPage>(navigationAwaiter, true)
                        .ConfigureAwait(false);
                    break;
                }
        }
    }

    private async ValueTask HandleDailyNoteActionAsync(string action, string parameter, bool isRedirectTo)
    {
        _ = parameter;
        switch (action)
        {
            case UrlActionRefresh:
                {
                    try
                    {
                        await serviceProvider
                            .GetRequiredService<IDailyNoteService>()
                            .RefreshDailyNotesAsync()
                            .ConfigureAwait(false);
                    }
                    catch
                    {
                    }

                    // Check if it's redirected.
                    if (!isRedirectTo)
                    {
                        // It's a direct open process, should exit immediately.
                        Process.GetCurrentProcess().Kill();
                    }

                    break;
                }
        }
    }
}