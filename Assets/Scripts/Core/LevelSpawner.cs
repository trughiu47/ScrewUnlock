using UnityEngine;

public class LevelSpawner : MonoBehaviour
{
    [Header("Block & Screw")]
    public GameObject blockPrefab;

    [Tooltip("Prefab block dai 2 o (dung cho block co unlockDistance >= 2).\n" +
             "Pivot dat tai TAM O SCREW, mesh keo dai ve NGUOC HUONG slide.\n" +
             "Script se tu xoay 90 deg Y khi slideDir la Forward/Back.")]
    public GameObject blockPrefab2x;

    public GameObject screwPrefab;

    [Header("Tile (nen o) — spawn 1 cai moi o luoi")]
    public GameObject tilePrefab;

    [Header("Vien board — tu dong tao")]
    public GameObject cornerPrefab;
    public GameObject edgePrefab;

    [Header("O chan (Blocker) — khong the dat block vao")]
    public GameObject blockerPrefab;
    public float blockerY = 0f;

    [Header("Y Offsets")]
    public float tileY = 0f;
    public float blockY = 0.25f;
    public float screwYExtra = 0.22f;
    public float borderY = 0f;
    public float borderPadding = 0.05f;
    public Vector2 cornerPivotOffset = Vector2.zero;
    [Tooltip("Rotation Y da bake san trong prefab corner (Inspector > Rotation Y). Thuong la 0 hoac 90.")]
    public float cornerPrefabBaseRotY = 90f;

    [Header("Materials theo mau (index = BlockColor enum)")]
    [Tooltip("Thu tu: Cyan, Orange, Red, Blue, Green, Yellow, Purple")]
    public Material[] colorMaterials = new Material[7];

    GameObject levelRoot;

    public BoardController[] SpawnLevel(LevelData levelData)
    {
        if (levelRoot != null) Destroy(levelRoot);
        levelRoot = new GameObject("Level_Root");
        levelRoot.transform.SetParent(transform);
        levelRoot.transform.localPosition = Vector3.zero;

        var result = new BoardController[levelData.boards.Length];
        for (int i = 0; i < levelData.boards.Length; i++)
            result[i] = SpawnBoard(levelData.boards[i], i);

        return result;
    }

    BoardController SpawnBoard(BoardData bd, int index)
    {
        GameObject boardRoot = new GameObject($"Board_{index:00}");
        boardRoot.transform.position = new Vector3(bd.worldPosition.x, 0f, bd.worldPosition.y);
        boardRoot.transform.SetParent(levelRoot.transform);

        SpawnTiles(bd, boardRoot.transform);
        SpawnBlockers(bd, boardRoot.transform);
        SpawnBorder(bd, boardRoot.transform);

        BoardController bc = boardRoot.AddComponent<BoardController>();
        bc.Init(bd);

        foreach (var blockData in bd.blocks)
            SpawnBlock(blockData, bd, bc, boardRoot.transform);

        return bc;
    }

    void SpawnTiles(BoardData bd, Transform parent)
    {
        if (tilePrefab == null) return;

        for (int r = 0; r < bd.cellRows; r++)
        {
            for (int c = 0; c < bd.cellCols; c++)
            {
                // Bo qua o disabled (danh dau X trong Level Editor)
                if (bd.IsCellDisabled(r, c)) continue;

                // Vi tri tam o, Y = tileY
                Vector3 target = bd.CellWorldPos(r, c);
                target.y = tileY;

                // Spawn tai tam o truoc
                GameObject tile = Instantiate(tilePrefab, target, Quaternion.identity, parent);
                tile.name = $"Tile_{r}_{c}";

                // Can chinh bounds XZ de tam visual trung voi tam o
                AlignBoundsCenter(tile, target, alignY: false);
            }
        }
    }

    void SpawnBlockers(BoardData bd, Transform parent)
    {
        if (blockerPrefab == null) return;

        foreach (var cell in bd.blockerCells)
        {
            int r = cell.x;
            int c = cell.y;

            // Kiem tra hop le
            if (r < 0 || r >= bd.cellRows || c < 0 || c >= bd.cellCols) continue;

            Vector3 target = bd.CellWorldPos(r, c);
            target.y = blockerY;

            GameObject go = Instantiate(blockerPrefab, target, Quaternion.identity, parent);
            go.name = $"Blocker_{r}_{c}";

            AlignBoundsCenter(go, target, alignY: false);
        }
    }

    void SpawnBorder(BoardData bd, Transform parent)
    {
        if (edgePrefab == null) return;

        float cs = bd.cellSize;
        float p = borderPadding;

        // Huong: 0=Top(+Z), 1=Bottom(-Z), 2=Left(-X), 3=Right(+X)
        // Voi moi o ACTIVE, kiem tra 4 canh — neu canh do tiep giap ô disabled/ngoai board
        // thi spawn 1 edge piece tai do
        for (int r = 0; r < bd.cellRows; r++)
        {
            for (int c = 0; c < bd.cellCols; c++)
            {
                // Chi xu ly o dang active (khong disabled, khong blocker)
                if (bd.IsCellDisabled(r, c)) continue;

                Vector3 cellCenter = bd.CellWorldPos(r, c);

                // ── Top edge (r-1 la disabled/ngoai) ─────────────────────
                if (IsEdge(bd, r - 1, c))
                {
                    Vector3 pos = new Vector3(cellCenter.x, borderY, cellCenter.z + cs * 0.5f + p);
                    SpawnBorderPiece(edgePrefab, parent, pos, 0f, $"Edge_T_{r}_{c}", false);
                }
                // ── Bottom edge (r+1 la disabled/ngoai) ──────────────────
                if (IsEdge(bd, r + 1, c))
                {
                    Vector3 pos = new Vector3(cellCenter.x, borderY, cellCenter.z - cs * 0.5f - p);
                    SpawnBorderPiece(edgePrefab, parent, pos, 180f, $"Edge_B_{r}_{c}", false);
                }
                // ── Left edge (c-1 la disabled/ngoai) ────────────────────
                if (IsEdge(bd, r, c - 1))
                {
                    Vector3 pos = new Vector3(cellCenter.x - cs * 0.5f - p, borderY, cellCenter.z);
                    SpawnBorderPiece(edgePrefab, parent, pos, 270f, $"Edge_L_{r}_{c}", false);
                }
                // ── Right edge (c+1 la disabled/ngoai) ───────────────────
                if (IsEdge(bd, r, c + 1))
                {
                    Vector3 pos = new Vector3(cellCenter.x + cs * 0.5f + p, borderY, cellCenter.z);
                    SpawnBorderPiece(edgePrefab, parent, pos, 90f, $"Edge_R_{r}_{c}", false);
                }
            }
        }

        // ── Corners: spawn tai cac goc giua 2 edge gap nhau ──────────────
        if (cornerPrefab == null) return;

        for (int r = 0; r < bd.cellRows; r++)
        {
            for (int c = 0; c < bd.cellCols; c++)
            {
                if (bd.IsCellDisabled(r, c)) continue;

                Vector3 cc = bd.CellWorldPos(r, c);
                float hcs = cs * 0.5f;

                // 4 goc cua o nay: TopLeft, TopRight, BottomRight, BottomLeft
                // Rotation tuyet doi (the gioi):
                //   TopLeft  (quay vao goc ↘) =   0°
                //   TopRight (quay vao goc ↙) =  90°
                //   BottomRight (quay vao goc ↖) = 180°
                //   BottomLeft  (quay vao goc ↗) = 270°
                // Tru di rotation da bake san trong prefab (cornerPrefabBaseRotY)
                // de bu tru, tranh xoay doi.

                // Goc TopLeft (+Z, -X) — spawn neu ca top va left deu la edge
                if (IsEdge(bd, r - 1, c) && IsEdge(bd, r, c - 1))
                    SpawnCorner(parent,
                        new Vector3(cc.x - hcs - p, borderY, cc.z + hcs + p),
                        0f   - cornerPrefabBaseRotY, Vector2.zero, $"Corner_TL_{r}_{c}");

                // Goc TopRight (+Z, +X) — top va right deu la edge
                if (IsEdge(bd, r - 1, c) && IsEdge(bd, r, c + 1))
                    SpawnCorner(parent,
                        new Vector3(cc.x + hcs + p, borderY, cc.z + hcs + p),
                        90f  - cornerPrefabBaseRotY, Vector2.zero, $"Corner_TR_{r}_{c}");

                // Goc BottomRight (-Z, +X) — bottom va right deu la edge
                if (IsEdge(bd, r + 1, c) && IsEdge(bd, r, c + 1))
                    SpawnCorner(parent,
                        new Vector3(cc.x + hcs + p, borderY, cc.z - hcs - p),
                        180f - cornerPrefabBaseRotY, Vector2.zero, $"Corner_BR_{r}_{c}");

                // Goc BottomLeft (-Z, -X) — bottom va left deu la edge
                if (IsEdge(bd, r + 1, c) && IsEdge(bd, r, c - 1))
                    SpawnCorner(parent,
                        new Vector3(cc.x - hcs - p, borderY, cc.z - hcs - p),
                        270f - cornerPrefabBaseRotY, Vector2.zero, $"Corner_BL_{r}_{c}");
            }
        }
    }

    /// <summary>
    /// Tra ve true neu o (row, col) la "ngoai bien" — tuc la:
    /// nam ngoai board, hoac la o disabled (khong active).
    /// Edge piece can duoc spawn tiep giap voi nhung o nhu vay.
    /// </summary>
    bool IsEdge(BoardData bd, int row, int col)
    {
        // Ngoai board
        if (row < 0 || row >= bd.cellRows || col < 0 || col >= bd.cellCols)
            return true;
        // O bi an
        if (bd.IsCellDisabled(row, col))
            return true;
        return false;
    }

    void SpawnCorner(Transform parent, Vector3 worldPos, float yRotDeg,
                     Vector2 manualOffset, string objName)
    {
        worldPos.x += manualOffset.x;
        worldPos.z += manualOffset.y;

        GameObject go = Instantiate(cornerPrefab, worldPos,
                                    Quaternion.Euler(0f, yRotDeg, 0f), parent);
        go.name = objName;

        if (cornerPivotOffset != Vector2.zero)
        {
            Vector3 localOff = new Vector3(cornerPivotOffset.x, 0f, cornerPivotOffset.y);
            go.transform.position += go.transform.rotation * localOff;
        }
    }

    void SpawnBorderPiece(GameObject prefab, Transform parent,
                           Vector3 worldPos, float yRotDeg, string objName,
                           bool useCornerMode)
    {
        GameObject go = Instantiate(prefab, worldPos,
                                    Quaternion.Euler(0f, yRotDeg, 0f), parent);
        go.name = objName;

        if (useCornerMode)
        {
            if (cornerPivotOffset != Vector2.zero)
            {
                Vector3 localOff = new Vector3(cornerPivotOffset.x, 0f, cornerPivotOffset.y);
                Vector3 worldOff = go.transform.rotation * localOff;
                go.transform.position += worldOff;
            }
        }
        else
        {
            AlignBoundsCenter(go, worldPos, alignY: false);
        }
    }

    void SpawnBlock(BlockInBoardData bData, BoardData bd,
                    BoardController bc, Transform boardRoot)
    {
        Vector3 cellPos = bd.CellWorldPos(bData.cellRow, bData.cellCol);
        Vector3 worldPos = new Vector3(cellPos.x, blockY, cellPos.z);

        ScrewController sc = SpawnScrew(worldPos, boardRoot);

        // ── Chon prefab dung theo unlockDistance ──────────────────────────────
        bool use2x = bData.screw.unlockDistance >= 2f && blockPrefab2x != null;
        GameObject prefabToUse = use2x ? blockPrefab2x : blockPrefab;

        // ── Xoay prefab theo slideDir (chi ap dung cho 1x2) ─────────────────
        // Quy uoc prefab 1x2: pivot tai dau screw, mesh keo dai ve phia -Z cuc bo.
        // GetBlockRotation xoay de -Z luon tro ve NGUOC HUONG slide:
        //   Forward (+Z)  →   0°  (-Z → -Z)  ✓
        //   Back    (-Z)  → 180°  (-Z → +Z)  ✓
        //   Right   (+X)  → 270°  (-Z → -X)  ✓
        //   Left    (-X)  →  90°  (-Z → +X)  ✓
        // Block 1x1 giu nguyen Quaternion.identity (hinh vuong, khong can xoay).
        Quaternion blockRot = use2x ? GetBlockRotation(bData.slideDirection) : Quaternion.identity;

        GameObject blockGO;
        if (prefabToUse != null)
        {
            blockGO = Instantiate(prefabToUse, worldPos, blockRot);
            blockGO.transform.SetParent(boardRoot, worldPositionStays: true);

            if (use2x)
            {
                // 1x2: pivot DA o dung vi tri (tam o screw) → KHONG can AlignBoundsCenter.
                // Neu goi AlignBoundsCenter, no se dich block de tam visual trung worldPos,
                // lam screw xuat hien o giua block thay vi o dau.
            }
            else
            {
                // 1x1: can AlignBoundsCenter de bu pivot lech cua prefab.
                AlignBoundsCenter(blockGO, worldPos, alignY: false);
            }
        }
        else
        {
            blockGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
            blockGO.transform.position = worldPos;
            blockGO.transform.localScale = new Vector3(bd.cellSize * 0.9f, 0.35f, bd.cellSize * 0.9f);
            blockGO.transform.SetParent(boardRoot, worldPositionStays: true);
        }

        blockGO.name = $"Block_{bData.color}_R{bData.cellRow}C{bData.cellCol}";
        ApplyColor(blockGO, bData.color);

        if (blockGO.GetComponent<Collider>() == null)
            blockGO.AddComponent<BoxCollider>();

        BlockController block = blockGO.AddComponent<BlockController>();
        block.Init(bc, bData.slideDirection, bData.screw.unlockDistance, sc, bd.cellSize);
        bc.RegisterBlock(block);
    }

    /// <summary>
    /// Tinh rotation Y cho prefab block 1x2.
    ///
    /// Gia dinh prefab: pivot tai TAM O SCREW, mesh keo dai ve phia +Z cuc bo.
    /// Unity xoay Y theo chieu kim dong ho khi nhin tu tren xuong:
    ///   +Z --(90°Y)--> +X --(90°Y)--> -Z --(90°Y)--> -X
    ///
    /// Xoay de vector +Z cuc bo tro ve NGUOC CHIEU slide trong world:
    ///   Forward (+Z)  → 180°  :  local +Z  →  world -Z   ✓
    ///   Back    (-Z)  →   0°  :  local +Z  →  world +Z   ✓
    ///   Right   (+X)  → 270°  :  local +Z  →  world -X   ✓
    ///   Left    (-X)  →  90°  :  local +Z  →  world +X   ✓
    /// </summary>
    static Quaternion GetBlockRotation(MoveDirection dir)
    {
        return dir switch
        {
            MoveDirection.Forward => Quaternion.Euler(0f, 180f, 0f),
            MoveDirection.Back    => Quaternion.Euler(0f,   0f, 0f),
            MoveDirection.Right   => Quaternion.Euler(0f, 270f, 0f),
            MoveDirection.Left    => Quaternion.Euler(0f,  90f, 0f),
            _                     => Quaternion.identity
        };
    }

    ScrewController SpawnScrew(Vector3 blockWorldPos, Transform boardRoot)
    {
        Vector3 screwPos = new Vector3(blockWorldPos.x,
                                       blockWorldPos.y + screwYExtra,
                                       blockWorldPos.z);
        GameObject screwGO;
        if (screwPrefab != null)
        {
            screwGO = Instantiate(screwPrefab, screwPos, Quaternion.identity, boardRoot);
            AlignBoundsCenter(screwGO, screwPos, alignY: false);
        }
        else
        {
            screwGO = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            screwGO.transform.position = screwPos;
            screwGO.transform.localScale = new Vector3(0.18f, 0.12f, 0.18f);
            screwGO.transform.SetParent(boardRoot, worldPositionStays: true);
            if (screwGO.TryGetComponent<Collider>(out var col)) Destroy(col);
        }

        screwGO.name = "Screw";
        return screwGO.AddComponent<ScrewController>();
    }

    static void AlignBoundsCenter(GameObject go, Vector3 targetWorldPos, bool alignY)
    {
        Renderer[] renderers = go.GetComponentsInChildren<Renderer>(includeInactive: true);
        if (renderers == null || renderers.Length == 0) return;

        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            b.Encapsulate(renderers[i].bounds);

        Vector3 currentCenter = b.center;

        float dx = targetWorldPos.x - currentCenter.x;
        float dz = targetWorldPos.z - currentCenter.z;
        float dy = alignY ? (targetWorldPos.y - currentCenter.y) : 0f;

        go.transform.position += new Vector3(dx, dy, dz);
    }

    void ApplyColor(GameObject go, BlockColor color)
    {
        if (go.TryGetComponent<MeshRenderer>(out var mr))
        {
            ApplyMaterialToRenderer(mr, color);
            return;
        }

        MeshRenderer[] children = go.GetComponentsInChildren<MeshRenderer>();
        foreach (var r in children)
            ApplyMaterialToRenderer(r, color);
    }

    void ApplyMaterialToRenderer(MeshRenderer mr, BlockColor color)
    {
        int idx = (int)color;

        // Luon dung MaterialPropertyBlock (per-instance) thay vi sharedMaterial.
        // Neu dung sharedMaterial, tat ca object dung chung asset do se bi doi mau cung luc
        // → bug "mau sai cho den khi mo screw".
        var mpb = new MaterialPropertyBlock();
        mr.GetPropertyBlock(mpb);

        if (colorMaterials != null && idx < colorMaterials.Length && colorMaterials[idx] != null)
        {
            // Assign material dung, nhung dat mau qua MPB de isolate per-instance
            mr.sharedMaterial = colorMaterials[idx];
            Color col = colorMaterials[idx].HasProperty("_BaseColor")
                ? colorMaterials[idx].GetColor("_BaseColor")
                : colorMaterials[idx].color;
            mpb.SetColor("_BaseColor", col);
        }
        else
        {
            mpb.SetColor("_BaseColor", ColorMap.Get(color));
        }

        mr.SetPropertyBlock(mpb);
    }
}