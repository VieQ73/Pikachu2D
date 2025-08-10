// File: _Project/Scripts/Gameplay/Tile.cs
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System;

[RequireComponent(typeof(Button))]
public class Tile : MonoBehaviour
{
    public int TileType { get; private set; }
    public Vector2Int GridPosition { get; private set; }

    [SerializeField] private Image pokemonImage;
    [SerializeField] private Image background;

    private Button tileButton;
    private readonly Color _normalColor = Color.white;
    private readonly Color _selectedColor = new Color(1f, 0.85f, 0.85f, 1f);

    private void Awake()
    {
        tileButton = GetComponent<Button>();
        // Thêm kiểm tra null để bắt lỗi sớm
        if (pokemonImage == null || background == null)
        {
            Debug.LogError($"Tile {gameObject.name} bị thiếu tham chiếu PokemonImage hoặc Background trong Inspector!");
        }
    }

    public void Initialize(int type, Vector2Int position)
    {
        TileType = type;
        GridPosition = position;
        gameObject.name = $"Tile_{position.x}_{position.y} (Type: {type})";

        // Reset trạng thái
        pokemonImage.color = _normalColor;
        pokemonImage.transform.localScale = Vector3.one;

        // Nếu là ô trống, ẩn ảnh Pokemon đi
        if (TileType == 0)
        {
            pokemonImage.gameObject.SetActive(false);
            background.gameObject.SetActive(true);
        }
        else // Nếu có tile, bật ảnh lên
        {
            pokemonImage.gameObject.SetActive(true);
            background.gameObject.SetActive(false);
        }
    }

    public void SetSprite(Sprite sprite)
    {
        if (pokemonImage != null && sprite != null)
        {
            pokemonImage.sprite = sprite;
        }
    }

    public void ShowAsEmpty()
    {
        TileType = 0;

        pokemonImage.transform.DOScale(0, 0.3f).SetEase(Ease.InBack).OnComplete(() =>
        {
            pokemonImage.gameObject.SetActive(false);
            background.gameObject.SetActive(true);
            pokemonImage.transform.localScale = Vector3.one;
            pokemonImage.color = _normalColor;
        });
    }

    public void SetSelected(bool isSelected)
    {
        if (pokemonImage == null) return;

        pokemonImage.DOKill();
        pokemonImage.DOColor(isSelected ? _selectedColor : _normalColor, 0.15f);

        pokemonImage.transform.DOKill();
        pokemonImage.transform.DOScale(isSelected ? 1.1f : 1.0f, 0.15f);
    }

    public void AddClickListener(Action action)
    {
        tileButton.onClick.RemoveAllListeners();
        tileButton.onClick.AddListener(() => action?.Invoke());
    }
}