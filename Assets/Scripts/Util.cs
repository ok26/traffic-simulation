using System.Collections.Generic;
using UnityEngine;

public class Util
{
    static float GetSweepRadians(float startAngle, float endAngle, bool clockwise)
    {
        float ccwDelta = Mathf.DeltaAngle(startAngle * Mathf.Rad2Deg, endAngle * Mathf.Rad2Deg) * Mathf.Deg2Rad;

        if (clockwise)
        {
            return ccwDelta <= 0f ? ccwDelta : ccwDelta - Mathf.PI * 2f;
        }

        return ccwDelta >= 0f ? ccwDelta : ccwDelta + Mathf.PI * 2f;
    }

    static (Vector3 center, float radius, float startAngle, float sweep) ChooseArcGeometry(
        Vector3 from,
        Vector3 to,
        bool clockwise)
    {
        Vector3[] centers =
        {
            new(from.x, 0f, to.z),
            new(to.x, 0f, from.z)
        };

        int bestIndex = 0;
        float bestCost = float.MaxValue;
        float[] candidateRadius = new float[centers.Length];
        float[] candidateStart = new float[centers.Length];
        float[] candidateSweep = new float[centers.Length];

        for (int i = 0; i < centers.Length; i++)
        {
            Vector3 center = centers[i];
            Vector3 fromOffset = from - center;
            Vector3 toOffset = to - center;

            float radiusFrom = fromOffset.magnitude;
            float radiusTo = toOffset.magnitude;
            float radius = (radiusFrom + radiusTo) * 0.5f;

            float startAngle = Mathf.Atan2(fromOffset.z, fromOffset.x);
            float endAngle = Mathf.Atan2(toOffset.z, toOffset.x);
            float sweep = GetSweepRadians(startAngle, endAngle, clockwise);

            float radiusMismatch = Mathf.Abs(radiusFrom - radiusTo);
            float quarterTurnPenalty = Mathf.Abs(Mathf.Abs(sweep) - Mathf.PI * 0.5f);
            float cost = radiusMismatch + quarterTurnPenalty;

            candidateRadius[i] = radius;
            candidateStart[i] = startAngle;
            candidateSweep[i] = sweep;

            if (cost < bestCost)
            {
                bestCost = cost;
                bestIndex = i;
            }
        }

        return (
            centers[bestIndex],
            candidateRadius[bestIndex],
            candidateStart[bestIndex],
            candidateSweep[bestIndex]
        );
    }

    // Only for axis aligned as for now
    public static List<Vector3> GenerateArc(
        Vector3 from,
        Vector3 to,
        bool clockwise)
    {
        var points = new List<Vector3>();

        if (Vector3.Distance(from, to) < 0.001f)
        {
            points.Add(from);
            return points;
        }

        var (center, radius, startAngle, sweep) = ChooseArcGeometry(from, to, clockwise);

        if (radius < 0.001f)
        {
            points.Add(from);
            points.Add(to);
            return points;
        }

        float endAngle = startAngle + sweep;
        float arcLength = radius * Mathf.Abs(sweep);
        int steps = Mathf.CeilToInt(arcLength / Constants.pointSpacing);
        steps = Mathf.Max(1, steps);

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

    public static List<Vector3> GenerateCubicBezier(Vector3 start, Vector3 control1, Vector3 control2, Vector3 end) {
        var points = new List<Vector3>();
        
        int steps = Mathf.CeilToInt(Vector3.Distance(start, end) / Constants.pointSpacing);

        for (int i = 0; i <= steps; i++)
        {
            float t = (float)i / steps;
            Vector3 Q0 = Vector3.Lerp(start, control1, t);
            Vector3 Q1 = Vector3.Lerp(control2, end, t);
            points.Add(Vector3.Lerp(Q0, Q1, t));
        }

        return points;
    }

    public static List<Vector3> GenerateLine(
        Vector3 start,
        Vector3 end)
    {
        var points = new List<Vector3>();

        float length = Vector3.Distance(start, end);
        int steps = Mathf.CeilToInt(length / Constants.pointSpacing);

        for (int i = 0; i <= steps; i++)
        {
            float t = (float)i / steps;
            points.Add(Vector3.Lerp(start, end, t));
        }

        return points;
    }

    public static Vector3 GetPointAtDistanceFrom(
        Vector3 from, 
        List<Vector3> points, 
        float distance) 
        {
        int closestPointIdx = 0;
        float closestDistance = Vector3.Distance(from, points[0]);
        for (int i = 1; i < points.Count; i++)
        {
            Vector3 point = points[i];
            float distanceToPoint = Vector3.Distance(from, point);
            if (distanceToPoint < closestDistance)
            {   
                closestDistance = distanceToPoint;
                closestPointIdx = i;
            }   
        }

        int pointsLookAheadCnt = Mathf.CeilToInt(distance / Constants.pointSpacing);
        int pointAtDistanceIdx = Mathf.Min(points.Count - 1, closestPointIdx + pointsLookAheadCnt);
        return points[pointAtDistanceIdx];
    }
}