using System.Collections.Generic;
using UnityEngine;

public class BoardController : MonoBehaviour
{
    [HideInInspector] public BoardData data;

    readonly List<BlockController> blocks = new List<BlockController>();

    Bounds boardBounds;

    public void Init(BoardData boardData)
    {
        data = boardData;

        Vector3 center = new Vector3(boardData.worldPosition.x, 4f,
                                     boardData.worldPosition.y);
        Vector3 size = new Vector3(boardData.TotalWidth, 12f,
                                     boardData.TotalHeight);
        boardBounds = new Bounds(center, size);
    }

    public void RegisterBlock(BlockController b)
    {
        if (b != null && !blocks.Contains(b)) blocks.Add(b);
    }

    public bool CanBlockMoveTo(BlockController moving, Vector3 newWorldPos)
    {
        Bounds movingB = BoundsAt(moving, newWorldPos);

        Bounds shrunk = movingB;
        shrunk.Expand(-0.04f);

        if (shrunk.min.x < boardBounds.min.x) return false;
        if (shrunk.max.x > boardBounds.max.x) return false;
        if (shrunk.min.z < boardBounds.min.z) return false;
        if (shrunk.max.z > boardBounds.max.z) return false;

        if (data != null)
        {
            float cs = data.cellSize;
            float halfW = data.TotalWidth  * 0.5f;
            float halfH = data.TotalHeight * 0.5f;

            // Xác định dải ô lưới (rows, cols) bị đè lên
            float minX = shrunk.min.x - (data.worldPosition.x - halfW);
            float maxX = shrunk.max.x - (data.worldPosition.x - halfW);
            float minZ = (data.worldPosition.y + halfH) - shrunk.max.z;
            float maxZ = (data.worldPosition.y + halfH) - shrunk.min.z;

            int minCol = Mathf.FloorToInt(minX / cs);
            int maxCol = Mathf.FloorToInt(maxX / cs);
            int minRow = Mathf.FloorToInt(minZ / cs);
            int maxRow = Mathf.FloorToInt(maxZ / cs);

            for (int r = minRow; r <= maxRow; r++)
            {
                for (int c = minCol; c <= maxCol; c++)
                {
                    if (r >= 0 && r < data.cellRows && c >= 0 && c < data.cellCols)
                    {
                        if (data.IsCellDisabled(r, c) || data.IsCellBlocker(r, c))
                        {
                            return false;
                        }
                    }
                }
            }
        }

        Bounds colB = movingB;
        colB.Expand(-0.08f);

        foreach (var other in blocks)
        {
            if (other == moving || other == null) continue;
            Bounds otherB = other.GetBounds();
            otherB.Expand(-0.08f);
            if (colB.Intersects(otherB)) return false;
        }

        return true;
    }

    bool OverlapsDisabledCell(float wx, float wz)
    {
        float halfW = data.TotalWidth  * 0.5f;
        float halfH = data.TotalHeight * 0.5f;

        float cs = data.cellSize;
        int col = Mathf.FloorToInt((wx - (data.worldPosition.x - halfW)) / cs);
        int row = Mathf.FloorToInt(((data.worldPosition.y + halfH) - wz)  / cs);

        // Ngoai board → bien board da xu ly o check 1, khong can check disabled
        if (row < 0 || row >= data.cellRows || col < 0 || col >= data.cellCols)
            return false;

        return data.IsCellDisabled(row, col) || data.IsCellBlocker(row, col);
    }
    static Bounds BoundsAt(BlockController block, Vector3 worldPos)
    {
        Bounds b = block.GetBounds();
        Vector3 delta = worldPos - block.transform.position;
        return new Bounds(b.center + delta, b.size);
    }

    public void OnBlockFreed(BlockController b)
    {
        LevelManager.Instance?.CheckWinCondition();
    }
    public bool AllBlocksFree()
    {
        foreach (var b in blocks)
            if (b != null && !b.isFree) return false;
        return true;
    }

    public void RemoveBlock(BlockController b)
    {
        if (blocks.Contains(b))
            blocks.Remove(b);
    }

    public List<BlockController> GetBlocks() => blocks;
}