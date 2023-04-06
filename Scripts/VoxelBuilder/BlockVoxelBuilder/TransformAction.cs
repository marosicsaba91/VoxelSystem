using System; 
using Dir = MUtility.GeneralDirection3D;

namespace VoxelSystem
{
    [Serializable]
    public struct Bool3
    {
        public bool x, y, z;

        public Bool3(bool x, bool y, bool z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public static readonly Bool3 false3 = new(false, false, false);
        public static readonly Bool3 true3 = new(true, true, true);
        public Bool3 RotateX90 => new(x, !z, y);
        public Bool3 RotateY90 => new(z, y, !x);
        public Bool3 RotateZ90 => new(!y, x, z);

        public Bool3 MirrorX => new(!x, y, z);
        public Bool3 MirrorY => new(x, !y, z);
        public Bool3 MirrorZ => new(x, y, !z);
    }

    [Serializable]
    public enum TransformAction
    {
        RotateX90,
        RotateY90,
        RotateZ90,
        RotateX180,
        RotateY180,
        RotateZ180,
        RotateX270,
        RotateY270,
        RotateZ270,
        MirrorX,
        MirrorY,
        MirrorZ,
    }
    
    /*
   public struct SimpleTransformation
   {
       Dir x;
       Dir y;
       Dir z;

       public SimpleTransformation(Dir x, Dir y, Dir z)
       {
           this.x = x;
           this.y = y;
           this.z = z;
       }
       
       Matrix4x4 ToTransformation
       {
           get
           {
               Vector3 i = x.ToVector();
               Vector3 j = y.ToVector();
               Vector3 k = z.ToVector();
               
               return new Matrix4x4(
                   new Vector4(i.x, j.x, k.x, 0),
                   new Vector4(i.y, j.y, k.y, 0),
                   new Vector4(i.z, j.z, k.z, 0),
                   new Vector4(0, 0, 0, 1));
           }
       }

       public static readonly SimpleTransformation identity =
           new(Dir.Right, Dir.Up, Dir.Forward);

       public SimpleTransformation Mirror(Axis3D mirror) => mirror switch
       {
           Axis3D.X => new SimpleTransformation(x.Opposite(), y, z),
           Axis3D.Y => new SimpleTransformation(x, y.Opposite(), z),
           Axis3D.Z => new SimpleTransformation(x, y, z.Opposite()),
           _ => throw new ArgumentOutOfRangeException(nameof(mirror), mirror, null)
       };

       public SimpleTransformation LeftHandRotate(Axis3D axis, int turn90) => new(
           x.LeftHandedRotate(axis, turn90),
           y.LeftHandedRotate(axis, turn90),
           z.LeftHandedRotate(axis, turn90));
   }
   */
}