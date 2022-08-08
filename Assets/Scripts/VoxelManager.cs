using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Rendering;

namespace VoxelEngine
{
    [RequireComponent(typeof(MeshFilter))]
    public class VoxelManager : MonoBehaviour
    {
        private Mesh mesh;
        private MeshFilter meshFilter;
        private MeshCollider meshCollider;
        private int3 gridSize;
        private JobHandle marchingCubesJobHandle;
        private JobHandle meshBuilderHandle;

        [SerializeField]
        private float isoLevel = 0.5f;

        [SerializeField] 
        private int3 chunkSize = new int3(16, 16, 16);

        [SerializeField]
        private float noiseScale = 0.5f;

        [Range(0.0f,0.5f)]
        [SerializeField]
        private float noiseSpeed = 0.005f;

        private bool growEffect;

        void Awake()
        {
            mesh = new Mesh();
            mesh.indexFormat = IndexFormat.UInt16;
            mesh.name = "Marching cubes mesh";
            meshFilter = GetComponent<MeshFilter>();
            meshCollider = GetComponent<MeshCollider>();
            gridSize = chunkSize + new int3(1,1,1);
        }

        // Start is called before the first frame update
        void Start()
        {
            GenerateVoxel();
        }

        private void Update()
        {
            if (marchingCubesJobHandle.IsCompleted)
            {
                if (!growEffect)
                {
                    if (noiseScale >= (chunkSize.x / 2 - 0.5f)) growEffect = true;
                    noiseScale += noiseSpeed;
                }

                if (growEffect)
                {
                    if (noiseScale <= 0f) growEffect = false;
                    noiseScale -= noiseSpeed;
                }

                GenerateVoxel();
            }
        }

        void GenerateVoxel()
        {
            NativeArray<VertexData> nativeVertexData = new NativeArray<VertexData>(15 * gridSize.x * gridSize.y * gridSize.z, Allocator.TempJob);
            NativeArray<ushort> nativeIndicess = new NativeArray<ushort>(15 * gridSize.x * gridSize.y * gridSize.z, Allocator.TempJob);
            NativeCounter nativCounter = new NativeCounter(Allocator.TempJob);

            MarchingCubesJob marchingcubesJob = new MarchingCubesJob
            {
                isoLevel = isoLevel,
                noiseScale = noiseScale,
                chunkSize = chunkSize,
                gridSize = gridSize,
                indices = nativeIndicess,
                vertexData = nativeVertexData,
                vertexCounter = nativCounter,
            };

            marchingCubesJobHandle = marchingcubesJob.Schedule(gridSize.x * gridSize.y * gridSize.z, 32);
            marchingCubesJobHandle.Complete();

            if (marchingCubesJobHandle.IsCompleted)
            {
                Mesh.MeshDataArray meshDataArray = Mesh.AllocateWritableMeshData(1);

                MeshBuilderJob meshBuilderJob = new MeshBuilderJob()
                {
                    indices = marchingcubesJob.indices, // HERE WE STRAIGHT USE THE DATA OF THE MESH GENERATOR
                    vertexData = marchingcubesJob.vertexData, // SAME HERE
                    _meshDataArray = meshDataArray, // MESH ALLOCATOR
                };

                meshBuilderHandle = meshBuilderJob.Schedule(marchingCubesJobHandle);
                meshBuilderHandle.Complete();

                mesh.Clear();

                Mesh.ApplyAndDisposeWritableMeshData(meshBuilderJob._meshDataArray, mesh, MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontNotifyMeshUsers);

                meshFilter.sharedMesh = mesh;
                //meshCollider.sharedMesh = mesh;
            }

            nativCounter.Dispose();
            nativeIndicess.Dispose();
            nativeVertexData.Dispose();
        }
    }

    public struct VertexData
    {
        public float3 Position;
        public float3 Normal;
        public Color Color;
        public float4 UV1;
    }
}
