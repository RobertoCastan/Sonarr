using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Download.Clients;
using NzbDrone.Core.Download.Clients.AMule;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Indexers.AMule;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Core.Tv;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.IndexerTests.AMuleTests
{
    public class AMuleIndexerFixture : CoreTest<AMuleIndexer>
    {
        [SetUp]
        public void Setup()
        {
            Subject.Definition = new IndexerDefinition
            {
                Settings = new AMuleIndexerSettings
                {
                    Host = "localhost",
                    Port = 4712,
                    Password = "secret",
                    SearchType = AMuleSearchType.Kad
                }
            };

            Mocker.GetMock<IAMuleProxy>()
                .Setup(v => v.Search(It.IsAny<AMuleSettings>(), It.IsAny<AMuleSearchType>(), It.IsAny<string>(), It.IsAny<int>()))
                .Returns(new List<AMuleSearchResult>());
        }

        [Test]
        public async Task should_use_configured_search_type()
        {
            await Subject.Fetch(new SeasonSearchCriteria
            {
                Series = new Series { Title = "Droned" },
                SeasonNumber = 1
            });

            Mocker.GetMock<IAMuleProxy>()
                .Verify(v => v.Search(It.IsAny<AMuleSettings>(), AMuleSearchType.Kad, "Droned S01", 5), Times.Once());
        }

        [Test]
        public async Task should_use_configured_search_delay()
        {
            ((AMuleIndexerSettings)Subject.Definition.Settings).SearchDelay = 9;

            await Subject.Fetch(new SeasonSearchCriteria
            {
                Series = new Series { Title = "Droned" },
                SeasonNumber = 1
            });

            Mocker.GetMock<IAMuleProxy>()
                .Verify(v => v.Search(It.IsAny<AMuleSettings>(), AMuleSearchType.Kad, "Droned S01", 9), Times.Once());
        }

        [Test]
        public async Task should_search_both_networks()
        {
            ((AMuleIndexerSettings)Subject.Definition.Settings).SearchType = AMuleSearchType.Both;

            await Subject.Fetch(new SeasonSearchCriteria
            {
                Series = new Series { Title = "Droned" },
                SeasonNumber = 1
            });

            Mocker.GetMock<IAMuleProxy>()
                .Verify(v => v.Search(It.IsAny<AMuleSettings>(), AMuleSearchType.Ed2kGlobal, "Droned S01", 5), Times.Once());
            Mocker.GetMock<IAMuleProxy>()
                .Verify(v => v.Search(It.IsAny<AMuleSettings>(), AMuleSearchType.Kad, "Droned S01", 5), Times.Once());
        }

        [Test]
        public async Task should_dedupe_both_network_results_by_hash()
        {
            ((AMuleIndexerSettings)Subject.Definition.Settings).SearchType = AMuleSearchType.Both;

            Mocker.GetMock<IAMuleProxy>()
                .Setup(v => v.Search(It.IsAny<AMuleSettings>(), AMuleSearchType.Ed2kGlobal, It.IsAny<string>(), It.IsAny<int>()))
                .Returns(new List<AMuleSearchResult>
                {
                    new AMuleSearchResult
                    {
                        FileName = "Droned.S01.mkv",
                        Size = 12345,
                        Hash = "0123456789ABCDEF0123456789ABCDEF",
                        Ed2kLink = "ed2k://|file|Droned.S01.mkv|12345|0123456789ABCDEF0123456789ABCDEF|/"
                    }
                });
            Mocker.GetMock<IAMuleProxy>()
                .Setup(v => v.Search(It.IsAny<AMuleSettings>(), AMuleSearchType.Kad, It.IsAny<string>(), It.IsAny<int>()))
                .Returns(new List<AMuleSearchResult>
                {
                    new AMuleSearchResult
                    {
                        FileName = "Droned.S01.Copy.mkv",
                        Size = 12345,
                        Hash = "0123456789ABCDEF0123456789ABCDEF",
                        Ed2kLink = "ed2k://|file|Droned.S01.Copy.mkv|12345|0123456789ABCDEF0123456789ABCDEF|/"
                    }
                });

            var releases = await Subject.Fetch(new SeasonSearchCriteria
            {
                Series = new Series { Title = "Droned" },
                SeasonNumber = 1
            });

            releases.Should().HaveCount(1);
            releases.Single().Guid.Should().Be("0123456789ABCDEF0123456789ABCDEF");
        }

        [Test]
        public void should_report_authentication_failure_on_password_field()
        {
            Mocker.GetMock<IAMuleProxy>()
                .Setup(v => v.GetVersion(It.IsAny<AMuleSettings>()))
                .Throws(new DownloadClientAuthenticationException("aMule EC authentication failed."));

            var result = Subject.Test();

            result.IsValid.Should().BeFalse();
            result.Errors.Single().PropertyName.Should().Be(nameof(AMuleIndexerSettings.Password));
            ExceptionVerification.ExpectedWarns(1);
        }
    }
}
