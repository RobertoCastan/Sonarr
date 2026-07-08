using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Localization;
using NzbDrone.Core.RemotePathMappings;

namespace NzbDrone.Core.Download.Clients.AMule
{
    public class AMuleEd2kGlobal : AMule
    {
        public override DownloadProtocol Protocol => DownloadProtocol.Ed2kGlobal;

        public AMuleEd2kGlobal(IAMuleProxy proxy, IConfigService configService, IDiskProvider diskProvider, IRemotePathMappingService remotePathMappingService, Logger logger, ILocalizationService localizationService)
            : base(proxy, configService, diskProvider, remotePathMappingService, logger, localizationService)
        {
        }
    }
}
