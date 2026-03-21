using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class SubdivideMeshForMorph : MonoBehaviour
{
    [Min(0)]
    public int subdivisions = 3;

    // How close two vertices must be (object space) to be welded together.
    // 0.0001 works for Unity's default Cube (unit-scale).
    public float weldThreshold = 0.0001f;

    private Mesh _runtimeMesh;

    private void Awake()
    {
        var mf = GetComponent<MeshFilter>();
        if (mf == null || mf.sharedMesh == null)
        {
            return;
        }

        // Duplicate source mesh so we never modify imported/shared assets directly.
        _runtimeMesh = Instantiate(mf.sharedMesh);
        _runtimeMesh.name = mf.sharedMesh.name + "_SubdividedRuntime";

        // STEP 1 – weld seam vertices so subdivision midpoints are shared across faces.
        // Unity's default Cube duplicates vertices at every edge/corner for hard normals;
        // without welding the morph tears at those seams and looks like nothing changed.
        _runtimeMesh = WeldVertices(_runtimeMesh, weldThreshold);

        // STEP 2 – subdivide the now-seamless mesh.
        for (int i = 0; i < subdivisions; i++)
        {
            _runtimeMesh = Subdivide(_runtimeMesh);
        }

        _runtimeMesh.RecalculateBounds();
        _runtimeMesh.RecalculateNormals();
        _runtimeMesh.RecalculateTangents();

        mf.sharedMesh = _runtimeMesh;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Merge vertices whose positions are within `threshold` of each other.
    // This removes the seam duplicates Unity adds for per-face normals.
    // ──────────────────────────────────────────────────────────────────────────
    private static Mesh WeldVertices(Mesh source, float threshold)
    {
        Vector3[] srcVerts = source.vertices;
        int[]     srcTris  = source.triangles;
        int       n        = srcVerts.Length;

        // Map: old index -> canonical (welded) index
        int[] remap = new int[n];
        var   welded = new List<Vector3>();

        for (int i = 0; i < n; i++)
        {
            int found = -1;
            for (int j = 0; j < welded.Count; j++)
            {
                if (Vector3.SqrMagnitude(srcVerts[i] - welded[j]) <= threshold * threshold)
                {
                    found = j;
                    break;
                }
            }
            if (found == -1)
            {
                remap[i] = welded.Count;
                welded.Add(srcVerts[i]);
            }
            else
            {
                remap[i] = found;
            }
        }

        int[] newTris = new int[srcTris.Length];
        for (int i = 0; i < srcTris.Length; i++)
        {
            newTris[i] = remap[srcTris[i]];
        }

        var mesh = new Mesh
        {
            indexFormat = (welded.Count > 65535)
                ? UnityEngine.Rendering.IndexFormat.UInt32
                : UnityEngine.Rendering.IndexFormat.UInt16
        };
        mesh.SetVertices(welded);
        mesh.SetTriangles(newTris, 0, true);
        return mesh;
    }

    private static Mesh Subdivide(Mesh source)
    {
        var vertices = source.vertices;
        var triangles = source.triangles;
        var uv = source.uv;

        var newVertices = new List<Vector3>(vertices);
        var newTriangles = new List<int>(triangles.Length * 4);
        var newUv = (uv != null && uv.Length == vertices.Length)
            ? new List<Vector2>(uv)
            : new List<Vector2>(vertices.Length);

        if (newUv.Count == 0)
        {
            for (int i = 0; i < vertices.Length; i++)
            {
                newUv.Add(Vector2.zero);
            }
        }

        var midpointCache = new Dictionary<long, int>();

        int GetMidpoint(int i0, int i1)
        {
            int a = Mathf.Min(i0, i1);
            int b = Mathf.Max(i0, i1);
            long key = ((long)a << 32) | (uint)b;

            if (midpointCache.TryGetValue(key, out int cachedIndex))
            {
                return cachedIndex;
            }

            Vector3 midPos = (newVertices[i0] + newVertices[i1]) * 0.5f;
            Vector2 midUv = (newUv[i0] + newUv[i1]) * 0.5f;

            int newIndex = newVertices.Count;
            newVertices.Add(midPos);
            newUv.Add(midUv);
            midpointCache[key] = newIndex;

            return newIndex;
        }

        for (int i = 0; i < triangles.Length; i += 3)
        {
            int i0 = triangles[i];
            int i1 = triangles[i + 1];
            int i2 = triangles[i + 2];

            int m01 = GetMidpoint(i0, i1);
            int m12 = GetMidpoint(i1, i2);
            int m20 = GetMidpoint(i2, i0);

            // Split one triangle into four.
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

        var mesh = new Mesh
        {
            indexFormat = (newVertices.Count > 65535)
                ? UnityEngine.Rendering.IndexFormat.UInt32
                : UnityEngine.Rendering.IndexFormat.UInt16
        };

        mesh.SetVertices(newVertices);
        mesh.SetTriangles(newTriangles, 0, true);
        mesh.SetUVs(0, newUv);

        return mesh;
    }
}
