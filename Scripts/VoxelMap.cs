using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MUtility;

namespace VoxelSystem
{
    [Serializable]
    public class VoxelMap
    {
        const int defaultMapSize = 10;
        
        [SerializeField] int width;
        [SerializeField] int height;
        [SerializeField] int depth;
        [SerializeField] Voxel[] voxelData;
        public event Action MapChangedEvent;

        void MapChanged() =>
            MapChangedEvent?.Invoke();

        public void UndoRedoEvenInvokedOnMap() =>
            MapChangedEvent?.Invoke();
        

        // Constructor -----------------------------------------------------------------------------
        public VoxelMap() 
        {
            Setup(new Vector3Int(defaultMapSize, defaultMapSize, defaultMapSize));
        }

        public VoxelMap(int x, int y, int z)
        {
            Setup(new Vector3Int(x,y,z));
        }

        public VoxelMap(Vector3Int size)
        {
            Setup(size) ;
        }

        void Setup(Vector3Int size) {
            bool isSizeInvalid = size.x <= 0 || size.y <= 0 || size.z <= 0;
            if (isSizeInvalid)
            {
                width = 0;
                height = 0;
                depth = 0;
                voxelData = null;
                return;
            }
            width = size.x;
            height = size.y;
            depth = size.z;
            voxelData = new Voxel[size.x * size.y * size.z];

            for (var i = 0; i < voxelData.Length; i++)
            {
                Vector3Int coord = Index(i);
                bool a = coord.x == 0 || coord.x == size.x - 1;
                bool b = coord.y == 0 || coord.y == size.y - 1;
                bool c = coord.z == 0 || coord.z == size.z - 1;
                bool filled = (a && b) || (b && c) || (c && a);
                voxelData[i] = new Voxel(filled ? 0 : -1);
            }
        }
        


        public VoxelMap(VoxelMap original) // Copy constructor
        {
            if(original == null) { return; }

            width = original.width;
            height = original.height;
            depth = original.depth;
            MapChangedEvent = original.MapChangedEvent;

            if (original.voxelData != null) {
                voxelData = MakeCopy(original.voxelData);
            }
            
            T[] MakeCopy<T>(IReadOnlyList<T> source)
            {
                var copy = new T[source.Count];
                for (var i=0; i< source.Count;i++) {
                    copy[i] = source[i];
                }
                return copy;
            }
        }

        public VoxelMap GetCopy() => new VoxelMap(this);
        

        // GET Voxels ----------------------------

        public int Index(Vector3Int coordinate)
        {
            return Index(coordinate.x, coordinate.y, coordinate.z);
        }

        public int Index(int x, int y, int z)
        {
            return x + (y * width) + (z * width * height);
        }
        
        Vector3Int Index(int i)
        {
            int z = i / (width * height);
            i -= z * width * height;

            int y = i / width;
            i -= y * width;

            int x = i;

            return new Vector3Int(x, y, z);
        }

        public int GetHightestMaterialIndex() => voxelData.Select(v => v.value).Prepend(-1).Max();

        // GET Voxels ----------------------------

        public Voxel Get(Vector3Int coordinate)
        {
            return voxelData[Index(coordinate)]; 
        }
        public Voxel Get(int x, int y, int z)
        {
            return voxelData[Index(x, y, z)];
        }
        public Voxel GetFast(int x, int y, int z, int w, int h)
        {
            return voxelData[x + (y * w) + (z * w * h)];
        }
        
        public bool IsFilledSafe(Vector3Int index)
        {
            return index.x >= 0 && index.x < width &&
                   index.y >= 0 && index.y < height &&
                   index.z >= 0 && index.z < depth &&
                   Get(index.x, index.y, index.z).IsFilled;
        }

        // SET Voxels ----------------------------

        public enum VoxelAreaAction { Fill, Clear, Repaint }

        public bool Set(int x, int y, int z, VoxelAreaAction action, int materialIndex)
        {
            if(!IsValidCoord(x, z, z)){ return false; }
            Voxel v = voxelData[Index(x, y, z)];
            Voxel oldV = v;

            if (action == VoxelAreaAction.Repaint)
            {
                if (oldV.IsFilled) { v.value = materialIndex; }
            }
            else if (action == VoxelAreaAction.Fill) {
                if (v.IsEmpty) { v.value = materialIndex; }
            }
            else if(action == VoxelAreaAction.Clear) {
                v.Clear();
            } 

            voxelData[Index(x, y, z)] = v;

            bool changed = (!oldV.Equals(v));
            if (changed) { MapChanged(); }
            return changed;
        }

        public bool Set(Vector3Int coordinate, VoxelAreaAction action, int materialIndex)
        {
            return Set(coordinate.x, coordinate.y, coordinate.z, action, materialIndex);
        }

        public bool Set(Vector3Int coordinate, int materialIndex)
        {
            return Set(coordinate.x, coordinate.y, coordinate.z, VoxelAreaAction.Fill, materialIndex);
        }

        public bool Set(int x, int y, int z, int materialIndex)
        {
            return Set(x, y, z, VoxelAreaAction.Fill, materialIndex);
        }
        
        public bool SetValueOf(Vector3Int coordinate, int materialIndex)
        {
            return Set(coordinate.x, coordinate.y, coordinate.z, VoxelAreaAction.Repaint, materialIndex);
        }

        public bool SetValueOf(int x, int y, int z, int materialIndex)
        {
            return Set(x, y, z, VoxelAreaAction.Repaint, materialIndex);
        }

        public bool ClearVoxel(Vector3Int coordinate)
        {
            return ClearVoxel(coordinate.x, coordinate.y, coordinate.z);
        }
        
        public bool ClearVoxel(int x, int y, int z)
        {
            return Set(x, y, z, VoxelAreaAction.Clear, materialIndex: 0);
        }

        public void ClearWhole() {
            for (var i = 0; i < voxelData.Length; i++)
            {
                voxelData[i] = new Voxel(materialIndex: -1);
            }
            MapChanged();
        }

        public void FillWhole(int materialIndex)
        {
            for (var i = 0; i < voxelData.Length; i++)
            {
                voxelData[i] = new Voxel(materialIndex);
            }
            MapChanged();
        }

        public void FillRange(Vector3Int startCoordinate, Vector3Int endCoordinate, int value) {
            SetRange(startCoordinate, endCoordinate, VoxelAreaAction.Fill,  value);
        }

        public void ClearRange(Vector3Int startCoordinate, Vector3Int endCoordinate)
        {
            SetRange(startCoordinate, endCoordinate, VoxelAreaAction.Clear, value: 0);
        }

        public void SetValueOfRange(Vector3Int startCoordinate, Vector3Int endCoordinate, int value)
        {
            SetRange(startCoordinate, endCoordinate, VoxelAreaAction.Repaint, value: 0);
        }

        public void SetRange(Vector3Int startCoordinate, Vector3Int endCoordinate, VoxelAreaAction action, int value)
        {
            // This code need to be highly optimized;
            // this is why its not clean

            Vector3Int size = Size;
            int minX = Mathf.Max(a: 0, Mathf.Min(startCoordinate.x, endCoordinate.x, size.x));
            int minY = Mathf.Max(a: 0, Mathf.Min(startCoordinate.y, endCoordinate.y, size.y));
            int minZ = Mathf.Max(a: 0, Mathf.Min(startCoordinate.z, endCoordinate.z, size.z));
            int maxX = Mathf.Min(size.x - 1, Mathf.Max(startCoordinate.x, endCoordinate.x, 0));
            int maxY = Mathf.Min(size.y - 1, Mathf.Max(startCoordinate.y, endCoordinate.y, 0));
            int maxZ = Mathf.Min(size.z - 1, Mathf.Max(startCoordinate.z, endCoordinate.z, 0));
            
            if (action == VoxelAreaAction.Repaint)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    for (int y = minY; y <= maxY; y++)
                    {
                        for (int z = minZ; z <= maxZ; z++)
                        {
                            int index = x + (y * size.x) + (z * size.x * size.y);
                            if (voxelData[index].IsFilled) { voxelData[index].value = value; }
                        }
                    }
                }
            }
            else if (action == VoxelAreaAction.Fill)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    for (int y = minY; y <= maxY; y++)
                    {
                        for (int z = minZ; z <= maxZ; z++)
                        {
                            int index = x + (y * size.x) + (z * size.x * size.y);
                            if (voxelData[index].IsEmpty) { voxelData[index].value = value; }
                        }
                    }
                }
            }
            else if (action == VoxelAreaAction.Clear)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    for (int y = minY; y <= maxY; y++)
                    {
                        for (int z = minZ; z <= maxZ; z++)
                        {
                            int index = x + (y * size.x) + (z * size.x * size.y);
                            if (voxelData[index].IsFilled) { voxelData[index].value = -1; }
                        }
                    }
                }
            }
            
            MapChanged();
        }

        public void CopyFromOtherMap(VoxelMap sourceMap, Vector3Int startCoordinateOfSourceMap, Vector3Int startCoordinateOfDestinationMap, Vector3Int copySize)
        {
            for (var x = 0; x < copySize.x; x++)
            {
                int destinationX  = startCoordinateOfDestinationMap.x + x;
                int sourceX = startCoordinateOfSourceMap.x + x;
                if (destinationX >= width || destinationX < 0) { continue; }
                if (sourceX >= sourceMap.width || sourceX < 0) { continue; }

                for (var y = 0; y < copySize.y; y++)
                {
                    int destinationY = startCoordinateOfDestinationMap.y + y;
                    int sourceY = startCoordinateOfSourceMap.y + y;
                    if (destinationY >= height || destinationY < 0) { continue; }
                    if (sourceY >= sourceMap.height || sourceY < 0) { continue; }

                    for (var z = 0; z < copySize.z; z++)
                    {
                        int destinationZ = startCoordinateOfDestinationMap.z + z;
                        int sourceZ = startCoordinateOfSourceMap.z + z;
                        if (destinationZ >= depth || destinationZ < 0) { continue; }
                        if (sourceZ >= sourceMap.depth || sourceZ < 0) { continue; }

                        // COPY VOXEL
                         
                        int val = sourceMap.Get(sourceX, sourceY, sourceZ).value; 
                        if (val >= 0)
                        {                            
                            Set(destinationX, destinationY, destinationZ, val);
                        }
                    }
                }
            }
        }

        // Transform --------------------------------------------

        public void Turn(Axis3D axis, bool leftHandPositive)
        {
            int newW =
                axis == Axis3D.X ? Width :
                axis == Axis3D.Y ? Depth :
                axis == Axis3D.Z ? Height : 0;
            int newH=
                axis == Axis3D.X ? Depth :
                axis == Axis3D.Y ? Height :
                axis == Axis3D.Z ? Width : 0;
            int newD =
                axis == Axis3D.X ? Height :
                axis == Axis3D.Y ? Width :
                axis == Axis3D.Z ? Depth : 0;

            var newVoxelData = new Voxel[voxelData.Length];

            for (var i = 0; i < voxelData.Length; i++) {
                Vector3Int original = Index(i);
                int nx =
                    axis == Axis3D.X ? original.x :
                    axis == Axis3D.Y ? (leftHandPositive ? depth - original.z - 1 : original.z) :     
                    axis == Axis3D.Z ? (leftHandPositive ? original.y : height -original.y - 1) : 0;
                int ny =
                    axis == Axis3D.X ?(leftHandPositive ? original.z : depth -original.z - 1) :
                    axis == Axis3D.Y ? original.y :
                    axis == Axis3D.Z ? (leftHandPositive ? width - original.x - 1 : original.x) : 0;
                int nz =
                    axis == Axis3D.X ? (leftHandPositive ? height - original.y - 1 : original.y) :
                    axis == Axis3D.Y ? (leftHandPositive ? original.x : width - original.x - 1) :
                    axis == Axis3D.Z ? original.z : 0;

                //Debug.Log(original+"  "+new Vector3Int(nx,ny,nz));
                int ni = nx + (ny * newW) + (nz * newW * newH);
                newVoxelData[ni] = voxelData[i];
            }

            width = newW;
            height = newH;
            depth = newD;
            voxelData = newVoxelData;
            MapChanged();
        }

        public void Mirror(Axis3D axis)
        {
            var newVoxelData = new Voxel[voxelData.Length];

            for (var i = 0; i < voxelData.Length; i++)
            {
                Vector3Int o = Index(i);
                if (axis == Axis3D.X) o.x = width - o.x - 1;
                if (axis == Axis3D.Y) o.y = height - o.y - 1;
                if (axis == Axis3D.Z) o.z = depth - o.z - 1;
                int ni = o.x + (o.y * width) + (o.z * width * height);

                newVoxelData[ni] = voxelData[i];
            }
            voxelData = newVoxelData;
            MapChanged();
        }

        public enum ResizeType { Resize, Repeat, Rescale}
        public void Resize(GeneralDirection3D direction, int steps, ResizeType type)
        {
            Axis3D axis = direction.GetAxis();
            int newW = (axis == Axis3D.X) ? Math.Max(val1: 1, width + steps) : width;
            int newH = (axis == Axis3D.Y) ? Math.Max(val1: 1, height + steps) : height;
            int newD = (axis == Axis3D.Z) ? Math.Max(val1: 1, depth + steps) : depth;

            var newVoxelData = new Voxel[newW * newH * newD];
            
            for (var i = 0; i < newVoxelData.Length; i++)
            {
                int oldIndex;
                if (type == ResizeType.Rescale)
                {
                    int nx = i;
                    int nz = nx / (newW * newH);
                    nx -= nz * (newW * newH);
                    int ny = nx / newW;
                    nx -= ny * (newW);
                    int ox = Mathf.Clamp((int)((float)nx / newW * width), min: 0, width-1);
                    int oy = Mathf.Clamp((int)((float)ny / newH * height), min: 0, height-1);
                    int oz = Mathf.Clamp((int)((float)nz / newD * depth), min: 0, depth-1);
                    oldIndex = Index(ox, oy, oz);
                    if (oldIndex < 0 || oldIndex >= voxelData.Length)
                    {
                        Debug.Log("W: " + width + " -> " + newW);
                        Debug.Log("X: " + ox + " -> " + nx);

                        Debug.Log("H: " + height + " -> " + newH);
                        Debug.Log("Y: " + oy + " -> " + ny);

                        Debug.Log("D: " + depth + " -> " + newD);
                        Debug.Log("Z: " + oz + " -> " + nz);
                    }
                }
                else
                {
                    int ox = i;
                    int oz = ox / (newW * newH);
                    ox -= oz * (newW * newH);
                    int oy = ox / newW;
                    ox -= oy * (newW);

                    if (direction == GeneralDirection3D.Left) { ox -= (newW - width); }
                    if (direction == GeneralDirection3D.Down) { oy -= (newH - height); }
                    if (direction == GeneralDirection3D.Back) { oz -= (newD - depth); }

                    if (ox >= width || oy >= height || oz >= depth || ox < 0 || oy < 0 || oz < 0)
                    {
                        if (type == ResizeType.Repeat)
                        {
                            oldIndex = Index(MathHelper.Mod(ox, width), MathHelper.Mod(oy, height), MathHelper.Mod(oz, depth));
                        }
                        else { oldIndex = -1; }
                    }
                    else
                    {
                        oldIndex = Index(ox, oy, oz);
                    }
                }

                if (oldIndex < 0)
                    newVoxelData[i] = new Voxel(materialIndex: -1);
                else
                    newVoxelData[i] = voxelData[oldIndex];
            }

            width = newW;
            height = newH;
            depth = newD;
            voxelData = newVoxelData;
            MapChanged();
        }

        // Voxel Map Info --------------------------------------------



        public Vector3Int Size => new (width, height, depth);

        public int GetSize(Axis3D a){
            if (a == Axis3D.X) return width;
            if (a == Axis3D.Y) return height;
            return depth;
        }


        public int Width => width;
        public int Height => height;
        public int Depth => depth;

        public bool IsValidCoord(int x, int y, int z)
        {
            if (voxelData == null) { return false; }
            return
                x >= 0 && x < width &&
                y >= 0 && y < height &&
                z >= 0 && z < depth;
        }

        public bool IsValidCoord(Vector3Int coord) => IsValidCoord(coord.x, coord.y, coord.z);
    }
}