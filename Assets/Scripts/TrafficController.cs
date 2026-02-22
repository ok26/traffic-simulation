using UnityEngine;
/*
public class TrafficController : MonoBehaviour
{
    [SerializeField] private RoadNetwork roadNetwork;
    [SerializeField] private GameObject carPrefab;
    [SerializeField] private float spawnIntervalSeconds = 2.0f;
    [SerializeField] private int maxCars = 10;

    private float spawnTimer = 0.0f;
    private int spawnedCars = 0;

    void Update()
    {
        if (!CanSpawn())
            return;

        spawnTimer += Time.deltaTime;
        if (spawnTimer >= spawnIntervalSeconds)
        {
            spawnTimer = 0.0f;
            SpawnCarAtRoot();
        }
    }

    private bool CanSpawn()
    {
        if (roadNetwork == null || carPrefab == null)
            return false;

        if (spawnedCars >= maxCars)
            return false;

        if (roadNetwork.root.outgoing == null || roadNetwork.root.outgoing.Count == 0)
            return false;

        RoadSegment segment = roadNetwork.root.outgoing[0].roadSegment;
        return segment != null && segment.points != null && segment.points.Count > 0;
    }

    private void SpawnCarAtRoot()
    {
        RoadSegment segment = roadNetwork.root.outgoing[0].roadSegment;
        Vector3 spawnPosition = segment.points[0];
        Quaternion spawnRotation = Quaternion.identity;

        if (segment.points.Count > 1)
        {
            Vector3 direction = (segment.points[1] - segment.points[0]).normalized;
            if (direction.sqrMagnitude > 0.0f)
                spawnRotation = Quaternion.LookRotation(direction, Vector3.up);
        }

        GameObject carObject = Instantiate(carPrefab, spawnPosition, spawnRotation);
        Car car = carObject.GetComponent<Car>();
        if (car != null)
        {
            car.position = spawnPosition;
            car.direction = carObject.transform.forward;
            car.roadNetwork = roadNetwork;
        }

        spawnedCars += 1;
    }
}
*/