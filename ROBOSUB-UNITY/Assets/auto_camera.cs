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
	public float checkFloorDistance = 4f;
	public Collider boundsCollider;
	public Collider poolCollider;
	public int movementInterval = 10;
	public int targetedCaptures = 90;
	public float minSmallDistance = 0.7f;	// For buoys, rocks, etc.
	public float maxSmallDistance = 4.0f;
	public float minLargeDistance = 4.0f;	// For gates, etc.
	public float maxLargeDistance = 7.0f;
	public int randomSeed = 42;

	private float minDistanceAboveFloor = 0.25f;
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

		Random.InitState(randomSeed);
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

		// Capture halfway through the interval of movement
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
			int targetIndex = captureCount % targetObjects.Length;	// capture all targets in a repeating order
			GameObject target = targetObjects[targetIndex];
			Vector3 targetPos = target.transform.position;

			float cameraDistance = target.CompareTag("Gate")
				? Random.Range(minLargeDistance, maxLargeDistance)
				: Random.Range(minSmallDistance, maxSmallDistance);
			Vector3 randomOffset = Random.onUnitSphere * cameraDistance;
			Vector3 newCameraPos = targetPos + randomOffset;

			if (boundsCollider.bounds.Contains(newCameraPos) &&
				IsPointAboveFloor(newCameraPos))
			{
				transform.position = newCameraPos;
				
				// Randomly uncenter the target
				transform.LookAt(targetPos);
				float distanceToTarget = (float)Vector3.Distance(newCameraPos, targetPos);
				Vector3 randomViewportPoint = new Vector3(Random.Range(0.35f, 0.65f), Random.Range(0.3f, 0.70f), distanceToTarget);
				Vector3 lookOffset = Camera.main.ViewportToWorldPoint(randomViewportPoint);
				transform.LookAt(lookOffset);

				// Hide too far objects
				for (int i = 0; i < targetObjects.Length; i++)
				{
					GameObject obj = targetObjects[i];
					float distanceToCamera = Vector3.Distance(transform.position, obj.transform.position);
					bool tooClose = distanceToCamera < minSmallDistance;
					
					float maxDistance = obj.CompareTag("Gate") ? (maxLargeDistance + 0.5f) : (maxSmallDistance + 0.5f);
					bool withinDistance = distanceToCamera <= maxDistance;

					Vector3 viewportPoint = Camera.main.WorldToViewportPoint(obj.transform.position);
					bool inView = viewportPoint.x >= 0 && viewportPoint.x <= 1 && viewportPoint.y >= 0 && viewportPoint.y <= 1;

					obj.SetActive(withinDistance && inView && !tooClose);
				}

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
		/// <returns>true if the point is above the floor within the specified distance;  otherwise, false. </returns>
	bool IsPointAboveFloor(Vector3 point)
	{
		RaycastHit hit;

		if (Physics.Raycast(point, Vector3.down, out hit, checkFloorDistance) && hit.collider == poolCollider)
		{
			float distanceToFloor = hit.distance;
			return distanceToFloor >= minDistanceAboveFloor;
		}
		
		return false;
	}
}
