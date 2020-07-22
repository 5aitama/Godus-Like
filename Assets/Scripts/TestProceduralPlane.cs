using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;
using Saitama.ProceduralMesh;

public class TestProceduralPlane : MonoBehaviour
{
    public float3 planePosition;
    public float3 planeRotation;
    public float2 planeSize;
    [Range(0, 128)]
    public int planeResolution;

    public float planetRadius = 10f;
    public float3 planetPosition = 0f;

    private Mesh            mesh;
    private MeshFilter      meshFilter;

    private void Awake()
    {
        mesh = new Mesh();
        meshFilter = GetComponent<MeshFilter>();

        meshFilter.mesh = mesh;
    }

    private void Update()
    {
        var plane = new ProceduralPlane.Plane(planePosition, planeRotation, planeSize, planeResolution);
        ProceduralPlane.Create(planetPosition, planetRadius, plane, out NativeArray<Triangle> triangles, out NativeArray<Vertex> vertices).Complete();
        mesh.Update(triangles, vertices);

        vertices.Dispose();
        triangles.Dispose();
    }
}
