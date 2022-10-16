using System;
using System.Collections;
using Epic.OnlineServices;
using Epic.OnlineServices.P2P;
using FishNet.Managing.Logging;
using PlayEveryWare.EpicOnlineServices;
using UnityEngine;

namespace FishNet.Transporting.FishyEOSPlugin
{
    public class ClientPeer : CommonPeer
    {
        #region Private.
        /// <summary>EOS Socket Id</summary>
        private SocketId? _socketId;
        
        /// <summary>EOS Client User Id</summary>
        private ProductUserId _localUserId;
        
        /// <summary>EOS Server User Id</summary>
        private ProductUserId _remoteUserId;
        
        /// <summary>EOS Connection Type</summary>
        private string connectionType;
        
        /// <summary>EOS NAT Type</summary>
        private string natType;
        
        /// <summary>EOS Handle for Peer Connection Established Event</summary>
        private ulong? _peerConnectionEstablishedEventHandle;
        
        /// <summary>EOS Handle for Peer Connection Closed Event</summary>
        private ulong? _peerConnectionClosedEventHandle;
        #endregion

        /// <summary>Starts the client connection. [Uses data stored on FishyEOS Transport]</summary>
        internal void StartConnection()
        {
            Transport.StartCoroutine(StartConnectionCoroutine());
        }

        /// <summary>Coroutine that authenticates with EOS and then starts a connection to the server.</summary>
        private IEnumerator StartConnectionCoroutine()
        {
            base.SetLocalConnectionState(LocalConnectionState.Starting, false);
            
            // Attempt to Authenticate with EOS Connect...
            Transport.AuthConnectData.Connect();
            yield return Transport.AuthConnectData.coroutine;
            if (Transport.AuthConnectData.loginCallbackInfo?.ResultCode != Result.Success)
            {
                if (Transport.NetworkManager.CanLog(LoggingType.Error))
                    Debug.LogError(
                        $"[ServerPeer] Failed to authenticate with EOS Connect. {Transport.AuthConnectData.loginCallbackInfo?.ResultCode}");
                base.SetLocalConnectionState(LocalConnectionState.Stopped, true);
                yield break;
            }
            if (Transport.NetworkManager.CanLog(LoggingType.Common))
                Debug.Log($"[ServerPeer] Authenticated with EOS Connect. {EOSManager.Instance.GetProductUserId()}");
            
            // Attempt to connect to Server Remote User Id P2P connection...
            _localUserId = EOSManager.Instance.GetProductUserId();
            _remoteUserId = ProductUserId.FromString(Transport.RemoteProductUserId);
            _socketId = new SocketId { SocketName = Transport.SocketName };

            if (_peerConnectionEstablishedEventHandle.HasValue)
                EOSManager.Instance.GetEOSP2PInterface()
                    .RemoveNotifyPeerConnectionEstablished(_peerConnectionEstablishedEventHandle.Value);

            if (_peerConnectionClosedEventHandle.HasValue)
                EOSManager.Instance.GetEOSP2PInterface()
                    .RemoveNotifyPeerConnectionClosed(_peerConnectionClosedEventHandle.Value);

            var addNotifyPeerConnectionEstablishedOptions = new AddNotifyPeerConnectionEstablishedOptions
            {
                LocalUserId = _localUserId,
                SocketId = _socketId,
            };
            _peerConnectionEstablishedEventHandle = EOSManager.Instance.GetEOSP2PInterface()
                .AddNotifyPeerConnectionEstablished(ref addNotifyPeerConnectionEstablishedOptions, null,
                    OnPeerConnectionEstablished);

            var addNotifyPeerConnectionClosedOptions = new AddNotifyPeerConnectionClosedOptions
            {
                LocalUserId = _localUserId,
                SocketId = _socketId,
            };
            _peerConnectionClosedEventHandle = EOSManager.Instance.GetEOSP2PInterface()
                .AddNotifyPeerConnectionClosed(ref addNotifyPeerConnectionClosedOptions, null,
                    OnPeerConnectionClosed);

            var acceptConnectionOptions = new AcceptConnectionOptions
            {
                LocalUserId = _localUserId,
                RemoteUserId = _remoteUserId,
                SocketId = _socketId,
            };
            var acceptConnectionResult =
                EOSManager.Instance.GetEOSP2PInterface().AcceptConnection(ref acceptConnectionOptions);
            if (acceptConnectionResult != Result.Success)
            {
                base.SetLocalConnectionState(LocalConnectionState.Stopped, false);
                if (Transport.NetworkManager.CanLog(LoggingType.Error))
                    Debug.LogError(
                        $"[{nameof(ClientPeer)}] AcceptConnection failed with error: {acceptConnectionResult}");
                StopConnection();
                yield break;
            }

            var queryNATTypeOptions = new QueryNATTypeOptions();
            EOSManager.Instance.GetEOSP2PInterface().QueryNATType(ref queryNATTypeOptions, null, OnQueryNATType);
        }

        /// <summary>Stops the client connection.</summary>
        internal bool StopConnection()
        {
            if (GetLocalConnectionState() == LocalConnectionState.Stopped ||
                GetLocalConnectionState() == LocalConnectionState.Stopping)
                return false;

            base.SetLocalConnectionState(LocalConnectionState.Stopping, false);

            var closeConnectionOptions = new CloseConnectionOptions
            {
                SocketId = _socketId,
                LocalUserId = _localUserId,
                RemoteUserId = _remoteUserId,
            };
            var result = EOSManager.Instance.GetEOSP2PInterface()?.CloseConnection(ref closeConnectionOptions);
            base.SetLocalConnectionState(LocalConnectionState.Stopped, false);

            if (result == Result.Success) return true;

            if (Transport.NetworkManager.CanLog(LoggingType.Error))
                Debug.LogWarning($"[ClientPeer] Failed to close connection. Error: {result}");
            return false;
        }

        /// <summary>Called when connected to the server.</summary>
        private void OnPeerConnectionEstablished(ref OnPeerConnectionEstablishedInfo data)
        {
            if (_peerConnectionEstablishedEventHandle.HasValue)
                EOSManager.Instance.GetEOSP2PInterface()
                    .RemoveNotifyPeerConnectionEstablished(_peerConnectionEstablishedEventHandle.Value);

            connectionType = data.ConnectionType.ToString();
            base.SetLocalConnectionState(LocalConnectionState.Started, false);
            if (Transport.NetworkManager.CanLog(LoggingType.Common))
                Debug.Log($"[ClientPeer] Connection to server established. ConnectionType: {connectionType}");
        }
        
        /// <summary>Called when disconnected from the server.</summary>
        private void OnPeerConnectionClosed(ref OnRemoteConnectionClosedInfo data)
        {
            if (_peerConnectionEstablishedEventHandle.HasValue)
                EOSManager.Instance.GetEOSP2PInterface()
                    .RemoveNotifyPeerConnectionEstablished(_peerConnectionEstablishedEventHandle.Value);

            if (_peerConnectionClosedEventHandle.HasValue)
                EOSManager.Instance.GetEOSP2PInterface()
                    .RemoveNotifyPeerConnectionClosed(_peerConnectionClosedEventHandle.Value);

            if (Transport.NetworkManager.CanLog(LoggingType.Common))
                Debug.Log($"[ClientPeer] Connection to server closed.");
            StopConnection();
        }
        
        /// <summary>Unused for EOS Transport</summary>
        internal void IterateOutgoing() { }

        /// <summary>Iterates incoming packets.</summary>
        internal void IterateIncoming()
        {
            //Stopped or trying to stop.
            if (GetLocalConnectionState() == LocalConnectionState.Stopped ||
                GetLocalConnectionState() == LocalConnectionState.Stopping)
                return;

            var incomingPacketCount = GetIncomingPacketQueueCurrentPacketCount();
            for (ulong i = 0; i < incomingPacketCount; i++)
                if (Receive(_localUserId, out _, out var segment, out var channel))
                    Transport.HandleClientReceivedDataArgs(
                        new ClientReceivedDataArgs(segment, channel, Transport.Index));
        }

        /// <summary>Sends a packet to the server.</summary>
        internal void SendToServer(byte channelId, ArraySegment<byte> segment)
        {
            if (GetLocalConnectionState() != LocalConnectionState.Started)
                return;

            var result = Send(_localUserId, _remoteUserId, _socketId, channelId, segment);
            if (result == Result.NoConnection || result == Result.InvalidParameters)
            {
                if (Transport.NetworkManager.CanLog(LoggingType.Common))
                    Debug.Log($"[ClientPeer] Connection to server was lost.");
                StopConnection();
            }
            else if (result != Result.Success)
            {
                if (Transport.NetworkManager.CanLog(LoggingType.Error))
                    Debug.LogError($"[ClientPeer] Could not send: {result.ToString()}");
            }
        }

        /// <summary>Determines the NAT Type of the client connection.</summary>
        private void OnQueryNATType(ref OnQueryNATTypeCompleteInfo data)
        {
            natType = data.NATType.ToString();
            if (Transport.NetworkManager.CanLog(LoggingType.Common))
                Debug.Log($"[{nameof(ClientPeer)}] NATType: {natType}");
        }
    }
}