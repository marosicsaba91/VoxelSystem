using MUtility;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.EditorCoroutines.Editor;
using UnityEngine;

namespace VoxelSystem
{
	[ExecuteAlways]
	public class VoxelNavMap : MonoBehaviour
	{
		[SerializeField] VoxelFilter voxelFilter;
		[SerializeField] VoxelNavAgentType agentType;
		[SerializeField] VoxelNavTarget[] targets;
		[Space]
		[SerializeField] ChangeOn autoRegenerateMap = ChangeOn.Never;
		[SerializeField] float regenDelay;
		[SerializeField] DisplayMember clearNavData = new(nameof(ClearNavData));
		[SerializeField] DisplayMember calculateNextDistanceLevel = new(nameof(CalculateNextDistanceLevel));
		[SerializeField] DisplayMember recalculateNavDataQuick = new(nameof(RecalculateNavDataQuick));
		[SerializeField] DisplayMember recalculateNavDataSlow = new(nameof(RecalculateNavDataSlow));

		[Space]
		[SerializeField] bool visualize = true;
		[SerializeField, ColorUsage(false)] Color color = Color.red;
		[SerializeField, ShowIf(nameof(visualize)), Min(1)] int maxDistanceToShow = 10;
		[SerializeField, ShowIf(nameof(visualize)), Range(0, 1)] float minDistanceAlpha = 0.1f;
		[SerializeField, ShowIf(nameof(visualize)), Range(0, 1)] float maxDistanceAlpha = 0.5f;
		[SerializeField, ShowIf(nameof(visualize)), Min(0.0001f)] float slowTimeStep = 0.1f;

		readonly Dictionary<Vector3Int, NavVoxelData> navMap = new();

		readonly Dictionary<Vector3Int, NavVoxelData> currentLevel = new();
		readonly Dictionary<Vector3Int, NavVoxelData> nextLevel = new();

		VoxelFilter _lastFilter;

		void Update()
		{
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
			if (autoRegenerateMap == ChangeOn.Never) return;

			if (_delayedGeneration != null)
				EditorCoroutineUtility.StopCoroutine(_delayedGeneration);

			if ((quick && autoRegenerateMap == ChangeOn.OnQuickChange) ||
				autoRegenerateMap == ChangeOn.EveryChange)
				RecalculateNavDataQuick();

			else if (!quick && autoRegenerateMap is ChangeOn.OnFinalChange)
				_delayedGeneration = EditorCoroutineUtility.StartCoroutine(RegenerateMeshesAfterDelay(), this);
		}

		IEnumerator RegenerateMeshesAfterDelay()
		{
			float time = Time.realtimeSinceStartup;
			while (Time.realtimeSinceStartup - time < regenDelay)
				yield return null;
			RecalculateNavDataQuick();
		}
		 

		void Start()
		{
			if (Application.isPlaying)
				RecalculateNavDataQuick();
		}

		protected static BenchmarkTimer benchmarkTimer;

		void RecalculateNavDataQuick()
		{
			benchmarkTimer ??= new BenchmarkTimer(name + " " + GetType());
			benchmarkTimer?.StartModule("Clear Lists");

			ClearNavData();
			while (currentLevel.Count > 0)
				CalculateNextDistanceLevel();

			string benchmarkResult = benchmarkTimer.ToString(); 
			Debug.Log(benchmarkResult);

			benchmarkTimer.Clear();
		}

#if UNITY_EDITOR
		Unity.EditorCoroutines.Editor.EditorCoroutine slowCoroutine = null;
#endif

		void RecalculateNavDataSlow()
		{
#if UNITY_EDITOR

			if (slowCoroutine != null)
			{
				Unity.EditorCoroutines.Editor.EditorCoroutineUtility.StopCoroutine(slowCoroutine);
				slowCoroutine = null;
				return;
			}


			slowCoroutine = Unity.EditorCoroutines.Editor.EditorCoroutineUtility.
				StartCoroutine(SlowRecalculateCoroutine(), this);


			IEnumerator SlowRecalculateCoroutine()
			{
				double lastFresh = UnityEditor.EditorApplication.timeSinceStartup;
				ClearNavData();

				while (currentLevel.Count > 0)
				{
					double freshAge = 0;
					while (freshAge < slowTimeStep)
					{
						freshAge = UnityEditor.EditorApplication.timeSinceStartup - lastFresh;
						yield return null;
					}

					lastFresh = UnityEditor.EditorApplication.timeSinceStartup;
					CalculateNextDistanceLevel();
					UnityEditor.SceneView.RepaintAll();
				}

				slowCoroutine = null;
			}
#endif
		}
		void ClearNavData()
		{
			navMap.Clear();
			currentLevel.Clear();
			nextLevel.Clear();
			// indexPointsSetupAlready.Clear();

			// Setup TargetPoints
			for (int i = 0; i < targets.Length; i++)
			{
				Vector3Int targetIndexPoint = targets[i].TargetPoint;
				NavVoxelData newNavData = new(targetIndexPoint, true);
				currentLevel.Add(targetIndexPoint, newNavData);
				navMap.Add(targetIndexPoint, newNavData);
				// indexPointsSetupAlready.Add(targetIndexPoint);
			}
		}

		void CalculateNextDistanceLevel()
		{
			if (voxelFilter == null)
				return;
			VoxelMap map = voxelFilter.GetVoxelMap();
			if (map == null)
				return;

			nextLevel.Clear();
			foreach ((Vector3Int currentIndexPoint, NavVoxelData currentVoxel) in currentLevel)
			{
				foreach (GeneralDirection3D direction in DirectionUtility.generalDirection3DValues)
				{
					Vector3Int nextIndexPoint = currentIndexPoint + direction.ToVectorInt();
					if (!map.IsValidCoord(nextIndexPoint))
						continue;

					int voxelValue = map.GetVoxel(nextIndexPoint);
					if (voxelValue.IsFilled())
						continue;

					if (!navMap.TryGetValue(nextIndexPoint, out NavVoxelData nextVoxel))
					{
						nextVoxel = new NavVoxelData(nextIndexPoint, false);
						navMap.Add(nextIndexPoint, nextVoxel);
						nextLevel.Add(nextIndexPoint, nextVoxel);
					}

					nextVoxel.TryAddConnection(currentVoxel, direction.Opposite());
				}
			}
			currentLevel.Clear();

			foreach ((Vector3Int point, NavVoxelData data) in nextLevel)
				currentLevel.Add(point, data);
		}

		void OnDrawGizmos()
		{
			if (!visualize)
				return;
			if (navMap == null)
				return;
			if (voxelFilter == null)
				return;

			Transform mapTransform = voxelFilter.transform;

			Vector3 half = Vector3.one / 2f;

			foreach ((Vector3Int point, NavVoxelData data) in navMap)
			{
				VoxelGate minimumGate = data.GetMinimalCostGate();
				if (minimumGate == null)
					continue;
				if (minimumGate.distanceLeft > maxDistanceToShow)
					continue;

				Vector3 centerPoint = mapTransform.TransformPoint(point + half);
				Vector3 closestOutput = centerPoint + mapTransform.TransformVector(minimumGate.inCellPosition);

				float a = Mathf.Lerp(minDistanceAlpha, maxDistanceAlpha, minimumGate.distanceLeft / maxDistanceToShow);
				Gizmos.color = new Color(color.r, color.g, color.b, a);

				Gizmos.DrawLine(centerPoint, closestOutput);
			}

			foreach ((Vector3Int point, NavVoxelData _) in currentLevel)
			{
				Vector3 worldPoint = mapTransform.TransformPoint(point + half);
				Gizmos.color = Color.yellow;
				Gizmos.DrawSphere(worldPoint, 0.3f);
			}

		}

		internal void GetPath(Vector3 position, List<Vector3> path)
		{
			path.Clear();
			if (navMap == null)
				return;
			if (voxelFilter == null)
				return;

			path.Add(position); 
			Transform t = voxelFilter.transform;

			Vector3 localPosInMap = t.InverseTransformPoint(position);
			Vector3Int currentIndexPoint = Vector3Int.RoundToInt(localPosInMap);
			if (!navMap.TryGetValue(currentIndexPoint, out NavVoxelData currentVoxel))
				return;

			Vector3 half = Vector3.one / 2f;
			while (currentVoxel != null)
			{
				currentIndexPoint = currentVoxel.indexPoint;
				VoxelGate nextGate = currentVoxel.GetMinimalCostGate();
				if (nextGate == null)
					break;

				Vector3 localPoint = nextGate.inCellPosition;
				Vector3 worldPoint = t.TransformPoint(currentIndexPoint + half + localPoint);
				path.Add(worldPoint);

				currentVoxel = nextGate.connectedVoxel;
				if (currentVoxel == null)
					break;
			}

		}

		class NavVoxelData
		{
			public Vector3Int indexPoint;
			public List<VoxelGate> outputPoints;

			public NavVoxelData(Vector3Int index, bool isTarget)
			{ 
				indexPoint = index;
				outputPoints = new List<VoxelGate>();

				if (isTarget)
				{
					outputPoints.Add(new VoxelGate()
					{
						connectedVoxel = null,
						distanceLeft = 0,
						inCellPosition = Vector3.zero,
					});
				}
			}

			public void TryAddConnection(NavVoxelData neighbour, GeneralDirection3D neighbourDirection)
			{
				if (neighbour == null)
					return;

				VoxelGate neighbourMinCostGate = neighbour.GetMinimalCostGate();
				if (neighbourMinCostGate == null)
					return;

				Vector3 dirVector = neighbourDirection.ToVector();
				Vector3 inCellPosition = dirVector * 0.5f;

				float inNeighbourGateCost =
					Vector3.Distance(neighbourMinCostGate.inCellPosition + dirVector, inCellPosition);

				float fullDistanceLeft = neighbourMinCostGate.distanceLeft + inNeighbourGateCost;

				VoxelGate toNeighbour = new()
				{
					connectedVoxel = neighbour,
					distanceLeft = fullDistanceLeft,
					inCellPosition = inCellPosition,
				};
				outputPoints.Add(toNeighbour);
			}

			public VoxelGate GetMinimalCostGate()
			{
				float minCost = float.MaxValue;
				VoxelGate minCostGate = null;
				for (int i = 0; i < outputPoints.Count; i++)
				{
					VoxelGate gate = outputPoints[i];
					if (gate == null)
						continue;
					if (gate.distanceLeft < minCost)
					{
						minCost = gate.distanceLeft;
						minCostGate = gate;
					}
				}
				return minCostGate;
			}
		}

		class VoxelGate
		{
			public NavVoxelData connectedVoxel;
			public float distanceLeft;
			public Vector3 inCellPosition;
		}
	}
}