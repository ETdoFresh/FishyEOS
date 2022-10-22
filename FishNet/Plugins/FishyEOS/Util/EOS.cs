using Epic.OnlineServices;
using Epic.OnlineServices.Auth;
using Epic.OnlineServices.Connect;
using Epic.OnlineServices.P2P;
using Epic.OnlineServices.Platform;
using PlayEveryWare.EpicOnlineServices;
using UnityEngine;

namespace FishNet.Plugins.FishyEOS.Util
{
    /// <summary>
    /// References to EOS Objects
    /// </summary>
    public static class EOS
    {
        private static AuthInterface _cachedAuthInterface;
        private static ConnectInterface _cachedConnectInterface;
        private static P2PInterface _cachedP2PInterface;
        private static EOSManager _eosManager;
        private static bool _createdOrGotPlatformInterface;

        private static PlatformInterface PlatformInterface => GetPlatformInterface();

        public static ProductUserId LocalProductUserId => GetCachedConnectInterface()?.GetLoggedInUserByIndex(0);

        public static PlatformInterface GetPlatformInterface()
        {
            if (_createdOrGotPlatformInterface) return EOSManager.Instance?.GetEOSPlatformInterface();
            GetManager();
            _createdOrGotPlatformInterface = true;
            if (PlatformInterface != null) return PlatformInterface;
            var gameObject = new GameObject("EOSManager");
            UnityEngine.Object.DontDestroyOnLoad(gameObject);
            _eosManager = gameObject.AddComponent<EOSManager>();
#if !UNITY_EDITOR && !(UNITY_STANDALONE_WIN)
            EOSManager.Instance?.Init(_eosManager, EOSManager.ConfigFileName);
#endif
            return PlatformInterface;
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
            return _eosManager;
        }

        public static AuthInterface GetCachedAuthInterface()
        {
            if (!IsSafeToUseCache()) return null;
            if (_cachedAuthInterface != null) return _cachedAuthInterface;
            _cachedAuthInterface = PlatformInterface?.GetAuthInterface();
            return _cachedAuthInterface;
        }

        public static ConnectInterface GetCachedConnectInterface()
        {
            if (!IsSafeToUseCache()) return null;
            if (_cachedConnectInterface != null) return _cachedConnectInterface;
            _cachedConnectInterface = PlatformInterface?.GetConnectInterface();
            return _cachedConnectInterface;
        }

        public static P2PInterface GetCachedP2PInterface()
        {
            if (!IsSafeToUseCache()) return null;
            if (_cachedP2PInterface != null) return _cachedP2PInterface;
            _cachedP2PInterface = PlatformInterface?.GetP2PInterface();
            return _cachedP2PInterface;
        }

        private static bool IsSafeToUseCache()
        {
            // Cache references maybe unsafe if PlatformInterface is null
            return PlatformInterface != null;
        }
    }
}