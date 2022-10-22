using System.Collections;
using Epic.OnlineServices;
using Epic.OnlineServices.Connect;
using UnityEngine;

namespace FishNet.Plugins.FishyEOS.Util.Coroutines
{
    internal class ConnectCreateUser
    {
        internal CreateUserCallbackInfo? createUserCallbackInfo;

        internal static Coroutine CreateUser(ContinuanceToken continuanceToken, float timeout, out ConnectCreateUser connectCreateUser)
        {
            var cl = new ConnectCreateUser();
            connectCreateUser = cl;
            var createUserOptions = new CreateUserOptions { ContinuanceToken = continuanceToken };
            EOS.GetCachedConnectInterface().CreateUser(ref createUserOptions, null,
                delegate(ref CreateUserCallbackInfo callbackInfo)
                {
                    cl.createUserCallbackInfo = callbackInfo;
                });
            
            return EOS.GetManager().StartCoroutine(WaitForConnectCreateUser());
            
            IEnumerator WaitForConnectCreateUser()
            {
                var timeoutTime = Time.time + timeout;
                while (!cl.createUserCallbackInfo.HasValue)
                {
                    if (Time.time > timeoutTime)
                    {
                        cl.createUserCallbackInfo = new CreateUserCallbackInfo { ResultCode = Result.TimedOut };
                        yield break;
                    }

                    yield return null;
                }
            }
        }
    }
}