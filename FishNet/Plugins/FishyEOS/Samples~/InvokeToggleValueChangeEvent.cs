using UnityEngine;
using UnityEngine.UI;

namespace EOSLobby
{
    public class InvokeToggleValueChangeEvent : MonoBehaviour
    {
        [SerializeField] private Toggle toggle;

        private void OnValidate()
        {
            if (!toggle) toggle = GetComponent<Toggle>();
        }

        private void OnEnable() => toggle.onValueChanged.AddListener(InvokeToggleValueChangedEvent);

        private void OnDisable() => toggle.onValueChanged.RemoveListener(InvokeToggleValueChangedEvent);

        private void InvokeToggleValueChangedEvent(bool value) =>
            LobbyEvents.Instance.ToggleValueChanged.Invoke(toggle);
    }
}