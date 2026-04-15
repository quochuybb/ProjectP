// Assets/Scripts/Connection/ConnectionManager.cs
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay.Models;


[RequireComponent(typeof(NetworkManager))]
[RequireComponent(typeof(UnityTransport))]
public class ConnectionManager : MonoBehaviour
{
    [Header("References")]
    public NetworkManager networkManager;
    public UnityTransport unityTransport;
    public LobbyInfo LobbyInfo;
    public string relayCode;
    public int maxPlayers = 2;
    private UnityAuthService unityAuthService;
    private UnityLobbyService unityLobbyService;
    private UnityRelayService unityRelayService;
    private TransportConfigurator transportConfigurator;
    private CancellationTokenSource Cts;
    private ILobbyEvents _lobbyEvents;
    private Lobby _lobby;
    private bool initialized;

    private void Start()
    {
        transportConfigurator = new TransportConfigurator(unityTransport);
        unityAuthService = new UnityAuthService();
        unityLobbyService = new UnityLobbyService();
        unityRelayService = new UnityRelayService();
        Cts = new CancellationTokenSource();
    }

    private async Task SubscribeLobbyEventsAsync(string lobbyId)
    {
        var callBacksLobby = new LobbyEventCallbacks();
        callBacksLobby.LobbyChanged += OnChangedLobby;
        callBacksLobby.KickedFromLobby += OnKickedMember;
        try
        {
            _lobbyEvents = await LobbyService.Instance.SubscribeToLobbyEventsAsync(lobbyId, callBacksLobby);
            Debug.Log("Subscribed to lobby events");

        }
        catch (Exception e)
        { 
            Debug.LogError("Subscribe Lobby Events Fail: " + e.Message);
            throw;
        }
    }

private void OnChangedLobby(ILobbyChanges changes)
{
    if (changes == null)
    {
        Debug.LogWarning("[Lobby] OnChangedLobby called with null changes");
        return;
    }
    try
    {
        if (changes.PlayerJoined.Value != null && changes.PlayerJoined.Changed)
        {
            var joinedList = changes.PlayerJoined.Value;
            if (joinedList != null)
            {
                foreach (var j in joinedList)
                {

                    if (j.Player == null)
                    {
                        Debug.LogWarning($"[Lobby] PlayerJoined entry had null Player (join info: {j})");
                        continue;
                    }

                    Debug.Log($"Player joined: id={j.Player.Id}");
                }
            }
            else
            {
                Debug.LogWarning("[Lobby] changes.PlayerJoined.Value is null");
            }
        }
    }
    catch (Exception ex)
    {
        Debug.LogException(ex);
    }

    if (_lobby == null)
    {
        Debug.LogWarning("[Lobby] _lobby is null — skipping changes.ApplyToLobby. Ensure lobby is initialized before subscription.");
    }
    else
    {
        try
        {
            changes.ApplyToLobby(_lobby);
        }
        catch (Exception ex)
        {
            Debug.LogError("[Lobby] Exception while applying lobby changes. _lobby.Id: " + (_lobby?.Id ?? "<null>"));
            Debug.LogException(ex);
        }
    }

    try
    {
        if (_lobby != null)
        {
            LobbyInfo = unityLobbyService.MapLobbyToLobbyInfo(_lobby);
            OnLobbyUpdated?.Invoke(LobbyInfo);
        }
        else
        {
            Debug.LogWarning("[Lobby] Skipping MapLobbyToLobbyInfo because _lobby is null.");
        }
    }
    catch (Exception ex)
    {
        Debug.LogException(ex);
    }
}

    private void OnKickedMember()
    {
        
        OnLobbyUpdated?.Invoke(LobbyInfo);
    }

    
    public event Action<LobbyInfo> OnLobbyUpdated;

    public async Task PrepareLobbyAsync()
    {
        if (initialized) return;
        try
        {
            Cts.Token.ThrowIfCancellationRequested();
            var playerData = await unityAuthService.SignInAnonymouslyAsync(Cts.Token); 
            Debug.Log("Creating lobby...");
            _lobby = await unityLobbyService.CreateLobby(playerData.playerId, maxPlayers,Cts.Token);
            LobbyInfo = unityLobbyService.MapLobbyToLobbyInfo(_lobby);
            Debug.Log($"Lobby created. id={LobbyInfo.lobbyId}, lobbyJoinCode (share to players)={LobbyInfo.joinCode}");
            initialized = true;
            OnLobbyUpdated?.Invoke(LobbyInfo);
            await SubscribeLobbyEventsAsync(LobbyInfo.lobbyId);
        }
        catch (OperationCanceledException)
        {
            Debug.Log("[ConnectionManager] StartHostAsync canceled.");
            throw;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ConnectionManager] StartHostAsync failed: {ex}");
            try { await StopHostAsync(); } catch { }
            throw;
        }
    }
    public async Task StartGameNetworkAsync(bool localHostPlays = true, CancellationToken externalToken = default)
    {
        if (LobbyInfo == null)
            throw new InvalidOperationException("Lobby not prepared. Call PrepareLobbyAsync() first.");
        var localCts = CancellationTokenSource.CreateLinkedTokenSource(externalToken);
        var ct = localCts.Token;

        try
        {
            ct.ThrowIfCancellationRequested();
            Debug.Log("Creating Relay allocation...");
            var allocation = await unityRelayService.CreateAllocation(Math.Max(1, maxPlayers - 1), ct);

            var joinCode = await unityRelayService.GetJoinCodeAllocation(allocation.AllocationId, ct);
            relayCode = joinCode;
            Debug.Log($"Relay allocation created. joinCode={joinCode}");

            var updateData = new Dictionary<string, DataObject>()
            {
                { "relayJoinCode", new DataObject(DataObject.VisibilityOptions.Member, joinCode) },
                { "hostId", new DataObject(DataObject.VisibilityOptions.Public, AuthenticationService.Instance.PlayerId) }
            };

            await unityLobbyService.UpdateLobby(LobbyInfo.lobbyId, updateData, ct);

            LobbyInfo.Metadata["relayJoinCode"] = joinCode;
            OnLobbyUpdated?.Invoke(LobbyInfo);

            await transportConfigurator.ApplyHostAllocationAsync(allocation, ct);

            if (networkManager == null) networkManager = GetComponent<NetworkManager>();

            if (localHostPlays)
            {
                Debug.Log("Starting Host (local plays)...");
                networkManager.StartHost();
            }
            else
            {
                Debug.Log("Starting Server (no local player)...");
                networkManager.StartServer();
            }

            OnLobbyUpdated?.Invoke(LobbyInfo);
            
        }
        catch (OperationCanceledException)
        {
            Debug.Log("[ConnectionManager] StartGameNetworkAsync canceled.");
            throw;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ConnectionManager] StartGameNetworkAsync failed: {ex}");
            try { await StopHostAsync(); } catch { }
            throw;
        }
        finally
        {
            localCts.Dispose();
        }
    }

    public async Task JoinGameNetworkAsync(CancellationToken externalToken = default)
    {

        try
        {
            if (LobbyInfo == null)
            {
                Debug.LogError("[ConnectionManager] Lobby Info Null");
                return;
            };
            string joinCode = await ReadRelayCode(LobbyInfo.lobbyId);
            var allocation = await unityRelayService.JoinAllocation(joinCode, externalToken);
            await transportConfigurator.ApplyClientJoinDataAsync(allocation, externalToken);
            networkManager.StartClient();
        }
        catch (Exception e)
        {
            Debug.LogError($"[ConnectionManager] JoinGameNetworkAsync failed: {e}");
            throw;
        }
    }

    public async Task<String> ReadRelayCode(string lobbyId)
    {
        var lobbyInfo = await unityLobbyService.GetLobbyById(lobbyId,Cts.Token);
        if (this.LobbyInfo != null && lobbyInfo.Data.TryGetValue("relayJoinCode", out var value))
        {
            return value.Value;
        }

        return null;
    }
    

    public async Task JoinLobbyAsync(string lobbyId, CancellationToken externalToken = default)
    {
        if (initialized) return;

        try
        {
            var localCts = CancellationTokenSource.CreateLinkedTokenSource(externalToken);
            var ct = localCts.Token;
            var playerData = await unityAuthService.SignInAnonymouslyAsync(ct); 
            Debug.Log("Join lobby...");
            _lobby = await unityLobbyService.JoinLobbyByJoinCode(lobbyId, ct);
            LobbyInfo = unityLobbyService.MapLobbyToLobbyInfo(_lobby);
            Debug.Log($"Join lobby. id={LobbyInfo.lobbyId}, lobbyJoinCode (share to players)={LobbyInfo.joinCode}");
            initialized = true;
            OnLobbyUpdated?.Invoke(LobbyInfo);
        }
        catch (OperationCanceledException)
        {
            Debug.Log("[ConnectionManager] JoinLobby canceled.");
            throw;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ConnectionManager] JoinLobby failed: {ex}");
            try { await StopHostAsync(); } catch { }
            throw;
        }
    }

    public async Task StopHostAsync()
    {
        try
        {
            if (Cts != null)
            {
                Cts.Cancel();
                Cts.Dispose();
                Cts = null;
            }
        }
        catch { }
        try
        {
            if (networkManager != null && (networkManager.IsHost || networkManager.IsServer))
            {
                networkManager.Shutdown();
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Error shutting down network: {ex.Message}");
        }

        try
        {
            if (LobbyInfo != null && LobbyInfo.lobbyId == AuthenticationService.Instance.PlayerId)
            {
                await unityLobbyService.LeaveLobby(LobbyInfo.lobbyId, CancellationToken.None);
                Debug.Log($"Deleted lobby {LobbyInfo.lobbyId}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"DeleteLobby failed: {ex.Message}");
        }

        initialized = false;
        LobbyInfo = null;
        relayCode = null;
        OnLobbyUpdated?.Invoke(null);
    }

    
}
