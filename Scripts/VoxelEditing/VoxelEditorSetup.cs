using Unity.Scripting.LifecycleManagement;
namespace VoxelSystem
{
	[NoAutoStaticsCleanup]
	static class VoxelEditorSetup
	{
		static VoxelAction _selectedAction = VoxelAction.Attach;
		static VoxelTool _selectedTool = VoxelTool.None;
		static ToolState _toolState = ToolState.None;
		static Voxel _selectedVoxelValue = Voxel.emptyValue;

		public static VoxelTool SelectedTool
		{
			get
			{
				Load();
				return _selectedTool;
			}

			set
			{
				if (value == _selectedTool) return;
				_selectedTool = value;
				Save();
			}
		}
		public static VoxelAction SelectedAction
		{
			get
			{
				Load();
				return _selectedAction;
			}

			set
			{
				if (value == _selectedAction) return;
				_selectedAction = value;
				Save();
			}
		}
		public static ToolState ToolState
		{
			get
			{
				Load();
				return _toolState;
			}

			set
			{
				if (value == _toolState) return;
				_toolState = value;
				Save();
			}
		}
		public static Voxel SelectedVoxelValue
		{
			get
			{
				Load();
				return _selectedVoxelValue;
			}

			set
			{
				if (value == _selectedVoxelValue) return;
				_selectedVoxelValue = value;
				Save();
			}
		}

		public static int SelectedShapeId
		{
			get => SelectedVoxelValue.shapeId;
			set
			{
				_selectedVoxelValue.shapeId = value;
				Save();
			}
		}

		public static byte SelectedMaterialIndex 
		{
			get => SelectedVoxelValue.materialIndex;
			set 
			{
				_selectedVoxelValue.materialIndex = value;
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

		static bool _areToolsLoaded = false;

		static void Load()
		{
			if (_areToolsLoaded) return;

#if UNITY_EDITOR
			if (!UnityEditor.EditorPrefs.HasKey(selectedToolKey)) return;

			_selectedTool = (VoxelTool)UnityEditor.EditorPrefs.GetInt(selectedToolKey, (int)_selectedTool);
			_selectedAction = (VoxelAction)UnityEditor.EditorPrefs.GetInt(selectedActionKey, (int)_selectedAction);
			_toolState = (ToolState)UnityEditor.EditorPrefs.GetInt(toolStateKey, (int)_toolState);

			_selectedVoxelValue.shapeId = UnityEditor.EditorPrefs.GetInt(selectedVoxelValue_ShapeId, _selectedVoxelValue.shapeId);
			_selectedVoxelValue.materialIndex = (byte)UnityEditor.EditorPrefs.GetInt(selectedVoxelVoxel_MaterialIndex, _selectedVoxelValue.materialIndex);
			_selectedVoxelValue.closednessInfo = (byte)UnityEditor.EditorPrefs.GetInt(selectedVoxelVoxel_ClosednessInfo, _selectedVoxelValue.closednessInfo);
			_selectedVoxelValue.extraData = (byte)UnityEditor.EditorPrefs.GetInt(selectedVoxelVoxel_ExtraVoxelData, _selectedVoxelValue.extraData);
#endif
			_areToolsLoaded = true;
		}

		static void Save()
		{
#if UNITY_EDITOR
			UnityEditor.EditorPrefs.SetInt(selectedToolKey, (int)_selectedTool);
			UnityEditor.EditorPrefs.SetInt(selectedActionKey, (int)_selectedAction);
			UnityEditor.EditorPrefs.SetInt(toolStateKey, (int)_toolState);

			UnityEditor.EditorPrefs.SetInt(selectedVoxelValue_ShapeId, _selectedVoxelValue.shapeId);
			UnityEditor.EditorPrefs.SetInt(selectedVoxelVoxel_MaterialIndex, _selectedVoxelValue.materialIndex);
			UnityEditor.EditorPrefs.SetInt(selectedVoxelVoxel_ClosednessInfo, _selectedVoxelValue.closednessInfo);
			UnityEditor.EditorPrefs.SetInt(selectedVoxelVoxel_ExtraVoxelData, _selectedVoxelValue.extraData);
#endif
		}
	}
}