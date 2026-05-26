#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class LevelEditorWindow : EditorWindow
{
    LevelData targetLevel;
    Vector2 scrollPos;
    int selectedBoardIndex = -1;

    const float CANVAS_W = 520f;
    const float CANVAS_H = 420f;
    const float CELL_PX = 52f;   
    const float CANVAS_CX = CANVAS_W * 0.5f;
    const float CANVAS_CY = CANVAS_H * 0.5f;
    const float WORLD_TO_PIX = CELL_PX; 
    int draggingBoard = -1;
    bool isDraggingBoard = false;
    Vector2 dragOffset;

    enum PaintMode { None, DisableCell, BlockerCell }
    PaintMode paintMode = PaintMode.None;

    static readonly Color DisabledCellColor = new Color(0.08f, 0.08f, 0.10f, 0.92f);
    static readonly Color BlockerCellColor = new Color(0.70f, 0.35f, 0.10f, 0.88f);

    int newBoardRows = 2;
    int newBoardCols = 1;
    float newCellSize = 1f;

    static readonly Color[] BlockColors =
    {
        new Color(0.30f, 0.78f, 0.90f), // Cyan
        new Color(0.95f, 0.52f, 0.15f), // Orange
        new Color(0.88f, 0.22f, 0.22f), // Red
        new Color(0.20f, 0.42f, 0.90f), // Blue
        new Color(0.22f, 0.78f, 0.32f), // Green
        new Color(0.95f, 0.85f, 0.10f), // Yellow
        new Color(0.60f, 0.22f, 0.82f), // Purple
    };

    static readonly Color BoardBgColor = new Color(0.18f, 0.20f, 0.24f, 0.95f);
    static readonly Color BoardSelectColor = new Color(0.30f, 0.65f, 1.00f, 0.30f);
    static readonly Color CellBorderColor = new Color(0.35f, 0.38f, 0.44f, 1.00f);
    static readonly Color BoardBorderColor = new Color(0.55f, 0.55f, 0.60f, 1.00f);
    static readonly Color SelectedBorderClr = new Color(0.30f, 0.65f, 1.00f, 1.00f);
    static readonly Color CanvasBgColor = new Color(0.13f, 0.14f, 0.17f);
    static readonly Color GridColor = new Color(1f, 1f, 1f, 0.04f);

    [MenuItem("ScrewUnlock/Level Editor")]
    static void Open()
    {
        var win = GetWindow<LevelEditorWindow>("Screw Unlock — Level Editor");
        win.minSize = new Vector2(940, 620);
        win.Show();
    }

    void OnGUI()
    {
        DrawToolbar();

        if (targetLevel == null) { DrawEmptyState(); return; }

        EditorGUILayout.BeginHorizontal();
        DrawLeftPanel();
        DrawRightPanel();
        EditorGUILayout.EndHorizontal();

        if (Event.current.type == EventType.MouseMove) Repaint();
    }

    void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        GUILayout.Label("Level:", EditorStyles.toolbarButton, GUILayout.Width(45));
        var newLevel = (LevelData)EditorGUILayout.ObjectField(
            targetLevel, typeof(LevelData), false, GUILayout.Width(200));
        if (newLevel != targetLevel) { targetLevel = newLevel; selectedBoardIndex = -1; Repaint(); }

        GUILayout.Space(8);
        if (GUILayout.Button("New Level", EditorStyles.toolbarButton, GUILayout.Width(80)))
            CreateNewLevel();

        GUILayout.FlexibleSpace();

        if (targetLevel != null)
        {
            GUI.color = new Color(0.5f, 1f, 0.5f);
            if (GUILayout.Button("▶  Test in Play", EditorStyles.toolbarButton, GUILayout.Width(110)))
            {
                Selection.activeObject = targetLevel;
                EditorGUIUtility.PingObject(targetLevel);
            }
            GUI.color = Color.white;
        }

        EditorGUILayout.EndHorizontal();
    }

    void DrawEmptyState()
    {
        GUILayout.FlexibleSpace();
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        EditorGUILayout.BeginVertical();

        var bigLabel = new GUIStyle(EditorStyles.largeLabel)
        {
            fontSize = 18,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.5f, 0.5f, 0.5f) }
        };
        GUILayout.Label("Chon hoac tao Level Data de bat dau", bigLabel, GUILayout.Height(40));
        GUILayout.Space(12);
        if (GUILayout.Button("  +  Tao Level moi", GUILayout.Height(36), GUILayout.Width(200)))
            CreateNewLevel();

        EditorGUILayout.EndVertical();
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
        GUILayout.FlexibleSpace();
    }

    void DrawLeftPanel()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(CANVAS_W + 20));
        GUILayout.Space(6);

        GUILayout.Label($"  Preview  ({targetLevel.boards.Length} board(s))", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Paint:", GUILayout.Width(38));

        bool pNone = paintMode == PaintMode.None;
        bool pDisable = paintMode == PaintMode.DisableCell;
        bool pBlocker = paintMode == PaintMode.BlockerCell;

        GUI.color = pNone ? new Color(0.7f, 1f, 0.7f) : Color.white;
        if (GUILayout.Toggle(pNone, "  ✋  Di chuyen board", EditorStyles.miniButtonLeft,
            GUILayout.Height(22)) && !pNone)
            paintMode = PaintMode.None;

        GUI.color = pDisable ? new Color(0.4f, 0.4f, 0.4f) : Color.white;
        if (GUILayout.Toggle(pDisable, "  ✕  An o (Disable)", EditorStyles.miniButtonMid,
            GUILayout.Height(22)) && !pDisable)
            paintMode = PaintMode.DisableCell;

        GUI.color = pBlocker ? new Color(1f, 0.65f, 0.3f) : Color.white;
        if (GUILayout.Toggle(pBlocker, "  ⬛  O chan (Blocker)", EditorStyles.miniButtonRight,
            GUILayout.Height(22)) && !pBlocker)
            paintMode = PaintMode.BlockerCell;

        GUI.color = Color.white;
        EditorGUILayout.EndHorizontal();
        GUILayout.Space(2);

        Rect canvasRect = GUILayoutUtility.GetRect(CANVAS_W, CANVAS_H);
        DrawCanvas(canvasRect);

        GUILayout.Space(6);
        DrawAddBoardPanel();
        EditorGUILayout.EndVertical();
    }

    void DrawCanvas(Rect rect)
    {
        EditorGUI.DrawRect(rect, CanvasBgColor);
        DrawGrid(rect);

        Event e = Event.current;

        for (int i = 0; i < targetLevel.boards.Length; i++)
        {
            BoardData bd = targetLevel.boards[i];
            Rect boardPx = BoardToCanvasRect(bd, rect);
            bool selected = (i == selectedBoardIndex);

            EditorGUI.DrawRect(boardPx, BoardBgColor);
            if (selected) EditorGUI.DrawRect(boardPx, BoardSelectColor);

            DrawCellGrid(boardPx, bd);

            DrawBlocksInBoard(boardPx, bd);

            DrawRectBorder(boardPx,
                selected ? SelectedBorderClr : BoardBorderColor, 2f);

            var lbl = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = new Color(0.75f, 0.75f, 0.75f) },
                alignment = TextAnchor.UpperLeft
            };
            GUI.Label(new Rect(boardPx.x + 4, boardPx.y + 2, 160, 16),
                      $"B{i}  {bd.cellRows}×{bd.cellCols}  dis:{bd.disabledCells.Length}  blk:{bd.blockerCells.Length}", lbl);

            if (paintMode != PaintMode.None)
                HandleCellPaint(e, boardPx, i, bd);
            else
                HandleBoardDrag(e, rect, boardPx, i, bd);
        }

        if (e.type == EventType.MouseUp && isDraggingBoard)
        {
            isDraggingBoard = false;
            draggingBoard = -1;
            Repaint();
        }

        if (e.type == EventType.MouseDown && rect.Contains(e.mousePosition))
        {
            bool hit = false;
            for (int i = 0; i < targetLevel.boards.Length; i++)
            {
                if (BoardToCanvasRect(targetLevel.boards[i], rect).Contains(e.mousePosition))
                { hit = true; break; }
            }
            if (!hit) { selectedBoardIndex = -1; Repaint(); }
        }

        Vector2 origin = CanvasOrigin(rect);
        DrawLine(new Vector2(origin.x - 8, origin.y), new Vector2(origin.x + 8, origin.y),
            new Color(1f, 1f, 1f, 0.18f), 1f);
        DrawLine(new Vector2(origin.x, origin.y - 8), new Vector2(origin.x, origin.y + 8),
            new Color(1f, 1f, 1f, 0.18f), 1f);
    }

    void DrawGrid(Rect rect)
    {
        float step = WORLD_TO_PIX;
        for (float x = CANVAS_CX % step; x < rect.width; x += step)
            DrawLine(new Vector2(rect.x + x, rect.y),
                     new Vector2(rect.x + x, rect.y + rect.height), GridColor, 1f);
        for (float y = CANVAS_CY % step; y < rect.height; y += step)
            DrawLine(new Vector2(rect.x, rect.y + y),
                     new Vector2(rect.x + rect.width, rect.y + y), GridColor, 1f);
    }

    void DrawCellGrid(Rect boardPx, BoardData bd)
    {
        float cellW = boardPx.width / bd.cellCols;
        float cellH = boardPx.height / bd.cellRows;

        for (int r = 0; r < bd.cellRows; r++)
        {
            for (int c = 0; c < bd.cellCols; c++)
            {
                Rect cellRect = new Rect(
                    boardPx.x + c * cellW,
                    boardPx.y + r * cellH,
                    cellW, cellH);

                if (bd.IsCellDisabled(r, c))
                {
                    EditorGUI.DrawRect(cellRect, DisabledCellColor);
                    DrawLine(new Vector2(cellRect.x + 4, cellRect.y + 4),
                             new Vector2(cellRect.xMax - 4, cellRect.yMax - 4),
                             new Color(0.4f, 0.4f, 0.4f, 0.6f), 1.5f);
                    DrawLine(new Vector2(cellRect.xMax - 4, cellRect.y + 4),
                             new Vector2(cellRect.x + 4, cellRect.yMax - 4),
                             new Color(0.4f, 0.4f, 0.4f, 0.6f), 1.5f);
                }
                else if (bd.IsCellBlocker(r, c))
                {
                    EditorGUI.DrawRect(cellRect, BlockerCellColor);
                    float inset = cellW * 0.18f;
                    Rect inner = new Rect(cellRect.x + inset, cellRect.y + inset,
                                         cellRect.width - inset * 2, cellRect.height - inset * 2);
                    EditorGUI.DrawRect(inner, new Color(0.9f, 0.5f, 0.1f, 0.9f));
                    DrawRectBorder(inner, new Color(1f, 0.75f, 0.3f, 0.8f), 1.5f);
                    var bs = new GUIStyle(EditorStyles.miniLabel)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        fontSize = Mathf.Max(8, (int)(cellW * 0.28f)),
                        normal = { textColor = new Color(1f, 0.95f, 0.8f) }
                    };
                    GUI.Label(inner, "B", bs);
                }
            }
        }

        for (int r = 1; r < bd.cellRows; r++)
        {
            float y = boardPx.y + r * cellH;
            DrawLine(new Vector2(boardPx.x, y), new Vector2(boardPx.xMax, y),
                     CellBorderColor, 1f);
        }
        for (int c = 1; c < bd.cellCols; c++)
        {
            float x = boardPx.x + c * cellW;
            DrawLine(new Vector2(x, boardPx.y), new Vector2(x, boardPx.yMax),
                     CellBorderColor, 1f);
        }
    }

    void DrawBlocksInBoard(Rect boardPx, BoardData bd)
    {
        float cellW = boardPx.width / bd.cellCols;
        float cellH = boardPx.height / bd.cellRows;
        float blockW = cellW * 0.78f;
        float blockH = cellH * 0.78f;

        foreach (var block in bd.blocks)
        {
            int row = Mathf.Clamp(block.cellRow, 0, bd.cellRows - 1);
            int col = Mathf.Clamp(block.cellCol, 0, bd.cellCols - 1);

            float cx = boardPx.x + (col + 0.5f) * cellW;
            float cy = boardPx.y + (row + 0.5f) * cellH;

            Rect blockRect = new Rect(cx - blockW * 0.5f, cy - blockH * 0.5f, blockW, blockH);

            int colorIdx = (int)block.color;
            Color bc = colorIdx < BlockColors.Length ? BlockColors[colorIdx] : Color.white;
            EditorGUI.DrawRect(blockRect, bc);
            DrawRectBorder(blockRect, Color.black * 0.4f, 1f);

            DrawScrewAt(new Vector2(cx, cy), Mathf.Min(blockW, blockH) * 0.18f);

            DrawBlockDirectionArrow(new Vector2(cx, cy), block.slideDirection,
                                    Mathf.Min(blockW, blockH));
        }
    }

    void DrawScrewAt(Vector2 center, float r)
    {
        Rect sr = new Rect(center.x - r, center.y - r, r * 2, r * 2);
        EditorGUI.DrawRect(sr, new Color(0.92f, 0.92f, 0.92f));
        DrawRectBorder(sr, Color.black * 0.55f, 1f);
        DrawLine(new Vector2(center.x - r + 1, center.y), new Vector2(center.x + r - 1, center.y),
            Color.black * 0.65f, 1.5f);
        DrawLine(new Vector2(center.x, center.y - r + 1), new Vector2(center.x, center.y + r - 1),
            Color.black * 0.65f, 1.5f);
    }

    void DrawBlockDirectionArrow(Vector2 blockCenter, MoveDirection dir, float size)
    {
        Vector2 canvasDir = DirToCanvas(dir);
        float len = size * 0.34f;
        Vector2 tip = blockCenter + canvasDir * len;
        Vector2 src = blockCenter + canvasDir * (len * 0.2f);

        Color ac = new Color(1f, 1f, 1f, 0.85f);
        DrawLine(src, tip, ac, 2f);
        DrawArrowHead(tip, canvasDir, ac, 6f);
    }

    static Vector2 DirToCanvas(MoveDirection dir)
    {
        return dir switch
        {
            MoveDirection.Right => Vector2.right,
            MoveDirection.Left => -Vector2.right,
            MoveDirection.Forward => -Vector2.up,
            MoveDirection.Back => Vector2.up,
            _ => Vector2.right
        };
    }

    void HandleCellPaint(Event e, Rect boardPx, int boardIdx, BoardData bd)
    {
        if (e.type != EventType.MouseDown && e.type != EventType.MouseDrag) return;
        if (!boardPx.Contains(e.mousePosition)) return;

        float cellW = boardPx.width / bd.cellCols;
        float cellH = boardPx.height / bd.cellRows;
        int col = Mathf.FloorToInt((e.mousePosition.x - boardPx.x) / cellW);
        int row = Mathf.FloorToInt((e.mousePosition.y - boardPx.y) / cellH);
        col = Mathf.Clamp(col, 0, bd.cellCols - 1);
        row = Mathf.Clamp(row, 0, bd.cellRows - 1);

        Undo.RecordObject(targetLevel, "Paint Cell");

        if (paintMode == PaintMode.DisableCell)
        {
            var dList = new List<Vector2Int>(bd.disabledCells);
            var bList = new List<Vector2Int>(bd.blockerCells);
            var key = new Vector2Int(row, col);
            if (dList.Contains(key))
                dList.Remove(key);
            else
            {
                dList.Add(key);
                bList.Remove(key);
            }
            bd.disabledCells = dList.ToArray();
            bd.blockerCells = bList.ToArray();
        }
        else if (paintMode == PaintMode.BlockerCell)
        {
            var dList = new List<Vector2Int>(bd.disabledCells);
            var bList = new List<Vector2Int>(bd.blockerCells);
            var key = new Vector2Int(row, col);
            if (bList.Contains(key))
                bList.Remove(key);
            else
            {
                bList.Add(key);
                dList.Remove(key);
            }
            bd.disabledCells = dList.ToArray();
            bd.blockerCells = bList.ToArray();
        }

        targetLevel.boards[boardIdx] = bd;
        EditorUtility.SetDirty(targetLevel);
        e.Use();
        Repaint();
    }

    void HandleBoardDrag(Event e, Rect canvasRect, Rect boardPx, int i, BoardData bd)
    {
        if (e.type == EventType.MouseDown && boardPx.Contains(e.mousePosition))
        {
            selectedBoardIndex = i;
            isDraggingBoard = true;
            draggingBoard = i;
            dragOffset = e.mousePosition - new Vector2(boardPx.x, boardPx.y);
            GUI.FocusControl(null);
            e.Use();
            Repaint();
        }

        if (e.type == EventType.MouseDrag && isDraggingBoard && draggingBoard == i)
        {
            Vector2 newPixelPos = e.mousePosition - dragOffset;
            Vector2 origin = CanvasOrigin(canvasRect);
            float newWorldX = (newPixelPos.x + boardPx.width * 0.5f - origin.x) / WORLD_TO_PIX;
            float newWorldY = (origin.y - (newPixelPos.y + boardPx.height * 0.5f)) / WORLD_TO_PIX;

            newWorldX = Mathf.Round(newWorldX * 2f) * 0.5f;
            newWorldY = Mathf.Round(newWorldY * 2f) * 0.5f;

            Undo.RecordObject(targetLevel, "Move Board");
            bd.worldPosition = new Vector2(newWorldX, newWorldY);
            targetLevel.boards[i] = bd;
            EditorUtility.SetDirty(targetLevel);
            e.Use();
            Repaint();
        }
    }

    void DrawRightPanel()
    {
        EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
        GUILayout.Space(6);
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        DrawLevelInfo();
        GUILayout.Space(8);

        if (selectedBoardIndex >= 0 && selectedBoardIndex < targetLevel.boards.Length)
            DrawBoardInspector(selectedBoardIndex);
        else
            DrawBoardList();

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    void DrawLevelInfo()
    {
        EditorGUILayout.LabelField("Level Info", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        int newIdx = EditorGUILayout.IntField("Level Index", targetLevel.levelIndex);
        float newTime = EditorGUILayout.FloatField("Time Limit (s)", targetLevel.timeLimit);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(targetLevel, "Edit Level Info");
            targetLevel.levelIndex = newIdx;
            targetLevel.timeLimit = newTime;
            EditorUtility.SetDirty(targetLevel);
        }
    }

    void DrawBoardList()
    {
        EditorGUILayout.LabelField($"Boards ({targetLevel.boards.Length})", EditorStyles.boldLabel);
        GUILayout.Space(4);

        for (int i = 0; i < targetLevel.boards.Length; i++)
        {
            BoardData bd = targetLevel.boards[i];
            EditorGUILayout.BeginHorizontal("box");

            var btnStyle = new GUIStyle(EditorStyles.miniButtonLeft) { alignment = TextAnchor.MiddleLeft };
            string lbl = $"Board {i}  |  {bd.blocks.Length} block(s)  |  {bd.cellRows}×{bd.cellCols} o";
            if (GUILayout.Button(lbl, btnStyle))
            { selectedBoardIndex = i; Repaint(); }

            GUI.color = new Color(1f, 0.4f, 0.4f);
            bool removeBoard = GUILayout.Button("✕", EditorStyles.miniButtonRight, GUILayout.Width(24));
            GUI.color = Color.white;
            EditorGUILayout.EndHorizontal();

            if (removeBoard)
            {
                Undo.RecordObject(targetLevel, "Remove Board");
                var list = new List<BoardData>(targetLevel.boards);
                list.RemoveAt(i);
                targetLevel.boards = list.ToArray();
                if (selectedBoardIndex >= list.Count) selectedBoardIndex = -1;
                EditorUtility.SetDirty(targetLevel);
                Repaint();
                break;
            }
        }
    }

    void DrawBoardInspector(int idx)
    {
        BoardData bd = targetLevel.boards[idx];

        EditorGUILayout.BeginHorizontal();
        bool goBack = GUILayout.Button("← Boards", EditorStyles.miniButton, GUILayout.Width(70));
        EditorGUILayout.LabelField($"Board {idx}  ({bd.cellRows}×{bd.cellCols} ô)", EditorStyles.boldLabel);
        EditorGUILayout.EndHorizontal();
        if (goBack) { selectedBoardIndex = -1; Repaint(); return; }

        GUILayout.Space(4);
        EditorGUI.BeginChangeCheck();

        Vector2 newWorldPos = EditorGUILayout.Vector2Field("World Position (XZ)", bd.worldPosition);

        GUILayout.Space(4);
        EditorGUILayout.LabelField("Luoi o (Cell Grid)", EditorStyles.boldLabel);
        int newRows = Mathf.Max(1, EditorGUILayout.IntField("So hang (Rows)", bd.cellRows));
        int newCols = Mathf.Max(1, EditorGUILayout.IntField("So cot (Cols)", bd.cellCols));
        float newCellSize = Mathf.Max(0.1f, EditorGUILayout.FloatField("Cell Size (units)", bd.cellSize));

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(targetLevel, "Edit Board");
            bd.worldPosition = newWorldPos;
            bd.cellRows = newRows;
            bd.cellCols = newCols;
            bd.cellSize = newCellSize;
            targetLevel.boards[idx] = bd;
            EditorUtility.SetDirty(targetLevel);
        }

        var infoStyle = new GUIStyle(EditorStyles.miniLabel)
        { normal = { textColor = new Color(0.6f, 0.8f, 1.0f) } };
        EditorGUILayout.LabelField(
            $"→ Kich thuoc world: {bd.TotalWidth:F1} × {bd.TotalHeight:F1} units", infoStyle);

        GUILayout.Space(8);
        EditorGUILayout.LabelField("Cell Mask", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal("helpbox");
        var maskInfo = new GUIStyle(EditorStyles.miniLabel) { richText = true };
        EditorGUILayout.LabelField(
            $"<color=#888888>✕ An:</color> <b>{bd.disabledCells.Length} o</b>    " +
            $"<color=#e08030>⬛ Chan:</color> <b>{bd.blockerCells.Length} o</b>",
            maskInfo);
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Xoa tat ca disabled", EditorStyles.miniButton))
        {
            Undo.RecordObject(targetLevel, "Clear Disabled");
            bd.disabledCells = new Vector2Int[0];
            targetLevel.boards[idx] = bd;
            EditorUtility.SetDirty(targetLevel);
        }
        if (GUILayout.Button("Xoa tat ca blocker", EditorStyles.miniButton))
        {
            Undo.RecordObject(targetLevel, "Clear Blockers");
            bd.blockerCells = new Vector2Int[0];
            targetLevel.boards[idx] = bd;
            EditorUtility.SetDirty(targetLevel);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.HelpBox(
            "Dung Paint toolbar tren canvas de click/keo ve o.\n" +
            "✕ An o: o se khong duoc render va khong the dat block.\n" +
            "⬛ Blocker: LevelSpawner se spawn Blocker prefab vao o do.",
            MessageType.None);

        GUILayout.Space(10);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"Blocks ({bd.blocks.Length})", EditorStyles.boldLabel);
        if (GUILayout.Button("+ Add Block", EditorStyles.miniButton, GUILayout.Width(80)))
        {
            Undo.RecordObject(targetLevel, "Add Block");
            var list = new List<BlockInBoardData>(bd.blocks);
            list.Add(new BlockInBoardData
            {
                color = BlockColor.Cyan,
                cellRow = 0,
                cellCol = 0,
                slideDirection = MoveDirection.Right,
                screw = new ScrewData { unlockDistance = 1f }
            });
            bd.blocks = list.ToArray();
            targetLevel.boards[idx] = bd;
            EditorUtility.SetDirty(targetLevel);
        }
        EditorGUILayout.EndHorizontal();

        for (int bi = 0; bi < bd.blocks.Length; bi++)
            DrawBlockInspector(idx, bi, ref bd);

        targetLevel.boards[idx] = bd;
    }

    void DrawBlockInspector(int boardIdx, int blockIdx, ref BoardData bd)
    {
        BlockInBoardData block = bd.blocks[blockIdx];

        EditorGUILayout.BeginVertical("helpbox");

        EditorGUILayout.BeginHorizontal();
        int colorIdx = (int)block.color;
        Color prevBg = GUI.backgroundColor;
        GUI.backgroundColor = colorIdx < BlockColors.Length ? BlockColors[colorIdx] : Color.white;
        EditorGUILayout.LabelField($"  Block {blockIdx}", EditorStyles.boldLabel, GUILayout.ExpandWidth(true));
        GUI.backgroundColor = prevBg;

        GUI.color = new Color(1f, 0.4f, 0.4f);
        if (GUILayout.Button("✕", EditorStyles.miniButton, GUILayout.Width(24)))
        {
            Undo.RecordObject(targetLevel, "Remove Block");
            var list = new List<BlockInBoardData>(bd.blocks);
            list.RemoveAt(blockIdx);
            bd.blocks = list.ToArray();
            EditorUtility.SetDirty(targetLevel);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            return;
        }
        GUI.color = Color.white;
        EditorGUILayout.EndHorizontal();

        EditorGUI.BeginChangeCheck();

        BlockColor newColor = (BlockColor)EditorGUILayout.EnumPopup("Color", block.color);
        MoveDirection newDir = (MoveDirection)EditorGUILayout.EnumPopup("Slide Direction", block.slideDirection);

        GUILayout.Space(4);
        EditorGUILayout.LabelField("Vi tri o (Cell Index)", EditorStyles.miniBoldLabel);
        EditorGUI.indentLevel++;
        int newRow = Mathf.Clamp(EditorGUILayout.IntField("  Hang (Row)", block.cellRow), 0, bd.cellRows - 1);
        int newCol = Mathf.Clamp(EditorGUILayout.IntField("  Cot (Col)", block.cellCol), 0, bd.cellCols - 1);
        EditorGUI.indentLevel--;

        GUILayout.Space(4);
        EditorGUILayout.LabelField("Screw", EditorStyles.miniBoldLabel);
        EditorGUI.indentLevel++;
        float newUnlockDist = EditorGUILayout.FloatField(
            new GUIContent("Unlock Distance",
                           "Khoang cach (world units) block phai keo theo Slide Direction de screw bung ra.\n" +
                           "Vi du: 1 = keo 1 o luoi."),
            block.screw.unlockDistance);
        newUnlockDist = Mathf.Max(0.1f, newUnlockDist);
        EditorGUI.indentLevel--;

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(targetLevel, "Edit Block");
            block.color = newColor;
            block.slideDirection = newDir;
            block.cellRow = newRow;
            block.cellCol = newCol;
            block.screw = new ScrewData { unlockDistance = newUnlockDist };
            bd.blocks[blockIdx] = block;
            EditorUtility.SetDirty(targetLevel);
        }

        var hintStyle = new GUIStyle(EditorStyles.miniLabel)
        { normal = { textColor = new Color(0.7f, 0.9f, 0.7f) } };
        EditorGUILayout.LabelField(
            $"→ O [{newRow},{newCol}] | Keo {newDir} >= {block.screw.unlockDistance}u de mo screw", hintStyle);

        EditorGUILayout.EndVertical();
    }

    void DrawAddBoardPanel()
    {
        EditorGUILayout.BeginVertical("helpbox");
        EditorGUILayout.LabelField("Them Board moi", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        newBoardRows = Mathf.Max(1, EditorGUILayout.IntField("So hang", newBoardRows, GUILayout.Width(180)));
        newBoardCols = Mathf.Max(1, EditorGUILayout.IntField("So cot", newBoardCols, GUILayout.Width(180)));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        newCellSize = Mathf.Max(0.1f, EditorGUILayout.FloatField("Cell Size", newCellSize, GUILayout.Width(180)));

        var sizeInfo = new GUIStyle(EditorStyles.miniLabel)
        { normal = { textColor = new Color(0.6f, 0.8f, 1f) } };
        GUILayout.Label($"→ {newBoardCols * newCellSize:F1} × {newBoardRows * newCellSize:F1} units", sizeInfo);
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(4);
        EditorGUILayout.BeginHorizontal();
        GUI.color = new Color(0.5f, 1f, 0.5f);
        if (GUILayout.Button($"  +  Add Board ({newBoardRows}×{newBoardCols})  ", GUILayout.Height(32)))
            AddNewBoard();
        GUI.color = Color.white;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.HelpBox(
            "Moi o vua du 1 block. Them block sau khi chon board ben phai.\n" +
            "Block duoc dat vao o bang Row + Col index (0-based).",
            MessageType.None);

        EditorGUILayout.EndVertical();
    }

    void AddNewBoard()
    {
        if (targetLevel == null) return;
        Undo.RecordObject(targetLevel, "Add Board");

        Vector2 pos = FindEmptyPosition();
        var list = new List<BoardData>(targetLevel.boards);
        list.Add(new BoardData
        {
            worldPosition = pos,
            cellRows = newBoardRows,
            cellCols = newBoardCols,
            cellSize = newCellSize,
            blocks = new BlockInBoardData[]
            {
                new BlockInBoardData
                {
                    color          = BlockColor.Cyan,
                    cellRow        = 0,
                    cellCol        = 0,
                    slideDirection = MoveDirection.Right,
                    screw          = new ScrewData { unlockDistance = 1f }
                }
            }
        });
        targetLevel.boards = list.ToArray();
        selectedBoardIndex = list.Count - 1;
        EditorUtility.SetDirty(targetLevel);
        Repaint();
    }

    Vector2 FindEmptyPosition()
    {
        float spacing = 5f;
        for (int i = 0; i < 20; i++)
        {
            float col = i % 3;
            float row = i / 3;
            Vector2 candidate = new Vector2(col * spacing - spacing, row * spacing - spacing);
            bool conflict = false;
            foreach (var b in targetLevel.boards)
            {
                if (Vector2.Distance(b.worldPosition, candidate) < spacing * 0.8f)
                { conflict = true; break; }
            }
            if (!conflict) return candidate;
        }
        return new Vector2(Random.Range(-6f, 6f), Random.Range(-6f, 6f));
    }

    void CreateNewLevel()
    {
        string path = EditorUtility.SaveFilePanelInProject(
            "Tao Level moi", "Level_01", "asset", "Chon noi luu Level Data");
        if (string.IsNullOrEmpty(path)) return;

        LevelData lv = ScriptableObject.CreateInstance<LevelData>();
        lv.levelIndex = 1;
        lv.timeLimit = 120f;
        lv.boards = new BoardData[0];

        AssetDatabase.CreateAsset(lv, path);
        AssetDatabase.SaveAssets();
        targetLevel = lv;
        selectedBoardIndex = -1;
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = lv;
    }

    Vector2 CanvasOrigin(Rect canvasRect) =>
        new Vector2(canvasRect.x + CANVAS_CX, canvasRect.y + CANVAS_CY);

    Rect BoardToCanvasRect(BoardData bd, Rect canvasRect)
    {
        Vector2 origin = CanvasOrigin(canvasRect);
        float pw = bd.cellCols * CELL_PX;
        float ph = bd.cellRows * CELL_PX;
        float px = origin.x + bd.worldPosition.x * WORLD_TO_PIX - pw * 0.5f;
        float py = origin.y - bd.worldPosition.y * WORLD_TO_PIX - ph * 0.5f;
        return new Rect(px, py, pw, ph);
    }

    static void DrawRectBorder(Rect r, Color c, float t)
    {
        EditorGUI.DrawRect(new Rect(r.x, r.y, r.width, t), c);
        EditorGUI.DrawRect(new Rect(r.x, r.yMax - t, r.width, t), c);
        EditorGUI.DrawRect(new Rect(r.x, r.y, t, r.height), c);
        EditorGUI.DrawRect(new Rect(r.xMax - t, r.y, t, r.height), c);
    }

    static void DrawLine(Vector2 from, Vector2 to, Color color, float thickness)
    {
        Handles.BeginGUI();
        Handles.color = color;
        Handles.DrawAAPolyLine(thickness, from, to);
        Handles.EndGUI();
    }

    static void DrawArrowHead(Vector2 tip, Vector2 dir, Color color, float size)
    {
        Vector2 right = new Vector2(-dir.y, dir.x);
        Vector2 a = tip - dir * size + right * size * 0.5f;
        Vector2 b = tip - dir * size - right * size * 0.5f;
        Handles.BeginGUI();
        Handles.color = color;
        Handles.DrawAAPolyLine(1.5f, a, tip, b);
        Handles.EndGUI();
    }
}
#endif