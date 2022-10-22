using System;
using System.Collections;
using System.Collections.Generic;
using FishNet.Utility.Performance;
using UnityEngine;

namespace FishNet.Transporting.FishyEOSPlugin
{
    public class ClientHostPeer : CommonPeer
    {
        #region Private
        /// <summary>
        /// Reference to the Local Server Peer.
        /// </summary>
        private ServerPeer _server;
        
        /// <summary>
        /// Queue of incoming packets for Client to process.
        /// </summary>
        private Queue<LocalPacket> _incoming = new Queue<LocalPacket>();
        #endregion

        /// <summary>
        /// Starts the client connection.
        /// </summary>
        internal bool StartConnection(ServerPeer serverPeer)
        {
            if (serverPeer == null) return false;
            
            _server = serverPeer;
            _server.SetClientHostPeer(this);
            
            if (GetLocalConnectionState() != LocalConnectionState.Stopped) return false;
            if (_server.GetLocalConnectionState() != LocalConnectionState.Started &&
                _server.GetLocalConnectionState() != LocalConnectionState.Starting) return false;

            SetLocalConnectionState(LocalConnectionState.Starting, false);
            _transport.StartCoroutine(StartClientHostOnServerStart());
            return true;
        }

        /// <summary>
        /// Waits for server to be started before finishing client connection.
        /// </summary>
        private IEnumerator StartClientHostOnServerStart()
        {
            var timeout = Time.time + 30f;
            while (Time.time < timeout)
            {
                if (_server.GetLocalConnectionState() == LocalConnectionState.Started)
                {
                    SetLocalConnectionState(LocalConnectionState.Started, false);
                    yield break;
                }
                yield return null;
            }
            SetLocalConnectionState(LocalConnectionState.Stopped, false);
        }

        /// <summary>
        /// Sets a new connection state.
        /// </summary>
        protected override void SetLocalConnectionState(LocalConnectionState connectionState, bool server)
        {
            base.SetLocalConnectionState(connectionState, server);
            if (connectionState == LocalConnectionState.Started || connectionState == LocalConnectionState.Stopped)
                _server.HandleClientHostConnectionStateChange(connectionState, server);
        }

        /// <summary>
        /// Stops the client connection.
        /// </summary>
        internal bool StopConnection()
        {
            if (GetLocalConnectionState() == LocalConnectionState.Stopped ||
                GetLocalConnectionState() == LocalConnectionState.Stopping)
                return false;

            ClearQueue(ref _incoming);
            SetLocalConnectionState(LocalConnectionState.Stopping, false);
            SetLocalConnectionState(LocalConnectionState.Stopped, false);
            _server.SetClientHostPeer(null);
            return true;
        }

        /// <summary>
        /// Iterate on incoming packets.
        /// </summary>
        internal void IterateIncoming()
        {
            if (GetLocalConnectionState() != LocalConnectionState.Started) return;
            
            while (_incoming.Count > 0)
            {
                var packet = _incoming.Dequeue();
                ArraySegment<byte> segment = new ArraySegment<byte>(packet.Data, 0, packet.Length);
                _transport.HandleClientReceivedDataArgs(new ClientReceivedDataArgs(segment, packet.Channel, _transport.Index));
                ByteArrayPool.Store(packet.Data);
            }
        }

        /// <summary>
        /// Called when local server sends local client data.
        /// </summary>
        internal void ReceivedFromLocalServer(LocalPacket packet)
        {
            _incoming.Enqueue(packet);
        }

        /// <summary>
        /// Queues data to be sent to the local server.
        /// </summary>
        internal void SendToServer(byte channelId, ArraySegment<byte> segment)
        {
            if (GetLocalConnectionState() != LocalConnectionState.Started) return;
            if (_server.GetLocalConnectionState() != LocalConnectionState.Started) return;
            var packet = new LocalPacket(segment, channelId);
            _server.ReceivedFromClientHost(packet);
        }
    }
}
