using System.Collections.Generic;

namespace NzbDrone.Core.Download.Clients.AMule
{
    public class AMuleSearchResult
    {
        public string FileName { get; set; }
        public long Size { get; set; }
        public string Hash { get; set; }
        public string Ed2kLink { get; set; }
        public int Sources { get; set; }
        public AMuleSearchType SearchType { get; set; }
    }

    public class AMuleQueueItem
    {
        public string FileName { get; set; }
        public long Size { get; set; }
        public long CompletedSize { get; set; }
        public string Hash { get; set; }
        public string Ed2kLink { get; set; }
        public int Sources { get; set; }
        public int Status { get; set; }
        public int Category { get; set; }
    }

    public class AMulePreferences
    {
        public string IncomingDirectory { get; set; }
        public List<AMuleCategory> Categories { get; set; } = new ();
    }

    public class AMuleCategory
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Path { get; set; }
    }

    public enum AMuleSearchType
    {
        Ed2kGlobal = AMuleEcCodes.SearchGlobal,
        Kad = AMuleEcCodes.SearchKad,
        Both = 100
    }
}
