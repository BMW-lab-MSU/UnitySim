using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Perception.GroundTruth;
using UnityEngine.Perception.GroundTruth.DataModel;


public class cameraMove : MonoBehaviour
{
    public float MVspeed = 10.0f;
    public float LookSpeed = 2.0f;

    private float verticalMV = 0.0f;
    private PerceptionCamera PC;
    private AutoCamera autoCamera;


    void Start(){
        PC = GetComponent<PerceptionCamera>();
        autoCamera = GetComponent<AutoCamera>();
    }


    void Update()
    {
		if (pauseMenu.GameIsPaused == false)
        {
            // Toggle the auto camera to collect data. Disables other controls.
            if (Input.GetKeyDown(KeyCode.G))
            {
                autoCamera.enabled = !autoCamera.enabled;
                PC.enabled = autoCamera.enabled;
                PC.captureTriggerMode = autoCamera.enabled ? CaptureTriggerMode.Manual : CaptureTriggerMode.Scheduled;
            }

            if (!autoCamera.enabled)
			{
				// Get the horizontal and vertical axis.
				// By default they are mapped to the arrow keys.
				// The value is in the range -1 to 1
				float translationV = Input.GetAxis("Vertical") * MVspeed;
                float translationH = Input.GetAxis("Horizontal") * MVspeed;

                //Save Labeled Images
                if (Input.GetKeyDown(KeyCode.F))
                {
                    PC.enabled = !PC.enabled;
                }

                // up or down
                if (Input.GetKey(KeyCode.Space))
                {
                    verticalMV = 1.0f;
				}
                else if (Input.GetKey(KeyCode.Q))
                {
                    verticalMV = -1.0f;
                }
                else
                {
                    verticalMV = 0.0f;
                }

                translationV *= Time.deltaTime;
                translationH *= Time.deltaTime;
                verticalMV *= Time.deltaTime;

                // Get the mouse delta. This is not in the range -1...1
                float h = LookSpeed * Input.GetAxis("Mouse X");
                float v = -LookSpeed * Input.GetAxis("Mouse Y");

                Vector3 rot = transform.localEulerAngles;
                rot.x += v;
                rot.y += h;
                transform.localEulerAngles = rot;


                // Move translation along the object's z-axis
                transform.Translate(translationH, verticalMV, translationV);
			}
        }
        else
        {
            PC.enabled = false;
            autoCamera.enabled = false;
        }
    }
}
