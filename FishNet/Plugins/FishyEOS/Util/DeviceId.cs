using System.Collections;
using Epic.OnlineServices.Connect;
using Epic.OnlineServices.Unity;
using UnityEngine;

namespace FishNet.Plugins.FishyEOS.Util
{
    /// <summary>
    /// Creates a new DeviceId for use with Connect.LoginWithDeviceToken()
    /// </summary>
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
            EOS.GetPlatformInterface().GetConnectInterface().CreateDeviceId(ref options, null,
                (ref CreateDeviceIdCallbackInfo data) => deviceId.createDeviceIdCallbackInfo = data);
            
            var eosManager = UnityEngine.Object.FindObjectOfType<EOS>();
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