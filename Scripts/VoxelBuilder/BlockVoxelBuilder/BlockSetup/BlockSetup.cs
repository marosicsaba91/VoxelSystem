using System;
using MUtility;
using UnityEngine;
using Utility.SerializableCollection;

namespace VoxelSystem
{ 
    [Serializable]
    class TransformDirectory : SerializableDictionary<InVoxelDirection, Transform> { }
 
     [ExecuteAlways]
    public class BlockSetup : MonoBehaviour
    {
        public BlockType blockType;
        [ShowIf(nameof(HaveAxis))] public Axis3D axis = Axis3D.X;
        public Mesh mesh;
        public Material testMaterial;
        
        [Header("Visualisation")]
        [SerializeField, Range(0,0.5f)] float testDistance = 0;
        [SerializeField] bool drawGizmo = true;


        [SerializeField, HideInInspector] TransformDirectory presentationObjects = new(); 
        
        
        bool HaveAxis => blockType.HaveAxis();

        void Update()
        {
            if (Application.isPlaying) return;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            
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
        
        void SetupChild(Transform t, InVoxelDirection d)
        { 
            t.SetParent(transform);
            t.localPosition = (Vector3)d.ToVector() * (0.25f + testDistance);
            t.name = d.ToString();

            Mesh mesh = this.mesh != null ? this.mesh : DefaultBlockInfo.Instance.GetMesh(blockType);
            
            if (mesh != null)
            {
                var meshFilter = t.GetComponent<MeshFilter>();
                if (meshFilter == null)
                    meshFilter = t.gameObject.AddComponent<MeshFilter>();
                meshFilter.sharedMesh = mesh;
            }

            Material material = testMaterial != null ? testMaterial : DefaultBlockInfo.Instance.GetBasicMaterial();
            
            var meshRenderer = t.GetComponent<MeshRenderer>();
            if (meshRenderer == null)
                meshRenderer = t.gameObject.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;
        }

        public BlockTransformation GetTransformation(Transform blockTransform)
        {
            BlockTransformation transformation = new ()
            {
                Rotation = blockTransform.rotation.eulerAngles,
                Scale = blockTransform.lossyScale
            };
            transformation.SetPosition(blockTransform.position);
            return transformation;
        }
        
        
        // bool ContainsValue<TK, TV>(IDictionary<TK, TV> dictionary, TV value) => 
        //    dictionary.Any(kvp => kvp.Value.Equals(value));
    }
}