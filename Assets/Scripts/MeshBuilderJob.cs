/*
    original repo: https://gist.github.com/LukasFratzl/b479a74087e17d729e8319edd4808757
    auther: Lukas Fratzl
    git: https://github.com/LukasFratzl
*/

using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Rendering;

namespace VoxelEngine
{
    public struct MeshBuilderJob : IJob
    {
        [ReadOnly]
        public NativeArray<VertexData> vertexData;

        [ReadOnly]
        public NativeArray<ushort> indices;

        public Mesh.MeshDataArray _meshDataArray;

        public void Execute()
        {
            if (indices.Length > 0)
            {
                Mesh.MeshData meshData = _meshDataArray[0];
                meshData.SetIndexBufferParams(indices.Length, IndexFormat.UInt16);
                meshData.SetVertexBufferParams(vertexData.Length, vertexAttributeDescriptor);
                meshData.GetIndexData<ushort>().CopyFrom(indices);
                meshData.GetVertexData<VertexData>().CopyFrom(vertexData);
                meshData.subMeshCount = 1;
                meshData.SetSubMesh(0, new SubMeshDescriptor(0, indices.Length));
            }
        }

        private static readonly VertexAttributeDescriptor[] vertexAttributeDescriptor =
        {
            new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, dimension: 3, stream: 0),
            new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, dimension: 3, stream: 0),
            new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.Float32, dimension: 4, stream: 0),
            new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, dimension: 4, stream: 0)
        };
    }
}