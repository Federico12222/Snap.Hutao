﻿// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.WinUI.Animations;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Snap.Hutao.Control.Animation;
using Snap.Hutao.Control.Theme;
using Snap.Hutao.Message;
using Snap.Hutao.Service.BackgroundImage;

namespace Snap.Hutao.ViewModel;

[ConstructorGenerated]
[Injection(InjectAs.Singleton)]
internal sealed partial class MainViewModel : Abstraction.ViewModel, IMainViewModelInitialization, IRecipient<BackgroundImageTypeChangedMessage>
{
    private readonly IBackgroundImageService backgroundImageService;
    private readonly ITaskContext taskContext;

    private BackgroundImage? previousBackgroundImage;
    private Image? backgroundImagePresenter;

    public void Initialize(IBackgroundImagePresenterAccessor accessor)
    {
        backgroundImagePresenter = accessor.BackgroundImagePresenter;
    }

    public void Receive(BackgroundImageTypeChangedMessage message)
    {
        UpdateBackgroundAsync().SafeForget();
    }

    [Command("UpdateBackgroundCommand")]
    private async Task UpdateBackgroundAsync()
    {
        if (backgroundImagePresenter is null)
        {
            return;
        }

        (bool isOk, BackgroundImage backgroundImage) = await backgroundImageService.GetNextBackgroundImageAsync(previousBackgroundImage).ConfigureAwait(false);

        if (isOk)
        {
            previousBackgroundImage = backgroundImage;
            await taskContext.SwitchToMainThreadAsync();

            await AnimationBuilder
                .Create()
                .Opacity(
                    to: 0D,
                    duration: ControlAnimationConstants.ImageOpacityFadeInOut,
                    easingType: EasingType.Quartic,
                    easingMode: EasingMode.EaseInOut)
                .StartAsync(backgroundImagePresenter)
                .ConfigureAwait(true);

            backgroundImagePresenter.Source = backgroundImage.ImageSource;
            double targetOpacity = ThemeHelper.IsDarkMode(backgroundImagePresenter.ActualTheme) ? 1 - backgroundImage.Luminance : backgroundImage.Luminance;

            await AnimationBuilder
                .Create()
                .Opacity(
                    to: targetOpacity,
                    duration: ControlAnimationConstants.ImageOpacityFadeInOut,
                    easingType: EasingType.Quartic,
                    easingMode: EasingMode.EaseInOut)
                .StartAsync(backgroundImagePresenter)
                .ConfigureAwait(true);
        }
    }
}