using System.Collections;
using Epic.OnlineServices;
using Epic.OnlineServices.Lobby;
using FishNet.Plugins.FishyEOS.Util;
using UnityEngine;

public class LobbyLeaveLobby
{
    public LeaveLobbyCallbackInfo? CallbackInfo { get; private set; }

    public static Coroutine Run(out LobbyLeaveLobby lobbyLeaveLobby, string lobbyId, ProductUserId localUserId,
        float timeout = 30f)
    {
        lobbyLeaveLobby = new LobbyLeaveLobby();
        return EOS.GetManager().StartCoroutine(lobbyLeaveLobby.LeaveLobby(lobbyId, localUserId, timeout));
    }

    private IEnumerator LeaveLobby(string lobbyId, ProductUserId localUserId, float timeout)
    {
        var leaveLobbyOptions = new LeaveLobbyOptions
        {
            LobbyId = lobbyId,
            LocalUserId = localUserId,
        };
        var lobbyInterface = EOS.GetPlatformInterface().GetLobbyInterface();
        lobbyInterface.LeaveLobby(ref leaveLobbyOptions, null,
            (ref LeaveLobbyCallbackInfo callbackInfo) => { CallbackInfo = callbackInfo; });

        yield return new WaitUntilOrTimeout(() => CallbackInfo.HasValue, timeout,
            () => CallbackInfo = new LeaveLobbyCallbackInfo { ResultCode = Result.TimedOut });
    }
}