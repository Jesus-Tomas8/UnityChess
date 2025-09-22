using UnityEngine;
using UnityEngine.ProBuilder;




/// Maps between screen clicks  board squares and squares  world positions
/// for a single flat board mesh (your Quad). Uses the MeshCollider UVs.


public class BoardGrid : MonoBehaviour
{
    public int size = 8;                 /// number of squares per side (8x8)
    public Renderer boardRenderer;       /// MeshRenderer on your BoardTop (the Quad)
    public Collider boardCollider;       /// MeshCollider on your BoardTop
    public float yOffset = 0.02f;        /// lift for highlights/pieces above the surface
    public LayerMask rayMask = ~0; // will set this in the Inspector to "Board" only

    public bool invertX = false;
    public bool invertY = false;
    public bool swapXY = false;


    /// Convert a camera ray to a board square (x,y). Returns true if the ray hit the board.

    public bool RayToSquare(Ray ray, out Vector2Int sq)
    {
        /// Raycast up to 500 units; hit gives us which collider we struck
        if (Physics.Raycast(ray, out var hit, 500f, rayMask, QueryTriggerInteraction.Ignore))
        {
            /// Only accept hits on the BoardTop's collider
            if (hit.collider == boardCollider)
            {
                /// Test 2: log the hit info
                Debug.Log($"[BoardGrid] Hit {hit.collider.name}  uv={hit.textureCoord}  -> sq=({Mathf.FloorToInt(hit.textureCoord.x * size)},{Mathf.FloorToInt(hit.textureCoord.y * size)})");


                /// UVs across the Quad go from 0..1 in X and Y
                Vector2 uv = hit.textureCoord;

                /// Scale UVs into 0..size and floor to get integer square indices
                int x = Mathf.Clamp(Mathf.FloorToInt(uv.x * size), 0, size - 1);
                int y = Mathf.Clamp(Mathf.FloorToInt(uv.y * size), 0, size - 1);

                // Apply orientation so squares match BoardState coordinates
                if (swapXY) { int tmp = x; x = y; y = tmp; }
                if (invertX) x = size - 1 - x;
                if (invertY) y = size - 1 - y;


                sq = new Vector2Int(x, y);
                return true;
            }
        }
        sq = default;
        return false;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!boardRenderer) return;
        Gizmos.color = Color.yellow;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                Gizmos.DrawSphere(SquareCenterWorld(new Vector2Int(x, y)), 0.05f);
    }
#endif


    /// Center world position of a given square (x,y) on the board surface.

    public Vector3 SquareCenterWorld(Vector2Int sq)
    {
        // Map logical (BoardState) square to physical (mesh) square
        var ps = sq;
        if (swapXY) { int tmp = ps.x; ps.x = ps.y; ps.y = tmp; }
        if (invertX) ps.x = size - 1 - ps.x;
        if (invertY) ps.y = size - 1 - ps.y;


        var r = boardRenderer;
        var t = r.transform;
        var mf = r.GetComponent<MeshFilter>();
        if (!mf || !mf.sharedMesh)
        {
            // Safe fallback: no assignment to t.position! Just return a lifted position.
            return t.position + (-t.forward).normalized * yOffset;
        }

        var mesh = mf.sharedMesh;

        // Get local-space bounds of the board mesh (Unity Quad min/max are in X/Y)
        Vector3 min = mesh.bounds.min;
        Vector3 max = mesh.bounds.max;

        // Transform local bounds corners to world space
        /// VERY IMPORTANT: min-y is BOTTOM on the Quad, max-y is TOP and must match raytosquare UVS
        Vector3 bl = t.TransformPoint(new Vector3(min.x, min.y, 0)); // bottom-left on board plane
        Vector3 br = t.TransformPoint(new Vector3(max.x, min.y, 0)); // bottom-right
        Vector3 tl = t.TransformPoint(new Vector3(min.x, max.y, 0)); // top-left

        Vector3 widthVec = br - bl;   // along files
        Vector3 depthVec = tl - bl;   // along ranks

        // IMPORTANT: your board’s Transform.forward points DOWN at X=+90°,
        // so "up from surface" is -forward:
        Vector3 surfUp = (-t.forward).normalized;

        float u = (ps.x + 0.5f) / size;
        float v = (ps.y + 0.5f) / size;

        // Center on the surface; we keep yOffset=0 in the Inspector
        return bl + widthVec * u + depthVec * v + surfUp * yOffset;
    }



    public Vector2 CellSizeWorld()
    {
        var r = boardRenderer;
        var t = r.transform;
        var mf = r.GetComponent<MeshFilter>();
        if (!mf || !mf.sharedMesh) return new Vector2(1f, 1f);

        var mesh = mf.sharedMesh;
        Vector3 min = mesh.bounds.min;
        Vector3 max = mesh.bounds.max;

        Vector3 bl = t.TransformPoint(new Vector3(min.x, min.y, 0));
        Vector3 br = t.TransformPoint(new Vector3(max.x, min.y, 0));
        Vector3 tl = t.TransformPoint(new Vector3(min.x, max.y, 0));

        float width = (br - bl).magnitude;
        float depth = (tl - bl).magnitude;

        return new Vector2(width / size, depth / size);
    }
}

