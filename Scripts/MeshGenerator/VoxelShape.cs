using MUtility;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelSystem
{
	public abstract class VoxelShape : ScriptableObject, IPaletteItem
	{
		[SerializeField] string niceName;
		[SerializeField] Color color;
		[SerializeField] VoxelShape simpleVersion;

		private void OnValidate()
		{
			if (simpleVersion != null)
			{
				if (simpleVersion.simpleVersion != null)
				{
					simpleVersion.simpleVersion = null;
					Debug.LogWarning("Quick version can not have a quick version");
				}
				if (simpleVersion == this)
				{
					simpleVersion = null;
					Debug.LogWarning("Quick version can not be self");
				}
			}

			OnValidateInternal();
		}

		protected virtual void OnValidateInternal() { }

		public string DisplayName => niceName.IsNullOrEmpty() ? name : niceName;

		public Color DisplayColor => color;

		VoxelShape GetVoxelVersion(bool quick) => (quick && simpleVersion != null) ? simpleVersion : this;

		internal void BeforeMeshGeneration(VoxelMap map, VoxelShapePalette palette, int shapeIndex, bool quick) =>
			GetVoxelVersion(quick).BeforeMeshGeneration(map, palette, shapeIndex);


		protected abstract void BeforeMeshGeneration(VoxelMap map, VoxelShapePalette palette, int shapeIndex);

		internal void GenerateMeshData(
			VoxelMap map,
			List<Vector3Int> palette,
			int shapeIndex,
			List<Vector3> vertexOut,
			List<Vector3> normalOut,
			List<Vector2> uvOut,
			List<int> triangleOut,
			bool quick) =>
			GetVoxelVersion(quick).GenerateMeshData(map, palette, shapeIndex, vertexOut, normalOut, uvOut, triangleOut);

		protected abstract void GenerateMeshData(
			VoxelMap map,
			List<Vector3Int> voxelPositions,
			int shapeIndex,
			List<Vector3> vertexOut,
			List<Vector3> normalOut,
			List<Vector2> uvOut,
			List<int> triangleOut);



		public virtual bool IsFlipEnabled 
		{ 
			get => false;
			internal set { }
		}


		public virtual bool IsRotationEnabled
		{
			get => false;
			internal set { }
		}

	}
}