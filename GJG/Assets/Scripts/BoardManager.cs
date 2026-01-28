using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardManager : MonoBehaviour
{
    public int rows, columns;
    public float cellSize;
    public int colorCount;
    public Block blockPrefab;
    public Sprite[] defaultSprites;
    public int A, B, C;
    public Sprite[] spritesA, spritesB, spritesC;
    public float dropSpeed, spawnAboveRows;
    private Block[,] grid;
    private int[,] visited;
    private int visitId = 1;
    private float startX, startY;
    private bool isAnimating = false;
    private int activeMoves = 0;
    public int prewarmPoolSize = 128;
    private Queue<Block> blockPool = new Queue<Block>();
    public Transform frameTransform;

    private void Start()
    {
        PrewarmPool();
        CreateGrid();
        AdjustFrame();
    }

    private void CreateGrid()
    {
        grid = new Block[rows, columns];
        visited = new int[rows, columns];

        startX = -(columns - 1) * cellSize * 0.5f;
        startY = (rows - 1) * cellSize * 0.5f;

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < columns; c++)
            {
                int colorId = Random.Range(0, colorCount);
                Sprite s = GetDefaultSprite(colorId);
                Vector3 pos = GridToWorld(r, c);
                Block b = GetBlockFromPool(pos);
                b.name = r.ToString() + "." + c.ToString();
                b.Init(this, r, c, colorId, s);
                grid[r, c] = b;
            }
        }
        UpdateAllIcons();
        if (!HasAnyMove())
            SmartShuffle();
    }
    private Vector3 GridToWorld(int r, int c)
    {
        return new Vector3(startX + c * cellSize, startY - r * cellSize, 0f);
    }
    private Sprite GetDefaultSprite(int colorId)
    {
        if (defaultSprites == null || defaultSprites.Length == 0)
            return null;

        int idx = Mathf.Clamp(colorId, 0, defaultSprites.Length - 1);
        return defaultSprites[idx];
    }
    public void OnBlockClicked(Block b)
    {
        if (isAnimating)
            return;

        visitId++;
        List<Block> group = GetGroup(b.row, b.col);
        if (group.Count < 2)
            return;
        foreach (var blk in group)
        {
            grid[blk.row, blk.col] = null;
            ReturnBlockToPool(blk);
        }
        StartCoroutine(CollapseAndRefill());
    }
    private List<Block> GetGroup(int startR, int startC)
    {
        List<Block> result = new List<Block>();

        Block start = grid[startR, startC];
        if (start == null)
            return result;

        int targetColor = start.colorId;
        Queue<(int r, int c)> q = new Queue<(int r, int c)>();
        q.Enqueue((startR, startC));
        visited[startR, startC] = visitId;

        while (q.Count > 0)
        {
            var (r, c) = q.Dequeue();
            Block cur = grid[r, c];
            if (cur == null)
                continue;

            result.Add(cur);
            TryEnqueue(r - 1, c, targetColor, q);
            TryEnqueue(r + 1, c, targetColor, q);
            TryEnqueue(r, c - 1, targetColor, q);
            TryEnqueue(r, c + 1, targetColor, q);
        }
        return result;
    }

    private void TryEnqueue(int r, int c, int targetColor, Queue<(int r, int c)> q)
    {
        if (!InBounds(r, c))
            return;
        if (visited[r, c] == visitId)
            return;

        Block b = grid[r, c];
        if (b == null)
            return;
        if (b.colorId != targetColor)
            return;

        visited[r, c] = visitId;
        q.Enqueue((r, c));
    }

    private bool InBounds(int r, int c)
    {
        return r >= 0 && r < rows && c >= 0 && c < columns;
    }

    private IEnumerator CollapseAndRefill()
    {
        isAnimating = true;

        for (int c = 0; c < columns; c++)
        {
            int writeRow = rows - 1;

            for (int r = rows - 1; r >= 0; r--)
            {
                if (grid[r, c] == null)
                    continue;
                if (r != writeRow)
                {
                    Block b = grid[r, c];
                    grid[writeRow, c] = b;
                    grid[r, c] = null;
                    b.row = writeRow;
                    b.col = c;
                    Vector3 target = GridToWorld(writeRow, c);
                    StartCoroutine(MoveTo(b.transform, target));
                }
                writeRow--;
            }

            for (int r = writeRow; r >= 0; r--)
            {
                int colorId = Random.Range(0, colorCount);
                Sprite s = GetDefaultSprite(colorId);
                Vector3 target = GridToWorld(r, c);
                Vector3 spawn = new Vector3(target.x, SpawnYAboveCamera(), 0f);
                Block nb = GetBlockFromPool(spawn);
                nb.name = $"{r}.{c}";
                nb.Init(this, r, c, colorId, s);
                grid[r, c] = nb;
                nb.transform.localScale = Vector3.zero;
                StartCoroutine(ScalePop(nb.transform, 0.08f));
                StartCoroutine(MoveTo(nb.transform, target));
            }
            yield return new WaitForSeconds(0.02f);
        }
        while (activeMoves > 0)
            yield return null;
        isAnimating = false;
        UpdateAllIcons();
        if (!HasAnyMove())
            SmartShuffle();
    }

    private IEnumerator MoveTo(Transform t, Vector3 target)
    {
        activeMoves++;

        Vector3 start = t.position;
        float dist = Vector3.Distance(start, target);
        float duration = Mathf.Clamp(dist / (dropSpeed * cellSize), 0.15f, 0.45f);
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float u = Mathf.Clamp01(time / duration);
            float eased = 1f - Mathf.Pow(1f - u, 3f);
            t.position = Vector3.LerpUnclamped(start, target, eased);
            yield return null;
        }
        t.position = target;
        activeMoves--;
    }

    private IEnumerator ScalePop(Transform t, float duration)
    {
        float time = 0f;
        while (time < duration)
        {
            time += Time.deltaTime;
            float u = Mathf.Clamp01(time / duration);
            float eased = 1f - Mathf.Pow(1f - u, 3f);
            t.localScale = Vector3.LerpUnclamped(Vector3.zero, Vector3.one, eased);
            yield return null;
        }
        t.localScale = Vector3.one;
    }

    private float SpawnYAboveCamera()
    {
        var cam = Camera.main;

        if (cam == null)
            return startY + (spawnAboveRows * cellSize);

        float camTop = cam.transform.position.y + cam.orthographicSize;
        return camTop + (2f * cellSize);
    }
    private Sprite GetSpriteFor(int colorId, int groupSize)
    {
        Sprite s = GetDefaultSprite(colorId);

        if (groupSize > C && spritesC != null && spritesC.Length > colorId)
            return spritesC[colorId];
        if (groupSize > B && spritesB != null && spritesB.Length > colorId)
            return spritesB[colorId];
        if (groupSize > A && spritesA != null && spritesA.Length > colorId)
            return spritesA[colorId];
        return s;
    }
    private void UpdateAllIcons()
    {
        visitId++;
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < columns; c++)
            {
                if (grid[r, c] == null)
                    continue;
                if (visited[r, c] == visitId)
                    continue;

                List<Block> group = GetGroup(r, c);
                int size = group.Count;
                foreach (var b in group)
                    b.SetSprite(GetSpriteFor(b.colorId, size));
            }
        }
    }

    private bool HasAnyMove()
    {
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < columns; c++)
            {
                Block b = grid[r, c];
                if (b == null)
                    continue;

                int color = b.colorId;
                if (c + 1 < columns && grid[r, c + 1] != null && grid[r, c + 1].colorId == color)
                    return true;
                if (r + 1 < rows && grid[r + 1, c] != null && grid[r + 1, c].colorId == color)
                    return true;
            }
        }
        return false;
    }
    private void ShuffleList(List<int> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            int tmp = list[i];
            list[i] = list[j];
            list[j] = tmp;
        }
    }
    private void SmartShuffle()
    {
        if (grid == null)
            return;

        List<int> colors = new List<int>(rows * columns);
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < columns; c++)
                if (grid[r, c] != null)
                    colors.Add(grid[r, c].colorId);
        if (colors.Count < 2)
            return;

        ShuffleList(colors);
        int r1 = 0, c1 = 0;
        int r2 = (columns > 1) ? 0 : 1;
        int c2 = (columns > 1) ? 1 : 0;

        if (r2 >= rows)
        {
            r2 = 0;
            c2 = 1;
        }
        if (c2 >= columns)
        {
            r2 = 1;
            c2 = 0;
        }

        int forcedColor = colors[0];
        colors[0] = forcedColor;
        colors[1] = forcedColor;
        int k = 0;
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < columns; c++)
            {
                Block b = grid[r, c];
                if (b == null)
                    continue;

                int newColor = colors[k++];
                b.colorId = newColor;
                b.SetSprite(GetDefaultSprite(newColor));
            }
        }
        if (grid[r1, c1] != null)
        {
            grid[r1, c1].colorId = forcedColor;
            grid[r1, c1].SetSprite(GetDefaultSprite(forcedColor));
        }
        if (grid[r2, c2] != null)
        {
            grid[r2, c2].colorId = forcedColor;
            grid[r2, c2].SetSprite(GetDefaultSprite(forcedColor));
        }
        UpdateAllIcons();
    }

    private void PrewarmPool()
    {
        for (int i = 0; i < prewarmPoolSize; i++)
        {
            Block b = Instantiate(blockPrefab, Vector3.one * 1000f, Quaternion.identity, transform);
            b.gameObject.SetActive(false);
            blockPool.Enqueue(b);
        }
    }
    private Block GetBlockFromPool(Vector3 pos)
    {
        Block b;

        if (blockPool.Count > 0)
        {
            b = blockPool.Dequeue();
            b.gameObject.SetActive(true);
        }
        else
        {
            b = Instantiate(blockPrefab, pos, Quaternion.identity, transform);
        }

        b.transform.position = pos;
        b.transform.localScale = Vector3.one;
        return b;
    }
    private void ReturnBlockToPool(Block b)
    {
        b.gameObject.SetActive(false);
        blockPool.Enqueue(b);
    }

    private void AdjustFrame()
    {
        if (frameTransform == null)
            return;

        float gridWidth = columns * cellSize;
        float gridHeight = rows * cellSize;
        float baseWidth = 8f * cellSize;  // Ya da Frame'in başlangıç genişliği
        float baseHeight = 8f * cellSize; // Ya da Frame'in başlangıç yüksekliği

        float scaleX = gridWidth / baseWidth;
        float scaleY = gridHeight / baseHeight;

        frameTransform.localScale = new Vector3(scaleX, scaleY, 1f);
        frameTransform.position = new Vector3(0f, 0f, frameTransform.position.z);
    }
}
