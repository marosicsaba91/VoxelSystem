using MUtility;
using System;
using UnityEngine;
using VoxelSystem;

// [CreateAssetMenu]
internal class VoxelEditorSettings : ScriptableObject
{

	static VoxelEditorSettings instance = null;
	public static VoxelEditorSettings Instance
	{
		get
		{
			if (instance == null)
				instance = MUtility.ScriptableObjectUtility.GetFromResources<VoxelEditorSettings>();

			return instance;
		}
	}

	public Texture attachVoxelActionIcon;
	public Texture eraseVoxelActionIcon;
	public Texture recolorVoxelActionIcon;
	public Texture overWriteVoxelActionIcon;

	public Texture attachBoxVoxelToolIcon;
	public Texture eraseBoxVoxelToolIcon;
	public Texture recolorBox_VoxelToolIcon;
	public Texture overWriteBoxVoxelToolIcon;

	public Texture attachFaceVoxelToolIcon;
	public Texture eraseFaceVoxelToolIcon;
	public Texture recolorFaceVoxelToolIcon;
	public Texture overWriteFaceVoxelToolIcon;

	public Texture attachPaintBucketVoxelToolIcon;
	public Texture erasePaintBucketVoxelToolIcon;
	public Texture recolorPaintBucketVoxelToolIcon;
	public Texture overWritePaintBucketVoxelToolIcon;


	public Texture addSelectVoxelToolIcon;
	public Texture removeSelectVoxelToolIcon;
	public Texture colorSelectVoxelToolIcon;
	public Texture overWriteSelectVoxelToolIcon;

	public Texture selectVoxelToolIcon;

	public Texture moveVoxelToolIcon;
	public Texture rotateVoxelToolIcon;
	public Texture mirrorVoxelToolIcon;
	public Texture resizeCanvasVoxelToolIcon;
	public Texture resizeVoxelToolIcon;
	public Texture repeatVoxelToolIcon;

	public Texture moveAttachVoxelToolIcon;
	public Texture rotateAttachVoxelToolIcon;
	public Texture mirrorAttachVoxelToolIcon;
	public Texture resizeAttachVoxelToolIcon;
	public Texture repeatAttachVoxelToolIcon;

	public Texture moveOverWriteVoxelToolIcon;
	public Texture rotateOverWriteVoxelToolIcon;
	public Texture mirrorOverWriteVoxelToolIcon;
	public Texture resizeOverWriteVoxelToolIcon;
	public Texture repeatOverWriteVoxelToolIcon;

	public Texture colorPickerVoxelToolIcon;

	public Texture lockOffIcon;
	public Texture lockOnIcon;

	[Space]
	public Texture2D selectedButton;
	public Texture2D selectedButtonAttach;
	public Texture2D selectedButtonErase;
	public Texture2D selectedButtonOverWrite;
	public Texture2D selectedButtonRecolor;

	internal Texture GetActionIcon(VoxelAction action) => action switch
	{
		VoxelAction.Overwrite => overWriteVoxelActionIcon,
		VoxelAction.Attach => attachVoxelActionIcon,
		VoxelAction.Erase => eraseVoxelActionIcon,
		VoxelAction.Repaint => recolorVoxelActionIcon,
		VoxelAction.RepaintMaterialOnly => recolorVoxelActionIcon,
		VoxelAction.RepaintShapeOnly => recolorVoxelActionIcon,
		_ => throw new ArgumentOutOfRangeException(nameof(action), action, null)
	};

	internal Texture GetToolIcon(VoxelTool tool) => tool switch
	{
		VoxelTool.Move => moveVoxelToolIcon,
		VoxelTool.Turn => rotateVoxelToolIcon,
		VoxelTool.Mirror => mirrorVoxelToolIcon,
		VoxelTool.Resize => resizeVoxelToolIcon,
		VoxelTool.Repeat => repeatVoxelToolIcon,
		VoxelTool.ResizeCanvas => resizeCanvasVoxelToolIcon,
		VoxelTool.MaterialPicker => colorPickerVoxelToolIcon,
		VoxelTool.ShapePicker => colorPickerVoxelToolIcon,
		VoxelTool.Select => selectVoxelToolIcon,

		VoxelTool.Box => overWriteBoxVoxelToolIcon,
		VoxelTool.Face => overWriteFaceVoxelToolIcon,
		VoxelTool.FloodFill => overWriteFaceVoxelToolIcon,

		VoxelTool.None => null,
		_ => null,
	};

	internal Texture GetToolIcon(VoxelTool tool, VoxelAction action)
	{

		if (action is VoxelAction.RepaintShapeOnly or VoxelAction.RepaintMaterialOnly)
			action = VoxelAction.Repaint;


		return tool switch
		{
			VoxelTool.Box => action switch
			{
				VoxelAction.Attach => attachBoxVoxelToolIcon,
				VoxelAction.Erase => eraseBoxVoxelToolIcon,
				VoxelAction.Repaint => recolorBox_VoxelToolIcon,
				VoxelAction.Overwrite => overWriteBoxVoxelToolIcon,
				_ => null
			},
			VoxelTool.Face => action switch
			{
				VoxelAction.Attach => attachFaceVoxelToolIcon,
				VoxelAction.Erase => eraseFaceVoxelToolIcon,
				VoxelAction.Repaint => recolorFaceVoxelToolIcon,
				VoxelAction.Overwrite => overWriteFaceVoxelToolIcon,
				_ => null
			},
			VoxelTool.FloodFill => action switch
			{
				VoxelAction.Attach => attachPaintBucketVoxelToolIcon,
				VoxelAction.Erase => erasePaintBucketVoxelToolIcon,
				VoxelAction.Repaint => recolorPaintBucketVoxelToolIcon,
				VoxelAction.Overwrite => overWritePaintBucketVoxelToolIcon,
				_ => null
			},
			VoxelTool.Move => action switch
			{
				VoxelAction.Attach => moveAttachVoxelToolIcon,
				VoxelAction.Overwrite => moveOverWriteVoxelToolIcon,
				_ => null,
			},
			VoxelTool.Turn => action switch
			{
				VoxelAction.Attach => rotateAttachVoxelToolIcon,
				VoxelAction.Overwrite => rotateOverWriteVoxelToolIcon,
				_ => null,
			},
			VoxelTool.Mirror => action switch
			{
				VoxelAction.Attach => mirrorAttachVoxelToolIcon,
				VoxelAction.Overwrite => mirrorOverWriteVoxelToolIcon,
				_ => null,
			},
			VoxelTool.Resize => action switch
			{
				VoxelAction.Attach => resizeAttachVoxelToolIcon,
				VoxelAction.Overwrite => resizeOverWriteVoxelToolIcon,
				_ => null,
			},
			VoxelTool.Repeat => action switch
			{
				VoxelAction.Attach => repeatAttachVoxelToolIcon,
				VoxelAction.Overwrite => repeatOverWriteVoxelToolIcon,
				_ => null,
			},

			VoxelTool.ResizeCanvas => resizeCanvasVoxelToolIcon,
			VoxelTool.MaterialPicker => colorPickerVoxelToolIcon,
			VoxelTool.ShapePicker => colorPickerVoxelToolIcon,
			VoxelTool.Select => selectVoxelToolIcon,
			VoxelTool.None => null,
			_ => null,

		};
	}
}
