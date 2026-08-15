using Unity.Scripting.LifecycleManagement;
using UnityEngine;

namespace VoxelSystem
{
	[NoAutoStaticsCleanup]
	static class VoxelClipboard
	{
		public static VoxelMap ClipboardMap { get; private set; } = null;

		public static bool IsEmpty => ClipboardMap == null || ClipboardMap.FullSize == Vector3Int.zero;
		public static bool HaveContent => ClipboardMap != null && ClipboardMap.FullSize != Vector3Int.zero;
		public static Vector3Int Size => ClipboardMap.FullSize;

		public static void Clear() => ClipboardMap = null;
		public static void Copy(this IVoxelEditor editor) => ClipboardMap = editor.SeparateSelection();
		public static void Paste(this IVoxelEditor editor)
		{
			VoxelMap map = editor.Map;
			BoundsInt sel = new(editor.Selection.min, Size);
			sel.size = Vector3Int.Min(sel.size, map.FullSize - sel.position);
			editor.Map.CopyFrom(ClipboardMap, Vector3Int.zero, sel.min, sel.size);
			editor.Selection = sel;
		}
	}
}
