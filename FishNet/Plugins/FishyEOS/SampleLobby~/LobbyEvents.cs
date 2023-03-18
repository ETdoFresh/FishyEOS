using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace EOSLobby
{
    [DefaultExecutionOrder(-20)]
    public class LobbyEvents : MonoBehaviour
    {
        [Header("General")]
        public UnityEvent<ButtonData> ButtonClicked;
        public UnityEvent<TMP_InputField> InputFieldChanged;
        public UnityEvent<Toggle> ToggleValueChanged;

        [Header("Lobby Room")]
        public UnityEvent<LobbyUser, bool> LobbyUserReadyChanged;
        public UnityEvent<LobbyUser> LobbyUserJoined;
        public UnityEvent<LobbyUser> LobbyUserLeft;
        public UnityEvent<LobbyUser, string> LobbyUserMessageReceived;

        [Header("Lobby Game")]
        public UnityEvent LeaveGameClicked;

        public static LobbyEvents Instance;

        private void Awake()
        {
            Instance = this;
        }
    }
}