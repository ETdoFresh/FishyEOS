using UnityEngine;
using UnityEngine.UI;

namespace EOSLobby
{
    public class InvokeButtonClickEvent : MonoBehaviour
    {
        [SerializeField] private Button button;

        public object CustomData { get; set; }

        private void OnValidate()
        {
            if (!button) button = GetComponent<Button>();
        }

        private void OnEnable() => button.onClick.AddPersistentListener(InvokeButtonClickedEvent);

        private void OnDisable() => button.onClick.RemovePersistentListener(InvokeButtonClickedEvent);

        private void InvokeButtonClickedEvent() =>
            LobbyEvents.Instance.ButtonClicked.Invoke(new ButtonData { Button = button, CustomData = CustomData });
    }
}