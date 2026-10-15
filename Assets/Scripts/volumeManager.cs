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

    private Slider effectsVolumeSlider;
    private Button reducedFlashingButton;
    private Text reducedFlashingButtonLabel;

    void Start()
    {
        float savedVolume = GetSavedVolume();
        if(volumeSlider != null)
        {
            volumeSlider.gameObject.SetActive(true);
            EnsureVolumeLabel(volumeSlider,"主音量");
            volumeSlider.SetValueWithoutNotify(savedVolume);
            EnsureAccessibilityControls();
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

    public void SetEffectsVolume(float sliderValue)
    {
        GameAccessibilitySettings.SetEffectsVolume(sliderValue);
    }

    public void ToggleReducedFlashing()
    {
        GameAccessibilitySettings.SetReducedFlashing(
            !GameAccessibilitySettings.ReducedFlashingEnabled);
        UpdateReducedFlashingLabel();
    }

    private void ApplyVolume(float volume)
    {
        if(myAudioMixer != null)
        {
            myAudioMixer.SetFloat("MasterVolume",volume);
        }
    }

    private void EnsureAccessibilityControls()
    {
        EnsureEffectsVolumeSlider();
        EnsureReducedFlashingButton();
    }

    private void EnsureEffectsVolumeSlider()
    {
        Transform existing=volumeSlider.transform.parent.Find("EffectsVolumeSlider");
        if(existing!=null)
        {
            effectsVolumeSlider=existing.GetComponent<Slider>();
        }else
        {
            effectsVolumeSlider=Instantiate(volumeSlider,volumeSlider.transform.parent);
            effectsVolumeSlider.name="EffectsVolumeSlider";
            effectsVolumeSlider.onValueChanged=new Slider.SliderEvent();
            RectTransform effectsTransform=effectsVolumeSlider.GetComponent<RectTransform>();
            effectsTransform.anchoredPosition+=new Vector2(0f,-55f);
        }

        effectsVolumeSlider.minValue=MinimumVolumeDb;
        effectsVolumeSlider.maxValue=MaximumVolumeDb;
        effectsVolumeSlider.SetValueWithoutNotify(GameAccessibilitySettings.EffectsVolumeDb);
        effectsVolumeSlider.onValueChanged.RemoveAllListeners();
        effectsVolumeSlider.onValueChanged.AddListener(SetEffectsVolume);
        EnsureVolumeLabel(effectsVolumeSlider,"音效音量");
    }

    private void EnsureReducedFlashingButton()
    {
        Transform existing=volumeSlider.transform.parent.Find("ReducedFlashingButton");
        if(existing!=null)
        {
            reducedFlashingButton=existing.GetComponent<Button>();
            reducedFlashingButtonLabel=existing.GetComponentInChildren<Text>();
        }else
        {
            GameObject buttonObject=new GameObject(
                "ReducedFlashingButton",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button));
            RectTransform buttonTransform=buttonObject.GetComponent<RectTransform>();
            buttonTransform.SetParent(volumeSlider.transform.parent,false);
            RectTransform masterTransform=volumeSlider.GetComponent<RectTransform>();
            buttonTransform.anchorMin=masterTransform.anchorMin;
            buttonTransform.anchorMax=masterTransform.anchorMax;
            buttonTransform.pivot=masterTransform.pivot;
            buttonTransform.anchoredPosition=masterTransform.anchoredPosition+new Vector2(0f,-112f);
            buttonTransform.sizeDelta=new Vector2(190f,36f);

            Image background=buttonObject.GetComponent<Image>();
            background.color=new Color(0.16f,0.31f,0.39f,0.92f);
            reducedFlashingButton=buttonObject.GetComponent<Button>();
            reducedFlashingButton.targetGraphic=background;
            reducedFlashingButton.onClick.AddListener(ToggleReducedFlashing);

            GameObject labelObject=new GameObject(
                "Label",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Text));
            RectTransform labelTransform=labelObject.GetComponent<RectTransform>();
            labelTransform.SetParent(buttonTransform,false);
            labelTransform.anchorMin=Vector2.zero;
            labelTransform.anchorMax=Vector2.one;
            labelTransform.offsetMin=Vector2.zero;
            labelTransform.offsetMax=Vector2.zero;
            reducedFlashingButtonLabel=labelObject.GetComponent<Text>();
            reducedFlashingButtonLabel.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            reducedFlashingButtonLabel.fontSize=17;
            reducedFlashingButtonLabel.alignment=TextAnchor.MiddleCenter;
            reducedFlashingButtonLabel.color=Color.white;
            reducedFlashingButtonLabel.raycastTarget=false;
        }

        UpdateReducedFlashingLabel();
    }

    private void UpdateReducedFlashingLabel()
    {
        if(reducedFlashingButtonLabel!=null)
        {
            reducedFlashingButtonLabel.text=GameAccessibilitySettings.ReducedFlashingEnabled
                ? "降低閃爍：開"
                : "降低閃爍：關";
        }
    }

    private static void EnsureVolumeLabel(Slider slider,string labelText)
    {
        Transform existing=slider.transform.Find("VolumeLabel");
        if(existing!=null)
        {
            Text existingLabel=existing.GetComponent<Text>();
            if(existingLabel!=null)
            {
                existingLabel.text=labelText;
            }
            return;
        }

        GameObject labelObject = new GameObject(
            "VolumeLabel",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Text));
        RectTransform labelTransform = labelObject.GetComponent<RectTransform>();
        labelTransform.SetParent(slider.transform,false);
        labelTransform.anchorMin = new Vector2(0f,0.5f);
        labelTransform.anchorMax = new Vector2(0f,0.5f);
        labelTransform.pivot = new Vector2(1f,0.5f);
        labelTransform.anchoredPosition = new Vector2(-12f,0f);
        labelTransform.sizeDelta = new Vector2(96f,30f);

        Text label = labelObject.GetComponent<Text>();
        label.text = labelText;
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = 18;
        label.alignment = TextAnchor.MiddleRight;
        label.color = Color.white;
        label.raycastTarget = false;
    }
}
