using System.Collections;
using Epic.OnlineServices.Connect;
using PlayEveryWare.EpicOnlineServices;
using UnityEngine;

namespace FishNet.Plugins.FishyEOS.Util
{
    public class DeviceId
    {
        public CreateDeviceIdCallbackInfo? createDeviceIdCallbackInfo;
        public Coroutine coroutine;

        public static DeviceId Create()
        {
            var deviceId = new DeviceId();
            var options = new CreateDeviceIdOptions
            {
                DeviceModel =
                    $"{SystemInfo.deviceModel} {SystemInfo.deviceName} {SystemInfo.deviceType} {SystemInfo.operatingSystem}",
            };
            EOSManager.Instance.GetEOSConnectInterface().CreateDeviceId(ref options, null,
                (ref CreateDeviceIdCallbackInfo data) => deviceId.createDeviceIdCallbackInfo = data);
            
            var eosManager = UnityEngine.Object.FindObjectOfType<EOSManager>();
            deviceId.coroutine = eosManager.StartCoroutine(deviceId.WaitForCreateDeviceIdCallbackInfo());
            return deviceId;
        }

        private IEnumerator WaitForCreateDeviceIdCallbackInfo()
        {
            while (!createDeviceIdCallbackInfo.HasValue)
                yield return null;
        }
    }
}