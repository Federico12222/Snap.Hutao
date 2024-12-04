// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Service.Game.Package.Advanced;

internal abstract class GamePackageOperationReport
{
    public GamePackageOperationReportKind Kind { get; private set; }

    internal abstract class Update : GamePackageOperationReport
    {
        public Update(long bytesRead, int chunks, string fileName)
        {
            BytesRead = bytesRead;
            Chunks = chunks;
            FileName = fileName;
        }

        public long BytesRead { get; }

        public int Chunks { get; }

        public string FileName { get; }
    }

    internal sealed class Download : Update
    {
        public Download(long bytesRead, int chunks, string fileName = default!)
            : base(bytesRead, chunks, fileName)
        {
            Kind = GamePackageOperationReportKind.Download;
        }
    }

    internal sealed class Install : Update
    {
        public Install(long bytesRead, int chunks, string fileName = default!)
            : base(bytesRead, chunks, fileName)
        {
            Kind = GamePackageOperationReportKind.Install;
        }
    }

    internal sealed class Reset : GamePackageOperationReport
    {
        public Reset(string title)
        {
            Kind = GamePackageOperationReportKind.Reset;
            Title = title;
        }

        public Reset(string title, int totalChunks, long contentLength)
        {
            Kind = GamePackageOperationReportKind.Reset;
            Title = title;
            DownloadTotalChunks = InstallTotalChunks = totalChunks;
            ContentLength = contentLength;
        }

        public Reset(string title, int downloadTotalChunks, int installTotalChunks, long contentLength)
        {
            Kind = GamePackageOperationReportKind.Reset;
            Title = title;
            DownloadTotalChunks = downloadTotalChunks;
            InstallTotalChunks = installTotalChunks;
            ContentLength = contentLength;
        }

        public string Title { get; }

        public int DownloadTotalChunks { get; }

        public int InstallTotalChunks { get; }

        public long ContentLength { get; }
    }

    internal sealed class Finish : GamePackageOperationReport
    {
        public Finish(GamePackageOperationKind kind, bool repaired = false)
        {
            Kind = GamePackageOperationReportKind.Finish;
            OperationKind = kind;
            Repaired = repaired;
        }

        public GamePackageOperationKind OperationKind { get; }

        public bool Repaired { get; }
    }
}