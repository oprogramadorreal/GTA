using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public class VideoCarController : MonoBehaviour
{
    [SerializeField]
    private List<AxleInfo> axleInfos;

    [SerializeField]
    private float maxMotorTorque;

    [SerializeField]
    private float maxSteeringAngle;

    [SerializeField]
    private bool accelerate = false;

    [SerializeField]
    private Animator driverAnimator;

    [SerializeField]
    private VideoPlayerController driverController;

    [SerializeField]
    private PlayableDirector doorAnimation;

    private const float maxBreakTorque = 5000.0f;

    private float currentMotor = 0.0f;
    private float currentSteering = 0.0f;
    private float currentBreakTorque = maxBreakTorque;

    private Rigidbody rb;

    private void Start()
    {
        ApplyMotorAndSteering(currentMotor, currentSteering, currentBreakTorque);

        rb = GetComponent<Rigidbody>();
        rb.isKinematic = false;
    }

    public void OpenDoor()
    {
        doorAnimation.Play();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.O))
        {
            accelerate = !accelerate;
        }

        if (!accelerate)
        {
            currentBreakTorque = maxBreakTorque;
        }
        else
        {
            //currentMotor = maxMotorTorque * Input.GetAxis("Vertical");
            currentMotor = maxMotorTorque;
            currentBreakTorque = 0.0f;
        }

        currentSteering = maxSteeringAngle * Input.GetAxis("Horizontal");
    }

    public void Accelerate()
    {
        accelerate = true;
    }

    private void FixedUpdate()
    {
        if (!rb.isKinematic)
        {
            ApplyMotorAndSteering(currentMotor, currentSteering, currentBreakTorque);
        }
    }

    private void ApplyMotorAndSteering(float motor, float steering, float breakTorque)
    {
        foreach (var axleInfo in axleInfos)
        {
            if (axleInfo.steering)
            {
                axleInfo.leftWheel.steerAngle = steering;
                axleInfo.rightWheel.steerAngle = steering;
            }

            if (axleInfo.motor)
            {
                axleInfo.leftWheel.brakeTorque = breakTorque;
                axleInfo.rightWheel.brakeTorque = breakTorque;

                axleInfo.leftWheel.motorTorque = motor;
                axleInfo.rightWheel.motorTorque = motor;
            }

            ApplyLocalPositionToVisuals(axleInfo.leftWheel);
            ApplyLocalPositionToVisuals(axleInfo.rightWheel);
        }
    }

    private void ApplyLocalPositionToVisuals(WheelCollider collider)
    {
        if (collider.transform.childCount == 0)
        {
            return;
        }

        var visualWheel = collider.transform.GetChild(0);

        collider.GetWorldPose(out var position, out var rotation);

        visualWheel.transform.position = position;
        visualWheel.transform.rotation = rotation;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !rb.isKinematic)
        {
            accelerate = false;
            other.GetComponent<VideoPlayerController>().DieRanOver();
            DriverLeaveCar();
        }
    }

    private void DriverLeaveCar()
    {
        OpenDoor();
        driverAnimator.SetTrigger("CarLeave");
        driverController.LeaveCar();
    }

    [System.Serializable]
    private sealed class AxleInfo
    {
        public WheelCollider leftWheel;
        public WheelCollider rightWheel;
        public bool motor;
        public bool steering;
    }
}
