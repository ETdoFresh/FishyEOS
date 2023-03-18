using System.Collections.Generic;
using System.Threading.Tasks;
using Epic.OnlineServices;
using Epic.OnlineServices.Lobby;
using FishNet.Plugins.FishyEOS.Util;

namespace EOSLobby
{
    public static class LobbyDetailsEOS
    {
        public static Result GetLobbyInfo(LobbyDetails lobbyDetail, out LobbyDetailsInfo? lobbyInfo)
        {
            var lobbyDetailsCopyInfoOptions = new LobbyDetailsCopyInfoOptions();
            return lobbyDetail.CopyInfo(ref lobbyDetailsCopyInfoOptions, out lobbyInfo);
        }

        public static Result GetAttribute(LobbyDetails lobbyDetail, string attrKey, out Attribute? lobbyAttribute)
        {
            var lobbyCopyAttributeByKeyOptions = new LobbyDetailsCopyAttributeByKeyOptions { AttrKey = attrKey };
            return lobbyDetail.CopyAttributeByKey(ref lobbyCopyAttributeByKeyOptions, out lobbyAttribute);
        }

        public static Result GetMemberAttribute(LobbyDetails lobbyDetail, ProductUserId productUserId, string attrKey,
            out Attribute? lobbyAttribute)
        {
            var lobbyCopyMemberAttributeByKeyOptions = new LobbyDetailsCopyMemberAttributeByKeyOptions
            {
                AttrKey = attrKey,
                TargetUserId = productUserId
            };
            return lobbyDetail.CopyMemberAttributeByKey(ref lobbyCopyMemberAttributeByKeyOptions, out lobbyAttribute);
        }

        public static async Task<UpdateLobbyCallbackInfo> SetMemberAttributeAsync(string lobbyId,
            ProductUserId localUserId, string attrKey, string attrValue,
            LobbyAttributeVisibility visibility = LobbyAttributeVisibility.Public)
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
            var tcs = new TaskCompletionSource<UpdateLobbyCallbackInfo>();
            lobbyInterface.UpdateLobby(ref updateLobbyOptions, null,
                (ref UpdateLobbyCallbackInfo callbackInfo) => { tcs.SetResult(callbackInfo); });
            return await tcs.Task;
        }

        public static List<ProductUserId> GetMembers(LobbyDetails lobbyDetails)
        {
            var lobbyDetailsGetMemberCountOptions = new LobbyDetailsGetMemberCountOptions();
            var memberCount = lobbyDetails.GetMemberCount(ref lobbyDetailsGetMemberCountOptions);
            var members = new List<ProductUserId>();
            for (uint i = 0; i < memberCount; i++)
            {
                var lobbyDetailsGetMemberByIndexOptions = new LobbyDetailsGetMemberByIndexOptions { MemberIndex = i };
                var member = lobbyDetails.GetMemberByIndex(ref lobbyDetailsGetMemberByIndexOptions);
                members.Add(member);
            }

            return members;
        }

        public static List<Attribute?> GetMemberAttributes(LobbyDetails lobbyDetails, ProductUserId targetUserId)
        {
            var lobbyDetailsGetMemberAttributeCountOptions = new LobbyDetailsGetMemberAttributeCountOptions
            {
                TargetUserId = targetUserId,
            };
            var memberAttributeCount =
                lobbyDetails.GetMemberAttributeCount(ref lobbyDetailsGetMemberAttributeCountOptions);
            var memberAttributes = new List<Attribute?>();
            for (uint i = 0; i < memberAttributeCount; i++)
            {
                var lobbyDetailsCopyMemberAttributeByIndexOptions = new LobbyDetailsCopyMemberAttributeByIndexOptions
                {
                    TargetUserId = targetUserId,
                    AttrIndex = i,
                };
                var result = lobbyDetails.CopyMemberAttributeByIndex(ref lobbyDetailsCopyMemberAttributeByIndexOptions,
                    out var attribute);
                if (result == Result.Success)
                    memberAttributes.Add(attribute);
            }

            return memberAttributes;
        }

        public static List<Attribute?> GetAttributes(LobbyDetails lobbyDetails)
        {
            var lobbyDetailsGetAttributeCountOptions = new LobbyDetailsGetAttributeCountOptions();
            var attributeCount = lobbyDetails.GetAttributeCount(ref lobbyDetailsGetAttributeCountOptions);
            var attributes = new List<Attribute?>();
            for (uint i = 0; i < attributeCount; i++)
            {
                var lobbyDetailsCopyAttributeByIndexOptions = new LobbyDetailsCopyAttributeByIndexOptions
                    { AttrIndex = i };
                var result =
                    lobbyDetails.CopyAttributeByIndex(ref lobbyDetailsCopyAttributeByIndexOptions, out var attribute);
                if (result == Result.Success)
                    attributes.Add(attribute);
            }

            return attributes;
        }
    }

    public static class LobbyDetailsA
    {
        public static async Task<UpdateLobbyCallbackInfo> SetMemberAttributeAsync(string lobbyId,
            ProductUserId localUserId, string attrKey, string attrValue,
            LobbyAttributeVisibility visibility = LobbyAttributeVisibility.Public)
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
            var tcs = new TaskCompletionSource<UpdateLobbyCallbackInfo>();
            lobbyInterface.UpdateLobby(ref updateLobbyOptions, null,
                (ref UpdateLobbyCallbackInfo callbackInfo) => { tcs.SetResult(callbackInfo); });
            return await tcs.Task;
        }
    }
}