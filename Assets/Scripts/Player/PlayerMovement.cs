using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Based on https://youtu.be/4HpC--2iowE
/// </summary>
public sealed class PlayerMovement : MonoBehaviour
{
    [SerializeField]
    private CharacterController controller;

    [SerializeField]
    private float speed = 6.0f;

    [SerializeField]
    private float gravity = -9.81f;

    [SerializeField]
    private float turnSmoothTime = 0.1f;

    [SerializeField]
    private float jumpHeight = 3.0f;

    [SerializeField]
    private Transform groundCheck;

    [SerializeField]
    private float groundDistance = 0.4f;

    [SerializeField]
    private LayerMask groundMask;

    [SerializeField]
    private UnityEvent<bool> moveEvent;

    [SerializeField]
    private UnityEvent<bool> jumpEvent;

    private bool isGrounded = false;
    private bool isMoving = false;

    private float turnSmoothVelocity = 0.0f;

    private float yVelocity = 0.0f;
    private Vector3 velocity = Vector3.zero;
    private Vector3 externalForces = Vector3.zero;

    public void Move(bool jump, float horizontalMovement, float verticalMovement, float cameraRotationY, float playerRotationY, float deltaTime)
    {
        if (!controller.enabled)
        {
            return;
        }

        TryToLand();
        TryToJump(jump);

        var movementDirection = new Vector3(horizontalMovement, 0.0f, verticalMovement).normalized;

        var wasMoving = isMoving;
        isMoving = movementDirection.magnitude >= 0.1f;

        var translation = Vector3.zero;
        var rotation = transform.rotation;

        if (isMoving)
        {
            translation = CalculateMovement(movementDirection, cameraRotationY, playerRotationY, deltaTime, out rotation);
        }

        translation = UpdateCustomPhysics(translation, deltaTime);

        transform.rotation = rotation;
        controller.Move(translation);

        if (wasMoving != isMoving)
        {
            moveEvent?.Invoke(isMoving);
        }
    }

    private Vector3 UpdateCustomPhysics(Vector3 translation, float deltaTime)
    {
        const float damping = 0.9f;

        velocity += externalForces;
        velocity *= damping; // apply damping

        yVelocity += gravity * deltaTime;
        translation += new Vector3(velocity.x, velocity.y + yVelocity, velocity.z) * deltaTime;

        velocity *= damping; // apply more damping

        externalForces = Vector3.zero;

        return translation;
    }

    public void AddExternalForce(Vector3 force)
    {
        //externalForces += force;
    }

    public void SetNotMoving()
    {
        isMoving = false;
    }

    public bool IsGrounded()
    {
        return isGrounded;
    }

    public void SetMovementEnabled(bool enabled)
    {
        controller.enabled = enabled;
    }

    private void TryToLand()
    {
        var wasGrounded = isGrounded;
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        if (isGrounded)
        {
            if (yVelocity < 0.0f)
            {
                yVelocity = -2.0f;
            }

            if (!wasGrounded)
            {
                jumpEvent?.Invoke(false);
            }
        }
    }

    private void TryToJump(bool jumpButton)
    {
        if (jumpButton && isGrounded)
        {
            yVelocity = Mathf.Sqrt(jumpHeight * -2.0f * gravity);
            jumpEvent?.Invoke(true);
        }
    }

    private Vector3 CalculateMovement(Vector3 direction, float cameraRotationY, float playerRotationY, float deltaTime, out Quaternion rotation)
    {
        var targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + cameraRotationY;
        var angle = Mathf.SmoothDampAngle(playerRotationY, targetAngle, ref turnSmoothVelocity, turnSmoothTime);

        rotation = Quaternion.Euler(0.0f, angle, 0.0f);

        var moveDir = Quaternion.Euler(0.0f, targetAngle, 0.0f) * Vector3.forward;
        return moveDir.normalized * speed * deltaTime;
    }
}
