using System.Collections.Generic;

namespace NzbDrone.Core.Download.Clients.AMule
{
    public class AMuleEcPacket
    {
        public byte OpCode { get; set; }
        public List<AMuleEcTag> Tags { get; set; } = new ();

        public AMuleEcPacket()
        {
        }

        public AMuleEcPacket(byte opCode)
        {
            OpCode = opCode;
        }

        public AMuleEcTag Find(ushort name)
        {
            return Tags.Find(v => v.Name == name);
        }
    }
}
