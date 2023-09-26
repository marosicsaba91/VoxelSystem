using System;
using UnityEngine;
using Utility.SerializableCollection;

namespace VoxelSystem
{

	[Serializable]
	class MeshDictionary : SerializableDictionary<OctoBlockType, Mesh> { }

	[CreateAssetMenu(menuName = EditorConstants.categoryPath + "Default Oct Block Info", order = EditorConstants.soOrder_OctoBlock)]
	class DefaultOctoBlockInfo : ScriptableObject
	{
		[SerializeField] SharedArrayVoxelMap standardMap;
		[SerializeField] Material basicMaterial;
		[SerializeField] Material mesNotFoundMaterial;
		[SerializeField] Material materialNotFoundMaterial;

		[SerializeField] MeshDictionary meshDictionary = new();


		static DefaultOctoBlockInfo _instance;
		public static DefaultOctoBlockInfo Instance
		{
			get
			{
				if (_instance == null)
					_instance = Resources.Load<DefaultOctoBlockInfo>("DefaultBlockInfo");

				if (_instance == null)
					Debug.LogError("DefaultBlockInfo not found");

				return _instance;
			}
		}

		public Mesh GetMesh(OctoBlockType blockType) => meshDictionary[blockType];

		public Material BasicMaterial => basicMaterial;
		public Material TestMaterial => basicMaterial;
		public Material MeshNotFoundMaterial => mesNotFoundMaterial;
		public Material MaterialNotFoundMaterial => materialNotFoundMaterial;

	}
}