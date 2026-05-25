using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class Tile : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    [Header("Координати")]
    public Vector2Int correctPosition;
    public Vector2Int currentPosition;

    [Header("Статус")]
    public bool isFixed;
    private static Tile draggedTile;
    private static GameObject ghostTile;
    [Header("UI Елементи")]
    public GameObject fixedIcon;

    private Image tileImage;
    private Button button;
    private CanvasGroup canvasGroup;
    private LevelGenerator levelManager;

    void Awake()
    {
        tileImage = GetComponent<Image>();
        button = GetComponent<Button>();
        button.onClick.AddListener(OnTileClicked);
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }
    public void Setup(Vector2Int pos, Color color, bool fixedTile, LevelGenerator manager)
    {
        correctPosition = pos;
        currentPosition = pos;
        isFixed = fixedTile;
        tileImage.color = color;
        levelManager = manager; 
        if (fixedIcon != null) fixedIcon.SetActive(isFixed);
    }

    public void SwapData(Tile otherTile)
    {
        Vector2Int tempPos = this.correctPosition;
        Color tempColor = this.tileImage.color;
        this.correctPosition = otherTile.correctPosition;
        this.tileImage.color = otherTile.tileImage.color;
        otherTile.correctPosition = tempPos;
        otherTile.tileImage.color = tempColor;
    }
    public bool IsInCorrectPosition()
    {
        return currentPosition == correctPosition;
    }
    void OnTileClicked()
    {
        if (isFixed) return;
        levelManager.TileClicked(this);
    }
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isFixed) return;

        draggedTile = this;
        canvasGroup.alpha = 0f;
        ghostTile = new GameObject("Ghost");
        ghostTile.transform.SetParent(GetComponentInParent<Canvas>().transform, false);
        ghostTile.transform.SetAsLastSibling();

        Image ghostImg = ghostTile.AddComponent<Image>();
        ghostImg.color = GetComponent<Image>().color;
        ghostImg.raycastTarget = false;

        RectTransform ghostRt = ghostTile.GetComponent<RectTransform>();
        ghostRt.sizeDelta = GetComponent<RectTransform>().rect.size;
        ghostRt.position = transform.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (ghostTile != null)
        {
            ghostTile.transform.position = eventData.position;
        }
    }
    public void OnEndDrag(PointerEventData eventData)
    {
        draggedTile = null;
        canvasGroup.alpha = 1f;
        if (ghostTile != null) Destroy(ghostTile);
    }
    public void OnDrop(PointerEventData eventData)
    {
        if (draggedTile != null && draggedTile != this && !this.isFixed) levelManager.PerformDragSwap(draggedTile, this);
    }
}