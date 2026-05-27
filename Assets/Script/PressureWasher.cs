using UnityEngine;
using Photon.Pun;

public class PressureWasher : MonoBehaviourPun
{
    [Header("Weapon Settings")]
    public ParticleSystem washingVFX;
    public Transform rayOrigin;
    public float rayRange = 10f;
    public float pushForce = 10f;

    [Header("Recoil Settings")]
    public float recoilForce = 20f;
    public float recoilDuration = 1f;

    [Header("Camera Shake Settings")]
    public Transform playerCamera;
    public float shakeIntensity = 0.05f;
    public float shakeDuration = 1f;

    [Header("Ray Spread Settings")]
    public float raySpread = 0.1f;

    [Header("Cooldown Settings")]
    public float cooldownTime = 2.5f; // ⏱️ thời gian hồi chiêu (giây)
    private bool isOnCooldown = false;

    private bool isWashingMode = false;
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (washingVFX != null)
            washingVFX.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        if (playerCamera == null && photonView.IsMine)
        {
            Camera cam = Camera.main;
            if (cam != null) playerCamera = cam.transform;
        }

        if (rayOrigin == null)
            rayOrigin = transform;
    }

    void Update()
    {
        if (!photonView.IsMine) return;

        // Bật/tắt chế độ rửa
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            isWashingMode = !isWashingMode;
            photonView.RPC("SetWashingModeRPC", RpcTarget.All, isWashingMode);
        }

        // --- Khi bấm chuột ---
        if (isWashingMode && !isOnCooldown && Input.GetMouseButtonDown(0))
        {
            photonView.RPC("StartWashingVFXRPC", RpcTarget.All, true);
            ShootMultipleRays();
            StartCoroutine(StartCooldown());
        }

        // --- Khi thả chuột ---
        if (isWashingMode && Input.GetMouseButtonUp(0))
        {
            photonView.RPC("StartWashingVFXRPC", RpcTarget.All, false);
        }
    }

    [PunRPC]
    void SetWashingModeRPC(bool enable)
    {
        isWashingMode = enable;

        if (!enable && washingVFX != null && washingVFX.isPlaying)
            washingVFX.Stop(false, ParticleSystemStopBehavior.StopEmitting);
    }

    [PunRPC]
    void StartWashingVFXRPC(bool enable)
    {
        if (washingVFX != null && isWashingMode)
        {
            if (enable)
            {
                washingVFX.Play();
                if (photonView.IsMine)
                {
                    StartCoroutine(ApplyRecoil());
                    StartCoroutine(CameraShake());
                }
            }
            else
            {
                washingVFX.Stop(false, ParticleSystemStopBehavior.StopEmitting);
            }
        }
    }

    private System.Collections.IEnumerator ApplyRecoil()
    {
        if (rb == null) yield break;
        Vector3 recoilDir = -transform.forward.normalized;
        rb.AddForce(recoilDir * recoilForce, ForceMode.Impulse);
        yield break;
    }

    private System.Collections.IEnumerator CameraShake()
    {
        if (playerCamera == null) yield break;

        Vector3 shakeOffset = Vector3.zero;
        float timer = 0f;

        while (timer < shakeDuration)
        {
            // Tạo dao động nhỏ
            Vector3 newShake = new Vector3(
                Random.Range(-1f, 1f) * shakeIntensity,
                Random.Range(-1f, 1f) * shakeIntensity,
                0f
            );

            // Gán dao động vào vị trí camera mà không ghi đè controller
            playerCamera.localPosition -= shakeOffset;  // bỏ dao động cũ
            playerCamera.localPosition += newShake;     // thêm dao động mới

            shakeOffset = newShake; // lưu lại dao động hiện tại để bỏ ra ở frame sau

            timer += Time.deltaTime;
            yield return null;
        }

        // Trả về vị trí gốc
        playerCamera.localPosition -= shakeOffset;
    }

    // --- 🔫 Bắn 5 tia ---
    private void ShootMultipleRays()
    {
        Vector3[] offsets = new Vector3[]
        {
            Vector3.zero,
            new Vector3(raySpread, raySpread, 0),
            new Vector3(-raySpread, raySpread, 0),
            new Vector3(raySpread, -raySpread, 0),
            new Vector3(-raySpread, -raySpread, 0)
        };

        foreach (var offset in offsets)
        {
            Vector3 rayDir = (rayOrigin.forward + rayOrigin.TransformDirection(offset)).normalized;
            Ray ray = new Ray(rayOrigin.position, rayDir);

            if (Physics.Raycast(ray, out RaycastHit hit, rayRange))
            {
                Debug.DrawLine(ray.origin, hit.point, Color.cyan);

                if (hit.collider.CompareTag("Player"))
                {
                    PhotonView targetView = hit.collider.GetComponentInParent<PhotonView>();
                    if (targetView != null && !targetView.IsMine)
                    {
                        Vector3 pushDir = rayDir;
                        targetView.RPC("ApplyPushForceRPC", RpcTarget.All, pushDir * pushForce);
                    }
                }
            }
        }
    }

    // --- ⏳ Thời gian hồi chiêu ---
    private System.Collections.IEnumerator StartCooldown()
    {
        isOnCooldown = true;
        yield return new WaitForSeconds(cooldownTime);
        isOnCooldown = false;
    }
}
