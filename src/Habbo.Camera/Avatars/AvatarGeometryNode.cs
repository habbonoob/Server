namespace Habbo.Camera.Avatars;

/// <summary>A point in the avatar's geometry with a radius: a body part, or an item (a part type) within one. How far it
/// is from the camera once the avatar is turned decides what is drawn over what.</summary>
public sealed record AvatarGeometryNode(string Id, double X, double Y, double Z, double Radius)
{
    /// <summary>The distance from the camera (on the z axis) after turning the avatar by the angle, in degrees, about its
    /// vertical axis (Matrix4x4.getYRotationMatrix). A node at the origin is never turned, as Node3D leaves it.</summary>
    public double Distance(double angle, double cameraZ)
    {
        var z = 0d;
        if (X != 0 || Y != 0 || Z != 0)
        {
            var radians = angle * 3.141592653589793 / 180;
            z = X * Math.Sin(radians) + Z * Math.Cos(radians);
        }
        return Math.Min(Math.Abs(cameraZ - z - Radius), Math.Abs(cameraZ - z + Radius));
    }
}
