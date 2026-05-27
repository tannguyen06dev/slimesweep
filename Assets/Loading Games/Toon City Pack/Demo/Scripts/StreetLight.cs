using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StreetLight : MonoBehaviour
{
    [Header("Light Settings")]
    public Light[] lights;               // Các bóng đèn thật
    public bool isOn = true;             // Bật hay tắt
    public Color onColor = Color.yellow; // Màu khi bật
    public Color offColor = Color.gray;  // Màu khi tắt

    private MeshRenderer mr;
    private Material lampMat;

    private void Start()
    {
        mr = GetComponent<MeshRenderer>();
        if (mr == null)
        {
            Debug.LogError("❌ StreetLight: MeshRenderer not found on " + gameObject.name);
            return;
        }

        // Lấy material phần kính (ví dụ materials[1])
        lampMat = mr.materials.Length > 1 ? mr.materials[1] : mr.materials[0];

        // Áp dụng shader toon nếu chưa đúng
        Shader toonShader = Shader.Find("Lpk/LightModel/ToonLightBase");
        if (toonShader == null)
        {
            Debug.LogError("❌ Không tìm thấy shader: Lpk/LightModel/ToonLightBase");
            return;
        }
        lampMat.shader = toonShader;

        // Áp trạng thái ban đầu
        SetLight(isOn);
    }

    public void SetLight(bool turnOn)
    {
        isOn = turnOn;

        if (lampMat == null) return;

        // Đổi màu toon (không đổi shader)
        Color targetColor = turnOn ? onColor : offColor;
        lampMat.SetColor("_BaseColor", targetColor);

        // Nếu shader toon hỗ trợ phát sáng (emission)
        if (lampMat.HasProperty("_EmissionColor"))
        {
            lampMat.SetColor("_EmissionColor", turnOn ? onColor * 1.5f : Color.black);
        }

        // Bật/tắt ánh sáng thật
        foreach (Light l in lights)
        {
            if (l != null)
                l.gameObject.SetActive(turnOn);
        }
    }
}
