using UnityEngine;

public class BuildingLights : MonoBehaviour
{
    [Header("Window Settings")]
    public int windowMaterialIndex;
    public Color lightColor = Color.yellow;
    public bool areLightsOn = true;

    private Color defaultColor;
    private MeshRenderer mr;
    private Material windowMat;

    private void Start()
    {
        mr = GetComponent<MeshRenderer>();
        if (mr == null)
        {
            Debug.LogError("MeshRenderer not found on " + gameObject.name);
            return;
        }

        // Lấy material của cửa sổ
        windowMat = mr.materials[windowMaterialIndex];

        // Ép shader toon (nếu chưa đúng)
        Shader toonShader = Shader.Find("Lpk/LightModel/ToonLightBase");
        if (toonShader == null)
        {
            Debug.LogError("Không tìm thấy shader: Lpk/LightModel/ToonLightBase");
            return;
        }
        windowMat.shader = toonShader;

        // Lưu màu gốc
        defaultColor = windowMat.GetColor("_BaseColor");

        // Áp trạng thái ban đầu
        SetLights(areLightsOn);
    }

    public void SetLights(bool isOn)
    {
        if (windowMat == null) return;

        areLightsOn = isOn;

        // Thay đổi màu sáng/tối, không đổi shader
        windowMat.SetColor("_BaseColor", isOn ? lightColor : defaultColor);

        // (tuỳ chọn) tăng hoặc giảm độ sáng bằng intensity
        float intensity = isOn ? 1.5f : 0.6f;
        if (windowMat.HasProperty("_EmissionColor"))
        {
            windowMat.SetColor("_EmissionColor", (isOn ? lightColor : defaultColor) * intensity);
        }
    }
}
