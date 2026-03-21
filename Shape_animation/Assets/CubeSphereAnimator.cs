using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Self-contained cube ↔ sphere morph animator.
///
/// Usage:
///   1. Attach this script to ANY GameObject (even an empty one).
///   2. Hit Play – a smooth cube↔sphere morph appears automatically.
///
/// Mesh strategy: 6-face grid (cube-sphere).
///   Each of the 6 cube faces is subdivided into an N×N grid.
///   - Cube position  = the original grid point on the cube face.
///   - Sphere position = normalise that direction, scale to radius 0.5.
///   This gives perfectly uniform vertex density on every face, so edges
///   morph cleanly without the pinching that UV-spheres produce.
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class CubeSphereAnimator : MonoBehaviour
{
    [Header("Mesh Quality")]
    [Tooltip("Grid subdivisions per cube face. 24 → 24×24 quads per face = 3456 verts/face.")]
    [Range(4, 48)]
    public int faceSegments = 24;

    [Header("Animation")]
    [Tooltip("How many full cycles (cube→sphere→cube) per second.")]
    [Min(0.01f)]
    public float speed = 0.5f;

    [Header("Appearance")]
    public Color cubeColor = new Color(1.0f, 0.5f, 0.0f, 1.0f);
    public Color sphereColor = new Color(0.25f, 0.6f, 1f, 1f);

    // ── private ──────────────────────────────────────────────────────────────
    private Mesh      _mesh;
    private Vector3[] _sphereVerts;
    private Vector3[] _cubeVerts;
    private Vector3[] _workVerts;
    private Material  _runtimeMaterial;

    // The 6 face normals of a unit cube (outward).
    private static readonly Vector3[] FaceNormals =
    {
        Vector3.up, Vector3.down,
        Vector3.left, Vector3.right,
        Vector3.forward, Vector3.back
    };

    // ─────────────────────────────────────────────────────────────────────────
    void Start()
    {
        BuildMesh();
        AssignMaterial();
    }

    void Update()
    {
        float raw   = Mathf.PingPong(Time.time * speed, 1f);
        float morph = Mathf.SmoothStep(0f, 1f, raw);

        for (int i = 0; i < _workVerts.Length; i++)
            _workVerts[i] = Vector3.LerpUnclamped(_cubeVerts[i], _sphereVerts[i], morph);

        _mesh.vertices = _workVerts;
        _mesh.RecalculateNormals();
        _mesh.RecalculateBounds();

        if (_runtimeMaterial != null)
        {
            _runtimeMaterial.color = Color.Lerp(cubeColor, sphereColor, morph);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Mesh generation — 6-face cube-sphere
    // ─────────────────────────────────────────────────────────────────────────
    void BuildMesh()
    {
        int n = faceSegments;   // quads per edge
        // Each face: (n+1)*(n+1) verts, n*n*2 triangles
        int vertsPerFace = (n + 1) * (n + 1);
        int totalVerts   = vertsPerFace * 6;

        _cubeVerts   = new Vector3[totalVerts];
        _sphereVerts = new Vector3[totalVerts];
        _workVerts   = new Vector3[totalVerts];

        var triList = new List<int>(n * n * 6 * 6);

        for (int face = 0; face < 6; face++)
        {
            int baseIdx = face * vertsPerFace;

            // Build two tangent vectors orthogonal to the face normal.
            Vector3 normal  = FaceNormals[face];
            Vector3 tangent = (face == 0 || face == 1)
                ? Vector3.right
                : Vector3.up;
            Vector3 bitangent = Vector3.Cross(normal, tangent);
            // Flip bitangent for down-face so winding stays consistent.
            if (face == 1) bitangent = -bitangent;

            for (int row = 0; row <= n; row++)
            {
                for (int col = 0; col <= n; col++)
                {
                    // u, v in [-0.5, 0.5]
                    float u = (col / (float)n) - 0.5f;
                    float v = (row / (float)n) - 0.5f;

                    // Point on the cube face (half-extent = 0.5)
                    Vector3 cubePoint = normal * 0.5f + tangent * u + bitangent * v;

                    int idx = baseIdx + row * (n + 1) + col;
                    _cubeVerts[idx]   = cubePoint;
                    _sphereVerts[idx] = cubePoint.normalized * 0.5f;
                }
            }

            // Triangles for this face
            for (int row = 0; row < n; row++)
            {
                for (int col = 0; col < n; col++)
                {
                    int a = baseIdx + row       * (n + 1) + col;
                    int b = baseIdx + (row + 1) * (n + 1) + col;
                    int c = a + 1;
                    int d = b + 1;

                    triList.Add(a); triList.Add(c); triList.Add(b);
                    triList.Add(c); triList.Add(d); triList.Add(b);
                }
            }
        }

        System.Array.Copy(_cubeVerts, _workVerts, totalVerts);

        _mesh = new Mesh
        {
            name        = "CubeSphereRuntime",
            indexFormat = totalVerts > 65535
                ? UnityEngine.Rendering.IndexFormat.UInt32
                : UnityEngine.Rendering.IndexFormat.UInt16
        };
        _mesh.vertices  = _cubeVerts;
        _mesh.triangles = triList.ToArray();
        _mesh.RecalculateNormals();
        _mesh.RecalculateBounds();

        GetComponent<MeshFilter>().mesh = _mesh;
    }

    /// <summary>
    /// Create a simple material so the object is always visible,
    /// regardless of whether URP / Built-in / HDRP is active.
    /// </summary>
    void AssignMaterial()
    {
        var mr = GetComponent<MeshRenderer>();

        // Try URP Lit first, then Standard (Built-in), then an unlit fallback.
        Shader sh = Shader.Find("Universal Render Pipeline/Lit")
                 ?? Shader.Find("Standard")
                 ?? Shader.Find("Unlit/Color");

        if (sh == null)
        {
            Debug.LogWarning("[CubeSphereAnimator] No suitable shader found – MeshRenderer may show pink.");
            return;
        }

        var mat = new Material(sh)
        {
            color = cubeColor
        };
        mr.material = mat;
        _runtimeMaterial = mat;
    }
}
