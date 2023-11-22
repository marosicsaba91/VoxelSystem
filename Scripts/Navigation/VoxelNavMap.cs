using Benchmark;
using EasyInspector;
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
		enum ChangeOn { Never, OnQuickChange, OnFinalChange, EveryChange }

		[SerializeField] VoxelObject voxelFilter;
		[SerializeField] VoxelNavAgentSetting agentSetting;

		[SerializeField] VoxelNavTarget[] targets;

		[Space]
		[SerializeField] ChangeOn autoRegenerateMap = ChangeOn.Never;
		[SerializeField] float regenDelay;

		[Header("Commands")]
		[SerializeField] EasyMember clearNavData = new(nameof(ClearNavData));
		[SerializeField] EasyMember calculateVoxels = new(nameof(CalculatePossibleVoxels));
		[SerializeField] EasyMember calculateConnections = new(nameof(SetupConnections));
		[SerializeField] EasyMember setupWeightWaves = new(nameof(SetupWave));
		[SerializeField] EasyMember calculateNextDistanceLevel = new(nameof(CalculateNextDistanceLevel));

		[Space]
		[SerializeField] EasyMember recalculateNavDataQuick = new(nameof(RecalculateNavDataQuick));

		[Space]

		[Space]
		[SerializeField] bool visualizePoints = true;
		[SerializeField, ShowIf(nameof(visualizePoints))] Color pointColor = Color.blue;
		[SerializeField, ShowIf(nameof(visualizePoints))] bool showPoints = true;
		[SerializeField, ShowIf(nameof(visualizePoints))] Color lineColor = Color.red;
		[SerializeField, ShowIf(nameof(visualizePoints))] bool showAllConnections = false;
		[SerializeField, ShowIf(nameof(visualizePoints))] bool showCheapestConnections = true;
		[SerializeField, ShowIf(nameof(visualizePoints))] bool showWaves = true;
		[Space]
		[SerializeField, ShowIf(nameof(visualizePoints)), Min(1)] int maxDistanceToShow = 10;
		[SerializeField, ShowIf(nameof(visualizePoints)), Range(0, 1)] float maxDistanceAlpha = 0.5f;


		readonly Dictionary<Vector3Int, NavVoxelData> navMap = new();
		VoxelObject _lastFilter;

		//--------------------------------------------------------------------

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
				_delayedGeneration = EditorCoroutineUtility.StartCoroutine(RegenerateNavDataAfterDelay(), this);

		}

		IEnumerator RegenerateNavDataAfterDelay()
		{
			float time = Time.realtimeSinceStartup;
			while (Time.realtimeSinceStartup - time < regenDelay)
				yield return null;
			RecalculateNavDataQuick();

			yield return null;
		}

		void Start()
		{
			if (Application.isPlaying)
				RecalculateNavDataQuick();
		}

		// --------------------------------------------------------------------

		protected static BenchmarkTimer benchmarkTimer;
		void RecalculateNavDataQuick()
		{
			benchmarkTimer ??= new BenchmarkTimer(name + " " + GetType());
			benchmarkTimer?.StartModule("Calculate Possible Voxels");

			CalculatePossibleVoxels();
			benchmarkTimer?.StartModule("Setup Connections");
			SetupConnections();

			benchmarkTimer?.StartModule("Setup Weight Waves");
			SetupWave();

			benchmarkTimer?.StartModule("Calculate Connection Weights");
			_wave.CalculateValuesQuick();

			string benchmarkResult = benchmarkTimer.ToString();
			Debug.Log(benchmarkResult);

			benchmarkTimer.Clear();
		}

		// --------------------------------------------------------------------

		void ClearNavData() => _positionsCache.Clear();

		readonly List<Vector3Int> _positionsCache = new();
		void CalculatePossibleVoxels()
		{
			_positionsCache.Clear();
			agentSetting.GetPossiblePositions(voxelFilter.GetVoxelMap(), _positionsCache);

			navMap.Clear();
			for (int i = 0; i < _positionsCache.Count; i++)
			{
				Vector3Int position = _positionsCache[i];
				navMap.Add(position, new NavVoxelData(position));
			}
		}

		void SetupConnections() => agentSetting.SetupConnections(voxelFilter.GetVoxelMap(), navMap);


		readonly NavConnectionWeightWave _wave = new();
		void SetupWave()
		{
			_wave.Clear();
			for (int i = 0; i < targets.Length; i++)
			{
				Vector3Int index = targets[i].TargetPoint;
				if (navMap.TryGetValue(index, out NavVoxelData voxel))
					_wave.SetupTarget(voxel);
			}
		}



		void CalculateNextDistanceLevel() => _wave.NextLevel();

		//--------------------------------------------------------------------

		void OnDrawGizmos()
		{
			if (!visualizePoints) return;
			if (navMap == null) return;
			if (voxelFilter == null) return;

			Transform mapTransform = voxelFilter.transform;

			// Points
			if (showPoints)
			{
				Gizmos.color = pointColor;
				foreach (NavVoxelData voxel in navMap.Values)
				{
					float cost = voxel.cost;
					if (cost > maxDistanceToShow) continue;
					if (cost < 0 ) continue;
					Vector3 voxelPosition = voxel.GlobalPoint(mapTransform);
					TextGizmos.TextColor = pointColor;
					// max one decimal, preferably no decimal
					string costString = cost.ToString("0.#");
					TextGizmos.DrawText(voxelPosition, costString);
				}
			}

			// Cheapest Connections
			if (showCheapestConnections && !showAllConnections)
			{
				Gizmos.color = lineColor;
				foreach (NavVoxelData voxel in navMap.Values)
				{
					float cost = voxel.cost;
					if (cost > maxDistanceToShow) continue;
					if (cost < 0) continue;
					NavVoxelData minimumCostNeighbour = voxel.GetMinimalCostNeighbour();
					if (minimumCostNeighbour == null) continue;

					Vector3 voxelPosition = voxel.GlobalPoint(mapTransform);
					Vector3 closestPosition = minimumCostNeighbour.GlobalPoint(mapTransform);
					float a = Mathf.Lerp(lineColor.a, maxDistanceAlpha, minimumCostNeighbour.cost / (maxDistanceToShow - 1));
					Gizmos.color = new Color(lineColor.r, lineColor.g, lineColor.b, a);
					DrawQuickArrow(voxelPosition, closestPosition);
				}
			}
			// All Connections
			else if (showAllConnections)
			{
				Gizmos.color = lineColor;
				foreach (NavVoxelData voxel in navMap.Values)
				{
					Vector3 voxelPosition = voxel.GlobalPoint(mapTransform);
					float cost = voxel.cost;
					if (cost >= maxDistanceToShow) continue;
					if (cost < 0) continue;
					float a = Mathf.Lerp(lineColor.a, maxDistanceAlpha, cost / (maxDistanceToShow - 1));
					Gizmos.color = new Color(lineColor.r, lineColor.g, lineColor.b, a);
					foreach (NavVoxelData neighbour in voxel.neighbours)
						DrawQuickArrow(voxelPosition, neighbour.GlobalPoint(mapTransform));

				}
			}

			// Waves
			if (showWaves)
				foreach (NavVoxelData data in _wave.CurrentLevel)
				{
					Vector3 worldPoint = data.GlobalPoint(mapTransform);
					Gizmos.color = lineColor;
					Gizmos.DrawSphere(worldPoint, 0.5f);
				}

			static void DrawQuickArrow(Vector3 from, Vector3 to)
			{
				const float baseSze = 0.1f;
				const float gapPercent = 0.1f;
				Vector3 perpendicular = (from - to).GetPerpendicular();
				Vector3 start = Vector3.Lerp(from, to, gapPercent);
				Vector3 end = Vector3.Lerp(from, to, 1 - gapPercent);
				Gizmos.DrawLine(start, end);
				Gizmos.DrawLine(start + perpendicular * baseSze, start - perpendicular * baseSze);
			}
		}

		//--------------------------------------------------------------------

		internal bool TryGetPath(Vector3 position, List<Vector3> path)
		{
			path.Clear();
			if (navMap == null)
				return false;
			if (voxelFilter == null)
				return false;

			Transform t = voxelFilter.transform;
			Vector3 half = Vector3.one / 2f;

			Vector3 localPosInMap = t.InverseTransformPoint(position);
			Vector3Int indexPoint = Vector3Int.RoundToInt(localPosInMap);
			path.Add(t.TransformPoint(indexPoint + half));
			Debug.Log($"LocalPos: {localPosInMap} Index: {indexPoint}");
			if (!navMap.TryGetValue(indexPoint, out NavVoxelData currentVoxel))
				return false;

			while (currentVoxel != null)
			{
				if (currentVoxel == null) return true; 
				NavVoxelData nextVoxel = currentVoxel.GetMinimalCostNeighbour();
				if (nextVoxel == null) return true;

				indexPoint = nextVoxel.indexPoint;
				Vector3 worldPoint = t.TransformPoint(indexPoint + half);
				path.Add(worldPoint);

				currentVoxel = nextVoxel;
			}
			return true;
		}
	}
}