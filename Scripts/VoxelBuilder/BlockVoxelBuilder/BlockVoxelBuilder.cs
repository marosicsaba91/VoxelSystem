using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace VoxelSystem
{

[CreateAssetMenu(fileName = "BlockVoxelBuilder", menuName = "VoxelSystem/BlockVoxelBuilder", order = 4)]
public class BlockVoxelBuilder : VoxelBuilder
{

    protected override void BuildMesh(VoxelMap voxelMap, List<Vector3> vertices, List<Vector3> normals, List<Vector2> uv, List<int> triangles)
    {
        
    }

    public override IEnumerable<PaletteItem> GetPaletteItems()
    {
        yield return new() { value = 0, name = "cyan", color = Color.cyan };
        yield return new() { value = 1, name = "Magenta", color = Color.magenta };
        yield return new() { value = 2, name = "Yellow", color = Color.yellow };
    }

    public override int PaletteLength => 3;
}
}