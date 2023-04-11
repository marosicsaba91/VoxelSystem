using System.Collections.Generic;
using UnityEngine;

namespace VoxelSystem
{
    [ExecuteAlways]
    public class BlockLibraryComponent : MonoBehaviour
    {
        [SerializeField] Material material;

        
        [SerializeField] DefaultBlockSetup defaultBlockSetup;
        public bool setUpProcess = false;

        static BlockLibraryComponent()
        {
            _setupDictionary = new Dictionary<BlockType, List<BlockSetup>>();
            foreach (BlockType bt in BlockUtility.allBlockType)
                _setupDictionary.Add(bt, new List<BlockSetup>());
        }

        static readonly Dictionary<BlockType, List<BlockSetup>> _setupDictionary;

        void Update()
        {
            if (Application.isPlaying) return;

            var blocks = GetComponentsInChildren<BlockComponent>();
            var setups = GetComponentsInChildren<BlockSetup>();
            
            foreach (var kvp in _setupDictionary)
                kvp.Value.Clear();

            foreach (BlockSetup setup in setups)
                _setupDictionary[setup.blockType].Add(setup);

            foreach (BlockComponent block in blocks)
                SetupBlock(block, _setupDictionary[block.blockType]);

            
            // SETUP PROCESS: ONLY ONE TIME
            if (setUpProcess)
            {
                foreach (BlockComponent block in blocks)
                {
                    block.Setup();
                    defaultBlockSetup.AddBlock(block);
                }

            }
        }

        void SetupBlock(BlockComponent block, List<BlockSetup> setups)
        {
            BlockSetup setup = setups.Count > 0 ? setups[0] : null;

            bool haveSetup = setup!=null;
            bool useSetup = haveSetup && setup != null;

            // if(haveSetup)
            //    Debug.Log(blockType);
            
            block.meshRenderer.material =
                useSetup && material != null ? material :
                useSetup && material == null ? defaultBlockSetup.GetSelectedMaterial() :
                haveSetup ? defaultBlockSetup.GetSelectableMaterial() :
                defaultBlockSetup.GetBasicMaterial();

            block.meshFilter.mesh =
                useSetup && setup.mesh != null ? setup.mesh : defaultBlockSetup.GetMesh(block.blockType);
        }
    }
}