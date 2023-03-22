using System.Collections.Generic;
using Epic.OnlineServices;
using Epic.OnlineServices.Lobby;
using FishNet.Plugins.FishyEOS.Util;
using TMPro;
using UnityEngine;

namespace EOSLobby
{
    [DefaultExecutionOrder(-10)]
    public class LobbyVariables : MonoBehaviour
    {
        [Header("Self Player Variables")]
        public Bindable<string> displayName;
        public string productUserId;

        [Header("Lobby Variables")]
        public Bindable<string> hostLobbyName;
        public uint maxLobbyUsers = 5;
        public string bucketId = "MyBucket";
        public AuthData editorAuthData;
        public AuthData buildAuthData;
        public LobbyData currentLobby;
        public float pollLobbiesInterval = 5f;
        public List<string> lobbyChatMessages = new List<string>();
        public LobbyDetails[] searchResults;

        [Header("Lobby References")]
        public GameObject lobbyBrowserUI;
        public GameObject lobbyRoomUI;
        public GameObject lobbyGameUI;
        public GameObject lobbyGame;
        public LobbyPopup lobbyPopupUI;
        public Transform lobbyListContent;
        public Transform userListContent;
        public GameObject hostStartGameButton;
        public GameObject selfReadyToggle;
        public TMP_InputField chatInputField;
        public TMP_Text chatText;

        public static LobbyVariables Instance;
        
        private ProductUserId _productUserId;

        public ProductUserId ProductUserId
        {
            get => _productUserId;
            set { _productUserId = value; productUserId = value.ToString(); }
        }

#if UNITY_EDITOR && PARREL_SYNC
        public AuthData AuthData => ClonesManager.Instance.IsClone ? buildAuthData : editorAuthData;
#elif UNITY_EDITOR
        public AuthData AuthData => editorAuthData;
#else
        public AuthData AuthData => buildAuthData;
#endif

        private void Awake()
        {
            Instance = this;
        }

        private void OnEnable()
        {
            LobbyEvents.Instance.InputFieldChanged.AddPersistentListener(OnInputFieldChanged);
        }

        private void OnDisable()
        {
            LobbyEvents.Instance.InputFieldChanged.RemovePersistentListener(OnInputFieldChanged);
        }

        private void OnInputFieldChanged(TMP_InputField inputField)
        {
            switch (inputField.name)
            {
                case "Name InputField (TMP)":
                    displayName.Value = inputField.text;
                    break;
                case "Host InputField (TMP)":
                    hostLobbyName.Value = inputField.text;
                    break;
            }
        }
    }
}