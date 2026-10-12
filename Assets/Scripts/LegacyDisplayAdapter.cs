using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class LegacyDisplayAdapter : MonoBehaviour
{
    public const float GameplayAspect = 4f/3f;
    public const float PresentationAspect = 16f/9f;
    private static LegacyDisplayAdapter instance;
    private int appliedWidth;
    private int appliedHeight;

    public static Rect SafeAreaPixels { get; private set; }
    public static Rect NormalizedViewport { get; private set; } = new Rect(0f,0f,1f,1f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        if(instance != null)
        {
            return;
        }

        GameObject adapterObject = new GameObject(nameof(LegacyDisplayAdapter));
        DontDestroyOnLoad(adapterObject);
        instance = adapterObject.AddComponent<LegacyDisplayAdapter>();
    }

    private void Awake()
    {
        if(instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        SceneManager.sceneLoaded += OnSceneLoaded;
        ApplyDisplayLayout();
    }

    private void OnDestroy()
    {
        if(instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            instance = null;
        }
    }

    private void Update()
    {
        if(appliedWidth != Screen.width || appliedHeight != Screen.height)
        {
            ApplyDisplayLayout();
        }
    }

    private void OnSceneLoaded(Scene scene,LoadSceneMode mode)
    {
        ApplyDisplayLayout();
        StartCoroutine(ApplyAfterSceneStart());
    }

    private IEnumerator ApplyAfterSceneStart()
    {
        yield return null;
        ApplyDisplayLayout();
    }

    private void ApplyDisplayLayout()
    {
        appliedWidth = Mathf.Max(1,Screen.width);
        appliedHeight = Mathf.Max(1,Screen.height);
        float targetAspect = GetTargetAspect(SceneManager.GetActiveScene().name);
        NormalizedViewport = CalculateViewport(appliedWidth,appliedHeight,targetAspect);
        SafeAreaPixels = new Rect(
            NormalizedViewport.x*appliedWidth,
            NormalizedViewport.y*appliedHeight,
            NormalizedViewport.width*appliedWidth,
            NormalizedViewport.height*appliedHeight);

        foreach(Camera sceneCamera in FindObjectsByType<Camera>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None))
        {
            if(sceneCamera.targetTexture == null)
            {
                sceneCamera.rect = NormalizedViewport;
            }
        }

        foreach(Canvas canvas in FindObjectsByType<Canvas>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None))
        {
            if(canvas.renderMode == RenderMode.ScreenSpaceOverlay && canvas.isRootCanvas)
            {
                AdaptCanvas(canvas,targetAspect);
            }
        }
    }

    private static void AdaptCanvas(Canvas canvas,float targetAspect)
    {
        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if(scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = targetAspect == PresentationAspect
                ? new Vector2(800f,450f)
                : new Vector2(800f,600f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = Screen.width/(float)Screen.height >= targetAspect ? 1f : 0f;
        }

        for(int childIndex=0;childIndex<canvas.transform.childCount;childIndex++)
        {
            RectTransform child = canvas.transform.GetChild(childIndex) as RectTransform;
            if(child == null)
            {
                continue;
            }
            LegacySafeAreaAnchors anchors = child.GetComponent<LegacySafeAreaAnchors>();
            if(anchors == null)
            {
                anchors = child.gameObject.AddComponent<LegacySafeAreaAnchors>();
            }
            anchors.Apply(NormalizedViewport);
        }
    }

    public static float GetTargetAspect(string sceneName)
    {
        switch(sceneName)
        {
            case "level1":
            case "level2":
            case "level3-1":
            case "level3-2":
            case "level4-1":
            case "level4-2":
                return GameplayAspect;
            default:
                return PresentationAspect;
        }
    }

    public static Rect CalculateViewport(int width,int height)
    {
        return CalculateViewport(width,height,GameplayAspect);
    }

    public static Rect CalculateViewport(int width,int height,float targetAspect)
    {
        float screenAspect = Mathf.Max(1,width)/(float)Mathf.Max(1,height);
        if(screenAspect > targetAspect)
        {
            float normalizedWidth = targetAspect/screenAspect;
            return new Rect((1f-normalizedWidth)*0.5f,0f,normalizedWidth,1f);
        }
        if(screenAspect < targetAspect)
        {
            float normalizedHeight = screenAspect/targetAspect;
            return new Rect(0f,(1f-normalizedHeight)*0.5f,1f,normalizedHeight);
        }
        return new Rect(0f,0f,1f,1f);
    }
}

public sealed class LegacySafeAreaAnchors : MonoBehaviour
{
    private bool initialized;
    private Vector2 originalAnchorMin;
    private Vector2 originalAnchorMax;

    public void Apply(Rect viewport)
    {
        RectTransform target = transform as RectTransform;
        if(target == null)
        {
            return;
        }
        if(!initialized)
        {
            initialized = true;
            originalAnchorMin = target.anchorMin;
            originalAnchorMax = target.anchorMax;
        }

        target.anchorMin = new Vector2(
            Mathf.Lerp(viewport.xMin,viewport.xMax,originalAnchorMin.x),
            Mathf.Lerp(viewport.yMin,viewport.yMax,originalAnchorMin.y));
        target.anchorMax = new Vector2(
            Mathf.Lerp(viewport.xMin,viewport.xMax,originalAnchorMax.x),
            Mathf.Lerp(viewport.yMin,viewport.yMax,originalAnchorMax.y));
    }
}
