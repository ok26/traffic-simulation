using System.Collections.Generic;
using UnityEngine;

public class TrafficController : MonoBehaviour
{
    public RoadNetwork roadNetwork;
    public GameObject carPrefab;
    public float spawnInterval = 3.0f;

    private float timer = 0f;

    private (int startId, int goalId)[] spawnConfigs = new (int, int)[]
    {
        (0, 1),
        (1, 0),
        (2, 3),
        (3, 2),
        (0, 2),
        (1, 3),
        (2, 0),
        (3, 1),
        (0, 3),
        (1, 2),
        (2, 1),
        (3, 0),
    };

    void Update()
    {
        if (roadNetwork == null || carPrefab == null)
            return;

        timer += Time.deltaTime;

        if (timer >= spawnInterval)
        {
            timer = 0f;
            SpawnCar();
        }
    }

    void SpawnCar()
    {
        int randomIndex = Random.Range(0, spawnConfigs.Length);
        var (startId, goalId) = spawnConfigs[randomIndex];

        RoadNode startNode = roadNetwork.GetNodeById(startId);   
        RoadNode goalNode = roadNetwork.GetNodeById(goalId);

        if (startNode == null || goalNode == null)
        {
            Debug.LogWarning($"Failed to find nodes: start={startId}, goal={goalId}");
            return;
        }

        List<Lane> outgoingLanes = roadNetwork.getOutgoingLanes(startNode);
        if (outgoingLanes.Count == 0)
        {
            Debug.LogWarning($"No outgoing lanes from node {startId}");
            return;
        }

        Lane startLane = outgoingLanes[0];
        Vector3 spawnPosition = startLane.points[0];
        
        Vector3 laneDirection = Vector3.forward;
        if (startLane.points.Count >= 2)
        {
            laneDirection = (startLane.points[1] - startLane.points[0]).normalized;
        }

        Quaternion spawnRotation = Quaternion.LookRotation(laneDirection, Vector3.up);
        GameObject carObj = Instantiate(carPrefab, spawnPosition, spawnRotation);
        Car car = carObj.GetComponent<Car>();

        if (car == null)
        {
            Debug.LogWarning("Car prefab does not have a Car component");
            Destroy(carObj);
            return;
        }

        car.roadNetwork = roadNetwork;
        car.startPoint = startNode;
        car.goal = goalNode;
        car.position = spawnPosition;
        car.direction = laneDirection;
    }
}
