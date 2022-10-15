using System;

namespace FishNet.Transporting.FishyEOSPlugin
{
    internal class LocalPacket
    {
        public byte[] Data { get; }
        public int Length { get; }
        public Channel Channel { get; }

        public LocalPacket(ArraySegment<byte> data, byte channelId)
        {
            Data = new byte[data.Count];
            Length = data.Count;
            Buffer.BlockCopy(data.Array, data.Offset, Data, 0, Length);
            Channel = (Channel)channelId;
        }
    }
}