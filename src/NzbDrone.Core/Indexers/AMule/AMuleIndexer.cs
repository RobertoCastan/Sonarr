using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation.Results;
using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Download.Clients;
using NzbDrone.Core.Download.Clients.AMule;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Indexers.Ed2k;
using NzbDrone.Core.Localization;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.AMule
{
    public class AMuleIndexer : IndexerBase<AMuleIndexerSettings>
    {
        private readonly IAMuleProxy _proxy;

        public override string Name => "eD2k/Kad";
        public override DownloadProtocol Protocol => DownloadProtocol.Ed2k;
        public override bool SupportsRss => false;
        public override bool SupportsSearch => true;

        public AMuleIndexer(IAMuleProxy proxy, IIndexerStatusService indexerStatusService, IConfigService configService, IParsingService parsingService, Logger logger, ILocalizationService localizationService)
            : base(indexerStatusService, configService, parsingService, logger, localizationService)
        {
            _proxy = proxy;
        }

        public override Task<IList<ReleaseInfo>> FetchRecent()
        {
            return Task.FromResult<IList<ReleaseInfo>>(Array.Empty<ReleaseInfo>());
        }

        public override Task<IList<ReleaseInfo>> Fetch(SingleEpisodeSearchCriteria searchCriteria)
        {
            return Fetch(ToQuery(searchCriteria.Series.Title, $"S{searchCriteria.SeasonNumber:00}E{searchCriteria.EpisodeNumber:00}"));
        }

        public override Task<IList<ReleaseInfo>> Fetch(SeasonSearchCriteria searchCriteria)
        {
            return Fetch(ToQuery(searchCriteria.Series.Title, $"S{searchCriteria.SeasonNumber:00}"));
        }

        public override Task<IList<ReleaseInfo>> Fetch(DailyEpisodeSearchCriteria searchCriteria)
        {
            return Fetch(ToQuery(searchCriteria.Series.Title, $"{searchCriteria.AirDate:yyyy-MM-dd}"));
        }

        public override Task<IList<ReleaseInfo>> Fetch(DailySeasonSearchCriteria searchCriteria)
        {
            return Fetch(searchCriteria.Series.Title);
        }

        public override Task<IList<ReleaseInfo>> Fetch(AnimeEpisodeSearchCriteria searchCriteria)
        {
            return Fetch(ToQuery(searchCriteria.Series.Title, searchCriteria.AbsoluteEpisodeNumber.ToString()));
        }

        public override Task<IList<ReleaseInfo>> Fetch(AnimeSeasonSearchCriteria searchCriteria)
        {
            return Fetch(searchCriteria.Series.Title);
        }

        public override Task<IList<ReleaseInfo>> Fetch(SpecialEpisodeSearchCriteria searchCriteria)
        {
            return Fetch(searchCriteria.Series.Title);
        }

        public override HttpRequest GetDownloadRequest(string link)
        {
            throw new NotSupportedException("aMule indexers provide ed2k links directly.");
        }

        protected override Task Test(List<ValidationFailure> failures)
        {
            try
            {
                _proxy.GetVersion(ToAMuleSettings());
            }
            catch (DownloadClientAuthenticationException ex)
            {
                _logger.Warn(ex, "Unable to authenticate with aMule");
                failures.Add(new NzbDroneValidationFailure(nameof(Settings.Password), "Unable to authenticate with aMule")
                {
                    DetailedDescription = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Unable to connect to aMule");
                failures.Add(new ValidationFailure(nameof(Settings.Host), _localizationService.GetLocalizedString("IndexerValidationUnableToConnect", new Dictionary<string, object> { { "exceptionMessage", ex.Message } })));
            }

            return Task.CompletedTask;
        }

        private Task<IList<ReleaseInfo>> Fetch(string query)
        {
            var settings = ToAMuleSettings();
            var releases = GetSearchTypes()
                .SelectMany(v => Search(settings, v, query))
                .Select(ToReleaseInfo)
                .Where(v => v.DownloadUrl != null)
                .GroupBy(GetDedupKey)
                .Select(v => v.First())
                .ToList();

            return Task.FromResult(CleanupReleases(releases));
        }

        private List<AMuleSearchResult> Search(AMuleSettings settings, AMuleSearchType searchType, string query)
        {
            var results = _proxy.Search(settings, searchType, query, Settings.SearchDelay);
            results.ForEach(v => v.SearchType = searchType);
            _logger.Debug("aMule {0} search for '{1}' returned {2} results.", searchType, query, results.Count);

            return results;
        }

        private static string GetDedupKey(ReleaseInfo release)
        {
            if (release is Ed2kReleaseInfo ed2kRelease && !string.IsNullOrWhiteSpace(ed2kRelease.Ed2kHash))
            {
                return ed2kRelease.Ed2kHash;
            }

            return release.DownloadUrl;
        }

        private IEnumerable<AMuleSearchType> GetSearchTypes()
        {
            if (Settings.SearchType == AMuleSearchType.Both)
            {
                yield return AMuleSearchType.Ed2kGlobal;
                yield return AMuleSearchType.Kad;
                yield break;
            }

            yield return Settings.SearchType;
        }

        private ReleaseInfo ToReleaseInfo(AMuleSearchResult result)
        {
            var link = result.Ed2kLink;

            if (link == null && result.FileName != null && result.Hash != null)
            {
                link = Ed2kLink.Create(result.FileName, result.Size, result.Hash);
            }

            return new Ed2kReleaseInfo
            {
                Guid = result.Hash ?? link,
                Title = result.FileName,
                Size = result.Size,
                DownloadUrl = link,
                PublishDate = DateTime.UtcNow,
                Ed2kHash = result.Hash
            };
        }

        private AMuleSettings ToAMuleSettings()
        {
            return new AMuleSettings
            {
                Host = Settings.Host,
                Port = Settings.Port,
                Password = Settings.Password
            };
        }

        private static string ToQuery(string title, string suffix)
        {
            return $"{title} {suffix}".Trim();
        }
    }
}
