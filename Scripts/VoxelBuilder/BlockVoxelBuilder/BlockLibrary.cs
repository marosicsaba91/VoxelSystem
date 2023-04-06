using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using MUtility;
using UnityEngine; 
using Utility.SerializableCollection;

namespace VoxelSystem
{
    [Serializable]
    public class BlockSetting
    {
        [UsedImplicitly] public string name = "Block";
        public BlockType blockType;
        public InVoxelDirection inVoxelDirection;
        public Mesh mesh;
        [HideInInspector] public Vector3Int doubleSize = new (1,1,1);
        public Vector3Int normal =  new (1,1,1);
        [SerializeField, UsedImplicitly] DisplayMember realSize = new (nameof(RealSize));
        
        public Vector3 RealSize
        {
            get => (Vector3)doubleSize / 2f;
            set => doubleSize = new Vector3Int(
                    Mathf.RoundToInt(Mathf.Max(1, value.x * 2)),
                    Mathf.RoundToInt(Mathf.Max(1, value.y * 2)),
                    Mathf.RoundToInt(Mathf.Max(1, value.z * 2)));
        }

        public Bool3 rotate = Bool3.false3;
        public Bool3 mirror = Bool3.false3;
        public Bool3 scale = Bool3.false3;
        
        [Range(1, 10)] public int maxScale = 5;
    }
    
    [Serializable]
    public class BakedBlock
    {
        public List<Mesh> meshes;
    }
    
    [Serializable]
    public struct BlockKey
    {
        public BlockType blockType;
        public Vector3Int inVoxelDirection;
        public Vector3Int normal;
        public Vector3Int doubleSize;
            
        public BlockKey(BlockType blockType, Vector3Int inVoxelDirection, Vector3Int normal, Vector3Int doubleSize)
        {
            this.blockType = blockType;
            this.inVoxelDirection = inVoxelDirection;
            this.doubleSize = doubleSize;
            this.normal = normal;
        }
    }
    
    [Serializable]
    public class BakedBlockDictionary : SerializableDictionary<BlockKey, BakedBlock>
    {
        public void NewElement(BlockKey key, Mesh mesh)
        {
            if (!ContainsKey(key))              
                Add(key, new BakedBlock { meshes = new List<Mesh>() });


            this[key].meshes.Add(mesh);
        }
    }
    
    [CreateAssetMenu(fileName = "BlockLibrary", menuName = "VoxelSystem/BlockLibrary", order = 3)]
    public class BlockLibrary : ScriptableObject
    {
        public Color color = Color.white;
        public List<BlockSetting> blocks = new();

        public BakedBlockDictionary bakedDictionary = new();
        public DisplayMember regenerateLibrary = new(nameof(RegenerateLibrary));
        
        void RegenerateLibrary()
        {
            bakedDictionary.Clear();

            foreach (BlockSetting blockSetting in blocks)
                AddToCache(blockSetting);
        }
        
        void AddToCache(BlockSetting blockSetting)
        {
            Mesh originalMesh = blockSetting.mesh;
            if(originalMesh == null)
                return;
            
            BlockType blockType = blockSetting.blockType;
            Vector3Int ivd = blockSetting.inVoxelDirection.ToVector();
            Vector3Int doubleSize = blockSetting.doubleSize;
            Vector3Int normal = blockSetting.normal; 
           
           //BlockKey originalKey = new BlockKey(blockType, ivd, normal, doubleSize);
           //bakedDictionary.NewElement(originalKey, originalMesh);

           // ADD MODIFIED MESHES

           var allPossibleTransformation = GenerateTransformations(blockSetting.rotate, blockSetting.mirror);
           Debug.Log($"{blockSetting.name} :   {allPossibleTransformation.Count}");


           foreach (Matrix4x4 transformation in allPossibleTransformation)
           {
               Vector3Int transformedIvd = ToVector3Int(transformation.MultiplyVector(ivd));
               Vector3Int transformedDoubleSize = ToVector3Int(transformation.MultiplyVector(doubleSize));
               Vector3Int transformedNormal = ToVector3Int(transformation.MultiplyVector(normal));
               transformedDoubleSize = new Vector3Int(
                   Mathf.Abs(transformedDoubleSize.x),
                   Mathf.Abs(transformedDoubleSize.y),
                   Mathf.Abs(transformedDoubleSize.z));
               
               var newKey = new BlockKey(blockType, transformedIvd, transformedNormal, transformedDoubleSize);
               Mesh newMesh = MeshUtility.GetTransformedMesh(originalMesh, transformation);
               
                bakedDictionary.NewElement(newKey, newMesh);
           }
        }

        static Vector3Int ToVector3Int(Vector3 v) => new(
            Mathf.RoundToInt(v.x),
            Mathf.RoundToInt(v.y),
            Mathf.RoundToInt(v.z));


        HashSet<Matrix4x4> GenerateTransformations(Bool3 rotate, Bool3 mirror)  // SOKNAK TŰNIK
        {
            var transformations = new HashSet<Matrix4x4> ();

            for (int rx = 0; rx <= (rotate.x ? 3 : 0); rx++)
            for (int ry = 0; ry <= (rotate.y ? 3 : 0); ry++)
            for (int rz = 0; rz <= (rotate.z ? 3 : 0); rz++)
            for (int mx = 0; mx <= (mirror.x ? 1 : 0); mx++)
            for (int my = 0; my <= (mirror.y ? 1 : 0); my++)
            for (int mz = 0; mz <= (mirror.z ? 1 : 0); mz++)
            { 
                Matrix4x4 transformationMatrix = Matrix4x4.identity;
                if (rotate.x)
                    transformationMatrix *= Matrix4x4.Rotate(Quaternion.Euler(rx * 90f, 0f, 0f));
                if (rotate.y)
                    transformationMatrix *= Matrix4x4.Rotate(Quaternion.Euler(0f, ry * 90f, 0f));
                if (rotate.z)
                    transformationMatrix *= Matrix4x4.Rotate(Quaternion.Euler(0f, 0f, rz * 90f));
                if (mirror.x && mx == 1)
                    transformationMatrix *= Matrix4x4.Scale(new Vector3(-1f, 1f, 1f));
                if (mirror.y && my == 1)
                    transformationMatrix *= Matrix4x4.Scale(new Vector3(1f, -1f, 1f));
                if (mirror.z && mz == 1)
                    transformationMatrix *= Matrix4x4.Scale(new Vector3(1f, 1f, -1f));

                transformations.Add(transformationMatrix);
            }

            return transformations;
        }


        public bool TryGetMesh(Block block, out Mesh mesh)
        {
            if (bakedDictionary == null && !Application.isPlaying)
                RegenerateLibrary();
            
            BlockType blockType = block.blockType;
            Vector3Int inVoxelDirection = block.inVoxelDirection;
            Vector3Int normal = block.normal;
            Vector3Int doubleSize = block.doubleSize;

            var key = new BlockKey(blockType, inVoxelDirection, normal, doubleSize);

            if (!bakedDictionary.TryGetValue(key, out BakedBlock meshes))
            {
                mesh = null;
                return false;
            }

            mesh = meshes.meshes[0];
            return true;
        }
    }
}