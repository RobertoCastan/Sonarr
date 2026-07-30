using System;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Download.Clients.AMule;

namespace NzbDrone.Core.Test.Download.DownloadClientTests.AMuleTests
{
    [TestFixture]
    public class AMuleEcCodecFixture
    {
        [Test]
        public void should_roundtrip_packet()
        {
            var packet = new AMuleEcPacket(AMuleEcCodes.OpAuthReq)
            {
                Tags =
                {
                    AMuleEcTag.String(AMuleEcCodes.TagClientName, "Sonarr"),
                    AMuleEcTag.UInt(AMuleEcCodes.TagProtocolVersion, AMuleEcCodes.ProtocolVersion)
                }
            };

            var encoded = AMuleEcCodec.Encode(packet);
            var length = AMuleEcCodec.GetFrameLength(encoded[..8]);
            var payload = encoded[8.. (8 + length)];
            var decoded = AMuleEcCodec.Decode(payload);

            decoded.OpCode.Should().Be(AMuleEcCodes.OpAuthReq);
            decoded.Find(AMuleEcCodes.TagClientName).StringValue.Should().Be("Sonarr");
            decoded.Find(AMuleEcCodes.TagProtocolVersion).IntegerValue.Should().Be(AMuleEcCodes.ProtocolVersion);
        }

        [Test]
        public void should_roundtrip_tag_with_children()
        {
            var searchTag = AMuleEcTag.UInt(AMuleEcCodes.TagSearchType, AMuleEcCodes.SearchGlobal);
            searchTag.Children.Add(AMuleEcTag.String(AMuleEcCodes.TagSearchName, "ubuntu"));

            var encoded = AMuleEcCodec.Encode(new AMuleEcPacket(AMuleEcCodes.OpSearchStart)
            {
                Tags = { searchTag }
            });

            var length = AMuleEcCodec.GetFrameLength(encoded[..8]);
            var decoded = AMuleEcCodec.Decode(encoded[8.. (8 + length)]);
            var decodedSearchTag = decoded.Find(AMuleEcCodes.TagSearchType);

            decodedSearchTag.IntegerValue.Should().Be(AMuleEcCodes.SearchGlobal);
            decodedSearchTag.Find(AMuleEcCodes.TagSearchName).StringValue.Should().Be("ubuntu");
        }

        [Test]
        public void should_create_salted_password_hash()
        {
            var hash = AMuleEcCodec.CreatePasswordHash("secret", 0x1234);

            BitConverter.ToString(hash).Replace("-", string.Empty).Should().HaveLength(32);
        }
    }
}
