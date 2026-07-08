using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Localization;
using NzbDrone.Core.RemotePathMappings;

namespace NzbDrone.Core.Download.Clients.AMule
{
    public class AMuleKad : AMule
    {
        public override DownloadProtocol Protocol => DownloadProtocol.Kad;

        public AMuleKad(IAMuleProxy proxy, IConfigService configService, IDiskProvider diskProvider, IRemotePathMappingService remotePathMappingService, Logger logger, ILocalizationService localizationService)
            : base(proxy, configService, diskProvider, remotePathMappingService, logger, localizationService)
        {
        }
    }
}
