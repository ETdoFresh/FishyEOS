using System;
using System.Collections;
using Epic.OnlineServices;
using Epic.OnlineServices.Auth;
using PlayEveryWare.EpicOnlineServices;
using UnityEngine;
using LoginCallbackInfo = Epic.OnlineServices.Connect.LoginCallbackInfo;

namespace FishNet.Plugins.FishyEOS.Util
{
    [Serializable]
    public class AuthConnectData
    {
        public LoginCredentialType loginCredentialType = LoginCredentialType.DeviceCode;
        public string id = "FishyEOS";
        public string token;
        public ExternalCredentialType externalCredentialType;
        public LoginCallbackInfo? loginCallbackInfo;
        public Coroutine coroutine;

        public void Connect()
        {
            var eosManager = UnityEngine.Object.FindObjectOfType<EOSManager>();
            if (!eosManager)
            {
                Debug.LogError("EOSManager not found");
                return;
            }
            coroutine = eosManager.StartCoroutine(ConnectCoroutine());
        }

        public IEnumerator ConnectCoroutine()
        {
            if (loginCredentialType == LoginCredentialType.DeviceCode)
            {
                var connect = Plugins.FishyEOS.Util.Connect.LoginWithDeviceToken(id);
                yield return connect.coroutine;
                if (connect.loginCallbackInfo?.ResultCode == Result.NotFound)
                {
                    Debug.Log("[Connect] Device ID not found on this system, creating new one");
                    var create = Plugins.FishyEOS.Util.DeviceId.Create();
                    yield return create.coroutine;
                    if (create.createDeviceIdCallbackInfo?.ResultCode != Result.Success)
                    {
                        Debug.LogError("[Connect] Failed to create device ID");
                        yield break;
                    }
                    var connectAgain = Plugins.FishyEOS.Util.Connect.LoginWithDeviceToken(id);
                    yield return connectAgain.coroutine;
                    if (connectAgain.loginCallbackInfo?.ResultCode != Result.Success)
                    {
                        Debug.LogError(
                            $"[Connect] Failed to login with device ID {connectAgain.loginCallbackInfo?.ResultCode}");
                        yield break;
                    }
                    Debug.Log("[Connect] Logged in with device ID");
                    loginCallbackInfo = connectAgain.loginCallbackInfo;
                }
                else if (connect.loginCallbackInfo?.ResultCode != Result.Success)
                {
                    Debug.LogError($"[Connect] Failed to login with device ID {connect.loginCallbackInfo?.ResultCode}");
                    yield break;
                }
                Debug.Log("[Connect] Logged in with device ID");
                loginCallbackInfo = connect.loginCallbackInfo;
            }
            else if (loginCredentialType == LoginCredentialType.Developer)
            {
                var auth = Plugins.FishyEOS.Util.Auth.Login(loginCredentialType, id, token);
                yield return auth.coroutine;
                if (auth.loginCallbackInfo?.ResultCode != Result.Success)
                {
                    Debug.LogError($"[Auth] Failed to login with developer credentials {auth.loginCallbackInfo?.ResultCode}");
                    yield break;
                }
                Debug.Log("[Auth] Logged in with developer credentials");
                var epicAccountId = auth.loginCallbackInfo?.LocalUserId;
                var connect = Plugins.FishyEOS.Util.Connect.LoginWithEpicAccount(epicAccountId);
                yield return connect.coroutine;
                if (connect.loginCallbackInfo?.ResultCode == Result.InvalidUser)
                {
                    // No user found with this account, assume new user should be created
                    var createUser = Plugins.FishyEOS.Util.Connect.CreateUserWithContinuanceToken(connect.loginCallbackInfo?.ContinuanceToken);
                    yield return createUser.coroutine;
                    if (createUser.createUserCallbackInfo?.ResultCode != Result.Success)
                    {
                        Debug.LogError($"[Connect] Failed to create user with continuance token {createUser.createUserCallbackInfo?.ResultCode}");
                        yield break;
                    }
                    var connectAgain = Plugins.FishyEOS.Util.Connect.LoginWithEpicAccount(epicAccountId);
                    yield return connectAgain.coroutine;
                    if (connectAgain.loginCallbackInfo?.ResultCode != Result.Success)
                    {
                        Debug.LogError($"[Connect] Failed to login with epic account {connectAgain.loginCallbackInfo?.ResultCode}");
                        yield break;
                    }
                }
                else if (connect.loginCallbackInfo?.ResultCode != Result.Success)
                {
                    Debug.LogError($"[Connect] Failed to login with epic account {connect.loginCallbackInfo?.ResultCode}");
                    yield break;
                }
                Debug.Log("[Connect] Logged in with developer credentials");
                loginCallbackInfo = connect.loginCallbackInfo;
            }
            else
            {
                throw new NotImplementedException($"[Auth] LoginCredentialType {loginCredentialType} not implemented");
            }
        }
    }
}