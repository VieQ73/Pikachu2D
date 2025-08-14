// File: _Project/Scripts/Gameplay/Tile.cs
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System;
using System.Threading.Tasks;

[RequireComponent(typeof(Button))]
public class Tile : MonoBehaviour
{
    public int TileType { get; private set; }
    public Vector2Int GridPosition { get; private set; }
    public bool IsFrozen { get; private set; }

    [Header("Component References")]
    [SerializeField] private Image pokemonImage;
    [SerializeField] private Image background;
    [SerializeField] public Transform contentTransform;
    [SerializeField] private GameObject freezeOverlay;
    [SerializeField] private Image specialIconImage;

    // Không cần gán Art Assets ở đây nữa

    private Button tileButton;
    private readonly Color _normalColor = Color.white;
    private readonly Color _selectedColor = new Color(1f, 0.85f, 0.85f, 1f);
    private readonly Color _hintColor = Color.yellow;

    private void Awake()
    {
        tileButton = GetComponent<Button>();
    }

    // HÀM QUAN TRỌNG NHẤT ĐÃ ĐƯỢC SỬA LẠI
    public void Initialize(int type, Vector2Int position, Sprite sprite = null)
    {
        TileType = type;
        GridPosition = position;
        gameObject.name = $"Tile_{position.x}_{position.y} (Type: {type})";

        // 1. Reset toàn bộ trạng thái về mặc định (trống)
        contentTransform.localScale = Vector3.one;
        pokemonImage.gameObject.SetActive(false);
        specialIconImage.gameObject.SetActive(false);
        background.gameObject.SetActive(true);
        SetFrozen(false);
        SetSelected(false);
        tileButton.interactable = true;

        // 2. Thiết lập dựa trên loại tile
        if (type > 0) // Tile Pokemon thường
        {
            if (pokemonImage != null && sprite != null)
            {
                pokemonImage.sprite = sprite; // Gán sprite TRƯỚC
                pokemonImage.gameObject.SetActive(true); // Bật GameObject SAU
                background.gameObject.SetActive(false);
            }
        }
        else if (type == -1) // Tile Bom (BB)
        {
            if (specialIconImage != null && sprite != null)
            {
                specialIconImage.sprite = sprite;
                specialIconImage.gameObject.SetActive(true);
                background.gameObject.SetActive(false);
            }
        }
        else if (type == -2) // Tile Đá (ST)
        {
            if (specialIconImage != null && sprite != null)
            {
                specialIconImage.sprite = sprite;
                specialIconImage.gameObject.SetActive(true);
                background.gameObject.SetActive(false);
                tileButton.interactable = false;
            }
        }
        else // Ô trống (Type = 0)
        {
            tileButton.interactable = false;
        }
    }

    // Hàm SpawnAsNewTile giờ cũng sẽ gọi Initialize
    public void SpawnAsNewTile(int newType, Sprite newSprite)
    {
        contentTransform.localScale = Vector3.zero;
        contentTransform.DOScale(1, 0.4f).SetEase(Ease.OutBack);
        Initialize(newType, this.GridPosition, newSprite);
    }

    public Sprite GetSprite()
    {
        if (TileType > 0 && pokemonImage != null) return pokemonImage.sprite;
        if (TileType < 0 && specialIconImage != null) return specialIconImage.sprite;
        return null;
    }

    public Task ShowAsEmptyAndReturnTask()
    {
        // Khi một tile bị xóa, nó trở thành ô trống
        Initialize(0, this.GridPosition);
        // Trả về một task hiệu ứng nhỏ
        return contentTransform.DOScale(0, 0.3f).SetEase(Ease.InBack).AsyncWaitForCompletion().ContinueWith(_ =>
        {
            MainThreadDispatcher.RunOnMainThread(() => {
                contentTransform.localScale = Vector3.one;
            });
        });
    }
    public void SetSelected(bool isSelected)
    {
        if (IsFrozen) return;

        var targetImage = TileType > 0 ? pokemonImage : specialIconImage;
        if (targetImage == null) return;

        targetImage.DOKill();
        targetImage.DOColor(isSelected ? _selectedColor : _normalColor, 0.15f);

        if (contentTransform != null)
        {
            contentTransform.DOKill();
            contentTransform.DOScale(isSelected ? 1.1f : 1.0f, 0.15f);
        }
    }

    public void AddClickListener(Action action)
    {
        tileButton.onClick.RemoveAllListeners();
        tileButton.onClick.AddListener(() => action?.Invoke());
    }

    public void DoHintEffect()
    {
        var targetImage = TileType > 0 ? pokemonImage : specialIconImage;
        if (targetImage == null) return;

        Sequence hintSequence = DOTween.Sequence();
        hintSequence.Append(targetImage.DOColor(_hintColor, 0.3f).SetEase(Ease.OutQuad));
        hintSequence.Append(targetImage.DOColor(_normalColor, 0.3f).SetEase(Ease.InQuad));
        hintSequence.SetLoops(3);
    }

    public void SetFrozen(bool isFrozen)
    {
        this.IsFrozen = isFrozen;
        if (freezeOverlay != null)
        {
            freezeOverlay.SetActive(isFrozen);
        }
        tileButton.interactable = !isFrozen;
    }

    public void Thaw()
    {
        if (!IsFrozen) return;

        SetFrozen(false);
        if (freezeOverlay != null)
        {
            freezeOverlay.transform.DOPunchScale(new Vector3(0.3f, 0.3f, 0.3f), 0.3f).OnComplete(() =>
            {
                freezeOverlay.SetActive(false);
            });
        }
    }
}