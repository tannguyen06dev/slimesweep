using System.Linq;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class TrashSpawner : MonoBehaviourPunCallbacks
{
    [Header("Cấu hình spawn")]
    public int trashCount = 30;
    public Vector3 areaSize = new Vector3(10f, 0f, 10f);
    public float spawnHeight = 0.5f;
    public bool randomRotation = true;

    private GameObject[] trashPrefabs;
    private bool hasSpawned = false; // tránh spawn trùng khi scene reload

    void Awake()
    {
        // Load tất cả prefab bắt đầu bằng "Trash" trong Resources
        trashPrefabs = Resources.LoadAll<GameObject>("")
            .Where(p => p.name.StartsWith("Trash"))
            .ToArray();

        if (trashPrefabs.Length == 0)
            Debug.LogWarning("⚠️ Không tìm thấy prefab nào có tên bắt đầu bằng 'Trash' trong Resources!");
    }

    void Start()
    {
        
    }

    public override void OnJoinedRoom()
    {
        Debug.Log($"🟢 [TrashSpawner] OnJoinedRoom() called. IsMasterClient={PhotonNetwork.IsMasterClient}");

        if (PhotonNetwork.IsMasterClient && !hasSpawned)
        {
            hasSpawned = true;
            SpawnAllTrashNetworked();
        }
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        Debug.Log($"🟠 [TrashSpawner] Master switched to {newMasterClient.NickName}");
        // Nếu Master rời và bạn là Master mới -> spawn bổ sung nếu cần
        if (PhotonNetwork.IsMasterClient && !hasSpawned)
        {
            hasSpawned = true;
            SpawnAllTrashNetworked();
        }
    }

    // --- Spawn cho mạng ---
    void SpawnAllTrashNetworked()
    {
        if (trashPrefabs.Length == 0) return;

        Debug.Log($"[TrashSpawner] 🔵 Spawning {trashCount} trash objects (Network).");

        for (int i = 0; i < trashCount; i++)
        {
            Vector3 pos = GetRandomPosition();
            Quaternion rot = randomRotation ? Random.rotation : Quaternion.identity;

            GameObject prefab = trashPrefabs[Random.Range(0, trashPrefabs.Length)];

            GameObject spawned = PhotonNetwork.Instantiate(prefab.name, pos, rot);

            PhotonView pv = spawned.GetComponent<PhotonView>();
            if (pv != null)
                Debug.Log($"✅ Spawned {prefab.name} with ViewID={pv.ViewID}");
            else
                Debug.LogError($"❌ Spawned {prefab.name} but missing PhotonView!");
        }
    }

    // --- Spawn offline ---
    void SpawnAllTrashLocal()
    {
        if (trashPrefabs.Length == 0) return;

        Debug.Log($"[TrashSpawner] 🧩 Spawning {trashCount} trash objects (Offline).");

        for (int i = 0; i < trashCount; i++)
        {
            Vector3 pos = GetRandomPosition();
            Quaternion rot = randomRotation ? Random.rotation : Quaternion.identity;

            GameObject prefab = trashPrefabs[Random.Range(0, trashPrefabs.Length)];
            GameObject local = Instantiate(prefab, pos, rot);

            Debug.Log($"🟢 Local spawn '{prefab.name}' → '{local.name}'");
        }
    }

    // --- Utility ---
    Vector3 GetRandomPosition()
    {
        Vector3 center = transform.position;
        float x = Random.Range(-areaSize.x / 2f, areaSize.x / 2f);
        float z = Random.Range(-areaSize.z / 2f, areaSize.z / 2f);
        return new Vector3(center.x + x, center.y + spawnHeight, center.z + z);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 1f, 0.2f, 0.3f);
        Gizmos.DrawCube(transform.position, new Vector3(areaSize.x, 0.1f, areaSize.z));
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position, new Vector3(areaSize.x, 0.1f, areaSize.z));
    }
#endif
}
