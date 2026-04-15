using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;

public class MenuManager : MonoBehaviour
{
    public static MenuManager instance;
    
    [SerializeField] private Button startClientButton;
    [SerializeField] private Button startHostButton;
    [SerializeField] private Button startGameButton;
    [SerializeField] private TMP_InputField inputField;       
    [SerializeField] private TextMeshProUGUI idLobby;         
    [SerializeField] private TextMeshProUGUI playersListText; 
    [SerializeField] private TextMeshProUGUI error;
    [SerializeField] private TMP_Dropdown mapDropdown; 
    
    [Header("Network Refs")]
    [SerializeField] private ConnectionManager connectionManager;
    private bool _isHostMode = false;

    private void Awake()
    {
        instance = this;
        Cursor.visible = true;
    }

    private void Start()
    {
        if (connectionManager == null)
        {
            HandleUIError("Missing ConnectionManager in scene!");
            DisableAllButtons();
            return;
        }

        startHostButton.onClick.AddListener(OnHostClicked);
        startClientButton.onClick.AddListener(OnJoinClicked);
        startGameButton.onClick.AddListener(OnStartGameClicked);

        connectionManager.OnLobbyUpdated += UpdateLobbyUI;

        ResetUI();
    }

    private void OnDestroy()
    {
        if (connectionManager != null)
        {
            connectionManager.OnLobbyUpdated -= UpdateLobbyUI;
        }
    }

    private async void OnHostClicked()
    {
        SetInteractable(false);
        error.text = "";

        try
        {
            _isHostMode = true;
            await connectionManager.PrepareLobbyAsync();
            ValidateLobbyConnection("Host");
        }
        catch (Exception ex) { HandleUIError($"Failed to create lobby: {ex.Message}"); }
    }

    private async void OnJoinClicked()
    {
        SetInteractable(false);
        error.text = "";

        try
        {
            _isHostMode = false;
            await connectionManager.JoinLobbyAsync(inputField.text);
            ValidateLobbyConnection("Client");
        }
        catch (Exception ex) { HandleUIError($"Failed to join lobby: {ex.Message}"); }
    }

    private async void OnStartGameClicked()
    {
        startGameButton.interactable = false;
        error.text = "";

        try
        {
            if (_isHostMode)
            {

                await connectionManager.StartGameNetworkAsync(localHostPlays: true);
                Debug.Log("[UI] Game network started (Host).");
            }
            else
            {
                await connectionManager.JoinGameNetworkAsync();
                Debug.Log("[UI] Game network started (Client).");
            }
        }
        catch (Exception ex) 
        { 
            HandleUIError($"Failed to start game: {ex.Message}"); 
        }
    }

    
    private void ValidateLobbyConnection(string role)
    {
        if (connectionManager.LobbyInfo == null)
        {
            throw new Exception("Lobby info is null after connection attempt.");
        }
        startClientButton.interactable = false;
        startHostButton.interactable = false;
        startGameButton.interactable = true;
    }

    private void UpdateLobbyUI(LobbyInfo info)
    {
        if (info == null)
        {
            ResetUI();
            return;
        }

        idLobby.text = info.joinCode ?? "";
        playersListText.text = FormatPlayers(info);
        startGameButton.interactable = !string.IsNullOrEmpty(idLobby.text);
    }

    private string FormatPlayers(LobbyInfo lobby)
    {
        if (lobby?.Players == null || lobby.Players.Count == 0) return "(0 players)";
        return string.Join("\n", lobby.Players.Select(p => p.DisplayName ?? p.Id));
    }

    private void HandleUIError(string message)
    {
        Debug.LogError($"[UIManager] {message}");
        error.text = message;
        SetInteractable(true); 
    }

    private void SetInteractable(bool enabled)
    {
        startHostButton.interactable = enabled;
        startClientButton.interactable = enabled;
        inputField.interactable = enabled;
    }

    private void ResetUI()
    {
        idLobby.text = "";
        playersListText.text = "(0 players)";
        error.text = "";
        startGameButton.interactable = false;
    }

    private void DisableAllButtons()
    {
        SetInteractable(false);
        startGameButton.interactable = false;
    }
}