using MUtility;

public enum ShapeType
{
	SimpleStair = 0,
	InnerCornerStair = 1,
	OuterCornerStair = 2,
	FullBlock = 3,
}
 

public struct PhysicalVoxelShape
{
	public ShapeType shapeType;       // 2 bits

	public byte levelCount;    // 2 bits   (0=1, 1=2, 2=3, 3=4)
	public byte currentLevel;  // 2 bits   (0=1, 1=2, 2=3, 3=4)
	public uint levelLeight;   // 2 bits   (0=1, 1=2, 2=3, 3=4)

	// For Filled Voxels
	public bool solidTop;
	public bool solidBottom;
	public bool solidLeft;
	public bool solidRight;
	public bool solidForward;
	public bool solidBack;

	// For Sloped Voxels:
	public GeneralDirection3D stairSideUp; // Default is up
	public GeneralDirection3D stairSide1;
	public GeneralDirection3D stairSide2;

	internal static readonly PhysicalVoxelShape full = new()
	{
		shapeType = ShapeType.FullBlock,
		stairSideUp = GeneralDirection3D.Up,
		stairSide1 = GeneralDirection3D.Right,
		stairSide2 = GeneralDirection3D.Forward,
		solidTop = true,
		solidBottom = true,
		solidLeft = true,
		solidRight = true,
		solidForward = true,
		solidBack = true,
		levelCount = 0,
		currentLevel = 0, 
		levelLeight = 0
	};
}