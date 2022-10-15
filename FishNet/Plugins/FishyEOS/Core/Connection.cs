using Epic.OnlineServices;
using Epic.OnlineServices.P2P;

namespace FishNet.Transporting.FishyEOSPlugin
{
    public struct Connection
    {
        public int Id;
        public ProductUserId LocalUserId;
        public ProductUserId RemoteUserId;
        public SocketId? SocketId;

        public Connection(int connectionId, ProductUserId localUserId, ProductUserId remoteUserId, SocketId? socketId)
        {
            Id = connectionId;
            LocalUserId = localUserId;
            RemoteUserId = remoteUserId;
            SocketId = socketId;
        }
    }
}