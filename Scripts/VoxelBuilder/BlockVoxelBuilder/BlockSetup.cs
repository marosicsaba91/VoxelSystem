using System;
using System.Collections.Generic;
using System.Linq;
using MUtility;
using UnityEngine;
using UnityEngine.Serialization;
using Utility.SerializableCollection;

namespace VoxelSystem
{

    [Serializable]
    class TransformDirectory : SerializableDictionary<SubVoxel, Transform>
    {
    }

    [Serializable]
    struct MeshInfo
    {
        public List<Mesh> meshes;
        public SubVoxel subVoxel;
    }
    
    public class BlockSetup : MonoBehaviour
    {
        public BlockType blockType;
        [ShowIf(nameof(HaveAxis))] public Axis3D axis = Axis3D.X;

        [Header("Visualisation")] [SerializeField, Range(0, 0.5f)]
        float testDistance = 0;
 
        [FormerlySerializedAs("presentationObjects2")] [SerializeField] TransformDirectory transformDictionary = new();
        [SerializeField, HideInInspector] BlockLibrary library;

        [SerializeField] List<MeshInfo> meshInfos;

        bool HaveAxis => blockType.HaveAxis();

        void OnValidate()
        {
            library = GetComponentInParent<BlockLibrary>();
            if (!blockType.HaveAxis())
                axis = default;
        }

        public void Setup()
        {
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;

            if (!blockType.HaveAxis())
                axis = default;

            CleanInternalState();
        }


        void CleanInternalState()
        {
            int allDirectionsCount = SubVoxelUtility.AllSubVoxel.Count;

            // Setup presentationObjects list
            for (var i = 0; i < allDirectionsCount; i++)
            {
                SubVoxel voxelDirection = SubVoxelUtility.AllSubVoxel[i];
                if (!transformDictionary.TryGetValue(voxelDirection, out Transform child) || child == null)
                {
                    transformDictionary.Remove(voxelDirection);
                    child = CreateNewChild(voxelDirection);
                    transformDictionary.Add(voxelDirection, child);
                }
                else
                {
                    SetupChild(child, voxelDirection);
                }
            }

            // Setup list order
            for (var i = 0; i < allDirectionsCount; i++)
            {
                SubVoxel voxelDirection =SubVoxelUtility.AllSubVoxel[i];
                Transform child = transformDictionary[voxelDirection];
                child.SetSiblingIndex(i);
            }

            // Destroy extra children
            transformDictionary.SortByKey();
            for (int i = transform.childCount - 1; i >= allDirectionsCount; i--)
            {
                Transform child = transform.GetChild(i);
                DestroyImmediate(child.gameObject);
            }
        }

        Transform CreateNewChild(SubVoxel d)
        {
            Transform t = new GameObject(d.ToString()).transform;
            SetupChild(t, d);
            t.localRotation = Quaternion.identity;
            t.localScale = Vector3.one;
            return t;
        }

        Vector3 GetChildLocalPosition(SubVoxel dir) => (Vector3)dir.ToVector() * (0.25f + testDistance);

        void SetupChild(Transform t, SubVoxel subVoxel)
        {
            t.SetParent(transform);
            t.localPosition = GetChildLocalPosition(subVoxel);
            t.name = subVoxel.ToString();
            
            Mesh mesh = TryFindMesh(subVoxel);
            bool isMeshFound = mesh != null;
            if(mesh == null) 
                mesh = DefaultBlockInfo.Instance.GetMesh(blockType);

            if (mesh != null)
            {
                var meshFilter = t.GetComponent<MeshFilter>();
                if (meshFilter == null)
                    meshFilter = t.gameObject.AddComponent<MeshFilter>();
                meshFilter.sharedMesh = mesh;
            }

            Material material =
                !isMeshFound ? DefaultBlockInfo.Instance.MeshNotFoundMaterial :
                library == null || library.material == null ? DefaultBlockInfo.Instance.MaterialNotFoundMaterial :
                library.material;

            var meshRenderer = t.GetComponent<MeshRenderer>();
            if (meshRenderer == null)
                meshRenderer = t.gameObject.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;
        }

        public Mesh TryFindMesh(SubVoxel subVoxel)
        {
            foreach (MeshInfo meshInfo in meshInfos)
            {
                if (meshInfo.subVoxel.HasFlag(subVoxel))
                    return meshInfo.meshes.FirstOrDefault();
            }

            return null;
        }

        public Matrix4x4 GetTransformation(SubVoxel subVoxel)
        {
            if (!transformDictionary.TryGetValue(subVoxel, out Transform child))
            {
                return Matrix4x4.identity;
            }

            // Matrix4x4 m = child.localToWorldMatrix;
            Matrix4x4 m = Matrix4x4.TRS(child.localPosition, child.localRotation, child.localScale);
            

            // Vector3 pos = GetChildLocalPosition(dir);
            // m.m03 += pos.x;
            // m.m13 += pos.y;
            // m.m23 += pos.z;
            
            return m;
        }
    }
}