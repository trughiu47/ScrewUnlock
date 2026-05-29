using UnityEngine;
using DG.Tweening;

public class BlockController : MonoBehaviour
{
    public BoardController parentBoard { get; private set; }
    public bool isFree { get; private set; } = false;

    public MoveDirection slideDir { get; private set; }

    public BlockColor blockColor { get; private set; }
    public bool isSandBlock => blockColor == BlockColor.Sand;

    ScrewController screw;

    Vector3 originWorldPos;

    float unlockDistance;

    float cellSize;
    float slideAccum;      // tich luy dich chuyen doc theo slide axis (world units)
    int   currentCell;     // o hien tai tren truc slideDir (bat dau = 0)
    Vector3 lockedPerp;    // vi tri tren truc VUONG GOC (giu co dinh khi !isFree)

    Vector3 freeGridOrigin;  // goc luoi = vi tri snap khi vua duoc giai phong
    float freeAccumX;
    float freeAccumZ;
    int freeCellX;
    int freeCellZ;
    Vector3 freeConfirmedPos;

    Renderer[] renderers;
    Bounds localBounds;

    public void Init(BoardController board, MoveDirection dir,
                     float unlockDist, ScrewController sc, BlockColor color, float cs = 1f)
    {
        parentBoard   = board;
        slideDir      = dir;
        unlockDistance = unlockDist;
        screw         = sc;
        blockColor    = color;
        cellSize      = cs;

        originWorldPos = transform.position;
        lockedPerp     = transform.position; // luu toan bo, se lay phan vuong goc

        slideAccum  = 0f;
        currentCell = 0;

        renderers = GetComponentsInChildren<Renderer>(includeInactive: true);
        CacheLocalBounds();
    }

    public void OnPickUp()
    {
        transform.DOKill(complete: false);
        transform.DOPunchScale(new Vector3(-0.06f, 0.18f, -0.06f), 0.25f, 5, 0.4f);

        // SFX: block được chạm vào
        SfxManager.Instance?.PlayBlockPickup();
    }

    public void DragTo(Vector3 worldDelta)
    {
        if (!isFree)
            DragLocked(worldDelta);
        else
            DragFree(worldDelta);
    }

    void DragLocked(Vector3 worldDelta)
    {
        Vector3 axis = SlideAxisVec();

        // Chi lay thanh phan doc theo slide axis
        float delta1D = Vector3.Dot(worldDelta, axis);

        // Tinh luot xoay cua screw tai cho de tao cam giac dang van oc
        screw?.OnDragUpdate(delta1D);

        // Tich luy di chuyen thuc te (world units), gioi han chat tu 0 den unlockDistance
        float proposedAccum = Mathf.Clamp(slideAccum + delta1D, 0f, unlockDistance);

        // Vi tri de xuat dua tren di chuyen thuc te (muot ma 100%)
        Vector3 proposedPos = SnapPos(axis, proposedAccum);

        // Kiem tra va cham o vi tri de xuat
        if (parentBoard.CanBlockMoveTo(this, proposedPos))
        {
            slideAccum = proposedAccum;

            // Xoa sach tat ca tween cu dang chay de theo sat tay nguoi dung truc tiep
            transform.DOKill(complete: false);
            transform.position = proposedPos;

            // Cap nhat cell hien tai cho logic snap neu tha tay khi chua mo khoa
            currentCell = Mathf.RoundToInt(slideAccum / cellSize);
        }

        CheckUnlock();
    }

    Vector3 SnapPos(Vector3 axis, float slideOffset)
    {
        Vector3 pos = originWorldPos + axis * slideOffset;
        if (axis.x != 0) pos.z = lockedPerp.z;
        if (axis.z != 0) pos.x = lockedPerp.x;
        pos.y = originWorldPos.y;
        return pos;
    }

    void DragFree(Vector3 worldDelta)
    {
        // Xoa tat ca tween de phan hoi truc tiep
        transform.DOKill(complete: false);

        // Tinh toan vi tri tu do analogue theo tat ca cac huong XZ
        Vector3 proposedPos = transform.position + new Vector3(worldDelta.x, 0f, worldDelta.z);

        if (parentBoard.CanBlockMoveTo(this, proposedPos))
        {
            transform.position = proposedPos;
        }
        else
        {
            // SLIDE ASSISTANCE: Truot muot doc canh neu bi chan cheo
            Vector3 proposedX = new Vector3(proposedPos.x, transform.position.y, transform.position.z);
            Vector3 proposedZ = new Vector3(transform.position.x, transform.position.y, proposedPos.z);

            if (parentBoard.CanBlockMoveTo(this, proposedX))
            {
                transform.position = proposedX;
            }
            else if (parentBoard.CanBlockMoveTo(this, proposedZ))
            {
                transform.position = proposedZ;
            }
        }

        // Tinh toan nguoc lai accum va cell de phuc vu cho viec snap dan hoi o OnRelease
        Vector3 diff = transform.position - freeGridOrigin;
        freeAccumX = diff.x;
        freeAccumZ = diff.z;
        freeCellX  = Mathf.RoundToInt(freeAccumX / cellSize);
        freeCellZ  = Mathf.RoundToInt(freeAccumZ / cellSize);
    }

    // Sweep o theo 1 truc, tra ve cell cuoi cung hop le
    int SweepCells(int fromCell, int desired, int otherCell, bool sweepX, ref float accum)
    {
        int step = desired > fromCell ? 1 : (desired < fromCell ? -1 : 0);
        if (step == 0) return fromCell;

        int current = fromCell;
        for (int c = fromCell + step;
             step > 0 ? c <= desired : c >= desired;
             c += step)
        {
            Vector3 testPos = sweepX
                ? FreeCellToWorld(c, otherCell)
                : FreeCellToWorld(otherCell, c);

            if (parentBoard.CanBlockMoveTo(this, testPos))
                current = c;
            else
            {
                accum = current * cellSize; // reset accum de tranh "phantom" tich luy
                break;
            }
        }
        return current;
    }

    Vector3 FreeCellToWorld(int cx, int cz) =>
        new Vector3(freeGridOrigin.x + cx * cellSize,
                    freeGridOrigin.y,
                    freeGridOrigin.z + cz * cellSize);

    public void OnRelease()
    {
        // SFX: thả tay ra — block snap về
        SfxManager.Instance?.PlayBlockRelease();

        if (!isFree)
        {
            // Bao screw dung xoay
            screw?.OnDragRelease();

            // Snap ve tam o hien tai khi nha tay
            float   snapOffset = currentCell * cellSize;
            Vector3 axis       = SlideAxisVec();
            Vector3 snapPos    = SnapPos(axis, snapOffset);

            transform.DOKill(complete: false);
            transform.DOMove(snapPos, 0.12f).SetEase(Ease.OutBack);
        }
        else
        {
            // Tu do: snap chinh xac ve tam o hien tai + bounce nhe
            Vector3 cellPos = FreeCellToWorld(freeCellX, freeCellZ);
            transform.DOKill(complete: false);
            transform.DOMove(cellPos, 0.12f).SetEase(Ease.OutBack)
                .OnComplete(() =>
                    transform.DOPunchScale(new Vector3(0.05f, -0.05f, 0.05f), 0.18f, 4, 0.3f));
        }
    }

    void PlayUnlockJuice()
    {
        transform.DOKill(complete: false);

        // 1. Punch scale manh – cam giac "pop!"
        transform.DOPunchScale(new Vector3(0.2f, -0.15f, 0.2f), 0.35f, 6, 0.5f);

        // 2. Flash emissive tren tat ca renderer
        foreach (var r in renderers)
        {
            foreach (var mat in r.materials)
            {
                if (mat.HasProperty("_EmissionColor"))
                {
                    Color baseEmit = Color.white * 2.5f;
                    mat.EnableKeyword("_EMISSION");
                    DOTween.To(
                        () => mat.GetColor("_EmissionColor"),
                        c => mat.SetColor("_EmissionColor", c),
                        baseEmit, 0.06f
                    ).SetEase(Ease.OutQuad).OnComplete(() =>
                        DOTween.To(
                            () => mat.GetColor("_EmissionColor"),
                            c => mat.SetColor("_EmissionColor", c),
                            Color.black, 0.30f
                        ).SetEase(Ease.InQuad)
                    );
                }
                else
                {
                    var mpb = new MaterialPropertyBlock();
                    r.GetPropertyBlock(mpb);
                    Color orig = mpb.GetColor("_BaseColor");
                    DOTween.To(() => 0f, t =>
                    {
                        var mpb2 = new MaterialPropertyBlock();
                        r.GetPropertyBlock(mpb2);
                        mpb2.SetColor("_BaseColor", Color.Lerp(orig, Color.white, Mathf.Sin(t * Mathf.PI)));
                        r.SetPropertyBlock(mpb2);
                    }, 1f, 0.35f).SetEase(Ease.OutQuad);
                }
            }
        }

        // 3. Nhay len nhe roi ve (bounce Y)
        float origY = transform.position.y;
        transform.DOMoveY(origY + 0.12f, 0.15f).SetEase(Ease.OutQuad).OnComplete(() =>
            transform.DOMoveY(origY, 0.18f).SetEase(Ease.InBack)
        );
    }

    void CheckUnlock()
    {
        if (screw == null || !screw.IsLocked) return;

        Vector3 displacement = transform.position - originWorldPos;
        float   signedDist   = Vector3.Dot(displacement, SlideAxisVec());

        if (signedDist >= unlockDistance)
        {
            screw.Unlock();

            if (isSandBlock)
            {
                TriggerSandCrumbleAndDestroy();
            }
            else
            {
                isFree = true;

                // Snap block ve tam o hien tai truoc khi chuyen sang free grid
                Vector3 axis    = SlideAxisVec();
                Vector3 snapPos = SnapPos(axis, currentCell * cellSize);
                transform.DOKill(complete: true);
                transform.position = snapPos;

                // Khoi tao goc luoi cho free drag
                freeGridOrigin   = snapPos;
                freeConfirmedPos = snapPos;
                freeAccumX = 0f;
                freeAccumZ = 0f;
                freeCellX  = 0;
                freeCellZ  = 0;

                Debug.Log($"[Block] {gameObject.name} is FREE! (keo {signedDist:F2}u)");
                SwapToSmoothVisual();
                PlayUnlockJuice();
                parentBoard.OnBlockFreed(this);
            }
        }
    }

    void SwapToSmoothVisual()
    {
        if (LevelSpawner.Instance == null) return;

        bool is2x = unlockDistance >= 2f;
        GameObject smoothPrefab = is2x 
            ? LevelSpawner.Instance.smoothBlockPrefab2x 
            : LevelSpawner.Instance.smoothBlockPrefab;

        if (smoothPrefab == null) return;

        // Xóa tất cả các con hiện tại (khối có rãnh cũ)
        int childCount = transform.childCount;
        for (int i = childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            child.gameObject.SetActive(false); // Vô hiệu hóa để không bị quét tìm collider khi đang chờ xóa
            Destroy(child.gameObject);
        }

        // Vô hiệu hóa MeshRenderer & MeshFilter trên chính root nếu có
        if (TryGetComponent<MeshRenderer>(out var rootMR))
        {
            rootMR.enabled = false;
        }
        if (TryGetComponent<MeshFilter>(out var rootMF))
        {
            Destroy(rootMF);
        }

        // Tạo khối nhẵn mới làm con
        GameObject smoothVisual = Instantiate(smoothPrefab, transform);
        smoothVisual.transform.localPosition = Vector3.zero;
        smoothVisual.transform.localRotation = Quaternion.identity;
        smoothVisual.transform.localScale = Vector3.one;

        // Cập nhật lại màu sắc cho khối mới
        LevelSpawner.Instance.ApplyColor(smoothVisual, blockColor);

        // Cập nhật lại mảng renderers để các hiệu ứng DOTween (như PlayUnlockJuice) chạy chính xác trên khối mới
        renderers = GetComponentsInChildren<Renderer>(includeInactive: true);
        CacheLocalBounds();
    }

    void TriggerSandCrumbleAndDestroy()
    {
        // Disable colliders to prevent any further interaction or movement blocking
        var colliders = GetComponentsInChildren<Collider>(includeInactive: true);
        foreach (var col in colliders)
        {
            col.enabled = false;
        }

        // Remove from board's active tracking so it doesn't block other blocks
        parentBoard.RemoveBlock(this);

        // Kill any ongoing tweens
        transform.DOKill(complete: false);

        // Dynamic 2D Sand Pieces effect
        if (LevelSpawner.Instance != null && LevelSpawner.Instance.sandPieceSprites != null && LevelSpawner.Instance.sandPieceSprites.Length > 0)
        {
            Bounds blockBounds = GetBounds();
            Vector3 center = blockBounds.center;
            Vector3 extents = blockBounds.extents;
            int pieceCount = Random.Range(12, 18);

            for (int i = 0; i < pieceCount; i++)
            {
                Sprite sprite = LevelSpawner.Instance.sandPieceSprites[Random.Range(0, LevelSpawner.Instance.sandPieceSprites.Length)];
                if (sprite == null) continue;

                GameObject fragment = new GameObject($"SandPiece_{i}");
                fragment.transform.position = center + new Vector3(
                    Random.Range(-extents.x * 0.8f, extents.x * 0.8f),
                    Random.Range(-extents.y * 0.5f, extents.y * 0.5f),
                    Random.Range(-extents.z * 0.8f, extents.z * 0.8f)
                );

                // Setup SpriteRenderer
                SpriteRenderer sr = fragment.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.sortingOrder = 15; // Render on top of blocks

                // Scale randomly
                fragment.transform.localScale = Vector3.one * Random.Range(0.2f, 0.45f);

                // Billboard to face main camera
                if (Camera.main != null)
                {
                    fragment.transform.rotation = Camera.main.transform.rotation;
                }
                else
                {
                    fragment.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                }

                // Scatter physical movement using DOTween
                float duration = Random.Range(0.7f, 1.1f);
                Vector3 scatterDir = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)).normalized;
                float scatterDist = Random.Range(0.4f, 1.2f);
                Vector3 targetHorizPos = fragment.transform.position + scatterDir * scatterDist;

                // 1. Horizontal spread
                fragment.transform.DOMoveX(targetHorizPos.x, duration).SetEase(Ease.OutQuad);
                fragment.transform.DOMoveZ(targetHorizPos.z, duration).SetEase(Ease.OutQuad);

                // 2. Parabolic arc height jump and gravity fall
                float peakHeight = center.y + Random.Range(0.3f, 0.6f);
                float jumpDuration = duration * 0.35f;
                float fallDuration = duration - jumpDuration;

                Sequence ySeq = DOTween.Sequence();
                ySeq.Append(fragment.transform.DOMoveY(peakHeight, jumpDuration).SetEase(Ease.OutQuad));
                ySeq.Append(fragment.transform.DOMoveY(center.y - 1.8f, fallDuration).SetEase(Ease.InQuad));

                // 3. Random tumble/rotation
                fragment.transform.DORotate(new Vector3(0f, 0f, Random.Range(-360f, 360f)), duration, RotateMode.FastBeyond360).SetEase(Ease.OutQuad);

                // 4. Fade & Shrink out
                sr.DOFade(0f, duration).SetEase(Ease.InQuad);
                fragment.transform.DOScale(Vector3.zero, duration).SetEase(Ease.InQuad).OnComplete(() =>
                {
                    Destroy(fragment);
                });
            }
        }
        else if (LevelSpawner.Instance != null && LevelSpawner.Instance.sandBreakEffectPrefab != null)
        {
            // Fallback to prefab
            GameObject effect = Instantiate(LevelSpawner.Instance.sandBreakEffectPrefab, transform.position, Quaternion.identity);
            Destroy(effect, 2.5f);
        }

        // Play crumble/shake & dissolve DOTween sequence on the block base itself
        Sequence seq = DOTween.Sequence();
        seq.Append(transform.DOShakePosition(0.25f, 0.05f, 15, 90, false, false));
        seq.Join(transform.DOScale(Vector3.zero, 0.35f).SetEase(Ease.InBack));
        seq.OnComplete(() =>
        {
            Destroy(gameObject);
        });

        // Notify board that block is freed to check victory condition
        parentBoard.OnBlockFreed(this);
    }

    Vector3 SlideAxisVec()
    {
        return slideDir switch
        {
            MoveDirection.Right   => Vector3.right,
            MoveDirection.Left    => Vector3.left,
            MoveDirection.Forward => Vector3.forward,
            MoveDirection.Back    => Vector3.back,
            _                     => Vector3.right
        };
    }

    Vector3 PerpAxisVec()
    {
        return slideDir switch
        {
            MoveDirection.Right   => Vector3.forward,
            MoveDirection.Left    => Vector3.forward,
            MoveDirection.Forward => Vector3.right,
            MoveDirection.Back    => Vector3.right,
            _                     => Vector3.forward
        };
    }

    public Bounds GetBounds()
    {
        // Lấy tâm và bán kính (extents) local
        Vector3 center = localBounds.center;
        Vector3 extents = localBounds.extents;

        // Chuyển đổi tâm local sang thế giới (bao gồm dịch chuyển, xoay và tỷ lệ!)
        Vector3 worldCenter = transform.TransformPoint(center);

        // Chuyển đổi các trục hướng local (nhân với bán kính tương ứng) sang thế giới
        Vector3 worldExtentsX = transform.TransformVector(new Vector3(extents.x, 0f, 0f));
        Vector3 worldExtentsY = transform.TransformVector(new Vector3(0f, extents.y, 0f));
        Vector3 worldExtentsZ = transform.TransformVector(new Vector3(0f, 0f, extents.z));

        // Kích thước thế giới mới (AABB) là tổng trị tuyệt đối của các thành phần hình chiếu
        float extX = Mathf.Abs(worldExtentsX.x) + Mathf.Abs(worldExtentsY.x) + Mathf.Abs(worldExtentsZ.x);
        float extY = Mathf.Abs(worldExtentsX.y) + Mathf.Abs(worldExtentsY.y) + Mathf.Abs(worldExtentsZ.y);
        float extZ = Mathf.Abs(worldExtentsX.z) + Mathf.Abs(worldExtentsY.z) + Mathf.Abs(worldExtentsZ.z);

        return new Bounds(worldCenter, new Vector3(extX * 2f, extY * 2f, extZ * 2f));
    }

    void CacheLocalBounds()
    {
        // includeInactive = false để loại bỏ hoàn toàn các collider cũ đã bị tắt SetActive(false) trong SwapToSmoothVisual
        Collider[] colliders = GetComponentsInChildren<Collider>(includeInactive: false);
        if (colliders == null || colliders.Length == 0)
        {
            localBounds = new Bounds(Vector3.zero, new Vector3(0.9f, 0.4f, 0.9f));
            return;
        }

        Bounds bounds = new Bounds();
        bool hasBounds = false;

        foreach (var col in colliders)
        {
            if (col == null) continue;

            Bounds colLocalBounds;

            if (col is BoxCollider box)
            {
                // BoxCollider có center và size rõ ràng trong không gian local của chính GameObject của nó
                Vector3 localCenter = box.center;
                Vector3 localSize = box.size;

                // Chuyển sang không gian local của block
                Vector3 blockLocalCenter = transform.InverseTransformPoint(col.transform.TransformPoint(localCenter));
                Vector3 blockLocalSize = transform.InverseTransformVector(col.transform.TransformVector(localSize));

                // Kích thước luôn dương
                blockLocalSize = new Vector3(Mathf.Abs(blockLocalSize.x), Mathf.Abs(blockLocalSize.y), Mathf.Abs(blockLocalSize.z));
                colLocalBounds = new Bounds(blockLocalCenter, blockLocalSize);
            }
            else
            {
                // Fallback cho các loại collider khác: sử dụng world bounds được chuyển về local
                Vector3 localCenter = transform.InverseTransformPoint(col.bounds.center);
                Vector3 localSize = transform.InverseTransformVector(col.bounds.size);
                localSize = new Vector3(Mathf.Abs(localSize.x), Mathf.Abs(localSize.y), Mathf.Abs(localSize.z));
                colLocalBounds = new Bounds(localCenter, localSize);
            }

            if (!hasBounds)
            {
                bounds = colLocalBounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(colLocalBounds);
            }
        }

        localBounds = hasBounds ? bounds : new Bounds(Vector3.zero, new Vector3(0.9f, 0.4f, 0.9f));
    }
}