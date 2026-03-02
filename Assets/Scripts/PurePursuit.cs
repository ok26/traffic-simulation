using System.Collections.Generic;
using UnityEngine;

public class PurePursuit
{
    public float Kdd = 1.0f;
    public float MinLd = 0.1f;
    public float MaxLd = 4.0f;

    public float CalculateSteeringAngle(Car car, List<Vector3> path, int closestPointIndex = -1)
    {
        float ld = Mathf.Clamp(Kdd * car.velocity, MinLd, MaxLd);
        Vector3 targetPoint = closestPointIndex < 0 || closestPointIndex >= path.Count ? 
            Util.GetPointAtDistanceFrom(car.position, path, ld) :
            path[Mathf.Min(path.Count - 1, Mathf.CeilToInt(ld / Constants.pointSpacing) + closestPointIndex)];

        Vector3 localTarget = car.transform.InverseTransformPoint(targetPoint);
        float alpha = Mathf.Atan2(localTarget.x, localTarget.z);
        float steeringAngle = Mathf.Atan(2.0f * car.wheelbase * Mathf.Sin(alpha) / ld);

        return Mathf.Rad2Deg * steeringAngle;
    }
}
