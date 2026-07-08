using System;
using System.Net;
using System.Text.RegularExpressions;
using NzbDrone.Common.Extensions;

namespace NzbDrone.Core.Indexers.Ed2k
{
    public class Ed2kLink
    {
        private static readonly Regex FileLinkRegex = new Regex(@"^ed2k://\|file\|(?<name>[^|]+)\|(?<size>\d+)\|(?<hash>[a-fA-F0-9]{32})\|/?$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public string FileName { get; set; }
        public long Size { get; set; }
        public string Hash { get; set; }
        public string Url { get; set; }

        public static bool TryParse(string link, out Ed2kLink result)
        {
            result = null;

            if (link.IsNullOrWhiteSpace())
            {
                return false;
            }

            var match = FileLinkRegex.Match(link.Trim());

            if (!match.Success)
            {
                return false;
            }

            result = new Ed2kLink
            {
                FileName = WebUtility.UrlDecode(match.Groups["name"].Value),
                Size = long.Parse(match.Groups["size"].Value),
                Hash = match.Groups["hash"].Value.ToUpperInvariant(),
                Url = link.Trim()
            };

            return true;
        }

        public static Ed2kLink Parse(string link)
        {
            if (!TryParse(link, out var result))
            {
                throw new FormatException("Invalid ed2k file link.");
            }

            return result;
        }

        public static string Create(string fileName, long size, string hash)
        {
            return $"ed2k://|file|{Uri.EscapeDataString(fileName)}|{size}|{hash.ToUpperInvariant()}|/";
        }
    }
}
