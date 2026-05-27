using UnityEngine;
using Photon.Pun;
using UnityEngine.Events;

public class PlayerCurrency : MonoBehaviourPun
{
    public UnityEvent<int> OnCoinChanged; // Event cho UI
    public UnityEvent<int> OnUnlockChanged; // Event khi unlock thay đổi
    [Header("Tiền tệ")]
    public int coin = 0;
    [Header("Unlock cấu hình (3 skills/items)")]
    public int[] unlockCosts = { 50, 100, 150 };
    [Tooltip("3 scripts cần unlock cho phím 2,3,4")]
    public MonoBehaviour[] unlockables;
    [Tooltip("Phím unlock index 0,1,2 -> phím 2,3,4")]
    public KeyCode[] unlockKeys = { KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4 };
    [Header("Bomb")]
    public int bombCost = 200; // giá để bấm phím 5 spawn bomb
    [Header("Trạng thái sync")]
    public bool[] isUnlocked;

    void Start()
    {
        if (isUnlocked == null || isUnlocked.Length != unlockables.Length)
            isUnlocked = new bool[unlockables.Length];
        for (int i = 0; i < unlockables.Length; i++)
        {
            if (unlockables[i] != null)
                unlockables[i].enabled = false;
        }
    }

    void Update()
    {
        if (!photonView.IsMine) return;
        // 🔥 PHÍM 5 = spawn bomb nếu đủ coin
        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            TrySpawnBomb();
            return;
        }
        // 🎯 PHÍM 2,3,4 = unlock skill
        for (int i = 0; i < unlockKeys.Length; i++)
        {
            if (Input.GetKeyDown(unlockKeys[i]))
                TryUnlock(i);
        }
    }

    void Awake() { LoadData(); }

    // ============================================
    // UNLOCK SKILLS
    // ============================================
    public void TryUnlock(int index)
    {
        if (index >= unlockables.Length) return;
        if (isUnlocked[index])
        {
            Debug.Log($"Đã unlock skill {index} rồi!");
            return;
        }
        if (coin >= unlockCosts[index])
        {
            coin -= unlockCosts[index];
            isUnlocked[index] = true;
            if (unlockables[index] != null)
                unlockables[index].enabled = true;
            photonView.RPC("OnUnlock", RpcTarget.AllBuffered, index);
            Debug.Log($"🪙 Đã unlock skill {index}! Coin còn: {coin}");
        }
        else
        {
            Debug.Log($"❌ Không đủ coin để unlock!");
        }
        SaveData(); OnCoinChanged?.Invoke(coin); OnUnlockChanged?.Invoke(index);
    }

    [PunRPC]
    void OnUnlock(int index)
    {
        if (index >= isUnlocked.Length) return;
        isUnlocked[index] = true;
    }

    // ============================================
    // SPAWN BOMB KHÔNG CẦN UNLOCK
    // ============================================
    void TrySpawnBomb()
    {
        if (coin < bombCost)
        {
            Debug.Log($"❌ Không đủ coin spawn bomb! Cần {bombCost}, bạn có {coin}");
            return;
        }
        // Trừ tiền
        coin -= bombCost;
        SpawnBomb();
        Debug.Log($"💣 Spawn bomb! (-{bombCost} coin) Coin còn: {coin}");
        SaveData(); OnCoinChanged?.Invoke(coin);
    }

    void SpawnBomb()
    {
        GameObject bombPrefab = Resources.Load<GameObject>("BombPrefab");
        if (bombPrefab == null)
        {
            Debug.LogError("❌ Không tìm thấy BombPrefab trong Resources!");
            return;
        }
        Vector3 spawnPos = transform.position + transform.forward * 1.5f;
        Quaternion spawnRot = Quaternion.LookRotation(transform.forward);
        PhotonNetwork.Instantiate("BombPrefab", spawnPos, spawnRot);
    }

    // ============================================
    // COIN
    // ============================================
    [PunRPC]
    public void AddCoin(int amount, PhotonMessageInfo info)
    {
        // BẢO MẬT TUYỆT ĐỐI: Chỉ chủ nhân vật mới được phép cộng coin
        if (!photonView.IsMine) return;
        coin += amount;
        string playerName = photonView.Owner?.NickName ?? "Player";
        Debug.Log($"[COIN] {playerName} +{amount} coin → Tổng: {coin}");
        SaveData(); OnCoinChanged?.Invoke(coin);
    }
    // THÊM VÀO PlayerCurrency.cs (thêm vào cuối class, trước LoadData/SaveData)

    [PunRPC]
    public void ResetCoin(int newAmount)
    {
        // Chỉ MasterClient được phép reset coin (bảo mật)
        if (!PhotonNetwork.IsMasterClient && photonView.IsMine)
        {
            coin = newAmount;
            Debug.Log($"🔥 COIN RESET bởi MasterClient: {coin}");
            SaveData();
            OnCoinChanged?.Invoke(coin);
        }
    }
    void LoadData()
    {
        coin = PlayerPrefs.GetInt("Coin_" + photonView.ViewID, 0); // Per player
        if (isUnlocked == null) isUnlocked = new bool[unlockables.Length];
        for (int i = 0; i < isUnlocked.Length; i++)
        {
            isUnlocked[i] = PlayerPrefs.GetInt($"Unlock_{i}_" + photonView.ViewID, 0) == 1;
            if (isUnlocked[i] && unlockables[i] != null) unlockables[i].enabled = true;
        }
        OnCoinChanged?.Invoke(coin); // Trigger UI ngay
    }

    void SaveData()
    {
        PlayerPrefs.SetInt("Coin_" + photonView.ViewID, coin);
        for (int i = 0; i < isUnlocked.Length; i++)
            PlayerPrefs.SetInt($"Unlock_{i}_" + photonView.ViewID, isUnlocked[i] ? 1 : 0);
        PlayerPrefs.Save();
    }
}