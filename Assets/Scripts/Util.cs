using System.Collections.Generic;
using UnityEngine;

public class Util
{
    // Only for axis aligned as for now
    public static List<Vector3> GenerateArc(
        Vector3 from,
        Vector3 to,
        bool clockwise)
    {
        var points = new List<Vector3>();

        Vector3 center;
        if (clockwise) center = new Vector3(from.x, 0f, to.z);
        else center = new Vector3(to.x, 0f, from.z);

        float radius = Mathf.Abs(from.x - to.x);

        // Get starting angle
        float startAngle = 0f;
        Vector3 r = from - center;

        if (Mathf.Abs(r.x) > 0.001f)
            startAngle = r.x > 0 ? 0f : Mathf.PI;
        else
            startAngle = r.z > 0 ? Mathf.PI * 0.5f : Mathf.PI * 1.5f;
        float endAngle = clockwise ? 
            endAngle = startAngle - Mathf.PI * 0.5f :
            startAngle + Mathf.PI * 0.5f;

        float arcLength = radius * Mathf.Abs(endAngle - startAngle);
        int steps = Mathf.CeilToInt(arcLength / Consts.pointSpacing);

        for (int i = 0; i <= steps; i++)
        {
            float t = (float)i / steps;
            float angle = Mathf.Lerp(startAngle, endAngle, t);

            float x = center.x + radius * Mathf.Cos(angle);
            float z = center.z + radius * Mathf.Sin(angle);

            points.Add(new Vector3(x, 0, z));
        }

        return points;
    }

    public static List<Vector3> GenerateLine(
        Vector3 start,
        Vector3 end)
    {
        var points = new List<Vector3>();

        float length = Vector3.Distance(start, end);
        int steps = Mathf.CeilToInt(length / Consts.pointSpacing);

        for (int i = 0; i <= steps; i++)
        {
            float t = (float)i / steps;
            points.Add(Vector3.Lerp(start, end, t));
        }

        return points;
    }
}