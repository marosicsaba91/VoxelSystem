using System;
using UnityEngine;
using MUtility; 

namespace VoxelSystem
{
    [ExecuteAlways]
    public partial class VoxelObject : MonoBehaviour
    {
        [Serializable]
        public struct References
        {
            public MeshRenderer meshRenderer;
            public MeshFilter meshFilter;
            public MeshCollider meshCollider;
        }
        public References references = new References();

        [Header("Lock Transform")]
        public bool lockPosition = true;
        public bool lockRotation = true;
        public bool lockScale = true;
        
        // Map, Palette, Builder
        [SerializeField, HideInInspector] internal VoxelMapScriptableObject connectedMap = null;
        [SerializeField, HideInInspector] internal VoxelBuilder connectedBuilder = null;

        [SerializeField, HideInInspector] internal VoxelMap innerMap = null;
        [SerializeField, HideInInspector] internal VoxelBuilder defaultBuilder =null;

        int _meshDirtyCounter = 0;
        VoxelMapScriptableObject _lastFrameConnectedMap;

        void OnValidate()
        {
            MaintainReferences();  
        }

        public bool HasConnectedMap()
        {
            return connectedMap != null;
        }

        public VoxelMapScriptableObject ConnectedMap
        {
            get => connectedMap; 
            set
            {
                if (connectedMap == value) return;
                if (value == null) {
                    innerMap = connectedMap.map.GetCopy();
                }
                else
                {
                    SetMeshDirty();
                }
                connectedMap = value;
            }
        }
        
        public VoxelBuilder ConnectedBuilder
        {
            get => connectedBuilder; 
            set
            {
                if (connectedBuilder == value) return; 
                connectedBuilder = value;
                SetMeshDirty();
            }
        }


        public VoxelBuilder Builder => connectedBuilder != null ? connectedBuilder : defaultBuilder;

        public VoxelMap Map
        {
            get => connectedMap != null ? connectedMap.map : innerMap;
            set
            {
                if (connectedMap != null) { connectedMap.map = value; }
                else if (innerMap != null) { innerMap = value; }
            }
        }
        
        void OnEnable()
        {
            MaintainReferences();
            if (innerMap == null) {
                innerMap = new();
                SetMeshDirty();
            }
            VoxelMap map = Map;
            if (map != null)
                map.MapChangedEvent += SetMeshDirty;
        }

        void OnDisable()
        {
            VoxelMap map = Map;
            if (map != null)
                map.MapChangedEvent -= SetMeshDirty;
        }

        // Update is called once per frame
        void Update()
        {
            if (_lastFrameConnectedMap != connectedMap)
            {
                if (_lastFrameConnectedMap != null) _lastFrameConnectedMap.map.MapChangedEvent -= SetMeshDirty;
                else if (innerMap != null) innerMap.MapChangedEvent -= SetMeshDirty;

                if (connectedMap != null) connectedMap.map.MapChangedEvent += SetMeshDirty;
                else if(innerMap != null) innerMap.MapChangedEvent += SetMeshDirty;
                _lastFrameConnectedMap = connectedMap;
                SetMeshDirty();
            }

            DoLockTransform();
        }

        void DoLockTransform()
        {
            if (lockPosition)
            {
                Vector3 lp = transform.localPosition;
                transform.localPosition = new Vector3(Mathf.RoundToInt(lp.x), Mathf.RoundToInt(lp.y), Mathf.RoundToInt(lp.z));
            }
            if (lockRotation)
            {
                Vector3 lr = transform.localRotation.eulerAngles;
                transform.localRotation = Quaternion.Euler(new Vector3(Mathf.RoundToInt(lr.x / 90f) * 90, Mathf.RoundToInt(lr.y / 90f) * 90, Mathf.RoundToInt(lr.z / 90f) * 90));
            }
            if (lockScale)
            {
                Vector3 ls = transform.localScale;
                transform.localScale = new Vector3(Mathf.RoundToInt(ls.x), Mathf.RoundToInt(ls.y), Mathf.RoundToInt(ls.z));
            }
        }

        void LateUpdate()
        {
            if (_meshDirtyCounter <= 0) return;
            
            RegenerateMesh();
            _meshDirtyCounter = 0;
        }

        public void RegenerateMesh()
        {
            VoxelMap map = Map;
            VoxelBuilder builder = Builder;

            if (map == null || builder == null) { return; }
            Mesh mesh = builder.VoxelModelToMesh(map) ;

            if (mesh == null) { return; }
            MaintainReferences();

            references.meshFilter.mesh = mesh;
            if (references.meshCollider != null) { references.meshCollider.sharedMesh = mesh; }
        }

        void MaintainReferences()
        {
            if (references.meshFilter == null)
                references.meshFilter = GetComponent<MeshFilter>();
            if (references.meshRenderer == null)
                references.meshRenderer = GetComponent<MeshRenderer>();
            if (references.meshCollider == null)                
                references.meshCollider = GetComponent<MeshCollider>(); 

        }

        public void ApplyRotation()
        {
            if (!lockRotation) { return; }
            VoxelMap map = Map;
            if (map == null) { return; }
            if (transform.localRotation == Quaternion.identity) { return; }


                Vector3 transformedOne = transform.TransformDirection(Vector3.one);
            Vector3 transformedSize =
                transform.TransformDirection(Vector3.right).normalized * map.Size.x +
                transform.TransformDirection(Vector3.up).normalized * map.Size.y +
                transform.TransformDirection(Vector3.forward).normalized * map.Size.z;
            Vector3 step = (Vector3.one - transformedOne) / 2f;
            Vector3 move = step.MultiplyAllAxis(transformedSize);
            transform.localPosition += move; 


            int actionCount = 0; // For safety
            Vector3Int rotated;
            do
            {
                Vector3 localRotation = transform.localRotation.eulerAngles;
                rotated = new Vector3Int(
                    Mathf.RoundToInt((localRotation.x % 360) / 90f),
                    Mathf.RoundToInt((localRotation.y % 360) / 90f),
                    Mathf.RoundToInt((localRotation.z % 360) / 90f));

                if (rotated.x != 0)
                {
                    map.Turn(Axis3D.X, leftHandPositive: false);
                    transform.Rotate(Vector3.right, angle: -90);
                    actionCount++;
                    continue;
                }
                if (rotated.y != 0)
                {
                    map.Turn(Axis3D.Y, leftHandPositive: false);
                    transform.Rotate(Vector3.up, angle: -90);
                    actionCount++;
                    continue;
                }
                if (rotated.z != 0)
                {
                    map.Turn(Axis3D.Z, leftHandPositive: false);
                    transform.Rotate(Vector3.forward, angle: -90);
                    actionCount++;
                }
            }
            while (rotated != Vector3Int.zero && actionCount < 100);

        }


        public void ApplyScale()
        {
            if (!lockScale) { return; }
            VoxelMap map = Map;
            if (map == null) { return; }
            if (transform.localScale == Vector3.one) { return; }

                Vector3 move = Vector3.zero;
            move += ApplyScaleOnAxis(map, Axis3D.X);
            move += ApplyScaleOnAxis(map, Axis3D.Y);
            move += ApplyScaleOnAxis(map, Axis3D.Z);
            transform.localScale = Vector3.one;
            transform.position += transform.TransformVector(move);
        }

        Vector3 ApplyScaleOnAxis(VoxelMap map, Axis3D axis ) {
            Transform trans = transform;
            Vector3 localScale = trans.localScale;
            float saleFloat =
                axis == Axis3D.X ? localScale.x :
                axis == Axis3D.Y ? localScale.y :
                localScale.z ;

            int scale = Mathf.RoundToInt(saleFloat);
            int size =
                axis == Axis3D.X ? map.Width :
                axis == Axis3D.Y ? map.Height :
                map.Depth ;
            GeneralDirection3D positiveDir =
                axis == Axis3D.X ? GeneralDirection3D.Right :
                axis == Axis3D.Y ? GeneralDirection3D.Up : 
                GeneralDirection3D.Forward;

            Vector3 move = Vector3.zero;
            if (scale < 0)
            {
                move =
                axis == Axis3D.X ? new Vector3(scale * size, y: 0, z: 0) :
                axis == Axis3D.Y ? new Vector3(x: 0, scale * size, z: 0) :
                new Vector3(x: 0, y: 0, scale*size);

                scale *= -1;
                map.Mirror(axis);
            }
            if (scale != 1 && scale != 0)
            {
                map.Resize(positiveDir, (scale-1) * size, VoxelMap.ResizeType.Rescale);
            }
            return move;
        }


        void SetMeshDirty()
        {
            if (Application.isPlaying)
                _meshDirtyCounter++;
            else
                RegenerateMesh();
        }

        public void FillWholeMap(int paletteIndex)
        {
            Map.FillWhole(paletteIndex); 
        }

        public void ClearWholeMap()
        {
            Map.ClearWhole(); 
        }

        public bool IsValidCoord(Vector3Int coord)
        {
            return Map.IsValidCoord(coord);
        }
    }
}