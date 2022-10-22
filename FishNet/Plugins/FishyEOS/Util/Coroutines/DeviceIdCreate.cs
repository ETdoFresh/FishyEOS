using System.Collections;
using Epic.OnlineServices.Connect;
using UnityEngine;

namespace FishNet.Plugins.FishyEOS.Util.Coroutines
{
    internal class DeviceIdCreate
    {
        internal CreateDeviceIdCallbackInfo? createDeviceIdCallbackInfo;
        
        internal static Coroutine Create(out DeviceIdCreate deviceIdCreate)
        {
            var dc = new DeviceIdCreate();
            deviceIdCreate = dc;
            var createDeviceIdOptions = new CreateDeviceIdOptions
            {
                DeviceModel =
                    $"{SystemInfo.deviceModel} {SystemInfo.deviceName} {SystemInfo.deviceType} {SystemInfo.operatingSystem}",
            };
            EOS.GetCachedConnectInterface().CreateDeviceId(ref createDeviceIdOptions, null,
                (ref CreateDeviceIdCallbackInfo data) => dc.createDeviceIdCallbackInfo = data);
            
            return EOS.GetManager().StartCoroutine(CreateCoroutine());
            
            IEnumerator CreateCoroutine()
            {
                while (dc.createDeviceIdCallbackInfo == null)
                    yield return null;
            }
        }
    }
}