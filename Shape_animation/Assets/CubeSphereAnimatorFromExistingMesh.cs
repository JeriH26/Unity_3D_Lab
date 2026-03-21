using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Morphs the existing mesh on this object to a sphere and back.
/// Unlike procedural builders, this script reads the current MeshFilter mesh
/// and deforms that mesh's own vertices.
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class CubeSphereAnimatorFromExistingMesh : MonoBehaviour
{
    [Header("Mesh Prep")]
    [Tooltip("Merge coincident vertices first so smoothing/subdivision works across cube seams.")]
    public bool weldSeams = true;

    [Tooltip("Position tolerance used for seam welding. Increase if imported mesh seams are not exactly coincident.")]
    [Min(1e-7f)]
    public float weldEpsilon = 0.0001f;

    [Tooltip("How many subdivision passes to run before morphing. 1-2 is usually enough.")]
    [Range(0, 6)]
    public int subdivisions = 2;

    [Header("Animation")]
    [Tooltip("How many full cycles (original->sphere->original) per second.")]
    [Min(0.01f)]
    public float speed = 0.5f;

    [Header("Appearance")]
    public bool assignMaterial = true;
    public Color cubeColor = new Color(1.0f, 0.5f, 0.0f, 1.0f);
    public Color sphereColor = new Color(0.25f, 0.6f, 1f, 1f);

    private Mesh _runtimeMesh;
    private Vector3[] _originalVerts;
    private Vector3[] _sphereVerts;
    private Vector3[] _workVerts;
    private Vector3[] _cubeNormals;
    private Vector3[] _sphereNormals;
    private Vector3[] _workNormals;
    private Material _runtimeMaterial;

    void Start()
    {
        BuildMesh();

        if (assignMaterial)
        {
            AssignMaterial();
        }
        else
        {
            var mr = GetComponent<MeshRenderer>();
            if (mr != null)
            {
                _runtimeMaterial = mr.material;
                if (_runtimeMaterial != null)
                {
                    _runtimeMaterial.color = cubeColor;
                }
            }
        }
    }

    void Update()
    {
        if (_runtimeMesh == null || _workVerts == null)
        {
            return;
        }

        float raw = Mathf.PingPong(Time.time * speed, 1f);
        float morph = Mathf.SmoothStep(0f, 1f, raw);

        for (int i = 0; i < _workVerts.Length; i++)
        {
            _workVerts[i] = Vector3.LerpUnclamped(_originalVerts[i], _sphereVerts[i], morph);
        }

        _runtimeMesh.vertices = _workVerts;
        for (int i = 0; i < _workNormals.Length; i++)
        {
            _workNormals[i] = Vector3.LerpUnclamped(_cubeNormals[i], _sphereNormals[i], morph).normalized;
        }
        _runtimeMesh.normals = _workNormals;
        _runtimeMesh.RecalculateBounds();

        if (_runtimeMaterial != null)
        {
            _runtimeMaterial.color = Color.Lerp(cubeColor, sphereColor, morph);
        }
    }

    // BuildMesh now reads the scene object's existing mesh instead of generating one.
    void BuildMesh()
    {
        var mf = GetComponent<MeshFilter>();
        if (mf == null || mf.sharedMesh == null)
        {
            Debug.LogWarning("[CubeSphereAnimatorFromExistingMesh] Missing MeshFilter or source mesh.");
            enabled = false;
            return;
        }

        // Clone so imported/shared assets are never modified.
        _runtimeMesh = Instantiate(mf.sharedMesh);
        _runtimeMesh.name = mf.sharedMesh.name + "_RuntimeMorph";

        if (weldSeams)
        {
            _runtimeMesh = WeldByPosition(_runtimeMesh, weldEpsilon);
        }

        for (int i = 0; i < subdivisions; i++)
        {
            _runtimeMesh = Subdivide(_runtimeMesh);
        }

        // Some imported meshes still contain near-duplicate seam verts after subdivision.
        // A second weld pass helps keep shading and silhouette continuous.
        if (weldSeams)
        {
            _runtimeMesh = WeldByPosition(_runtimeMesh, weldEpsilon);
        }

        _originalVerts = _runtimeMesh.vertices;
        if (_originalVerts == null || _originalVerts.Length == 0)
        {
            Debug.LogWarning("[CubeSphereAnimatorFromExistingMesh] Source mesh has no vertices.");
            enabled = false;
            return;
        }

        _sphereVerts = new Vector3[_originalVerts.Length];
        _workVerts = new Vector3[_originalVerts.Length];
        _cubeNormals = new Vector3[_originalVerts.Length];
        _sphereNormals = new Vector3[_originalVerts.Length];
        _workNormals = new Vector3[_originalVerts.Length];

        Bounds b = _runtimeMesh.bounds;
        Vector3 center = b.center;
        float radius = Mathf.Max(b.extents.x, Mathf.Max(b.extents.y, b.extents.z));
        if (radius <= 1e-6f)
        {
            radius = 0.5f;
        }

        for (int i = 0; i < _originalVerts.Length; i++)
        {
            Vector3 fromCenter = _originalVerts[i] - center;
            if (fromCenter.sqrMagnitude <= 1e-12f)
            {
                _sphereVerts[i] = _originalVerts[i];
                _sphereNormals[i] = Vector3.up;
            }
            else
            {
                _sphereVerts[i] = center + fromCenter.normalized * radius;
                _sphereNormals[i] = fromCenter.normalized;
            }
        }

        // Hard-edge cube normals at morph=0 (0-degree smoothing keeps crisp edges).
        _runtimeMesh.RecalculateNormals(0f);
        System.Array.Copy(_runtimeMesh.normals, _cubeNormals, _cubeNormals.Length);

        System.Array.Copy(_originalVerts, _workVerts, _originalVerts.Length);
        System.Array.Copy(_cubeNormals, _workNormals, _cubeNormals.Length);
        _runtimeMesh.vertices = _workVerts;
        _runtimeMesh.normals = _workNormals;
        _runtimeMesh.RecalculateBounds();

        mf.mesh = _runtimeMesh;
    }

    private static Mesh WeldByPosition(Mesh source, float epsilon)
    {
        Vector3[] oldVerts = source.vertices;
        int[] oldTris = source.triangles;

        var newVerts = new List<Vector3>(oldVerts.Length);
        var remap = new int[oldVerts.Length];
        var map = new Dictionary<Vector3Int, int>(oldVerts.Length);
        float inv = 1f / Mathf.Max(epsilon, 1e-12f);

        for (int i = 0; i < oldVerts.Length; i++)
        {
            Vector3 v = oldVerts[i];
            var key = new Vector3Int(
                Mathf.RoundToInt(v.x * inv),
                Mathf.RoundToInt(v.y * inv),
                Mathf.RoundToInt(v.z * inv));

            if (!map.TryGetValue(key, out int idx))
            {
                idx = newVerts.Count;
                newVerts.Add(v);
                map[key] = idx;
            }

            remap[i] = idx;
        }

        var newTris = new int[oldTris.Length];
        for (int i = 0; i < oldTris.Length; i++)
        {
            newTris[i] = remap[oldTris[i]];
        }

        var result = new Mesh
        {
            name = source.name + "_Welded",
            indexFormat = newVerts.Count > 65535
                ? UnityEngine.Rendering.IndexFormat.UInt32
                : UnityEngine.Rendering.IndexFormat.UInt16
        };

        result.SetVertices(newVerts);
        result.SetTriangles(newTris, 0, true);
        result.RecalculateNormals();
        result.RecalculateBounds();
        return result;
    }

    private static Mesh Subdivide(Mesh source)
    {
        Vector3[] vertices = source.vertices;
        int[] triangles = source.triangles;

        var newVertices = new List<Vector3>(vertices);
        var newTriangles = new List<int>(triangles.Length * 4);
        var midpointCache = new Dictionary<long, int>();

        int GetMidpoint(int i0, int i1)
        {
            int a = Mathf.Min(i0, i1);
            int b = Mathf.Max(i0, i1);
            long key = ((long)a << 32) | (uint)b;

            if (midpointCache.TryGetValue(key, out int cached))
            {
                return cached;
            }

            int idx = newVertices.Count;
            newVertices.Add((newVertices[i0] + newVertices[i1]) * 0.5f);
            midpointCache[key] = idx;
            return idx;
        }

        for (int i = 0; i < triangles.Length; i += 3)
        {
            int i0 = triangles[i];
            int i1 = triangles[i + 1];
            int i2 = triangles[i + 2];

            int m01 = GetMidpoint(i0, i1);
            int m12 = GetMidpoint(i1, i2);
            int m20 = GetMidpoint(i2, i0);

            newTriangles.Add(i0);
            newTriangles.Add(m01);
            newTriangles.Add(m20);

            newTriangles.Add(i1);
            newTriangles.Add(m12);
            newTriangles.Add(m01);

            newTriangles.Add(i2);
            newTriangles.Add(m20);
            newTriangles.Add(m12);

            newTriangles.Add(m01);
            newTriangles.Add(m12);
            newTriangles.Add(m20);
        }

        var result = new Mesh
        {
            name = source.name + "_Subdivided",
            indexFormat = newVertices.Count > 65535
                ? UnityEngine.Rendering.IndexFormat.UInt32
                : UnityEngine.Rendering.IndexFormat.UInt16
        };

        result.SetVertices(newVertices);
        result.SetTriangles(newTriangles, 0, true);
        result.RecalculateNormals();
        result.RecalculateBounds();
        return result;
    }

    void AssignMaterial()
    {
        var mr = GetComponent<MeshRenderer>();

        Shader sh = Shader.Find("Universal Render Pipeline/Lit")
                 ?? Shader.Find("Standard")
                 ?? Shader.Find("Unlit/Color");

        if (sh == null)
        {
            Debug.LogWarning("[CubeSphereAnimatorFromExistingMesh] No suitable shader found.");
            return;
        }

        var mat = new Material(sh) { color = cubeColor };
        mr.material = mat;
        _runtimeMaterial = mat;
    }
}
