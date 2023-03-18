using System;
using System.Collections.Generic;
using Epic.OnlineServices;
using Epic.OnlineServices.Lobby;

namespace EOSLobby
{
    [Serializable]
    public class LobbyData
    {
        public string lobbyId;
        public string lobbyName;
        public uint maxPlayers;
        public List<LobbyMember> lobbyMembers = new();
        public string[] attributeKeys;
        public string[] attributeValues;

        [Serializable]
        public class LobbyMember
        {
            public string productUserId;
            public string displayName;
            public LobbyMemberStatus status;
            public string[] attributeKeys;
            public string[] attributeValues;

            private ProductUserId _productUserId;

            public ProductUserId ProductUserId
            {
                get => _productUserId;
                set
                {
                    _productUserId = value;
                    productUserId = value.ToString();
                }
            }
        }
    }
}