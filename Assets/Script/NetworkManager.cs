using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using System.Collections.Generic;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    [Header("UI References")]
    public TMP_InputField roomInputField;
    public TextMeshProUGUI statusText;
    public GameObject lobbyUI;

    [Header("Slime Prefabs (Resources paths)")]
    private string[] slimePrefabs = {
        "SlimePrefabs/Slime1",
        "SlimePrefabs/Slime2",
        "SlimePrefabs/Slime3",
        "SlimePrefabs/Slime4",
        "SlimePrefabs/Slime5",
        "SlimePrefabs/Slime6",
        "SlimePrefabs/Slime7",
        "SlimePrefabs/Slime8",
        "SlimePrefabs/Slime9",
        "SlimePrefabs/Slime10"
    };

    // Danh sách prefab đã dùng trong phòng (được đồng bộ qua RPC)
    private static List<int> usedIndices = new List<int>();

    void Start()
    {
        
        PhotonNetwork.ConnectUsingSettings();
        PhotonNetwork.AutomaticallySyncScene = true;
        statusText.text = "Connecting to Photon...";
    }

    public override void OnConnectedToMaster()
    {
        statusText.text = "Connected to server!";
        PhotonNetwork.JoinLobby(); // đảm bảo ở lobby sẵn
    }

    public void CreateRoom()
    {
        string roomName = string.IsNullOrEmpty(roomInputField.text)
            ? "Room" + Random.Range(1000, 9999)
            : roomInputField.text;
        PhotonNetwork.CreateRoom(roomName, new RoomOptions { MaxPlayers = 10 });
        statusText.text = "Creating room: " + roomName;
    }

    public void JoinRoom()
    {
        if (string.IsNullOrEmpty(roomInputField.text))
        {
            statusText.text = "Room name cannot be empty!";
            return;
        }
        PhotonNetwork.JoinRoom(roomInputField.text);
        statusText.text = "Joining room: " + roomInputField.text;
    }

    public void JoinRandomRoom()
    {
        PhotonNetwork.JoinRandomRoom();
        statusText.text = "Joining random room...";
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        statusText.text = "No available rooms, creating a new one.";
        PhotonNetwork.CreateRoom("Room" + Random.Range(1000, 9999), new RoomOptions { MaxPlayers = 10 });
    }

    // ✅ KHI MASTER CLIENT TẠO ROOM THÀNH CÔNG → RESET COIN CHO TẤT CẢ
    public override void OnCreatedRoom()
    {
        statusText.text = "Room created! Waiting for players...";
        Debug.Log($"[NetworkManager] Room '{PhotonNetwork.CurrentRoom.Name}' created by MasterClient");

        // 🔥 RESET COIN VỀ 0 CHO TẤT CẢ PLAYER TRONG ROOM NÀY
        if (PhotonNetwork.IsMasterClient)
        {
            ResetAllCoins();
        }
    }

    // ✅ Khi đã vào phòng (join hoặc create)
    public override void OnJoinedRoom()
    {
        PlayerPrefs.SetInt("Coin", 0);
        PlayerPrefs.Save();
        statusText.text = "Joined room: " + PhotonNetwork.CurrentRoom.Name;
        Debug.Log($"[NetworkManager] Joined room '{PhotonNetwork.CurrentRoom.Name}', MasterClient={PhotonNetwork.IsMasterClient}");

        if (lobbyUI != null)
            lobbyUI.SetActive(false);

        // Ẩn chuột để chơi game
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        // Nếu master -> reset danh sách prefab đã dùng
        if (PhotonNetwork.IsMasterClient)
            usedIndices.Clear();

        // Gọi spawn slime
        SpawnUniqueSlime();
    }

    // 🔥 RPC RESET COIN CHO TẤT CẢ PLAYER
    void ResetAllCoins()
    {
        // Tìm tất cả PlayerCurrency trong scene và gọi RPC reset
        PlayerCurrency[] currencies = FindObjectsOfType<PlayerCurrency>();
        foreach (var currency in currencies)
        {
            if (currency.photonView != null)
            {
                currency.photonView.RPC("ResetCoin", RpcTarget.AllBuffered, 0);
                Debug.Log($"Reset coin cho player ViewID: {currency.photonView.ViewID}");
            }
        }
        Debug.Log("🔥 ĐÃ RESET TOÀN BỘ COIN VỀ 0 CHO ROOM MỚI!");
    }

    private void SpawnUniqueSlime()
    {
        // chỉ thực thi nếu thực sự đang trong phòng
        if (!PhotonNetwork.InRoom)
        {
            Debug.LogWarning("❌ SpawnUniqueSlime bị gọi khi chưa ở trong phòng!");
            return;
        }

        Vector3 randomPos = new Vector3(Random.Range(-3, 3), 0, Random.Range(-3, 3));
        int prefabIndex = GetUniqueSlimeIndex();
        string selectedPrefab = slimePrefabs[prefabIndex];

        // Spawn mạng với PhotonNetwork.Instantiate
        GameObject slime = PhotonNetwork.Instantiate(selectedPrefab, randomPos, Quaternion.identity);

        if (slime == null)
        {
            Debug.LogError($"❌ Instantiate FAIL! Prefab path: {selectedPrefab}");
            return;
        }

        PhotonView pv = slime.GetComponent<PhotonView>();
        PlayerCurrency pc = slime.GetComponent<PlayerCurrency>();

        Debug.Log($"✅ Spawned slime: {selectedPrefab} (index {prefabIndex}) | ViewID={pv?.ViewID} | Has PlayerCurrency: {pc != null}");

        // Nếu là master, sync danh sách index đã dùng
        if (PhotonNetwork.IsMasterClient)
        {
            photonView.RPC("SyncUsedIndices", RpcTarget.Others, usedIndices.ToArray());
        }
    }

    private int GetUniqueSlimeIndex()
    {
        List<int> available = new List<int>();
        for (int i = 0; i < slimePrefabs.Length; i++)
        {
            if (!usedIndices.Contains(i))
                available.Add(i);
        }
        int newIndex = available.Count > 0
            ? available[Random.Range(0, available.Count)]
            : Random.Range(0, slimePrefabs.Length); // fallback
        usedIndices.Add(newIndex);
        return newIndex;
    }

    [PunRPC]
    private void SyncUsedIndices(int[] indices)
    {
        usedIndices = new List<int>(indices);
        Debug.Log($"🔄 Synced used indices: [{string.Join(", ", usedIndices)}]");
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log($"{otherPlayer.NickName} left room.");
    }

    public override void OnLeftRoom()
    {
        // hiện lại UI nếu rời phòng
        if (lobbyUI != null)
            lobbyUI.SetActive(true);
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }
}