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
    public class AMule : DownloadClientBase<AMuleSettings>
    {
        private readonly IAMuleProxy _proxy;

        public override string Name => "aMule";
        public override DownloadProtocol Protocol => DownloadProtocol.Ed2k;

        public AMule(IAMuleProxy proxy,
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
            var preferences = _proxy.GetPreferences(Settings);
            var category = Settings.TvCategory.IsNullOrWhiteSpace()
                ? null
                : preferences.Categories.FirstOrDefault(v => string.Equals(v.Title, Settings.TvCategory, StringComparison.InvariantCultureIgnoreCase));
            var outputDirectory = category?.Path.IsNotNullOrWhiteSpace() == true ? category.Path : preferences.IncomingDirectory;

            if (Settings.TvCategory.IsNotNullOrWhiteSpace() && category == null)
            {
                yield break;
            }

            foreach (var item in _proxy.GetQueue(Settings))
            {
                if (category != null && item.Category != category.Id)
                {
                    continue;
                }

                var hash = item.Hash;
                if (hash == null && Ed2kLink.TryParse(item.Ed2kLink, out var link))
                {
                    hash = link.Hash;
                }

                var remainingSize = Math.Max(0, item.Size - item.CompletedSize);
                var status = item.Status == AMuleEcCodes.StatusComplete ? DownloadItemStatus.Completed : DownloadItemStatus.Downloading;
                var outputPath = outputDirectory.IsNullOrWhiteSpace() || item.FileName == null
                    ? new OsPath(null)
                    : _remotePathMappingService.RemapRemoteToLocal(Settings.Host, new OsPath(Path.Combine(outputDirectory, item.FileName)));

                var queueItem = new DownloadClientItem
                {
                    DownloadClientInfo = DownloadClientItemClientInfo.FromDownloadClient(this, false),
                    DownloadId = hash,
                    Category = Settings.TvCategory,
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
            var preferences = _proxy.GetPreferences(Settings);
            var category = Settings.TvCategory.IsNullOrWhiteSpace()
                ? null
                : preferences.Categories.FirstOrDefault(v => string.Equals(v.Title, Settings.TvCategory, StringComparison.InvariantCultureIgnoreCase));
            var outputDirectory = category?.Path.IsNotNullOrWhiteSpace() == true ? category.Path : preferences.IncomingDirectory;

            return new DownloadClientInfo
            {
                IsLocalhost = Settings.Host == "localhost" || Settings.Host == "127.0.0.1",
                OutputRootFolders = outputDirectory.IsNullOrWhiteSpace() ? new List<OsPath>() : new List<OsPath> { _remotePathMappingService.RemapRemoteToLocal(Settings.Host, new OsPath(outputDirectory)) }
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
                return TestCategory(_proxy.GetPreferences(Settings));
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
        }

        private ValidationFailure TestCategory(AMulePreferences preferences)
        {
            if (Settings.TvCategory.IsNullOrWhiteSpace() ||
                preferences.Categories.Any(v => string.Equals(v.Title, Settings.TvCategory, StringComparison.InvariantCultureIgnoreCase)))
            {
                return null;
            }

            if (preferences.IncomingDirectory.IsNullOrWhiteSpace())
            {
                return new NzbDroneValidationFailure(nameof(Settings.TvCategory), _localizationService.GetLocalizedString("DownloadClientAMuleValidationCategoryIncomingMissing"))
                {
                    DetailedDescription = _localizationService.GetLocalizedString("DownloadClientAMuleValidationCategoryIncomingMissingDetail")
                };
            }

            _logger.Debug("aMule category '{0}' does not exist and will be created when adding a download.", Settings.TvCategory);
            return null;
        }
    }
}
