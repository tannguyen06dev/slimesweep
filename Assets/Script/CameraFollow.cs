using UnityEngine;
using Photon.Pun;

public class CameraFollow : MonoBehaviourPun
{
    private Transform target;  // Slime để theo dõi
    public Vector3 offset = new Vector3(0, 5, -6);
    public float followSpeed = 5f;

    void Start()
    {
        // Tìm slime của chính mình khi spawn xong
        InvokeRepeating(nameof(FindLocalPlayer), 0f, 1f);
    }

    void FindLocalPlayer()
    {
        // Tìm tất cả slime trong scene
        SlimeController[] slimes = FindObjectsOfType<SlimeController>();
        foreach (var slime in slimes)
        {
            if (slime.photonView.IsMine) // chỉ slime của mình
            {
                target = slime.transform;
                CancelInvoke(nameof(FindLocalPlayer)); // dừng tìm sau khi có rồi
                break;
            }
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        // Di chuyển camera mượt theo slime
        Vector3 desiredPos = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desiredPos, followSpeed * Time.deltaTime);
        transform.LookAt(target);
    }
}
