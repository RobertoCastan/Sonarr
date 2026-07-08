using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using NLog;
using NzbDrone.Core.Download.Clients;
using NzbDrone.Core.Indexers.Ed2k;

namespace NzbDrone.Core.Download.Clients.AMule
{
    public class AMuleProxy : IAMuleProxy
    {
        private readonly Logger _logger;

        public AMuleProxy(Logger logger)
        {
            _logger = logger;
        }

        public string GetVersion(AMuleSettings settings)
        {
            using var connection = Connect(settings);
            return connection.Version;
        }

        public void AddLink(AMuleSettings settings, string ed2kLink)
        {
            using var connection = Connect(settings);

            var response = connection.Send(new AMuleEcPacket(AMuleEcCodes.OpAddLink)
            {
                Tags =
                {
                    AMuleEcTag.String(AMuleEcCodes.TagString, ed2kLink)
                }
            });

            ThrowIfFailed(response);
        }

        public List<AMuleQueueItem> GetQueue(AMuleSettings settings)
        {
            using var connection = Connect(settings);

            var response = connection.Send(new AMuleEcPacket(AMuleEcCodes.OpGetDloadQueue)
            {
                Tags =
                {
                    AMuleEcTag.UInt(AMuleEcCodes.TagDetailLevel, AMuleEcCodes.DetailFull)
                }
            });

            ThrowIfFailed(response);

            return response.Tags
                .Where(v => v.Name == AMuleEcCodes.TagPartFile)
                .Select(ToQueueItem)
                .Where(v => v.Hash != null || v.Ed2kLink != null)
                .ToList();
        }

        public AMulePreferences GetPreferences(AMuleSettings settings)
        {
            using var connection = Connect(settings);

            var response = connection.Send(new AMuleEcPacket(AMuleEcCodes.OpGetPreferences)
            {
                Tags =
                {
                    AMuleEcTag.UInt(AMuleEcCodes.TagDetailLevel, AMuleEcCodes.DetailFull),
                    AMuleEcTag.Empty(AMuleEcCodes.TagSelectPrefs)
                }
            });

            ThrowIfFailed(response);

            return new AMulePreferences
            {
                IncomingDirectory = response.Find(AMuleEcCodes.TagDirectoriesIncoming)?.StringValue
            };
        }

        public List<AMuleSearchResult> Search(AMuleSettings settings, AMuleSearchType searchType, string query)
        {
            using var connection = Connect(settings);

            var searchTag = AMuleEcTag.UInt(AMuleEcCodes.TagSearchType, (ulong)searchType);
            searchTag.Children.Add(AMuleEcTag.String(AMuleEcCodes.TagSearchName, query));
            searchTag.Children.Add(AMuleEcTag.String(AMuleEcCodes.TagSearchFileType, string.Empty));

            var startResponse = connection.Send(new AMuleEcPacket(AMuleEcCodes.OpSearchStart)
            {
                Tags = { searchTag }
            });

            ThrowIfFailed(startResponse);

            // ponytail: aMule searches are async; replace with progress polling if this delay is too coarse.
            Thread.Sleep(5000);

            var response = connection.Send(new AMuleEcPacket(AMuleEcCodes.OpSearchResults)
            {
                Tags =
                {
                    AMuleEcTag.UInt(AMuleEcCodes.TagDetailLevel, AMuleEcCodes.DetailFull)
                }
            });

            ThrowIfFailed(response);

            return response.Tags
                .Where(v => v.Name == AMuleEcCodes.TagSearchFile)
                .Select(ToSearchResult)
                .Where(v => v.Hash != null || v.Ed2kLink != null)
                .ToList();
        }

        private AMuleConnection Connect(AMuleSettings settings)
        {
            return new AMuleConnection(settings, _logger);
        }

        private static AMuleQueueItem ToQueueItem(AMuleEcTag tag)
        {
            var item = new AMuleQueueItem
            {
                FileName = tag.Find(AMuleEcCodes.TagPartFileName)?.StringValue,
                Size = (long)(tag.Find(AMuleEcCodes.TagPartFileSizeFull)?.IntegerValue ?? 0),
                CompletedSize = (long)(tag.Find(AMuleEcCodes.TagPartFileSizeDone)?.IntegerValue ?? 0),
                Hash = tag.Find(AMuleEcCodes.TagPartFileHash)?.HashValue,
                Ed2kLink = tag.Find(AMuleEcCodes.TagPartFileEd2kLink)?.StringValue,
                Sources = (int)(tag.Find(AMuleEcCodes.TagPartFileSourceCount)?.IntegerValue ?? 0),
                Status = (int)(tag.Find(AMuleEcCodes.TagPartFileStatus)?.IntegerValue ?? 0)
            };

            if (item.Hash == null && Ed2kLink.TryParse(item.Ed2kLink, out var link))
            {
                item.Hash = link.Hash;
            }

            return item;
        }

        private static AMuleSearchResult ToSearchResult(AMuleEcTag tag)
        {
            var result = new AMuleSearchResult
            {
                FileName = tag.Find(AMuleEcCodes.TagPartFileName)?.StringValue,
                Size = (long)(tag.Find(AMuleEcCodes.TagPartFileSizeFull)?.IntegerValue ?? 0),
                Hash = tag.Find(AMuleEcCodes.TagPartFileHash)?.HashValue,
                Ed2kLink = tag.Find(AMuleEcCodes.TagPartFileEd2kLink)?.StringValue,
                Sources = (int)(tag.Find(AMuleEcCodes.TagSearchAvailability)?.IntegerValue ?? 0)
            };

            if (result.Ed2kLink == null && result.FileName != null && result.Hash != null)
            {
                result.Ed2kLink = Ed2kLink.Create(result.FileName, result.Size, result.Hash);
            }

            if (result.Hash == null && Ed2kLink.TryParse(result.Ed2kLink, out var link))
            {
                result.Hash = link.Hash;
            }

            return result;
        }

        private static void ThrowIfFailed(AMuleEcPacket packet)
        {
            if (packet.OpCode == AMuleEcCodes.OpFailed || packet.OpCode == AMuleEcCodes.OpAuthFail)
            {
                throw new DownloadClientException(packet.Find(AMuleEcCodes.TagString)?.StringValue ?? "aMule EC request failed.");
            }
        }

        private sealed class AMuleConnection : IDisposable
        {
            private readonly TcpClient _client;
            private readonly NetworkStream _stream;
            private readonly Logger _logger;

            public string Version { get; private set; }

            public AMuleConnection(AMuleSettings settings, Logger logger)
            {
                _logger = logger;
                _client = new TcpClient();
                _client.ReceiveTimeout = 15000;
                _client.SendTimeout = 15000;
                _client.Connect(settings.Host, settings.Port);
                _stream = _client.GetStream();

                Authenticate(settings);
            }

            public AMuleEcPacket Send(AMuleEcPacket packet)
            {
                var bytes = AMuleEcCodec.Encode(packet);
                _stream.Write(bytes, 0, bytes.Length);

                return Receive();
            }

            public void Dispose()
            {
                _stream?.Dispose();
                _client?.Dispose();
            }

            private void Authenticate(AMuleSettings settings)
            {
                var authReq = new AMuleEcPacket(AMuleEcCodes.OpAuthReq)
                {
                    Tags =
                    {
                        AMuleEcTag.String(AMuleEcCodes.TagClientName, "Sonarr"),
                        AMuleEcTag.String(AMuleEcCodes.TagClientVersion, "Sonarr"),
                        AMuleEcTag.UInt(AMuleEcCodes.TagProtocolVersion, AMuleEcCodes.ProtocolVersion)
                    }
                };

                var saltResponse = Send(authReq);

                if (saltResponse.OpCode != AMuleEcCodes.OpAuthSalt)
                {
                    throw new DownloadClientAuthenticationException("aMule EC authentication salt was not returned.");
                }

                var salt = saltResponse.Find(AMuleEcCodes.TagPasswdSalt)?.IntegerValue;

                if (salt == null)
                {
                    throw new DownloadClientAuthenticationException("aMule EC authentication salt is missing.");
                }

                var passwordResponse = Send(new AMuleEcPacket(AMuleEcCodes.OpAuthPasswd)
                {
                    Tags =
                    {
                        AMuleEcTag.Hash(AMuleEcCodes.TagPasswdHash, AMuleEcCodec.CreatePasswordHash(settings.Password, salt.Value))
                    }
                });

                if (passwordResponse.OpCode != AMuleEcCodes.OpAuthOk)
                {
                    _logger.Debug("aMule EC authentication failed with opcode {0}", passwordResponse.OpCode);
                    throw new DownloadClientAuthenticationException("aMule EC authentication failed.");
                }

                Version = passwordResponse.Find(AMuleEcCodes.TagServerVersion)?.StringValue;
            }

            private AMuleEcPacket Receive()
            {
                var header = ReadExact(8);
                var length = AMuleEcCodec.GetFrameLength(header);
                return AMuleEcCodec.Decode(ReadExact(length));
            }

            private byte[] ReadExact(int length)
            {
                var buffer = new byte[length];
                var offset = 0;

                while (offset < length)
                {
                    var read = _stream.Read(buffer, offset, length - offset);

                    if (read == 0)
                    {
                        throw new DownloadClientException("aMule EC connection closed unexpectedly.");
                    }

                    offset += read;
                }

                return buffer;
            }
        }
    }
}
