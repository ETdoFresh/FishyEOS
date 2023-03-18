using TMPro;
using UnityEngine;

namespace EOSLobby
{
    public class InvokeValueChangeEvent : MonoBehaviour
    {
        [SerializeField] private TMP_InputField inputField;
        
        private void OnValidate()
        {
            if (!inputField) inputField = GetComponent<TMP_InputField>();
        }
        
        private void OnEnable() => inputField.onValueChanged.AddPersistentListener(InvokeInputFieldChangedEvent);
        
        private void OnDisable() => inputField.onValueChanged.RemovePersistentListener(InvokeInputFieldChangedEvent);
        
        private void InvokeInputFieldChangedEvent(string value) => LobbyEvents.Instance.InputFieldChanged.Invoke(inputField);
    }
}