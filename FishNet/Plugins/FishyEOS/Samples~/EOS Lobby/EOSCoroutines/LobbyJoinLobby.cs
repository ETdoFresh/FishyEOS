using System.Collections;
using Epic.OnlineServices;
using Epic.OnlineServices.Lobby;
using FishNet.Plugins.FishyEOS.Util;
using UnityEngine;

public class LobbyJoinLobby
{
    public JoinLobbyCallbackInfo? CallbackInfo { get; private set; }

    public static Coroutine Run(out LobbyJoinLobby lobbyJoinLobby, ProductUserId localUserId, LobbyDetails lobbyDetails,
        float timeout = 30f)
    {
        lobbyJoinLobby = new LobbyJoinLobby();
        return EOS.GetManager().StartCoroutine(lobbyJoinLobby.JoinLobby(localUserId, lobbyDetails, timeout));
    }

    private IEnumerator JoinLobby(ProductUserId localUserId, LobbyDetails lobbyDetails, float timeout)
    {
        var joinLobbyOptions = new JoinLobbyOptions
        {
            LobbyDetailsHandle = lobbyDetails,
            LocalUserId = localUserId,
        };
        var lobbyInterface = EOS.GetPlatformInterface().GetLobbyInterface();
        lobbyInterface.JoinLobby(ref joinLobbyOptions, null,
            (ref JoinLobbyCallbackInfo callbackInfo) => { CallbackInfo = callbackInfo; });

        yield return new WaitUntilOrTimeout(() => CallbackInfo.HasValue, timeout,
            () => CallbackInfo = new JoinLobbyCallbackInfo { ResultCode = Result.TimedOut });
    }
}