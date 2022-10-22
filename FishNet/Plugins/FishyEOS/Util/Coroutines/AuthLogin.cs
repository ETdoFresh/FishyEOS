using System.Collections;
using Epic.OnlineServices;
using Epic.OnlineServices.Auth;
using UnityEngine;
using Credentials = Epic.OnlineServices.Auth.Credentials;
using LoginCallbackInfo = Epic.OnlineServices.Auth.LoginCallbackInfo;
using LoginOptions = Epic.OnlineServices.Auth.LoginOptions;

namespace FishNet.Plugins.FishyEOS.Util.Coroutines
{
    internal class AuthLogin
    {
        internal LoginCallbackInfo? loginCallbackInfo;
        
        internal static Coroutine Login(out AuthLogin authLogin, string id, string token,
            LoginCredentialType loginCredentialType, ExternalCredentialType externalCredentialType,
            AuthScopeFlags scopeFlags, float timeout)
        {
            var al = new AuthLogin();
            authLogin = al;
            var loginOptions = new LoginOptions
            {
                Credentials = new Credentials
                {
                    Id = id,
                    Token = token,
                    Type = loginCredentialType,
                    SystemAuthCredentialsOptions = default,
                    ExternalType = externalCredentialType
                },
                ScopeFlags = scopeFlags
            };

            EOS.GetCachedAuthInterface().Login(ref loginOptions, null,
                delegate(ref LoginCallbackInfo callbackInfo) { al.loginCallbackInfo = callbackInfo; });

            return EOS.GetManager().StartCoroutine(WaitForAuthLogin());
            
            IEnumerator WaitForAuthLogin()
            {
                var timeoutTime = Time.time + timeout;
                while (!al.loginCallbackInfo.HasValue)
                {
                    if (Time.time > timeoutTime)
                    {
                        al.loginCallbackInfo = new LoginCallbackInfo { ResultCode = Result.TimedOut };
                        yield break;
                    }
                    yield return null;
                }
            }
        }
    }
}