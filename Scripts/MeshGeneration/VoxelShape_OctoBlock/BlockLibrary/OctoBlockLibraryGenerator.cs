using System;
using System.Collections.Generic;
using MUtility;
using JetBrains.Annotations;
using UnityEngine;
using EasyEditor;
using Benchmark;
using MeshUtility;

namespace VoxelSystem
{
	enum TransformAction
	{
		None,
		OffsetX,
		OffsetY,
		OffsetZ,
		RotateX,
		RotateY,
		RotateZ,
		MirrorX,
		MirrorY,
		MirrorZ,
	}

	[Serializable]
	struct MeshInfoNew
	{
		public Mesh mesh;
		public Vector3 offset;
		public Vector3 rotation;
		public Vector3 scale;
		public SubVoxelFlags enabledSubVoxels;
		public Axis3D enabledAxis;

		public TransformAction action1;
		public TransformAction action2;
		public TransformAction action3;

	}

	[Serializable]
	struct BlockSetupNew
	{
		public bool useThisSetup;
		public OctoBlockType blockType;
		public List<MeshInfoNew> meshInfo;
	}



	[ExecuteAlways]
	public class OctoBlockLibraryGenerator : MonoBehaviour
	{
		// SETTINGS
		[SerializeField] OctoBlockLibrary voxelBlockLibrary;
		[SerializeField] OctoBlockSetup[] blockSetups;
		[SerializeField] Material material;
		 
		// BUTTONS
		[Header("Actions")]
		[SerializeField, UsedImplicitly] EasyButton clearLibrary = new(nameof(Clear));
		[SerializeField, UsedImplicitly] EasyButton regenerateLibrary = new(nameof(RegenerateLibrary));

		// -------------------------------------------------------------------------------------------------------------

		readonly BenchmarkTimer _benchmarkTimer = new("Whole Building Process");

		public Material Material => material;

		void Clear()
		{
			if (ErrorTest())
				return;
			voxelBlockLibrary.Clear();
		}

		public bool ErrorTest()
		{
			if (!EnableRegenerate())
			{
				Debug.LogWarning("Regenerating the Library is not allowed is the prefab is not loaded!");
				return true;
			}

			return false;
		}

		void RegenerateLibrary()
		{
			if (ErrorTest())
				return;

			voxelBlockLibrary.Clear();
			foreach (OctoBlockSetup setup in blockSetups)
			{
				setup.Setup();

				OctoBlockType blockType = setup.blockType;
				Axis3D axis = setup.axis;

				foreach (SubVoxelFlags subVoxel in SubVoxelUtility.AllSubVoxel)
				{
					Mesh mesh = setup.TryFindMesh(subVoxel);

					if (mesh == null)
						continue;

					Matrix4x4 matrix4X4 = setup.GetTransformation(subVoxel);
					MeshBuilder customMesh = new (mesh, matrix4X4);
					voxelBlockLibrary.AddBlock(new OctoBlockKey(blockType, subVoxel, axis), customMesh);
				}
			}

			voxelBlockLibrary.MakeDirty();
		}

		bool EnableRegenerate() => gameObject.scene.isLoaded;

		// -------------------------------------------------------------------------------------------------------------
	}
}