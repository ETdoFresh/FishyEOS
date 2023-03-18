using System.Threading.Tasks;
using Epic.OnlineServices;
using Epic.OnlineServices.Auth;
using FishNet.Plugins.FishyEOS.Util;

public static class AuthEOS
{
    public static async Task<LoginCallbackInfo> Login(string id, string token, LoginCredentialType loginCredentialType,
        ExternalCredentialType externalCredentialType, AuthScopeFlags scopeFlags, int timeout)
    {
        var loginOptions = new LoginOptions
        {
            Credentials = new Credentials
            {
                Id = id,
                Token = token,
                Type = loginCredentialType,
                SystemAuthCredentialsOptions = default,
                ExternalType = externalCredentialType
            },
            ScopeFlags = scopeFlags
        };

        var tcs = new TaskCompletionSource<LoginCallbackInfo>();
        EOS.GetCachedAuthInterface().Login(ref loginOptions, null,
            (ref LoginCallbackInfo callbackInfo) => { tcs.SetResult(callbackInfo); });

        var timeoutTask = Task.Delay(timeout * 1000);
        var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);
        if (completedTask == timeoutTask) return new LoginCallbackInfo { ResultCode = Result.TimedOut };
        return await tcs.Task;
    }
}