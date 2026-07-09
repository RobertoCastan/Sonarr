using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Disk;
using NzbDrone.Core.Download;
using NzbDrone.Core.Download.Clients.AMule;
using NzbDrone.Core.Indexers.Ed2k;
using NzbDrone.Core.Localization;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.RemotePathMappings;
using NzbDrone.Core.Test.Download.DownloadClientTests;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Test.Download.DownloadClientTests.AMuleTests
{
    public class AMuleFixture : DownloadClientFixtureBase<AMule>
    {
        private const string Ed2kUrl = "ed2k://|file|Droned.S01E01.mkv|12345|0123456789abcdef0123456789abcdef|/";

        [SetUp]
        public void Setup()
        {
            Subject.Definition = new DownloadClientDefinition
            {
                Id = 1,
                Name = "aMule",
                Settings = new AMuleSettings
                {
                    Host = "localhost",
                    Port = 4712,
                    Password = "secret",
                    TvCategory = "sonarr"
                }
            };

            Mocker.GetMock<IAMuleProxy>()
                .Setup(v => v.GetPreferences(It.IsAny<AMuleSettings>()))
                .Returns(new AMulePreferences
                {
                    IncomingDirectory = "/downloads/Incoming",
                    Categories = new List<AMuleCategory>
                    {
                        new AMuleCategory { Id = 1, Title = "sonarr", Path = "/downloads/sonarr" },
                        new AMuleCategory { Id = 2, Title = "radarr", Path = "/downloads/radarr" }
                    }
                });

            Mocker.GetMock<IRemotePathMappingService>()
                .Setup(v => v.RemapRemoteToLocal(It.IsAny<string>(), It.IsAny<OsPath>()))
                .Returns<string, OsPath>((h, p) => p);
        }

        [Test]
        public async System.Threading.Tasks.Task should_add_ed2k_link_and_return_hash()
        {
            var remoteEpisode = CreateRemoteEpisode();
            remoteEpisode.Release.DownloadUrl = Ed2kUrl;

            var downloadId = await Subject.Download(remoteEpisode, CreateIndexer());

            downloadId.Should().Be("0123456789ABCDEF0123456789ABCDEF");
            Mocker.GetMock<IAMuleProxy>().Verify(v => v.AddLink(It.IsAny<AMuleSettings>(), Ed2kUrl), Times.Once());
        }

        [Test]
        public void should_mark_complete_queue_item()
        {
            Mocker.GetMock<IAMuleProxy>()
                .Setup(v => v.GetQueue(It.IsAny<AMuleSettings>()))
                .Returns(new List<AMuleQueueItem>
                {
                    new AMuleQueueItem
                    {
                        FileName = "Droned.S01E01.mkv",
                        Size = 12345,
                        CompletedSize = 12345,
                        Hash = "0123456789ABCDEF0123456789ABCDEF",
                        Status = AMuleEcCodes.StatusComplete,
                        Category = 1
                    },
                    new AMuleQueueItem
                    {
                        FileName = "Movie.mkv",
                        Size = 12345,
                        CompletedSize = 12345,
                        Hash = "FEDCBA9876543210FEDCBA9876543210",
                        Status = AMuleEcCodes.StatusComplete,
                        Category = 2
                    }
                });

            var item = Subject.GetItems().Single();

            item.DownloadId.Should().Be("0123456789ABCDEF0123456789ABCDEF");
            item.Category.Should().Be("sonarr");
            item.Status.Should().Be(DownloadItemStatus.Completed);
            item.OutputPath.FullPath.Should().Be("/downloads/sonarr/Droned.S01E01.mkv");
        }

        [Test]
        public void should_report_category_path_as_output_root()
        {
            var status = Subject.GetStatus();

            status.OutputRootFolders.Single().FullPath.Should().Be("/downloads/sonarr");
        }

        [Test]
        public void should_warn_when_category_is_empty()
        {
            var settings = new AMuleSettings
            {
                Host = "localhost",
                Port = 4712,
                Password = "secret",
                TvCategory = string.Empty
            };

            var result = settings.Validate();

            result.Warnings.Single().PropertyName.Should().Be(nameof(AMuleSettings.TvCategory));
        }

        [Test]
        public void should_fail_test_when_category_cannot_be_created_without_incoming_directory()
        {
            Mocker.GetMock<ILocalizationService>()
                .Setup(v => v.GetLocalizedString("DownloadClientAMuleValidationCategoryIncomingMissingDetail"))
                .Returns("Set an Incoming directory in aMule.");
            Mocker.GetMock<IAMuleProxy>()
                .Setup(v => v.GetVersion(It.IsAny<AMuleSettings>()))
                .Returns("2.3.3");
            Mocker.GetMock<IAMuleProxy>()
                .Setup(v => v.GetPreferences(It.IsAny<AMuleSettings>()))
                .Returns(new AMulePreferences
                {
                    IncomingDirectory = string.Empty,
                    Categories = new List<AMuleCategory>()
                });

            var result = Subject.Test();
            var failure = (NzbDroneValidationFailure)result.Errors.Single();

            result.IsValid.Should().BeFalse();
            failure.PropertyName.Should().Be(nameof(AMuleSettings.TvCategory));
            failure.DetailedDescription.Should().Contain("Incoming");
        }

        protected override RemoteEpisode CreateRemoteEpisode()
        {
            var remoteEpisode = base.CreateRemoteEpisode();
            remoteEpisode.Release = new Ed2kReleaseInfo
            {
                Title = _title,
                DownloadUrl = Ed2kUrl,
                DownloadProtocol = Subject.Protocol
            };

            return remoteEpisode;
        }
    }
}
