using System.Collections;
using Epic.OnlineServices;
using Epic.OnlineServices.Lobby;
using FishNet.Plugins.FishyEOS.Util;
using UnityEngine;

public class LobbyUpdateLobby
{
    public UpdateLobbyCallbackInfo? CallbackInfo { get; private set; }

    public static Coroutine Run(out LobbyUpdateLobby lobbyUpdateLobby, string lobbyId, Utf8String attrKey,
        Utf8String attrValue, LobbyAttributeVisibility visibility = LobbyAttributeVisibility.Public,
        float timeout = 30f)
    {
        lobbyUpdateLobby = new LobbyUpdateLobby();
        return EOS.GetManager()
            .StartCoroutine(lobbyUpdateLobby.UpdateLobby(lobbyId, attrKey, attrValue, visibility, timeout));
    }

    private IEnumerator UpdateLobby(string lobbyId, Utf8String attrKey, Utf8String attrValue,
        LobbyAttributeVisibility visibility, float timeout)
    {
        var lobbyInterface = EOS.GetPlatformInterface().GetLobbyInterface();
        var updateLobbyModificationOptions = new UpdateLobbyModificationOptions
        {
            LobbyId = lobbyId,
            LocalUserId = EOS.LocalProductUserId,
        };
        lobbyInterface.UpdateLobbyModification(ref updateLobbyModificationOptions, out var lobbyModification);
        var addAttributeOptions = new LobbyModificationAddAttributeOptions
        {
            Attribute = new AttributeData
            {
                Key = attrKey,
                Value = new AttributeDataValue { AsUtf8 = attrValue },
            },
            Visibility = visibility,
        };
        lobbyModification.AddAttribute(ref addAttributeOptions);
        var updateLobbyOptions = new UpdateLobbyOptions
        {
            LobbyModificationHandle = lobbyModification,
        };
        lobbyInterface.UpdateLobby(ref updateLobbyOptions, null,
            (ref UpdateLobbyCallbackInfo callbackInfo) => { CallbackInfo = callbackInfo; });

        yield return new WaitUntilOrTimeout(() => CallbackInfo.HasValue, timeout,
            () => CallbackInfo = new UpdateLobbyCallbackInfo { ResultCode = Result.TimedOut });
    }
}