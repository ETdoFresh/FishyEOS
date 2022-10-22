using System;
using Epic.OnlineServices.P2P;
using FishNet.Managing;
using FishNet.Managing.Logging;
using FishNet.Plugins.FishyEOS.Util;
using UnityEngine;

namespace FishNet.Transporting.FishyEOSPlugin
{
    [AddComponentMenu("FishNet/Transport/FishyEOS")]
    public class FishyEOS : Transport
    {
        #region Serialized.

        /// <summary>
        /// Maximum number of players which may be connected at once.
        /// </summary>
        [Tooltip("Maximum number of players which may be connected at once.")]
        [Range(1, 9999)]
        [SerializeField]
        private int _maximumClients = 4095;

        /// <summary>
        /// Socket ID [Must be the same on all clients and server].
        /// </summary>
        [Header("EOS")]
        [Tooltip("Socket ID [Must be the same on all clients and server].")]
        [SerializeField] private string socketName = "FishyEOS";

        /// <summary>
        /// Server Product User ID. Must be set for remote clients.
        /// </summary>
        [Tooltip("Server Product User ID. Must be set for remote clients.")]
        [SerializeField] private string remoteServerProductUserId;

        /// <summary>
        /// Automatically Authenticate/Login to EOS Connect when starting server or client.
        /// </summary>
        [Tooltip("Automatically Authenticate/Login to EOS Connect when starting server or client.")]
        [SerializeField] private bool autoAuthenticate = true;
        
        /// <summary>
        /// Authentication Data for EOS Connect.
        /// </summary>
        [Tooltip("Auth Connect Data. Must be unique for all clients and server. [Host only needs 1 unique value.]")]
        [SerializeField] private AuthData authConnectData = new AuthData();

        #endregion

        #region Private.

        /// <summary>
        /// Server peer and handler
        /// </summary>
        private ServerPeer _server = new ServerPeer();

        /// <summary>
        /// Client peer and handler
        /// </summary>
        private ClientPeer _client = new ClientPeer();

        /// <summary>
        /// Client Host peer and handler
        /// </summary>
        private ClientHostPeer _clientHost = new ClientHostPeer();

        #endregion

        #region Constants.

        /// <summary>
        /// Id to use for client when acting as host.
        /// </summary>
        internal const int CLIENT_HOST_ID = short.MaxValue;

        #endregion

        #region Properties.

        /// <summary>
        /// Automatically Authenticate/Login to EOS Connect when starting server or client.
        /// </summary>
        public bool AutoAuthenticate => autoAuthenticate;
        
        /// <summary>
        /// Authentication Data for EOS Connect
        /// </summary>
        public AuthData AuthConnectData => authConnectData;

        /// <summary>
        /// Name of EOS Socket. Must be the same on all clients and server.
        /// </summary>
        public string SocketName { get => socketName; set => socketName = value; }

        /// <summary>
        /// Product User Id of Local EOS Connection
        /// </summary>
        public string LocalProductUserId =>
            EOS.GetPlatformInterface().GetConnectInterface().GetLoggedInUserByIndex(0).ToString();

        /// <summary>
        /// Product User Id of Remote Server EOS Connection
        /// </summary>
        public string RemoteProductUserId
        {
            get => remoteServerProductUserId;
            set => remoteServerProductUserId = value;
        }

        #endregion

        #region Initialization and unity.

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

        #endregion

        #region ConnectionStates.

        // -----------------------------------
        /// <summary>
        /// Gets the EOS Connect Peer Id of a remote connection Id.
        /// </summary>
        /// <param name="connectionId"></param>
        /// <returns></returns>
        public override string GetConnectionAddress(int connectionId)
        {
            return _server.GetConnectionAddress(connectionId);
        }

        /// <summary>
        /// Called when a connection state changes for the local client.
        /// </summary>
        public override event Action<ClientConnectionStateArgs> OnClientConnectionState;

        /// <summary>
        /// Called when a connection state changes for the local server.
        /// </summary>
        public override event Action<ServerConnectionStateArgs> OnServerConnectionState;

        /// <summary>
        /// Called when a connection state changes for a remote client.
        /// </summary>
        public override event Action<RemoteConnectionStateArgs> OnRemoteConnectionState;

        /// <summary>
        /// Gets the current local ConnectionState.
        /// </summary>
        /// <param name="server">True if getting ConnectionState for the server.</param>
        public override LocalConnectionState GetConnectionState(bool server)
        {
            if (server)
                return _server.GetLocalConnectionState();
            else
                return _client.GetLocalConnectionState();
        }

        /// <summary>
        /// Gets the current ConnectionState of a remote client on the server.
        /// </summary>
        /// <param name="connectionId">ConnectionId to get ConnectionState for.</param>
        public override RemoteConnectionState GetConnectionState(int connectionId)
        {
            return _server.GetConnectionState(connectionId);
        }

        /// <summary>
        /// Handles a ConnectionStateArgs for the local client.
        /// </summary>
        /// <param name="connectionStateArgs"></param>
        public override void HandleClientConnectionState(ClientConnectionStateArgs connectionStateArgs)
        {
            OnClientConnectionState?.Invoke(connectionStateArgs);
        }

        /// <summary>
        /// Handles a ConnectionStateArgs for the local server.
        /// </summary>
        /// <param name="connectionStateArgs"></param>
        public override void HandleServerConnectionState(ServerConnectionStateArgs connectionStateArgs)
        {
            OnServerConnectionState?.Invoke(connectionStateArgs);
        }

        /// <summary>
        /// Handles a ConnectionStateArgs for a remote client.
        /// </summary>
        /// <param name="connectionStateArgs"></param>
        public override void HandleRemoteConnectionState(RemoteConnectionStateArgs connectionStateArgs)
        {
            OnRemoteConnectionState?.Invoke(connectionStateArgs);
        }

        #endregion

        #region Iterating.

        /// <summary>
        /// Processes data received by the socket.
        /// </summary>
        /// <param name="server">True to process data received on the server.</param>
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

        /// <summary>
        /// Processes data to be sent by the socket.
        /// </summary>
        /// <param name="server">True to process data received on the server.</param>
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

        #endregion

        #region ReceivedData.

        /// <summary>
        /// Called when client receives data.
        /// </summary>
        public override event Action<ClientReceivedDataArgs> OnClientReceivedData;

        /// <summary>
        /// Handles a ClientReceivedDataArgs.
        /// </summary>
        /// <param name="receivedDataArgs"></param>
        public override void HandleClientReceivedDataArgs(ClientReceivedDataArgs receivedDataArgs)
        {
            OnClientReceivedData?.Invoke(receivedDataArgs);
        }

        /// <summary>Called when server receives data.</summary>
        public override event Action<ServerReceivedDataArgs> OnServerReceivedData;

        /// <summary>
        /// Handles a ClientReceivedDataArgs.
        /// </summary>
        /// <param name="receivedDataArgs"></param>
        public override void HandleServerReceivedDataArgs(ServerReceivedDataArgs receivedDataArgs)
        {
            OnServerReceivedData?.Invoke(receivedDataArgs);
        }

        #endregion

        #region SendingData.

        /// <summary>
        /// Sends to the server or all clients.
        /// </summary>
        /// <param name="channelId">Channel to use.</param>
        /// <param name="segment">Data to send.</param>
        public override void SendToServer(byte channelId, ArraySegment<byte> segment)
        {
            _client.SendToServer(channelId, segment);
            _clientHost.SendToServer(channelId, segment);
        }

        /// <summary>
        /// Sends data to a client.
        /// </summary>
        /// <param name="channelId"></param>
        /// <param name="segment"></param>
        /// <param name="connectionId"></param>
        public override void SendToClient(byte channelId, ArraySegment<byte> segment, int connectionId)
        {
            _server.SendToClient(channelId, segment, connectionId);
        }

        #endregion

        #region Configuration.

        public override bool IsLocalTransport(int connectionId)
        {
            return true;
        }

        /// <summary>
        /// Returns the maximum number of clients allowed to connect to the server.
        /// If the transport does not support this method the value -1 is returned.
        /// </summary>
        /// <returns></returns>
        public override int GetMaximumClients()
        {
            return _server.GetMaximumClients();
        }

        /// <summary>
        /// Sets maximum number of clients allowed to connect to the server.
        /// If applied at runtime and clients exceed this value existing clients will stay connected but new clients may not connect.
        /// </summary>
        /// <param name="value"></param>
        public override void SetMaximumClients(int value)
        {
            _server.SetMaximumClients(value);
        }

        /// <summary>
        /// EOS Not Used
        /// </summary>
        public override void SetClientAddress(string address)
        {
            _ = address;
        }

        /// <summary>
        /// EOS Not Used
        /// </summary>
        public override void SetServerBindAddress(string address, IPAddressType addressType)
        {
            _ = address;
        }

        /// <summary>
        /// EOS Not Used
        /// </summary>
        public override void SetPort(ushort port)
        {
            _ = port;
        }

        #endregion

        #region Start and stop.

        /// <summary>
        /// Starts the local server or client using configured settings.
        /// </summary>
        /// <param name="server">True to start server.</param>
        public override bool StartConnection(bool server)
        {
            if (server)
                return StartServer();
            else
                return StartClient();
        }

        /// <summary>
        /// Stops the local server or client.</summary>
        /// <param name="server">True to stop server.</param>
        public override bool StopConnection(bool server)
        {
            if (server)
                return StopServer();
            else
                return StopClient();
        }

        /// <summary>
        /// Stops a remote client from the server, disconnecting the client.
        /// </summary>
        /// <param name="connectionId">ConnectionId of the client to disconnect.</param>
        /// <param name="immediately">True to abrutly stop the client socket. The technique used to accomplish immediate disconnects may vary depending on the transport.
        /// When not using immediate disconnects it's recommended to perform disconnects using the ServerManager rather than accessing the transport directly.
        /// </param>
        public override bool StopConnection(int connectionId, bool immediately)
        {
            return StopClient(connectionId, immediately);
        }

        /// <summary>
        /// Stops both client and server.
        /// </summary>
        public override void Shutdown()
        {
            //Stops client then server connections.
            StopConnection(false);
            StopConnection(true);
        }

        #region Privates.

        /// <summary>
        /// Starts server.
        /// </summary>
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

        /// <summary>
        /// Stops server.
        /// </summary>
        private bool StopServer()
        {
            if (_server != null)
                return _server.StopConnection();

            return false;
        }

        /// <summary>
        /// Starts the client.
        /// </summary>
        /// <param name="address"></param>
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

        /// <summary>
        /// Stops the client.
        /// </summary>
        private bool StopClient()
        {
            bool result = false;
            if (_client != null)
                result |= _client.StopConnection();
            if (_clientHost != null)
                result |= _clientHost.StopConnection();
            return result;
        }

        /// <summary>
        /// Stops the client.
        /// </summary>
        private bool StopClient(int connectionId, bool immediately)
        {
            return _server.StopConnection(connectionId);
        }

        #endregion

        #endregion

        #region Channels.

        /// <summary>
        /// Gets the MTU for a channel. This should take header size into consideration.
        /// For example, if MTU is 1200 and a packet header for this channel is 10 in size, this method should return 1190.
        /// </summary>
        /// <param name="channel"></param>
        /// <returns></returns>
        public override int GetMTU(byte channel)
        {
            return P2PInterface.MaxPacketSize;
        }

        #endregion
    }
}