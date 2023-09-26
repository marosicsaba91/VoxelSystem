using MUtility;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelSystem
{
	[CreateAssetMenu(fileName = "RampVoxelShape", menuName = EditorConstants.categoryPath + "VoxelShape: Ramp", order = EditorConstants.soOrder_VoxelShape)]
	public class VoxelShape_Ramp : VoxelShapeBuilder, IRamShapeHolder
	{
		[SerializeField] Ramp9Shape rampShape = new();
		[SerializeField] CubeUVSetup uvCoordinates = new();
		 
		[SerializeField, HideInInspector] MeshBuilder[] sides = new MeshBuilder[6];
		[SerializeField, HideInInspector] MeshBuilder full = new();

		void IRamShapeHolder.OnRampUpdate(Ramp9Shape shape) => base.OnValidate();
		protected sealed override void ValidateInternal()
		{
			rampShape.Validate(); 
			UpdateMesh();
		}

		public void UpdateMesh() 
		{
			GeneralDirection3D[] directions = DirectionUtility.generalDirection3DValues;
			full.Clear();

			if (sides == null || sides.Length != directions.Length)
				sides = new MeshBuilder[directions.Length];

			for (int i = 0; i < directions.Length; i++)
			{
				if (sides[i] == null)
					sides[i] = new MeshBuilder();

				sides[i].Clear();
				Rect uvRect = uvCoordinates.GetRect(directions[i]);
				rampShape.UpdateAnySideMesh(sides[i], directions[i], uvRect);

				full.Add(sides[i]);
			}
		}

		protected sealed override void GenerateMeshData(
			VoxelMap map,
			List<Vector3Int> voxelPositions,
			int shapeIndex,
			MeshBuilder meshBuilder)
		{
			Vector3 half = Vector3.one * 0.5f;

			for (int i = 0; i < voxelPositions.Count; i++)
			{
				Vector3Int position = voxelPositions[i];
				meshBuilder.Add(full, position + half);
			}
		}

		protected sealed override bool IsSideFilled(GeneralDirection3D dir) => rampShape.IsSideFilled(dir);

		public sealed override bool IsTransformEnabled => false;
	}
}