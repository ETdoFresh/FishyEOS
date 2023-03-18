using System;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using EOSLobby.EOSCoroutines;
using Epic.OnlineServices;
using Epic.OnlineServices.Auth;
using Epic.OnlineServices.Lobby;
using FishNet;
using FishNet.Transporting.FishyEOSPlugin;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

// https://dev.epicgames.com/docs/game-services/lobbies
// https://dev.epicgames.com/docs/api-ref/interfaces/lobby

namespace EOSLobby
{
    public class LobbyCode : MonoBehaviour
    {
        private Coroutine _pollCoroutine;

        // TODO: Add leave game button implementation.
        // TODO: Fix bug LobbyNotifyHandles already contains callback for LobbyCode (EOSLobby.LobbyCode) [leaving host and hosting again]
        // TODO: Fix occasional crashes (I think due to async calls or releasing handles too early).

        private void OnEnable()
        {
            LobbyEvents.Instance.ButtonClicked.AddPersistentListener(OnButtonClicked);
            LobbyEvents.Instance.ToggleValueChanged.AddPersistentListener(OnToggleValueChanged);
            DisableUIListsFirstElements();
            StartPollingLobbies();
        }

        private void OnDisable()
        {
            LobbyEvents.Instance.ButtonClicked.RemovePersistentListener(OnButtonClicked);
            LobbyEvents.Instance.ToggleValueChanged.RemovePersistentListener(OnToggleValueChanged);
            StopPollingLobbies();
        }

        private void OnButtonClicked(ButtonData buttonData)
        {
            switch (buttonData.Button.name)
            {
                case "Host Button":
                    OnHostLobbyClicked();
                    break;
                case "Join Button":
                    OnJoinLobbyClicked(buttonData);
                    break;
                case "Leave Room Button":
                    OnLeaveLobbyClicked();
                    break;
                case "Start Button":
                    OnStartGameClicked();
                    break;
                case "ChatSend Button":
                    OnChatSendClicked();
                    break;
                case "Leave Game Button":
                    OnLeaveGameClicked();
                    break;
            }
        }

        private void OnToggleValueChanged(Toggle toggle)
        {
            switch (toggle.name)
            {
                case "Ready Toggle":
                    OnReadyToggleValueChanged(toggle);
                    break;
            }
        }

        private IEnumerator PollLobbiesRoutine()
        {
            yield return LocalUser.Get(out var localUser);
            while (enabled)
            {
                yield return LobbySearchLobbies.Run(out var searchLobbies, localUser.Id);
                PopulateLobbiesList(searchLobbies.LobbyDetailsArray);
                yield return new WaitForSeconds(LobbyVariables.Instance.pollLobbiesInterval);
            }
        }

        private void OnHostLobbyClicked() => StartCoroutine(OnHobbyLobbyClickedRoutine());

        private IEnumerator OnHobbyLobbyClickedRoutine()
        {
            StopPollingLobbies();
            if (string.IsNullOrEmpty(LobbyVariables.Instance.displayName))
                LobbyVariables.Instance.displayName.Value = $"Player{Random.Range(0, 1000):000}";
            if (string.IsNullOrEmpty(LobbyVariables.Instance.hostLobbyName))
                LobbyVariables.Instance.hostLobbyName.Value = $"Lobby{Random.Range(0, 1000):000}";

            LobbyVariables.Instance.AuthData.displayName = LobbyVariables.Instance.displayName;
            var lobbyName = LobbyVariables.Instance.hostLobbyName;
            var maxLobbyUsers = LobbyVariables.Instance.maxLobbyUsers;
            var bucketId = LobbyVariables.Instance.bucketId;

            LobbyVariables.Instance.lobbyPopupUI.Show("Hosting Lobby...", "Logging in...");
            yield return LocalUser.Get(out var localUser);
            var localUserId = localUser.Id;

            LobbyVariables.Instance.lobbyPopupUI.Show("Hosting Lobby...", "Creating Lobby...");
            yield return LobbyCreateLobby.Run(out var createLobby, localUserId, maxLobbyUsers, bucketId);
            if (createLobby.CallbackInfo?.ResultCode != Result.Success)
            {
                yield return LobbyVariables.Instance.lobbyPopupUI.PromptCoroutine(out _, "Error",
                    createLobby.CallbackInfo?.ResultCode.ToString());
                StartPollingLobbies();
                yield break;
            }

            var lobbyId = createLobby.CallbackInfo?.LobbyId;
            LobbyVariables.Instance.lobbyPopupUI.Show("Hosting Lobby...", "Setting Lobby Name...");
            yield return LobbyUpdateLobby.Run(out var updateLobby, lobbyId, "NAME", lobbyName.Value);
            if (updateLobby.CallbackInfo?.ResultCode != Result.Success)
                Debug.LogWarning($"[LobbyCode] Failed to update lobby name: {updateLobby.CallbackInfo?.ResultCode}");

            var result = Lobby.GetLobbyDetails(out var lobbyDetails, lobbyId, localUserId);
            if (result != Result.Success)
            {
                Debug.LogWarning($"[LobbyCode] Failed to get lobby details: {result}");
            }

            var currentLobby = new LobbyData
            {
                LobbyDetails = lobbyDetails, lobbyId = lobbyId, lobbyName = lobbyName, maxPlayers = maxLobbyUsers
            };
            LobbyVariables.Instance.currentLobby = currentLobby;

            LobbyVariables.Instance.lobbyPopupUI.Show("Hosting Lobby...", "Setting Host Display Name...");
            yield return LobbySetMemberAttribute.Run(out var setName, lobbyId, localUserId, "NAME",
                LobbyVariables.Instance.displayName);
            if (setName.CallbackInfo?.ResultCode != Result.Success)
                Debug.LogWarning($"[LobbyCode] Failed to update lobby member name: {setName.CallbackInfo?.ResultCode}");

            LobbyVariables.Instance.lobbyPopupUI.Show("Hosting Lobby...", "Setting Host Ready...");
            yield return LobbySetMemberAttribute.Run(out var setReady, lobbyId, localUserId, "READY",
                "Ready");
            if (setReady.CallbackInfo?.ResultCode != Result.Success)
                Debug.LogWarning($"[LobbyCode] Failed to set lobby member ready: {setReady.CallbackInfo?.ResultCode}");

            LobbyVariables.Instance.lobbyPopupUI.Show("Hosting Lobby...", "Setting Host Id...");
            yield return LobbyUpdateLobby.Run(out var setId, lobbyId, "HOST_ID",
                localUserId.ToString());
            if (setId.CallbackInfo?.ResultCode != Result.Success)
                Debug.LogWarning($"[LobbyCode] Failed to set lobby member host id: {setId.CallbackInfo?.ResultCode}");

            LobbyVariables.Instance.lobbyPopupUI.Hide();

            var attributes = LobbyDetailsEOS.GetAttributes(lobbyDetails);
            currentLobby.attributeKeys = attributes.Select(x => x?.Data?.Key).Select(x => (string)x).ToArray();
            currentLobby.attributeValues =
                attributes.Select(x => x?.Data?.Value.AsUtf8).Select(x => (string)x).ToArray();

            LobbyVariables.Instance.hostStartGameButton.SetActive(true);
            LobbyVariables.Instance.selfReadyToggle.gameObject.SetActive(false);
            LobbyVariables.Instance.lobbyBrowserUI.SetActive(false);
            LobbyVariables.Instance.lobbyRoomUI.SetActive(true);
            PopulateUserList();
            LobbyNotify.AddNotifyLobbyMemberStatusReceived(OnLobbyMemberStatusReceived);
            LobbyNotify.AddNotifyLobbyMemberUpdateReceived(OnLobbyMemberUpdateReceived);
        }

        private void OnJoinLobbyClicked(ButtonData buttonData) => StartCoroutine(OnJoinLobbyClickedRoutine(buttonData));
        
        private IEnumerator OnJoinLobbyClickedRoutine(ButtonData buttonData)
        {
            StopPollingLobbies();
            if (string.IsNullOrEmpty(LobbyVariables.Instance.displayName))
                LobbyVariables.Instance.displayName.Value = $"Player{Random.Range(0, 1000):000}";

            yield return LocalUser.Get(out var localUser);
            var localUserId = localUser.Id;
            var lobbyDetails = (LobbyDetails)buttonData.CustomData;

            LobbyNotify.AddNotifyLobbyMemberStatusReceived(OnLobbyMemberStatusReceived);
            LobbyNotify.AddNotifyLobbyMemberUpdateReceived(OnLobbyMemberUpdateReceived);
            LobbyNotify.AddNotifyLobbyUpdateReceived(OnLobbyUpdateReceived);

            LobbyVariables.Instance.lobbyPopupUI.Show("Joining Lobby...", "Please wait...");
            yield return LobbyJoinLobby.Run(out var joinLobby, localUserId, lobbyDetails);
            if (joinLobby.CallbackInfo?.ResultCode != Result.Success)
            {
                yield return LobbyVariables.Instance.lobbyPopupUI.PromptCoroutine(out _, "Error",
                    joinLobby.CallbackInfo?.ResultCode.ToString());
                StartPollingLobbies();
                yield break;
            }

            LobbyVariables.Instance.lobbyPopupUI.Show("Joining Lobby...", "Getting Lobby Info...");
            var getLobbyInfoResult = LobbyDetailsEOS.GetLobbyInfo(lobbyDetails, out var lobbyInfo);
            if (getLobbyInfoResult != Result.Success)
                Debug.LogWarning($"[LobbyCode] Failed to get lobby info: {getLobbyInfoResult}");
            var lobbyId = lobbyInfo?.LobbyId;

            LobbyVariables.Instance.lobbyPopupUI.Show("Joining Lobby...", "Getting Lobby Name...");
            var getAttributeResult = LobbyDetailsEOS.GetAttribute(lobbyDetails, "NAME", out var lobbyNameAttribute);
            if (getAttributeResult != Result.Success)
                Debug.LogWarning($"[LobbyCode] Failed to get lobby name: {getAttributeResult}");
            LobbyVariables.Instance.hostLobbyName.Value = lobbyNameAttribute?.Data?.Value.AsUtf8;
            var currentLobby = new LobbyData
            {
                LobbyDetails = lobbyDetails, lobbyId = lobbyId, lobbyName = lobbyNameAttribute?.Data?.Value.AsUtf8,
            };
            LobbyVariables.Instance.currentLobby = currentLobby;

            LobbyVariables.Instance.lobbyPopupUI.Show("Joining Lobby...", "Setting Local User Display Name...");
            yield return LobbySetMemberAttribute.Run(out var setName, lobbyId, localUserId, "NAME",
                LobbyVariables.Instance.displayName);
            if (setName.CallbackInfo?.ResultCode != Result.Success)
                Debug.LogWarning($"[LobbyCode] Failed to update lobby member name: {setName.CallbackInfo?.ResultCode}");

            LobbyVariables.Instance.lobbyPopupUI.Show("Joining Lobby...", "Getting Attributes...");
            var attributes = LobbyDetailsEOS.GetAttributes(lobbyDetails);
            currentLobby.attributeKeys = attributes.Select(x => x?.Data?.Key).Select(x => (string)x).ToArray();
            currentLobby.attributeValues =
                attributes.Select(x => x?.Data?.Value.AsUtf8).Select(x => (string)x).ToArray();

            var gameStartedIndex = Array.IndexOf(currentLobby.attributeKeys, "GAME");
            if (gameStartedIndex != -1 && currentLobby.attributeValues[gameStartedIndex] == "Started")
            {
                yield return LobbyVariables.Instance.lobbyPopupUI.PromptCoroutine(out _, "Error", "Game has already started.");
                StartPollingLobbies();
                yield break;
            }

            LobbyVariables.Instance.lobbyPopupUI.Hide();

            LobbyVariables.Instance.hostStartGameButton.SetActive(false);
            LobbyVariables.Instance.selfReadyToggle.gameObject.SetActive(true);
            LobbyVariables.Instance.lobbyBrowserUI.SetActive(false);
            LobbyVariables.Instance.lobbyRoomUI.SetActive(true);
        }

        private void DisableUIListsFirstElements()
        {
            LobbyVariables.Instance.lobbyListContent.GetChild(0).gameObject.SetActive(false);
            LobbyVariables.Instance.userListContent.GetChild(0).gameObject.SetActive(false);
        }

        private void StartPollingLobbies()
        {
            _pollCoroutine = StartCoroutine(PollLobbiesRoutine());
        }

        private void StopPollingLobbies()
        {
            if (_pollCoroutine != null) StopCoroutine(_pollCoroutine);
        }

        private void PopulateLobbiesList(LobbyDetails[] lobbyDetails)
        {
            var lobbies = LobbyVariables.Instance.lobbyListContent;
            for (var i = lobbies.childCount - 1; i > 0; i--)
                Destroy(lobbies.GetChild(i).gameObject);

            if (lobbyDetails == null) return;
            var lobbyPrefab = lobbies.GetChild(0).gameObject;
            foreach (var lobbyDetail in lobbyDetails)
            {
                var getAttributeResult = Lobby.GetAttribute(lobbyDetail, "NAME", out var lobbyNameAttribute);
                if (getAttributeResult != Result.Success) continue;
                var lobbyInstance = Instantiate(lobbyPrefab, lobbies);
                var lobbyText = lobbyInstance.GetComponentInChildren<TMP_Text>();
                lobbyText.text = lobbyNameAttribute?.Data?.Value.AsUtf8;
                var lobbyButton = lobbyInstance.GetComponentInChildren<InvokeButtonClickEvent>();
                lobbyButton.CustomData = lobbyDetail;
                lobbyInstance.SetActive(true);
            }
        }

        private void PopulateUserList(ProductUserId targetUserId = null)
        {
            var users = LobbyVariables.Instance.userListContent;
            for (var i = users.childCount - 1; i > 0; i--)
                Destroy(users.GetChild(i).gameObject);

            var lobby = LobbyVariables.Instance.currentLobby;
            if (lobby == null) return;

            var lobbyId = lobby.lobbyId;
            var lobbyMembers = lobby.lobbyMembers;
            var localUserId = LobbyVariables.Instance.ProductUserId;
            if (lobby.LobbyDetails != null) lobby.LobbyDetails.Release();
            LobbyEOS.GetLobbyDetails(lobbyId, localUserId, out var lobbyDetails);
            lobby.LobbyDetails = lobbyDetails;
            lobbyMembers.Clear();
            foreach (var productUserId in LobbyDetailsEOS.GetMembers(lobbyDetails))
            {
                var getMemberAttributeResult =
                    LobbyDetailsEOS.GetMemberAttribute(lobbyDetails, productUserId, "NAME", out var memberName);
                if (getMemberAttributeResult != Result.Success)
                    Debug.LogWarning(
                        $"[LobbyCode] Failed to get member name. {getMemberAttributeResult} - {productUserId}");
                var allAttributes = LobbyDetailsEOS.GetMemberAttributes(lobbyDetails, productUserId);
                lobbyMembers.Add(new LobbyData.LobbyMember
                {
                    displayName = memberName?.Data?.Value.AsUtf8,
                    ProductUserId = productUserId,
                    attributeKeys = allAttributes.Select(x => x?.Data?.Key).Select(x => (string)x).ToArray(),
                    attributeValues = allAttributes.Select(x => x?.Data?.Value.AsUtf8).Select(x => (string)x).ToArray()
                });
            }

            var userPrefab = users.GetChild(0).gameObject;
            foreach (var lobbyMember in lobbyMembers)
            {
                if (lobbyMember.ProductUserId == localUserId) continue;
                var userInstance = Instantiate(userPrefab, users);
                var tmpTexts = userInstance.GetComponentsInChildren<TMP_Text>();
                var userText = tmpTexts[0];
                userText.text = lobbyMember.displayName;
                var readyText = tmpTexts[1];
                var index = Array.IndexOf(lobbyMember.attributeKeys, "READY");
                var readyString = index == -1 ? "Not Ready" : lobbyMember.attributeValues[index];
                readyString = readyString == "Ready" ? "<color=green>Ready</color>" : "<color=red>Not Ready</color>";
                readyText.text = readyString;
                userInstance.SetActive(true);
            }

            var isEveryoneReady = lobbyMembers
                .All(member => member.attributeValues.Contains("Ready") &&
                               member.attributeValues[Array.IndexOf(member.attributeKeys, "READY")] == "Ready");

            var chatMember = lobbyMembers.FirstOrDefault(x => x.ProductUserId == targetUserId);
            if (chatMember != null)
            {
                var chatMessageIndex = Array.IndexOf(chatMember.attributeKeys, "CHAT");
                if (chatMessageIndex >= 0)
                {
                    var chatMessages = LobbyVariables.Instance.lobbyChatMessages;
                    var chatMessage = chatMember.attributeValues[chatMessageIndex];
                    if (!string.IsNullOrEmpty(chatMessage))
                    {
                        var chatMessageText = $"{chatMember.displayName}: {chatMessage}";
                        if (chatMessageText != chatMessages.LastOrDefault())
                            chatMessages.Add(chatMessageText);
                    }

                    LobbyVariables.Instance.chatText.text = string.Join("\n", chatMessages);
                }
            }

            LobbyVariables.Instance.hostStartGameButton.GetComponent<Button>().interactable = isEveryoneReady;
        }

        private void OnLobbyMemberStatusReceived(LobbyMemberStatusReceivedCallbackInfo e)
        {
            PopulateUserList();
        }

        private void OnLobbyMemberUpdateReceived(LobbyMemberUpdateReceivedCallbackInfo e)
        {
            PopulateUserList(e.TargetUserId);
        }

        private void OnLobbyUpdateReceived(LobbyUpdateReceivedCallbackInfo e)
        {
            var localUserId = LobbyVariables.Instance.ProductUserId;
            var currentLobby = LobbyVariables.Instance.currentLobby;
            var result = LobbyEOS.GetLobbyDetails(e.LobbyId, localUserId, out var lobbyDetails);
            if (result != Result.Success)
            {
                Debug.LogWarning($"[LobbyCode] Failed to get lobby details. {result}");
                return;
            }

            currentLobby.LobbyDetails = lobbyDetails;

            var wasGameStarted = currentLobby.attributeKeys.Contains("GAME") &&
                                 currentLobby.attributeValues[Array.IndexOf(currentLobby.attributeKeys, "GAME")] ==
                                 "Started";

            var attributes = LobbyDetailsEOS.GetAttributes(lobbyDetails);
            currentLobby.attributeKeys = new string[attributes.Count];
            currentLobby.attributeValues = new string[attributes.Count];
            for (var i = 0; i < attributes.Count; i++)
            {
                currentLobby.attributeKeys[i] = attributes[i]?.Data?.Key;
                currentLobby.attributeValues[i] = attributes[i]?.Data?.Value.AsUtf8;
            }

            var isGameStarted = currentLobby.attributeKeys.Contains("GAME") &&
                                currentLobby.attributeValues[Array.IndexOf(currentLobby.attributeKeys, "GAME")] ==
                                "Started";

            if (!wasGameStarted && isGameStarted)
            {
                var hostIdIndex = Array.IndexOf(currentLobby.attributeKeys, "HOST_ID");
                if (hostIdIndex == -1)
                {
                    Debug.LogWarning("[LobbyCode] Failed to get host id.");
                    return;
                }

                var hostId = currentLobby.attributeValues[hostIdIndex];

                var networkManager = InstanceFinder.NetworkManager;
                var fishyEOS = networkManager.GetComponent<FishyEOS>();
                fishyEOS.RemoteProductUserId = hostId;
                fishyEOS.AuthConnectData.loginCredentialType = LobbyVariables.Instance.AuthData.loginCredentialType;
                fishyEOS.AuthConnectData.externalCredentialType =
                    LobbyVariables.Instance.AuthData.externalCredentialType;
                fishyEOS.AuthConnectData.id = LobbyVariables.Instance.AuthData.id;
                fishyEOS.AuthConnectData.token = LobbyVariables.Instance.AuthData.token;
                fishyEOS.AuthConnectData.displayName =
                    LobbyVariables.Instance.AuthData.loginCredentialType == LoginCredentialType.Developer
                        ? ""
                        : LobbyVariables.Instance.AuthData.displayName;
                fishyEOS.gameObject.SetActive(true);
                networkManager.ClientManager.StartConnection();
                
                LobbyNotify.RemoveNotifyLobbyMemberStatusReceived(OnLobbyMemberStatusReceived);
                LobbyNotify.RemoveNotifyLobbyMemberUpdateReceived(OnLobbyMemberUpdateReceived);
                LobbyNotify.RemoveNotifyLobbyUpdateReceived(OnLobbyUpdateReceived);
                
                LobbyLeaveLobby.Run(out _, currentLobby.lobbyId, localUserId);
                LobbyVariables.Instance.currentLobby = null;

                LobbyVariables.Instance.lobbyRoomUI.SetActive(false);
                LobbyVariables.Instance.lobbyGameUI.SetActive(true);
                LobbyVariables.Instance.lobbyGame.SetActive(true);
            }

            PopulateUserList();
        }

        private class Authenticate
        {
            public ProductUserId LocalUserId { get; set; }

            public static Coroutine Run(out Authenticate authenticate)
            {
                authenticate = new Authenticate();
                return LobbyVariables.Instance.StartCoroutine(authenticate.AuthenticateCoroutine());
            }

            private IEnumerator AuthenticateCoroutine()
            {
                var loginCredentialType = LobbyVariables.Instance.AuthData.loginCredentialType;
                var externalCredentialType = LobbyVariables.Instance.AuthData.externalCredentialType;
                var id = LobbyVariables.Instance.AuthData.id;
                var token = LobbyVariables.Instance.AuthData.token;
                var displayName = LobbyVariables.Instance.AuthData.displayName;
                var automaticallyCreateDeviceId = LobbyVariables.Instance.AuthData.automaticallyCreateDeviceId;
                var automaticallyCreateConnectAccount =
                    LobbyVariables.Instance.AuthData.automaticallyCreateConnectAccount;
                var timeout = (int)LobbyVariables.Instance.AuthData.timeout;
                var scopeFlags = LobbyVariables.Instance.AuthData.authScopeFlags;
                yield return ConnectLogin.Run(loginCredentialType, externalCredentialType, id, token, displayName,
                    automaticallyCreateDeviceId, automaticallyCreateConnectAccount, timeout, scopeFlags,
                    out var login);
                LocalUserId = login.CallbackInfo?.LocalUserId;
            }
        }

        private static async Task<ProductUserId> AuthenticateAsync()
        {
            var loginCredentialType = LobbyVariables.Instance.AuthData.loginCredentialType;
            var externalCredentialType = LobbyVariables.Instance.AuthData.externalCredentialType;
            var id = LobbyVariables.Instance.AuthData.id;
            var token = LobbyVariables.Instance.AuthData.token;
            var displayName = LobbyVariables.Instance.AuthData.displayName;
            var automaticallyCreateDeviceId = LobbyVariables.Instance.AuthData.automaticallyCreateDeviceId;
            var automaticallyCreateConnectAccount =
                LobbyVariables.Instance.AuthData.automaticallyCreateConnectAccount;
            var timeout = (int)LobbyVariables.Instance.AuthData.timeout;
            var scopeFlags = LobbyVariables.Instance.AuthData.authScopeFlags;
            var loginCallbackInfo = await ConnectEOS.LoginAsync(loginCredentialType, externalCredentialType, id,
                token,
                displayName, automaticallyCreateDeviceId, automaticallyCreateConnectAccount, timeout, scopeFlags);
            var localUserId = loginCallbackInfo.LocalUserId;
            return localUserId;
        }

        private void OnLeaveLobbyClicked() => StartCoroutine(OnLeaveLobbyClickedCoroutine());
        
        private IEnumerator OnLeaveLobbyClickedCoroutine()
        {
            var localUserId = LobbyVariables.Instance.ProductUserId;
            var lobbyId = LobbyVariables.Instance.currentLobby.lobbyId;

            LobbyVariables.Instance.lobbyPopupUI.Show("Leaving Lobby...", "Please wait...");
            yield return LobbyLeaveLobby.Run(out var leaveLobby, lobbyId, localUserId);
            LobbyVariables.Instance.lobbyPopupUI.Hide();
            LobbyNotify.RemoveNotifyLobbyMemberStatusReceived(OnLobbyMemberStatusReceived);
            LobbyNotify.RemoveNotifyLobbyMemberUpdateReceived(OnLobbyMemberUpdateReceived);
            LobbyNotify.RemoveNotifyLobbyUpdateReceived(OnLobbyUpdateReceived);

            if (leaveLobby.CallbackInfo?.ResultCode != Result.Success)
            {
                yield return LobbyVariables.Instance.lobbyPopupUI.PromptCoroutine(out _, "Error",
                    leaveLobby.CallbackInfo?.ResultCode.ToString());
            }

            if (LobbyVariables.Instance.currentLobby.LobbyDetails != null)
                LobbyVariables.Instance.currentLobby.LobbyDetails.Release();

            LobbyVariables.Instance.currentLobby = null;
            LobbyVariables.Instance.lobbyRoomUI.SetActive(false);
            LobbyVariables.Instance.lobbyBrowserUI.SetActive(true);
            StartPollingLobbies();
        }

        private async Task<ProductUserId> GetLocalUsedIdAsync()
        {
            return LobbyVariables.Instance.ProductUserId = LobbyVariables.Instance.ProductUserId == null
                ? await AuthenticateAsync()
                : LobbyVariables.Instance.ProductUserId;
        }

        private class LocalUser
        {
            public ProductUserId Id { get; private set; }

            public static Coroutine Get(out LocalUser localUser)
            {
                localUser = new LocalUser();
                return LobbyVariables.Instance.StartCoroutine(localUser.GetCoroutine());
            }

            private IEnumerator GetCoroutine()
            {
                if (LobbyVariables.Instance.ProductUserId != null)
                {
                    Id = LobbyVariables.Instance.ProductUserId;
                    yield break;
                }

                yield return Authenticate.Run(out var authenticate);
                Id = LobbyVariables.Instance.ProductUserId = authenticate.LocalUserId;
            }
        }

        private void OnReadyToggleValueChanged(Toggle toggle) => StartCoroutine(OnReadyToggleValueChangedCoroutine(toggle));
        
        private IEnumerator OnReadyToggleValueChangedCoroutine(Toggle toggle)
        {
            var isReady = toggle.isOn;
            var localUserId = LobbyVariables.Instance.ProductUserId;
            var lobbyId = LobbyVariables.Instance.currentLobby.lobbyId;
            yield return LobbySetMemberAttribute.Run(out var setReady, lobbyId, localUserId, "READY",
                isReady ? "Ready" : "Not Ready");
            if (setReady.CallbackInfo?.ResultCode != Result.Success)
                Debug.LogWarning($"[LobbyCode] Failed to update lobby member name: {setReady.CallbackInfo?.ResultCode}");
            var toggleLabel = toggle.GetComponentInChildren<TextMeshProUGUI>();
            toggleLabel.text = isReady ? "<color=green>Ready</color>" : "<color=red>Not Ready</color>";
        }

        private void OnStartGameClicked() => StartCoroutine(OnStartGameClickedCoroutine()); 
        
        private IEnumerator OnStartGameClickedCoroutine()
        {
            var lobbyId = LobbyVariables.Instance.currentLobby.lobbyId;
            yield return LobbyUpdateLobby.Run(out var updateLobby, lobbyId, "GAME", "Started");
            if (updateLobby.CallbackInfo?.ResultCode != Result.Success)
            {
                yield return LobbyVariables.Instance.lobbyPopupUI.PromptCoroutine(out _, "Error", updateLobby.CallbackInfo?.ResultCode.ToString());
                yield break;
            }

            var networkManager = InstanceFinder.NetworkManager;
            var localUserId = LobbyVariables.Instance.ProductUserId;
            var fishyEOS = networkManager.GetComponent<FishyEOS>();
            fishyEOS.RemoteProductUserId = localUserId.ToString();
            fishyEOS.AuthConnectData.loginCredentialType = LobbyVariables.Instance.AuthData.loginCredentialType;
            fishyEOS.AuthConnectData.externalCredentialType = LobbyVariables.Instance.AuthData.externalCredentialType;
            fishyEOS.AuthConnectData.id = LobbyVariables.Instance.AuthData.id;
            fishyEOS.AuthConnectData.token = LobbyVariables.Instance.AuthData.token;
            fishyEOS.AuthConnectData.displayName =
                LobbyVariables.Instance.AuthData.loginCredentialType == LoginCredentialType.Developer
                    ? ""
                    : LobbyVariables.Instance.AuthData.displayName;
            fishyEOS.gameObject.SetActive(true);
            networkManager.ServerManager.StartConnection();
            networkManager.ClientManager.StartConnection();
            
            yield return LobbyLeaveLobby.Run(out _, lobbyId, localUserId);
            LobbyVariables.Instance.currentLobby = null;

            LobbyVariables.Instance.lobbyRoomUI.SetActive(false);
            LobbyVariables.Instance.lobbyGameUI.SetActive(true);
            LobbyVariables.Instance.lobbyGame.SetActive(true);
        }

        private void OnChatSendClicked() => StartCoroutine(OnChatSendClickedCoroutine());
        
        private IEnumerator OnChatSendClickedCoroutine()
        {
            var chatInput = LobbyVariables.Instance.chatInputField;
            var chatText = chatInput.text;
            if (string.IsNullOrEmpty(chatText)) yield break;
            var localUserId = LobbyVariables.Instance.ProductUserId;
            var lobbyId = LobbyVariables.Instance.currentLobby.lobbyId;
            yield return LobbySetMemberAttribute.Run(out var setChat, lobbyId, localUserId, "CHAT", chatText);
            if (setChat.CallbackInfo?.ResultCode != Result.Success)
            {
                yield return LobbyVariables.Instance.lobbyPopupUI.PromptCoroutine(out _, "Error", setChat.CallbackInfo?.ResultCode.ToString());
                yield break;
            }

            chatInput.text = "";
        }

        private void OnLeaveGameClicked()
        {
            var networkManager = InstanceFinder.NetworkManager;
            networkManager.ServerManager.StopConnection(true);
            networkManager.ClientManager.StopConnection();
            LobbyVariables.Instance.lobbyGameUI.SetActive(false);
            LobbyVariables.Instance.lobbyGame.SetActive(false);
            LobbyVariables.Instance.lobbyBrowserUI.SetActive(true);
            StartPollingLobbies();
        }

#if UNITY_EDITOR
        [UnityEditor.CustomEditor(typeof(LobbyCode))]
        public class Editor : UnityEditor.Editor
        {
            public override void OnInspectorGUI()
            {
                base.OnInspectorGUI();
                if (GUILayout.Button("Populate User List"))
                {
                    var lobbyCode = (LobbyCode)target;
                    lobbyCode.PopulateUserList();
                }
            }
        }
#endif
    }
}