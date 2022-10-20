using Epic.OnlineServices.Platform;
using PlayEveryWare.EpicOnlineServices;
using UnityEngine;

namespace FishNet.Plugins.FishyEOS.Util
{
    public static class EOS
    {
        private static PlatformInterface _platformInterface;
        private static EOSManager _eosManager;
        private static bool _createdOrGotPlatformInterface = false;

        public static PlatformInterface GetPlatformInterface()
        {
            if (_createdOrGotPlatformInterface) return _platformInterface;
            GetManager();
            _createdOrGotPlatformInterface = true;
            if (_platformInterface != null) return _platformInterface;
            _platformInterface = EOSManager.Instance?.GetEOSPlatformInterface();
            if (_platformInterface != null) return _platformInterface;
            var gameObject = new GameObject("EOSManager");
            UnityEngine.Object.DontDestroyOnLoad(gameObject);
            _eosManager = gameObject.AddComponent<EOSManager>();
#if !UNITY_EDITOR && !(UNITY_STANDALONE_WIN)
            EOSManager.Instance?.Init(_eosManager, EOSManager.ConfigFileName);
#endif
            _platformInterface = EOSManager.Instance?.GetEOSPlatformInterface();
            return _platformInterface;
        }

        public static EOSManager GetManager()
        {
            if (_eosManager) return _eosManager;
            _eosManager = UnityEngine.Object.FindObjectOfType<EOSManager>();
            if (_eosManager) return _eosManager;
            if (_createdOrGotPlatformInterface) return null;
            _createdOrGotPlatformInterface = true;
            var gameObject = new GameObject("EOSManager");
            UnityEngine.Object.DontDestroyOnLoad(gameObject);
            _eosManager = gameObject.AddComponent<EOSManager>();
#if !UNITY_EDITOR && !(UNITY_STANDALONE_WIN)
            EOSManager.Instance?.Init(_eosManager, EOSManager.ConfigFileName);
#endif
            _platformInterface = EOSManager.Instance?.GetEOSPlatformInterface();
            return _eosManager;
        }
    }
}