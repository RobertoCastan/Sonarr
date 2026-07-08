namespace NzbDrone.Core.Download.Clients.AMule
{
    public static class AMuleEcCodes
    {
        public const byte OpAuthReq = 0x02;
        public const byte OpAuthFail = 0x03;
        public const byte OpAuthOk = 0x04;
        public const byte OpFailed = 0x05;
        public const byte OpAddLink = 0x09;
        public const byte OpGetDloadQueue = 0x0D;
        public const byte OpSearchStart = 0x26;
        public const byte OpSearchResults = 0x28;
        public const byte OpGetPreferences = 0x3F;
        public const byte OpAuthSalt = 0x4F;
        public const byte OpAuthPasswd = 0x50;

        public const ushort TagString = 0x0000;
        public const ushort TagPasswdHash = 0x0001;
        public const ushort TagProtocolVersion = 0x0002;
        public const ushort TagDetailLevel = 0x0004;
        public const ushort TagPasswdSalt = 0x000B;
        public const ushort TagClientName = 0x0100;
        public const ushort TagClientVersion = 0x0101;
        public const ushort TagPartFile = 0x0300;
        public const ushort TagPartFileName = 0x0301;
        public const ushort TagPartFileSizeFull = 0x0303;
        public const ushort TagPartFileSizeDone = 0x0306;
        public const ushort TagPartFileStatus = 0x0308;
        public const ushort TagPartFileSourceCount = 0x030A;
        public const ushort TagPartFileEd2kLink = 0x030E;
        public const ushort TagPartFileHash = 0x031E;
        public const ushort TagSearchFile = 0x0700;
        public const ushort TagSearchType = 0x0701;
        public const ushort TagSearchName = 0x0702;
        public const ushort TagSearchFileType = 0x0705;
        public const ushort TagSearchAvailability = 0x0707;
        public const ushort TagSearchStatus = 0x0708;
        public const ushort TagSelectPrefs = 0x1000;
        public const ushort TagDirectoriesIncoming = 0x1A01;
        public const ushort TagServerVersion = 0x050B;

        public const byte DetailCmd = 0;
        public const byte DetailFull = 2;
        public const byte SearchGlobal = 1;
        public const byte SearchKad = 2;
        public const byte StatusComplete = 9;
        public const ushort ProtocolVersion = 0x0204;
    }
}
