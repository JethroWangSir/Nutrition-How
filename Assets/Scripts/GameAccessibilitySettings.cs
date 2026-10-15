using UnityEngine;

public static class GameAccessibilitySettings
{
    public const string EffectsVolumePreference = "settings.effectsVolumeDb";
    public const string ReducedFlashingPreference = "settings.reducedFlashing";
    public const float DefaultEffectsVolumeDb = 0f;

    public static float EffectsVolumeDb => Mathf.Clamp(
        PlayerPrefs.GetFloat(EffectsVolumePreference,DefaultEffectsVolumeDb),
        volumeManager.MinimumVolumeDb,
        volumeManager.MaximumVolumeDb);

    public static float EffectsVolumeLinear
    {
        get
        {
            float volumeDb=EffectsVolumeDb;
            return volumeDb<=volumeManager.MinimumVolumeDb
                ? 0f
                : Mathf.Pow(10f,volumeDb/20f);
        }
    }

    public static bool ReducedFlashingEnabled =>
        PlayerPrefs.GetInt(ReducedFlashingPreference,0)==1;

    public static void SetEffectsVolume(float volumeDb)
    {
        PlayerPrefs.SetFloat(
            EffectsVolumePreference,
            Mathf.Clamp(volumeDb,volumeManager.MinimumVolumeDb,volumeManager.MaximumVolumeDb));
        PlayerPrefs.Save();
    }

    public static void SetReducedFlashing(bool enabled)
    {
        PlayerPrefs.SetInt(ReducedFlashingPreference,enabled ? 1 : 0);
        PlayerPrefs.Save();
    }
}
