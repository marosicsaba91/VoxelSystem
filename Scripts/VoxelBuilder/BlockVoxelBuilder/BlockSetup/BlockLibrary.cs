using System.Text;
using JetBrains.Annotations;
using MUtility;
using UnityEngine;
using MessageType = MUtility.MessageType;

# if UNITY_EDITOR
using UnityEditor;
# endif

namespace VoxelSystem
{
    [ExecuteAlways]
    public class BlockLibrary : MonoBehaviour
    {
        public Color libraryColor = Color.white;
        public Material material;
        
        [SerializeField, HideInInspector] BlockSetup[] blockSetups;
        [SerializeField] BlockDictionary blockDictionary;
        
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

        void RegenerateLibrary()
        {
            //  if this is not a prefab, do nothing
            if (!gameObject.scene.IsValid()) return;
            string path = AssetDatabase.GetAssetPath(this);
            string folderPath = path.Substring(0, path.LastIndexOf('/'));
            // Clear the dictionary
            
            blockDictionary.Clear();
            
            foreach (BlockSetup setup in blockSetups)
            {
                setup.Setup();
                if (setup.mesh == null) continue;

                BlockType blockType = setup.blockType;
                Axis3D axis = setup.axis;

                foreach (InVoxelDirection dir in VoxelUtility.AllInVoxelDirection)
                {
                    if(!setup.ContainsDirection(dir)) continue;
                    Matrix4x4 matrix4X4 = setup.GetTransformation(dir);
                    Mesh mesh = MeshUtility.GetTransformedMesh(setup.mesh, matrix4X4);
                    // Save mesh to file next to the Library Prefab File In a Folder with the same name 
                    string fileName = $"{blockType}_{dir}_{axis}.asset";
                    string filePath = $"{folderPath}/{fileName}";
                    AssetDatabase.CreateAsset(mesh, filePath);
                    
                    if (setup.ContainsDirection(dir))
                        blockDictionary.AddBlock(new BlockKey(blockType, dir, axis), mesh);
                }
            }
            
            // Make this component dirty
            # if UNITY_EDITOR
            EditorUtility.SetDirty(this);
            # endif
        }


        void OnValidate()
        {
            blockSetups = GetComponentsInChildren<BlockSetup>();
            blockDictionary ??= new BlockDictionary();

        }


        public bool TryGetMesh(Block block, out Mesh mesh)
        {
            mesh = null;
            BlockType blockType = block.blockType;
            Axis3D axis = block.axis;
            InVoxelDirection dir = VoxelUtility.FromVector(block.inVoxelDirection);
            if (!blockDictionary.TryGetValue(new BlockKey(blockType, dir, axis), out BakedBlock bakedBlock))
                return false;

            if (bakedBlock.meshes is { Count: > 0 })
            {
                mesh = bakedBlock.meshes[0];
                return mesh != null;
            }
            
            return false;
        }
        
        
        
        
        
         // Warning Message

        [SerializeField, UsedImplicitly] DisplayMessage warning = new(nameof(WarningMessage), true)
            { messageType = MessageType.Warning, messageSize = MessageSize.Normal};
        
        public string WarningMessage {
            get
            {
                StringBuilder stringBuilder = new ();
                foreach (BlockType blockType in VoxelUtility.AllBlockType)
                {
                    foreach (InVoxelDirection dir in VoxelUtility.AllInVoxelDirection)
                    {
                        if (blockType.HaveAxis())
                        { 
                            foreach (Axis3D axis in VoxelUtility.AllAxis)
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
                    if (!blockDictionary.ContainsKey(key))
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