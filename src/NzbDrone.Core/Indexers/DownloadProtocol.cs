namespace NzbDrone.Core.Indexers
{
    public enum DownloadProtocol
    {
        Unknown = 0,
        Usenet = 1,
        Torrent = 2,
        Kad = 3,
        Ed2kGlobal = 4
    }
}
