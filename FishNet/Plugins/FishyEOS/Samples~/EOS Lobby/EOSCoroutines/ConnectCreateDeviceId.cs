using System.Collections;
using Epic.OnlineServices;
using Epic.OnlineServices.Connect;
using FishNet.Plugins.FishyEOS.Util;
using UnityEngine;
using SystemInfo = UnityEngine.Device.SystemInfo;

namespace EOSLobby
{
    public class ConnectCreateDeviceId
    {
        public CreateDeviceIdCallbackInfo? CallbackInfo { get; private set; }
        
        public static Coroutine Run(int timeout, out ConnectCreateDeviceId connectCreateDeviceId)
        {
            connectCreateDeviceId = new ConnectCreateDeviceId();
            return EOS.GetManager().StartCoroutine(connectCreateDeviceId.CreateDeviceIdCoroutine(timeout));
        }
        
        private IEnumerator CreateDeviceIdCoroutine(int timeout)
        {
            var createDeviceIdOptions = new CreateDeviceIdOptions
            {
                DeviceModel =
                    $"{SystemInfo.deviceModel} {SystemInfo.deviceName} {SystemInfo.deviceType} {SystemInfo.operatingSystem}",
            };
            EOS.GetCachedConnectInterface().CreateDeviceId(ref createDeviceIdOptions, null,
                (ref CreateDeviceIdCallbackInfo data) => { CallbackInfo = data; });

            yield return new WaitUntilOrTimeout(() => CallbackInfo.HasValue, timeout,
                () => CallbackInfo = new CreateDeviceIdCallbackInfo { ResultCode = Result.TimedOut });
        }
    }
}