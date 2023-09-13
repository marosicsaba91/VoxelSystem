using MUtility;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.EditorCoroutines.Editor;
using UnityEngine;
using UnityEngine.Rendering;

namespace VoxelSystem
{
	enum ChangeOn { Never, OnQuickChange, OnFinalChange, EveryChange }

	public abstract class VoxelMeshGenerator : MonoBehaviour
	{
		internal abstract int VoxelPaletteLength { get; }
		internal abstract IEnumerable<IVoxelPaletteItem> voxelPaletteItems { get; }

		internal abstract VoxelMeshGenerator CreateACopy(GameObject newGO);
		public abstract void RegenerateMeshes();
	}

	public abstract class VoxelMeshGenerator<TVoxelPalette, TPaletteItem> : VoxelMeshGenerator where TVoxelPalette : IVoxelPalette<TPaletteItem>
	{
		[SerializeField, HideInInspector] protected VoxelObject voxelFilter;
		[SerializeField] protected TVoxelPalette voxelPalette;

		[SerializeField] protected Mesh destinationMesh;
		[SerializeField] protected MeshFilter destinationMeshFilter;
		[SerializeField] protected MeshCollider destinationMeshCollider;

		[SerializeField] ChangeOn autoRegenerateMeshes = ChangeOn.Never;
		[SerializeField, Min(0)] float regenDelay;
		[SerializeField] DisplayMember regenerateMesh = new(nameof(RegenerateMeshes));
		[SerializeField] DisplayMember createMeshFile = new(nameof(CreateMeshFile));

		[SerializeField] protected bool doBenchmark;
		[SerializeField] protected TMP_Text benchmarkOutput;


		protected VoxelMap Map => voxelFilter == null ? null : voxelFilter.GetVoxelMap();

		internal sealed override int VoxelPaletteLength => voxelPalette == null ? 1 : voxelPalette.Length;
		internal sealed override IEnumerable<IVoxelPaletteItem> voxelPaletteItems
		{
			get
			{
				if (voxelPalette == null) yield break;
				foreach (TPaletteItem item in voxelPalette.Items)
					yield return (IVoxelPaletteItem)item;
			}
		}

		public void CreateMeshFile()
		{
#if UNITY_EDITOR
			if (destinationMesh == null)
				RegenerateMeshes();

			string path = UnityEditor.EditorUtility.SaveFilePanel(
				"Save Voxel Mesh",
				"",
				voxelFilter.MapName + ".asset",
				"asset");

			int index = path.IndexOf("Assets/");
			if (index >= 0)
				path = path[index..];

			if (!path.IsNullOrEmpty())
				UnityEditor.AssetDatabase.CreateAsset(destinationMesh, path);
#endif
		}

		VoxelObject _lastFilter;
		void Update()
		{
			Debug.Log(voxelFilter);
			if (voxelFilter != null)
			{
				voxelFilter.MapChanged -= OnMapChanged;
				voxelFilter.MapChanged += OnMapChanged;
				_lastFilter = voxelFilter;
			}
			else if (_lastFilter != null)
			{
				_lastFilter.MapChanged -= OnMapChanged;
			}
		}

		EditorCoroutine _delayedGeneration = null;

		void OnMapChanged(bool quick)
		{
			if (autoRegenerateMeshes == ChangeOn.Never) return;

			if (_delayedGeneration != null)
				EditorCoroutineUtility.StopCoroutine(_delayedGeneration);

			if ((quick && autoRegenerateMeshes == ChangeOn.OnQuickChange) ||
				autoRegenerateMeshes == ChangeOn.EveryChange)
			{
				Debug.Log("!!!");
				RegenerateMeshes();

			}

			else if (!quick && autoRegenerateMeshes is ChangeOn.OnFinalChange)
				_delayedGeneration = EditorCoroutineUtility.StartCoroutine(RegenerateMeshesAfterDelay(), this);
		}

		IEnumerator RegenerateMeshesAfterDelay()
		{
			float time = Time.realtimeSinceStartup;
			while (Time.realtimeSinceStartup - time < regenDelay)
				yield return null;
			RegenerateMeshes();
		}

		void OnValidate()
		{
			voxelFilter = GetComponent<VoxelObject>();
		}

		protected static BenchmarkTimer benchmarkTimer;

		static readonly List<Vector3> _vertices = new();
		static readonly List<Vector3> _normals = new();
		static readonly List<Vector2> _uv = new();
		static readonly List<int> _triangles = new();
		static readonly List<SubMeshDescriptor> _descriptors = new();

		static int _currentTriangleIndex = 0;

		const int vertexLimitOf16Bit = 65536;

		public override void RegenerateMeshes()
		{
			if (!isActiveAndEnabled)
				return;

			if (doBenchmark)
				benchmarkTimer ??= new BenchmarkTimer(name + " " + GetType());
			else
				benchmarkTimer = null;

			benchmarkTimer?.StartModule("Clear Lists");



			_vertices.Clear();
			_normals.Clear();
			_uv.Clear();
			_triangles.Clear();
			_currentTriangleIndex = 0;
			_descriptors.Clear();


			BeforeMeshGeneration(Map, voxelPalette);


			int i = 0;
			foreach (TPaletteItem paletteItem in voxelPalette.Items)
			{
				GenerateMeshData(i, paletteItem, _vertices, _normals, _uv, _triangles);

				int triangleCount = _triangles.Count - _currentTriangleIndex;
				_descriptors.Add(new SubMeshDescriptor(_currentTriangleIndex, triangleCount));
				_currentTriangleIndex = _triangles.Count;
				i++;
			}


			if (destinationMesh == null)
			{
				destinationMesh = new()
				{
					name = voxelFilter.MapName
				};
			}

			benchmarkTimer?.StartModule("Setup Mesh");
			destinationMesh.Clear();
			destinationMesh.indexFormat = _vertices.Count >= vertexLimitOf16Bit ?
				IndexFormat.UInt32 : IndexFormat.UInt16;

			benchmarkTimer?.StartModule("Copy Vertex data to Mesh");
			destinationMesh.vertices = _vertices.ToArray();
			destinationMesh.normals = _normals.ToArray();
			destinationMesh.uv = _uv.ToArray();

			benchmarkTimer?.StartModule("Copy Triangle data to Mesh");
			destinationMesh.triangles = _triangles.ToArray();

			benchmarkTimer?.StartModule("Copy SubMesh data");
			destinationMesh.subMeshCount = _descriptors.Count;
			for (int j = 0; j < _descriptors.Count; j++)
			{
				SubMeshDescriptor descriptor = _descriptors[j];
				destinationMesh.SetSubMesh(j, descriptor);
			}

			if (doBenchmark)
			{
				string benchmarkResult = benchmarkTimer.ToString();
				if (benchmarkOutput != null)
					benchmarkOutput.text = benchmarkResult;
				Debug.Log(benchmarkResult);

				benchmarkTimer.Clear();
			}

			if (destinationMeshCollider != null)
				destinationMeshCollider.sharedMesh = destinationMesh;
			if (destinationMeshFilter != null)
				destinationMeshFilter.sharedMesh = destinationMesh;
		}

		internal override VoxelMeshGenerator CreateACopy(GameObject newGO)
		{
			VoxelMeshGenerator<TVoxelPalette, TPaletteItem> copy = AddACopy(newGO);
			copy.voxelPalette = voxelPalette;
			copy.RegenerateMeshes();
			return copy;
		}

		protected abstract void BeforeMeshGeneration(VoxelMap map, TVoxelPalette palette);

		protected abstract void GenerateMeshData(int paletteIndex, TPaletteItem paletteItem, List<Vector3> vertices, List<Vector3> normals, List<Vector2> uv, List<int> triangles);


		internal abstract VoxelMeshGenerator<TVoxelPalette, TPaletteItem> AddACopy(GameObject newGO);
	}
}