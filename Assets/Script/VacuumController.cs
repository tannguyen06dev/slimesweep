using UnityEngine;
using Photon.Pun;
using System.Collections.Generic;

[RequireComponent(typeof(VFXManager))]
public class VacuumController : MonoBehaviourPun
{
    [Header("Cấu hình hút")]
    public float suctionRadius = 6f;
    public float suctionSpeed = 10f;
    public float stopDistance = 0.5f;
    public float holdHeight = 1f;
    public float orbitRadius = 1.5f;
    public float orbitSpeed = 80f;
    public int maxSuckedObjects = 10; // ✅ Giới hạn tối đa số vật bị hút
    public Transform suctionPoint;

    private VFXManager vfxManager;
    private bool vacuumMode = false;
    private bool isSucking = false;

    private HashSet<int> suckedObjectIds = new HashSet<int>();
    private Dictionary<int, float> orbitAngles = new Dictionary<int, float>();

    private void Start()
    {
        vfxManager = GetComponent<VFXManager>();
        if (suctionPoint == null) suctionPoint = transform;
    }

    private void Update()
    {
        if (!photonView.IsMine) return;

        // Bật/tắt chế độ hút
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            vacuumMode = !vacuumMode;
            photonView.RPC(nameof(RPC_ToggleVFX), RpcTarget.All, false);

            // Khi tắt chế độ hút => thả toàn bộ vật
            if (!vacuumMode)
                photonView.RPC(nameof(RPC_ReleaseObjects), RpcTarget.All);
        }

        if (!vacuumMode) return;

        // Giữ chuột trái để hút
        if (Input.GetMouseButton(0))
        {
            if (!isSucking)
            {
                isSucking = true;
                photonView.RPC(nameof(RPC_ToggleVFX), RpcTarget.All, true);
            }
            SuckObjects();
        }
        else if (isSucking)
        {
            isSucking = false;
            photonView.RPC(nameof(RPC_ToggleVFX), RpcTarget.All, false);
            photonView.RPC(nameof(RPC_ReleaseObjects), RpcTarget.All);
        }

        UpdateOrbitingObjects();
    }

    private void SuckObjects()
    {
        Collider[] hits = Physics.OverlapSphere(suctionPoint.position, suctionRadius);

        foreach (Collider hit in hits)
        {
            if (hit == null) continue;
            if (suckedObjectIds.Count >= maxSuckedObjects) break; // ✅ Giới hạn tối đa

            // Không hút chính mình
            if (hit.gameObject == this.gameObject) continue;

            // Chỉ hút tag Trash và Player
            if (!hit.CompareTag("Trash") && !hit.CompareTag("Player")) continue;

            PhotonView targetView = hit.GetComponent<PhotonView>();
            if (targetView == null) continue;

            int id = targetView.ViewID;

            if (!suckedObjectIds.Contains(id))
            {
                suckedObjectIds.Add(id);

                // Nếu là Trash thì cho vào danh sách xoay quanh
                if (hit.CompareTag("Trash"))
                    orbitAngles[id] = Random.Range(0f, 360f);
            }

            // Gửi RPC di chuyển object bị hút
            photonView.RPC(nameof(RPC_PullObject), RpcTarget.All, id, suctionPoint.position);
        }
    }

    [PunRPC]
    private void RPC_PullObject(int viewId, Vector3 targetPos)
    {
        PhotonView targetView = PhotonView.Find(viewId);
        if (targetView == null) return;

        Transform t = targetView.transform;

        // Nếu là player => hút nhẹ hơn để không bị "bật"
        float speedMultiplier = targetView.CompareTag("Player") ? 0.3f : 1f;
        float step = suctionSpeed * speedMultiplier * Time.deltaTime;

        Vector3 target = new Vector3(targetPos.x, targetPos.y + holdHeight, targetPos.z);
        float dist = Vector3.Distance(t.position, target);

        if (dist > stopDistance)
        {
            t.position = Vector3.MoveTowards(t.position, target, step);
        }
        else if (targetView.CompareTag("Trash"))
        {
            Rigidbody rb = t.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.useGravity = false;
            }
        }
    }

    private void UpdateOrbitingObjects()
    {
        foreach (var kvp in new Dictionary<int, float>(orbitAngles))
        {
            int viewId = kvp.Key;
            PhotonView targetView = PhotonView.Find(viewId);
            if (targetView == null)
            {
                orbitAngles.Remove(viewId);
                suckedObjectIds.Remove(viewId);
                continue;
            }

            if (!targetView.CompareTag("Trash")) continue;

            Transform t = targetView.transform;

            orbitAngles[viewId] += orbitSpeed * Time.deltaTime;
            float angleRad = orbitAngles[viewId] * Mathf.Deg2Rad;

            Vector3 offset = new Vector3(Mathf.Cos(angleRad), 0, Mathf.Sin(angleRad)) * orbitRadius;
            Vector3 orbitPos = suctionPoint.position + offset + Vector3.up * holdHeight;

            t.position = Vector3.Lerp(t.position, orbitPos, Time.deltaTime * 5f);
        }
    }

    [PunRPC]
    private void RPC_ReleaseObjects()
    {
        foreach (int id in suckedObjectIds)
        {
            PhotonView targetView = PhotonView.Find(id);
            if (targetView == null) continue;

            Rigidbody rb = targetView.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.useGravity = true;
                rb.velocity = Vector3.zero;
            }
        }

        suckedObjectIds.Clear();
        orbitAngles.Clear();
    }

    [PunRPC]
    private void RPC_ToggleVFX(bool active)
    {
        if (vfxManager != null)
            vfxManager.SetVFXActive(active);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(
            suctionPoint != null ? suctionPoint.position : transform.position,
            suctionRadius
        );
    }
}
