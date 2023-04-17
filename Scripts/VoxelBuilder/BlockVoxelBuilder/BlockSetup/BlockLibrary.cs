using JetBrains.Annotations;
using MUtility;
using UnityEngine;

# if UNITY_EDITOR
using UnityEditor;
# endif

namespace VoxelSystem
{
    [ExecuteAlways]
    public class BlockLibrary : MonoBehaviour
    {
        public Material material;
        
        [SerializeField, HideInInspector] BlockSetup[] blockSetups;
        [SerializeField] BlockLibrarySO blockLibrarySO;
        
        [SerializeField] bool autoRegenerateLibrary = false;
        [SerializeField, UsedImplicitly] DisplayMember regenerateLibrary = new(nameof(RegenerateLibrary));

        void Update()
        {
            if(Application.isPlaying) return;

            foreach (BlockSetup setup in blockSetups)
                setup.Setup();
            
            if(autoRegenerateLibrary)
                RegenerateLibrary();
        }

        public static BlockType currentBlockType;
        public static Axis3D currentAxis;
        public static InVoxelDirection currentDirection;

        void RegenerateLibrary()
        {
            blockLibrarySO.Clear();
            foreach (BlockSetup setup in blockSetups)
            {
                setup.Setup();
                if (setup.mesh == null) continue;

                BlockType blockType = setup.blockType;
                currentBlockType = setup.blockType;
                Axis3D axis = setup.axis;
                currentAxis = setup.axis;

                foreach (InVoxelDirection dir in BlockVoxelUtility.AllInVoxelDirection)
                {
                    if(!setup.ContainsDirection(dir)) continue;
                    currentDirection = dir;
                    Matrix4x4 matrix4X4 = setup.GetTransformation(dir);
                    CustomMesh mesh = MeshUtility.GetTransformedMesh(setup.mesh, matrix4X4);
                    if (setup.ContainsDirection(dir))
                        blockLibrarySO.AddBlock(new BlockKey(blockType, dir, axis), mesh);
                }
            }
            
            # if UNITY_EDITOR
            EditorUtility.SetDirty(blockLibrarySO);
            # endif
        }

        void OnValidate()
        {
            blockSetups = GetComponentsInChildren<BlockSetup>();
            // blockDictionary ??= new BlockDictionary();

        }
    }
}