﻿using Unity.Collections;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Burst;

using Saitama.ProceduralMesh;
using Saitama.Mathematics;

public static class ProceduralPlane
{
    public struct Plane
    {
        public float3 Pos;
        public float3 Rot;
        public float2 Size;
        public int Resolution;

        public int VertexAmount
                => Resolution * Resolution;

        public int SquareAmount
                => (Resolution - 1) * (Resolution - 1);

        public int IndexAmount
                => SquareAmount * 6;

        public Plane(float3 position, float3 rotation, float2 size, int resolution)
        {
            Pos = position;
            Rot = rotation;
            Size = size;
            Resolution = resolution < 2 ? 2 : resolution;
        }
    }

    /// <summary>
    /// Create plane.
    /// </summary>
    /// <param name="plane">Infos abount the plane</param>
    /// <param name="triangles">Plane mesh triangles</param>
    /// <param name="vertices">Plane mesh vertices</param>
    /// <param name="allocator">Allocator for the triangle and vertex array</param>
    public static void Create(Plane plane, out NativeArray<Triangle> triangles, out NativeArray<Vertex> vertices, Allocator allocator)
    {
        triangles = new NativeArray<Triangle>(plane.IndexAmount / 3, allocator, NativeArrayOptions.UninitializedMemory);
        vertices = new NativeArray<Vertex>(plane.VertexAmount, allocator, NativeArrayOptions.UninitializedMemory);

        // Offset between each vertex
        var offset = plane.Size / (plane.Resolution - 1);

        var rot = quaternion.Euler(math.radians(plane.Rot));
        var up = new float3(0, 0, -1);

        for (var i = 0; i < vertices.Length; i++)
        {
            // The position of the vertex inside the plane.
            // Each axis value is in range of 0..plane.Resolution - 1
            var vLocalPos = i.To2D(plane.Resolution);

            var vWorldPos = vLocalPos * offset;
            //var vPos = math.mul(rot, new float3(vWorldPos.x, vWorldPos.y, 0)) + plane.Pos;

            //math.normalize(vPos - Center)
            vertices[i] = new Vertex
            {
                pos = math.mul(rot, new float3(vWorldPos.x, vWorldPos.y, 0)) + plane.Pos,
                norm = math.mul(rot, up),
                col = new float4(0, 0, 0, 0),
                uv0 = (float2)vLocalPos / (plane.Resolution - 1),
            };

            if (vLocalPos.x < plane.Resolution - 1 && vLocalPos.y < plane.Resolution - 1)
            {
                var tIndex = vLocalPos.To1D(plane.Resolution - 1) * 2;

                triangles[tIndex] = new Triangle(i, i + plane.Resolution, i + plane.Resolution + 1);
                triangles[tIndex + 1] = new Triangle(i, i + plane.Resolution + 1, i + 1);
            }
        }
    }

    [BurstCompile]
    private struct PlaneJob : IJobParallelFor
    {
        /// <summary>
        /// Plane position.
        /// </summary>
        [ReadOnly]
        public float3 Position;

        /// <summary>
        /// Plane rotation
        /// </summary>
        [ReadOnly]
        public quaternion Rotation;

        /// <summary>
        /// Plane resolution.
        /// </summary>
        [ReadOnly]
        public int Resolution;

        /// <summary>
        /// Vertex offset
        /// </summary>
        [ReadOnly]
        public float2 Offset;

        /// <summary>
        /// The center of the planet
        /// </summary>
        [ReadOnly]
        public float3 Center;

        /// <summary>
        /// Radius of the planet
        /// </summary>
        [ReadOnly]
        public float Radius;

        /// <summary>
        /// Plane vertices.
        /// </summary>
        [WriteOnly]
        public NativeArray<Vertex> vertices;

        /// <summary>
        /// Plane triangles.
        /// </summary>
        [WriteOnly, NativeDisableParallelForRestriction]
        public NativeArray<Triangle> triangles;

        public void Execute(int index)
        {
            // The position of the vertex inside the plane.
            // Each axis value is in range of 0..plane.Resolution - 1
            var vLocalPos = index.To2D(Resolution);

            var vWorldPos = (vLocalPos - new float2((Resolution - 1) / 2f)) * Offset;
            var vPos = math.mul(Rotation, new float3(vWorldPos.x, vWorldPos.y, 0)) + Position;
            var vDir = math.normalize(vPos - Center);
            vPos = vDir * Radius;

            vertices[index] = new Vertex
            {
                pos     = vPos,
                norm    = vDir,
                col     = new float4(0, 0, 0, 0),
                uv0     = (float2)vLocalPos / (Resolution - 1),
            };

            if (vLocalPos.x < Resolution - 1 && vLocalPos.y < Resolution - 1)
            {
                var tIndex = vLocalPos.To1D(Resolution - 1) * 2;

                triangles[tIndex    ] = new Triangle(index, index + Resolution, index + Resolution + 1);
                triangles[tIndex + 1] = new Triangle(index, index + Resolution + 1, index + 1);
            }
        }
    }

    public static JobHandle Create(float3 center, float radius, Plane plane, out NativeArray<Triangle> triangles, out NativeArray<Vertex> vertices, JobHandle inputDeps = default)
    {
        triangles = new NativeArray<Triangle>(plane.IndexAmount / 3, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        vertices  = new NativeArray<Vertex>(plane.VertexAmount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

        return new PlaneJob
        {
            Center      = center,
            Radius      = radius,
            Position    = plane.Pos,
            Rotation    = quaternion.Euler(math.radians(plane.Rot)),
            Resolution  = plane.Resolution,
            Offset      = plane.Size / (plane.Resolution - 1),
            vertices    = vertices,
            triangles   = triangles,
        }
        .Schedule(vertices.Length, 1, inputDeps);
    }
}