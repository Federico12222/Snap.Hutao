﻿// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Service.AvatarInfo.Factory.Builder;

internal sealed class AvatarViewBuilder : IAvatarViewBuilder
{
    public ViewModel.AvatarProperty.AvatarView AvatarView { get; } = new();
}