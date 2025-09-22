using UnityEngine;

/// <summary>
/// Tells which piece is on what square and keeps track. 
/// </summary>

public class BoardState
{   // En passant
    public Vector2Int? enPassantTarget = null;

    // castling flags. To makes sure that the pieces involved have not moved yet
    public bool whiteKingMoved = false, blackKingMoved = false;
    public bool whiteKingsideRookMoved = false,  whiteQueensideRookMoved = false;
    public bool blackKingsideRookMoved = false,  blackQueensideRookMoved = false;

    // null = empty 8x8 grid of chess holds the pieceview on the square.
    public PieceView[,] occ = new PieceView[8, 8];

    /// <summary> is (x,y) a actual coordinate on the chessboard? </summary>
    public static bool In(int x, int y) => x >= 0 && x < 8 && y >= 0 && y < 8;

    /// Overload: is the square on the board?
    public static bool In(Vector2Int s) => In(s.x, s.y);

    /// is the target square we want to move to empty?///
    public bool IsEmpty(int x, int y) => In(x, y) && occ[x, y] == null;

    ///Next we check if there is a friendly piece on the square///
    public bool HasFriendly(int x, int y, Side me) => In(x, y) && occ[x,y] != null && occ[x, y].side == me;

    /// is there an enemy piece on the square?///
    public bool HasEnemy(int x, int y, Side me) => In(x, y) && occ[x,y] != null && occ[x, y].side != me;

    ///return the piece if any is on the square but null if empty///
    public PieceView At(Vector2Int s) => In(s) ? occ[s.x, s.y] : null;

    
    ///  put piece p on square s in the logical grid not physical
    ///  used at startup to seed pieces that are already in the scene.
   

    public void Place(PieceView p, Vector2Int s)
    {
        occ[s.x, s.y] = p;  // write into grid
        p.square = s;       // keep the piece's own square in sync
    }

    /// <summary>
    /// Move a piece from 'from' to 'to'. Captures any enemy on 'to'.
    /// </summary>
    
    public void Move(Vector2Int from, Vector2Int to)
    {
        // Grab moving piece
        var moving = occ[from.x, from.y];

        // If there's a captured piece at 'to', destroy it (visual) and clear the cell (logical)
        var captured = occ[to.x, to.y];
        if (captured)
        {
            // This is allowed from a non-MonoBehaviour as long as we reference UnityEngine.Object
            Object.Destroy(captured.gameObject);
        }

        // Commit the move in the array
        occ[to.x, to.y] = moving;
        occ[from.x, from.y] = null;

        // Update the piece's own square coordinate
        moving.square = to;
    }
}
