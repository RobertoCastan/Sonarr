using NLog;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Download.Clients.AMule;
using NzbDrone.Core.Localization;
using NzbDrone.Core.Parser;

namespace NzbDrone.Core.Indexers.AMule
{
    public class AMuleKad : AMuleIndexer
    {
        public override DownloadProtocol Protocol => DownloadProtocol.Kad;
        protected override AMuleSearchType SearchType => AMuleSearchType.Kad;

        public AMuleKad(IAMuleProxy proxy, IIndexerStatusService indexerStatusService, IConfigService configService, IParsingService parsingService, Logger logger, ILocalizationService localizationService)
            : base(proxy, indexerStatusService, configService, parsingService, logger, localizationService)
        {
        }
    }
}
