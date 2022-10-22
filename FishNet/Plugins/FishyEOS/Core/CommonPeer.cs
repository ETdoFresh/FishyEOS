using System;
using System.Collections.Generic;
using Epic.OnlineServices;
using Epic.OnlineServices.P2P;
using FishNet.Managing.Logging;
using FishNet.Plugins.FishyEOS.Util;
using UnityEngine;

namespace FishNet.Transporting.FishyEOSPlugin
{
    public abstract class CommonPeer
    {
        # region Private.

        /// <summary>
        /// Current ConnectionState.
        /// </summary>
        private LocalConnectionState _connectionState = LocalConnectionState.Stopped;

        #endregion

        #region Protected.

        /// <summary>
        /// Transport controlling this peer.
        /// </summary>
        protected FishyEOS _transport;

        #endregion

        /// <summary>
        /// Returns the current ConnectionState.
        /// </summary>
        internal LocalConnectionState GetLocalConnectionState()
        {
            return _connectionState;
        }

        /// <summary>
        /// Sets a new connection state.
        /// </summary>
        /// <param name="connectionState"></param>
        protected virtual void SetLocalConnectionState(LocalConnectionState connectionState, bool server)
        {
            //If state hasn't changed.
            if (connectionState == _connectionState)
                return;

            _connectionState = connectionState;

            if (server)
                _transport.HandleServerConnectionState(new ServerConnectionStateArgs(connectionState,
                    _transport.Index));
            else
                _transport.HandleClientConnectionState(new ClientConnectionStateArgs(connectionState,
                    _transport.Index));
        }

        /// <summary>
        /// Initializes this for use.
        /// </summary>
        /// <param name="transport"></param>
        internal void Initialize(FishyEOS transport)
        {
            _transport = transport;
        }

        /// <summary>
        /// Clears a queue.
        /// </summary>
        /// <param name="queue"></param>
        internal void ClearQueue(ref Queue<LocalPacket> queue)
        {
            while (queue.Count > 0)
            {
                queue.Dequeue();
            }
        }

        /// <summary>
        /// Sends a message to remote user through EOS P2P Interface.
        /// </summary>
        internal Result Send(ProductUserId localUserId, ProductUserId remoteUserId, SocketId? socketId,
            byte channelId, ArraySegment<byte> segment)
        {
            if (GetLocalConnectionState() != LocalConnectionState.Started)
                return Result.InvalidState;

            var reliability =
                channelId == 0 ? PacketReliability.ReliableOrdered : PacketReliability.UnreliableUnordered;
            var allowDelayedDelivery = channelId == 0;

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
            var result = EOS.GetCachedP2PInterface().SendPacket(ref sendPacketOptions);
            if (result != Result.Success)
                Debug.LogWarning(
                    $"Failed to send packet to {remoteUserId} with size {segment.Count} with error {result}");
            return result;
        }

        /// <summary>
        /// Returns a message from the EOS P2P Interface.
        /// </summary>
        protected bool Receive(ProductUserId localUserId, out ProductUserId remoteUserId, out ArraySegment<byte> data,
            out Channel channel)
        {
            remoteUserId = null;
            data = default;
            channel = Channel.Unreliable;

            var getNextReceivedPacketSizeOptions = new GetNextReceivedPacketSizeOptions
            {
                LocalUserId = localUserId,
            };
            var getPacketSizeResult =
                EOS.GetCachedP2PInterface()
                    .GetNextReceivedPacketSize(ref getNextReceivedPacketSizeOptions, out var packetSize);
            if (getPacketSizeResult == Result.NotFound)
            {
                return false; // this is fine, just no packets to read
            }

            if (getPacketSizeResult != Result.Success)
            {
                if (_transport.NetworkManager.CanLog(LoggingType.Error))
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
            var receivePacketResult = EOS.GetCachedP2PInterface().ReceivePacket(ref receivePacketOptions,
                out remoteUserId, out _, out var channelByte, data, out _);
            channel = (Channel)channelByte;
            if (receivePacketResult != Result.Success)
            {
                if (_transport.NetworkManager.CanLog(LoggingType.Error))
                    Debug.LogError(
                        $"[{nameof(ClientPeer)}] ReceivePacket failed with error: {receivePacketResult}");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Gets the number of packets incoming from the EOS P2P Interface.
        /// </summary>
        protected ulong GetIncomingPacketQueueCurrentPacketCount()
        {
            var getPacketQueueInfoOptions = new GetPacketQueueInfoOptions();
            var getPacketQueueResult =
                EOS.GetCachedP2PInterface().GetPacketQueueInfo(ref getPacketQueueInfoOptions, out var packetQueueInfo);
            if (getPacketQueueResult != Result.Success)
            {
                if (_transport.NetworkManager.CanLog(LoggingType.Error))
                    Debug.LogError($"[CommonSocket] Failed to get packet queue info with error {getPacketQueueResult}");
                return 0;
            }

            return packetQueueInfo.IncomingPacketQueueCurrentPacketCount;
        }
    }
}