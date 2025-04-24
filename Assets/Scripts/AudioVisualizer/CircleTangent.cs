using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CircleTangent : MonoBehaviour
{
    protected Vector3 GetRotatedTangent(float degree, float radius)
    {
        double angle = degree * Mathf.Deg2Rad;
        float x = radius * (float)System.Math.Sin(angle);
        float z = radius * (float)System.Math.Cos(angle);
        return new Vector3(x, 0, z);
    }

    protected Vector4 FindTangentCircle(Vector4 A, Vector4 B, float degree)
    {
        Vector3 C = GetRotatedTangent(degree, A.w);
        float AB = Mathf.Max(Vector3.Distance(new Vector3(A.x, A.y, A.z), new Vector3(B.x, B.y, B.z)), 0.1f);
        float AC = Vector3.Distance(new Vector3(A.x, A.y, A.z), C);
        float BC = Vector3.Distance(new Vector3(B.x, B.y, B.z), C);
        float angleCAB = ((AC * AC) + (AB * AB) - (BC * BC)) / (2 * AC * AB);
        float r = ((A.w * A.w) - (B.w * B.w) + (AB * AB) - (2 * A.w * AB * angleCAB)) / (2 * (A.w + B.w - AB * angleCAB));
        C = GetRotatedTangent(degree, A.w - r);
        return new Vector4(C.x, C.y, C.z, r);
    }
}
