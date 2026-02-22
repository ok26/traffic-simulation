using System.Collections.Generic;
using UnityEngine;

public class Car : MonoBehaviour
{
    public RoadNetwork roadNetwork;
    public RoadNode startPoint;
    public RoadNode goal;
    Stack<LaneConnection> roadPath;
    Lane currentLane;
    PurePursuit purePursuit;
    IDM idm;

    public Vector3 position = Vector3.zero;
    public Vector3 direction = Vector3.forward;
    public float velocity = 0.0f;
    public float wheelbase = 1.5f;
    public float steeringAngle = 0.0f;
    public float maxSpeed = 5f;

    void Start()
    {
        if (roadNetwork == null || startPoint == null || goal == null) 
            return;

        roadPath = Astar.findPath(roadNetwork, startPoint, goal);
        if (roadPath.Count == 0)
            return;

        currentLane = roadPath.Peek().from;
        purePursuit = new PurePursuit();
        idm = new IDM();
    }

    void Update()
    {
        if (currentLane == null)
            return;

        steeringAngle = purePursuit.CalculateSteeringAngle(this, currentLane);
        velocity += idm.CalculateCarAcceleration(this, currentLane.segment.speedLimit) * Time.deltaTime;
        
        velocity = Mathf.Clamp(velocity, -maxSpeed, maxSpeed);

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

        transform.position = position;
        if (direction != Vector3.zero) 
            transform.rotation = Quaternion.LookRotation(direction, Vector3.up); 

        // Change
        if (Vector3.Distance(position, currentLane.getEndPos()) < 1.0f)
        {
            if (roadPath.Count == 0)
            {
                Destroy(gameObject);
            }
            else
            {
                // Do some goofy bussiness
                
            }
        }
    }
}
