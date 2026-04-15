using System.Threading;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

public class UnityAuthService : IAuthService
{
    public bool isSignedIn => AuthenticationService.Instance.IsSignedIn;
    public string playerId => AuthenticationService.Instance.PlayerId;

    public async Task<AuthResult> SignInAnonymouslyAsync(CancellationToken ct)
    {
        try
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                await UnityServices.InitializeAsync();
                Debug.Log("UnityServices Initialized in AuthService");
            }

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }
            Debug.Log("Id SignedIn from AuthService: " + AuthenticationService.Instance.PlayerId);
            return new AuthResult{isSuccess = true, playerId = AuthenticationService.Instance.PlayerId};
        }
        catch (System.Exception e)
        {
            Debug.LogError("[UnityService] Sign In Fail: " + e.Message);
            return new AuthResult{isSuccess = false, playerId = AuthenticationService.Instance.PlayerId};
        }
    }
}
