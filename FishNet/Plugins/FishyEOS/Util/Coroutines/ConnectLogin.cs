using System.Collections;
using Epic.OnlineServices;
using Epic.OnlineServices.Auth;
using Epic.OnlineServices.Connect;
using UnityEngine;
using Credentials = Epic.OnlineServices.Connect.Credentials;
using LoginCallbackInfo = Epic.OnlineServices.Connect.LoginCallbackInfo;
using LoginOptions = Epic.OnlineServices.Connect.LoginOptions;

namespace FishNet.Plugins.FishyEOS.Util.Coroutines
{
    internal class ConnectLogin
    {
        internal LoginCallbackInfo? loginCallbackInfo;

        internal static Coroutine Login(string token, ExternalCredentialType externalCredentialType, string displayName,
            bool automaticallyCreateConnectAccount, float timeout, out ConnectLogin connectLogin)
        {
            var cl = new ConnectLogin();
            connectLogin = cl;
            var loginOptions = new LoginOptions
            {
                Credentials = new Credentials
                {
                    Token = token,
                    Type = externalCredentialType
                },
            };
            if (!string.IsNullOrEmpty(displayName))
                loginOptions.UserLoginInfo = new UserLoginInfo { DisplayName = displayName };
            
            EOS.GetCachedConnectInterface().Login(ref loginOptions, null,
                (ref LoginCallbackInfo data) => cl.loginCallbackInfo = data);

            return EOS.GetManager().StartCoroutine(WaitForConnectLogin());

            IEnumerator WaitForConnectLogin()
            {
                var timeoutTime = Time.time + timeout;
                while (!cl.loginCallbackInfo.HasValue)
                {
                    if (Time.time > timeoutTime)
                    {
                        cl.loginCallbackInfo = new LoginCallbackInfo { ResultCode = Result.TimedOut };
                        yield break;
                    }

                    yield return null;
                }

                if (cl.loginCallbackInfo?.ResultCode == Result.InvalidUser)
                {
                    if (automaticallyCreateConnectAccount)
                    {
                        yield return ConnectCreateUser.CreateUser(cl.loginCallbackInfo?.ContinuanceToken, timeout,
                            out var connectCreateUser);
                        if (connectCreateUser.createUserCallbackInfo?.ResultCode != Result.Success)
                        {
                            cl.loginCallbackInfo = new LoginCallbackInfo
                            {
                                ResultCode = connectCreateUser.createUserCallbackInfo?.ResultCode ??
                                             Result.UnexpectedError
                            };
                            yield break;
                        }

                        yield return Login(token, externalCredentialType, displayName, false, timeout,
                            out var connectLoginAgain);
                        cl.loginCallbackInfo = connectLoginAgain.loginCallbackInfo;
                    }
                }
            }
        }

        internal static Coroutine Login(Token? token, ExternalCredentialType externalCredentialType, string displayName,
            bool automaticallyCreateConnectAccount, float timeout, out ConnectLogin connectLogin)
        {
            var tokenString = token?.AccessToken;
            return Login(tokenString, externalCredentialType, displayName, automaticallyCreateConnectAccount, timeout,
                out connectLogin);
        }
    }
}