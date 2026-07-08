using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Disk;
using NzbDrone.Core.Download;
using NzbDrone.Core.Download.Clients.AMule;
using NzbDrone.Core.Indexers.Ed2k;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.RemotePathMappings;
using NzbDrone.Core.Test.Download.DownloadClientTests;

namespace NzbDrone.Core.Test.Download.DownloadClientTests.AMuleTests
{
    public class AMuleKadFixture : DownloadClientFixtureBase<AMuleKad>
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
                    Password = "secret"
                }
            };

            Mocker.GetMock<IAMuleProxy>()
                .Setup(v => v.GetPreferences(It.IsAny<AMuleSettings>()))
                .Returns(new AMulePreferences { IncomingDirectory = "/downloads/Incoming" });

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
                        Status = AMuleEcCodes.StatusComplete
                    }
                });

            var item = Subject.GetItems().Single();

            item.DownloadId.Should().Be("0123456789ABCDEF0123456789ABCDEF");
            item.Status.Should().Be(DownloadItemStatus.Completed);
            item.OutputPath.FullPath.Should().Be("/downloads/Incoming/Droned.S01E01.mkv");
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
