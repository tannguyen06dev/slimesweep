using UnityEngine;
using Photon.Pun;

public class ThirdPersonCamera : MonoBehaviourPun
{
    public Transform target;
    public Vector3 offset = new Vector3(0, 3, -5);
    public float mouseSensitivity = 150f;
    public float distance = 5f;
    public float smoothTime = 0.1f;
    private float yaw = 0f;
    private float pitch = 15f;
    private Vector3 currentVelocity;
    private bool uiVisible = false;
    // Biến kéo vật thể
    private Rigidbody grabbedBody;
    private float grabDistance;
    private PhotonView grabbedPhotonView; // Để kiểm tra ownership

    void LateUpdate()
    {
        // --- Tự tìm slime của mình ---
        if (target == null)
        {
            SlimeController[] slimes = FindObjectsOfType<SlimeController>();
            foreach (var slime in slimes)
            {
                if (slime.photonView.IsMine)
                {
                    target = slime.transform;
                    break;
                }
            }
            return;
        }
        // --- ESC để hiện UI ---
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            uiVisible = !uiVisible;
            var networkManager = FindObjectOfType<NetworkManager>();
            if (networkManager != null && networkManager.lobbyUI != null)
                networkManager.lobbyUI.SetActive(uiVisible);
            Cursor.visible = uiVisible;
            Cursor.lockState = uiVisible ? CursorLockMode.None : CursorLockMode.Locked;
        }
        // --- Điều khiển camera ---
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;
        yaw += mouseX;
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, 5f, 60f);
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);
        Vector3 desiredPos = target.position - rotation * Vector3.forward * distance + Vector3.up * 1.5f;
        transform.position = Vector3.SmoothDamp(transform.position, desiredPos, ref currentVelocity, smoothTime);
        transform.LookAt(target.position + Vector3.up * 1.5f);
        // --- Xử lý kéo thả vật ---
        HandleObjectDragging();
    }

    void HandleObjectDragging()
    {
        // Bắt đầu kéo
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 10f))
            {
                Rigidbody rb = hit.collider.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    grabbedBody = rb;
                    grabbedPhotonView = rb.GetComponent<PhotonView>();
                    grabDistance = Vector3.Distance(transform.position, hit.point);
                    // Yêu cầu ownership nếu cần
                    if (grabbedPhotonView != null && !grabbedPhotonView.IsMine)
                        grabbedPhotonView.RequestOwnership();

                    // Nếu là rác, set owner là player local
                    if (rb.CompareTag("Trash"))
                    {
                        TrashObject trashObj = rb.GetComponent<TrashObject>();
                        if (trashObj != null && trashObj.photonView != null)
                        {
                            trashObj.photonView.RPC("SetOwner", RpcTarget.AllBuffered, PhotonNetwork.LocalPlayer.ActorNumber);
                        }
                    }
                }
            }
        }
        // Đang kéo
        if (Input.GetMouseButton(0) && grabbedBody != null)
        {
            Vector3 targetPos = Camera.main.transform.position + Camera.main.transform.forward * grabDistance;
            Vector3 moveDirection = (targetPos - grabbedBody.position) * 10f;
            grabbedBody.velocity = moveDirection;
        }
        // Thả ra (không +coin ở đây, để TrashBin xử lý)
        if (Input.GetMouseButtonUp(0) && grabbedBody != null)
        {
            grabbedBody.velocity = Vector3.zero;
            grabbedBody = null;
            grabbedPhotonView = null;
        }
    }
}