using System.Collections;
using Epic.OnlineServices;
using Epic.OnlineServices.Lobby;
using FishNet.Plugins.FishyEOS.Util;
using UnityEngine;

namespace EOSLobby
{
    public class LobbySetMemberAttribute
    {
        public UpdateLobbyCallbackInfo? CallbackInfo { get; private set; }

        public static Coroutine Run(out LobbySetMemberAttribute lobbySetMemberAttribute, string lobbyId,
            ProductUserId localUserId, string attrKey, string attrValue,
            LobbyAttributeVisibility visibility = LobbyAttributeVisibility.Public, float timeout = 30f)
        {
            lobbySetMemberAttribute = new LobbySetMemberAttribute();
            return EOS.GetManager()
                .StartCoroutine(lobbySetMemberAttribute.SetMemberAttributeCoroutine(lobbyId, localUserId, attrKey,
                    attrValue, visibility, timeout));
        }

        private IEnumerator SetMemberAttributeCoroutine(string lobbyId, ProductUserId localUserId, string attrKey,
            string attrValue, LobbyAttributeVisibility visibility, float timeout)
        {
            var lobbyInterface = EOS.GetPlatformInterface().GetLobbyInterface();
            var updateLobbyModificationOptions = new UpdateLobbyModificationOptions()
            {
                LobbyId = lobbyId,
                LocalUserId = localUserId,
            };
            lobbyInterface.UpdateLobbyModification(ref updateLobbyModificationOptions, out var lobbyModification);
            var addMemberAttributeOptions = new LobbyModificationAddMemberAttributeOptions
            {
                Attribute = new AttributeData
                {
                    Key = attrKey,
                    Value = new AttributeDataValue
                    {
                        AsUtf8 = attrValue,
                    },
                },
                Visibility = visibility,
            };
            lobbyModification.AddMemberAttribute(ref addMemberAttributeOptions);
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
}