using UnityEngine;
using Photon.Pun;
using System.Collections;

public class ExplosionBomb : MonoBehaviourPunCallbacks
{
    [Header("Explosion")]
    public ParticleSystem explosionVFX;     // Kéo particle nổ vào đây
    public float explosionForce = 1800f;
    public float explosionRadius = 5f;
    public float upwardsModifier = 3f;
    public float destroyDelay = 2f;         // Thời gian chờ VFX hết rồi mới destroy

    private bool hasExploded = false;

    // CHỈ NỔ KHI CHẠM PLAYER
    private void OnCollisionEnter(Collision collision)
    {
        // Chỉ owner của quả bom mới được quyết định nổ (tránh double explode)
        if (!photonView.IsMine) return;
        if (hasExploded) return;

        // ←←← ĐIỀU KIỆN DUY NHẤT: chỉ nổ khi va chạm đúng tag "Player"
        if (collision.gameObject.CompareTag("Player"))
        {
            hasExploded = true;
            photonView.RPC("RPC_Explode", RpcTarget.All, transform.position);
        }
    }

    [PunRPC]
    void RPC_Explode(Vector3 explosionPos)
    {
        // 1. Phát VFX trên mọi client
        if (explosionVFX != null)
        {
            explosionVFX.transform.position = explosionPos;
            explosionVFX.Play();
        }

        // 2. Đẩy văng TẤT CẢ player trong bán kính
        Collider[] hits = Physics.OverlapSphere(explosionPos, explosionRadius);
        foreach (Collider col in hits)
        {
            if (col.CompareTag("Player"))
            {
                Rigidbody rb = col.attachedRigidbody;
                if (rb != null && !rb.isKinematic)
                {
                    rb.AddExplosionForce(explosionForce, explosionPos, explosionRadius, upwardsModifier, ForceMode.Impulse);
                }
            }
        }

        // 3. Destroy quả bom
        if (photonView.IsMine)
        {
            StartCoroutine(DestroyAfterDelay());
        }
    }


    IEnumerator DestroyAfterDelay()
    {
        yield return new WaitForSeconds(destroyDelay);
        PhotonNetwork.Destroy(gameObject);
    }

    // Xem bán kính trong Scene view
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0f, 0f, 0.35f);
        Gizmos.DrawSphere(transform.position, explosionRadius);
    }
}