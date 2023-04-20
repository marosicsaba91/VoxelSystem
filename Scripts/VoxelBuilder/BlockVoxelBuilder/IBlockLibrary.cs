using MUtility;

namespace VoxelSystem
{
    public interface IBlockLibrary
    {
        bool TryGetMesh(Block block, out CustomMesh mesh, BenchmarkTimer benchmarkTimer = null);
    }
}