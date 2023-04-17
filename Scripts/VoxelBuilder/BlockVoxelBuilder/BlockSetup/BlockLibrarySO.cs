using System.Collections.Generic;
using System.Text;
using JetBrains.Annotations;
using MUtility;
using UnityEngine;

namespace VoxelSystem
{
    [CreateAssetMenu(fileName = "BlockLibrary", menuName = "VoxelSystem/BlockLibrary", order = 1)]
    public class BlockLibrarySO : ScriptableObject
    {
        public Color libraryColor = Color.white;
        [SerializeField] List<BlockKey> keys = new ();
        [SerializeField] List<CustomMesh> meshes = new ();

        public void AddBlock(BlockKey key, CustomMesh mesh)
        {
            keys.Add(key);
            meshes.Add(mesh);
        }

        public void Clear()
        {
            keys.Clear();
            meshes.Clear();
        }

        public bool TryGetMesh(Block block, out CustomMesh mesh) 
        {
            mesh = default;
            BlockType blockType = block.blockType;
            Axis3D axis = block.axis;
            InVoxelDirection dir = BlockVoxelUtility.FromVector(block.inVoxelDirection);
            int index = keys.IndexOf(new BlockKey(blockType, dir, axis));
            if (index < 0 )
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
                { messageType = MessageType.Warning, messageSize = MessageSize.Normal};
        
        public string WarningMessage {
            get
            {
                StringBuilder stringBuilder = new ();
                foreach (BlockType blockType in BlockVoxelUtility.AllBlockType)
                {
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