using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace TpacTool.Lib.Util
{
    public static class MathUtils
    {
        public static void Decompose(this Quaternion quaternion, out Vector3 axis, out float theta)
        {
            theta = 2f * (float)Math.Acos(quaternion.W);
            if (theta == 0)
            {
                axis = Vector3.UnitZ;
                return;
            }
            var sinTheta = (float)Math.Sin(theta / 2);
            axis = new Vector3(quaternion.X / sinTheta, quaternion.Y / sinTheta, quaternion.Z / sinTheta);
            return;
        }
        public static Vector3 RotateByQuaternion(this Vector3 v, Quaternion quat)
        {
            var q = new Quaternion(v, 0f);
            q = quat * q * Quaternion.Conjugate(quat);
            return new Vector3(q.X, q.Y, q.Z);
        }
    }
}
