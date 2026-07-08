using System.Linq;
using System.Text;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Common.Http;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Indexers.Ed2k;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.IndexerTests.Ed2kTests
{
    public class Ed2kRssParserFixture : CoreTest<Ed2kRssParser>
    {
        [Test]
        public void should_parse_ed2k_link_from_rss()
        {
            var xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<rss><channel><item>
<title>Droned.S01E01.1080p.WEB-DL</title>
<link>ed2k://|file|Droned.S01E01.1080p.WEB-DL.mkv|12345|0123456789abcdef0123456789abcdef|/</link>
<pubDate>Wed, 08 Jul 2026 10:00:00 GMT</pubDate>
</item></channel></rss>";

            var releases = Subject.ParseResponse(CreateResponse(xml));

            releases.Should().HaveCount(1);
            releases.First().DownloadUrl.Should().StartWith("ed2k://|file|");
            releases.First().Size.Should().Be(12345);
            releases.First().Guid.Should().Be("0123456789ABCDEF0123456789ABCDEF");
        }

        private static IndexerResponse CreateResponse(string content)
        {
            var httpRequest = new HttpRequest("http://indexer.local/rss");
            var httpResponse = new HttpResponse(httpRequest, new HttpHeader(), Encoding.UTF8.GetBytes(content));

            return new IndexerResponse(new IndexerRequest(httpRequest), httpResponse);
        }
    }
}
