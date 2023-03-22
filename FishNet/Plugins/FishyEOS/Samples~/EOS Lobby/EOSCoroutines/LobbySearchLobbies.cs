using System.Collections;
using Epic.OnlineServices;
using Epic.OnlineServices.Lobby;
using FishNet.Plugins.FishyEOS.Util;
using UnityEngine;

public class LobbySearchLobbies
{
    public LobbySearchFindCallbackInfo? CallbackInfo { get; private set; }
    public LobbyDetails[] LobbyDetailsArray { get; private set; }
    
    public static Coroutine Run(out LobbySearchLobbies lobbySearchLobbies, ProductUserId localUserId, uint maxResults = 10)
    {
        lobbySearchLobbies = new LobbySearchLobbies();
        return EOS.GetManager().StartCoroutine(lobbySearchLobbies.SearchLobbiesCoroutine(localUserId, maxResults));
    }
    
    private IEnumerator SearchLobbiesCoroutine(ProductUserId localUserId, uint maxResults)
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
        
        lobbySearch.Find(ref lobbySearchFindOptions, null,
            (ref LobbySearchFindCallbackInfo data) => { CallbackInfo = data; });
        
        yield return new WaitUntilOrTimeout(() => CallbackInfo.HasValue, 10,
            () => CallbackInfo = new LobbySearchFindCallbackInfo { ResultCode = Result.TimedOut });
        
        if (CallbackInfo?.ResultCode != Result.Success)
        {
            lobbySearch.Release();
            yield break;
        }

        var getSearchResultCountOptions = new LobbySearchGetSearchResultCountOptions();
        var numberOfResults = lobbySearch.GetSearchResultCount(ref getSearchResultCountOptions);
        LobbyDetailsArray = new LobbyDetails[numberOfResults];
        for (uint i = 0; i < numberOfResults; i++)
        {
            var copySearchResultByIndexOptions = new LobbySearchCopySearchResultByIndexOptions { LobbyIndex = i };
            var result =
                lobbySearch.CopySearchResultByIndex(ref copySearchResultByIndexOptions, out var lobbyDetailsHandle);
            LobbyDetailsArray[i] = lobbyDetailsHandle;
        }

        lobbySearch.Release();
    }
}