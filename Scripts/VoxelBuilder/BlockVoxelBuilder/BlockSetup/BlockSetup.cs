using System;
using MUtility;
using UnityEngine;
using Utility.SerializableCollection;

namespace VoxelSystem
{ 
    [Serializable]
    class TransformDirectory : SerializableDictionary<InVoxelDirection, Transform> { }
  
    public class BlockSetup : MonoBehaviour
    {
        public BlockType blockType;
        [ShowIf(nameof(HaveAxis))] public Axis3D axis = Axis3D.X;
        public Mesh mesh;
        
        [Header("Visualisation")]
        [SerializeField, Range(0,0.5f)] float testDistance = 0;
        //  [SerializeField] bool drawGizmo = true;
        
        [SerializeField, HideInInspector] TransformDirectory presentationObjects = new(); 
        [SerializeField, HideInInspector] BlockLibrary library;
        
        // TODO: WARNING - NO LIBRARY
        
        
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
            int allDirectionsCount = VoxelUtility.AllInVoxelDirection.Count;
            
            // Setup presentationObjects list
            for (var i = 0; i < allDirectionsCount; i++)
            {
                InVoxelDirection voxelDirection = VoxelUtility.AllInVoxelDirection[i];
                if (!presentationObjects.TryGetValue(voxelDirection, out Transform child) || child == null)
                {
                    presentationObjects.Remove(voxelDirection);
                    child = CreateNewChild(voxelDirection);
                    presentationObjects.Add(voxelDirection, child); 
                }
                else
                {
                    SetupChild(child, voxelDirection);
                }
            }

            // Setup list order
            for (var i = 0; i < allDirectionsCount; i++)
            {
                InVoxelDirection voxelDirection = VoxelUtility.AllInVoxelDirection[i];
                Transform child = presentationObjects[voxelDirection];
                child.SetSiblingIndex(i);
            }
            
            // Destroy extra children
            presentationObjects.SortByKey();
            for (int i = transform.childCount - 1; i >= allDirectionsCount; i--)
            {
                Transform child = transform.GetChild(i);
                DestroyImmediate(child.gameObject);
            }
        }

        Transform CreateNewChild(InVoxelDirection d)
        {
            Transform t = new GameObject(d.ToString()).transform;
            SetupChild(t, d);
            t.localRotation = Quaternion.identity;
            t.localScale = Vector3.one;
            return t;
        }
        
        Vector3 GetChildLocalPosition(InVoxelDirection dir) => (Vector3)dir.ToVector() * (0.25f + testDistance);

        void SetupChild(Transform t, InVoxelDirection dir)
        { 
            
            t.SetParent(transform);
            t.localPosition = GetChildLocalPosition(dir);
            t.name = dir.ToString();

            Mesh mesh = this.mesh != null ? this.mesh : DefaultBlockInfo.Instance.GetMesh(blockType);
            
            if (mesh != null)
            {
                var meshFilter = t.GetComponent<MeshFilter>();
                if (meshFilter == null)
                    meshFilter = t.gameObject.AddComponent<MeshFilter>();
                meshFilter.sharedMesh = mesh;
            }

            Material material =
                this.mesh == null ? DefaultBlockInfo.Instance.MeshNotFoundMaterial:
                library == null || library.material == null ? DefaultBlockInfo.Instance.MaterialNotFoundMaterial:
                library.material;
                    
            var meshRenderer = t.GetComponent<MeshRenderer>();
            if (meshRenderer == null)
                meshRenderer = t.gameObject.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;
        }

        
        
        public bool ContainsDirection(InVoxelDirection dir)
        {
            if (presentationObjects.TryGetValue(dir, out Transform child))
                return child.gameObject.activeInHierarchy;

            return false;
        }

        public Matrix4x4 GetTransformation(InVoxelDirection dir)
        {
            if (!presentationObjects.TryGetValue(dir, out Transform child))
                return Matrix4x4.identity;

            Vector3 pos = GetChildLocalPosition(dir);
            Matrix4x4 m = child.localToWorldMatrix;
            // Add offset to matrix
            m.m03 += pos.x;
            m.m13 += pos.y;
            m.m23 += pos.z;
            return m;
        }
    }
}