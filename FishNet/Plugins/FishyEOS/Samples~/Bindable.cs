using System;
using UnityEngine;

[Serializable]
public class Bindable<T>
{
    public event Action<T> OnValueChanged;
    [SerializeField]
    private T value;
    public T Value { get => value; set => SetValue(value); }

    public Bindable(T value)
    {
        this.value = value;
    }

    private void SetValue(T newValue)
    {
        if (newValue.Equals(value)) return;
        value = newValue;
        OnValueChanged?.Invoke(newValue);
    }
    
    public static implicit operator T(Bindable<T> bindable) => bindable.Value;
}

#if UNITY_EDITOR
[UnityEditor.CustomPropertyDrawer(typeof(Bindable<>), true)]
public class BindableTPropertyDrawer : UnityEditor.PropertyDrawer
{
    public override void OnGUI(Rect position, UnityEditor.SerializedProperty property, GUIContent label)
    {
        var valueProperty = property.FindPropertyRelative("value");
        switch (valueProperty.propertyType)
        {
            case UnityEditor.SerializedPropertyType.Boolean:
                valueProperty.boolValue = UnityEditor.EditorGUI.Toggle(position, label, valueProperty.boolValue);
                break;
            case UnityEditor.SerializedPropertyType.Integer:
                valueProperty.intValue = UnityEditor.EditorGUI.IntField(position, label, valueProperty.intValue);
                break;
            case UnityEditor.SerializedPropertyType.Float:
                valueProperty.floatValue = UnityEditor.EditorGUI.FloatField(position, label, valueProperty.floatValue);
                break;
            case UnityEditor.SerializedPropertyType.String:
                valueProperty.stringValue = UnityEditor.EditorGUI.TextField(position, label, valueProperty.stringValue);
                break;
            case UnityEditor.SerializedPropertyType.ObjectReference:
                valueProperty.objectReferenceValue = UnityEditor.EditorGUI.ObjectField(position, label,
                    valueProperty.objectReferenceValue, valueProperty.objectReferenceValue.GetType(), true);
                break;
            default:
                UnityEditor.EditorGUI.LabelField(position, label, new GUIContent("Not supported"));
                break;
        }
    }
}
#endif