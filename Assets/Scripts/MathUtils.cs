using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MathUtils {
    public static Vector4 Vec3to4(Vector3 vec, float w) {
        return new Vector4(vec.x, vec.y, vec.z, w);
    }

    public static Vector3 Vec4to3(Vector4 vec) {
        return new Vector3(vec.x, vec.y, vec.z);
    }

    public static Vector2 Vec3to2(Vector3 vec) {
        return new Vector2(vec.x, vec.y);
    }

    public static Vector3 HomogenousToCartesian(Vector4 homogenous) {
        return new Vector3(
            homogenous.x / homogenous.w,
            homogenous.y / homogenous.w,
            homogenous.z / homogenous.w);
    }
}
