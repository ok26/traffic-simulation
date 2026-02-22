using UnityEngine;

// Only free road term implemented
public class IDM
{
    float maxAcceleration = 2.0f;
    float delta = 4.0f;

    public float CalculateCarAcceleration(Car car, float speedLimit)
    {
        return maxAcceleration * (1.0f - Mathf.Pow(car.velocity / speedLimit, delta));
    }
}
