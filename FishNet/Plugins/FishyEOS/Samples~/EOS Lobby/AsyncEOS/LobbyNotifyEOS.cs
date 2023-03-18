using System;
using System.Collections.Generic;
using Epic.OnlineServices.Lobby;
using FishNet.Plugins.FishyEOS.Util;

public static class LobbyNotifyEOS
{
    private enum LobbyNotifyType
    {
        LobbyMemberUpdateReceived,
        LobbyMemberStatusReceived,
        LobbyUpdateReceived,
    }
    
    private static readonly Dictionary<LobbyNotifyType, Dictionary<object, ulong>> LobbyNotifyHandles = new();

    public static void AddNotifyLobbyMemberUpdateReceived(Action<LobbyMemberUpdateReceivedCallbackInfo> callback)
    {
        var addNotifyLobbyMemberUpdateReceivedOptions = new AddNotifyLobbyMemberUpdateReceivedOptions { };
        var lobbyInterface = EOS.GetPlatformInterface().GetLobbyInterface();
        var handle = lobbyInterface.AddNotifyLobbyMemberUpdateReceived(ref addNotifyLobbyMemberUpdateReceivedOptions,
            null,
            (ref LobbyMemberUpdateReceivedCallbackInfo callbackInfo) => callback?.Invoke(callbackInfo));
        if (!LobbyNotifyHandles.ContainsKey(LobbyNotifyType.LobbyMemberUpdateReceived))
            LobbyNotifyHandles.Add(LobbyNotifyType.LobbyMemberUpdateReceived, new Dictionary<object, ulong>());
        if (LobbyNotifyHandles[LobbyNotifyType.LobbyMemberUpdateReceived].ContainsKey(callback.Target))
            throw new Exception($"LobbyNotifyHandles already contains callback for {callback.Target}");
        LobbyNotifyHandles[LobbyNotifyType.LobbyMemberUpdateReceived].Add(callback.Target, handle);
    }

    public static void AddNotifyLobbyMemberStatusReceived(Action<LobbyMemberStatusReceivedCallbackInfo> callback = null)
    {
        var addNotifyLobbyMemberStatusReceivedOptions = new AddNotifyLobbyMemberStatusReceivedOptions { };
        var lobbyInterface = EOS.GetPlatformInterface().GetLobbyInterface();
        var handle = lobbyInterface.AddNotifyLobbyMemberStatusReceived(ref addNotifyLobbyMemberStatusReceivedOptions,
            null,
            (ref LobbyMemberStatusReceivedCallbackInfo callbackInfo) => callback?.Invoke(callbackInfo));
        if (!LobbyNotifyHandles.ContainsKey(LobbyNotifyType.LobbyMemberStatusReceived))
            LobbyNotifyHandles.Add(LobbyNotifyType.LobbyMemberStatusReceived, new Dictionary<object, ulong>());
        if (LobbyNotifyHandles[LobbyNotifyType.LobbyMemberStatusReceived].ContainsKey(callback.Target))
            throw new Exception($"LobbyNotifyHandles already contains callback for {callback.Target}");
        LobbyNotifyHandles[LobbyNotifyType.LobbyMemberStatusReceived].Add(callback.Target, handle);
    }

    public static void AddNotifyLobbyUpdateReceived(Action<LobbyUpdateReceivedCallbackInfo> callback = null)
    {
        var addNotifyLobbyUpdateReceivedOptions = new AddNotifyLobbyUpdateReceivedOptions { };
        var lobbyInterface = EOS.GetPlatformInterface().GetLobbyInterface();
        var handle = lobbyInterface.AddNotifyLobbyUpdateReceived(ref addNotifyLobbyUpdateReceivedOptions, null,
            (ref LobbyUpdateReceivedCallbackInfo callbackInfo) => callback?.Invoke(callbackInfo));
        if (!LobbyNotifyHandles.ContainsKey(LobbyNotifyType.LobbyUpdateReceived))
            LobbyNotifyHandles.Add(LobbyNotifyType.LobbyUpdateReceived, new Dictionary<object, ulong>());
        if (LobbyNotifyHandles[LobbyNotifyType.LobbyUpdateReceived].ContainsKey(callback.Target))
            throw new Exception($"LobbyNotifyHandles already contains callback for {callback.Target}");
        LobbyNotifyHandles[LobbyNotifyType.LobbyUpdateReceived].Add(callback.Target, handle);
    }

    public static void RemoveNotifyLobbyMemberUpdateReceived(Action<LobbyMemberUpdateReceivedCallbackInfo> callback)
    {
        if (!LobbyNotifyHandles.ContainsKey(LobbyNotifyType.LobbyMemberUpdateReceived)) return;
        if (!LobbyNotifyHandles[LobbyNotifyType.LobbyMemberUpdateReceived].ContainsKey(callback.Target)) return;
        var handle = LobbyNotifyHandles[LobbyNotifyType.LobbyMemberUpdateReceived][callback.Target];
        var lobbyInterface = EOS.GetPlatformInterface().GetLobbyInterface();
        lobbyInterface.RemoveNotifyLobbyMemberUpdateReceived(handle);
        LobbyNotifyHandles[LobbyNotifyType.LobbyMemberUpdateReceived].Remove(callback.Target);
    }
    
    public static void RemoveNotifyLobbyMemberStatusReceived(Action<LobbyMemberStatusReceivedCallbackInfo> callback)
    {
        if (!LobbyNotifyHandles.ContainsKey(LobbyNotifyType.LobbyMemberStatusReceived)) return;
        if (!LobbyNotifyHandles[LobbyNotifyType.LobbyMemberStatusReceived].ContainsKey(callback.Target)) return;
        var handle = LobbyNotifyHandles[LobbyNotifyType.LobbyMemberStatusReceived][callback.Target];
        var lobbyInterface = EOS.GetPlatformInterface().GetLobbyInterface();
        lobbyInterface.RemoveNotifyLobbyMemberStatusReceived(handle);
        LobbyNotifyHandles[LobbyNotifyType.LobbyMemberStatusReceived].Remove(callback.Target);
    }
    
    public static void RemoveNotifyLobbyUpdateReceived(Action<LobbyUpdateReceivedCallbackInfo> callback)
    {
        if (!LobbyNotifyHandles.ContainsKey(LobbyNotifyType.LobbyUpdateReceived)) return;
        if (!LobbyNotifyHandles[LobbyNotifyType.LobbyUpdateReceived].ContainsKey(callback.Target)) return;
        var handle = LobbyNotifyHandles[LobbyNotifyType.LobbyUpdateReceived][callback.Target];
        var lobbyInterface = EOS.GetPlatformInterface().GetLobbyInterface();
        lobbyInterface.RemoveNotifyLobbyUpdateReceived(handle);
        LobbyNotifyHandles[LobbyNotifyType.LobbyUpdateReceived].Remove(callback.Target);
    }
}