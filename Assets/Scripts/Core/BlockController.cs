using UnityEngine;
using DG.Tweening;

/// <summary>
/// Dieu khien hanh vi cua 1 block:
///   - Khi con screw: chi truot theo truc slideDir, snap theo luoi, khong bi lech ngang.
///     Keo > 0.5 * cellSize sang o ke → snap sang o do luon.
///   - Sau khi screw bung (isFree): di chuyen tu do 2D (XZ), van cham tuong va block khac.
///   - Juice: pick-up squeeze, unlock flash + punch scale.
/// </summary>
public class BlockController : MonoBehaviour
{
    // ── Public state ───────────────────────────────────────────────────────
    public BoardController parentBoard { get; private set; }
    public bool isFree { get; private set; } = false;

    /// <summary>Huong keo de mo khoa screw (chi co hieu luc khi !isFree)</summary>
    public MoveDirection slideDir { get; private set; }

    // ── Private ────────────────────────────────────────────────────────────
    ScrewController screw;

    /// <summary>Vi tri world cua block khi bat dau (= vi tri screw)</summary>
    Vector3 originWorldPos;

    /// <summary>Khoang cach can keo theo slideDir de screw bat ra</summary>
    float unlockDistance;

    /// <summary>Kich thuoc 1 o luoi (de snap)</summary>
    float cellSize;

    // Snap state — vi tri toa do doc theo slideAxis hien tai (co the la nhieu o)
    float slideAccum;      // tich luy dich chuyen doc theo slide axis (world units)
    int   currentCell;     // o hien tai tren truc slideDir (bat dau = 0)
    Vector3 lockedPerp;    // vi tri tren truc VUONG GOC (giu co dinh khi !isFree)

    // Grid state cho free drag (sau khi isFree) — snap theo luoi XZ
    Vector3 freeGridOrigin;  // goc luoi = vi tri snap khi vua duoc giai phong
    float freeAccumX;
    float freeAccumZ;
    int freeCellX;
    int freeCellZ;
    // Vi tri world da duoc xac nhan (sau khi pass collision) de smooth follow
    Vector3 freeConfirmedPos;

    // Renderer de flash khi mo screw
    Renderer[] renderers;

    // ── Init ───────────────────────────────────────────────────────────────
    public void Init(BoardController board, MoveDirection dir,
                     float unlockDist, ScrewController sc, float cs = 1f)
    {
        parentBoard   = board;
        slideDir      = dir;
        unlockDistance = unlockDist;
        screw         = sc;
        cellSize      = cs;

        originWorldPos = transform.position;
        lockedPerp     = transform.position; // luu toan bo, se lay phan vuong goc

        slideAccum  = 0f;
        currentCell = 0;

        renderers = GetComponentsInChildren<Renderer>(includeInactive: true);
    }

    // ── Juice: pick-up ─────────────────────────────────────────────────────
    public void OnPickUp()
    {
        transform.DOKill(complete: false);
        // Bop nhe khi nhat len, roi phong lai – cam giac "chop"
        transform.DOPunchScale(new Vector3(-0.06f, 0.18f, -0.06f), 0.25f, 5, 0.4f);
    }

    // ── Drag (goi tu InputManager moi frame khi dang keo) ─────────────────
    public void DragTo(Vector3 worldDelta)
    {
        if (!isFree)
            DragLocked(worldDelta);
        else
            DragFree(worldDelta);
    }

    // ── Locked drag (con screw) ────────────────────────────────────────────
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

    // Helper tao vi tri snap (lock truc vuong goc)
    Vector3 SnapPos(Vector3 axis, float slideOffset)
    {
        Vector3 pos = originWorldPos + axis * slideOffset;
        if (axis.x != 0) pos.z = lockedPerp.z;
        if (axis.z != 0) pos.x = lockedPerp.x;
        pos.y = originWorldPos.y;
        return pos;
    }

    // ── Free drag (sau khi mo screw) — mượt, slide assistance ────────────
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

    // ── Release ────────────────────────────────────────────────────────────
    public void OnRelease()
    {
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

    // ── Unlock: goi khi screw vua bung ────────────────────────────────────
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

    // ── Unlock check ───────────────────────────────────────────────────────
    void CheckUnlock()
    {
        if (screw == null || !screw.IsLocked) return;

        Vector3 displacement = transform.position - originWorldPos;
        float   signedDist   = Vector3.Dot(displacement, SlideAxisVec());

        if (signedDist >= unlockDistance)
        {
            screw.Unlock();
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
            PlayUnlockJuice();
            parentBoard.OnBlockFreed(this);
        }
    }

    // ── Helpers ────────────────────────────────────────────────────────────
    /// <summary>Vector don vi the hien huong slideDir trong world space</summary>
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

    /// <summary>Vector vuong goc voi slideDir (trong mat phang XZ)</summary>
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
        Collider col = GetComponent<Collider>();
        if (col != null) return col.bounds;
        return new Bounds(transform.position, new Vector3(0.9f, 0.4f, 0.9f));
    }
}