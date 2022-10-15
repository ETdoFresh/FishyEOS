using System;
using Epic.OnlineServices.P2P;
using FishNet.Managing;
using FishNet.Managing.Logging;
using FishNet.Plugins.FishyEOS.Util;
using PlayEveryWare.EpicOnlineServices;
using UnityEngine;

namespace FishNet.Transporting.FishyEOSPlugin
{
    [AddComponentMenu("FishNet/Transport/FishyEOS")]
    public class FishyEOS : Transport
    {
        [Tooltip("Maximum number of players which may be connected at once.")]
        [Range(1, 9999)]
        [SerializeField]
        private int _maximumClients = 4095;

        [Header("EOS")]
        [Tooltip("Socket ID [Must be the same on all clients and server].")]
        [SerializeField] private string socketName = "FishyEOS";

        [Tooltip("Server Product User ID. Must be set for remote clients.")]
        [SerializeField] private string remoteServerProductUserId;
        
        [Tooltip("Auth Connect Data. Must be unique for all clients and server. [Host only needs 1 unique value.]")]
        [SerializeField] private AuthConnectData _authConnectData = new AuthConnectData();

        private ServerPeer _server = new();
        private ClientPeer _client = new();
        private ClientHostPeer _clientHost = new();

        internal const int CLIENT_HOST_ID = short.MaxValue;

        
        public AuthConnectData AuthConnectData => _authConnectData;
        public string SocketName => socketName;
        public string LocalProductUserId => EOSManager.Instance.GetProductUserId().ToString();

        public string RemoteProductUserId
        {
            get => remoteServerProductUserId;
            set => remoteServerProductUserId = value;
        }

        public override void Initialize(NetworkManager networkManager, int transportIndex)
        {
            base.Initialize(networkManager, transportIndex);
            _client.Initialize(this);
            _clientHost.Initialize(this);
            _server.Initialize(this);
        }

        private void OnDestroy()
        {
            Shutdown();
        }

        // -----------------------------------

        public override string GetConnectionAddress(int connectionId)
        {
            return String.Empty;
        }

        public override event Action<ClientConnectionStateArgs> OnClientConnectionState;
        public override event Action<ServerConnectionStateArgs> OnServerConnectionState;
        public override event Action<RemoteConnectionStateArgs> OnRemoteConnectionState;

        public override LocalConnectionState GetConnectionState(bool server)
        {
            if (server)
                return _server.GetLocalConnectionState();
            else
                return _client.GetLocalConnectionState();
        }

        public override RemoteConnectionState GetConnectionState(int connectionId)
        {
            return _server.GetConnectionState(connectionId);
        }

        public override void HandleClientConnectionState(ClientConnectionStateArgs connectionStateArgs)
        {
            OnClientConnectionState?.Invoke(connectionStateArgs);
        }

        public override void HandleServerConnectionState(ServerConnectionStateArgs connectionStateArgs)
        {
            OnServerConnectionState?.Invoke(connectionStateArgs);
        }

        public override void HandleRemoteConnectionState(RemoteConnectionStateArgs connectionStateArgs)
        {
            OnRemoteConnectionState?.Invoke(connectionStateArgs);
        }

        // -----------------------------------

        public override void IterateIncoming(bool server)
        {
            if (server)
            {
                _server.IterateIncoming();
            }
            else
            {
                _client.IterateIncoming();
                _clientHost.IterateIncoming();
            }
        }

        public override void IterateOutgoing(bool server)
        {
            if (server)
            {
                _server.IterateOutgoing();
            }
            else
            {
                _client.IterateOutgoing();
            }
        }

        // -----------------------------------

        public override event Action<ClientReceivedDataArgs> OnClientReceivedData;

        public override void HandleClientReceivedDataArgs(ClientReceivedDataArgs receivedDataArgs)
        {
            OnClientReceivedData?.Invoke(receivedDataArgs);
        }

        public override event Action<ServerReceivedDataArgs> OnServerReceivedData;

        public override void HandleServerReceivedDataArgs(ServerReceivedDataArgs receivedDataArgs)
        {
            OnServerReceivedData?.Invoke(receivedDataArgs);
        }

        // -----------------------------------

        public override void SendToServer(byte channelId, ArraySegment<byte> segment)
        {
            _client.SendToServer(channelId, segment);
            _clientHost.SendToServer(channelId, segment);
        }

        public override void SendToClient(byte channelId, ArraySegment<byte> segment, int connectionId)
        {
            _server.SendToClient(channelId, segment, connectionId);
        }

        // -----------------------------------

        public override bool IsLocalTransport(int connectionId)
        {
            return true;
        }

        public override int GetMaximumClients()
        {
            return _server.GetMaximumClients();
        }

        public override void SetMaximumClients(int value)
        {
            _server.SetMaximumClients(value);
        }

        public override void SetClientAddress(string address)
        {
            _ = address;
        }

        public override void SetServerBindAddress(string address, IPAddressType addressType)
        {
            _ = address;
        }

        public override void SetPort(ushort port)
        {
            _ = port;
        }

        // -----------------------------------

        public override bool StartConnection(bool server)
        {
            if (server)
                return StartServer();
            else
                return StartClient();
        }

        public override bool StopConnection(bool server)
        {
            if (server)
                return StopServer();
            else
                return StopClient();
        }

        public override bool StopConnection(int connectionId, bool immediately)
        {
            return StopClient(connectionId, immediately);
        }

        public override void Shutdown()
        {
            //Stops client then server connections.
            StopConnection(false);
            StopConnection(true);
        }

        // -----------------------------------

        private bool StartServer()
        {
            if (_server.GetLocalConnectionState() != LocalConnectionState.Stopped)
            {
                if (NetworkManager.CanLog(LoggingType.Error))
                    Debug.LogError("Server is already running.");
                return false;
            }

            var clientRunning = (_client.GetLocalConnectionState() != LocalConnectionState.Stopped);
            /* If remote _client is running then stop it
             * and start the client host variant. */
            if (clientRunning)
                _client.StopConnection();
            
            var result = _server.StartConnection();

            //If need to restart client.
            if (result && clientRunning)
                StartConnection(false);

            return result;
        }

        private bool StopServer()
        {
            if (_server != null)
                return _server.StopConnection();

            return false;
        }

        private bool StartClient()
        {
            //If not acting as a host.
            if (_server.GetLocalConnectionState() == LocalConnectionState.Stopped)
            {
                if (_client.GetLocalConnectionState() != LocalConnectionState.Stopped)
                {
                    if (NetworkManager.CanLog(LoggingType.Error))
                        Debug.LogError("Client is already running.");
                    return false;
                }

                //Stop client host if running.
                if (_clientHost.GetLocalConnectionState() != LocalConnectionState.Stopped)
                    _clientHost.StopConnection();
                
                _client.StartConnection();
            }
            //Acting as host.
            else
            {
                _clientHost.StartConnection(_server);
            }

            return true;
        }

        private bool StopClient()
        {
            bool result = false;
            if (_client != null)
                result |= _client.StopConnection();
            if (_clientHost != null)
                result |= _clientHost.StopConnection();
            return result;
        }

        private bool StopClient(int connectionId, bool immediately)
        {
            return _server.StopConnection(connectionId);
        }

        // -----------------------------------

        public override int GetMTU(byte channel)
        {
            return P2PInterface.MaxPacketSize;
        }
    }
}