using UnityEngine;
using UnityEngine.UI;

public class PulseGlow : MonoBehaviour
{
    public Image glowImage;

    public float minScale = 0.85f;
    public float maxScale = 1.25f;
    public float pulseSpeed = 2f;

    public float minAlpha = 0.25f;
    public float maxAlpha = 0.75f;

    void Update()
    {
        float pulse = (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f;

        float scale = Mathf.Lerp(minScale, maxScale, pulse);
        transform.localScale = new Vector3(scale, scale, 1f);

        Color c = glowImage.color;
        c.a = Mathf.Lerp(minAlpha, maxAlpha, pulse);
        glowImage.color = c;
    }
}