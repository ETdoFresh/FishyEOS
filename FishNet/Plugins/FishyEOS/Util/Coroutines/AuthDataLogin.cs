using System;
using System.Collections;
using Epic.OnlineServices;
using Epic.OnlineServices.Auth;
using UnityEngine;
using Credentials = Epic.OnlineServices.Connect.Credentials;
using LoginCallbackInfo = Epic.OnlineServices.Connect.LoginCallbackInfo;
using LoginOptions = Epic.OnlineServices.Connect.LoginOptions;

namespace FishNet.Plugins.FishyEOS.Util.Coroutines
{
    public class AuthDataLogin
    {
        public LoginCallbackInfo? loginCallbackInfo;

        internal static Coroutine Login(LoginCredentialType loginCredentialType, ExternalCredentialType externalCredentialType,
            string id, string token, string displayName, bool automaticallyCreateDeviceId,
            bool automaticallyCreateConnectAccount, float timeout, AuthScopeFlags scopeFlags,
            out AuthDataLogin authDataLogin)
        {
            var adl = new AuthDataLogin();
            authDataLogin = adl;
            switch (loginCredentialType)
            {
                case LoginCredentialType.AccountPortal:
                case LoginCredentialType.ExchangeCode:
                case LoginCredentialType.ExternalAuth:
                case LoginCredentialType.Password:
                case LoginCredentialType.PersistentAuth:
                case LoginCredentialType.RefreshToken:
                    return StartCoroutine(adl.LoginCoroutine(token, externalCredentialType, displayName, timeout,
                        automaticallyCreateConnectAccount));
                case LoginCredentialType.Developer:
                    return StartCoroutine(adl.LoginDeveloperCoroutine(id, token, loginCredentialType,
                        externalCredentialType, scopeFlags, timeout, displayName, automaticallyCreateConnectAccount));
                case LoginCredentialType.DeviceCode:
                    return StartCoroutine(adl.LoginDeviceIdCoroutine(token, externalCredentialType, displayName,
                        automaticallyCreateConnectAccount, timeout, automaticallyCreateDeviceId));
                default:
                    throw new ArgumentOutOfRangeException(nameof(loginCredentialType), loginCredentialType, null);
            }
        }

        private static Coroutine StartCoroutine(IEnumerator routine)
        {
            return EOS.GetManager().StartCoroutine(routine);
        }

        private IEnumerator LoginCoroutine(string token, ExternalCredentialType externalCredentialType,
            string displayName, float timeout, bool automaticallyCreateConnectAccount)
        {
            loginCallbackInfo = null;
            yield return ConnectLogin.Login(token, externalCredentialType, displayName,
                automaticallyCreateConnectAccount, timeout, out var connectLogin);
            loginCallbackInfo = connectLogin.loginCallbackInfo;
        }

        private IEnumerator LoginDeveloperCoroutine(string id, string token, LoginCredentialType loginCredentialType,
            ExternalCredentialType externalCredentialType, AuthScopeFlags scopeFlags, float timeout, string displayName,
            bool automaticallyCreateConnectAccount)
        {
            loginCallbackInfo = null;
            yield return AuthLogin.Login(out var authLogin, id, token, loginCredentialType, externalCredentialType,
                scopeFlags, timeout);
            if (authLogin.loginCallbackInfo?.ResultCode != Result.Success)
            {
                loginCallbackInfo = new LoginCallbackInfo
                    { ResultCode = authLogin.loginCallbackInfo?.ResultCode ?? Result.UnexpectedError };
                yield break;
            }

            var copyUserAuthTokenOptions = new CopyUserAuthTokenOptions();
            var result = EOS.GetCachedAuthInterface().CopyUserAuthToken(ref copyUserAuthTokenOptions,
                authLogin.loginCallbackInfo?.LocalUserId, out var authToken);
            if (result != Result.Success)
            {
                loginCallbackInfo = new LoginCallbackInfo { ResultCode = result };
                yield break;
            }

            yield return ConnectLogin.Login(authToken, externalCredentialType, displayName,
                automaticallyCreateConnectAccount, timeout, out var connectLogin);
            loginCallbackInfo = connectLogin.loginCallbackInfo;
        }

        private IEnumerator LoginDeviceIdCoroutine(string token, ExternalCredentialType externalCredentialType,
            string displayName, bool automaticallyCreateConnectAccount, float timeout, bool automaticallyCreateDeviceId)
        {
            loginCallbackInfo = null;
            yield return ConnectLogin.Login(token, externalCredentialType, displayName,
                automaticallyCreateConnectAccount, timeout, out var connectLogin);
            loginCallbackInfo = connectLogin.loginCallbackInfo;

            if (connectLogin.loginCallbackInfo?.ResultCode == Result.NotFound)
            {
                if (automaticallyCreateDeviceId)
                {
                    yield return DeviceIdCreate.Create(out var createDeviceId);
                    if (createDeviceId.createDeviceIdCallbackInfo?.ResultCode != Result.Success)
                    {
                        loginCallbackInfo = new LoginCallbackInfo
                        {
                            ResultCode = createDeviceId.createDeviceIdCallbackInfo?.ResultCode ?? Result.UnexpectedError
                        };
                        yield break;
                    }

                    yield return ConnectLogin.Login(token, externalCredentialType, displayName,
                        automaticallyCreateConnectAccount, timeout, out connectLogin);
                    loginCallbackInfo = connectLogin.loginCallbackInfo;
                }
            }
        }
    }
}