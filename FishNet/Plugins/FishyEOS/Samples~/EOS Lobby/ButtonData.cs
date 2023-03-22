using System;
using UnityEngine.UI;

namespace EOSLobby
{
    [Serializable]
    public class ButtonData
    {
        public Button Button { get; set; }
        public object CustomData { get; set; }
    }
}