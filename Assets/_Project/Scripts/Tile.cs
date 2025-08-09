using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System;

[RequireComponent(typeof(Image), typeof(Button))]
public class Tile : MonoBehaviour
{
    public int TileType { get; private set; }
    public Vector2Int GridPosition { get; private set; }
    public bool IsFrozen { get; private set; }

    // Các biến này là private, chỉ được gán bằng code trong Awake()
    private Image tileImage;
    private Button tileButton;

    [SerializeField] private GameObject freezeOverlay;

    private void Awake()
    {
        // Gán các component bằng code. Đây là nguồn duy nhất.
        tileImage = GetComponent<Image>();
        tileButton = GetComponent<Button>();

        if (freezeOverlay != null)
        {
            freezeOverlay.SetActive(false);
        }
    }

    public void Initialize(int type, Vector2Int position)
    {
        TileType = type;
        GridPosition = position;
        gameObject.name = $"Tile_{position.x}_{position.y} (Type: {type})";
    }

    public void SetGridPosition(Vector2Int newPosition)
    {
        GridPosition = newPosition;
        gameObject.name = $"Tile_{newPosition.x}_{newPosition.y} (Type: {TileType})";
    }

    public void SetFrozen(bool frozen)
    {
        IsFrozen = frozen;
        if (freezeOverlay != null)
        {
            freezeOverlay.SetActive(frozen);
        }
        tileButton.interactable = !frozen;
    }

    public void Thaw()
    {
        if (!IsFrozen) return;

        SetFrozen(false);
        if (freezeOverlay != null && freezeOverlay.activeInHierarchy)
        {
            freezeOverlay.transform.DOPunchScale(new Vector3(0.3f, 0.3f, 0.3f), 0.3f).OnComplete(() =>
            {
                freezeOverlay.SetActive(false);
            });
        }
    }

    public Image GetTileImage()
    {
        return tileImage;
    }

    public void AddClickListener(Action action)
    {
        tileButton.onClick.RemoveAllListeners();
        tileButton.onClick.AddListener(() => action?.Invoke());
    }
}