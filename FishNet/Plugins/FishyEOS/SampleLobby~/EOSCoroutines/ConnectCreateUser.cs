using System.Collections;
using Epic.OnlineServices;
using Epic.OnlineServices.Connect;
using FishNet.Plugins.FishyEOS.Util;
using UnityEngine;

namespace EOSLobby
{
    public class ConnectCreateUser
    {
        public CreateUserCallbackInfo? CallbackInfo { get; private set; }

        public static Coroutine Run(ContinuanceToken continuanceToken, int timeout, out ConnectCreateUser connectCreateUser)
        {
            connectCreateUser = new ConnectCreateUser();
            return EOS.GetManager().StartCoroutine(connectCreateUser.CreateUserCoroutine(continuanceToken, timeout));
        }
        
        private IEnumerator CreateUserCoroutine(ContinuanceToken continuanceToken, int timeout)
        {
            var createUserOptions = new CreateUserOptions { ContinuanceToken = continuanceToken };
            EOS.GetCachedConnectInterface().CreateUser(ref createUserOptions, null,
                (ref CreateUserCallbackInfo callbackInfo) => { CallbackInfo = callbackInfo; });
            
            yield return new WaitUntilOrTimeout(() => CallbackInfo.HasValue, timeout,
                () => CallbackInfo = new CreateUserCallbackInfo { ResultCode = Result.TimedOut });
        }
    }
}