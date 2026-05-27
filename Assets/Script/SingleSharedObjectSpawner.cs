using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

/// <summary>
/// Spawn ra duy nhất 1 prefab dùng chung cho tất cả client.
/// MasterClient chịu trách nhiệm tạo object.
/// Nếu Master rời đi, Master mới sẽ không spawn thêm (tránh trùng).
/// </summary>
public class SingleSharedObjectSpawner : MonoBehaviourPunCallbacks
{
    [Header("Prefab Settings")]
    [Tooltip("Tên prefab trong thư mục Resources (không cần đuôi .prefab)")]
    public string prefabName = "SharedObject";

    [Tooltip("Vị trí spawn")]
    public Vector3 spawnPosition = Vector3.zero;

    [Tooltip("Hướng quay spawn")]
    public Vector3 spawnRotation = Vector3.zero;

    private GameObject sharedInstance; // tham chiếu object đã spawn
    private bool hasSpawned = false;

    void Start()
    {
        if (PhotonNetwork.InRoom)
        {
            TrySpawn();
        }
    }

    public override void OnJoinedRoom()
    {
        Debug.Log($"[SingleSharedObjectSpawner] OnJoinedRoom. IsMasterClient={PhotonNetwork.IsMasterClient}");
        TrySpawn();
    }

    void TrySpawn()
    {
        // chỉ master client mới spawn
        if (PhotonNetwork.IsMasterClient && !hasSpawned)
        {
            hasSpawned = true;
            SpawnSharedObject();
        }
    }

    void SpawnSharedObject()
    {
        if (string.IsNullOrEmpty(prefabName))
        {
            Debug.LogError("[SingleSharedObjectSpawner] ❌ prefabName chưa được đặt!");
            return;
        }

        Debug.Log($"[SingleSharedObjectSpawner] 🟢 Spawning shared object '{prefabName}'...");
        sharedInstance = PhotonNetwork.Instantiate(prefabName, spawnPosition, Quaternion.Euler(spawnRotation));

        PhotonView pv = sharedInstance.GetComponent<PhotonView>();
        if (pv)
            Debug.Log($"✅ Shared object spawned with ViewID={pv.ViewID}");
        else
            Debug.LogWarning("⚠️ Shared object không có PhotonView! Không thể đồng bộ network.");
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        Debug.Log($"[SingleSharedObjectSpawner] Master switched → {newMasterClient.NickName}");

        // Không spawn thêm — object cũ vẫn tồn tại, photon tự giữ reference
        // Nhưng có thể thêm logic nếu bạn muốn Master mới kiểm soát shared object.
    }
}
