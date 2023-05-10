using MUtility;
using System;
using System.Linq;
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

			// Debug.Log($"RecordForUndo: {string.Join(", ", objects.Select(o => o.name + o.GetType().ToString()))}");	
			if (record.HasFlag(RecordType.Map))
			{
				string guid = editor.Map.UniqueID;
				message += VoxelMap.undoGuidString + guid;
			}

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
			var objects = new Object[count];

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

		public static bool FillInsideSelection(this IVoxelEditor editor) => FillInsideSelection(editor, editor.SelectedPaletteIndex);

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
			var separated = new ArrayVoxelMap(selection.size);
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
	}
}
