using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// this script will return pseudo legal moves
/// it will have the movement of pieces and capturing by does not yet include king in check state
/// promotion, castling and en passant will be added later
/// add AI to play against computer
/// </summary>

public static class MoveGenerator
{

    static Side Opposite(Side s) => (s == Side.White) ? Side.Black : Side.White;
    static int SideIndex(Side s) => (s == Side.White) ? 0 : 1;   // white=0, black=1


    /// Knight will move in L shapes : 8 variations
    static readonly (int dx, int dy)[] KnightD =
    {
        (1,2), (2,1), (-1, 2), (-2, 1), (1, -2), (2,-1), (-1,-2), (-2,-1)
    };

    /// King will move one square in any direction : 8 variations
    static readonly (int dx, int dy)[] KingD =
    {
        (1,0), (0,1), (-1,0), (0,-1), (1,1), (1,-1), (-1,1), (-1,-1)

    };

    /// Entrypoint: route to the correct move each piece type

    public static List<Vector2Int> PseudoLegal(BoardState s, PieceView p)
    {
        return p.type switch
        {
            PieceType.Pawn => PawnMoves(s, p),
            PieceType.Knight => KnightMoves(s, p),
            PieceType.Bishop => RayMoves(s, p, new[] { (1, 1), (1, -1), (-1, 1), (-1, -1) }),
            PieceType.Rook => RayMoves(s, p, new[] { (1, 0), (0, 1), (-1, 0), (0, -1) }),
            PieceType.Queen => RayMoves(s, p, new[] { (1, 0), (0, 1), (-1, 0), (0, -1), (1, 1), (1, -1), (-1, 1), (-1, -1) }),
            PieceType.King => KingMoves(s, p),
            _ => new List<Vector2Int>()
        };
    }
    /// Pawn moves: 1 square forward, 2 squares forward from starting position, captures diagonally
    /// assume white +y direction, black -y direction
    /// no en passant or promotion yet

    static List<Vector2Int> PawnMoves(BoardState s, PieceView p)
    {
        var outMoves = new List<Vector2Int>();
        int dir = (p.side == Side.White) ? 1 : -1; // direction of movement based on side white moves +y, black -y

        ///current coordinate
        int x = p.square.x;
        int y = p.square.y;

        ///forward movement
        /// one square forward if empty
        int ny = y + dir;
        if (BoardState.In(x, ny) && s.IsEmpty(x, ny))
        {
            outMoves.Add(new Vector2Int(x, ny));

            /// two squares forward if on starting position and both squares empty
            bool atStart = (p.side == Side.White && y == 1) || (p.side == Side.Black && y == 6);
            int ny2 = y + 2 * dir;
            if (atStart && s.IsEmpty(x, ny2))
                outMoves.Add(new Vector2Int(x, ny2));

        }

        /// capturing diagonally
        int[] dxs = { -1, +1 };
        foreach (int dx in dxs)
        {
            int cx = x + dx, cy = y + dir; // capture square
            if (BoardState.In(cx, cy) && s.HasEnemy(cx, cy, p.side))
                outMoves.Add(new Vector2Int(cx, cy));
        }

        if (s.enPassantTarget.HasValue)
        {
            var ep = s.enPassantTarget.Value; // en passant target square
            if (ep.y == y + dir && (ep.x == x - 1 || ep.x == x + 1)) // En passant is only legal if its digonally in front of the pawn
            {
                // destination of the piece has to be empty as well for the move to be legal.
                if (BoardState.In(ep.x, ep.y) && s.IsEmpty(ep.x, ep.y))
                    outMoves.Add(ep);


            }

        }
        return outMoves;
    }

    /// Knight moves in L shapes and can hop over pieces as long as target square is valid

    static List<Vector2Int> KnightMoves(BoardState s, PieceView p)
    {
        var moves = new List<Vector2Int>();

        ///Try each of the 8 possible knight moves
        foreach (var d in KnightD)
        {
            int nx = p.square.x + d.dx;
            int ny = p.square.y + d.dy;
            ///ignore if off board

            if (!BoardState.In(nx, ny))
                continue;

            /// cannot land on a friendly piece
            if (s.HasFriendly(nx, ny, p.side))
                continue;

            moves.Add(new Vector2Int(nx, ny));
        }

        return moves;
    }


    ///sliding pieces: bishop, rook, queen walk rays in their allowed directions until blocked

    static List<Vector2Int> RayMoves(BoardState s, PieceView p, (int dx, int dy)[] dirs)
    {
        var moves = new List<Vector2Int>();
        ///Try each direction stated earlier

        foreach (var d in dirs)
        {
            int x = p.square.x + d.dx;
            int y = p.square.y + d.dy;

            ///keep goin in the direction while within limits of the board
            while (BoardState.In(x, y))
            {
                if (s.IsEmpty(x, y))
                {
                    moves.Add(new Vector2Int(x, y));
                }
                else
                {
                    //This else happens if we hit a piece. If its an enemy piece we can capture it
                    if (s.HasEnemy(x, y, p.side))
                        moves.Add(new Vector2Int(x, y));
                    break; //stop after capturing or hitting a friendly piece 
                }

                ///advacne along the ray
                x += d.dx;
                y += d.dy;
            }

        }

        return moves;
    }


    ///King moves one square in any direction but cannot land on a friendly piece
    ///ADD castling later

    static List<Vector2Int> KingMoves(BoardState s, PieceView p)
    {
        var moves = new List<Vector2Int>();
        ///Try each of the 8 possible king moves
        foreach (var d in KingD)
        {
            int nx = p.square.x + d.dx;
            int ny = p.square.y + d.dy;
            ///ignore if off board
            if (!BoardState.In(nx, ny))
                continue;
            /// cannot land on a friendly piece
            if (s.HasFriendly(nx, ny, p.side))
                continue;
            moves.Add(new Vector2Int(nx, ny));
        }

        /// Castling pseudo legal movement
        /// white king starts at 4,0. White rooks at 0,0 and 7,0
        /// black king starts at 4,7. Black rooks at 0,7 and 7,7

        int rank = (p.side == Side.White) ? 0 : 7;
        Vector2Int K = p.square;
        if (K == new Vector2Int(4, rank)) //only from starting square
        {
            bool kingMoved = (p.side == Side.White) ? s.whiteKingMoved : s.blackKingMoved;

            /// king side (short castling)

            bool rookMovedK = (p.side == Side.White) ? s.whiteKingsideRookMoved : s.blackKingsideRookMoved;
            bool rookPresentK = s.At(new Vector2Int(7, rank)) != null;
            bool emptyF = s.IsEmpty(5, rank);
            bool emptyG = s.IsEmpty(6, rank);

            ///Squares the king is passing through or landing on must not be under attack.
            if (!kingMoved && !rookMovedK && rookPresentK && emptyF && emptyG)
            {
                // Squares king stands on, passes through, and lands on must not be attacked
                if (!SquareAttacked(s, Opposite(p.side), new Vector2Int(4, rank)) &&
                    !SquareAttacked(s, Opposite(p.side), new Vector2Int(5, rank)) &&
                    !SquareAttacked(s, Opposite(p.side), new Vector2Int(6, rank)))
                {
                    moves.Add(new Vector2Int(6, rank)); // castle king side to g-file
                }

            }

            // ----- Queen side (long) -----
            bool rookMovedQ = (p.side == Side.White) ? s.whiteQueensideRookMoved : s.blackQueensideRookMoved;
            bool rookPresentQ = s.At(new Vector2Int(0, rank)) != null;
            bool emptyB = s.IsEmpty(1, rank);
            bool emptyC = s.IsEmpty(2, rank);
            bool emptyD = s.IsEmpty(3, rank);

            if (!kingMoved && !rookMovedQ && rookPresentQ && emptyB && emptyC && emptyD)
            {
                if (!SquareAttacked(s, Opposite(p.side), new Vector2Int(4, rank)) &&
                    !SquareAttacked(s, Opposite(p.side), new Vector2Int(3, rank)) &&
                    !SquareAttacked(s, Opposite(p.side), new Vector2Int(2, rank)))
                {
                    moves.Add(new Vector2Int(2, rank)); // castle queen side to c-file
                }
            }
        }

        return moves;

        // Add this helper function at the class level (outside any method)
        static Side Opposite(Side s) => (s == Side.White) ? Side.Black : Side.White;

        // Is 'sq' attacked by any piece of 'bySide' (pseudo-legal: pins/checks not resolved)
        static bool SquareAttacked(BoardState s, Side bySide, Vector2Int sq)
        {
            for (int y = 0; y < 8; y++)
                for (int x = 0; x < 8; x++)
                {
                    var pv = s.occ[x, y];
                    if (pv == null || pv.side != bySide) continue;

                    // use attack patterns; pawns attack diagonally 1, not forward
                    foreach (var a in AttackMovesFor(s, pv))
                        if (a == sq) return true;
                }
            return false;
        }

        // Attack patterns for each piece (no castling, pawns only diagonals)
        static IEnumerable<Vector2Int> AttackMovesFor(BoardState s, PieceView p)
        {
            int x = p.square.x, y = p.square.y;

            switch (p.type)
            {
                case PieceType.Pawn:
                    {
                        int dir = (p.side == Side.White) ? 1 : -1;
                        int[] dxs = { -1, +1 };
                        foreach (var dx in dxs)
                        {
                            int ax = x + dx, ay = y + dir;
                            if (BoardState.In(ax, ay))
                                yield return new Vector2Int(ax, ay);
                        }
                        yield break;
                    }

                case PieceType.Knight:
                    {
                        foreach (var d in KnightD)
                        {
                            int nx = x + d.dx, ny = y + d.dy;
                            if (BoardState.In(nx, ny)) yield return new Vector2Int(nx, ny);
                        }
                        yield break;
                    }

                case PieceType.King:
                    {
                        foreach (var d in KingD)
                        {
                            int nx = x + d.dx, ny = y + d.dy;
                            if (BoardState.In(nx, ny)) yield return new Vector2Int(nx, ny);
                        }
                        yield break; // no castling as an attack
                    }

                case PieceType.Bishop:
                    foreach (var sq in RayAttacks(s, x, y, new[] { (1, 1), (1, -1), (-1, 1), (-1, -1) })) yield return sq;
                    yield break;

                case PieceType.Rook:
                    foreach (var sq in RayAttacks(s, x, y, new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })) yield return sq;
                    yield break;

                case PieceType.Queen:
                    foreach (var sq in RayAttacks(s, x, y, new[] { (1, 0), (-1, 0), (0, 1), (0, -1), (1, 1), (1, -1), (-1, 1), (-1, -1) })) yield return sq;
                    yield break;
            }
        }

        // Walk a ray, stop after first occupied (it is attacked)
        static IEnumerable<Vector2Int> RayAttacks(BoardState s, int x, int y, (int dx, int dy)[] dirs)
        {
            foreach (var (dx, dy) in dirs)
            {
                int nx = x + dx, ny = y + dy;
                while (BoardState.In(nx, ny))
                {
                    yield return new Vector2Int(nx, ny);
                    if (!s.IsEmpty(nx, ny)) break; // blocked by first piece
                    nx += dx; ny += dy;
                }
            }
        }


    }

    // --- PUBLIC: filter pseudo-legal into legal (king not left in check) ---
    public static List<Vector2Int> LegalMoves(BoardState s, PieceView p)

    {
        var legal = new List<Vector2Int>();
        var pseudo = PseudoLegal(s, p);
        foreach (var to in pseudo)
        {
            if (!LeavesKingInCheck(s, p, to))
                legal.Add(to);
        }
        return legal;
    }

    public static bool IsInCheck(BoardState s, Side side)


    {
        

        // find king square
        Vector2Int king = new Vector2Int(-1, -1);
        for (int y = 0; y < 8; y++)
            for (int x = 0; x < 8; x++)
            {
                var pv = s.occ[x, y];
                if (pv != null && pv.side == side && pv.type == PieceType.King)
                {
                    king = new Vector2Int(x, y);
                    goto found;
                }
            }
        found:
        if (king.x < 0) return false; // should not happen
        return SquareAttackedSnapshot(BuildSnapshot(s), SideIndex(Opposite(side)), king);
    }

    // --- INTERNAL: simulate a move on a snapshot and see if king is attacked ---
    static bool LeavesKingInCheck(BoardState s, PieceView p, Vector2Int to)
    {
        var snap = BuildSnapshot(s);

        int fx = p.square.x, fy = p.square.y;
        int tx = to.x, ty = to.y;
        int me = (p.side == Side.White) ? 0 : 1;

        // en passant capture removal (if applicable)
        if (p.type == PieceType.Pawn && s.enPassantTarget.HasValue && to == s.enPassantTarget.Value)
        {
            int dir = (p.side == Side.White) ? 1 : -1;
            snap.type[tx, ty - dir] = -1;
            snap.side[tx, ty - dir] = -1;
        }

        // clear from, handle castling rook hop in snapshot if king castles
        bool kingCastle = (p.type == PieceType.King && Mathf.Abs(tx - fx) == 2 && ty == fy);

        // move piece
        snap.type[tx, ty] = (int)p.type;
        snap.side[tx, ty] = me;
        snap.type[fx, fy] = -1;
        snap.side[fx, fy] = -1;

        // castling rook
        if (kingCastle)
        {
            int rank = fy;
            if (tx == 6) // king side
            {
                // rook 7->5
                snap.type[5, rank] = (int)PieceType.Rook;
                snap.side[5, rank] = me;
                snap.type[7, rank] = -1;
                snap.side[7, rank] = -1;
            }
            else if (tx == 2) // queen side
            {
                // rook 0->3
                snap.type[3, rank] = (int)PieceType.Rook;
                snap.side[3, rank] = me;
                snap.type[0, rank] = -1;
                snap.side[0, rank] = -1;
            }
        }

        // locate my king AFTER the move
        Vector2Int king = new Vector2Int(-1, -1);
        for (int y = 0; y < 8; y++)
            for (int x = 0; x < 8; x++)
                if (snap.side[x, y] == me && snap.type[x, y] == (int)PieceType.King)
                    king = new Vector2Int(x, y);

        // if we just moved the king, its new square is 'to'
        if (p.type == PieceType.King) king = new Vector2Int(tx, ty);

        int opp = 1 - me;
        return SquareAttackedSnapshot(snap, opp, king);
    }

    // --- snapshot (side,type) arrays so we never touch PieceView during tests ---
    struct Snapshot { public int[,] side; public int[,] type; }
    static Snapshot BuildSnapshot(BoardState s)
    {
        var snap = new Snapshot { side = new int[8, 8], type = new int[8, 8] };
        for (int y = 0; y < 8; y++)
            for (int x = 0; x < 8; x++)
            {
                var pv = s.occ[x, y];
                if (pv == null) { snap.side[x, y] = -1; snap.type[x, y] = -1; }
                else { snap.side[x, y] = (pv.side == Side.White) ? 0 : 1; snap.type[x, y] = (int)pv.type; }
            }
        return snap;
    }

    // --- attack detection entirely on the snapshot ---
    static bool SquareAttackedSnapshot(Snapshot s, int bySide, Vector2Int sq)
    {
        int xT = sq.x, yT = sq.y;

        // Knights
        var N = new (int dx, int dy)[] { (1, 2), (2, 1), (-1, 2), (-2, 1), (1, -2), (2, -1), (-1, -2), (-2, -1) };
        foreach (var d in N)
        {
            int nx = xT + d.dx, ny = yT + d.dy;
            if (!In(nx, ny)) continue;
            if (s.side[nx, ny] == bySide && s.type[nx, ny] == (int)PieceType.Knight) return true;
        }

        // Pawns (bySide attacks "forward" toward the opponent)
        int dir = (bySide == 0) ? 1 : -1; // white=0 goes +y
        int[] dxs = { -1, +1 };
        foreach (var dx in dxs)
        {
            int px = xT - dx, py = yT - dir; // invert relation: attacker sits one step behind diagonally
            if (In(px, py) && s.side[px, py] == bySide && s.type[px, py] == (int)PieceType.Pawn) return true;
        }

        // King (adjacent)
        for (int dy = -1; dy <= 1; dy++)
            for (int dx = -1; dx <= 1; dx++)
            {
                if (dx == 0 && dy == 0) continue;
                int nx = xT + dx, ny = yT + dy;
                if (In(nx, ny) && s.side[nx, ny] == bySide && s.type[nx, ny] == (int)PieceType.King) return true;
            }

        // Sliding: rook/queen (orthogonal)
        var R = new (int dx, int dy)[] { (1, 0), (-1, 0), (0, 1), (0, -1) };
        foreach (var d in R)
        {
            int nx = xT + d.dx, ny = yT + d.dy;
            while (In(nx, ny))
            {
                if (s.side[nx, ny] != -1)
                {
                    if (s.side[nx, ny] == bySide &&
                        (s.type[nx, ny] == (int)PieceType.Rook || s.type[nx, ny] == (int)PieceType.Queen))
                        return true;
                    break;
                }
                nx += d.dx; ny += d.dy;
            }
        }

        // Sliding: bishop/queen (diagonal)
        var B = new (int dx, int dy)[] { (1, 1), (1, -1), (-1, 1), (-1, -1) };
        foreach (var d in B)
        {
            int nx = xT + d.dx, ny = yT + d.dy;
            while (In(nx, ny))
            {
                if (s.side[nx, ny] != -1)
                {
                    if (s.side[nx, ny] == bySide &&
                        (s.type[nx, ny] == (int)PieceType.Bishop || s.type[nx, ny] == (int)PieceType.Queen))
                        return true;
                    break;
                }
                nx += d.dx; ny += d.dy;
            }
        }

        return false;
    }

    static bool In(int x, int y) => x >= 0 && x < 8 && y >= 0 && y < 8;


}