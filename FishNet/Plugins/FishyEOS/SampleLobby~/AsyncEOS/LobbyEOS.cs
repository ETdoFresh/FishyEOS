using System.Threading.Tasks;
using Epic.OnlineServices;
using Epic.OnlineServices.Lobby;
using FishNet.Plugins.FishyEOS.Util;

public static class LobbyEOS
{
    public static async Task<CreateLobbyCallbackInfo> CreateLobbyAsync(ProductUserId localUserId,
        uint maxLobbyMembers, string bucketId = "MyBucket",
        LobbyPermissionLevel permissionLevel = LobbyPermissionLevel.Publicadvertised)
    {
        var createLobbyOptions = new CreateLobbyOptions
        {
            LocalUserId = localUserId,
            MaxLobbyMembers = maxLobbyMembers,
            PermissionLevel = permissionLevel,
            BucketId = bucketId,
        };
        var lobbyInterface = EOS.GetPlatformInterface().GetLobbyInterface();
        var tcs = new TaskCompletionSource<CreateLobbyCallbackInfo>();
        lobbyInterface.CreateLobby(ref createLobbyOptions, null,
            (ref CreateLobbyCallbackInfo callbackInfo) => { tcs.SetResult(callbackInfo); });
        return await tcs.Task;
    }

    public static async Task<UpdateLobbyCallbackInfo> UpdateLobbyAsync(Utf8String lobbyId, Utf8String attrKey,
        Utf8String attrValue, LobbyAttributeVisibility visibility = LobbyAttributeVisibility.Public)
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
        var tcs = new TaskCompletionSource<UpdateLobbyCallbackInfo>();
        lobbyInterface.UpdateLobby(ref updateLobbyOptions, null,
            (ref UpdateLobbyCallbackInfo callbackInfo) => { tcs.SetResult(callbackInfo); });
        return await tcs.Task;
    }

    public static async Task<JoinLobbyCallbackInfo> JoinLobbyAsync(ProductUserId localUserId, LobbyDetails lobbyDetails)
    {
        var joinLobbyOptions = new JoinLobbyOptions
        {
            LobbyDetailsHandle = lobbyDetails,
            LocalUserId = localUserId,
        };
        var lobbyInterface = EOS.GetPlatformInterface().GetLobbyInterface();
        var tcs = new TaskCompletionSource<JoinLobbyCallbackInfo>();
        lobbyInterface.JoinLobby(ref joinLobbyOptions, null,
            (ref JoinLobbyCallbackInfo callbackInfo) => { tcs.SetResult(callbackInfo); });
        return await tcs.Task;
    }

    public static async Task<LeaveLobbyCallbackInfo> LeaveLobbyAsync(string lobbyId, ProductUserId localUserId)
    {
        var leaveLobbyOptions = new LeaveLobbyOptions
        {
            LobbyId = lobbyId,
            LocalUserId = localUserId,
        };
        var lobbyInterface = EOS.GetPlatformInterface().GetLobbyInterface();
        var tcs = new TaskCompletionSource<LeaveLobbyCallbackInfo>();
        lobbyInterface.LeaveLobby(ref leaveLobbyOptions, null,
            (ref LeaveLobbyCallbackInfo callbackInfo) => { tcs.SetResult(callbackInfo); });
        return await tcs.Task;
    }

    public static async
        Task<(LobbySearchFindCallbackInfo lobbySearchFindCallbackInfo, LobbyDetails[] lobbyDetailsArray)>
        SearchLobbiesAsync(ProductUserId localUserId, uint maxResults = 10)
    {
        var createLobbySearchOptions = new CreateLobbySearchOptions { MaxResults = maxResults, };
        var lobbyInterface = EOS.GetPlatformInterface().GetLobbyInterface();
        lobbyInterface.CreateLobbySearch(ref createLobbySearchOptions, out var lobbySearch);
        var lobbySearchFindOptions = new LobbySearchFindOptions { LocalUserId = localUserId, };
        var lobbySearchParameterOptions = new LobbySearchSetParameterOptions
        {
            ComparisonOp = ComparisonOp.Notequal,
            Parameter = new AttributeData
            {
                Key = "NAME",
                Value = new AttributeDataValue { AsUtf8 = "" },
            },
        };
        lobbySearch.SetParameter(ref lobbySearchParameterOptions);
        var tcs = new TaskCompletionSource<LobbySearchFindCallbackInfo>();
        lobbySearch.Find(ref lobbySearchFindOptions, null,
            (ref LobbySearchFindCallbackInfo data) => { tcs.SetResult(data); });
        var lobbySearchFindCallbackInfo = await tcs.Task;
        if (lobbySearchFindCallbackInfo.ResultCode != Result.Success)
        {
            lobbySearch.Release();
            return (lobbySearchFindCallbackInfo, null);
        }

        var getSearchResultCountOptions = new LobbySearchGetSearchResultCountOptions();
        var numberOfResults = lobbySearch.GetSearchResultCount(ref getSearchResultCountOptions);
        var lobbyDetails = new LobbyDetails[numberOfResults];
        for (uint i = 0; i < numberOfResults; i++)
        {
            var copySearchResultByIndexOptions = new LobbySearchCopySearchResultByIndexOptions { LobbyIndex = i };
            var result =
                lobbySearch.CopySearchResultByIndex(ref copySearchResultByIndexOptions, out var lobbyDetailsHandle);
            lobbyDetails[i] = lobbyDetailsHandle;
        }

        lobbySearch.Release();
        return (lobbySearchFindCallbackInfo, lobbyDetails);
    }

    public static Result GetLobbyDetails(Utf8String lobbyId, ProductUserId localUserId,
        out LobbyDetails lobbyDetailsHandle)
    {
        var copyLobbyDetailsHandleOptions = new CopyLobbyDetailsHandleOptions
        {
            LobbyId = lobbyId,
            LocalUserId = localUserId,
        };
        var lobbyInterface = EOS.GetPlatformInterface().GetLobbyInterface();
        return lobbyInterface.CopyLobbyDetailsHandle(ref copyLobbyDetailsHandleOptions, out lobbyDetailsHandle);
    }
}