using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Epic.OnlineServices;
using Epic.OnlineServices.P2P;
using FishNet.Managing.Logging;
using PlayEveryWare.EpicOnlineServices;
using UnityEngine;

namespace FishNet.Transporting.FishyEOSPlugin
{
    public class ServerPeer : CommonPeer
    {
        /// <summary>An auto-incrementing id for each peer.</summary>
        private static int _latestId = 1;

        # region Private.

        /// <summary>EOS Socket Id for this peer.</summary>
        private SocketId _socketId;

        /// <summary>EOS Server User Id for this peer.</summary>
        private ProductUserId _localUserId;

        /// <summary>Queue of incoming local client host packets</summary>
        private Queue<LocalPacket> _clientHostIncoming = new();

        /// <summary>Reference to Local Client Host</summary>
        private ClientHostPeer _clientHost;

        /// <summary>EOS Handle for Incoming Peer Requests.</summary>
        private ulong? _acceptPeerConnectionsEventHandle;

        /// <summary>EOS Handle for Incoming Peer Connections.</summary>
        private Dictionary<Connection, ulong> _establishPeerConnectionEventHandles = new();

        /// <summary>EOS Handle for Incoming Peer Disconnections.</summary>
        private Dictionary<Connection, ulong> _closePeerConnectionEventHandles = new();

        /// <summary>List of connections to this server.</summary>
        private List<Connection> _clients = new();

        /// <summary>Maximum number of connections allowed.</summary>
        private int _maximumClients = short.MaxValue;

        #endregion

        /// <summary>Starts the server.</summary>
        internal bool StartConnection()
        {
            base.SetLocalConnectionState(LocalConnectionState.Starting, true);
            Transport.StartCoroutine(AuthenticateAndStartListeningForConnections());
            return true;
        }

        /// <summary>Coroutine that authenticates with EOS and starts listening for incoming connections.</summary>
        private IEnumerator AuthenticateAndStartListeningForConnections()
        {
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

            // Attempt to Start Listening for Peer Connections...
            try
            {
                _localUserId = EOSManager.Instance.GetProductUserId();
                _socketId = new SocketId { SocketName = Transport.SocketName };
                var addNotifyPeerConnectionRequestOptions = new AddNotifyPeerConnectionRequestOptions
                {
                    SocketId = _socketId,
                    LocalUserId = _localUserId,
                };
                _acceptPeerConnectionsEventHandle = EOSManager.Instance.GetEOSP2PInterface()
                    .AddNotifyPeerConnectionRequest(ref addNotifyPeerConnectionRequestOptions, null,
                        OnPeerConnectionRequest);

                if (Transport.NetworkManager.CanLog(LoggingType.Common))
                    Debug.Log(
                        $"[ServerPeer] Started listening for incoming connections. Handle #{_acceptPeerConnectionsEventHandle}");
            }
            catch (Exception e)
            {
                if (Transport.NetworkManager.CanLog(LoggingType.Error))
                    Debug.LogError($"[ServerPeer] Failed to start listening for incoming connections. {e}");
                base.SetLocalConnectionState(LocalConnectionState.Stopped, true);
                yield break;
            }

            base.SetLocalConnectionState(LocalConnectionState.Started, true);
        }

        /// <summary>Event Callback when peer connection request is received.</summary>
        private void OnPeerConnectionRequest(ref OnIncomingConnectionRequestInfo data)
        {
            var nextId = _latestId++;
            var clientConnection = new Connection(nextId, data.LocalUserId, data.RemoteUserId, data.SocketId);
            _clients.Add(clientConnection);

            var addNotifyPeerConnectionEstablishedOptions = new AddNotifyPeerConnectionEstablishedOptions
            {
                SocketId = data.SocketId,
                LocalUserId = data.LocalUserId,
            };
            var connectionEstablishedHandle = EOSManager.Instance.GetEOSP2PInterface()
                .AddNotifyPeerConnectionEstablished(ref addNotifyPeerConnectionEstablishedOptions, clientConnection,
                    OnPeerConnectionEstablished);
            _establishPeerConnectionEventHandles.Add(clientConnection, connectionEstablishedHandle);

            var acceptConnectionOptions = new AcceptConnectionOptions
            {
                LocalUserId = _localUserId,
                RemoteUserId = data.RemoteUserId,
                SocketId = data.SocketId,
            };
            var acceptConnectionResult =
                EOSManager.Instance.GetEOSP2PInterface().AcceptConnection(ref acceptConnectionOptions);

            if (acceptConnectionResult != Result.Success)
            {
                _clients.Remove(clientConnection);
                if (Transport.NetworkManager.CanLog(LoggingType.Error))
                    Debug.LogError(
                        $"[ServerPeer] Failed to accept connection from {data.RemoteUserId} with handle #{data.SocketId} and connection id {nextId}. {acceptConnectionResult}");
            }
        }

        /// <summary>Event Callback when peer connection is established.</summary>
        private void OnPeerConnectionEstablished(ref OnPeerConnectionEstablishedInfo data)
        {
            var clientConnection = (Connection)data.ClientData;
            if (_establishPeerConnectionEventHandles.TryGetValue(clientConnection, out var notificationId))
            {
                EOSManager.Instance.GetEOSP2PInterface().RemoveNotifyPeerConnectionEstablished(notificationId);
                _establishPeerConnectionEventHandles.Remove(clientConnection);
            }

            var addNotifyPeerConnectionClosedOptions = new AddNotifyPeerConnectionClosedOptions
            {
                SocketId = clientConnection.SocketId,
                LocalUserId = clientConnection.LocalUserId,
            };
            var closePeerConnectionHandle = EOSManager.Instance.GetEOSP2PInterface()
                .AddNotifyPeerConnectionClosed(ref addNotifyPeerConnectionClosedOptions, clientConnection,
                    OnPeerConnectionClosed);
            _closePeerConnectionEventHandles.Add(clientConnection, closePeerConnectionHandle);

            Transport.HandleRemoteConnectionState(new RemoteConnectionStateArgs(RemoteConnectionState.Started,
                clientConnection.Id, Transport.Index));
            if (Transport.NetworkManager.CanLog(LoggingType.Common))
                Debug.Log(
                    $"[ServerPeer] Established connection from {data.RemoteUserId} with handle #{data.SocketId} and connection id {clientConnection.Id}.");
        }

        /// <summary>Event Callback when peer connection is closed.</summary>
        private void OnPeerConnectionClosed(ref OnRemoteConnectionClosedInfo data)
        {
            var clientConnection = (Connection)data.ClientData;
            _clients.Remove(clientConnection);

            if (_closePeerConnectionEventHandles.TryGetValue(clientConnection, out var notificationId))
            {
                EOSManager.Instance.GetEOSP2PInterface().RemoveNotifyPeerConnectionClosed(notificationId);
                _closePeerConnectionEventHandles.Remove(clientConnection);
            }

            Transport.HandleRemoteConnectionState(new RemoteConnectionStateArgs(RemoteConnectionState.Stopped,
                clientConnection.Id, Transport.Index));
            if (Transport.NetworkManager.CanLog(LoggingType.Common))
                Debug.Log(
                    $"[ServerPeer] Closed connection from {data.RemoteUserId} with handle #{data.SocketId} and connection id {clientConnection.Id}.");
        }

        /// <summary>Stops the server.</summary>
        internal bool StopConnection()
        {
            if (GetLocalConnectionState() == LocalConnectionState.Stopped ||
                GetLocalConnectionState() == LocalConnectionState.Stopping)
                return false;

            base.SetLocalConnectionState(LocalConnectionState.Stopping, true);

            if (EOSManager.Instance.GetEOSP2PInterface() == null)
            {
                if (Transport.NetworkManager.CanLog(LoggingType.Error))
                    Debug.LogError($"[ServerPeer] Failed to stop connections. EOSP2PInterface is null.");
                base.SetLocalConnectionState(LocalConnectionState.Stopped, true);
                return false;
            }

            try
            {
                _clients.Clear();
                _clientHostIncoming.Clear();
                _clientHost.StopConnection();

                foreach (var entry in _closePeerConnectionEventHandles)
                    EOSManager.Instance.GetEOSP2PInterface().RemoveNotifyPeerConnectionClosed(entry.Value);
                _closePeerConnectionEventHandles.Clear();

                foreach (var entry in _establishPeerConnectionEventHandles)
                    EOSManager.Instance.GetEOSP2PInterface().RemoveNotifyPeerConnectionEstablished(entry.Value);
                _establishPeerConnectionEventHandles.Clear();

                if (_acceptPeerConnectionsEventHandle.HasValue)
                {
                    EOSManager.Instance.GetEOSP2PInterface()
                        .RemoveNotifyPeerConnectionRequest(_acceptPeerConnectionsEventHandle.Value);
                    _acceptPeerConnectionsEventHandle = null;
                }

                var closeConnectionOptions = new CloseConnectionsOptions
                {
                    SocketId = _socketId,
                    LocalUserId = _localUserId,
                };
                EOSManager.Instance.GetEOSP2PInterface().CloseConnections(ref closeConnectionOptions);
            }
            catch (Exception e)
            {
                if (Transport.NetworkManager.CanLog(LoggingType.Error))
                    Debug.LogError($"[ServerPeer] Failed to stop listening for incoming connections. {e}");
                base.SetLocalConnectionState(LocalConnectionState.Stopped, true);
                return false;
            }

            base.SetLocalConnectionState(LocalConnectionState.Stopped, true);
            return true;
        }

        /// <summary>Stops a remote client from the server, disconnecting the client.</summary>
        /// <param name="connectionId"></param>
        internal bool StopConnection(int connectionId)
        {
            if (connectionId == FishyEOS.CLIENT_HOST_ID)
            {
                _clientHost.StopConnection();
                return true;
            }

            var clientConnectionExists = _clients.Any(x => x.Id == connectionId);
            if (!clientConnectionExists) return false;

            var clientConnection = _clients.FirstOrDefault(x => x.Id == connectionId);
            var closeConnectionOptions = new CloseConnectionOptions
            {
                SocketId = _socketId,
                LocalUserId = clientConnection.LocalUserId,
                RemoteUserId = clientConnection.RemoteUserId
            };
            EOSManager.Instance.GetEOSP2PInterface().CloseConnection(ref closeConnectionOptions);
            return true;
        }

        /// <summary>Gets the current ConnectionState of a remote client on the server.</summary>
        /// <param name="connectionId">ConnectionId to get ConnectionState for.</param>
        internal RemoteConnectionState GetConnectionState(int connectionId)
        {
            if (_clients.Any(x => x.Id == connectionId))
                return RemoteConnectionState.Started;
            else
                return RemoteConnectionState.Stopped;
        }

        /// <summary>Unused by EOS.</summary>
        internal void IterateOutgoing() { }

        /// <summary>Iterates through all incoming packets and handles them.</summary>
        internal void IterateIncoming()
        {
            if (GetLocalConnectionState() != LocalConnectionState.Started)
                return;

            //Iterate local client packets first.
            while (_clientHostIncoming.Count > 0)
            {
                var packet = _clientHostIncoming.Dequeue();
                var segment = new ArraySegment<byte>(packet.Data, 0, packet.Length);
                Transport.HandleServerReceivedDataArgs(new ServerReceivedDataArgs(segment, packet.Channel,
                    FishyEOS.CLIENT_HOST_ID, Transport.Index));
            }

            var incomingPacketCount = GetIncomingPacketQueueCurrentPacketCount();
            for (ulong i = 0; i < incomingPacketCount; i++)
                if (Receive(_localUserId, out var remoteUserId, out var data, out var channel))
                {
                    var connectionId = _clients.First(x => x.RemoteUserId == remoteUserId).Id;
                    Transport.HandleServerReceivedDataArgs(new ServerReceivedDataArgs(data, channel, connectionId,
                        Transport.Index));
                }
        }

        /// <summary>Sends a packet to a single, or all clients.</summary>
        internal void SendToClient(byte channelId, ArraySegment<byte> segment, int connectionId)
        {
            if (GetLocalConnectionState() != LocalConnectionState.Started)
                return;

            if (connectionId == FishyEOS.CLIENT_HOST_ID)
            {
                if (_clientHost != null)
                {
                    var packet = new LocalPacket(segment, channelId);
                    _clientHost.ReceivedFromLocalServer(packet);
                }

                return;
            }

            if (_clients.Any(x => x.Id == connectionId))
            {
                var clientConnection = _clients.First(x => x.Id == connectionId);
                var result = Send(_localUserId, clientConnection.RemoteUserId, _socketId, channelId, segment);

                if (result == Result.NoConnection || result == Result.InvalidParameters)
                {
                    if (Transport.NetworkManager.CanLog(LoggingType.Common))
                        Debug.Log($"Connection to {connectionId} was lost.");
                    StopConnection(connectionId);
                }
                else if (result != Result.Success)
                {
                    if (Transport.NetworkManager.CanLog(LoggingType.Error))
                        Debug.LogError($"Could not send: {result.ToString()}");
                }
            }
            else
            {
                if (Transport.NetworkManager.CanLog(LoggingType.Error))
                    Debug.LogError($"ConnectionId {connectionId} does not exist, data will not be sent.");
            }
        }

        /// <summary>Returns the maximum number of clients allowed to connect to the server. If the transport does not support this method the value -1 is returned.</summary>
        public int GetMaximumClients()
        {
            return _maximumClients;
        }

        /// <summary>Sets the maximum number of clients allowed to connect to the server.</summary>
        public void SetMaximumClients(int value)
        {
            _maximumClients = value;
        }

        /// <summary>Sets the local client host.</summary>
        internal void SetClientHostPeer(ClientHostPeer clientHostPeer)
        {
            _clientHost = clientHostPeer;
        }

        /// <summary>Queues a received packet from the local client host.</summary>
        internal void ReceivedFromClientHost(LocalPacket packet)
        {
            if (_clientHost == null || _clientHost.GetLocalConnectionState() != LocalConnectionState.Started) return;
            _clientHostIncoming.Enqueue(packet);
        }

        /// <summary>Called when Client Host Connection state changes.</summary>
        internal void HandleClientHostConnectionStateChange(LocalConnectionState state, bool server)
        {
            switch (state)
            {
                case LocalConnectionState.Started:
                    Transport.HandleRemoteConnectionState(new RemoteConnectionStateArgs(RemoteConnectionState.Started,
                        FishyEOS.CLIENT_HOST_ID, Transport.Index));
                    break;
                case LocalConnectionState.Stopped:
                    Transport.HandleRemoteConnectionState(new RemoteConnectionStateArgs(RemoteConnectionState.Stopped,
                        FishyEOS.CLIENT_HOST_ID, Transport.Index));
                    break;
                case LocalConnectionState.Starting:
                case LocalConnectionState.Stopping:
                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state, null);
            }
        }

        /// <summary>Gets the EOS Local Product User Id of the server.</summary>
        internal string GetConnectionAddress(int connectionId)
        {
            var client = _clients.FirstOrDefault(x => x.Id == connectionId);
            return client.LocalUserId.ToString();
        }
    }
}