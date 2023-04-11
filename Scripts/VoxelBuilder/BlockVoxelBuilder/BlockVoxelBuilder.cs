using System;
using System.Collections.Generic;
using System.Linq;
using MUtility;
using UnityEngine;
using Utility.SerializableCollection;

namespace VoxelSystem
{
    [Serializable] class BlockColorDictionary : SerializableDictionary<BlockType, Color>{ }

    [Serializable]
    class BlockDrawingSettings
    {
        public bool drawVoxels = true;
        public Color voxelGizmoColor = new (1, 1, 1, 0.25f);
        public bool drawBlocks = true;
        public BlockColorDictionary blockColors = new();
        [Range (0,0.25f)] public float margin = 0.1f;
    }


    [CreateAssetMenu(fileName = "BlockVoxelBuilder", menuName = "VoxelSystem/BlockVoxelBuilder", order = 4)]
    public class BlockVoxelBuilder : VoxelBuilder
    {
        [SerializeField] BlockDrawingSettings drawingSettings = new();
        [SerializeField] List<BlockLibrary_Legcy> blockLibraries;

        List<Block> _blocks;
        
        protected override void BuildMesh(VoxelMap voxelMap, List<Vector3> vertices, List<Vector3> normals, List<Vector2> uv, List<int> triangles)
        {
            _blocks ??= new List<Block>();
            CalculateBlocks(voxelMap);
            foreach (Block block in _blocks)
            {
                int selected = VoxelEditorWindow.SelectedPaletteIndex;
                selected = Mathf.Clamp(selected, 0, blockLibraries.Count - 1);
                BlockLibrary_Legcy blockLibraryLegcy = blockLibraries[selected];
                
                if (blockLibraryLegcy.TryGetMesh(block, out Mesh mesh))
                {
                    Vector3 offset = block.Center;
                    vertices.AddRange(mesh.vertices.Select(v => v + offset));
                    normals.AddRange(mesh.normals);
                    uv.AddRange(mesh.uv);
                    triangles.AddRange(mesh.triangles.Select(t => t + vertices.Count - mesh.vertices.Length));
                }
            }
        }

        void CalculateBlocks(VoxelMap voxelMap)
        {
            _blocks.Clear();

            for (int x = voxelMap.Width - 1; x >= 0; x--)
            for (int y = voxelMap.Height - 1; y >= 0; y--)
            for (int z = voxelMap.Depth - 1; z >= 0; z--)
                _blocks.AddRange(VoxelToBlocks(voxelMap, x, y, z));
        }

        public override IEnumerable<PaletteItem> GetPaletteItems()
        {
            for (var index = 0; index < blockLibraries.Count; index++)
            {
                BlockLibrary_Legcy blockLibraryLegcy = blockLibraries[index];
                yield return new PaletteItem { value = index, name = blockLibraryLegcy.name, color = blockLibraryLegcy.color };
            }
        }

        public override int PaletteLength => blockLibraries.Count;


        public override void DrawGizmos(VoxelMap map)
        {
            // Draw whole voxel map
            if (drawingSettings.drawVoxels)
            {
                Gizmos.color = drawingSettings.voxelGizmoColor;
                for (int x = map.Width - 1; x >= 0; x--)
                for (int y = map.Height - 1; y >= 0; y--)
                for (int z = map.Depth - 1; z >= 0; z--)
                {
                    if (map.Get(x, y, z).IsFilled)
                        Gizmos.DrawWireCube(new Vector3(x + 0.5f, y + 0.5f, z + 0.5f), Vector3.one);
                }
            }

            // Draw Blocks
            if (drawingSettings.drawBlocks)
            {
                if (_blocks == null)
                {
                    _blocks = new List<Block>();
                    CalculateBlocks(map);
                }
                
                foreach (Block block in _blocks)
                {
                    Gizmos.color = drawingSettings.blockColors.TryGetValue(block.blockType, out Color color)
                        ? color
                        : Color.magenta;
                    block.DrawGizmo(drawingSettings.margin);
                }
            }
        }

        IEnumerable<Block> VoxelToBlocks(VoxelMap voxelMap, int vXi, int vYi, int vZi)   // Voxel Index
        { 
            Voxel voxel = voxelMap.Get(vXi, vYi, vZi);
            bool vf = voxel.IsFilled; // Is Voxel Filled
            Vector3Int voxelIndex = new Vector3Int(vXi, vYi, vZi);
            
            Vector3Int blockSize = Vector3Int.one;

            for (var dX = -1; dX <= 1; dX += 2) // In Voxel Direction
            for (var dY = -1; dY <= 1; dY += 2)
            for (var dZ = -1; dZ <= 1; dZ += 2)
            {
                int nXi = vXi + dX; // Neighbour Voxel Index
                int nYi = vYi + dY;
                int nZi = vZi + dZ;
                Vector3Int inVoxelDir = new (dX, dY, dZ);
                
                var blockPosition = new Vector3(
                    vXi + (dX + 1) * 0.25f,
                    vYi + (dY + 1) * 0.25f,
                    vZi + (dZ + 1) * 0.25f);

                bool nXe = nXi >= 0 && nXi < voxelMap.Width; // Do Neighbour Row Exist
                bool nYe = nYi >= 0 && nYi < voxelMap.Height;
                bool nZe = nZi >= 0 && nZi < voxelMap.Depth;

                bool nXf = nXe && voxelMap.Get(nXi, vYi, vZi).IsFilled; // Is Neighbour Filled
                bool nYf = nYe && voxelMap.Get(vXi, nYi, vZi).IsFilled;
                bool nZf = nZe && voxelMap.Get(vXi, vYi, nZi).IsFilled;

                // ---------------------------------------------------------------------------------------------
                
                int neighbourCount = (nXf ? 1 : 0) + (nYf ? 1 : 0) + (nZf ? 1 : 0);

                var normal = new Vector3Int(); 

                if (vf)
                {
                    normal.x = nXf ? 0 : dX;
                    normal.y = nYf ? 0 : dY;
                    normal.z = nZf ? 0 : dZ;

                    if (neighbourCount == 2) // SIDE
                    {
                        if (TestSide(voxelMap, voxelIndex, normal, inVoxelDir))
                            yield return new Block(BlockType.SidePositive, inVoxelDir, blockSize, normal, blockPosition);

                    }
                    else if (neighbourCount == 1)
                    {
                        if (TestEdge(voxelMap, voxelIndex, normal, inVoxelDir))
                            yield return new Block(BlockType.EdgePositive, inVoxelDir, blockSize, normal, blockPosition);
                    }
                    else if (neighbourCount == 0)
                    {
                        yield return new Block(BlockType.CornerPositive, inVoxelDir, blockSize, normal, blockPosition);
                    }
                }
                else
                {
                    normal.x = !nXf ? 0 : -dX;
                    normal.y = !nYf ? 0 : -dY;
                    normal.z = !nZf ? 0 : -dZ;

                    if (neighbourCount == 3)
                    {
                        yield return new Block(BlockType.CornerNegative, inVoxelDir, blockSize, normal, blockPosition);
                    }
                    else if (neighbourCount == 2)
                    {
                        yield return new Block(BlockType.EdgeNegative, inVoxelDir, blockSize, normal, blockPosition); 
                    }
                }
            }
        }
        
        bool TestSide(VoxelMap voxelMap, Vector3Int voxelIndex, Vector3Int normal, Vector3Int inVoxelDir)
        {
            bool diagonalFilled = voxelMap.IsFilledSafe(voxelIndex + inVoxelDir);
            if (diagonalFilled)
                return false;

            Vector3Int midDir = new(
                normal.x == 0 ? inVoxelDir.x : 0,
                normal.y == 0 ? inVoxelDir.y : 0,
                normal.z == 0 ? inVoxelDir.z : 0);

            bool midFilled = voxelMap.IsFilledSafe(voxelIndex + midDir);
            if (!midFilled)
                return false;

            SeparateDirection(midDir, out Vector3Int leftDir, out Vector3Int rightDir);
             
            bool leftUpFilled = voxelMap.IsFilledSafe(voxelIndex + leftDir + normal);
            if (leftUpFilled)
                return false; 
            
            bool rightUpFilled = voxelMap.IsFilledSafe(voxelIndex + rightDir + normal);
            if (rightUpFilled)
                return false;
            
            return true;
        }
        
        bool TestEdge(VoxelMap voxelMap, Vector3Int voxelIndex, Vector3Int normal, Vector3Int inVoxelDir)
        {
            SeparateDirection(normal, out Vector3Int side1Dir, out Vector3Int side2Dir);
            Vector3Int forwardDir = inVoxelDir - normal;
            
            bool side1ForwardFilled = voxelMap.IsFilledSafe(voxelIndex + side1Dir + forwardDir);
            if (side1ForwardFilled)
                return false;
            
            bool side2ForwardFilled = voxelMap.IsFilledSafe(voxelIndex + side2Dir + forwardDir);
            if (side2ForwardFilled)
                return false;
            
            
            return true;
        }
        
        void SeparateDirection(Vector3Int dir, out Vector3Int a, out Vector3Int b)
        {
            a = dir.x == 0 ? dir.MultiplyAllAxis(0, 1, 0) : dir.MultiplyAllAxis(1, 0, 0);
            b = dir.y == 0 ? dir.MultiplyAllAxis(0, 0, 1) : dir.MultiplyAllAxis(0, 1, 0);
        }
        
        void SeparateDirection(Vector3Int dir, out Vector3Int a, out Vector3Int b, out Vector3Int c)
        {
            a = dir.x == 0 ? dir.MultiplyAllAxis(0, 1, 0) : dir.MultiplyAllAxis(1, 0, 0);
            b = dir.y == 0 ? dir.MultiplyAllAxis(0, 0, 1) : dir.MultiplyAllAxis(0, 1, 0);
            c = dir.z == 0 ? dir.MultiplyAllAxis(1, 0, 0) : dir.MultiplyAllAxis(0, 0, 1);
        }
    }
}