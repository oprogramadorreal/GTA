using Cinemachine;
using Mirror;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

/// <summary>
/// Based on https://docs.unity3d.com/Manual/WheelColliderTutorial.html
/// </summary>
public sealed class CarController : NetworkBehaviour
{
    [SerializeField]
    private List<AxleInfo> axleInfos;

    [SerializeField]
    private float maxMotorTorque;

    [SerializeField]
    private float maxSteeringAngle;

    [SerializeField]
    private Transform carDoorSpot;

    [SerializeField]
    private CinemachineVirtualCamera carCamera;

    [SerializeField]
    private PlayableDirector doorAnimation;

    [SerializeField]
    private int carId = 0;

    public CinemachineVirtualCamera CarCamera => carCamera;

    [SyncVar]
    private bool carControlAllowed = false;

    [SyncVar]
    private uint driverId = 0;

    [SyncVar]
    private Vector3 velocity = Vector3.zero;

    private const float maxBreakTorque = 5000.0f;

    private float currentMotor = 0.0f;
    private float currentSteering = 0.0f;
    private float currentBreakTorque = maxBreakTorque;

    private Rigidbody rb;

    private void Start()
    {
        ApplyMotorAndSteering(currentMotor, currentSteering, currentBreakTorque);

        rb = GetComponent<Rigidbody>();

        // Based on the anwser of vis2k on this thread:
        // https://github.com/vis2k/Mirror/issues/264
        rb.isKinematic = true;
    }

    [Client]
    public int GetCarId()
    {
        return carId;
    }

    [Client]
    public void OpenDoor()
    {
        doorAnimation.Play();
    }

    [Client]
    public bool IsAvailable()
    {
        return driverId == 0;
    }

    [Client]
    public void AllowCarControl(bool allowed)
    {
        CmdAllowCarControl(allowed);
    }

    [Command]
    private void CmdAllowCarControl(bool allowed)
    {
        carControlAllowed = allowed;
    }

    [Client]
    public void RequestAuthority()
    {
        CmdRequestAuthority();
        rb.isKinematic = false;
    }

    [Command(requiresAuthority = false)]
    private void CmdRequestAuthority(NetworkConnectionToClient sender = null)
    {
        netIdentity.AssignClientAuthority(sender);
        driverId = sender.identity.netId;
    }

    [Client]
    public void RemoveAuthority()
    {
        rb.isKinematic = true;
        CmdRemoveAuthority();
    }

    [Command]
    private void CmdRemoveAuthority(NetworkConnectionToClient sender = null)
    {
        netIdentity.RemoveClientAuthority();
        driverId = 0u;
    }

    [ClientCallback]
    private void Update()
    {
        if (!hasAuthority)
        {
            return;
        }

        if (carControlAllowed)
        {
            if (Input.GetKey(KeyCode.Space))
            {
                currentBreakTorque = maxBreakTorque;
            }
            else
            {
                currentMotor = maxMotorTorque * Input.GetAxis("Vertical");
                currentBreakTorque = 0.0f;
            }

            currentSteering = maxSteeringAngle * Input.GetAxis("Horizontal");
        }
        else
        {
            currentMotor = 0.0f;
            currentSteering = 0.0f;
            currentBreakTorque = maxBreakTorque;
        }
    }

    [ClientCallback]
    private void FixedUpdate()
    {
        if (!hasAuthority)
        {
            return;
        }

        if (!rb.isKinematic)
        {
            ApplyMotorAndSteering(currentMotor, currentSteering, currentBreakTorque);
            CmdSetVelocity(rb.velocity);
        }
    }

    [Command]
    private void CmdSetVelocity(Vector3 newVelocity)
    {
        velocity = newVelocity;
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

    [ClientCallback]
    private void OnTriggerEnter(Collider other)
    {
        if (hasAuthority)
        {
            if (other.CompareTag("Player") && !rb.isKinematic)
            {
                var carSpeed = velocity.magnitude;

                if (carSpeed > 2.0f)
                {
                    other.GetComponent<PlayerController>().Die(driverId);
                }
            }
        }
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
