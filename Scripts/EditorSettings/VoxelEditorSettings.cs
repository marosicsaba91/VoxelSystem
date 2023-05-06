using MUtility;
using System;
using UnityEngine;
using VoxelSystem;

[CreateAssetMenu]
internal class VoxelEditorSettings : ScriptableObject
{

	static VoxelEditorSettings instance = null;
	public static VoxelEditorSettings Instance
	{
		get
		{
			if (instance == null)
				instance = ScriptableObjectUtility.GetFromResources<VoxelEditorSettings>();
			
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

	public Texture colorPickerVoxelToolIcon;

	public Texture lockOffIcon;
	public Texture lockOnIcon;

	internal Texture GetActionIcon(VoxelAction action) => action switch
	{
		VoxelAction.Attach => attachVoxelActionIcon,
		VoxelAction.Erase => eraseVoxelActionIcon,
		VoxelAction.Repaint => recolorVoxelActionIcon,
		VoxelAction.Overwrite => overWriteVoxelActionIcon,
		_ => throw new ArgumentOutOfRangeException(nameof(action), action, null)
	};

	internal Texture GetToolIcon(VoxelTool tool, VoxelAction action = VoxelAction.Overwrite) => tool switch
	{
		VoxelTool.Box => action switch
		{
			VoxelAction.Attach => attachBoxVoxelToolIcon,
			VoxelAction.Erase => eraseBoxVoxelToolIcon,
			VoxelAction.Repaint => recolorBox_VoxelToolIcon,
			VoxelAction.Overwrite => overWriteBoxVoxelToolIcon,
			_ => throw new ArgumentOutOfRangeException(nameof(action), action, null)
		},
		VoxelTool.Face => action switch
		{
			VoxelAction.Attach => attachFaceVoxelToolIcon,
			VoxelAction.Erase => eraseFaceVoxelToolIcon,
			VoxelAction.Repaint => recolorFaceVoxelToolIcon,
			VoxelAction.Overwrite => overWriteFaceVoxelToolIcon,
			_ => throw new ArgumentOutOfRangeException(nameof(action), action, null)
		},
		VoxelTool.FloodFill => action switch
		{
			VoxelAction.Attach => attachPaintBucketVoxelToolIcon,
			VoxelAction.Erase => erasePaintBucketVoxelToolIcon,
			VoxelAction.Repaint => recolorPaintBucketVoxelToolIcon,
			VoxelAction.Overwrite => overWritePaintBucketVoxelToolIcon,
			_ => throw new ArgumentOutOfRangeException(nameof(action), action, null)
		},
		VoxelTool.Select => selectVoxelToolIcon,

		//VoxelTool.Select => action switch
		//{
		//	VoxelAction.Attach => addSelectVoxelToolIcon,
		//	VoxelAction.Erase => removeSelectVoxelToolIcon,
		//	VoxelAction.Repaint => colorSelectVoxelToolIcon,
		//	VoxelAction.Overwrite => overWriteSelectVoxelToolIcon,
		//	_ => throw new ArgumentOutOfRangeException(nameof(action), action, null)
		//},	

		VoxelTool.Turn => rotateVoxelToolIcon,
		VoxelTool.Mirror => mirrorVoxelToolIcon,
		VoxelTool.ResizeCanvas => resizeCanvasVoxelToolIcon,	
		VoxelTool.Resize => resizeVoxelToolIcon,
		VoxelTool.Repeat => repeatVoxelToolIcon,
		VoxelTool.ColorPicker => colorPickerVoxelToolIcon,
		VoxelTool.Move => moveVoxelToolIcon,
		VoxelTool.None => null,
		_ => throw new ArgumentOutOfRangeException(nameof(tool), tool, null)

	};
}
