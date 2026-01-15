using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RankingEntryUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _infoText;
    [SerializeField] private Image _colorPreview;

    public void UpdateInfo(string info, Sprite sprite, Color color)
    {
        _infoText.text = info;

        if (_colorPreview != null)
        {
            _colorPreview.sprite = sprite;
            _colorPreview.color = color;

            if (sprite == null)
            {
                _colorPreview.color = new Color(color.r, color.g, color.b, 0);
            }
        }
    }
}