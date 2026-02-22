using UnityEngine;

public class PurePursuit
{
    public float Kdd = 1.0f;
    public float minLd = 0.1f;
    public float maxLd = 4.0f;

    public float CalculateSteeringAngle(Car car, Lane lane)
    {
        float ld = Mathf.Clamp(Kdd * car.velocity, minLd, maxLd);
        Vector3 targetPoint = lane.getPointAtDistanceFrom(car.position, ld);

        Vector3 localTarget = car.transform.InverseTransformPoint(targetPoint);
        float alpha = Mathf.Atan2(localTarget.x, localTarget.z);
        float steeringAngle = Mathf.Atan(2.0f * car.wheelbase * Mathf.Sin(alpha) / ld);

        return Mathf.Rad2Deg * steeringAngle;
    }
}
