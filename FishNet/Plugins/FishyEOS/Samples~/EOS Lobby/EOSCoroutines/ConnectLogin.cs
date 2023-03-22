using System;
using System.Collections;
using EOSLobby.EOSCoroutines;
using Epic.OnlineServices;
using Epic.OnlineServices.Auth;
using Epic.OnlineServices.Connect;
using FishNet.Plugins.FishyEOS.Util;
using UnityEngine;
using Credentials = Epic.OnlineServices.Connect.Credentials;
using LoginCallbackInfo = Epic.OnlineServices.Connect.LoginCallbackInfo;
using LoginOptions = Epic.OnlineServices.Connect.LoginOptions;

namespace EOSLobby
{
    public class ConnectLogin
    {
        public LoginCallbackInfo? CallbackInfo { get; private set; }

        public static Coroutine Run(LoginCredentialType loginCredentialType,
            ExternalCredentialType externalCredentialType, string id, string token, string displayName,
            bool automaticallyCreateDeviceId, bool automaticallyCreateConnectAccount, int timeout,
            AuthScopeFlags scopeFlags, out ConnectLogin connectLogin)
        {
            connectLogin = new ConnectLogin();
            return EOS.GetManager().StartCoroutine(connectLogin.LoginSelectorCoroutine(loginCredentialType,
                externalCredentialType, id, token, displayName, automaticallyCreateDeviceId,
                automaticallyCreateConnectAccount, timeout, scopeFlags));
        }

        private IEnumerator LoginSelectorCoroutine(LoginCredentialType loginCredentialType,
            ExternalCredentialType externalCredentialType, string id, string token, string displayName,
            bool automaticallyCreateDeviceId, bool automaticallyCreateConnectAccount, int timeout,
            AuthScopeFlags scopeFlags)
        {
            switch (loginCredentialType)
            {
                case LoginCredentialType.AccountPortal:
                case LoginCredentialType.ExchangeCode:
                case LoginCredentialType.ExternalAuth:
                case LoginCredentialType.Password:
                case LoginCredentialType.PersistentAuth:
                case LoginCredentialType.RefreshToken:
                    yield return ConnectLogin.Login(token, externalCredentialType, displayName,
                        automaticallyCreateConnectAccount, out var connectLogin, timeout);
                    CallbackInfo = connectLogin.CallbackInfo;
                    break;
                case LoginCredentialType.Developer:
                    yield return ConnectLogin.LoginDeveloper(id, token, loginCredentialType,
                        externalCredentialType, scopeFlags, timeout, null, automaticallyCreateConnectAccount,
                        out connectLogin);
                    CallbackInfo = connectLogin.CallbackInfo;
                    break;
                case LoginCredentialType.DeviceCode:
                    yield return ConnectLogin.LoginDeviceCode(token, externalCredentialType, displayName,
                        automaticallyCreateConnectAccount, timeout, automaticallyCreateDeviceId, out connectLogin);
                    CallbackInfo = connectLogin.CallbackInfo;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(loginCredentialType), loginCredentialType, null);
            }
        }

        private static Coroutine Login(string token, ExternalCredentialType externalCredentialType, string displayName,
            bool automaticallyCreateConnectAccount, out ConnectLogin connectLogin, int timeout = 30)
        {
            connectLogin = new ConnectLogin();
            return EOS.GetManager().StartCoroutine(connectLogin.LoginCoroutine(token, externalCredentialType,
                displayName, automaticallyCreateConnectAccount, timeout));
        }

        private static Coroutine LoginDeveloper(string id, string token, LoginCredentialType loginCredentialType,
            ExternalCredentialType externalCredentialType, AuthScopeFlags scopeFlags, int timeout, string displayName,
            bool automaticallyCreateConnectAccount, out ConnectLogin connectLogin)
        {
            connectLogin = new ConnectLogin();
            return EOS.GetManager().StartCoroutine(connectLogin.LoginDeveloperCoroutine(id, token, loginCredentialType,
                externalCredentialType, scopeFlags, timeout, displayName, automaticallyCreateConnectAccount));
        }

        private static Coroutine LoginDeviceCode(string token, ExternalCredentialType externalCredentialType,
            string displayName, bool automaticallyCreateConnectAccount, int timeout, bool automaticallyCreateDeviceId,
            out ConnectLogin connectLogin)
        {
            connectLogin = new ConnectLogin();
            return EOS.GetManager().StartCoroutine(connectLogin.LoginDeviceCodeCoroutine(token, externalCredentialType,
                displayName, automaticallyCreateConnectAccount, timeout, automaticallyCreateDeviceId));
        }

        private IEnumerator LoginCoroutine(string token, ExternalCredentialType externalCredentialType,
            string displayName, bool automaticallyCreateConnectAccount, int timeout = 30)
        {
            while (true)
            {
                var loginOptions = new LoginOptions
                    { Credentials = new Credentials { Token = token, Type = externalCredentialType }, };

                if (!string.IsNullOrEmpty(displayName))
                    loginOptions.UserLoginInfo = new UserLoginInfo { DisplayName = displayName };

                EOS.GetCachedConnectInterface().Login(ref loginOptions, null,
                    (ref LoginCallbackInfo callbackInfo) => { CallbackInfo = callbackInfo; });

                yield return new WaitUntilOrTimeout(
                    () => CallbackInfo.HasValue, timeout,
                    () => CallbackInfo = new LoginCallbackInfo { ResultCode = Result.TimedOut });

                if (CallbackInfo?.ResultCode != Result.TimedOut) yield break;
                if (CallbackInfo?.ResultCode != Result.InvalidUser) yield break;
                if (!automaticallyCreateConnectAccount) yield break;

                yield return ConnectCreateUser.Run(CallbackInfo?.ContinuanceToken, timeout, out var createUser);
                if (createUser.CallbackInfo?.ResultCode != Result.Success)
                {
                    CallbackInfo = new LoginCallbackInfo
                        { ResultCode = createUser.CallbackInfo?.ResultCode ?? Result.InvalidAuth };
                    yield break;
                }

                automaticallyCreateConnectAccount = false;
            }
        }

        private IEnumerator LoginDeveloperCoroutine(string id, string token, LoginCredentialType loginCredentialType,
            ExternalCredentialType externalCredentialType, AuthScopeFlags scopeFlags, int timeout, string displayName,
            bool automaticallyCreateConnectAccount)
        {
            yield return AuthLogin.Login(id, token, loginCredentialType, externalCredentialType, scopeFlags, timeout,
                out var authLogin);
            if (authLogin.CallbackInfo?.ResultCode != Result.Success)
            {
                CallbackInfo = new LoginCallbackInfo
                    { ResultCode = authLogin.CallbackInfo?.ResultCode ?? Result.InvalidAuth };
                yield break;
            }

            var copyUserAuthTokenOptions = new CopyUserAuthTokenOptions();
            var result = EOS.GetCachedAuthInterface().CopyUserAuthToken(ref copyUserAuthTokenOptions,
                authLogin.CallbackInfo?.LocalUserId, out var authToken);

            if (result != Result.Success)
            {
                CallbackInfo = new LoginCallbackInfo { ResultCode = result };
                yield break;
            }

            var tokenString = authToken?.AccessToken;
            yield return ConnectLogin.Login(tokenString, externalCredentialType, displayName,
                automaticallyCreateConnectAccount, out var connectLogin, timeout);
            CallbackInfo = connectLogin.CallbackInfo;
        }

        private IEnumerator LoginDeviceCodeCoroutine(string token, ExternalCredentialType externalCredentialType,
            string displayName, bool automaticallyCreateConnectAccount, int timeout, bool automaticallyCreateDeviceId)
        {
            yield return ConnectLogin.Login(token, externalCredentialType, displayName,
                automaticallyCreateConnectAccount, out var connectLogin, timeout);
            CallbackInfo = connectLogin.CallbackInfo;
            if (CallbackInfo?.ResultCode != Result.NotFound) yield break;
            if (!automaticallyCreateDeviceId) yield break;

            yield return ConnectCreateDeviceId.Run(timeout, out var connectCreateDeviceId);
            if (connectCreateDeviceId.CallbackInfo?.ResultCode != Result.Success)
            {
                CallbackInfo = new LoginCallbackInfo
                    { ResultCode = connectCreateDeviceId.CallbackInfo?.ResultCode ?? Result.InvalidAuth };
                yield break;
            }

            yield return ConnectLogin.Login(token, externalCredentialType, displayName,
                automaticallyCreateConnectAccount, out connectLogin, timeout);
            CallbackInfo = connectLogin.CallbackInfo;
        }
    }
}