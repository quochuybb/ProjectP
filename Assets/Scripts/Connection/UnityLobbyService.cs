using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
public class UnityLobbyService : ILobbyService
{
    public async Task<Lobby> CreateLobby(string name, int maxPlayers, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (!AuthenticationService.Instance.IsSignedIn)
            throw new InvalidOperationException("Please sign in first");

        var options = new CreateLobbyOptions
        {
            IsPrivate = false,
            Data = new Dictionary<string, DataObject>()
            {
                { "hostId", new DataObject(DataObject.VisibilityOptions.Public, AuthenticationService.Instance.PlayerId) }
            },
            Player = new Player(
                id: AuthenticationService.Instance.PlayerId,
                data: new Dictionary<string, PlayerDataObject>()
                {
                    { "displayName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, AuthenticationService.Instance.PlayerName ?? AuthenticationService.Instance.PlayerId) }
                })
        };

        try
        {

            var lobby = await LobbyService.Instance.CreateLobbyAsync(name, maxPlayers, options);
            Debug.Log($"CreateLobby success id={lobby.Id}, joinCode={lobby.LobbyCode}");
            return lobby;
        }
        catch (Exception ex)
        {
            Debug.LogError($"CreateLobby failed: {ex.Message}");
            throw;
        }
    }

    public async Task<Lobby> GetLobbyById(string lobbyId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (string.IsNullOrEmpty(lobbyId)) throw new ArgumentNullException(nameof(lobbyId));

        try
        {
            var lobby = await LobbyService.Instance.GetLobbyAsync(lobbyId);
            return lobby;
        }
        catch (Exception ex)
        {
            Debug.LogError($"GetLobbyById failed: {ex.Message}");
            throw;
        }
    }

    public async Task<Lobby> JoinLobbyByJoinCode(string joinCode, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(joinCode)) throw new ArgumentNullException(nameof(joinCode));
        if (!AuthenticationService.Instance.IsSignedIn) throw new InvalidOperationException("Please sign in first");

        try
        {
            var lobby = await LobbyService.Instance.JoinLobbyByCodeAsync(joinCode);
            Debug.Log($"Join lobby by join code success: join code={lobby.LobbyCode}");
            return lobby;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Join lobby by join code failed: {ex.Message}");
            throw;
        }
    }
    public async Task UpdateLobby(string lobbyId, Dictionary<string, DataObject> metadata, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (string.IsNullOrEmpty(lobbyId)) throw new ArgumentNullException(nameof(lobbyId));
        if (metadata == null || metadata.Count == 0) return;

        var update = new UpdateLobbyOptions
        {
            HostId = AuthenticationService.Instance.PlayerId,
            Data = new Dictionary<string, DataObject>()
        };

        foreach (var kv in metadata)
        {
            var data = kv.Value;
            update.Data[kv.Key] = new DataObject(
                data.Visibility,
                data.Value,
                data.Index
            );
        }

        try
        {
            await LobbyService.Instance.UpdateLobbyAsync(lobbyId, update);
            Debug.Log($"UpdateLobby success for {lobbyId}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"UpdateLobby failed: {ex.Message}");
        }
    }


    public async Task LeaveLobby(string lobbyId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (string.IsNullOrEmpty(lobbyId)) return;
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            Debug.Log("Not signed in when leaving lobby");
            return;
        }

        var playerId = AuthenticationService.Instance.PlayerId;

        try
        {
            await LobbyService.Instance.RemovePlayerAsync(lobbyId, playerId);

            Debug.Log($"Left lobby {lobbyId} as {playerId}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"LeaveLobby failed: {ex.Message}");
        }
    }
    public LobbyInfo MapLobbyToLobbyInfo(Lobby lobby)
    {
        var info = new LobbyInfo
        {
            lobbyId = lobby.Id,
            joinCode = lobby.LobbyCode,
            Players = new List<AccountUser>(),
            Metadata = new Dictionary<string, string>()
        };

        if (lobby.Players != null)
        {
            foreach (var p in lobby.Players)
                info.Players.Add(new AccountUser(p.Id) { DisplayName = p.Data != null && p.Data.ContainsKey("displayName") ? p.Data["displayName"].Value : p.Id });
        }

        if (lobby.Data != null)
        {
            foreach (var kv in lobby.Data)
            {
                if (!info.Metadata.ContainsKey(kv.Key))
                    info.Metadata[kv.Key] = kv.Value.Value;
            }
        }

        return info;
    }
}
