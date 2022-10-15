using System.Collections;
using Epic.OnlineServices;
using Epic.OnlineServices.Auth;
using PlayEveryWare.EpicOnlineServices;
using UnityEngine;
using LoginCallbackInfo = Epic.OnlineServices.Auth.LoginCallbackInfo;

namespace FishNet.Plugins.FishyEOS.Util
{
    /// <summary>
    /// Start an EOS Auth Login with the passed in LoginOptions.
    /// Not to be confused with EOS Connect Login (which contains desired EOS services).
    /// Return EpicAccountId which can then be used to login to EOS Connect.
    /// In other words, not necessarily required if using another means to connect to EOS Connect.
    /// (like DeviceToken or ExternalAuth)
    ///
    /// Usage:
    ///   var auth = Auth.Login("id", "token", LoginCredentialType.Developer);
    ///   yield return auth.coroutine;
    ///   Debug.Log(auth.loginCallbackInfo.ResultCode);
    /// </summary>
    public class Auth
    {
        public LoginCredentialType loginCredentialType;
        public string id;
        public string token;
        public LoginCallbackInfo? loginCallbackInfo;
        public Coroutine coroutine;
        public float timeoutSeconds;

        public static Auth Login(LoginCredentialType loginCredentialType, string id, string token,
            System.IntPtr systemAuthCredentialsOptions = default,
            ExternalCredentialType externalType = ExternalCredentialType.Epic, float timeoutSeconds = 30)
        {
            var auth = new Auth();
            auth.loginCredentialType = loginCredentialType;
            auth.id = id;
            auth.token = token;
            auth.timeoutSeconds = timeoutSeconds;
            
            EOSManager.Instance.StartLoginWithLoginTypeAndToken(loginCredentialType, id, token,
                callbackInfo => auth.loginCallbackInfo = callbackInfo);

            var eosManager = UnityEngine.Object.FindObjectOfType<EOSManager>();
            auth.coroutine = eosManager.StartCoroutine(auth.StartLoginWithLoginOptionsCoroutine());

            return auth;
        }

        private IEnumerator StartLoginWithLoginOptionsCoroutine()
        {
            while (!loginCallbackInfo.HasValue)
            {
                if (timeoutSeconds <= 0)
                {
                    loginCallbackInfo = new LoginCallbackInfo { ResultCode = Result.TimedOut };
                    yield break;
                }

                timeoutSeconds -= Time.deltaTime;
                yield return null;
            }
        }
    }
}