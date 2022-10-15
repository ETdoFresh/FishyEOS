using System;
using System.Collections.Generic;
using Epic.OnlineServices;
using Epic.OnlineServices.P2P;
using FishNet.Managing.Logging;
using PlayEveryWare.EpicOnlineServices;
using UnityEngine;

namespace FishNet.Transporting.FishyEOSPlugin
{
    public abstract class CommonPeer
    {
        private LocalConnectionState _connectionState = LocalConnectionState.Stopped;

        internal LocalConnectionState GetLocalConnectionState()
        {
            return _connectionState;
        }

        protected virtual void SetLocalConnectionState(LocalConnectionState connectionState, bool server)
        {
            //If state hasn't changed.
            if (connectionState == _connectionState)
                return;

            _connectionState = connectionState;

            if (server)
                Transport.HandleServerConnectionState(new ServerConnectionStateArgs(connectionState, Transport.Index));
            else
                Transport.HandleClientConnectionState(new ClientConnectionStateArgs(connectionState, Transport.Index));
        }

        protected FishyEOS Transport = null;

        internal void Initialize(FishyEOS transport)
        {
            Transport = transport;
        }

        internal void ClearQueue(ref Queue<LocalPacket> queue)
        {
            while (queue.Count > 0)
            {
                LocalPacket lp = queue.Dequeue();
                //lp.Dispose();
            }
        }

        internal Result Send(ProductUserId localUserId, ProductUserId remoteUserId, SocketId? socketId,
            byte channelId, ArraySegment<byte> segment)
        {
            if (GetLocalConnectionState() != LocalConnectionState.Started)
                return Result.InvalidState;

            var reliability =
                channelId == 0 ? PacketReliability.ReliableOrdered : PacketReliability.UnreliableUnordered;
            var allowDelayedDelivery = channelId == 0 ? true : false;

            var sendPacketOptions = new SendPacketOptions
            {
                LocalUserId = localUserId,
                RemoteUserId = remoteUserId,
                SocketId = socketId,
                Channel = channelId,
                Data = segment,
                Reliability = reliability,
                AllowDelayedDelivery = allowDelayedDelivery
            };
            var result = EOSManager.Instance.GetEOSP2PInterface().SendPacket(ref sendPacketOptions);
            if (result != Result.Success)
                Debug.LogWarning(
                    $"Failed to send packet to {remoteUserId} with size {segment.Count} with error {result}");
            return result;
        }

        protected bool Receive(ProductUserId localUserId, out ProductUserId remoteUserId, out ArraySegment<byte> data,
            out Channel channel)
        {
            remoteUserId = null;
            data = null;
            channel = Channel.Unreliable;
            
            var getNextReceivedPacketSizeOptions = new GetNextReceivedPacketSizeOptions
            {
                LocalUserId = localUserId,
            };
            var getPacketSizeResult = EOSManager.Instance.GetEOSP2PInterface()
                .GetNextReceivedPacketSize(ref getNextReceivedPacketSizeOptions, out var packetSize);
            if (getPacketSizeResult == Result.NotFound)
            {
                return false; // this is fine, just no packets to read
            }

            if (getPacketSizeResult != Result.Success)
            {
                if (Transport.NetworkManager.CanLog(LoggingType.Error))
                    Debug.LogError(
                        $"[{nameof(ClientPeer)}] GetNextReceivedPacketSize failed with error: {getPacketSizeResult}");
                return false;
            }

            var receivePacketOptions = new ReceivePacketOptions
            {
                LocalUserId = localUserId,
                MaxDataSizeBytes = packetSize,
            };
            data = new ArraySegment<byte>(new byte[packetSize]);
            var receivePacketResult = EOSManager.Instance.GetEOSP2PInterface()
                .ReceivePacket(ref receivePacketOptions, out remoteUserId, out _, out var channelByte, data, out _);
            channel = (Channel)channelByte;
            if (receivePacketResult != Result.Success)
            {
                if (Transport.NetworkManager.CanLog(LoggingType.Error))
                    Debug.LogError(
                        $"[{nameof(ClientPeer)}] ReceivePacket failed with error: {receivePacketResult}");
                return false;
            }

            return true;
        }
        
        protected ulong GetIncomingPacketQueueCurrentPacketCount()
        {
            var getPacketQueueOptions = new GetPacketQueueInfoOptions();
            var getPacketQueueResult = EOSManager.Instance.GetEOSP2PInterface()
                .GetPacketQueueInfo(ref getPacketQueueOptions, out var packetQueueInfo);
            if (getPacketQueueResult != Result.Success)
            {
                if (Transport.NetworkManager.CanLog(LoggingType.Error))
                    Debug.LogError($"[CommonSocket] Failed to get packet queue info with error {getPacketQueueResult}");
                return 0;
            }

            return packetQueueInfo.IncomingPacketQueueCurrentPacketCount;
        }
    }
}