using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Relay.Models; 

public class TransportConfigurator
{
    private readonly UnityTransport _transport;
    private readonly string _connectionType = "dtls"; 

    public TransportConfigurator(UnityTransport transport)
    {
        _transport = transport;
    }

    public Task ApplyHostAllocationAsync(Allocation allocation, CancellationToken ct)
    {
        if (_transport == null) throw new System.ArgumentNullException(nameof(_transport));
        if (allocation == null) throw new System.ArgumentNullException(nameof(allocation));

        try
        {
            RelayServerEndpoint endpoint = null;
            if (allocation.ServerEndpoints != null && allocation.ServerEndpoints.Count > 0)
            {
                endpoint = allocation.ServerEndpoints.FirstOrDefault(e => e.ConnectionType == _connectionType)
                           ?? allocation.ServerEndpoints.First(); 
            }

            string host = endpoint?.Host ?? allocation.RelayServer?.IpV4; 
            ushort port = (ushort)(endpoint?.Port ?? allocation.RelayServer?.Port ?? 0);

            var allocationIdBytes = allocation.AllocationIdBytes; 
            var key = allocation.Key; 
            var connectionData = allocation.ConnectionData;

            byte[] hostConnectionDataBytes = null;

            _transport.SetRelayServerData(host, port,
                                          allocationIdBytes,
                                          key,
                                          connectionData,
                                          hostConnectionDataBytes,
                                          isSecure: endpoint?.Secure ?? false);

            Debug.Log("[TransportConfigurator] ApplyHostAllocation: set relay server data (" + host + ":" + port + ")");
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[TransportConfigurator] ApplyHostAllocation failed: " + ex.Message);
            throw;
        }

        return Task.CompletedTask;
    }

    public Task ApplyClientJoinDataAsync(JoinAllocation joinAlloc, CancellationToken ct)
    {
        if (_transport == null) throw new System.ArgumentNullException(nameof(_transport));
        if (joinAlloc == null) throw new System.ArgumentNullException(nameof(joinAlloc));

        try
        {
            RelayServerEndpoint endpoint = null;
            if (joinAlloc.ServerEndpoints != null && joinAlloc.ServerEndpoints.Count > 0)
            {
                endpoint = joinAlloc.ServerEndpoints.FirstOrDefault(e => e.ConnectionType == _connectionType)
                           ?? joinAlloc.ServerEndpoints.First();
            }

            string host = endpoint?.Host ?? joinAlloc.Region; 
            ushort port = (ushort)(endpoint?.Port ?? 0);

            var allocationIdBytes = joinAlloc.AllocationIdBytes; 
            var key = joinAlloc.Key; 
            var connectionData = joinAlloc.ConnectionData; 
            var hostConnectionData = joinAlloc.HostConnectionData; 

            _transport.SetRelayServerData(host, port,
                                          allocationIdBytes,
                                          key,
                                          connectionData,
                                          hostConnectionData,
                                          isSecure: endpoint?.Secure ?? false);

            Debug.Log("[TransportConfigurator] ApplyClientJoinData: set relay server data (" + host + ":" + port + ")");
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[TransportConfigurator] ApplyClientJoinData failed: " + ex.Message);
            throw;
        }

        return Task.CompletedTask;
    }

    public void ApplyLocalLoopback(ushort port = 7777)
    {
        if (_transport == null) throw new System.ArgumentNullException(nameof(_transport));
        _transport.SetConnectionData("127.0.0.1", port);
        Debug.Log("[TransportConfigurator] Applied local loopback: 127.0.0.1:" + port);
    }
}
