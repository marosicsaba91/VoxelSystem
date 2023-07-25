using UnityEngine;

namespace VoxelSystem
{
	static class VoxelClipboard
	{
		static VoxelMap _clipboard = null;
		public static VoxelMap ClipboardMap => _clipboard;

		public static bool IsEmpty => _clipboard == null || _clipboard.FullSize == Vector3Int.zero;
		public static bool HaveContent => _clipboard != null && _clipboard.FullSize != Vector3Int.zero;
		public static Vector3Int Size => _clipboard.FullSize;

		public static void Clear() => _clipboard = null;
		public static void Copy(this IVoxelEditor editor) => _clipboard = editor.SeparateSelection();
		public static void Paste(this IVoxelEditor editor)
		{
			VoxelMap map = editor.Map;
			BoundsInt sel = new(editor.Selection.min, Size);
			sel.size = Vector3Int.Min(sel.size, map.FullSize - sel.position);
			editor.Map.CopyFrom(_clipboard, Vector3Int.zero, sel.min, sel.size);
			editor.Selection = sel;
		}
	}
}
