using Epic.OnlineServices;
using Epic.OnlineServices.P2P;

namespace FishNet.Transporting.FishyEOSPlugin
{
    /// <summary>A struct that contains information about connection between two peers.</summary>
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