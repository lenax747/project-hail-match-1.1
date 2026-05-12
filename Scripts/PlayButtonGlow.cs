using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class PlayButtonGlow : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public Image glowImage;

    Vector3 originalScale;
    Coroutine currentRoutine;

    void Start()
    {
        originalScale = glowImage.rectTransform.localScale;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        AnimateGlow(3f, 1f);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        AnimateGlow(1f, 0.35f);
    }

    void AnimateGlow(float scaleTarget, float alphaTarget)
    {
        if (glowImage == null) return;

        if (currentRoutine != null)
            StopCoroutine(currentRoutine);

        currentRoutine = StartCoroutine(GlowRoutine(scaleTarget, alphaTarget));
    }

    IEnumerator GlowRoutine(float targetScale, float targetAlpha)
    {
        Vector3 startScale = glowImage.rectTransform.localScale;
        Vector3 endScale = originalScale * targetScale;

        Color startColor = glowImage.color;
        Color endColor = startColor;
        endColor.a = targetAlpha;

        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime * 8f;

            glowImage.rectTransform.localScale = Vector3.Lerp(startScale, endScale, t);
            glowImage.color = Color.Lerp(startColor, endColor, t);

            yield return null;
        }
    }
}