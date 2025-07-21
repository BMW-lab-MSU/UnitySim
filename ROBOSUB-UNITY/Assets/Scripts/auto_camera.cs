using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Perception.GroundTruth;
using UnityEngine.Perception.GroundTruth.DataModel;



/// <summary>
/// This script automatically moves and captures images from a camera to objects tagged as "Small" or "Large".
/// This requires for PerceptionCamera to both be attached to the same camera and set to Manual capture mode.
/// </summary>
public class AutoCamera : MonoBehaviour
{
	public float checkFloorDistance = 4f;
	public Collider boundsCollider;
	public Collider poolCollider;
	public int movementInterval = 10;
	public int targetedCaptures = 90;
	public float minSmallDistance = 0.7f;   // For buoys, rocks, etc.
	public float maxSmallDistance = 4.0f;
	public float minLargeDistance = 4.0f;   // For gates, etc.
	public float maxLargeDistance = 7.0f;
	public int randomSeed = 42;
	public string posesFile = "C:\\Users\\sterl\\reu\\UnitySim\\poses.json";
	public bool useReplayMode = false;


	private float minDistanceAboveFloor = 0.25f;
	private GameObject[] targetObjects;
	private PerceptionCamera PC;
	private int framesSinceMovement = 0;
	private int captureCount = 0;
	private int maxCaptures;

	private List<CameraPose> savedPoses = new List<CameraPose>();
	private int poseIndex = 0;
	private System.Random rng;


	void Start()
	{
		rng = new System.Random(randomSeed);
		if (useReplayMode)
		{
			if (File.Exists(posesFile))
			{
				string json = File.ReadAllText(posesFile);
				savedPoses = JsonUtility.FromJson<CameraPoseList>(json).poses;
				Debug.Log($"Loaded {savedPoses.Count} camera poses from {posesFile}");
			}
			else
			{
				Debug.LogError("Replay mode enabled but camera poses file not found!");
				enabled = false;
			}
		}

		List<GameObject> targets = new List<GameObject>();
		targets.AddRange(GameObject.FindGameObjectsWithTag("Small"));
		targets.AddRange(GameObject.FindGameObjectsWithTag("Large"));
		targetObjects = targets.ToArray();

		maxCaptures = targetObjects.Length * targetedCaptures;
		PC = GetComponent<PerceptionCamera>();
	}


	void Update()
	{
		if (targetObjects.Length == 0)
		{
			Debug.LogWarning("No target objects (Small or Large tags) found!");
			return;
		}

		if (captureCount >= maxCaptures)
		{
			Debug.Log("Max captures reached. Stopping auto camera.");
			if (!useReplayMode && savedPoses.Count > 0)
			{
				string json = JsonUtility.ToJson(new CameraPoseList { poses = savedPoses }, true);
				File.WriteAllText(posesFile, json);
				Debug.Log($"Saved {savedPoses.Count} camera poses to {posesFile}");
			}
			PC.enabled = false;
			PC.captureTriggerMode = CaptureTriggerMode.Scheduled;
			this.enabled = false;
			return;
		}

		// Capture halfway through the interval of movement
		if (Time.frameCount % movementInterval != 0)
		{
			framesSinceMovement++;
			if (framesSinceMovement == movementInterval / 2)
			{

				if (!useReplayMode)
				{
					Vector3 lookAt = GetCurrentLookAt();
					List<string> activeNames = new List<string>();
					for (int i = 0; i < targetObjects.Length; i++)
					{
						if (targetObjects[i].activeSelf)
							activeNames.Add(targetObjects[i].name);
					}

					savedPoses.Add(new CameraPose { position = transform.position, lookAt = lookAt, activeNames = activeNames });
				}
				PC.RequestCapture();
				captureCount++;
				poseIndex++;
			}

			if (framesSinceMovement == movementInterval) 
			{
				if (poseIndex < savedPoses.Count)
				{
					CameraPose pose = savedPoses[poseIndex];
					transform.position = pose.position;
					transform.LookAt(pose.lookAt);
					ApplyActiveObjects(pose.activeNames);
					framesSinceMovement = 0;
					
				}
				else
				{
					Debug.LogWarning("No more saved poses to replay.");
				}
		}
			
			
				return;
		}

		if (!useReplayMode)
		{
			Move();
		}
	}


	void Move()
	{
		bool hasMoved = false;

		while (!hasMoved)
		{
			int targetIndex = captureCount % targetObjects.Length;  // capture all targets in a repeating order
			GameObject target = targetObjects[targetIndex];
			Vector3 targetPos = target.transform.position;

			float cameraDistance = target.CompareTag("Large")
				? RandomRange(minLargeDistance, maxLargeDistance)
				: RandomRange(minSmallDistance, maxSmallDistance);


			Vector3 randomOffset = RandomOnUnitSphere() * cameraDistance;
			Vector3 newCameraPos = targetPos + randomOffset;

			if (boundsCollider.bounds.Contains(newCameraPos) &&
				IsPointAboveFloor(newCameraPos))
			{
				transform.position = newCameraPos;

				// Randomly uncenter the target
				transform.LookAt(targetPos);
				float distanceToTarget = (float)Vector3.Distance(newCameraPos, targetPos);
				Vector3 randomViewportPoint = new Vector3(RandomRange(0.35f, 0.65f), RandomRange(0.3f, 0.70f), distanceToTarget);
				Vector3 lookOffset = Camera.main.ViewportToWorldPoint(randomViewportPoint);
				transform.LookAt(lookOffset);

				HideFarObjects();

				if (target.activeSelf)
				{
					framesSinceMovement = 0;
					hasMoved = true;
				}
			}
		}
	}

	
	void HideFarObjects()
	{
		for (int i = 0; i < targetObjects.Length; i++)
		{
			GameObject obj = targetObjects[i];
			float distanceToCamera = Vector3.Distance(transform.position, obj.transform.position);
			bool tooClose = distanceToCamera < minSmallDistance;

			float maxDistance = obj.CompareTag("Large") ? (maxLargeDistance + 0.5f) : (maxSmallDistance + 0.5f);
			bool withinDistance = distanceToCamera <= maxDistance;

			Vector3 viewportPoint = Camera.main.WorldToViewportPoint(obj.transform.position);
			bool inView = viewportPoint.x >= 0 && viewportPoint.x <= 1 && viewportPoint.y >= 0 && viewportPoint.y <= 1;
			obj.SetActive(withinDistance && inView && !tooClose);
		}
	}

	void ApplyActiveObjects(List<string> activeNames)
	{
		for (int i = 0; i < targetObjects.Length; i++)
		{
			targetObjects[i].SetActive(activeNames.Contains(targetObjects[i].name));
		}
	}


	Vector3 GetCurrentLookAt()
	{
		Ray ray = new Ray(transform.position, transform.forward);
		if (Physics.Raycast(ray, out RaycastHit hit, 100f))
		{
			return hit.point;
		}
		else
		{
			return transform.position + transform.forward * 10f;
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


	// Functions to help with positions and repeatable randomizations
	float RandomRange(float min, float max)
	{
		return (float)(rng.NextDouble() * (max - min) + min);
	}


	Vector3 RandomOnUnitSphere()
	{
		float theta = (float)(rng.NextDouble() * 2 * Mathf.PI);
		float phi = (float)(System.Math.Acos(2 * rng.NextDouble() - 1));

		float x = Mathf.Sin(phi) * Mathf.Cos(theta);
		float y = Mathf.Sin(phi) * Mathf.Sin(theta);
		float z = Mathf.Cos(phi);

		return new Vector3(x, y, z);
	}

}


[System.Serializable]
public class CameraPose
{
	public Vector3 position;
	public Vector3 lookAt;
	public List<string> activeNames;
}

[System.Serializable]
public class CameraPoseList
{
	public List<CameraPose> poses;
}