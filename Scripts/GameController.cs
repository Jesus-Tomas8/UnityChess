using UnityEngine;
using System.Collections.Generic;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// Ties the BoardGrid, BoardState, PieceView, and MoveGenerator together.
/// Click to select, show legal moves; click a highlighted square to move.
public class GameController : MonoBehaviour
{
    // ----- References -----
    [Header("Board")]
    public BoardGrid grid;                 // assign the object with BoardGrid
    public Transform highlightPrefab;      // a flat Quad with transparent material
    public EndgameBanner banner;            // helps hook up banner to game controller

    [Header("Turn")]
    public Side sideToMove = Side.White;

    // ----- Internals -----
    BoardState state = new BoardState();            // logical board
    readonly List<Transform> highlightPool = new(); // pooled highlight quads
    PieceView selected;                             // currently selected piece
    List<Vector2Int> currentMoves = new();          // legal moves for selected

    // --- tune later if needed ---
    const float PieceLift = 0.035f;  // ~3.5cm above the board
    const float HighlightBump = 0.004f; // ~0.4mm above the board

    // Convert a board-center into a proper piece position by lifting along the board's surface normal
    // Convert a tile center to a correct piece position (lifts along the chosen normal)
    Vector3 LiftToPieceHeight(Vector3 center) => center + SurfaceUpAt(center) * PieceLift;

    Vector3 SurfaceUpAt(Vector3 center)
    {
        var t = grid.boardRenderer.transform;
        var n = t.forward.normalized;                         // geometric normal (may point down)
        var toCam = (Camera.main ? (Camera.main.transform.position - center) : Vector3.up);
        return (Vector3.Dot(n, toCam) > 0f) ? n : -n;         // always face the camera side
    }

    /// <summary>
    /// Banner Helper Return to later to create more detailed banner
    /// 
    /// </summary>
   
    void ShowBanner(string msg)
    {
        if (banner) banner.Show(msg);
        else Debug.Log(msg);
    }





    // ----- Auto-spawn start position -----
    [Header("Auto-Spawn Start Position")]
    [SerializeField] bool autoSpawnStartPosition = true;

    [Header("Prefabs - White")]
    public PieceView wPawn, wRook, wKnight, wBishop, wQueen, wKing;

    [Header("Prefabs - Black")]
    public PieceView bPawn, bRook, bKnight, bBishop, bQueen, bKing;

    // ---------- Input helpers ----------
    bool ClickThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        return Pointer.current != null && Pointer.current.press.wasPressedThisFrame;
#else
        return Input.GetMouseButtonDown(0);
#endif
    }

    Vector2 PointerPos()
    {
#if ENABLE_INPUT_SYSTEM
        return Pointer.current != null ? Pointer.current.position.ReadValue() : Vector2.zero;
#else
        return Input.mousePosition;
#endif
    }

    // ---------- Diagnostics / utilities ----------
#if UNITY_2023_1_OR_NEWER
    int CountPiecesInScene() => Object.FindObjectsByType<PieceView>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length;
#else
    int CountPiecesInScene() => Object.FindObjectsOfType<PieceView>().Length;
#endif

    bool PrefabsAssigned(out string missing)
    {
        var miss = new List<string>();
        if (!wPawn) miss.Add(nameof(wPawn));
        if (!wRook) miss.Add(nameof(wRook));
        if (!wKnight) miss.Add(nameof(wKnight));
        if (!wBishop) miss.Add(nameof(wBishop));
        if (!wQueen) miss.Add(nameof(wQueen));
        if (!wKing) miss.Add(nameof(wKing));
        if (!bPawn) miss.Add(nameof(bPawn));
        if (!bRook) miss.Add(nameof(bRook));
        if (!bKnight) miss.Add(nameof(bKnight));
        if (!bBishop) miss.Add(nameof(bBishop));
        if (!bQueen) miss.Add(nameof(bQueen));
        if (!bKing) miss.Add(nameof(bKing));
        missing = string.Join(", ", miss);
        return miss.Count == 0;
    }

#if UNITY_EDITOR
    [ContextMenu("Debug: Validate Prefab References")]
    void DebugValidate()
    {
        Debug.Log(PrefabsAssigned(out var m) ? "All 12 piece prefabs assigned." : "Missing: " + m);
        Debug.Log("Pieces in scene at Start: " + CountPiecesInScene());
    }

    [ContextMenu("Debug: Force Spawn Start Position")]
    void DebugForceSpawn()
    {
        if (!PrefabsAssigned(out var m)) { Debug.LogError("Missing: " + m); return; }
        SpawnStartPosition();
        Debug.Log("Forced spawn done.");
    }

    [ContextMenu("Debug: Despawn All Pieces (Play Mode)")]
    void DebugDespawnAll()
    {
#if UNITY_2023_1_OR_NEWER
        foreach (var p in Object.FindObjectsByType<PieceView>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            Destroy(p.gameObject);
#else
        foreach (var p in Object.FindObjectsOfType<PieceView>())
            Destroy(p.gameObject);
#endif
        Debug.Log("Despawned all pieces.");
    }
#endif

    // ---------- Unity lifecycle ----------
    void Start()
    {
        // Spawn start position ONLY if there are no pieces already in the scene
#if UNITY_2023_1_OR_NEWER
        var anyPiece = Object.FindFirstObjectByType<PieceView>(FindObjectsInactive.Exclude);
#else
        var anyPiece = Object.FindObjectOfType<PieceView>();
#endif
        if (anyPiece == null && autoSpawnStartPosition)
        {
            if (!PrefabsAssigned(out var missing))
            {
                Debug.LogError("Auto-spawn is ON but these prefabs are missing: " + missing);
            }
            else
            {
                SpawnStartPosition();
            }
        }

        // Register pieces into BoardState and snap visuals to board centers
#if UNITY_2023_1_OR_NEWER
        var pieces = Object.FindObjectsByType<PieceView>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
#else
        var pieces = Object.FindObjectsOfType<PieceView>();
#endif
        foreach (var p in pieces)
        {
            state.Place(p, p.square);
            

            var center = grid.SquareCenterWorld(p.square);
            var up = SurfaceUpAt(center);

            p.PlaceAt(center + up * PieceLift);
            p.transform.rotation = Quaternion.LookRotation(
                Vector3.ProjectOnPlane(grid.boardRenderer.transform.up, up).normalized,
                up
            );

        }

        HideHighlights();
    }

    void Update()
    {



        // Guards: clearer errors instead of NullReference exceptions
        if (grid == null) { Debug.LogError("GameController: 'grid' not assigned."); return; }
        if (grid.boardRenderer == null) { Debug.LogError("BoardGrid: 'boardRenderer' not assigned."); return; }
        if (grid.boardCollider == null) { Debug.LogError("BoardGrid: 'boardCollider' not assigned."); return; }

        var cam = Camera.main;
        if (cam == null) { Debug.LogError("No camera tagged 'MainCamera'."); return; }

        if (!ClickThisFrame()) return;

        ///Test 1; RaycastAll to see what we hit
        var ray = cam.ScreenPointToRay(PointerPos());

        var hits = Physics.RaycastAll(ray, 500f);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        for (int i = 0; i < hits.Length; i++)
        {
            var h = hits[i];
            Debug.Log($"[RaycastAll] #{i} {h.collider.name}  layer={LayerMask.LayerToName(h.collider.gameObject.layer)}  dist={h.distance:F2}");
        }

        if (!TryGetSquareFromBoardOrPiece(ray, out var sq))

        {
            
            Debug.Log("[GC] No board/piece under cursor.");
            return;
        }


        var piece = state.At(sq);

        // 1) If you clicked a friendly piece, (re)select it
        if (piece != null && piece.side == sideToMove)
        {
            Select(piece);
            return;
        }

        // 2) If you already have a selection, only then attempt a move/capture
        if (selected != null)
        {
            if (Contains(currentMoves, sq))
            {
                MoveSelectedTo(sq);
                return;
            }

            // Clicked empty or enemy not a legal capture -> clear selection
            Deselect();
            return;
        }

        // 3) No selection and clicked empty/enemy (wrong turn) -> ensure cleared
        Deselect();
    }

    // ---------- Gameplay ----------
    void Select(PieceView p)
    {
        selected = p;
        currentMoves = MoveGenerator.LegalMoves(state, p); // fixed spelling
        ShowHighlights(currentMoves);
    }

    void Deselect()
    {
        selected = null;
        currentMoves.Clear();
        HideHighlights();
    }

    void MoveSelectedTo(Vector2Int to)
    {
        var from = selected.square;

        // ---------- EN PASSANT capture ----------
        if (selected.type == PieceType.Pawn && state.enPassantTarget.HasValue && to == state.enPassantTarget.Value)
        {
            int dir = (selected.side == Side.White) ? 1 : -1;
            var capSq = new Vector2Int(to.x, to.y - dir);      // pawn behind the target square
            var cap = state.At(capSq);
            if (cap != null)
            {
                Destroy(cap.gameObject);
                state.occ[capSq.x, capSq.y] = null;
            }
        }

        // ---------- CASTLING: move the rook ----------
        if (selected.type == PieceType.King && Mathf.Abs(to.x - from.x) == 2 && to.y == from.y)
        {
            int rank = from.y;
            if (to.x == 6) // king side
            {
                var rookFrom = new Vector2Int(7, rank);
                var rookTo = new Vector2Int(5, rank);
                var rook = state.At(rookFrom);
                if (rook != null)
                {
                    state.occ[rookFrom.x, rookFrom.y] = null;
                    state.occ[rookTo.x, rookTo.y] = rook;
                    rook.square = rookTo;
                    rook.PlaceAt(grid.SquareCenterWorld(rookTo));
                }
            }
            else if (to.x == 2) // queen side
            {
                var rookFrom = new Vector2Int(0, rank);
                var rookTo = new Vector2Int(3, rank);
                var rook = state.At(rookFrom);
                if (rook != null)
                {
                    state.occ[rookFrom.x, rookFrom.y] = null;
                    state.occ[rookTo.x, rookTo.y] = rook;
                    rook.square = rookTo;
                    rook.PlaceAt(grid.SquareCenterWorld(rookTo));
                }
            }
        }

        // ---------- Do the move (handles normal captures on 'to') ----------
        state.Move(from, to);
        selected.PlaceAt(grid.SquareCenterWorld(to));

        // ---------- Update en passant target for the NEXT move ----------
        // only set if this move was a 2-square pawn push; otherwise clear
        if (selected.type == PieceType.Pawn && Mathf.Abs(to.y - from.y) == 2)
        {
            int midY = (to.y + from.y) / 2;
            state.enPassantTarget = new Vector2Int(from.x, midY);
        }
        else
        {
            state.enPassantTarget = null;
        }

        // ---------- Update castling rights (has-moved flags) ----------
        if (selected.type == PieceType.King)
        {
            if (selected.side == Side.White) state.whiteKingMoved = true;
            else state.blackKingMoved = true;
        }
        else if (selected.type == PieceType.Rook)
        {
            // if a rook moved off its original corner, mark that side's rook as 'moved'
            if (selected.side == Side.White)
            {
                if (from == new Vector2Int(0, 0)) state.whiteQueensideRookMoved = true;
                if (from == new Vector2Int(7, 0)) state.whiteKingsideRookMoved = true;
            }
            else
            {
                if (from == new Vector2Int(0, 7)) state.blackQueensideRookMoved = true;
                if (from == new Vector2Int(7, 7)) state.blackKingsideRookMoved = true;
            }
        }

        // ---------- Finish turn ----------
        Deselect();
        sideToMove = (sideToMove == Side.White) ? Side.Black : Side.White;
        CheckForGameEnd();
    }



    // ---------- Highlights ----------
    void HideHighlights()
    {
        for (int i = 0; i < highlightPool.Count; i++)
            if (highlightPool[i]) highlightPool[i].gameObject.SetActive(false);
    }


    ///  Draws highlights quads on the given list of squares which is based on the legal moves of the selected piece 

    void ShowHighlights(List<Vector2Int> squares)
    {
        EnsurePoolSize(squares.Count);

        var boardT = grid.boardRenderer.transform;
        var cell = grid.CellSizeWorld();

        for (int i = 0; i < highlightPool.Count; i++)
        {
            var h = highlightPool[i];

            if (i < squares.Count)
            {
                var center = grid.SquareCenterWorld(squares[i]);
                var up = SurfaceUpAt(center);          // same normal logic as pieces
                var inPlan = boardT.up;

                // QUAD: local +Z faces 'up', local +Y lies along board.up
                h.rotation = Quaternion.LookRotation(up, inPlan);
                h.position = center + up * HighlightBump;          // above the board texture
                h.localScale = new Vector3(cell.x, cell.y, 1f);      // QUAD is XY plane (scale X & Y)

                h.gameObject.SetActive(true);
            }
            else
            {
                h.gameObject.SetActive(false);
            }
        }
    }




    void EnsurePoolSize(int n)
    {
        if (!highlightPrefab)
        {
            if (n > 0) Debug.LogWarning("No highlightPrefab assigned; moves will not be highlighted.");
            return;
        }
        while (highlightPool.Count < n)
        {
            var t = Instantiate(highlightPrefab);
            highlightPool.Add(t);
        }
    }

    static bool Contains(List<Vector2Int> list, Vector2Int target)
    {
        for (int i = 0; i < list.Count; i++)
            if (list[i] == target) return true;
        return false;
    }

    // ---------- Spawning ----------
    void SpawnStartPosition()
    {
        // White back rank (y = 0)
        Spawn(wRook, Side.White, PieceType.Rook, 0, 0);
        Spawn(wKnight, Side.White, PieceType.Knight, 1, 0);
        Spawn(wBishop, Side.White, PieceType.Bishop, 2, 0);
        Spawn(wQueen, Side.White, PieceType.Queen, 3, 0);
        Spawn(wKing, Side.White, PieceType.King, 4, 0);
        Spawn(wBishop, Side.White, PieceType.Bishop, 5, 0);
        Spawn(wKnight, Side.White, PieceType.Knight, 6, 0);
        Spawn(wRook, Side.White, PieceType.Rook, 7, 0);

        // White pawns (y = 1)
        for (int x = 0; x < 8; x++) Spawn(wPawn, Side.White, PieceType.Pawn, x, 1);

        // Black back rank (y = 7)
        Spawn(bRook, Side.Black, PieceType.Rook, 0, 7);
        Spawn(bKnight, Side.Black, PieceType.Knight, 1, 7);
        Spawn(bBishop, Side.Black, PieceType.Bishop, 2, 7);
        Spawn(bQueen, Side.Black, PieceType.Queen, 3, 7);
        Spawn(bKing, Side.Black, PieceType.King, 4, 7);
        Spawn(bBishop, Side.Black, PieceType.Bishop, 5, 7);
        Spawn(bKnight, Side.Black, PieceType.Knight, 6, 7);
        Spawn(bRook, Side.Black, PieceType.Rook, 7, 7);

        // Black pawns (y = 6)
        for (int x = 0; x < 8; x++) Spawn(bPawn, Side.Black, PieceType.Pawn, x, 6);
    }

    // Single, hardened Spawn (no duplicate)
    void Spawn(PieceView prefab, Side side, PieceType type, int x, int y)
    {
        if (!prefab)
        {
            Debug.LogError($"Spawn called with NULL prefab for {side} {type} at {x},{y}. Assign all prefabs on GameController.");
            return;
        }
        var view = Instantiate(prefab);
        view.side = side;
        view.type = type;
        view.square = new Vector2Int(x, y);

        

        var center = grid.SquareCenterWorld(view.square);
        var up = SurfaceUpAt(center);

        view.PlaceAt(center + up * PieceLift);
        view.transform.rotation = Quaternion.LookRotation(
            Vector3.ProjectOnPlane(grid.boardRenderer.transform.up, up).normalized,
            up
        );

    }

    bool TryGetSquareFromBoardOrPiece(Ray ray, out Vector2Int sq)
    {
        // Prefer the board hit
        if (grid.RayToSquare(ray, out sq)) return true;

        // Fallback: if we hit a piece collider, use its known square
        if (Physics.Raycast(ray, out var hit, 500f))
        {
            var pv = hit.collider.GetComponentInParent<PieceView>();
            if (pv != null) { sq = pv.square; return true; }
        }
        sq = default;
        return false;
    }

    void CheckForGameEnd()
    {
        var opp = (sideToMove == Side.White) ? Side.Black : Side.White; // the side that just moved
        var next = sideToMove; // side to play now

        bool inCheck = MoveGenerator.IsInCheck(state, next);

        // Does 'next' have any legal move?
        bool any = false;
        for (int y = 0; y < 8 && !any; y++)
            for (int x = 0; x < 8 && !any; x++)
            {
                var pv = state.occ[x, y];
                if (pv == null || pv.side != next) continue;
                if (MoveGenerator.LegalMoves(state, pv).Count > 0) any = true;
            }

        if (!any)
        {
            if (inCheck) ShowBanner($"{(next == Side.White ? "White" : "Black")} is checkmated — {(opp == Side.White ? "White" : "Black")} wins!");
            else ShowBanner("Stalemate — Draw");
        }
        else if (inCheck)
        {
            // optional: show small "Check!" somewhere
            // Debug.Log($"{next} to move is in check");
        }
    }



}
