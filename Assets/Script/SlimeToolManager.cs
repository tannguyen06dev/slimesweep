using System.Collections;
using UnityEngine;
using Photon.Pun;
using TMPro;
using UnityEngine.UI;

public class SlimeToolManager : MonoBehaviourPun
{
    // Cài đặt trong Inspector
    [Header("Water Balloon Settings")]
    public GameObject waterBalloonPrefab;
    public Transform launchPoint;
    public float launchForce = 15f;

    [Header("UI Effects - Tag Setup")]
    [Tooltip("Đảm bảo GameObject UI (WetScreenPanel) trong Scene có Tag là 'WetScreenUI'")]
    private const string WET_SCREEN_TAG = "WetScreenUI"; // Tag cố định để tìm UI

    private GameObject wetScreenUI;          // Panel/Image UI màn hình ướt (tìm bằng Tag)
    private CanvasGroup wetScreenCanvasGroup; // CanvasGroup component của wetScreenUI

    [Range(0.1f, 1.0f)]
    public float fadeDuration = 0.2f;      // Thời gian fade in và fade out

    private bool isLaunchMode = false;

    void Start()
    {
        // -------------------------------------------------------------
        // **THAY ĐỔI CHÍNH: TÌM UI VÀ CANVAS GROUP BẰNG TAG**
        // -------------------------------------------------------------
        if (photonView.IsMine)
        {
            // Tìm GameObject UI dựa trên Tag.
            wetScreenUI = GameObject.FindGameObjectWithTag(WET_SCREEN_TAG);

            if (wetScreenUI != null)
            {
                // Lấy CanvasGroup từ GameObject đã tìm thấy
                wetScreenCanvasGroup = wetScreenUI.GetComponent<CanvasGroup>();

                if (wetScreenCanvasGroup == null)
                {
                    Debug.LogError($"WetScreenPanel: Thiếu CanvasGroup component trên đối tượng có Tag '{WET_SCREEN_TAG}'. UI sẽ không fade!");
                    // Nếu không có CanvasGroup, chỉ tắt/bật GameObject
                    wetScreenUI.SetActive(false);
                }
                else
                {
                    // Thiết lập trạng thái ban đầu cho CanvasGroup
                    wetScreenUI.SetActive(false);
                    wetScreenCanvasGroup.alpha = 0;
                    wetScreenCanvasGroup.interactable = false;
                    wetScreenCanvasGroup.blocksRaycasts = false;
                }
            }
            else
            {
                Debug.LogError($"Không tìm thấy Wet Screen UI. Vui lòng kiểm tra và thiết lập Tag là '{WET_SCREEN_TAG}' cho GameObject UI đó trong Scene!");
            }
        }
        // -------------------------------------------------------------
    }

    void Update()
    {
        if (!photonView.IsMine) return;

        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            isLaunchMode = !isLaunchMode;
            Debug.Log($"Water Balloon Mode: {isLaunchMode}");
        }

        if (isLaunchMode && Input.GetMouseButtonDown(0))
        {
            if (waterBalloonPrefab != null && launchPoint != null)
            {
                Debug.Log("Instantiate Called!");
                photonView.RPC("RPC_LaunchWaterBalloon", RpcTarget.All, launchForce);
            }
            else
            {
                Debug.LogError("Thiếu Prefab bóng nước hoặc điểm phóng (Launch Point)!");
            }
        }
    }

    [PunRPC]
    void RPC_LaunchWaterBalloon(float force)
    {
        Vector3 launchDirection = transform.forward;
        float travelSpeed = 10.0f;
        float timeToLive = 5.0f / travelSpeed;

        Vector3 launchVelocity = launchDirection * travelSpeed;

        // KHỞI TẠO BÓNG VÀ ÁP DỤNG LỰC
        GameObject balloon = PhotonNetwork.Instantiate(
            waterBalloonPrefab.name,
            launchPoint.position,
            Quaternion.identity
        );

        WaterBalloonProjectile balloonScript = balloon.GetComponent<WaterBalloonProjectile>();
        if (balloonScript != null)
        {
            balloonScript.Launch(launchVelocity);
            balloonScript.DestroyAfterDelay(timeToLive);
        }
        else
        {
            Debug.LogError("WaterBalloonPrefab thiếu script WaterBalloonProjectile!");
            PhotonNetwork.Destroy(balloon);
        }
    }

    [PunRPC]
    public void RPC_TriggerWetScreenUI(float duration)
    {
        // Sử dụng wetScreenCanvasGroup đã được gán tự động trong Start()
        if (wetScreenCanvasGroup != null && wetScreenUI != null)
        {
            Debug.Log("OUCH! UI Water Splash Active with Fade!");
            StartCoroutine(ShowWetScreenEffect(duration));
        }
        else
        {
            // Log lỗi này chỉ xảy ra nếu việc tìm kiếm thất bại ngay từ đầu trong Start()
            Debug.LogError("Thiếu Canvas Group hoặc Wet Screen UI (tìm kiếm bằng Tag thất bại). Không thể fade.");

            // Fallback nếu không tìm thấy/có CanvasGroup
            if (wetScreenUI != null)
            {
                StartCoroutine(SimpleToggleWetScreenEffect(duration));
            }
        }
    }

    // COROUTINE: HIỂN THỊ VÀ ẨN UI MÀN HÌNH ƯỚT VỚI FADE
    private IEnumerator ShowWetScreenEffect(float displayDuration)
    {
        if (wetScreenCanvasGroup == null || wetScreenUI == null) yield break;

        wetScreenUI.SetActive(true);

        // --- FADE IN ---
        float timer = 0f;
        while (timer < fadeDuration)
        {
            // Sử dụng Lerp: alpha tăng từ 0 lên 1
            wetScreenCanvasGroup.alpha = Mathf.Lerp(0f, 1f, timer / fadeDuration);
            timer += Time.deltaTime;
            yield return null;
        }
        wetScreenCanvasGroup.alpha = 1f;

        // --- HIỂN THỊ TRONG KHOẢNG THỜI GIAN CỐ ĐỊNH ---
        yield return new WaitForSeconds(displayDuration);

        // --- FADE OUT ---
        timer = 0f;
        while (timer < fadeDuration)
        {
            // Sử dụng Lerp: alpha giảm từ 1 xuống 0
            wetScreenCanvasGroup.alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration);
            timer += Time.deltaTime;
            yield return null;
        }
        wetScreenCanvasGroup.alpha = 0f;

        wetScreenUI.SetActive(false);
    }

    // Coroutine dự phòng nếu không có CanvasGroup
    private IEnumerator SimpleToggleWetScreenEffect(float duration)
    {
        if (wetScreenUI != null) wetScreenUI.SetActive(true);
        yield return new WaitForSeconds(duration);
        if (wetScreenUI != null) wetScreenUI.SetActive(false);
    }
}