using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Quan ly 1 board: luu danh sach block, kiem tra va cham, kiem tra win.
/// Board duoc chia thanh luoi o (cellRows x cellCols), moi o vua du 1 block.
///
/// Logic CanBlockMoveTo:
///   1. Toan bo bounds XZ cua block phai NAM TRONG board → block khong the xuyen qua canh.
///   2. Bounds XZ cua block khong duoc giao voi bounds cua block khac → khong xuyen block.
///   Ca hai dieu kien ap dung cho ca khi con screw (truot 1 huong) lan sau khi tu do.
/// </summary>
public class BoardController : MonoBehaviour
{
    [HideInInspector] public BoardData data;

    readonly List<BlockController> blocks = new List<BlockController>();

    /// <summary>Bounds world-space cua board (XZ = khu vuc o luoi, Y rong)</summary>
    Bounds boardBounds;

    // ── Init ───────────────────────────────────────────────────────────────
    public void Init(BoardData boardData)
    {
        data = boardData;

        // Tam board trong world XZ
        // Y range rong (-2 den 10) de check khong bi anh huong boi Y cua block
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

    // ── Movement validation ────────────────────────────────────────────────
    /// <summary>
    /// Tra ve true neu block co the di chuyen den newWorldPos.
    ///
    /// Quy tac:
    ///   1. TOAN BO bounds XZ cua block phai nam TRONG board.
    ///      (Truoc day chi kiem tra tam diem → block van truot ra ngoai 1 nua.)
    ///   2. Bounds XZ khong duoc giao voi bounds block khac.
    /// </summary>
    public bool CanBlockMoveTo(BlockController moving, Vector3 newWorldPos)
    {
        // Tinh bounds thuc cua block tai vi tri moi
        Bounds movingB = BoundsAt(moving, newWorldPos);

        // 1. ── Kiem tra bien board (XZ) ─────────────────────────────────────
        // Shrink nho 0.04 de tranh float-precision khi block nam sat canh
        Bounds shrunk = movingB;
        shrunk.Expand(-0.04f);

        if (shrunk.min.x < boardBounds.min.x) return false;
        if (shrunk.max.x > boardBounds.max.x) return false;
        if (shrunk.min.z < boardBounds.min.z) return false;
        if (shrunk.max.z > boardBounds.max.z) return false;

        // 2. ── Kiem tra o disabled (cac o danh dau X) ───────────────────────
        // Block khong duoc di chuyen vao bat ky o nao la disabled.
        // Kiem tra bang cach sample 4 goc bounds cua block (shrunk) + tam.
        if (data != null)
        {
            // Lay danh sach diem can kiem tra (tam + 4 goc bounds XZ)
            float bMinX = shrunk.min.x;
            float bMaxX = shrunk.max.x;
            float bMinZ = shrunk.min.z;
            float bMaxZ = shrunk.max.z;

            // Voi moi diem sample, tinh o luoi tuong ung va kiem tra disabled
            if (OverlapsDisabledCell(bMinX + 0.01f, bMinZ + 0.01f)) return false;
            if (OverlapsDisabledCell(bMaxX - 0.01f, bMinZ + 0.01f)) return false;
            if (OverlapsDisabledCell(bMinX + 0.01f, bMaxZ - 0.01f)) return false;
            if (OverlapsDisabledCell(bMaxX - 0.01f, bMaxZ - 0.01f)) return false;
            if (OverlapsDisabledCell((bMinX + bMaxX) * 0.5f, (bMinZ + bMaxZ) * 0.5f)) return false;
        }

        // 3. ── Kiem tra va cham voi block khac ──────────────────────────────
        // Shrink 0.08 de tranh tiep xuc sat canh bi nhan la va cham
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

    /// <summary>
    /// Tra ve true neu diem world (wx, wz) roi vao o luoi nao do la disabled.
    /// </summary>
    bool OverlapsDisabledCell(float wx, float wz)
    {
        float halfW = data.TotalWidth  * 0.5f;
        float halfH = data.TotalHeight * 0.5f;

        // Chuyen tu world XZ → toa do luoi (row, col)
        // CellWorldPos: x = worldPos.x - halfW + (col+0.5)*cs
        //               z = worldPos.y + halfH - (row+0.5)*cs
        float cs = data.cellSize;
        int col = Mathf.FloorToInt((wx - (data.worldPosition.x - halfW)) / cs);
        int row = Mathf.FloorToInt(((data.worldPosition.y + halfH) - wz)  / cs);

        // Ngoai board → bien board da xu ly o check 1, khong can check disabled
        if (row < 0 || row >= data.cellRows || col < 0 || col >= data.cellCols)
            return false;

        return data.IsCellDisabled(row, col) || data.IsCellBlocker(row, col);
    }

    /// <summary>
    /// Tinh Bounds cua block neu no o vi tri worldPos (khong thay doi transform thuc).
    /// </summary>
    static Bounds BoundsAt(BlockController block, Vector3 worldPos)
    {
        Bounds b = block.GetBounds();
        Vector3 delta = worldPos - block.transform.position;
        return new Bounds(b.center + delta, b.size);
    }

    // ── Events ─────────────────────────────────────────────────────────────
    /// <summary>Goi khi 1 block vua duoc mo screw. Kiem tra win condition.</summary>
    public void OnBlockFreed(BlockController b)
    {
        LevelManager.Instance?.CheckWinCondition();
    }

    /// <summary>True khi TẤT CA block trong board da duoc mo screw (isFree).</summary>
    public bool AllBlocksFree()
    {
        foreach (var b in blocks)
            if (b != null && !b.isFree) return false;
        return true;
    }

    public List<BlockController> GetBlocks() => blocks;
}