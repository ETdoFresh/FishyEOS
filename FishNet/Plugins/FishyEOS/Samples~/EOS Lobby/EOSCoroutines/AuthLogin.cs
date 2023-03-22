using System.Collections;
using Epic.OnlineServices;
using Epic.OnlineServices.Auth;
using FishNet.Plugins.FishyEOS.Util;
using UnityEngine;

namespace EOSLobby.EOSCoroutines
{
    public class AuthLogin
    {
        public LoginCallbackInfo? CallbackInfo { get; private set; }
    
        public static Coroutine Login(string id, string token, LoginCredentialType loginCredentialType,
            ExternalCredentialType externalCredentialType, AuthScopeFlags scopeFlags, int timeout, out AuthLogin authLogin)
        {
            authLogin = new AuthLogin();
            return EOS.GetManager().StartCoroutine(authLogin.LoginCoroutine(id, token, loginCredentialType,
                externalCredentialType, scopeFlags, timeout));
        }
    
        private IEnumerator LoginCoroutine(string id, string token, LoginCredentialType loginCredentialType,
            ExternalCredentialType externalCredentialType, AuthScopeFlags scopeFlags, int timeout)
        {
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
                (ref LoginCallbackInfo callbackInfo) => { this.CallbackInfo = callbackInfo; });
        
            yield return new WaitUntilOrTimeout(
                () => CallbackInfo.HasValue, timeout,
                () => CallbackInfo = new LoginCallbackInfo { ResultCode = Result.TimedOut });
        }
    }
}