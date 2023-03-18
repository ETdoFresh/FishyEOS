using System.Collections;
using Epic.OnlineServices;
using Epic.OnlineServices.Lobby;
using FishNet.Plugins.FishyEOS.Util;
using UnityEngine;

public class LobbyCreateLobby
{
    public CreateLobbyCallbackInfo? CallbackInfo { get; private set; }

    public static Coroutine Run(out LobbyCreateLobby lobbyCreateLobby, ProductUserId localUserId, uint maxLobbyMembers,
        string bucketId = "MyBucket", LobbyPermissionLevel permissionLevel = LobbyPermissionLevel.Publicadvertised,
        float timeout = 30f)
    {
        lobbyCreateLobby = new LobbyCreateLobby();
        return EOS.GetManager()
            .StartCoroutine(lobbyCreateLobby.CreateLobby(localUserId, maxLobbyMembers, bucketId, permissionLevel,
                timeout));
    }

    private IEnumerator CreateLobby(ProductUserId localUserId, uint maxLobbyMembers, string bucketId,
        LobbyPermissionLevel permissionLevel, float timeout)
    {
        var createLobbyOptions = new CreateLobbyOptions
        {
            LocalUserId = localUserId,
            MaxLobbyMembers = maxLobbyMembers,
            PermissionLevel = permissionLevel,
            BucketId = bucketId,
        };
        var lobbyInterface = EOS.GetPlatformInterface().GetLobbyInterface();
        lobbyInterface.CreateLobby(ref createLobbyOptions, null,
            (ref CreateLobbyCallbackInfo callbackInfo) => { CallbackInfo = callbackInfo; });

        yield return new WaitUntilOrTimeout(() => CallbackInfo.HasValue, timeout,
            () => CallbackInfo = new CreateLobbyCallbackInfo { ResultCode = Result.TimedOut });
    }
}