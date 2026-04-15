using System;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

public class UnityRelayService : IRelayService
{
    public async Task<Allocation> CreateAllocation(int maxConnections, CancellationToken ct)
    {
        try
        {
            var allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections);
            return allocation;
        }
        catch (Exception e)
        {
            Debug.LogError("[UnityRelayService] Create Allocation Fail: " + e.Message);
            throw;
        }
    }

    public async Task<string> GetJoinCodeAllocation(Guid allocation, CancellationToken ct)
    {
        try
        {
            var joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation);
            return joinCode;
        }
        catch (Exception e)
        { 
            Debug.LogError("[UnityRelayService] GetJoinCodeAllocation Fail: " + e.Message);
            throw;
        }
    }

    public async Task<JoinAllocation> JoinAllocation(string joinCode, CancellationToken ct)
    {
        try
        {
            var allocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
            return allocation;
        }
        catch (Exception e)
        { 
            Debug.LogError("[UnityRelayService] JoinAllocation Fail: " + e.Message);
            throw;
        }
    }
    public async Task CleanupAllocationAsync(Guid allocationId, CancellationToken ct)
    {
        try
        {
            Debug.Log("[UnityRelayServiceWrapper] CleanupAllocation: attempted delete (if supported) or no-op.");
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[UnityRelayServiceWrapper] CleanupAllocation failed or not supported: " + ex.Message);
        }
    }
    
}