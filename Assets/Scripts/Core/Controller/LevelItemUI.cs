using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LevelItemUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _levelNameText;
    [SerializeField] private Image _thumbnailImage;
    [SerializeField] private Button _button;
    [SerializeField] private GameObject _selectedHighlight;
    [SerializeField] private GameObject _lockedOverlay;

    private LevelData _data;
    private MapSelectionController _controller;

    public void Initialize(MapSelectionController controller)
    {
        _controller = controller;
        _button.onClick.AddListener(OnClicked);
    }

    public void Setup(LevelData data, bool isSelected)
    {
        _data = data;
        gameObject.SetActive(true);

        _levelNameText.text = data.LevelName;
        if (data.Thumbnail != null) _thumbnailImage.sprite = data.Thumbnail;

        SetSelectionState(isSelected);
        _lockedOverlay.SetActive(false);
    }

    public void Deactivate() => gameObject.SetActive(false);

    private void OnClicked()
    {
        if (_data != null) _controller.OnLevelSelected(_data);
    }

    public void SetSelectionState(bool isSelected) => _selectedHighlight.SetActive(isSelected);

    public LevelData GetData() => _data;
}