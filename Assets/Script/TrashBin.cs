using UnityEngine;
using Photon.Pun;

public class TrashBin : MonoBehaviourPun
{
    [Header("Cấu hình thùng rác")]
    [Tooltip("Tag của các vật thể rác hợp lệ")]
    public string trashTag = "Trash";
    [Tooltip("Hiệu ứng khi rác bị bỏ vào (tuỳ chọn)")]
    public GameObject collectEffect;
    [Tooltip("Số coin thưởng mỗi lần bỏ rác thành công")]
    public int coinPerTrash = 1;

    private void OnTriggerEnter(Collider other)
    {
        // Kiểm tra nếu vật thể có tag "Trash"
        if (other.CompareTag(trashTag))
        {
            TrashObject trashObj = other.GetComponent<TrashObject>();
            if (trashObj != null && trashObj.ownerActorNr != -1)
            {
                // Tìm PlayerCurrency của owner
                PlayerCurrency targetCurrency = FindPlayerCurrencyByActorNr(trashObj.ownerActorNr);
                if (targetCurrency != null)
                {
                    // Gọi RPC AddCoin trên PlayerCurrency của owner
                    targetCurrency.photonView.RPC("AddCoin", RpcTarget.AllBuffered, coinPerTrash);
                    Debug.Log($"Bỏ rác thành công cho player {trashObj.ownerActorNr}! +{coinPerTrash} coin");
                }
                else
                {
                    Debug.LogWarning($"Không tìm thấy PlayerCurrency cho owner {trashObj.ownerActorNr}");
                }
            }
            else
            {
                Debug.LogWarning("Rác không có owner hoặc TrashObject component!");
            }

            // Xoá rác và spawn effect
            CollectTrash(other.gameObject);
        }
    }

    void CollectTrash(GameObject trash)
    {
        // Spawn hiệu ứng nếu có
        if (collectEffect != null)
        {
            Instantiate(collectEffect, trash.transform.position, Quaternion.identity);
        }
        // Nếu đang trong game Photon
        if (PhotonNetwork.IsConnected)
        {
            // Chỉ MasterClient được phép xoá object Photon
            if (PhotonNetwork.IsMasterClient)
            {
                PhotonNetwork.Destroy(trash);
            }
            else
            {
                // Nếu client thường ném rác vào, gửi yêu cầu cho Master xoá
                PhotonView trashView = trash.GetComponent<PhotonView>();
                if (trashView != null)
                {
                    photonView.RPC("RequestDestroyTrash", RpcTarget.MasterClient, trashView.ViewID);
                }
            }
        }
        else
        {
            // Game offline → xoá bình thường
            Destroy(trash);
        }
    }

    [PunRPC]
    void RequestDestroyTrash(int trashViewID)
    {
        PhotonView trashView = PhotonView.Find(trashViewID);
        if (trashView != null)
        {
            PhotonNetwork.Destroy(trashView.gameObject);
        }
    }

    PlayerCurrency FindPlayerCurrencyByActorNr(int actorNr)
    {
        PlayerCurrency[] currencies = FindObjectsOfType<PlayerCurrency>();
        foreach (var currency in currencies)
        {
            if (currency.photonView.Owner.ActorNumber == actorNr)
            {
                return currency;
            }
        }
        return null;
    }
}