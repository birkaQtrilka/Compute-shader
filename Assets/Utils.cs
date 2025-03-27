using System;
using UnityEngine;

public static class Utils
{
    public static void Limit(this ref Vector3 vec, float limit)
    {
        float l = vec.magnitude;
        if (l > limit && l != 0)
            vec =  (vec / l) * limit;
    }

    public static void SetLength(this ref Vector3 vec, float length)
    {
        float l = vec.magnitude;
        if (l == 0) return;
        vec = (vec / l) * length;
    }
    public static float Lerp(float a, float b, float t)
    {
        return (b - a) * t + a;
    }
}
