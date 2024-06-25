﻿// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.UI.Xaml;

namespace Snap.Hutao.ViewModel.Abstraction;

[HighQuality]
internal interface IViewModel : IPageScoped
{
    CancellationToken CancellationToken { get; set; }

    SemaphoreSlim DisposeLock { get; set; }

    IDeferContentLoader DeferContentLoader { get; set; }

    bool IsViewDisposed { get; set; }
}