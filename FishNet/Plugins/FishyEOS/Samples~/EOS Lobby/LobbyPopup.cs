using System.Collections;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EOSLobby
{
    public class LobbyPopup : MonoBehaviour
    {
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private Button okButton;
        [SerializeField] private Button cancelButton;
        private Coroutine _popupCoroutine;
        
        public void Show(string title, string message)
        {
            Debug.Log($"[LobbyPopup] Showing: {title} - {message}"); 
            titleText.text = title;
            messageText.text = message;
            okButton.gameObject.SetActive(false);
            cancelButton.gameObject.SetActive(false);
            gameObject.SetActive(true);
        }
        
        public void Hide()
        {
            Debug.Log("[LobbyPopup] Hiding");
            gameObject.SetActive(false);
        }

        public async Task<bool> PromptAsync(string title, string message, bool showOkButton = true, bool showCancelButton = false)
        {
            okButton.onClick.RemoveAllListeners();
            cancelButton.onClick.RemoveAllListeners();
            titleText.text = title;
            messageText.text = message;
            okButton.gameObject.SetActive(showOkButton);
            cancelButton.gameObject.SetActive(showCancelButton);
            gameObject.SetActive(true);
            var tcs = new TaskCompletionSource<bool>();
            okButton.onClick.AddListener(() => tcs.SetResult(true));
            cancelButton.onClick.AddListener(() => tcs.SetResult(false));
            var result = await tcs.Task;
            gameObject.SetActive(false);
            okButton.onClick.RemoveAllListeners();
            cancelButton.onClick.RemoveAllListeners();
            return result;
        }

        public class PromptResult
        {
            public bool? Value { get; set; }
        }
        
        public Coroutine PromptCoroutine(out PromptResult promptResult, string title, string message, bool showOkButton = true, bool showCancelButton = false)
        {
            gameObject.SetActive(true);
            promptResult = new PromptResult();
            if (_popupCoroutine != null) StopCoroutine(_popupCoroutine);
            return _popupCoroutine = StartCoroutine(PromptCoroutineRoutine(promptResult, title, message, showOkButton, showCancelButton));
        }
        
        private IEnumerator PromptCoroutineRoutine(PromptResult promptResult, string title, string message, bool showOkButton, bool showCancelButton)
        {
            okButton.onClick.RemoveAllListeners();
            cancelButton.onClick.RemoveAllListeners();
            titleText.text = title;
            messageText.text = message;
            okButton.gameObject.SetActive(showOkButton);
            cancelButton.gameObject.SetActive(showCancelButton);
            okButton.onClick.AddListener(() => promptResult.Value = true);
            cancelButton.onClick.AddListener(() => promptResult.Value = false);
            yield return new WaitUntil(() => promptResult.Value.HasValue);
            okButton.onClick.RemoveAllListeners();
            cancelButton.onClick.RemoveAllListeners();
            _popupCoroutine = null;
            gameObject.SetActive(false);
        }
    }
}