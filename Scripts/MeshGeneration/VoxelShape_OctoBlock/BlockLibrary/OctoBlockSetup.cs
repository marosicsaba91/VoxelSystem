using System;
using System.Collections.Generic;
using System.Linq;
using EasyEditor;
using MUtility;
using UnityEngine;
using UnityEngine.Serialization;
using Utility.SerializableCollection;

namespace VoxelSystem
{

	[Serializable]
	class TransformDirectory : SerializableDictionary<SubVoxelFlags, Transform> { }

	[Serializable]
	struct MeshInfo
	{
		public List<Mesh> meshes;
		public SubVoxelFlags subVoxel;
	}

	public class OctoBlockSetup : MonoBehaviour
	{
		public OctoBlockType blockType;
		[ShowIf(nameof(HaveAxis))] public Axis3D axis = Axis3D.X;

		[Header("Visualisation")]
		[SerializeField, Range(0, 0.5f)]
		float testDistance = 0;

		[FormerlySerializedAs("presentationObjects2")][SerializeField] TransformDirectory transformDictionary = new();
		[SerializeField] OctoBlockLibraryGenerator library;

		[SerializeField] List<MeshInfo> meshInfos;

		bool HaveAxis => blockType.HaveAxis();

		void OnValidate()
		{
			library = GetComponentInParent<OctoBlockLibraryGenerator>();
			if (!blockType.HaveAxis())
				axis = default;
		}

		public void Setup()
		{
			transform.localRotation = Quaternion.identity;
			transform.localScale = Vector3.one;

			if (!blockType.HaveAxis())
				axis = default;

			CleanInternalState();
		}


		void CleanInternalState()
		{
			int allDirectionsCount = SubVoxelUtility.AllSubVoxel.Count;

			// SetupFromMesh presentationObjects list
			for (int i = 0; i < allDirectionsCount; i++)
			{
				SubVoxelFlags voxelDirection = SubVoxelUtility.AllSubVoxel[i];
				if (!transformDictionary.TryGetValue(voxelDirection, out Transform child) || child == null)
				{
					transformDictionary.Remove(voxelDirection);
					child = CreateNewChild(voxelDirection);
					transformDictionary.Add(voxelDirection, child);
				}
				else
				{
					SetupChild(child, voxelDirection);
				}
			}

			// SetupFromMesh list order
			for (int i = 0; i < allDirectionsCount; i++)
			{
				SubVoxelFlags voxelDirection = SubVoxelUtility.AllSubVoxel[i];
				Transform child = transformDictionary[voxelDirection];
				child.SetSiblingIndex(i);
			}

			// Destroy extra children
			for (int i = transform.childCount - 1; i >= allDirectionsCount; i--)
			{
				Transform child = transform.GetChild(i);
				DestroyImmediate(child.gameObject);
			}
		}

		Transform CreateNewChild(SubVoxelFlags d)
		{
			Transform t = new GameObject(d.ToString()).transform;
			SetupChild(t, d);
			t.localRotation = Quaternion.identity;
			t.localScale = Vector3.one;
			return t;
		}

		Vector3 GetChildLocalPosition(SubVoxelFlags dir) => (Vector3)dir.ToVector() * (0.25f + testDistance);

		void SetupChild(Transform t, SubVoxelFlags subVoxel)
		{
			t.SetParent(transform);
			t.localPosition = GetChildLocalPosition(subVoxel);
			t.name = subVoxel.ToString();

			Mesh mesh = TryFindMesh(subVoxel);
			bool isMeshFound = mesh != null;
			if (mesh == null)
				mesh = DefaultOctoBlockInfo.Instance.GetMesh(blockType);

			if (mesh != null)
			{
				if (!t.TryGetComponent(out MeshFilter meshFilter))
					meshFilter = t.gameObject.AddComponent<MeshFilter>();
				meshFilter.sharedMesh = mesh;
			}

			Material material =
				!isMeshFound ? DefaultOctoBlockInfo.Instance.MeshNotFoundMaterial :
				library == null || library.Material == null ? DefaultOctoBlockInfo.Instance.MaterialNotFoundMaterial :
				library.Material;

			if (!t.TryGetComponent(out MeshRenderer meshRenderer))
				meshRenderer = t.gameObject.AddComponent<MeshRenderer>();
			meshRenderer.sharedMaterial = material;
		}

		public Mesh TryFindMesh(SubVoxelFlags subVoxel)
		{
			foreach (MeshInfo meshInfo in meshInfos)
			{
				if (meshInfo.subVoxel.HasFlag(subVoxel))
					return meshInfo.meshes.FirstOrDefault();
			}

			return null;
		}

		public Matrix4x4 GetTransformation(SubVoxelFlags subVoxel)
		{
			if (!transformDictionary.TryGetValue(subVoxel, out Transform child))
				return Matrix4x4.identity;

			return Matrix4x4.TRS(child.localPosition, child.localRotation, child.localScale);
		}
	}
}