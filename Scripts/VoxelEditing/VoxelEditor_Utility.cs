using MUtility;
using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace VoxelSystem
{
	[Flags]
	public enum RecordType
	{
		Map = 1,
		Transform = 2,
		Editor = 4,
	}

	public static class VoxelEditor_Utility
	{
		public static void RecordForUndo(this IVoxelEditor editor, string message, RecordType record)
		{
#if UNITY_EDITOR
			Object[] objects = editor.Objects(record);

			UnityEditor.Undo.RecordObjects(objects, message);

			foreach (Object obj in objects)
			{
				if (editor.MapContainer is ScriptableObject so)
					UnityEditor.EditorUtility.SetDirty(obj);
			}
#endif
		}


		static Object[] Objects(this IVoxelEditor editor, RecordType record)
		{
			bool recordMap = record.HasFlag(RecordType.Map);
			bool recordTransform = record.HasFlag(RecordType.Transform);
			bool RecordSelection = record.HasFlag(RecordType.Editor);

			int count = (recordMap ? 1 : 0) + (recordTransform ? 1 : 0) + (RecordSelection ? 1 : 0);
			Object[] objects = new Object[count];

			int index = 0;
			if (recordMap)
				objects[index++] = editor.MapContainer;
			if (recordTransform)
				objects[index++] = editor.transform;
			if (RecordSelection)
				objects[index] = editor.EditorObject;

			return objects;
		}



		public static void Turn(this IVoxelEditor editor, Axis3D axis, bool leftHandPositive)
		{
			VoxelMap map = editor.Map;
			Vector3 center = map.FullSize / 2;
			Vector3 worldCenter = editor.transform.TransformPoint(center);

			editor.Map.Turn(axis, leftHandPositive);

			Vector3 newWorldCenter = editor.transform.TransformPoint(center);
			Vector3Int delta = (newWorldCenter - worldCenter).RoundToInt();
			editor.transform.position -= delta;
		}

		// Selection

		public static bool ClearInsideSelection(this IVoxelEditor editor) => FillInsideSelection(editor, IntVoxelUtility.emptyValue);

		public static bool FillInsideSelection(this IVoxelEditor editor) => FillInsideSelection(editor, editor.SelectedMaterialIndex);

		public static bool FillInsideSelection(this IVoxelEditor editor, int paletteIndex)
		{
			VoxelMap map = editor.Map;
			if (!editor.HasSelection())
				return map.SetWhole(paletteIndex);

			bool change = false;
			foreach (Vector3Int coordinate in editor.Selection.WalkThrough())
				change |= map.SetVoxel(coordinate, paletteIndex);

			return change;
		}

		public static ArrayVoxelMap SeparateSelection(this IVoxelEditor voxelEditor)
		{
			if (!voxelEditor.HasSelection())
				return null;

			BoundsInt selection = voxelEditor.Selection;
			ArrayVoxelMap separated = new(selection.size);
			separated.CopyFrom(voxelEditor.Map, selection.min, Vector3Int.zero, selection.size);

			return separated;
		}

		public static bool HasSelection(this IVoxelEditor editor)
		{
			Vector3Int size = editor.Selection.size;
			return size.x >= 0 && size.y >= 0 && size.z >= 0;
		}

		public static void Deselect(this IVoxelEditor editor) =>
			editor.Selection = new BoundsInt(Vector3Int.zero, Vector3Int.one * -1);

		public static void SeparateSelectionToGameObject(this IVoxelEditor voxelEditor)
		{
			Transform original = voxelEditor.transform;
			string name = $"{original.name} - Separated - {voxelEditor.Selection.min}{voxelEditor.Selection.max}";
			GameObject newGO = new(name);
			newGO.transform.SetParent(original.parent);
			newGO.transform.SetSiblingIndex(original.GetSiblingIndex() + 1);

			newGO.transform.position = original.TransformPoint(voxelEditor.Selection.min);
			newGO.transform.localRotation = original.rotation;
			newGO.transform.localScale = original.localScale;

			VoxelObject newMapFilter = newGO.AddComponent<VoxelObject>();
			ArrayVoxelMap map = voxelEditor.SeparateSelection();
			newMapFilter.SetVoxelMap(map);
			VoxelEditor newEditor = newGO.AddComponent<VoxelEditor>();
			newEditor.voxelFilter = newMapFilter;

			foreach (VoxelMeshGenerator generator in newGO.GetComponents<VoxelMeshGenerator>())
				newEditor.meshGenerator = generator.CreateACopy(newGO);

#if UNITY_EDITOR
			UnityEditor.Undo.RegisterCreatedObjectUndo(newGO, "VoxelMap Separated"); 
#endif
		}

		public static void MergeInto(this IVoxelEditor source, IVoxelEditor destination)
		{
			RecordForUndo(destination, "Merge Voxel Maps", RecordType.Map);
			Vector3 sourcePosition = source.transform.position;
			Vector3 offset = destination.transform.transform.InverseTransformPoint(sourcePosition);
			Vector3Int offsetInt = offset.RoundToInt();

			destination.Map.CopyFrom(source.Map, Vector3Int.zero, offsetInt, source.Map.FullSize);
			destination.Map.MapChanged(MapChange.Final);
		}
	}
}
