using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay.Models;

public class AuthResult
{
    public bool isSuccess;
    public string playerId;
}

public interface IAuthService
{
    bool isSignedIn { get; }
    string playerId { get; }
    Task<AuthResult> SignInAnonymouslyAsync(CancellationToken ct);
}

public class LobbyInfo
{
    public string lobbyId;
    public string joinCode;
    public List<AccountUser> Players = new List<AccountUser>();
    public Dictionary<string,string> Metadata;
}

public interface ILobbyService
{
    Task<Lobby> CreateLobby(string name, int maxPlayers, CancellationToken ct);
    Task UpdateLobby(string lobbyId, Dictionary<string,DataObject> metadata, CancellationToken ct);
    Task LeaveLobby(string lobbyId, CancellationToken ct);

}

public interface IRelayService
{
    Task<Allocation> CreateAllocation(int maxConnections, CancellationToken ct);
    Task<string> GetJoinCodeAllocation(System.Guid allocation, CancellationToken ct);
    Task<JoinAllocation> JoinAllocation(string joinCode, CancellationToken ct);
    Task CleanupAllocationAsync(Guid allocationId, CancellationToken ct);

}
