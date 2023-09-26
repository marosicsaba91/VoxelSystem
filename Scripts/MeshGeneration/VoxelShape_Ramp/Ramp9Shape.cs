using MUtility;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelSystem
{
	interface IRamShapeHolder
	{
		void OnRampUpdate(Ramp9Shape shape);
	}

	[Serializable]
	class Ramp9Shape
	{
		[SerializeField] bool[] isNodeSet;
		[SerializeField] float[] heightMap;

		Dictionary<GeneralDirection3D, bool> isSideFilledMap;

		int IndexOf(Vector2Int v) => (v.x + 1) + (v.y + 1) * 3;
		int IndexOf(int x, int y) => (x + 1) + (y + 1) * 3;
		public float GetNodeHeight(Vector2Int position) => heightMap[IndexOf(Clamp1(position))];
		public bool IsNodeSet(Vector2Int position) => isNodeSet[IndexOf(Clamp1(position))];

		public void SetNodeHeight(Vector2Int position, float value)
		{
			if (SetNodeHeightNoValidate(position, value))
				Validate();
		}

		public void SetNode(Vector2Int position, bool value)
		{
			if (SetNodeNoValidate(position, value))
				Validate();
		}

		public bool SetNodeHeightNoValidate(Vector2Int position, float value)
		{
			int index = IndexOf(Clamp1(position));
			value = Mathf.Clamp01(value);
			if (heightMap[index] == value) return false;
			heightMap[index] = value;
			return true;
		}

		public bool SetNodeNoValidate(Vector2Int position, bool value)
		{
			int index = IndexOf(Clamp1(position));
			if (isNodeSet[index] == value) return false;
			isNodeSet[index] = value;
			return true;
		}

		public bool IsSideFilled(GeneralDirection3D direction)
		{
			if (isSideFilledMap == null)
				RecalculateFill();
			return isSideFilledMap[direction];
		}

		public Ramp9Shape()
		{
			isNodeSet = new bool[9];
			heightMap = new float[9];
			RecalculateHeights();
			RecalculateFill();
		}

		public void Validate()
		{
			RecalculateHeights();
			RecalculateFill();
		}

		private static Vector2Int Clamp1(Vector2Int position)
		{
			if (Mathf.Abs(position.x) > 1)
				position.x = position.x > 0 ? 1 : -1;
			if (Mathf.Abs(position.y) > 1)
				position.y = position.y > 0 ? 1 : -1;
			return position;
		}

		void RecalculateFill()
		{
			if (isSideFilledMap == null)
				isSideFilledMap = new Dictionary<GeneralDirection3D, bool>();
			else
				isSideFilledMap.Clear();

			bool leftBack = GetNodeHeight(new Vector2Int(-1, -1)) >= 1;
			bool rightBack = GetNodeHeight(new Vector2Int(1, -1)) >= 1;
			bool leftFront = GetNodeHeight(new Vector2Int(-1, 1)) >= 1;
			bool rightFront = GetNodeHeight(new Vector2Int(1, 1)) >= 1;
			bool left = GetNodeHeight(new Vector2Int(-1, 0)) >= 1;
			bool right = GetNodeHeight(new Vector2Int(1, 0)) >= 1;
			bool back = GetNodeHeight(new Vector2Int(0, -1)) >= 1;
			bool front = GetNodeHeight(new Vector2Int(0, 1)) >= 1;

			bool all = leftBack && rightBack && leftFront && rightFront && left && right && back && front;
			bool allRight = rightBack && rightFront && right;
			bool allLeft = leftBack && leftFront && left;
			bool allFront = front && rightFront && leftFront;
			bool allBack = back && rightBack && leftBack;

			isSideFilledMap.Add(GeneralDirection3D.Down, true);
			isSideFilledMap.Add(GeneralDirection3D.Up, all);
			isSideFilledMap.Add(GeneralDirection3D.Right, allRight);
			isSideFilledMap.Add(GeneralDirection3D.Left, allLeft);
			isSideFilledMap.Add(GeneralDirection3D.Forward, allFront);
			isSideFilledMap.Add(GeneralDirection3D.Back, allBack);
		}


		static readonly List<Vector2Int> allSetNode = new();
		static readonly List<Vector2Int> allNotSetNode = new();
		void RecalculateHeights()
		{
			allSetNode.Clear();
			allNotSetNode.Clear();
			for (int x = -1; x <= 1; x++)
				for (int y = -1; y <= 1; y++)
				{
					int i = IndexOf(x, y);
					List<Vector2Int> relevantList = isNodeSet[i] ? allSetNode : allNotSetNode;
					relevantList.Add(new Vector2Int(x, y));
				}

			if (allSetNode.Count == 0) // No node set
			{
				for (int i = 0; i < heightMap.Length; i++)
				{
					heightMap[i] = 1;
				}
				return;
			}

			for (int i = 0; i < allNotSetNode.Count; i++)
			{
				Vector2Int currentPosition = allNotSetNode[i];

				// Find closest set node distance
				float closestDistance = float.MaxValue;
				for (int j = 0; j < allSetNode.Count; j++)
				{
					Vector2Int neighbourPos = allSetNode[j];
					float distance = (neighbourPos - currentPosition).magnitude;
					if (closestDistance > distance && distance > 0)
						closestDistance = distance;
				}

				// Find all set nodes with closest distance
				int nodeCount = 0;
				float totalHeight = 0;
				for (int j = 0; j < allSetNode.Count; j++)
				{
					Vector2Int neighbourPos = allSetNode[j];
					float distance = (neighbourPos - currentPosition).magnitude;
					if (distance == closestDistance)
					{
						nodeCount++;
						int neighbourIndex = IndexOf(neighbourPos);
						totalHeight += heightMap[neighbourIndex];
					}
				}

				// Calculate average height of the closest set nodes
				int currentIndex = IndexOf(currentPosition);
				heightMap[currentIndex] = totalHeight / nodeCount;
			}
		}

		public void UpdateAnySideMesh(MeshBuilder target, GeneralDirection3D dir, Rect uv)
		{
			if (dir == GeneralDirection3D.Down)
				UpdateBottomMesh(target, uv);
			else if (dir == GeneralDirection3D.Up)
				UpdateTopMesh(target, uv);
			else
				UpdateSideMesh(target, dir, uv);
		}

		void UpdateTopMesh(MeshBuilder target, Rect uv)
		{
			int[,] vertexI = new int[3, 3];

			for (int x = -1; x <= 1; x++)
				for (int z = -1; z <= 1; z++)
				{ 
					float y = GetNodeHeight(new Vector2Int(x, z));
					vertexI[x+1,z+1] = target.VertexCount;

					target.AddVertex(new Vector3(x/2f, y - 0.5f, z / 2f), Vector3.up, Lerp(uv, x, y));
				}

			target.AddTriangle(vertexI[0, 0], vertexI[0, 1], vertexI[1, 0]);
			target.AddTriangle(vertexI[0, 1], vertexI[1, 1], vertexI[1, 0]);

			target.AddTriangle(vertexI[2, 0], vertexI[1, 0], vertexI[2, 1]);
			target.AddTriangle(vertexI[1, 0], vertexI[1, 1], vertexI[2, 1]);

			target.AddTriangle(vertexI[0, 2], vertexI[1, 2], vertexI[0, 1]);
			target.AddTriangle(vertexI[0, 1], vertexI[1, 2], vertexI[1, 1]);

			target.AddTriangle(vertexI[2, 2], vertexI[2, 1], vertexI[1, 2]);
			target.AddTriangle(vertexI[1, 2], vertexI[2, 1], vertexI[1, 1]);

			target.RecalculateWindings();
		}

		Vector2 Lerp(Rect rect, float x, float z) 
		{
			float u = Mathf.Lerp(rect.xMin, rect.xMax, x);
			float v = Mathf.Lerp(rect.yMin, rect.yMax, z);
			return new Vector2(u, v);
		}

		void UpdateSideMesh(MeshBuilder target, GeneralDirection3D dir, Rect uv)
		{
			// NO Top or Bottom!

			Vector3 dirVec = dir.ToVector();
			Vector3 perpendicular = Vector3.Cross(dirVec, Vector3.up);

			Vector2Int corner1NodeIndex = new ((int)(dirVec.x + perpendicular.x), (int)(dirVec.z + perpendicular.z));
			Vector2Int corner2NodeIndex = new ((int)(dirVec.x - perpendicular.x), (int)(dirVec.z - perpendicular.z));
			Vector2Int middleNodeIndex = new ((int)dirVec.x, (int)dirVec.z);

			Vector3 bottomCorner1Pos = (dirVec + perpendicular + Vector3.down) * 0.5f;
			Vector3 bottomCorner2Pos = (dirVec - perpendicular + Vector3.down) * 0.5f;

			Vector3 topCorner1Pos = bottomCorner1Pos + Vector3.up * GetNodeHeight(corner1NodeIndex);
			Vector3 topCorner2Pos = bottomCorner2Pos + Vector3.up * GetNodeHeight(corner2NodeIndex);
			Vector3 topMiddlePos = dirVec*0.5f  + Vector3.up * (GetNodeHeight(middleNodeIndex) - 0.5f);

			// Vertices

			int indexBottom1 = target.vertices.Count;
			target.vertices.Add(bottomCorner1Pos);
			target.normals.Add(dirVec);
			target.uv.Add(Vector2.zero);  // TODO

			int indexBottom2 = target.vertices.Count;
			target.vertices.Add(bottomCorner2Pos);
			target.normals.Add(dirVec);
			target.uv.Add(Vector2.zero);  // TODO

			int indexTop1 = target.vertices.Count;  
			target.vertices.Add(topCorner1Pos);
			target.normals.Add(dirVec);
			target.uv.Add(Vector2.zero);  // TODO

			int indexTop2 = target.vertices.Count;
			target.vertices.Add(topCorner2Pos);
			target.normals.Add(dirVec);
			target.uv.Add(Vector2.zero);  // TODO

			int indexTopMiddle = target.vertices.Count;
			target.vertices.Add(topMiddlePos);
			target.normals.Add(dirVec);
			target.uv.Add(Vector2.zero);  // TODO

			// Triangles (Winding: Clockwise)

			target.triangles.Add(indexTopMiddle);
			target.triangles.Add(indexTop1);
			target.triangles.Add(indexBottom1); 

			target.triangles.Add(indexBottom2);
			target.triangles.Add(indexTopMiddle);
			target.triangles.Add(indexBottom1);

			target.triangles.Add(indexTop2);
			target.triangles.Add(indexTopMiddle);
			target.triangles.Add(indexBottom2);
		}

		static void UpdateBottomMesh(MeshBuilder target, Rect uv)
		{
			int vIndexA = target.vertices.Count;
			target.vertices.Add(new Vector3(-0.5f, -0.5f, -0.5f));
			target.normals.Add(Vector3.down);
			target.uv.Add(new Vector2(uv.xMin, uv.yMin));

			int vIndexB = target.vertices.Count;
			target.vertices.Add(new Vector3(-0.5f, -0.5f, 0.5f));
			target.normals.Add(Vector3.down);
			target.uv.Add(new Vector2(uv.xMin, uv.yMax));

			int vIndexC = target.vertices.Count;
			target.vertices.Add(new Vector3(0.5f, -0.5f, 0.5f));
			target.normals.Add(Vector3.down);
			target.uv.Add(new Vector2(uv.xMax, uv.yMax));

			int vIndexD = target.vertices.Count;
			target.vertices.Add(new Vector3(0.5f, -0.5f, -0.5f));
			target.normals.Add(Vector3.down);
			target.uv.Add(new Vector2(uv.xMax, uv.yMin));

			target.triangles.Add(vIndexC);
			target.triangles.Add(vIndexB);
			target.triangles.Add(vIndexA);

			target.triangles.Add(vIndexD);
			target.triangles.Add(vIndexC);
			target.triangles.Add(vIndexA);
		}
	}
}