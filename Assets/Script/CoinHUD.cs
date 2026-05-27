using UnityEngine;
using TMPro;
using System.Collections;
using Photon.Pun;

public class CoinHUD : MonoBehaviour
{
    public TextMeshProUGUI coinText;

    private PlayerCurrency localCurrency;

    void Start()
    {
        if (coinText == null)
        {
            Debug.LogError("CoinText không được assign!");
            return;
        }

        StartCoroutine(WaitAndFindLocalCurrency());
    }

    IEnumerator WaitAndFindLocalCurrency()
    {
        float timeout = 20f; // Max chờ 20 giây
        float elapsed = 0f;

        while (localCurrency == null && elapsed < timeout)
        {
            FindLocalCurrency();

            if (localCurrency != null) break;

            elapsed += 0.5f;
            yield return new WaitForSeconds(0.5f);

            Debug.Log($"Đang chờ PlayerCurrency... (thời gian chờ: {elapsed}s) | InRoom: {PhotonNetwork.InRoom}");
        }

        if (localCurrency == null)
        {
            Debug.LogError("Timeout! Không tìm thấy PlayerCurrency. Kiểm tra: 1. Prefab Slime có PhotonView + PlayerCurrency? 2. Spawn thành công? 3. Tag 'Player' nếu dùng fallback.");
        }
    }

    void FindLocalCurrency()
    {
        PlayerCurrency[] currencies = FindObjectsOfType<PlayerCurrency>();
        Debug.Log($"Tìm thấy {currencies.Length} PlayerCurrency instances.");

        foreach (var c in currencies)
        {
            if (c.photonView != null && c.photonView.IsMine)
            {
                localCurrency = c;
                localCurrency.OnCoinChanged.AddListener(UpdateCoin);
                UpdateCoin(localCurrency.coin);
                Debug.Log($"Kết nối HUD thành công! Player local coin: {localCurrency.coin}");
                return;
            }
        }

        // Fallback offline
        if (!PhotonNetwork.IsConnected && currencies.Length > 0)
        {
            localCurrency = currencies[0];
            localCurrency.OnCoinChanged.AddListener(UpdateCoin);
            UpdateCoin(localCurrency.coin);
            Debug.Log("Offline: Kết nối với PlayerCurrency đầu tiên.");
        }

        // Fallback tag (assign tag "Player" cho prefab Slime)
        if (localCurrency == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                localCurrency = player.GetComponent<PlayerCurrency>();
                if (localCurrency != null)
                {
                    localCurrency.OnCoinChanged.AddListener(UpdateCoin);
                    UpdateCoin(localCurrency.coin);
                    Debug.Log("Fallback tag 'Player': Kết nối thành công.");
                }
            }
        }
    }

    void UpdateCoin(int amount)
    {
        coinText.text = $"{amount} $";
        Debug.Log($"HUD update: {amount} coin");
    }
}