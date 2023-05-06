
using UnityEngine;

namespace VoxelSystem
{
	[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
	class PlayModeHandele : MonoBehaviour
	{
		[SerializeField, HideInInspector] MeshFilter meshFilter;
		[SerializeField, HideInInspector] MeshRenderer meshRenderer;
		[SerializeField, HideInInspector] MeshCollider meshCollider;

		PlayModeHandleManager manager;

		bool _putBackToPool = true;

		void OnValidate()
		{
			meshFilter = GetComponent<MeshFilter>();
			meshRenderer = GetComponent<MeshRenderer>();
			meshCollider = GetComponent<MeshCollider>();
		}

		internal void Setup(PlayModeHandleManager manager, Mesh mesh, Material material, Pose pose) 
		{
			this.manager = manager;
			meshFilter.sharedMesh = mesh;
			meshCollider.sharedMesh = mesh;
			meshRenderer.sharedMaterial = material;
			transform.SetPositionAndRotation(pose.position, pose.rotation);
			_putBackToPool = false;
		}

		public void SetMesh(Mesh mesh)
		{
			meshFilter.sharedMesh = mesh;
			meshCollider.sharedMesh = mesh;
		}

		private void LateUpdate()
		{
			if (_putBackToPool)
				manager.PutBack(this);
			else
				_putBackToPool = true;
		}
	}
}
