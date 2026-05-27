using UnityEngine;

public class VFXManager : MonoBehaviour
{
    [Header("VFX Components")]
    public ParticleSystem vacuumVFX; // Hệ thống hạt đại diện cho luồng hút

    public void SetVFXActive(bool active)
    {
        if (vacuumVFX == null)
        {
            Debug.LogWarning("Vacuum VFX is not assigned.");
            return;
        }

        if (active)
        {
            if (!vacuumVFX.isPlaying)
            {
                vacuumVFX.Play();
            }
        }
        else
        {
            if (vacuumVFX.isPlaying)
            {
                vacuumVFX.Stop();
            }
        }
    }
}