import Provider from './Provider';

export type Protocol = 'torrent' | 'usenet' | 'kad' | 'ed2kGlobal' | 'unknown';

interface DownloadClient extends Provider {
  enable: boolean;
  protocol: Protocol;
  priority: number;
  removeCompletedDownloads: boolean;
  removeFailedDownloads: boolean;
  tags: number[];
}

export default DownloadClient;
