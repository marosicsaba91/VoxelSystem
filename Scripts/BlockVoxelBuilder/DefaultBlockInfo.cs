using System;
using UnityEngine;
using Utility.SerializableCollection;

namespace VoxelSystem
{

	[Serializable]
	class MeshDictionary : SerializableDictionary<BlockType, Mesh> { }


	[CreateAssetMenu(menuName = "Voxel System/Default Block Info")]
	class DefaultBlockInfo : ScriptableObject
	{
		[SerializeField]  SharedArrayVoxelMap standardMap;
		[SerializeField]  Material basicMaterial;
		[SerializeField]  Material mesNotFoundMaterial;
		[SerializeField]  Material materialNotFoundMaterial;

		[SerializeField] MeshDictionary meshDictionary = new();


		static DefaultBlockInfo _instance;
		public static DefaultBlockInfo Instance
		{
			get
			{
				if (_instance == null)
					_instance = Resources.Load<DefaultBlockInfo>("DefaultBlockInfo");

				if (_instance == null)
					Debug.LogError("DefaultBlockInfo not found");

				return _instance;
			}
		}

		public Mesh GetMesh(BlockType blockType) => meshDictionary[blockType];

		public Material BasicMaterial => basicMaterial;
		public Material TestMaterial => basicMaterial;
		public Material MeshNotFoundMaterial => mesNotFoundMaterial;
		public Material MaterialNotFoundMaterial => materialNotFoundMaterial;

	}
}