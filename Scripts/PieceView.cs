using UnityEngine;

public enum Side { White, Black }
public enum PieceType { Pawn, Knight, Bishop, Rook, Queen, King }

public class PieceView : MonoBehaviour
{
    public Side side;
    public PieceType type;
    public Vector2Int square;

    
    public void PlaceAt(Vector3 worldPos)
    {
        transform.position = worldPos;

        
    }

}


