using System.Collections.Generic;
using UnityEngine;
/*
public class RoadSegment
{
    public List<Vector3> points;
    public float distanceBetweenPoints;
    public float speedLimit = 6.0f;

    public RoadSegment(float distanceBetweenPoints)
    {
        this.points = new List<Vector3>();
        this.distanceBetweenPoints = distanceBetweenPoints;
    }
    
    public RoadSegment(List<Vector3> points, float distanceBetweenPoints)
    {
        this.points = points;
        this.distanceBetweenPoints = distanceBetweenPoints;
    }
    
    public void AddSegment(List<Vector3> points)
    {
        this.points.AddRange(points);
    }

    public Vector3 GetPointAtDistanceFrom(Vector3 from, float distance)
    {
        int closestPointIdx = 0;
        float closestsDistance = Vector3.Distance(from, points[0]);
        for (int i = 1; i < points.Count; i++)
        {
            Vector3 point = points[i];
            float distanceToPoint = Vector3.Distance(from, point);
            if (distanceToPoint < closestsDistance)
            {   
                closestsDistance = distanceToPoint;
                closestPointIdx = i;
            }   
        }

        int pointsLookAheadCnt = Mathf.CeilToInt(distance / distanceBetweenPoints);
        int pointAtDistanceIdx = Mathf.Min(points.Count - 1, closestPointIdx + pointsLookAheadCnt);
        return points[pointAtDistanceIdx];
    }

    public static List<Vector3> GenerateLine(
        Vector3 start,
        Vector3 end,
        float spacing)
    {
        var points = new List<Vector3>();

        float length = Vector3.Distance(start, end);
        int steps = Mathf.CeilToInt(length / spacing);

        for (int i = 0; i <= steps; i++)
        {
            float t = (float)i / steps;
            points.Add(Vector3.Lerp(start, end, t));
        }

        return points;
    }

    public static List<Vector3> GenerateArc(
        Vector3 center,
        float radius,
        float startAngle,
        float endAngle,
        float spacing)
    {
        var points = new List<Vector3>();

        float arcLength = radius * Mathf.Abs(endAngle - startAngle);
        int steps = Mathf.CeilToInt(arcLength / spacing);

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
}
*/