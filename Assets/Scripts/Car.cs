using System.Collections.Generic;
using UnityEngine;

enum CarState
{
    Lane,
    Intersection,
    Lost
}

public class Car : MonoBehaviour
{
    public RoadNetwork roadNetwork;
    public RoadNode startPoint;
    public RoadNode goal;

    CarNavigator carNavigator;
    PurePursuit purePursuit;
    IDM idm;

    public Vector3 position = Vector3.zero;
    public Vector3 direction = Vector3.forward;
    public float velocity = 0.0f;
    public float wheelbase = 1.5f;
    public float steeringAngle = 0.0f;
    public float maxSpeed = 5f;

    public Vector3 BackBumperPosition => position - (wheelbase / 2) * direction;
    public Vector3 FrontBumberPosition => position + (wheelbase / 2) * direction;
    public bool inIntersection => carNavigator.inIntersection;

    void Start()
    {
        if (roadNetwork == null || startPoint == null || goal == null) 
            return;

        carNavigator = new(this, roadNetwork, startPoint, goal);
        purePursuit = new();
        idm = new();
    }

    void Update()
    {
        if (carNavigator == null || purePursuit == null || idm == null)
            return;

        bool reachedGoal = carNavigator.UpdateState();

        if (reachedGoal)
        {
            Destroy();
            return;
        }

        (float speedLimit, 
        float distanceToNextCar, 
        float velocityOfNextCar) = carNavigator.GetRoadInfo();

        (List<Vector3> upcomingPath,
        int closestPointIndex) = carNavigator.GetUpcomingPath();

        float acceleration = idm.CalculateCarAcceleration(
            this, 
            speedLimit, 
            distanceToNextCar,
            velocityOfNextCar);

        if (float.IsNaN(acceleration) || float.IsInfinity(acceleration))
            acceleration = 0f;

        steeringAngle = purePursuit.CalculateSteeringAngle(this, upcomingPath, closestPointIndex);
        velocity += acceleration * Time.deltaTime;
        velocity = Mathf.Clamp(velocity, 0f, maxSpeed);

        if (float.IsNaN(velocity) || float.IsInfinity(velocity))
            velocity = 0f;

        float deltaRad = Mathf.Deg2Rad * steeringAngle;
        float angularVelocity = (velocity / wheelbase) * Mathf.Tan(deltaRad);

        if (angularVelocity != 0f)
        {
            Quaternion rot = Quaternion.AngleAxis(Mathf.Rad2Deg * angularVelocity * Time.deltaTime, Vector3.up);
            direction = rot * direction;
            direction.y = 0;
            direction.Normalize();
        }

        position += direction * velocity * Time.deltaTime;

        if (!float.IsFinite(position.x) || !float.IsFinite(position.y) || !float.IsFinite(position.z))
        {
            Destroy();
            return;
        }

        transform.position = position;
        if (direction != Vector3.zero) 
            transform.rotation = Quaternion.LookRotation(direction, Vector3.up); 
    }

    public void Destroy()
    {
        carNavigator?.OnCarDestroyed();
        Destroy(gameObject);
    }

    void OnDestroy()
    {
        carNavigator?.OnCarDestroyed();
    }
}