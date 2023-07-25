using MUtility;
using System.Collections.Generic;
using UnityEngine;
using VoxelSystem;
using System.Linq;

[ExecuteAlways]
[RequireComponent(typeof(VoxelFilter))]
public class BlockMeshGenerator : VoxelMeshGenerator<BlockVoxelPalette, BlockVoxelPaletteItem>
{
	[SerializeField] BlockGenerationSetting blockGenerationSetting;

	readonly List<Dictionary<Vector3Int, Block>> blocksByMaterial = new();

	protected sealed override void BeforeMeshGeneration(VoxelMap map, BlockVoxelPalette voxelPalette)
	{
		benchmarkTimer?.StartModule("Generate Blocks based on VoxelMap"); 

		BlockMapGenerator.blockSetup = blockGenerationSetting;
		BlockMapGenerator.CalculateBlocks(blocksByMaterial, map, voxelPalette.Length);
	}

	protected sealed override void GenerateMeshData(int materialIndex, BlockVoxelPaletteItem paletteItem, List<Vector3> vertices, List<Vector3> normals, List<Vector2> uv, List<int> triangles)
	{
		benchmarkTimer?.StartModule(("Generate Vertex & Triangle data" + materialIndex));

		Dictionary<Vector3Int, Block> blocks = blocksByMaterial[materialIndex];
		VoxelBlockLibrary blockLibrary = paletteItem.blockLibrary;
		Vector3 quarter = Vector3.one * 0.25f;

		foreach (KeyValuePair<Vector3Int, Block> blockWithPosition in blocks)
		{
			Vector3Int subVoxelIndex = blockWithPosition.Key;
			Block block = blockWithPosition.Value;
			if (!blockLibrary.TryGetMesh(block.blockType, block.axis, block.subVoxel, out CustomMesh mesh))
				continue;
			Vector3 offset = block .Center(subVoxelIndex);

			// OPTIMALIZÁLHATÓ:
			vertices.AddRange(mesh.vertices.Select(v => v + offset));
			normals.AddRange(mesh.normals);
			uv.AddRange(mesh.uv);
			triangles.AddRange(mesh.triangles.Select(t => t + vertices.Count - mesh.vertices.Length));
		}
	}

	internal sealed override VoxelMeshGenerator<BlockVoxelPalette, BlockVoxelPaletteItem> AddACopy(GameObject newGO) 
	{
		BlockMeshGenerator newComponent = newGO.AddComponent<BlockMeshGenerator>();
		newComponent.blockGenerationSetting = blockGenerationSetting;

		return newComponent;
	}
}