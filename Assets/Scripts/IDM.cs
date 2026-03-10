using UnityEngine;

public class IDM
{
    readonly float maxAcceleration = 2.5f;
    readonly float targetDeceleration = 1.8f;
    readonly float maxDeceleration = 6.0f;
    readonly float minimumGapBetweenCars = 1.0f;
    readonly float minimumTimeToCar = 1.2f;
    readonly float delta = 4.0f;

    public float CalculateCarAcceleration(
        Car car, 
        float speedLimit,
        float distanceToNextCar,
        float velocityOfNextCar)
    {
        float safeSpeedLimit = Mathf.Max(0.1f, speedLimit);
        float safeDistanceToNextCar = Mathf.Max(0.1f, distanceToNextCar);
        float deltaVelocity = car.velocity - velocityOfNextCar;
        float sStar = minimumGapBetweenCars + car.velocity * minimumTimeToCar +
            (car.velocity * deltaVelocity) / (2 * Mathf.Sqrt(maxAcceleration * targetDeceleration));
        float interactionTerm = maxAcceleration * Mathf.Pow(sStar / safeDistanceToNextCar, 2);
        float freeRoadTerm = maxAcceleration * (1.0f - Mathf.Pow(car.velocity / safeSpeedLimit, delta));
        float acceleration = freeRoadTerm - interactionTerm;
        return Mathf.Clamp(acceleration, -maxDeceleration, maxAcceleration);
    }
}
