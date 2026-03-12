using System;
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
    public float wheelbase = 1.9f;
    public float steeringAngle = 0.0f;
    public float maxSpeed = 5f;

    private Rigidbody rb;
    private CarPhysicsModel physicsModel;

    public Vector3 BackBumperPosition => position - (wheelbase / 2) * direction;
    public Vector3 FrontBumberPosition => position + (wheelbase / 2) * direction;
    public bool inIntersection => carNavigator.inIntersection;
    public LaneConnection NextConnection => carNavigator.NextConnection;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        physicsModel = GetComponent<CarPhysicsModel>();
        if (physicsModel == null)
            physicsModel = gameObject.AddComponent<CarPhysicsModel>();

        if (physicsModel != null)
            physicsModel.wheelbase = wheelbase;

        if (roadNetwork == null || startPoint == null || goal == null || rb == null) 
            return;

        position = rb.position;
        direction = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
        if (direction.sqrMagnitude < 0.0001f)
            direction = Vector3.forward;
        direction.Normalize();
        velocity = Mathf.Clamp(Vector3.ProjectOnPlane(rb.linearVelocity, Vector3.up).magnitude, 0f, maxSpeed);

        carNavigator = new(this, roadNetwork, startPoint, goal);
        purePursuit = new();
        idm = new();
    }

    void FixedUpdate()
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

        speedLimit = Mathf.Min(speedLimit, maxSpeed);

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

        if (physicsModel != null)
        {
            physicsModel.wheelbase = wheelbase;
            physicsModel.Step(acceleration, steeringAngle);
        }

        if (!float.IsFinite(position.x) || !float.IsFinite(position.y) || !float.IsFinite(position.z))
        {
            Destroy();
            return;
        }

        position = rb.position;

        Vector3 planarForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
        if (planarForward.sqrMagnitude > 0.0001f)
            direction = planarForward.normalized;

        Vector3 planarVelocity = Vector3.ProjectOnPlane(rb.linearVelocity, Vector3.up);
        velocity = Mathf.Clamp(planarVelocity.magnitude, 0f, maxSpeed);
        if (!float.IsFinite(velocity))
            velocity = 0f;

        // transform.position = position;
        // if (direction != Vector3.zero) 
            // transform.rotation = Quaternion.LookRotation(direction, Vector3.up); 
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