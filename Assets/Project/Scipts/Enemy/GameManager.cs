using Unity.Netcode;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }
    public List<Player> AllPlayers { get; private set; } = new();

    private void Awake() {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    public override void OnNetworkSpawn() {
        if (!IsServer) return;

        // Host ve tüm client’lar buradan eklenir
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
    }

    private void OnClientConnected(ulong clientId) {
        var client = NetworkManager.Singleton.ConnectedClients[clientId];
        if (client.PlayerObject != null) {
            var player = client.PlayerObject.GetComponent<Player>();
            AllPlayers.Add(player);
            Debug.Log($"[Server] Player eklendi: ClientId = {clientId}");
        }
    }

    private void OnClientDisconnected(ulong clientId) {
        var client = NetworkManager.Singleton.ConnectedClients[clientId];
        if (client.PlayerObject != null) {
            var player = client.PlayerObject.GetComponent<Player>();
            AllPlayers.Remove(player);
            Debug.Log($"[Server] Player çıkarıldı: ClientId = {clientId}");
        }
    }
}
