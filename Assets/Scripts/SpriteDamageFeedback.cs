using UnityEngine;

public sealed class SpriteDamageFeedback
{
    public const float PulsePeriod = 0.2f;
    public const float MinimumAlpha = 0.35f;
    public const float ReducedFlashingAlpha = 0.72f;

    private readonly SpriteRenderer[] renderers;
    private readonly Color[] originalColors;

    public SpriteDamageFeedback(GameObject target)
    {
        renderers=target.GetComponentsInChildren<SpriteRenderer>(true);
        originalColors=new Color[renderers.Length];
        for(int index=0;index<renderers.Length;index++)
        {
            originalColors[index]=renderers[index].color;
        }
    }

    public void Pulse(float elapsedTime)
    {
        float alphaMultiplier;
        if(GameAccessibilitySettings.ReducedFlashingEnabled)
        {
            alphaMultiplier=ReducedFlashingAlpha;
        }else
        {
            float phase=Mathf.PingPong(elapsedTime/(PulsePeriod*0.5f),1f);
            alphaMultiplier=Mathf.Lerp(MinimumAlpha,1f,phase);
        }
        for(int index=0;index<renderers.Length;index++)
        {
            if(renderers[index]==null)
            {
                continue;
            }

            Color color=originalColors[index];
            color.a*=alphaMultiplier;
            renderers[index].color=color;
        }
    }

    public void Restore()
    {
        for(int index=0;index<renderers.Length;index++)
        {
            if(renderers[index]!=null)
            {
                renderers[index].color=originalColors[index];
            }
        }
    }
}
