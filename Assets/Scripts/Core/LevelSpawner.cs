using UnityEngine;

public class LevelSpawner : MonoBehaviour
{
    public static LevelSpawner Instance { get; private set; }

    void Awake()
    {
        Instance = this;
    }

    [Header("Block & Screw")]
    public GameObject blockPrefab;

    public GameObject blockPrefab2x;

    [Header("Smooth Blocks (Prefab cu nhẵn, dùng sau khi mở khóa)")]
    public GameObject smoothBlockPrefab;

    public GameObject smoothBlockPrefab2x;

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
    public float cornerPrefabBaseRotY = 90f;
    public float edgeCornerShrink = 0.08f;

    [Header("Materials theo mau (index = BlockColor enum)")]
    public Material[] colorMaterials = new Material[8];

    [Header("Sand Block Effects")]
    public GameObject sandBreakEffectPrefab;
    public Sprite[] sandPieceSprites;

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

        for (int r = 0; r < bd.cellRows; r++)
        {
            for (int c = 0; c < bd.cellCols; c++)
            {
                // Chi xu ly o dang active (khong disabled, khong blocker)
                if (bd.IsCellDisabled(r, c)) continue;

                Vector3 cellCenter = bd.CellWorldPos(r, c);

                if (IsEdge(bd, r - 1, c))
                {
                    Vector3 pos = new Vector3(cellCenter.x, borderY, cellCenter.z + cs * 0.5f + p);
                    SpawnEdgePiece(bd, parent, pos, 0f, $"Edge_T_{r}_{c}", HasCorner(bd, r, c), HasCorner(bd, r, c + 1), Vector3.right);
                }
                if (IsEdge(bd, r + 1, c))
                {
                    Vector3 pos = new Vector3(cellCenter.x, borderY, cellCenter.z - cs * 0.5f - p);
                    SpawnEdgePiece(bd, parent, pos, 180f, $"Edge_B_{r}_{c}", HasCorner(bd, r + 1, c), HasCorner(bd, r + 1, c + 1), Vector3.right);
                }
                if (IsEdge(bd, r, c - 1))
                {
                    Vector3 pos = new Vector3(cellCenter.x - cs * 0.5f - p, borderY, cellCenter.z);
                    SpawnEdgePiece(bd, parent, pos, 270f, $"Edge_L_{r}_{c}", HasCorner(bd, r, c), HasCorner(bd, r + 1, c), Vector3.back);
                }
                if (IsEdge(bd, r, c + 1))
                {
                    Vector3 pos = new Vector3(cellCenter.x + cs * 0.5f + p, borderY, cellCenter.z);
                    SpawnEdgePiece(bd, parent, pos, 90f, $"Edge_R_{r}_{c}", HasCorner(bd, r, c + 1), HasCorner(bd, r + 1, c + 1), Vector3.back);
                }
            }
        }

        if (cornerPrefab == null) return;

        for (int r = 0; r <= bd.cellRows; r++)
        {
            for (int c = 0; c <= bd.cellCols; c++)
            {
                bool tl = IsEdge(bd, r - 1, c - 1);
                bool tr = IsEdge(bd, r - 1, c);
                bool bl = IsEdge(bd, r, c - 1);
                bool br = IsEdge(bd, r, c);

                float halfW = bd.TotalWidth * 0.5f;
                float halfH = bd.TotalHeight * 0.5f;
                float cx = bd.worldPosition.x - halfW + c * cs;
                float cz = bd.worldPosition.y + halfH - r * cs;

                // 1. TL Corner Piece (rotation 0) - Associated with cell BR
                // - Outer corner of BR (!br && tr && bl) -> sits at TL quadrant (cx - p, cz + p)
                // - Inner corner of BR (br && !tl && !tr && !bl) -> sits at BR quadrant (cx + p, cz - p)
                if (!br && tr && bl)
                {
                    SpawnCorner(parent,
                        new Vector3(cx - p, borderY, cz + p),
                        0f - cornerPrefabBaseRotY, Vector2.zero, $"Corner_TL_{r}_{c}");
                }
                else if (br && !tl && !tr && !bl)
                {
                    SpawnCorner(parent,
                        new Vector3(cx + p, borderY, cz - p),
                        0f - cornerPrefabBaseRotY, Vector2.zero, $"Corner_TL_Inner_{r}_{c}");
                }

                // 2. TR Corner Piece (rotation 90) - Associated with cell BL
                // - Outer corner of BL (!bl && tl && br) -> sits at TR quadrant (cx + p, cz + p)
                // - Inner corner of BL (bl && !tl && !tr && !br) -> sits at BL quadrant (cx - p, cz - p)
                if (!bl && tl && br)
                {
                    SpawnCorner(parent,
                        new Vector3(cx + p, borderY, cz + p),
                        90f - cornerPrefabBaseRotY, Vector2.zero, $"Corner_TR_{r}_{c}");
                }
                else if (bl && !tl && !tr && !br)
                {
                    SpawnCorner(parent,
                        new Vector3(cx - p, borderY, cz - p),
                        90f - cornerPrefabBaseRotY, Vector2.zero, $"Corner_TR_Inner_{r}_{c}");
                }

                // 3. BR Corner Piece (rotation 180) - Associated with cell TL
                // - Outer corner of TL (!tl && bl && tr) -> sits at BR quadrant (cx + p, cz - p)
                // - Inner corner of TL (tl && !tr && !bl && !br) -> sits at TL quadrant (cx - p, cz + p)
                if (!tl && bl && tr)
                {
                    SpawnCorner(parent,
                        new Vector3(cx + p, borderY, cz - p),
                        180f - cornerPrefabBaseRotY, Vector2.zero, $"Corner_BR_{r}_{c}");
                }
                else if (tl && !tr && !bl && !br)
                {
                    SpawnCorner(parent,
                        new Vector3(cx - p, borderY, cz + p),
                        180f - cornerPrefabBaseRotY, Vector2.zero, $"Corner_BR_Inner_{r}_{c}");
                }

                // 4. BL Corner Piece (rotation 270) - Associated with cell TR
                // - Outer corner of TR (!tr && tl && br) -> sits at BL quadrant (cx - p, cz - p)
                // - Inner corner of TR (tr && !tl && !bl && !br) -> sits at TR quadrant (cx + p, cz + p)
                if (!tr && tl && br)
                {
                    SpawnCorner(parent,
                        new Vector3(cx - p, borderY, cz - p),
                        270f - cornerPrefabBaseRotY, Vector2.zero, $"Corner_BL_{r}_{c}");
                }
                else if (tr && !tl && !bl && !br)
                {
                    SpawnCorner(parent,
                        new Vector3(cx + p, borderY, cz + p),
                        270f - cornerPrefabBaseRotY, Vector2.zero, $"Corner_BL_Inner_{r}_{c}");
                }
            }
        }
    }

    void SpawnEdgePiece(BoardData bd, Transform parent, Vector3 basePos, float yRotDeg, string objName,
                        bool hasStartCorner, bool hasEndCorner, Vector3 shiftDir)
    {
        GameObject go = Instantiate(edgePrefab, basePos, Quaternion.Euler(0f, yRotDeg, 0f), parent);
        go.name = objName;

        AlignBoundsCenter(go, basePos, alignY: false);

        int cornerCount = (hasStartCorner ? 1 : 0) + (hasEndCorner ? 1 : 0);
        if (cornerCount > 0 && edgeCornerShrink > 0f)
        {
            float scaleX = 1f - (cornerCount * edgeCornerShrink);
            Vector3 localScale = go.transform.localScale;
            localScale.x *= scaleX;
            go.transform.localScale = localScale;

            if (cornerCount == 1)
            {
                float shiftDist = edgeCornerShrink * 0.5f * bd.cellSize;
                Vector3 worldShift = shiftDir * (hasStartCorner ? shiftDist : -shiftDist);
                go.transform.position += worldShift;
            }
        }
    }

    bool HasCorner(BoardData bd, int r, int c)
    {
        if (cornerPrefab == null) return false;

        bool tl = IsEdge(bd, r - 1, c - 1);
        bool tr = IsEdge(bd, r - 1, c);
        bool bl = IsEdge(bd, r, c - 1);
        bool br = IsEdge(bd, r, c);

        // 1. TL Corner
        if (!br && tr && bl) return true;
        if (br && !tl && !tr && !bl) return true;

        // 2. TR Corner
        if (!bl && tl && br) return true;
        if (bl && !tl && !tr && !br) return true;

        // 3. BR Corner
        if (!tl && bl && tr) return true;
        if (tl && !tr && !bl && !br) return true;

        // 4. BL Corner
        if (!tr && tl && br) return true;
        if (tr && !tl && !bl && !br) return true;

        return false;
    }

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

        bool use2x = bData.screw.unlockDistance >= 2f && blockPrefab2x != null;
        GameObject prefabToUse = use2x ? blockPrefab2x : blockPrefab;

        Quaternion baseRot = prefabToUse != null ? prefabToUse.transform.rotation : Quaternion.identity;
        Quaternion rotOffset = use2x ? GetBlockRotation(bData.slideDirection) : Get1x1BlockRotation(bData.slideDirection);
        Quaternion blockRot = rotOffset * baseRot;

        GameObject blockGO;
        if (prefabToUse != null)
        {
            blockGO = Instantiate(prefabToUse, worldPos, blockRot);
            blockGO.transform.SetParent(boardRoot, worldPositionStays: true);

            if (use2x)
            {

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

        if (blockGO.GetComponentInChildren<Collider>() == null)
            blockGO.AddComponent<BoxCollider>();

        BlockController block = blockGO.AddComponent<BlockController>();
        block.Init(bc, bData.slideDirection, bData.screw.unlockDistance, sc, bData.color, bd.cellSize);
        bc.RegisterBlock(block);
    }

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

    static Quaternion Get1x1BlockRotation(MoveDirection dir)
    {
        // Giả sử rãnh trên model 3D (Blender) mặc định hướng về phía BACK (-Z)
        return dir switch
        {
            MoveDirection.Forward => Quaternion.Euler(0f, 0f, 0f),      // Trượt Forward -> Rãnh hướng Back (0 độ)
            MoveDirection.Back    => Quaternion.Euler(0f, 180f, 0f),    // Trượt Back -> Rãnh hướng Forward (180 độ)
            MoveDirection.Right   => Quaternion.Euler(0f, 90f, 0f),     // Trượt Right -> Rãnh hướng Left (90 độ)
            MoveDirection.Left    => Quaternion.Euler(0f, 270f, 0f),    // Trượt Left -> Rãnh hướng Right (270 độ)
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

    public void ApplyColor(GameObject go, BlockColor color)
    {
        MeshRenderer[] renderers = go.GetComponentsInChildren<MeshRenderer>(includeInactive: true);
        foreach (var r in renderers)
        {
            ApplyMaterialToRenderer(r, color);
        }
    }

    void ApplyMaterialToRenderer(MeshRenderer mr, BlockColor color)
    {
        int idx = (int)color;

        var mpb = new MaterialPropertyBlock();
        mr.GetPropertyBlock(mpb);

        Color col;
        if (colorMaterials != null && idx < colorMaterials.Length && colorMaterials[idx] != null)
        {
            mr.sharedMaterial = colorMaterials[idx];
            col = colorMaterials[idx].HasProperty("_BaseColor")
                ? colorMaterials[idx].GetColor("_BaseColor")
                : (colorMaterials[idx].HasProperty("_Color") ? colorMaterials[idx].GetColor("_Color") : Color.white);
        }
        else
        {
            col = ColorMap.Get(color);
        }

        mpb.SetColor("_BaseColor", col);
        mpb.SetColor("_Color", col);

        mr.SetPropertyBlock(mpb);
    }
}