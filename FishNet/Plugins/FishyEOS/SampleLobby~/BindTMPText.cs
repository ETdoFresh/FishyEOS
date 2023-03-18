using System;
using EOSLobby;
using TMPro;
using UnityEngine;

public class BindTMPText : MonoBehaviour
{
    [SerializeField] private TMP_Text text;
    [SerializeField] private string bindableName;
    [SerializeField] private string suffix;
    private Bindable<string> _bindable;

    private void OnValidate()
    {
        if (!text) text = GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        var bindableField = typeof(LobbyVariables).GetField(bindableName);
        if (bindableField == null) return;
        _bindable = bindableField.GetValue(LobbyVariables.Instance) as Bindable<string>;
        if (_bindable == null) return;
        text.text = _bindable.Value + suffix;
        _bindable.OnValueChanged += OnValueChanged;
    }

    private void OnDisable()
    {
        if (_bindable == null) return;
        _bindable.OnValueChanged -= OnValueChanged;
    }

    private void OnValueChanged(string newValue)
    {
        text.text = newValue + suffix;
    }
}
