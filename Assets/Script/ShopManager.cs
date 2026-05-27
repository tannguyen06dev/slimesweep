using UnityEngine;

public class ShopManager : MonoBehaviour
{
    // Kéo và thả Panel UI Menu Shop của bạn vào trường này trong Inspector
    public GameObject shopPanel;

    // Biến để theo dõi trạng thái hiện tại của Panel
    private bool isShopOpen = false;

    void Start()
    {
        // Đảm bảo Panel Shop ban đầu được ẩn (Disable)
        if (shopPanel != null)
        {
            shopPanel.SetActive(isShopOpen);
        }

        // Thiết lập ban đầu: Khóa và ẩn con trỏ chuột
        // Cursor.lockState = CursorLockMode.Locked;
        // Cursor.visible = false;
    }

    void Update()
    {
        // Kiểm tra xem người chơi có nhấn phím Tab hay không
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            // Gọi hàm để thay đổi trạng thái UI
            TogglePanel();
        }
    }

    // Hàm để bật/tắt Panel
    void TogglePanel()
    {
        // Đảo ngược trạng thái hiện tại (Nếu đang mở -> đóng, nếu đang đóng -> mở)
        isShopOpen = !isShopOpen;

        // Thiết lập trạng thái hoạt động của GameObject (Panel)
        if (shopPanel != null)
        {
            shopPanel.SetActive(isShopOpen);

            // Quản lý con trỏ chuột và Time Scale (Tùy chọn)
            if (isShopOpen)
            {
                // Mở Shop: Hiện con trỏ chuột và cho phép thao tác
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                // Time.timeScale = 0f; // Tùy chọn: Tạm dừng game khi mở shop
            }
            else
            {
                // Đóng Shop: Ẩn/Khóa con trỏ chuột để tiếp tục chơi
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                // Time.timeScale = 1f; // Tùy chọn: Tiếp tục game
            }
        }
    }
}