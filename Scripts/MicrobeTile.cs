using UnityEngine;
using UnityEngine.UI;

public class MicrobeTile : MonoBehaviour
{
    public MicrobeType Type { get; private set; }

    [SerializeField]
    private Image iconImage;

    public void Initialize(
        MicrobeType type,
        Sprite sprite)
    {
        Type = type;
        iconImage.sprite = sprite;
    }
}