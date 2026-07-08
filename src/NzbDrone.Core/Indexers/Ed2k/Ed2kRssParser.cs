using System.Xml.Linq;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Indexers.Ed2k
{
    public class Ed2kRssParser : RssParser
    {
        protected override ReleaseInfo CreateNewReleaseInfo()
        {
            return new Ed2kReleaseInfo();
        }

        protected override string GetDownloadUrl(XElement item)
        {
            var link = item.TryGetValue("link");

            if (link.IsNullOrWhiteSpace() || !link.StartsWith("ed2k://"))
            {
                link = item.TryGetValue("guid");
            }

            if (link.IsNullOrWhiteSpace() || !link.StartsWith("ed2k://"))
            {
                var enclosure = GetEnclosure(item, false);
                link = enclosure?.Url;
            }

            return link != null && Ed2kLink.TryParse(link, out _) ? link : null;
        }

        protected override long GetSize(XElement item)
        {
            var downloadUrl = GetDownloadUrl(item);

            if (downloadUrl != null && Ed2kLink.TryParse(downloadUrl, out var link))
            {
                return link.Size;
            }

            return base.GetSize(item);
        }

        protected override ReleaseInfo PostProcessItem(XElement item, ReleaseInfo releaseInfo)
        {
            if (releaseInfo is Ed2kReleaseInfo ed2kRelease && Ed2kLink.TryParse(releaseInfo.DownloadUrl, out var link))
            {
                ed2kRelease.Ed2kHash = link.Hash;
                ed2kRelease.Guid = link.Hash;

                if (releaseInfo.Title.IsNullOrWhiteSpace() || releaseInfo.Title == "Unknown")
                {
                    releaseInfo.Title = link.FileName;
                }
            }

            return releaseInfo;
        }
    }
}
