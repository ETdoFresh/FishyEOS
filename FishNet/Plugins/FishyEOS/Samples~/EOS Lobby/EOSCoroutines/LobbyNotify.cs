using System;
using System.Collections.Generic;
using Epic.OnlineServices.Lobby;
using FishNet.Plugins.FishyEOS.Util;
using UnityEngine;

namespace EOSLobby.EOSCoroutines
{
    public class LobbyNotify
    {
        public static Dictionary<string, ulong> handles = new Dictionary<string, ulong>();

        public static void AddNotifyLobbyMemberUpdateReceived(Action<LobbyMemberUpdateReceivedCallbackInfo> callback)
        {
            var addNotifyLobbyMemberUpdateReceivedOptions = new AddNotifyLobbyMemberUpdateReceivedOptions();
            var lobbyInterface = EOS.GetPlatformInterface().GetLobbyInterface();
            var handle = lobbyInterface.AddNotifyLobbyMemberUpdateReceived(
                ref addNotifyLobbyMemberUpdateReceivedOptions,
                null, (ref LobbyMemberUpdateReceivedCallbackInfo callbackInfo) => callback?.Invoke(callbackInfo));
            handles.Add($"LobbyMemberUpdateReceived-{callback.Target}-{callback.Method.Name}", handle);
            Debug.Log($"[LobbyNotify] AddNotifyLobbyMemberUpdateReceived: {callback.Target}-{callback.Method.Name} - {handle}");
        }

        public static void AddNotifyLobbyMemberStatusReceived(
            Action<LobbyMemberStatusReceivedCallbackInfo> callback = null)
        {
            var addNotifyLobbyMemberStatusReceivedOptions = new AddNotifyLobbyMemberStatusReceivedOptions { };
            var lobbyInterface = EOS.GetPlatformInterface().GetLobbyInterface();
            var handle = lobbyInterface.AddNotifyLobbyMemberStatusReceived(
                ref addNotifyLobbyMemberStatusReceivedOptions,
                null,
                (ref LobbyMemberStatusReceivedCallbackInfo callbackInfo) => callback?.Invoke(callbackInfo));
            handles.Add($"LobbyMemberStatusReceived-{callback.Target}-{callback.Method.Name}", handle);
            Debug.Log($"[LobbyNotify] AddNotifyLobbyMemberStatusReceived: {callback.Target}-{callback.Method.Name} - {handle}");
        }

        public static void AddNotifyLobbyUpdateReceived(Action<LobbyUpdateReceivedCallbackInfo> callback = null)
        {
            var addNotifyLobbyUpdateReceivedOptions = new AddNotifyLobbyUpdateReceivedOptions { };
            var lobbyInterface = EOS.GetPlatformInterface().GetLobbyInterface();
            var handle = lobbyInterface.AddNotifyLobbyUpdateReceived(ref addNotifyLobbyUpdateReceivedOptions, null,
                (ref LobbyUpdateReceivedCallbackInfo callbackInfo) => callback?.Invoke(callbackInfo));
            handles.Add($"LobbyUpdateReceived-{callback.Target}-{callback.Method.Name}", handle);
            Debug.Log($"[LobbyNotify] AddNotifyLobbyUpdateReceived: {callback.Target}-{callback.Method.Name} - {handle}");
        }

        public static void RemoveNotifyLobbyMemberUpdateReceived(Action<LobbyMemberUpdateReceivedCallbackInfo> callback)
        {
            var key = $"LobbyMemberUpdateReceived-{callback.Target}-{callback.Method.Name}";
            if (!handles.ContainsKey(key)) return;
            var handle = handles[key];
            var lobbyInterface = EOS.GetPlatformInterface()?.GetLobbyInterface();
            lobbyInterface?.RemoveNotifyLobbyMemberUpdateReceived(handle);
            handles.Remove(key);
            Debug.Log($"[LobbyNotify] RemoveNotifyLobbyMemberUpdateReceived: {callback.Target}-{callback.Method.Name} - {handle}");
        }

        public static void RemoveNotifyLobbyMemberStatusReceived(Action<LobbyMemberStatusReceivedCallbackInfo> callback)
        {
            var key = $"LobbyMemberStatusReceived-{callback.Target}-{callback.Method.Name}";
            if (!handles.ContainsKey(key)) return;
            var handle = handles[key];
            var lobbyInterface = EOS.GetPlatformInterface()?.GetLobbyInterface();
            lobbyInterface?.RemoveNotifyLobbyMemberStatusReceived(handle);
            handles.Remove(key);
            Debug.Log($"[LobbyNotify] RemoveNotifyLobbyMemberStatusReceived: {callback.Target}-{callback.Method.Name} - {handle}");
        }

        public static void RemoveNotifyLobbyUpdateReceived(Action<LobbyUpdateReceivedCallbackInfo> callback)
        {
            var key = $"LobbyUpdateReceived-{callback.Target}-{callback.Method.Name}";
            if (!handles.ContainsKey(key)) return;
            var handle = handles[key];
            var lobbyInterface = EOS.GetPlatformInterface()?.GetLobbyInterface();
            lobbyInterface?.RemoveNotifyLobbyUpdateReceived(handle);
            handles.Remove(key);
            Debug.Log($"[LobbyNotify] RemoveNotifyLobbyUpdateReceived: {callback.Target}-{callback.Method.Name} - {handle}");
        }
    }
}