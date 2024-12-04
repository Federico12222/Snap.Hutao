// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Web.Hoyolab.DataSigning;
using System.Collections.Frozen;
using System.Security.Cryptography;

namespace Snap.Hutao.Web.Hoyolab;

/// <summary>
/// 米游社选项
/// </summary>
internal static class HoyolabOptions
{
    /// <summary>
    /// 米游社请求UA
    /// </summary>
    public const string UserAgent = $"Mozilla/5.0 (Windows NT 10.0; Win64; x64) miHoYoBBS/{SaltConstants.CNVersion}";

    /// <summary>
    /// Hoyolab 请求UA
    /// </summary>
    public const string UserAgentOversea = $"Mozilla/5.0 (Windows NT 10.0; Win64; x64) miHoYoBBSOversea/{SaltConstants.OSVersion}";

    /// <summary>
    /// 米游社移动端请求UA
    /// </summary>
    public const string MobileUserAgent = $"Mozilla/5.0 (Linux; Android 12) Mobile miHoYoBBS/{SaltConstants.CNVersion}";

    /// <summary>
    /// Hoyolab 移动端请求UA
    /// </summary>
    public const string MobileUserAgentOversea = $"Mozilla/5.0 (Linux; Android 12) Mobile miHoYoBBSOversea/{SaltConstants.OSVersion}";

    public const string HoyoPlayUserAgent = $"HYPContainer/1.1.4.133";

    public const string ToolVersion = "v4.2.2-ys";

    /// <summary>
    /// 米游社设备Id
    /// </summary>
    public static string DeviceId36 { get; } = Guid.NewGuid().ToString();

    /// <summary>
    /// 扫码登录设备Id
    /// </summary>
    public static string DeviceId40 { get; } = GenerateDeviceId40();

    /// <summary>
    /// 盐
    /// </summary>
    public static FrozenDictionary<SaltType, string> Salts { get; } = FrozenDictionary.ToFrozenDictionary(
    [

        // Chinese
        KeyValuePair.Create(SaltType.K2, SaltConstants.CNK2),
        KeyValuePair.Create(SaltType.LK2, SaltConstants.CNLK2),
        KeyValuePair.Create(SaltType.X4, "xV8v4Qu54lUKrEYFZkJhB8cuOh9Asafs"),
        KeyValuePair.Create(SaltType.X6, "t0qEgfub6cvueAPgR5m9aQWWVciEer7v"),
        KeyValuePair.Create(SaltType.PROD, "JwYDpKvLj6MrMqqYU6jTKF17KNO2PXoS"),

        // Oversea
        KeyValuePair.Create(SaltType.OSK2, SaltConstants.OSK2),
        KeyValuePair.Create(SaltType.OSLK2, SaltConstants.OSLK2),
        KeyValuePair.Create(SaltType.OSX4, "h4c1d6ywfq5bsbnbhm1bzq7bxzzv6srt"),
        KeyValuePair.Create(SaltType.OSX6, "okr4obncj8bw5a65hbnn5oo6ixjc3l9w"),
    ]);

    [SuppressMessage("", "CA1308")]
    private static string GenerateDeviceId40()
    {
        Guid uuid = Core.Uuid.NewV5(DeviceId36, new("9450ea74-be9c-35c0-9568-f97407856768"));

        Span<byte> uuidSpan = stackalloc byte[16];
        Span<byte> hash = stackalloc byte[20];

        Verify.Operation(uuid.TryWriteBytes(uuidSpan), "Failed to write UUID bytes");
        Verify.Operation(SHA1.TryHashData(uuidSpan, hash, out _), "Failed to write SHA1 hash");
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}