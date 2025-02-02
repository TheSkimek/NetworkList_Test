using UnityEngine;
using Unity.Netcode;
using System;
using System.Text;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    private NetworkList<PlayerData> playerDataNetworkList;

    public event EventHandler OnPlayerDataNetworkListChanged;

    private void Awake()
    {
        if(Instance != null)
        {
            Debug.LogWarningFormat($"There was more than one GameManager! -> {transform} - {Instance}");
            
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        playerDataNetworkList = new();
    }

    private void Update()
    {
        CheckListCount();
    }

    private string ConnectionPrefix => IsServer ? "[S]" : "[C]";

    private void CheckListCount()
    {
        if(playerDataNetworkList != null)
        {
            Debug.LogWarningFormat($"{ConnectionPrefix} PLAYERLIST Count {playerDataNetworkList.Count}");
        }

        if(Input.GetKeyDown(KeyCode.F1))
        {
            Debug.LogFormat($"{ConnectionPrefix} Current connected players: {playerDataNetworkList.Count}. Output:");
            foreach(var i in playerDataNetworkList)
            {
                Debug.LogFormat($"{ConnectionPrefix} Client: {i.clientID}-> {i.playerName}");
            }
        }

        if(Input.GetKeyDown(KeyCode.F2))
        {
            Debug.LogFormat($"{ConnectionPrefix} Current connected on Networkmanager {NetworkManager.Singleton.ConnectedClientsList.Count} ");

            foreach(var i in NetworkManager.Singleton.ConnectedClientsList)
            {
                Debug.LogFormat($"{ConnectionPrefix} Client: {i.ClientId}");
            }
        }
    }

    public override void OnNetworkSpawn()
    {
        playerDataNetworkList.OnListChanged += PlayerDataNetworkList_OnListChanged;  
    }

    public override void OnNetworkDespawn()
    {
        NetworkManager.Singleton.ConnectionApprovalCallback -= NetworkManager_ConnectionApprovalCallback;
        NetworkManager.Singleton.OnClientConnectedCallback -= NetworkManager_OnClientConnectedCallback;
        NetworkManager.Singleton.OnClientDisconnectCallback -= NetworkManager_OnClientDisconnectCallback;
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        playerDataNetworkList.OnListChanged -= PlayerDataNetworkList_OnListChanged;
    }

    public void StartHost(string playerName = "You")
    {
        NetworkManager.Singleton.ConnectionApprovalCallback += NetworkManager_ConnectionApprovalCallback;
        NetworkManager.Singleton.OnClientConnectedCallback += NetworkManager_OnClientConnectedCallback;
        NetworkManager.Singleton.OnClientDisconnectCallback += NetworkManager_Server_OnClientDisconnectCallback;
        Debug.LogFormat($"{ConnectionPrefix} Starting Host");
        NetworkManager.Singleton.StartHost();

        Debug.LogFormat($"{ConnectionPrefix} Adding Host to playerList");
        AddToPlayerList(playerName, OwnerClientId);
    }

    public void StartClient()
    {
        Debug.LogFormat($"{ConnectionPrefix} Trying to join game");
        NetworkManager.Singleton.OnClientDisconnectCallback += NetworkManager_Client_OnClientDisconnectCallback;
        NetworkManager.Singleton.OnClientConnectedCallback += Client_OnClientConnectedCallback;
        NetworkManager.Singleton.NetworkConfig.ConnectionData = Encoding.ASCII.GetBytes("Client");
        Debug.LogFormat($"{ConnectionPrefix} Starting Client");
        NetworkManager.Singleton.StartClient();
        
    }

    private void NetworkManager_Server_OnClientDisconnectCallback(ulong clientID)
    {
        for(int i = 0; i < playerDataNetworkList.Count; i++)
        {
            if(playerDataNetworkList[i].clientID == clientID)
            {
                playerDataNetworkList.RemoveAt(i);
            }
        }

        Debug.LogFormat($"{ConnectionPrefix} Somebody disconnected: {clientID}");
    }

    private void NetworkManager_Client_OnClientDisconnectCallback(ulong clientID)
    {
        Debug.LogFormat($"{ConnectionPrefix} Client failed to join and got disconnected");
    }

    private void NetworkManager_ConnectionApprovalCallback(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        string requestPlayerName = Encoding.ASCII.GetString(request.Payload);
        Debug.LogFormat($"{ConnectionPrefix} Server receives request of {requestPlayerName} from {request.ClientNetworkId}; isHost: {request.ClientNetworkId == NetworkManager.Singleton.LocalClientId}");

        if(request.ClientNetworkId == NetworkManager.Singleton.LocalClientId) //If connection is host, always approve
        {
            //Host was already added to playerDataList in StartHost()
            Debug.LogFormat($"{ConnectionPrefix} AUTO APPROVED Host");
            response.Approved = true;
            return;
        }
        else
        {
            //Would do some checking, here will just skip for testing purposes
            if(playerDataNetworkList.Count >= 4)
            {
                response.Approved = false;
                return;
            }

            response.Approved = true;
            AddToPlayerList(requestPlayerName, request.ClientNetworkId);
        }
    }

    private void AddToPlayerList(string playerName, ulong clientId)
    {
        PlayerData newPlayerData = new PlayerData
        {
            clientID = clientId,
            playerName = playerName
        };

        bool check = false;
        Debug.LogFormat($"{ConnectionPrefix} Before check {playerDataNetworkList.Count}");
        foreach(var i in playerDataNetworkList)
        {
            Debug.LogFormat($"{ConnectionPrefix} {i.clientID} vs {clientId} or {newPlayerData.clientID}");
            if(i.clientID == newPlayerData.clientID)
            {
                check = true;
            }
        }
        Debug.LogFormat($"{ConnectionPrefix} Is client already in? => {check}");

        Debug.LogFormat($"{ConnectionPrefix} PlayerData created: {newPlayerData.clientID}-> {newPlayerData.playerName}. Adding to PlayerList");

        Debug.LogFormat($"{ConnectionPrefix} Is list null: {playerDataNetworkList != null}, Count: {playerDataNetworkList.Count}");
        playerDataNetworkList.Add(newPlayerData);
    }


    private void PlayerDataNetworkList_OnListChanged(NetworkListEvent<PlayerData> changeEvent)
    {
        Debug.LogFormat($"{ConnectionPrefix} PLAYERLIST CHANGED {changeEvent.Type} -> Value playerName {changeEvent.Value.playerName}/Client {changeEvent.Value.clientID} -- at Index {changeEvent.Index}, count now: {playerDataNetworkList.Count}");
        OnPlayerDataNetworkListChanged?.Invoke(this, EventArgs.Empty);
    }

    private void NetworkManager_OnClientConnectedCallback(ulong clientID)
    {
        Debug.LogFormat($"{ConnectionPrefix} Client {clientID} connected");
    }

    private void NetworkManager_OnClientDisconnectCallback(ulong clientID)
    { 
        Debug.LogFormat($"{ConnectionPrefix} Client {clientID} disconnected");
    }

    private void Client_OnClientConnectedCallback(ulong obj)
    {
        Debug.LogWarningFormat($"{ConnectionPrefix} Client connection success, list {playerDataNetworkList.Count}");
    }
}
