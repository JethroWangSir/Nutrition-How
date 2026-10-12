using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class volumeManager : MonoBehaviour
{
    public const string MasterVolumePreference = "settings.masterVolumeDb";
    public const float MinimumVolumeDb = -80f;
    public const float MaximumVolumeDb = 0f;
    public const float DefaultVolumeDb = 0f;

    public AudioMixer myAudioMixer;
    public Slider volumeSlider;

    void Start()
    {
        float savedVolume = GetSavedVolume();
        if(volumeSlider != null)
        {
            volumeSlider.gameObject.SetActive(true);
            EnsureVolumeLabel();
            volumeSlider.SetValueWithoutNotify(savedVolume);
        }
        ApplyVolume(savedVolume);
    }

    public void SetVolume(float sliderValue)
    {
        float volume = Mathf.Clamp(sliderValue,MinimumVolumeDb,MaximumVolumeDb);
        PlayerPrefs.SetFloat(MasterVolumePreference,volume);
        PlayerPrefs.Save();
        ApplyVolume(volume);
    }

    public static float GetSavedVolume()
    {
        return Mathf.Clamp(
            PlayerPrefs.GetFloat(MasterVolumePreference,DefaultVolumeDb),
            MinimumVolumeDb,
            MaximumVolumeDb);
    }

    private void ApplyVolume(float volume)
    {
        if(myAudioMixer != null)
        {
            myAudioMixer.SetFloat("MasterVolume",volume);
        }
    }

    private void EnsureVolumeLabel()
    {
        if(volumeSlider.transform.Find("VolumeLabel") != null)
        {
            return;
        }

        GameObject labelObject = new GameObject(
            "VolumeLabel",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Text));
        RectTransform labelTransform = labelObject.GetComponent<RectTransform>();
        labelTransform.SetParent(volumeSlider.transform,false);
        labelTransform.anchorMin = new Vector2(0f,0.5f);
        labelTransform.anchorMax = new Vector2(0f,0.5f);
        labelTransform.pivot = new Vector2(1f,0.5f);
        labelTransform.anchoredPosition = new Vector2(-12f,0f);
        labelTransform.sizeDelta = new Vector2(72f,30f);

        Text label = labelObject.GetComponent<Text>();
        label.text = "音量";
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = 18;
        label.alignment = TextAnchor.MiddleRight;
        label.color = Color.white;
        label.raycastTarget = false;
    }
}
