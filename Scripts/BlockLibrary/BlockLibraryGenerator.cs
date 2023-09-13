using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using MUtility;
using UnityEngine;

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
		public BlockType blockType;
		public List<MeshInfoNew> meshInfos;
	}



	[ExecuteAlways]
	public class BlockLibraryGenerator : MonoBehaviour
	{
		// SETTINGS
		[SerializeField] VoxelBlockLibrary voxelBlockLibrary;
		[SerializeField] BlockSetup[] blockSetups;
		[SerializeField] Material material;

		/*
		[Header("Settings")]
		[SerializeField] BlockSetupNew corners;
		[SerializeField] BlockSetupNew edges;
		[SerializeField] BlockSetupNew sides;
		[SerializeField] BlockSetupNew negativeCorners;
		[SerializeField] BlockSetupNew negativeSides;
		[SerializeField] BlockSetupNew negativeEdges;
		[SerializeField] BlockSetupNew cross;
		[SerializeField] BlockSetupNew sideMeetsEdge;
		[SerializeField] BlockSetupNew sideMeetsNegativeEdge;
		[SerializeField] BlockSetupNew sideMeetsCorner;
		*/

		// BUTTONS
		[Header("Actions")]
		[SerializeField, UsedImplicitly] DisplayMember clearLibrary = new(nameof(Clear));
		[SerializeField, UsedImplicitly] DisplayMember regenerateLibrary = new(nameof(RegenerateLibrary));

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
			foreach (BlockSetup setup in blockSetups)
			{
				setup.Setup();

				BlockType blockType = setup.blockType;
				Axis3D axis = setup.axis;

				foreach (SubVoxelFlags subVoxel in SubVoxelUtility.AllSubVoxel)
				{
					Mesh mesh = setup.TryFindMesh(subVoxel);

					if (mesh == null)
						continue;

					Matrix4x4 matrix4X4 = setup.GetTransformation(subVoxel);
					CustomMesh customMesh = CustomMesh.CreateFromMesh(mesh, matrix4X4);
					voxelBlockLibrary.AddBlock(new BlockKey(blockType, subVoxel, axis), customMesh);
				}
			}

			voxelBlockLibrary.MakeDirty();
		}

		bool EnableRegenerate() => gameObject.scene.isLoaded;

		// -------------------------------------------------------------------------------------------------------------
	}
}