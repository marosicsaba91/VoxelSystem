using System.Collections.Generic;
using System.Text;
using JetBrains.Annotations;
using MUtility;
using UnityEngine;

namespace VoxelSystem
{
    [ExecuteAlways]
    public class BlockLibrary : MonoBehaviour
    {
        public Material material;

        [SerializeField, HideInInspector] BlockSetup[] blockSetups;
        [SerializeField] Color libraryColor = Color.white;
 

        [Space]
        [SerializeField, ReadOnly] List<BlockKey> keys = new();
        [SerializeField, ReadOnly] List<CustomMesh> meshes = new();
        [SerializeField, UsedImplicitly] 
        DisplayMember clearLibrary = new(nameof(Clear));
        [SerializeField, UsedImplicitly]
        DisplayMember regenerateLibrary = new(nameof(RegenerateLibrary));

        public Color LibraryColor => libraryColor;

        void Update()
        {
            if (Application.isPlaying) return;
            foreach (BlockSetup setup in blockSetups)
                setup.Setup();
        }

        void RegenerateLibrary()
        {
            if (ErrorTest()) return;
            
            Clear();
            foreach (BlockSetup setup in blockSetups)
            {
                setup.Setup();
                if (setup.mesh == null) continue;

                BlockType blockType = setup.blockType;
                Axis3D axis = setup.axis;

                foreach (InVoxelDirection dir in BlockVoxelUtility.AllInVoxelDirection)
                {
                    if (!setup.ContainsDirection(dir)) continue;
                    Matrix4x4 matrix4X4 = setup.GetTransformation(dir);
                    CustomMesh mesh = MeshUtility.GetTransformedMesh(setup.mesh, matrix4X4);
                    if (setup.ContainsDirection(dir))
                        AddBlock(new BlockKey(blockType, dir, axis), mesh);
                }
            }

            MakeDirty();
        }

        bool EnableRegenerate() => gameObject.scene.isLoaded;

        bool ErrorTest()
        {
            if (!EnableRegenerate())
            {
                Debug.LogWarning("Regenerating the Library is not allowed is the prefab is not loaded!");
                return true;
            }

            return false;
        }

        
        
        void MakeDirty()
        {
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        void OnValidate()
        {
            blockSetups = GetComponentsInChildren<BlockSetup>();
        }


        void AddBlock(BlockKey key, CustomMesh mesh)
        {
            keys.Add(key);
            meshes.Add(mesh);
        }

        void Clear()
        {
            if (ErrorTest()) return;
            keys.Clear();
            meshes.Clear();
            MakeDirty();
        }

        public bool TryGetMesh(Block block, out CustomMesh mesh)
        {
            mesh = default;
            BlockType blockType = block.blockType;
            Axis3D axis = block.axis;
            InVoxelDirection dir = SubVoxelUtility.FromVector(block.inVoxelDirection);
            int index = keys.IndexOf(new BlockKey(blockType, dir, axis));
            if (index < 0)
                return false;

            if (meshes != null && meshes.Count > index)
            {
                mesh = meshes[index];
                return true;
            }

            return false;
        }

        // Warning Message

        [SerializeField, UsedImplicitly] DisplayMessage warning =
            new(nameof(WarningMessage), true)
                { messageType = MessageType.Warning, messageSize = MessageSize.Normal };

        public string WarningMessage
        {
            get
            {
                StringBuilder stringBuilder = new();
                foreach (BlockType blockType in BlockVoxelUtility.AllBlockType)
                {
                    if (blockType == BlockType.BreakPoint) continue;
                    
                    foreach (InVoxelDirection dir in BlockVoxelUtility.AllInVoxelDirection)
                    {
                        if (blockType.HaveAxis())
                        {
                            foreach (Axis3D axis in BlockVoxelUtility.AllAxis)
                                MissingKey(blockType, dir, axis);
                        }
                        else
                            MissingKey(blockType, dir, default);
                    }
                }

                return stringBuilder.ToString();

                void MissingKey(BlockType blockType, InVoxelDirection dir, Axis3D axis)
                {
                    BlockKey key = new(blockType, dir, axis);
                    if (!keys.Contains(key))
                    {
                        stringBuilder.Append("Missing: ");
                        key.AppendTo(stringBuilder);
                        stringBuilder.AppendLine();
                    }
                }
            }
        }
    }
}