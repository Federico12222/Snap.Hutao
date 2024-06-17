﻿// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Web.Hoyolab.HoyoPlay.Connect.Package;

internal sealed class Game
{
    [JsonPropertyName("major")]
    public Package? Major { get; set; }

    [JsonPropertyName("patches")]
    public List<Package> Patches { get; set; } = default!;
}
