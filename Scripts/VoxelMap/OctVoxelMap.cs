using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MUtility;
using Newtonsoft.Json.Linq;

namespace VoxelSystem
{
    [Serializable]
    public class OctVoxelMap
    {
        const int defaultCanvasSize = 8;


        [SerializeField] Vector3Int canvasSize;
        [SerializeField] int levels;
        [SerializeField] List<OctVoxel> voxelData;

        // If the first element is -1, the whole map is empty 
        // If the first element >= 0, the whole map fully filled with a the same material
        // If the first element is -2, the whole map is partially filled or with different materials
        // In this case, the map is divided into 8 submaps, each of which is checked recursively


        // Actions --------------------------------------------

        public event Action MapChangedEvent;

        void MapChanged() =>
            MapChangedEvent?.Invoke();

        public void UndoRedoEvenInvokedOnMap() =>
            MapChangedEvent?.Invoke();


        // Voxel Map Info --------------------------------------------

        int RealSize
        {
            get
            {
                int s = 1;
                for (int i = 0; i < levels; i++)
                {
                    s *= 2;
                }
                return s;
            }
        }

        public Vector3Int Size => canvasSize;

        public int GetSize(Axis3D a)
        {
            if (a == Axis3D.X) return canvasSize.x;
            if (a == Axis3D.Y) return canvasSize.y;
            return canvasSize.z;
        }


        public int Width => canvasSize.x;
        public int Height => canvasSize.y;
        public int Depth => canvasSize.z;

        public bool IsValidCoord(int x, int y, int z)
        {
            if (voxelData == null) { return false; }
            return
                x >= 0 && x < canvasSize.x &&
                y >= 0 && y < canvasSize.y &&
                z >= 0 && z < canvasSize.z;
        }

        public bool IsValidCoord(Vector3Int coord) => IsValidCoord(coord.x, coord.y, coord.z);

        // Constructor -----------------------------------------------------------------------------

        public OctVoxelMap()
        {
            Setup(new Vector3Int(defaultCanvasSize, defaultCanvasSize, defaultCanvasSize));
        }

        public OctVoxelMap(int x, int y, int z)
        {
            Setup(new Vector3Int(x, y, z));
        }

        public OctVoxelMap(Vector3Int canvasSize)
        {
            Setup(canvasSize);
        }

        void Setup(Vector3Int canvasSize)
        {

            bool isSizeInvalid = canvasSize.x <= 0 || canvasSize.y <= 0 || canvasSize.z <= 0;
            if (isSizeInvalid)
            {
                this.canvasSize = Vector3Int.zero;
                voxelData = null;
                return;
            }

            int longestCanvasSize = Mathf.Max(canvasSize.x, canvasSize.y, canvasSize.z);
            this.levels = Mathf.CeilToInt(Mathf.Log(longestCanvasSize, 2));


            this.canvasSize = canvasSize;
            voxelData = new List<OctVoxel>();
            voxelData.Add(new OctVoxel(-1)); // Only 1 fully empty element
        }



        public OctVoxelMap(OctVoxelMap original) // Copy constructor
        {
            if (original == null) { return; }

            canvasSize = original.canvasSize;

            MapChangedEvent = original.MapChangedEvent;

            if (original.voxelData != null)
            {
                voxelData = MakeCopy(original.voxelData);
            }

            List<T> MakeCopy<T>(IReadOnlyList<T> source)
            {
                var copy = new List<T>();
                for (var i = 0; i < source.Count; i++)
                {
                    copy[i] = source[i];
                }
                return copy;
            }
        }

        public OctVoxelMap GetCopy() => new OctVoxelMap(this);

        SubVoxel GetSubChunk(ref int x, ref int y, ref int z, int size)
        {
            if (x > size / 2)
            {
                x = size / 2;
                if (y > size / 2)
                {
                    y = size / 2;
                    if (z > size / 2)
                    {
                        z = size / 2;
                        return SubVoxel.RightUpForward;
                    }
                    return SubVoxel.RightUpBackward;
                }

                if (z > size / 2)
                {
                    z = size / 2;
                    return SubVoxel.RightDownForward;
                }
                return SubVoxel.RightDownBackward;
            }

            if (y > size / 2)
            {
                y = size / 2;
                if (z > size / 2)
                {
                    z = size / 2;
                    return SubVoxel.LeftUpForward;
                }
                return SubVoxel.LeftUpBackward;
            }

            if (z > size / 2)
            {
                z = size / 2;
                return SubVoxel.LeftDownForward;
            }
            return SubVoxel.LeftDownBackward;


        }


        // GET Voxels ----------------------------

        public int GetHightestMaterialIndex() => voxelData.Select(v => v.Value).Prepend(-1).Max();

        // GET Voxels --------------------------------------------------------
         

        public OctVoxel Get(int x, int y, int z)
        {
            int currentIndex = 0;
            int size = RealSize;

            for (int level = levels; level >= 0; level--)
            {
                OctVoxel chunk = voxelData[currentIndex]; 

                if (chunk.IsMixed)
                {
                    SubVoxel subVoxel = GetSubChunk(ref x, ref y, ref z, size);
                    int subIndex = (int)subVoxel;
                    currentIndex++;
                    for (int i = 0; i < subIndex; i++)
                    {
                        currentIndex += chunk.ChunkSize;
                    }
                }
                else
                    return chunk;
            }
            throw new ArgumentException("Something is not right.");
        }

        public bool IsFilledSafe(Vector3Int index) => index.x >= 0 && index.x < canvasSize.x &&
                   index.y >= 0 && index.y < canvasSize.y &&
                   index.z >= 0 && index.z < canvasSize.z &&
                   Get(index.x, index.y, index.z).IsFilled;

        // SET Voxels --------------------------------------------------------

        public enum VoxelAreaAction { Fill, Clear, Repaint }

        static List<int> _chunkCache = new List<int>();

        bool SetUnsafe(int x, int y, int z, OctVoxel newValue)
        { 
            int currentIndex = 0;
            int size = RealSize;

            _chunkCache.Clear();

            for (int level = levels; level >= 0; level--)
            {
                OctVoxel chunk = voxelData[currentIndex];
                _chunkCache.Add(currentIndex);

                if (chunk.IsMixed) 
                {
                    SubVoxel subVoxel = GetSubChunk(ref x, ref y, ref z, size);
                    int subIndex = (int) subVoxel;
                    currentIndex++;
                    for (int i = 0; i < subIndex; i++)
                    {
                        currentIndex += chunk.ChunkSize;
                    }
                }
                else // Is Not Mixed
                {
                    if (chunk.Value == newValue.Value) { return false; }

                    // New element
                    for (int i = 0; i < _chunkCache.Count; i++)
                    {
                        int chunkIndex = _chunkCache[i];
                        OctVoxel upperChunk = voxelData[chunkIndex];
                        upperChunk.ChunkSize += 8;
                        voxelData[chunkIndex] = upperChunk;
                    }

                    voxelData.InsertRange(currentIndex + 1, Enumerable.Repeat(new OctVoxel(chunk.Value), 8));
                } 
            }

            return true;
        }

        public bool Set(int x, int y, int z, VoxelAreaAction action, int materialIndex)
        {
            if (!IsValidCoord(x, z, z)) { return false; }
            OctVoxel v = Get(x, y, z);
            OctVoxel oldV = v;

            if (action == VoxelAreaAction.Repaint)
            {
                if (oldV.IsFilled) { v.Value = materialIndex; }
            }
            else if (action == VoxelAreaAction.Fill)
            {
                if (v.IsEmpty) { v.Value = materialIndex; }
            }
            else if (action == VoxelAreaAction.Clear)
            {
                v.Clear();
            }
             
            bool changed = SetUnsafe(x, y, z, v);
            if (changed) { MapChanged(); }
            return changed;
        }

        public bool Set(Vector3Int coordinate, VoxelAreaAction action, int materialIndex) => Set(coordinate.x, coordinate.y, coordinate.z, action, materialIndex);

        public bool Set(Vector3Int coordinate, int materialIndex) => Set(coordinate.x, coordinate.y, coordinate.z, VoxelAreaAction.Fill, materialIndex);

        public bool Set(int x, int y, int z, int materialIndex) => Set(x, y, z, VoxelAreaAction.Fill, materialIndex);

        public bool SetValueOf(Vector3Int coordinate, int materialIndex) => Set(coordinate.x, coordinate.y, coordinate.z, VoxelAreaAction.Repaint, materialIndex);

        public bool SetValueOf(int x, int y, int z, int materialIndex) => Set(x, y, z, VoxelAreaAction.Repaint, materialIndex);

        public bool ClearVoxel(Vector3Int coordinate) => ClearVoxel(coordinate.x, coordinate.y, coordinate.z);

        public bool ClearVoxel(int x, int y, int z) => Set(x, y, z, VoxelAreaAction.Clear, materialIndex: 0);

        public void ClearWhole()
        {
            for (var i = 0; i < voxelData.Count; i++)
            {
                voxelData[i] = new OctVoxel(materialIndex: -1);
            }
            MapChanged();
        }

        public void FillWhole(int materialIndex)
        {
            for (var i = 0; i < voxelData.Count; i++)
            {
                voxelData[i] = new OctVoxel(materialIndex);
            }
            MapChanged();
        }

        public void FillRange(Vector3Int startCoordinate, Vector3Int endCoordinate, int value) => SetRange(startCoordinate, endCoordinate, VoxelAreaAction.Fill, value);

        public void ClearRange(Vector3Int startCoordinate, Vector3Int endCoordinate) => SetRange(startCoordinate, endCoordinate, VoxelAreaAction.Clear, value: 0);

        public void SetValueOfRange(Vector3Int startCoordinate, Vector3Int endCoordinate, int value) => SetRange(startCoordinate, endCoordinate, VoxelAreaAction.Repaint, value: 0);

        public void SetRange(Vector3Int startCoordinate, Vector3Int endCoordinate, VoxelAreaAction action, int value)
        {
            
        }

        internal void CopyFromOtherMap(OctVoxelMap sourceMap, Vector3Int startCoordinateOfSourceMap, Vector3Int startCoordinateOfDestinationMap, Vector3Int copySize)
        {

        }

        // Transform --------------------------------------------

        public void Turn(Axis3D axis, bool leftHandPositive) => MapChanged();

        public void Mirror(Axis3D axis) => MapChanged();

        public enum ResizeType { Resize, Repeat, Rescale }
        public void Resize(GeneralDirection3D direction, int steps, ResizeType type) => MapChanged();
    }
}