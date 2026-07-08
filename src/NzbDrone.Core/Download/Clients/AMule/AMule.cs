using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentValidation.Results;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Indexers.Ed2k;
using NzbDrone.Core.Localization;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.RemotePathMappings;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Download.Clients.AMule
{
    public abstract class AMule : DownloadClientBase<AMuleSettings>
    {
        private readonly IAMuleProxy _proxy;

        public override string Name => Protocol == DownloadProtocol.Kad ? "aMule Kad" : "aMule eD2k Global";

        protected AMule(IAMuleProxy proxy,
                        IConfigService configService,
                        IDiskProvider diskProvider,
                        IRemotePathMappingService remotePathMappingService,
                        Logger logger,
                        ILocalizationService localizationService)
            : base(configService, diskProvider, remotePathMappingService, logger, localizationService)
        {
            _proxy = proxy;
        }

        public override System.Threading.Tasks.Task<string> Download(RemoteEpisode remoteEpisode, IIndexer indexer)
        {
            if (!Ed2kLink.TryParse(remoteEpisode.Release.DownloadUrl, out var link))
            {
                throw new DownloadClientException("aMule requires an ed2k:// file link.");
            }

            _proxy.AddLink(Settings, link.Url);

            return System.Threading.Tasks.Task.FromResult(link.Hash);
        }

        public override IEnumerable<DownloadClientItem> GetItems()
        {
            var incoming = _proxy.GetPreferences(Settings).IncomingDirectory;

            foreach (var item in _proxy.GetQueue(Settings))
            {
                var hash = item.Hash;
                if (hash == null && Ed2kLink.TryParse(item.Ed2kLink, out var link))
                {
                    hash = link.Hash;
                }

                var remainingSize = Math.Max(0, item.Size - item.CompletedSize);
                var status = item.Status == AMuleEcCodes.StatusComplete ? DownloadItemStatus.Completed : DownloadItemStatus.Downloading;
                var outputPath = incoming == null || item.FileName == null
                    ? new OsPath(null)
                    : _remotePathMappingService.RemapRemoteToLocal(Settings.Host, new OsPath(Path.Combine(incoming, item.FileName)));

                var queueItem = new DownloadClientItem
                {
                    DownloadClientInfo = DownloadClientItemClientInfo.FromDownloadClient(this, false),
                    DownloadId = hash,
                    Title = item.FileName,
                    TotalSize = item.Size,
                    RemainingSize = remainingSize,
                    RemainingTime = status == DownloadItemStatus.Completed ? TimeSpan.Zero : null,
                    Status = status,
                    OutputPath = outputPath,
                    CanMoveFiles = false,
                    CanBeRemoved = false
                };

                yield return queueItem;
            }
        }

        public override DownloadClientItem GetImportItem(DownloadClientItem item, DownloadClientItem previousImportAttempt)
        {
            if (item.Status == DownloadItemStatus.Completed && !item.OutputPath.IsEmpty && !_diskProvider.FileExists(item.OutputPath.FullPath))
            {
                item = item.Clone();
                item.Status = DownloadItemStatus.Warning;
                item.Message = "Completed aMule download could not be found on disk. Check Docker volume mounts or remote path mapping.";
            }

            return item;
        }

        public override void RemoveItem(DownloadClientItem item, bool deleteData)
        {
            if (deleteData)
            {
                DeleteItemData(item);
            }
        }

        public override DownloadClientInfo GetStatus()
        {
            var incoming = _proxy.GetPreferences(Settings).IncomingDirectory;

            return new DownloadClientInfo
            {
                IsLocalhost = Settings.Host == "localhost" || Settings.Host == "127.0.0.1",
                OutputRootFolders = incoming == null ? new List<OsPath>() : new List<OsPath> { _remotePathMappingService.RemapRemoteToLocal(Settings.Host, new OsPath(incoming)) }
            };
        }

        protected override void Test(List<ValidationFailure> failures)
        {
            failures.AddIfNotNull(TestConnection());
        }

        private ValidationFailure TestConnection()
        {
            try
            {
                _proxy.GetVersion(Settings);
            }
            catch (DownloadClientAuthenticationException ex)
            {
                return new NzbDroneValidationFailure(nameof(Settings.Password), _localizationService.GetLocalizedString("DownloadClientValidationAuthenticationFailure"))
                {
                    DetailedDescription = ex.Message
                };
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to test aMule");

                return new NzbDroneValidationFailure(nameof(Settings.Host), _localizationService.GetLocalizedString("DownloadClientValidationUnableToConnect", new Dictionary<string, object> { { "clientName", Name } }))
                {
                    DetailedDescription = ex.Message
                };
            }

            return null;
        }
    }
}
