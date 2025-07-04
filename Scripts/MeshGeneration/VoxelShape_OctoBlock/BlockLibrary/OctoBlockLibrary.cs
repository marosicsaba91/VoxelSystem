﻿using MUtility;
using JetBrains.Annotations;
using System.Collections.Generic;
using System.Text;
using UnityEngine; 
using EasyEditor;
using ReadOnlyAttribute = EasyEditor.ReadOnlyAttribute;
using VoxelSystem.MeshUtility;

namespace VoxelSystem
{
	[CreateAssetMenu(menuName = EditorConstants.categoryPath + "Octo Block Library", order = EditorConstants.soOrder_OctoBlock)]
	public class OctoBlockLibrary : ScriptableObject
	{
		[Header("Generated Data")]
		// CACHED DATA - GENERATED
		[SerializeField, ReadOnly] List<OctoBlockKey> keys = new(); 
		[SerializeField, ReadOnly] List<MeshBuilder> meshes = new();

		Dictionary<OctoBlockKey, MeshBuilder> _meshCache = new();

		public bool TryGetMesh(OctoBlockType blockType, Axis3D axis, Vector3Int subVoxel, out MeshBuilder mesh)
		{ 
			SubVoxelFlags dir = SubVoxelUtility.FromVector(subVoxel);
			OctoBlockKey blockKey = new(blockType, dir, axis);

			if (_meshCache.IsNullOrEmpty())
			{
				_meshCache = new Dictionary<OctoBlockKey, MeshBuilder>();
				for (int i = 0; i < keys.Count; i++)
					_meshCache.Add(keys[i], meshes[i]);
			}

			return _meshCache.TryGetValue(blockKey, out mesh);
		}

		public void AddBlock(OctoBlockKey key, MeshBuilder mesh)
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
		EasyMessage warning = new(nameof(WarningMessage)) { messageType = MessageType.Warning, messageSize = MessageSize.Normal };

		public string WarningMessage
		{
			get
			{
				StringBuilder stringBuilder = new();
				foreach (OctoBlockType blockType in BlockVoxelUtility.AllBlockType)
				{
					if (blockType == OctoBlockType.BreakPoint)
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

				void MissingKey(OctoBlockType blockType, SubVoxelFlags dir, Axis3D axis)
				{
					OctoBlockKey key = new(blockType, dir, axis);
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