using MUtility;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

namespace VoxelSystem
{
	public abstract class VoxelMeshGenerator : MonoBehaviour
	{
		internal abstract int PaletteLength { get; }
		internal abstract IEnumerable<IVoxelPaletteItem> PaletteItems { get; }

		internal abstract VoxelMeshGenerator CreateACopy(GameObject newGO);
		public abstract void RegenerateMeshes();
	}

	public abstract class VoxelMeshGenerator<TVoxelPalette, TPaletteItem> : VoxelMeshGenerator where TVoxelPalette : IVoxelPalette<TPaletteItem>
	{
		[SerializeField, HideInInspector] VoxelFilter voxelFilter;
		[SerializeField] TVoxelPalette voxelPalette;

		[SerializeField] Mesh destinationMesh;
		[SerializeField] MeshFilter destinationMeshFilter;
		[SerializeField] MeshCollider destinationMeshCollider;

		[SerializeField] DisplayMember regenerateMesh = new(nameof(RegenerateMeshes));
		[SerializeField] DisplayMember createMeshFile = new(nameof(CreateMeshFile));

		[SerializeField] bool doBenchmark;
		[SerializeField] TMP_Text benchmarkOutput;


		VoxelMap Map => voxelFilter == null ? null : voxelFilter.GetVoxelMap();

		internal sealed override int PaletteLength => voxelPalette == null ? 1 : voxelPalette.Length;
		internal sealed override IEnumerable<IVoxelPaletteItem> PaletteItems
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

		void OnValidate()
		{
			voxelFilter = GetComponent<VoxelFilter>();
		}

		protected static BenchmarkTimer _benchmarkTimer;

		static readonly List<Vector3> _vertices = new();
		static readonly List<Vector3> _normals = new();
		static readonly List<Vector2> _uv = new();
		static readonly List<int> _triangles = new();
		static readonly List<SubMeshDescriptor> _descriptors = new();

		static int _currentTriangleIndex = 0;

		const int vertexLimitOf16Bit = 65536;

		public override sealed void RegenerateMeshes()
		{
			if (!isActiveAndEnabled)
				return;

			if (doBenchmark)
				_benchmarkTimer ??= new BenchmarkTimer(name + " " + GetType());
			else
				_benchmarkTimer = null;

			_benchmarkTimer?.StartModule("Clear Lists");



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

			_benchmarkTimer?.StartModule("Setup Mesh");
			destinationMesh.Clear();
			destinationMesh.indexFormat = _vertices.Count >= vertexLimitOf16Bit ?
				IndexFormat.UInt32 : IndexFormat.UInt16;

			_benchmarkTimer?.StartModule("Copy Vertex data to Mesh");
			destinationMesh.vertices = _vertices.ToArray();
			destinationMesh.normals = _normals.ToArray();
			destinationMesh.uv = _uv.ToArray();

			_benchmarkTimer?.StartModule("Copy Triangle data to Mesh");
			destinationMesh.triangles = _triangles.ToArray();

			_benchmarkTimer?.StartModule("Copy SubMesh data");
			destinationMesh.subMeshCount = _descriptors.Count;
			for (int j = 0; j < _descriptors.Count; j++)
			{
				SubMeshDescriptor descriptor = _descriptors[j];
				destinationMesh.SetSubMesh(j, descriptor);
			}

			if (doBenchmark)
			{
				string benchmarkResult = _benchmarkTimer.ToString();
				if (benchmarkOutput != null)
					benchmarkOutput.text = benchmarkResult;
				Debug.Log(benchmarkResult);

				_benchmarkTimer.Clear();
			}

			if (destinationMeshCollider != null)
				destinationMeshCollider.sharedMesh = destinationMesh;
			if (destinationMeshFilter != null)
				destinationMeshFilter.sharedMesh = destinationMesh;
		}
		protected abstract void BeforeMeshGeneration(VoxelMap map, TVoxelPalette palette);

		protected abstract void GenerateMeshData(int paletteIndex, TPaletteItem paletteItem, List<Vector3> vertices, List<Vector3> normals, List<Vector2> uv, List<int> triangles);

		internal override VoxelMeshGenerator CreateACopy(GameObject newGO)
		{
			VoxelMeshGenerator<TVoxelPalette, TPaletteItem> copy = AddACopy(newGO);
			copy.voxelPalette = voxelPalette;
			copy.RegenerateMeshes();
			return copy;
		}

		internal abstract VoxelMeshGenerator<TVoxelPalette, TPaletteItem> AddACopy(GameObject newGO);
	}
}