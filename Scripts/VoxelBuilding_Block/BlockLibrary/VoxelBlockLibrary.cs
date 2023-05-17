using JetBrains.Annotations;
using MUtility;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace VoxelSystem
{
	[CreateAssetMenu(menuName = "Voxel System/Block Library")]
	public class VoxelBlockLibrary : ScriptableObject
	{
		[Header("Generated Data")]
		// CACHED DATA - GENERATED
		[SerializeField, ReadOnly] List<BlockKey> keys = new();
		[SerializeField, ReadOnly] List<CustomMesh> meshes = new();

		Dictionary<BlockKey, CustomMesh> _meshCache = new();

		public bool TryGetMesh(Block block, out CustomMesh mesh)
		{
			BlockType blockType = block.blockType;
			Axis3D axis = block.axis;
			SubVoxelFlags dir = SubVoxelUtility.FromVector(block.subVoxel);
			BlockKey blockKey = new(blockType, dir, axis);

			if (_meshCache.IsNullOrEmpty())
			{
				_meshCache = new Dictionary<BlockKey, CustomMesh>();
				for (int i = 0; i < keys.Count; i++)
					_meshCache.Add(keys[i], meshes[i]);
			}

			return _meshCache.TryGetValue(blockKey, out mesh);
		}

		public void AddBlock(BlockKey key, CustomMesh mesh)
		{
			keys.Add(key);
			meshes.Add(mesh);
			_meshCache.Add(key, mesh);
		}

		public void Clear()
		{
			keys.Clear();
			meshes.Clear();
			_meshCache.Clear();
			MakeDirty();
		}

		[SerializeField, UsedImplicitly]
		DisplayMessage warning = new(nameof(WarningMessage), true)
		{ messageType = MessageType.Warning, messageSize = MessageSize.Normal };

		public string WarningMessage
		{
			get
			{
				StringBuilder stringBuilder = new();
				foreach (BlockType blockType in BlockVoxelUtility.AllBlockType)
				{
					if (blockType == BlockType.BreakPoint)
						continue;

					foreach (SubVoxelFlags dir in SubVoxelUtility.AllSubVoxel)
					{
						if (blockType.HaveAxis())
						{
							foreach (Axis3D axis in BlockVoxelUtility.AllAxis)
								MissingKey(blockType, dir, axis);
						}
						else
							MissingKey(blockType, dir, default);
					}
				}

				return stringBuilder.ToString();

				void MissingKey(BlockType blockType, SubVoxelFlags dir, Axis3D axis)
				{
					BlockKey key = new(blockType, dir, axis);
					if (!keys.Contains(key))
					{
						stringBuilder.Append("Missing: ");
						key.AppendTo(stringBuilder);
						stringBuilder.AppendLine();
					}
				}
			}
		}


		public void MakeDirty()
		{
#if UNITY_EDITOR
			UnityEditor.EditorUtility.SetDirty(this);
#endif
		}
	}
}