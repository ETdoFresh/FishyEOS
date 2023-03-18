using System;
using System.Threading.Tasks;
using Epic.OnlineServices;
using Epic.OnlineServices.Auth;
using Epic.OnlineServices.Connect;
using FishNet.Plugins.FishyEOS.Util;
using Credentials = Epic.OnlineServices.Connect.Credentials;
using LoginCallbackInfo = Epic.OnlineServices.Connect.LoginCallbackInfo;
using LoginOptions = Epic.OnlineServices.Connect.LoginOptions;
using SystemInfo = UnityEngine.Device.SystemInfo;

namespace EOSLobby
{
    public static class ConnectEOS
    {
        public static async Task<LoginCallbackInfo> LoginAsync(LoginCredentialType loginCredentialType,
            ExternalCredentialType externalCredentialType,
            string id, string token, string displayName, bool automaticallyCreateDeviceId,
            bool automaticallyCreateConnectAccount, int timeout, AuthScopeFlags scopeFlags)
        {
            switch (loginCredentialType)
            {
                case LoginCredentialType.AccountPortal:
                case LoginCredentialType.ExchangeCode:
                case LoginCredentialType.ExternalAuth:
                case LoginCredentialType.Password:
                case LoginCredentialType.PersistentAuth:
                case LoginCredentialType.RefreshToken:
                    return await Login(token, externalCredentialType, displayName,
                        automaticallyCreateConnectAccount, timeout);
                case LoginCredentialType.Developer:
                    return await LoginDeveloper(id, token, loginCredentialType, externalCredentialType, scopeFlags,
                        timeout, null, automaticallyCreateConnectAccount);
                case LoginCredentialType.DeviceCode:
                    return await LoginDeviceCode(token, externalCredentialType, displayName,
                        automaticallyCreateConnectAccount, timeout, automaticallyCreateDeviceId);
                default:
                    throw new ArgumentOutOfRangeException(nameof(loginCredentialType), loginCredentialType, null);
            }
        }

        private static async Task<LoginCallbackInfo> LoginDeveloper(string id, string token,
            LoginCredentialType loginCredentialType,
            ExternalCredentialType externalCredentialType, AuthScopeFlags scopeFlags, int timeout, string displayName,
            bool automaticallyCreateConnectAccount)
        {
            var loginCallbackInfo = await AuthEOS.Login(id, token, loginCredentialType, externalCredentialType,
                scopeFlags, timeout);

            if (loginCallbackInfo.ResultCode != Result.Success)
                return new LoginCallbackInfo { ResultCode = loginCallbackInfo.ResultCode };

            var copyUserAuthTokenOptions = new CopyUserAuthTokenOptions();
            var result = EOS.GetCachedAuthInterface().CopyUserAuthToken(ref copyUserAuthTokenOptions,
                loginCallbackInfo.LocalUserId, out var authToken);
            if (result != Result.Success)
                return new LoginCallbackInfo { ResultCode = result };

            return await Login(authToken, externalCredentialType, displayName, automaticallyCreateConnectAccount,
                timeout);
        }

        private static async Task<LoginCallbackInfo> LoginDeviceCode(string token,
            ExternalCredentialType externalCredentialType, string displayName, bool automaticallyCreateConnectAccount,
            int timeout, bool automaticallyCreateDeviceId)
        {
            var loginCallbackInfo = await Login(token, externalCredentialType, displayName,
                automaticallyCreateConnectAccount, timeout);

            if (loginCallbackInfo.ResultCode != Result.NotFound) return loginCallbackInfo;
            if (!automaticallyCreateDeviceId) return loginCallbackInfo;

            var createDeviceIdCallbackInfo = await CreateDeviceId(timeout);
            if (createDeviceIdCallbackInfo.ResultCode != Result.Success)
                return new LoginCallbackInfo { ResultCode = createDeviceIdCallbackInfo.ResultCode };

            return await Login(token, externalCredentialType, displayName, automaticallyCreateConnectAccount, timeout);
        }

        public static async Task<LoginCallbackInfo> Login(string token,
            ExternalCredentialType externalCredentialType,
            string displayName, bool automaticallyCreateConnectAccount, int timeout = 30)
        {
            while (true)
            {
                var loginOptions = new LoginOptions
                    { Credentials = new Credentials { Token = token, Type = externalCredentialType }, };

                if (!string.IsNullOrEmpty(displayName))
                    loginOptions.UserLoginInfo = new UserLoginInfo { DisplayName = displayName };

                var tcs = new TaskCompletionSource<LoginCallbackInfo>();
                EOS.GetCachedConnectInterface().Login(ref loginOptions, null,
                    (ref LoginCallbackInfo callbackInfo) => { tcs.SetResult(callbackInfo); });

                var timeoutTask = Task.Delay(timeout * 1000);
                var taskResult = await Task.WhenAny(tcs.Task, timeoutTask);

                if (taskResult == timeoutTask) return new LoginCallbackInfo { ResultCode = Result.TimedOut };

                var loginCallbackInfo = tcs.Task.Result;

                if (loginCallbackInfo.ResultCode != Result.InvalidUser) return loginCallbackInfo;
                if (!automaticallyCreateConnectAccount) return loginCallbackInfo;

                var createUserCallbackInfo = await CreateUser(loginCallbackInfo.ContinuanceToken, timeout);

                if (createUserCallbackInfo.ResultCode != Result.Success)
                    return new LoginCallbackInfo { ResultCode = createUserCallbackInfo.ResultCode };

                automaticallyCreateConnectAccount = false;
            }
        }

        private static async Task<CreateUserCallbackInfo> CreateUser(ContinuanceToken continuanceToken, int timeout)
        {
            var createUserOptions = new CreateUserOptions { ContinuanceToken = continuanceToken };
            var tcs = new TaskCompletionSource<CreateUserCallbackInfo>();
            EOS.GetCachedConnectInterface().CreateUser(ref createUserOptions, null,
                (ref CreateUserCallbackInfo callbackInfo) => { tcs.SetResult(callbackInfo); });
            return await tcs.Task;
        }

        private static async Task<LoginCallbackInfo> Login(Token? token,
            ExternalCredentialType externalCredentialType, string displayName,
            bool automaticallyCreateConnectAccount, int timeout)
        {
            var tokenString = token?.AccessToken;
            return await Login(tokenString, externalCredentialType, displayName,
                automaticallyCreateConnectAccount, timeout);
        }

        private static async Task<CreateDeviceIdCallbackInfo> CreateDeviceId(int timeout)
        {
            var createDeviceIdOptions = new CreateDeviceIdOptions
            {
                DeviceModel =
                    $"{SystemInfo.deviceModel} {SystemInfo.deviceName} {SystemInfo.deviceType} {SystemInfo.operatingSystem}",
            };
            var tcs = new TaskCompletionSource<CreateDeviceIdCallbackInfo>();
            EOS.GetCachedConnectInterface().CreateDeviceId(ref createDeviceIdOptions, null,
                (ref CreateDeviceIdCallbackInfo data) => { tcs.SetResult(data); });

            var timeoutTask = Task.Delay(timeout * 1000);
            var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);
            return completedTask == timeoutTask
                ? new CreateDeviceIdCallbackInfo { ResultCode = Result.TimedOut }
                : tcs.Task.Result;
        }
    }
}