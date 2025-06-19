using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Perception.GroundTruth;
using UnityEngine.Perception.GroundTruth.DataModel;



/// <summary>
/// This script automatically moves and captures images from a camera to objects tagged as "Buoy" or "Gate".
/// This requires for PerceptionCamera to both be attached to the same camera and set to Manual capture mode.
/// </summary>
public class AutoCamera : MonoBehaviour
{
	public float minDistanceAboveFloor = 0.25f;
	public float checkFloorDistance = 4f;
	public Collider boundsCollider;
	public Collider poolCollider;
	public int movementInterval = 10;
	public int targetedCaptures = 50;
	public float minBuoyAutoDistance = 0.7f;
	public float maxBuoyAutoDistance = 5.0f;    // If making a threshold for buoy removal, do 6.0f
	public float minGateAutoDistance = 4.0f;
	public float maxGateAutoDistance = 7.0f;    // If making a threshold for gate removal, do 8.0f

	private GameObject[] targetObjects;
	private PerceptionCamera PC;
	private int framesSinceMovement = 0;
	private int captureCount = 0;
	private int maxCaptures;


	void Start()
	{
		List<GameObject> targets = new List<GameObject>();
		targets.AddRange(GameObject.FindGameObjectsWithTag("Buoy"));
		targets.AddRange(GameObject.FindGameObjectsWithTag("Gate"));
		targetObjects = targets.ToArray();

		maxCaptures = targetObjects.Length * targetedCaptures;

		PC = GetComponent<PerceptionCamera>();
	}


	void Update()
	{
		if (targetObjects.Length == 0)
		{
			Debug.LogWarning("No target objects (Buoy or Gate) found!");
			return;
		}

		if (captureCount >= maxCaptures)
		{
			Debug.Log("Max captures reached. Stopping auto camera.");
			PC.enabled = false;
			PC.captureTriggerMode = CaptureTriggerMode.Scheduled;
			this.enabled = false;
			return;
		}

		if (Time.frameCount % movementInterval != 0)
		{
			framesSinceMovement++;
			if(framesSinceMovement == movementInterval/2)
			{
				PC.RequestCapture();
				captureCount++;
			}
			return;
		}

		bool hasMoved = false;

		while (!hasMoved){
			int targetIndex = captureCount % targetObjects.Length;
			GameObject target = targetObjects[targetIndex];
			Vector3 targetPos = target.transform.position;

			Vector3 randomOffset = Random.onUnitSphere;
			if (target.CompareTag("Buoy"))
			{
				float cameraDistance = Random.Range(minBuoyAutoDistance, maxBuoyAutoDistance);
				randomOffset *= cameraDistance;
			}
			else
			{
				float cameraDistance = Random.Range(minGateAutoDistance, maxGateAutoDistance);
				randomOffset *= cameraDistance;
			}

			Vector3 newCameraPos = targetPos + randomOffset;


			if (boundsCollider.bounds.Contains(newCameraPos) &&
				IsPointAboveFloor(newCameraPos, checkFloorDistance, minDistanceAboveFloor))
			{
				transform.position = newCameraPos;
				transform.LookAt(targetPos);
				framesSinceMovement = 0;
				hasMoved = true;
			}
		}
	}


		/// <summary>
		/// Determines whether the specified point is above the floor mesh within a given distance.
		/// Requires that boundsCollider bottom is just below the pool floor's lowest point.
		/// </summary>
		/// <param name="point">The point in world space to check.</param>
		/// <param name="checkDistance">The maximum distance to check below the point for the floor.</param>
		/// <returns>true if the point is above the floor within the specified distance;  otherwise, false. </returns>
	bool IsPointAboveFloor(Vector3 point, float checkDistance, float minDistance)
	{
		RaycastHit hit;

		if (Physics.Raycast(point, Vector3.down, out hit, checkDistance) && hit.collider == poolCollider)
		{
			float distanceToFloor = hit.distance;
			return distanceToFloor >= minDistance;
		}
		
		return false;
	}
}
