using System.Collections;
using Epic.OnlineServices;
using Epic.OnlineServices.Auth;
using Epic.OnlineServices.Connect;
using Epic.OnlineServices.Unity;
using UnityEngine;
using Credentials = Epic.OnlineServices.Connect.Credentials;
using LoginCallbackInfo = Epic.OnlineServices.Connect.LoginCallbackInfo;
using LoginOptions = Epic.OnlineServices.Connect.LoginOptions;

namespace FishNet.Plugins.FishyEOS.Util
{
    /// <summary>
    /// Logs into EOS Connect using coroutines.
    /// </summary>
    internal class Connect
    {
        public LoginOptions loginOptions;
        public LoginCallbackInfo? loginCallbackInfo;
        public CreateUserOptions createUserOptions;
        public CreateUserCallbackInfo? createUserCallbackInfo;
        public Coroutine coroutine;
        public float timeoutSeconds = 30;

        public static Connect LoginWithEpicAccount(EpicAccountId epicAccountId, float timeoutSeconds = 30)
        {
            var connect = new Connect();

            var copyUserAuthTokenOptions = new CopyUserAuthTokenOptions();
            EOS.GetPlatformInterface().GetAuthInterface()
                .CopyUserAuthToken(ref copyUserAuthTokenOptions, epicAccountId, out var token);

            connect.loginOptions = new LoginOptions
            {
                Credentials = new Credentials
                {
                    Token = token?.AccessToken,
                    Type = ExternalCredentialType.Epic
                },
                UserLoginInfo = null
            };
            EOS.GetPlatformInterface().GetConnectInterface().Login(ref connect.loginOptions, null,
                delegate(ref LoginCallbackInfo loginCallbackInfo) { connect.loginCallbackInfo = loginCallbackInfo; });

            var eosManager = UnityEngine.Object.FindObjectOfType<EOS>();
            connect.coroutine = eosManager.StartCoroutine(connect.WaitForLoginCallbackInfo());
            return connect;
        }

        public static Connect LoginWithDeviceToken(string displayName)
        {
            var connect = new Connect();
            connect.loginOptions = new LoginOptions
            {
                Credentials = new Credentials
                {
                    Token = null,
                    Type = ExternalCredentialType.DeviceidAccessToken
                },
                UserLoginInfo = new UserLoginInfo
                {
                    DisplayName = displayName
                }
            };
            EOS.GetPlatformInterface().GetConnectInterface().Login(ref connect.loginOptions, null,
                delegate(ref LoginCallbackInfo loginCallbackInfo) { connect.loginCallbackInfo = loginCallbackInfo; });

            var eosManager = UnityEngine.Object.FindObjectOfType<EOS>();
            connect.coroutine = eosManager.StartCoroutine(connect.WaitForLoginCallbackInfo());
            return connect;
        }

        public static Connect CreateUserWithContinuanceToken(ContinuanceToken continuanceToken)
        {
            var connect = new Connect();
            connect.createUserOptions = new CreateUserOptions
            {
                ContinuanceToken = continuanceToken
            };
            EOS.GetPlatformInterface().GetConnectInterface().CreateUser(ref connect.createUserOptions, null,
                delegate(ref CreateUserCallbackInfo createUserCallbackInfo)
                {
                    connect.createUserCallbackInfo = createUserCallbackInfo;
                });

            var eosManager = UnityEngine.Object.FindObjectOfType<EOS>();
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