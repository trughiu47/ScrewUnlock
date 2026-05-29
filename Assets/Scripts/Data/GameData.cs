using UnityEngine;

public enum MoveDirection { Right, Left, Forward, Back }

public enum BlockColor
{
    Cyan, Orange, Red, Blue, Green, Yellow, Purple, Sand
}

[System.Serializable]
public class ScrewData
{
    public float unlockDistance = 1f;
}

[System.Serializable]
public class BlockInBoardData
{
    [Header("Block")]
    public BlockColor color = BlockColor.Cyan;
    public int cellRow = 0;
    public int cellCol = 0;
    public MoveDirection slideDirection = MoveDirection.Right;

    [Header("Screw gan vao block nay")]
    public ScrewData screw = new ScrewData();
}

[System.Serializable]
public class BoardData
{
    [Header("Vi tri tam board trong scene (world XZ)")]
    public Vector2 worldPosition = Vector2.zero;

    [Header("So o luoi (rows x cols)")]
    public int cellRows = 2;
    public int cellCols = 1;
    public float cellSize = 1f;

    [Header("Blocks trong board nay")]
    public BlockInBoardData[] blocks = new BlockInBoardData[0];

    [Header("O bi vo hieu (khong hien thi, khong co block)")]
    public Vector2Int[] disabledCells = new Vector2Int[0];

    [Header("O chan (co prefab chan, khong the dat block)")]
    public Vector2Int[] blockerCells = new Vector2Int[0];

    public bool IsCellDisabled(int row, int col)
    {
        foreach (var c in disabledCells)
            if (c.x == row && c.y == col) return true;
        return false;
    }

    public bool IsCellBlocker(int row, int col)
    {
        foreach (var c in blockerCells)
            if (c.x == row && c.y == col) return true;
        return false;
    }

    public float TotalWidth => cellCols * cellSize;

    public float TotalHeight => cellRows * cellSize;

    public Vector3 WorldCenter => new Vector3(worldPosition.x, 0f, worldPosition.y);

    public Vector3 CellWorldPos(int row, int col)
    {
        float halfW = TotalWidth * 0.5f;
        float halfH = TotalHeight * 0.5f;

        float x = worldPosition.x - halfW + (col + 0.5f) * cellSize;
        float z = worldPosition.y + halfH - (row + 0.5f) * cellSize;
        return new Vector3(x, 0f, z);
    }
}