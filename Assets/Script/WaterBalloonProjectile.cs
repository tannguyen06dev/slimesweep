using UnityEngine;
using Photon.Pun;
using System.Collections; // Cần thiết cho Coroutine

public class WaterBalloonProjectile : MonoBehaviour
{
    private const float SplashDuration = 2.0f; // Thời gian UI ướt
    private Rigidbody rb;
    private Collider ownCollider;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        ownCollider = GetComponent<Collider>();
    }

    // Khởi tạo vận tốc ban đầu (Chỉ gọi từ SlimeToolManager)
    public void Launch(Vector3 velocity)
    {
        if (rb == null)
        {
            Debug.LogError("Rigidbody is missing on WaterBalloon Prefab!");
            return;
        }

        // QUAN TRỌNG: Áp dụng lực ở chế độ VelocityChange để thiết lập vận tốc tức thời
        rb.AddForce(velocity, ForceMode.VelocityChange);

        // Tránh va chạm với chính người chơi vừa phóng ra (tạm thời)
        StartCoroutine(IgnoreSelfCollision(0.1f));

        // Tự hủy sau 3 giây nếu không trúng ai (tránh rác mạng)
        Destroy(gameObject, 3f);
    }

    // Coroutine tạm thời bỏ qua va chạm với người phóng
    private IEnumerator IgnoreSelfCollision(float delay)
    {
        // Giả định rằng Player có một Collider nào đó
        // (Đây là cách đơn giản, cần cải tiến bằng cách truyền PhotonView của người phóng)

        // Trong trường hợp này, chúng ta chỉ delay va chạm cho đến khi nó bay ra xa người chơi.
        yield return new WaitForSeconds(delay);
        // Sau khi delay, hệ thống va chạm hoạt động bình thường
    }


    void OnCollisionEnter(Collision collision)
    {
        // Chỉ xử lý va chạm trên Client sở hữu Prefab (thường là Master Client)
        // Dùng PhotonView của quả bóng để kiểm tra.
        PhotonView pv = GetComponent<PhotonView>();
        if (pv == null || !pv.IsMine)
        {
            // Nếu không phải Client sở hữu bóng, không xử lý va chạm để tránh nhân bản RPC
            return;
        }

        // Lấy PhotonView của đối tượng va chạm
        PhotonView targetPV = collision.gameObject.GetComponentInParent<PhotonView>();

        // Kiểm tra xem đối tượng có tag "Player" không (Hoặc kiểm tra Component Slime Controller)
        if (collision.gameObject.CompareTag("Player") || (targetPV != null && targetPV.gameObject.CompareTag("Player")))
        {
            if (targetPV != null)
            {
                Debug.Log($"Bóng nước trúng Player: {targetPV.Owner.NickName}");
                // Gửi RPC chỉ đến người chơi bị trúng để kích hoạt UI
                targetPV.RPC("RPC_TriggerWetScreenUI", targetPV.Owner, SplashDuration);
            }
        }

        // Luôn phá hủy quả bóng sau khi va chạm (dù trúng hay trượt)
        PhotonNetwork.Destroy(gameObject);
    }
    public void DestroyAfterDelay(float delay)
    {
        // Kiểm tra để đảm bảo chỉ Client sở hữu (thường là Master) mới gọi PhotonNetwork.Destroy
        PhotonView pv = GetComponent<PhotonView>();
        if (pv != null && pv.IsMine)
        {
            // Sử dụng PhotonNetwork.Destroy để đồng bộ việc hủy
            // Chúng ta phải dùng Coroutine để chờ đợi.
            StartCoroutine(DelayedPhotonDestroy(delay));
        }
    }

    private IEnumerator DelayedPhotonDestroy(float delay)
    {
        yield return new WaitForSeconds(delay);

        // Chỉ hủy nếu đối tượng vẫn còn tồn tại
        if (gameObject != null)
        {
            PhotonNetwork.Destroy(gameObject);
        }
    }
}