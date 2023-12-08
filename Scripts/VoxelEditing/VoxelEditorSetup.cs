namespace VoxelSystem
{
	static class VoxelEditorSetup
	{
		static VoxelAction selectedAction = VoxelAction.Attach;
		static VoxelTool selectedTool = VoxelTool.None;
		static ToolState toolState = ToolState.None;
		static Voxel selectedVoxelValue = Voxel.emptyValue;

		public static VoxelTool SelectedTool
		{
			get
			{
				Load();
				return selectedTool;
			}

			set
			{
				if (value == selectedTool) return;
				selectedTool = value;
				Save();
			}
		}
		public static VoxelAction SelectedAction
		{
			get
			{
				Load();
				return selectedAction;
			}

			set
			{
				if (value == selectedAction) return;
				selectedAction = value;
				Save();
			}
		}
		public static ToolState ToolState
		{
			get
			{
				Load();
				return toolState;
			}

			set
			{
				if (value == toolState) return;
				toolState = value;
				Save();
			}
		}
		public static Voxel SelectedVoxelValue
		{
			get
			{
				Load();
				return selectedVoxelValue;
			}

			set
			{
				if (value == selectedVoxelValue) return;
				selectedVoxelValue = value;
				Save();
			}
		}

		public static int SelectedShapeId
		{
			get => SelectedVoxelValue.shapeId;
			set
			{
				selectedVoxelValue.shapeId = value;
				Save();
			}
		}

		public static byte SelectedMaterialIndex 
		{
			get => SelectedVoxelValue.materialIndex;
			set 
			{
				selectedVoxelValue.materialIndex = value;
				Save();
			}
		}

		const string selectedToolKey = "VoxelEditor_SelectedTool";
		const string selectedActionKey = "VoxelEditor_SelectedAction";
		const string toolStateKey = "VoxelEditor_ToolState";

		const string selectedVoxelValue_ShapeId = "VoxelEditor_SelectedVoxel_ShapeId";
		const string selectedVoxelVoxel_MaterialIndex = "VoxelEditor_SelectedVoxel_MaterialIndex";
		const string selectedVoxelVoxel_ClosednessInfo = "VoxelEditor_SelectedVoxel_ClosednessInfo";
		const string selectedVoxelVoxel_ExtraVoxelData = "VoxelEditor_SelectedVoxel_ExtraVoxelData";	

		static bool areToolsLoaded = false;

		static void Load()
		{
			if (areToolsLoaded) return;

#if UNITY_EDITOR
			if (!UnityEditor.EditorPrefs.HasKey(selectedToolKey)) return;

			selectedTool = (VoxelTool)UnityEditor.EditorPrefs.GetInt(selectedToolKey, (int)selectedTool);
			selectedAction = (VoxelAction)UnityEditor.EditorPrefs.GetInt(selectedActionKey, (int)selectedAction);
			toolState = (ToolState)UnityEditor.EditorPrefs.GetInt(toolStateKey, (int)toolState);

			selectedVoxelValue.shapeId = UnityEditor.EditorPrefs.GetInt(selectedVoxelValue_ShapeId, selectedVoxelValue.shapeId);
			selectedVoxelValue.materialIndex = (byte)UnityEditor.EditorPrefs.GetInt(selectedVoxelVoxel_MaterialIndex, selectedVoxelValue.materialIndex);
			selectedVoxelValue.closednessInfo = (byte)UnityEditor.EditorPrefs.GetInt(selectedVoxelVoxel_ClosednessInfo, selectedVoxelValue.closednessInfo);
			selectedVoxelValue.extraVoxelData = (byte)UnityEditor.EditorPrefs.GetInt(selectedVoxelVoxel_ExtraVoxelData, selectedVoxelValue.extraVoxelData);
#endif
			areToolsLoaded = true;
		}

		static void Save()
		{
#if UNITY_EDITOR
			UnityEditor.EditorPrefs.SetInt(selectedToolKey, (int)selectedTool);
			UnityEditor.EditorPrefs.SetInt(selectedActionKey, (int)selectedAction);
			UnityEditor.EditorPrefs.SetInt(toolStateKey, (int)toolState);

			UnityEditor.EditorPrefs.SetInt(selectedVoxelValue_ShapeId, selectedVoxelValue.shapeId);
			UnityEditor.EditorPrefs.SetInt(selectedVoxelVoxel_MaterialIndex, selectedVoxelValue.materialIndex);
			UnityEditor.EditorPrefs.SetInt(selectedVoxelVoxel_ClosednessInfo, selectedVoxelValue.closednessInfo);
			UnityEditor.EditorPrefs.SetInt(selectedVoxelVoxel_ExtraVoxelData, selectedVoxelValue.extraVoxelData);
#endif
		}
	}
}