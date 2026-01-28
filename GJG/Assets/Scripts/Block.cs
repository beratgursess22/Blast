using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(BoxCollider2D))]
public class Block : MonoBehaviour
{
    [HideInInspector] public int row, col, colorId;
    private BoardManager board;
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }
    public void Init(BoardManager boardManager, int r, int c, int color, Sprite sprite)
    {
        board = boardManager;
        row = r;
        col = c;
        colorId = color;
        SetSprite(sprite);
    }
    public void SetSprite(Sprite sprite)
    {
        if (spriteRenderer == null) 
            spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = sprite;
    }
    private void OnMouseDown()
    {
        if (board == null) 
            return;
        board.OnBlockClicked(this);
    }
}
