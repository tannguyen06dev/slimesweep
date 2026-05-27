using UnityEngine;
using Photon.Pun;
using System.Collections;

public class SlimeController : MonoBehaviourPun
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float acceleration = 8f;
    public float deceleration = 4f;
    public float rotationSpeed = 10f;
    private Vector3 velocity;

    [Header("Jump Settings")]
    public float jumpForce = 5f;
    private bool canJump = true; // ✅ chỉ ngăn nhảy trong 1 giây sau khi ấn Space

    [Header("VFX Settings")]
    public GameObject moveVFX;
    public ParticleSystem jumpVFX; // ✅ là particle system
    public float vfxActivationThreshold = 0.1f;

    private bool isVFXActive = false;
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null) rb.freezeRotation = true;

        if (moveVFX != null) moveVFX.SetActive(false);
        if (jumpVFX != null) jumpVFX.Stop();
    }

    void Update()
    {
        if (!photonView.IsMine) return;

        HandleMovement();

        // ✅ Nhấn SPACE để nhảy, nhưng chỉ khi được phép
        if (Input.GetKeyDown(KeyCode.Space) && canJump)
        {
            StartCoroutine(JumpAndDelayVFX());
        }

        UpdateMoveVFX();

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private void HandleMovement()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Transform cam = Camera.main.transform;
        Vector3 camForward = Vector3.Scale(cam.forward, new Vector3(1, 0, 1)).normalized;
        Vector3 moveInput = (v * camForward + h * cam.right).normalized;

        if (moveInput.magnitude > 0.1f)
        {
            velocity = Vector3.MoveTowards(velocity, moveInput * moveSpeed, acceleration * Time.deltaTime);
            Quaternion targetRot = Quaternion.LookRotation(moveInput);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
        }
        else
        {
            velocity = Vector3.MoveTowards(velocity, Vector3.zero, deceleration * Time.deltaTime);
        }

        transform.position += velocity * Time.deltaTime;
    }

    private void UpdateMoveVFX()
    {
        if (moveVFX == null) return;

        if (velocity.magnitude > vfxActivationThreshold)
        {
            if (!isVFXActive)
            {
                moveVFX.SetActive(true);
                isVFXActive = true;
            }
        }
        else
        {
            if (isVFXActive)
            {
                moveVFX.SetActive(false);
                isVFXActive = false;
            }
        }
    }

    private IEnumerator JumpAndDelayVFX()
    {
        canJump = false; // ❌ tạm thời không cho nhảy thêm
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);

        // Gửi RPC bật VFX sau 1 giây
        photonView.RPC("TriggerJumpVFX_RPC", RpcTarget.All);

        yield return new WaitForSeconds(1f); // ⏳ đếm ngược 1 giây

        canJump = true; // ✅ cho phép nhảy lại sau khi VFX kích hoạt
    }

    [PunRPC]
    private void TriggerJumpVFX_RPC()
    {
        StartCoroutine(PlayJumpVFXWithDelay());
    }

    private IEnumerator PlayJumpVFXWithDelay()
    {
        yield return new WaitForSeconds(1f); // ⏳ delay 1 giây
        if (jumpVFX != null)
        {
            jumpVFX.Play();
        }
    }

    [PunRPC]
    public void ApplyPushForceRPC(Vector3 force)
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        rb.AddForce(force, ForceMode.Impulse);
    }
}
