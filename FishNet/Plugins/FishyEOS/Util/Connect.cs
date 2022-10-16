using System.Collections;
using Epic.OnlineServices;
using Epic.OnlineServices.Connect;
using PlayEveryWare.EpicOnlineServices;
using UnityEngine;

namespace FishNet.Plugins.FishyEOS.Util
{
    /// <summary>
    /// Logs into EOS Connect using coroutines.
    /// </summary>
    public class Connect
    {
        public Credentials credentials;
        public LoginCallbackInfo? loginCallbackInfo;
        public Coroutine coroutine;
        public float timeoutSeconds = 30;
        public CreateUserCallbackInfo? createUserCallbackInfo;

        public static Connect LoginWithEpicAccount(EpicAccountId epicAccountId, float timeoutSeconds = 30)
        {
            var connect = new Connect();
            EOSManager.Instance.StartConnectLoginWithEpicAccount(epicAccountId,
                loginCallbackInfo => connect.loginCallbackInfo = loginCallbackInfo);

            var eosManager = UnityEngine.Object.FindObjectOfType<EOSManager>();
            connect.coroutine = eosManager.StartCoroutine(connect.WaitForLoginCallbackInfo());
            return connect;
        }

        public static Connect LoginWithDeviceToken(string displayName)
        {
            var connect = new Connect();
            EOSManager.Instance.StartConnectLoginWithDeviceToken(displayName,
                loginCallbackInfo => connect.loginCallbackInfo = loginCallbackInfo);

            var eosManager = UnityEngine.Object.FindObjectOfType<EOSManager>();
            connect.coroutine = eosManager.StartCoroutine(connect.WaitForLoginCallbackInfo());
            return connect;
        }

        public static Connect CreateUserWithContinuanceToken(ContinuanceToken continuanceToken)
        {
            var connect = new Connect();
            EOSManager.Instance.CreateConnectUserWithContinuanceToken(continuanceToken,
                createUserCallbackInfo => connect.createUserCallbackInfo = createUserCallbackInfo);

            var eosManager = UnityEngine.Object.FindObjectOfType<EOSManager>();
            connect.coroutine = eosManager.StartCoroutine(connect.WaitForCreateUserCallbackInfo());
            return connect;
        }

        private IEnumerator WaitForLoginCallbackInfo()
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
        
        private IEnumerator WaitForCreateUserCallbackInfo()
        {
            while (!createUserCallbackInfo.HasValue)
            {
                if (timeoutSeconds <= 0)
                {
                    createUserCallbackInfo = new CreateUserCallbackInfo { ResultCode = Result.TimedOut };
                    yield break;
                }

                timeoutSeconds -= Time.deltaTime;
                yield return null;
            }
        }
    }
}