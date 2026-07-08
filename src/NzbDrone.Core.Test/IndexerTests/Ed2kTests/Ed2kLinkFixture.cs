using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Indexers.Ed2k;

namespace NzbDrone.Core.Test.IndexerTests.Ed2kTests
{
    [TestFixture]
    public class Ed2kLinkFixture
    {
        [Test]
        public void should_parse_file_link()
        {
            var link = Ed2kLink.Parse("ed2k://|file|Droned.S01E01.mkv|12345|0123456789abcdef0123456789abcdef|/");

            link.FileName.Should().Be("Droned.S01E01.mkv");
            link.Size.Should().Be(12345);
            link.Hash.Should().Be("0123456789ABCDEF0123456789ABCDEF");
        }

        [Test]
        public void should_reject_non_file_link()
        {
            Ed2kLink.TryParse("ed2k://|server|127.0.0.1|4661|/", out _).Should().BeFalse();
        }
    }
}
