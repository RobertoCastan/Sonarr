using System.Collections.Generic;

namespace NzbDrone.Core.Download.Clients.AMule
{
    public interface IAMuleProxy
    {
        string GetVersion(AMuleSettings settings);
        void AddLink(AMuleSettings settings, string ed2kLink);
        List<AMuleQueueItem> GetQueue(AMuleSettings settings);
        AMulePreferences GetPreferences(AMuleSettings settings);
        List<AMuleSearchResult> Search(AMuleSettings settings, AMuleSearchType searchType, string query, int searchDelay);
    }
}
