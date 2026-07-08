using NLog;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Download.Clients.AMule;
using NzbDrone.Core.Localization;
using NzbDrone.Core.Parser;

namespace NzbDrone.Core.Indexers.AMule
{
    public class AMuleEd2kGlobal : AMuleIndexer
    {
        public override DownloadProtocol Protocol => DownloadProtocol.Ed2kGlobal;
        protected override AMuleSearchType SearchType => AMuleSearchType.Ed2kGlobal;

        public AMuleEd2kGlobal(IAMuleProxy proxy, IIndexerStatusService indexerStatusService, IConfigService configService, IParsingService parsingService, Logger logger, ILocalizationService localizationService)
            : base(proxy, indexerStatusService, configService, parsingService, logger, localizationService)
        {
        }
    }
}
