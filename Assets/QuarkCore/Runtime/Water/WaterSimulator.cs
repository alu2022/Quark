using System;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter),typeof(MeshRenderer))]
public class WaterSimulator : MonoBehaviour
{
    public enum SimulateType { 
        Sin,
        Gerstner,
        FFT,
    }

    #region Inspector

    public SimulateType type = SimulateType.Sin;
    public Vector2Int segment = new Vector2Int(100,100);

    [Space]
    public float amplitude = 1.0f;//振幅
    public float[] wavelength;//波长
    public float[] speed;//速度
    public Vector2[] direction;//风向
    public float sharp = 0.5f;

    #endregion

    #region Cycle

    private void Awake()
    {
        var meshFilter = GetComponent<MeshFilter>();
        meshFilter.mesh = GenerateMesh();
    }

    private void Update()
    {
        if (type == SimulateType.Sin) SinWave();
        else if (type == SimulateType.Gerstner) GerstnerWave();
    }

    #endregion

    #region Private

    private Mesh mesh;
    private Vector3[] baseVertices;

    private Mesh GenerateMesh() {
        if (segment.x <= 0 || segment.y == 0)
            throw new System.InvalidOperationException("segment.x and segment.y must be positive int");

        mesh = new Mesh();
        mesh.name = "Water Mesh";

        int verticeCount = (segment.x + 1) * (segment.y + 1);
        Vector3[] vertices = new Vector3[verticeCount];
        Vector2[] uv = new Vector2[verticeCount];
        int[] triangles = new int[segment.x * segment.y * 6];

        for (int vIdx = 0, y = 0; y <= segment.y; y++)
        {
            for (int x = 0; x <= segment.x; x++, vIdx++)
            {
                vertices[vIdx] = new Vector3((float)x / segment.x - 0.5f, 0, (float)y / segment.y - 0.5f);
                uv[vIdx] = new Vector2((float)x / segment.x, (float)y / segment.y);
            }
        }

        for (int vIdx = 0, tIdx = 0, y = 0; y < segment.y; y++, vIdx++)
        {
            for (int x = 0; x < segment.x; x++, vIdx++, tIdx += 6)
            {
                triangles[tIdx] = vIdx;
                triangles[tIdx + 1] = triangles[tIdx + 4] = vIdx + segment.x + 1;
                triangles[tIdx + 2] = triangles[tIdx + 3] = vIdx + 1;
                triangles[tIdx + 5] = vIdx + segment.x + 2;
            }
        }

        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        baseVertices = mesh.vertices;

        return mesh;
    }

    private void SinWave() {
        Vector3[] vertices = mesh.vertices;
        Vector3[] normals = new Vector3[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 vertice = baseVertices[i];
            for (int k = 0; k < direction.Length; k++)
            {
                float w = (float)(2 * Math.PI / wavelength[k]);
                float vertexVal = Time.time * speed[k] + w * Vector2.Dot(direction[k], new Vector2(vertices[i].x, vertices[i].z));
                //TODO float normalVal = Time.time * speed[k] + w * Vector2.Dot(direction[k], new Vector2(vertices[i].x, vertices[i].z));
                vertice += Vector3.one * amplitude * Mathf.Sin(vertexVal);
            }
            vertices[i] = vertice;
        }
        mesh.vertices = vertices;
        mesh.normals = normals;
        //mesh.RecalculateNormals();
    }

    private void GerstnerWave() {
        Vector3[] vertices = mesh.vertices;
        Vector3[] normals = mesh.normals;
        Vector4[] tangents = mesh.tangents;
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 vertice = baseVertices[i];
            Vector3 normal = baseVertices[i];
            float allY = 0;
            for (int j = 0; j < direction.Length; j++)
            {
                Vector3 ver = calSingleWaveVertexs(j, vertice);
                vertice.x += ver.x;
                vertice.z += ver.z;
                allY += ver.y;
            }
            // vertice.y = allY;
            //这样波峰会更尖锐
            vertice = vertice + normals[i] * allY;
            vertices[i] = vertice;

            float nx = 0;
            float nz = 0;
            float ny = 0;
            float tx = 0;
            float tz = 0;
            float ty = 0;
            for (int j = 0; j < direction.Length; j++)
            {
                Vector3 tangent;
                Vector3 ver = calSingleWaveNormals(j, vertice, out tangent);
                nx += ver.x;
                nz += ver.z;
                ny += ver.y;
                tx += tangent.x;
                tz += tangent.z;
                ty += tangent.y;
            }
            if (tangents.Length > 0)
            {
                tangents[i].x = -(tx);
                tangents[i].z = 1 - (tz);
                tangents[i].y = ty;
            }
            normal.x = -(nx);
            normal.z = -(nz);
            normal.y = 1 - (ny);
            normal = Vector3.Normalize(normal);
            normals[i] = normal;
        }
        mesh.vertices = vertices;
    }

    private Vector3 calSingleWaveVertexs(int index, Vector3 vertice)
    {
        Vector3 ver = Vector3.zero;
        Vector2 WaveDir = direction[index];
        float w = (float)(2 * Math.PI / wavelength[index]);
        float Qi = sharp / (w * amplitude);
        float cosNum = Mathf.Cos(w * Vector2.Dot(WaveDir, new Vector2(vertice.x, vertice.z)) + Time.time * speed[index]);
        float sinNum = Mathf.Sin(w * Vector2.Dot(WaveDir, new Vector2(vertice.x, vertice.z)) + Time.time * speed[index]);

        ver.x = Qi * amplitude * WaveDir.x * cosNum;
        ver.z = Qi * amplitude * WaveDir.y * cosNum;
        ver.y = sinNum * amplitude;
        return ver;
    }

    private Vector3 calSingleWaveNormals(int index, Vector3 vertice, out Vector3 tangent)
    {
        Vector2 WaveDir = direction[index];
        Vector3 ver = Vector3.zero;
        Vector3 _tangent = Vector3.zero;
        Vector3 P = new Vector3(vertice.x, vertice.z, vertice.y);
        float w = (float)(1f / wavelength[index]);
        float WA = w * amplitude;

        float Qi = sharp / WA;

        float sFun = Mathf.Sin(w * Vector3.Dot(WaveDir, P) + speed[index] * Time.time);
        float cFun = Mathf.Cos(w * Vector3.Dot(WaveDir, P) + speed[index] * Time.time);

        float nx = WaveDir.x * WA * cFun;
        float nz = WaveDir.y * WA * cFun;
        float ny = Qi * WA * sFun;
        ver.x = -nx;
        ver.z = -nz;
        ver.y = ny - 1;

        _tangent.x = Qi * WaveDir.x * WaveDir.y * WA * sFun;
        _tangent.z = Qi * WaveDir.y * WaveDir.y * WA * sFun;
        _tangent.y = -WaveDir.y * WA * cFun;

        tangent = Vector3.Normalize(_tangent);
        return ver;
    }

    #endregion
}