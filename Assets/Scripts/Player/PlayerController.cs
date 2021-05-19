using Cinemachine;
using Mirror;
using UnityEngine;
using UnityEngine.Events;

public sealed class PlayerController : NetworkBehaviour
{
    [SerializeField]
    private PlayerMovement movement;

    [SerializeField]
    private PlayerCarController carController;

    [SerializeField]
    private PlayerKamehameha kamehameha;

    [SerializeField]
    private RagdollController ragdoll;

    [SerializeField]
    private Transform cameraFollowTarget;

    [SerializeField]
    private Transform cameraLookAtTarget;

    [SerializeField]
    private GameObject playerNameUI;

    [SerializeField]
    private UnityEvent dieEvent;

    private Transform cameraTransform;

    private bool runningCarAnimation = false;

    [SyncVar]
    private bool isDead = false;

    public override void OnStartClient()
    {
        FindObjectOfType<GameConfigurator>().ClientSetupPlayer(gameObject);

        if (isDead)
        {
            ragdoll.EnableRagdoll();
        }
    }

    public override void OnStartLocalPlayer()
    {
        // This happens after OnStartClient()
        // Called only for the local player.

        FindObjectOfType<GameConfigurator>().LocalPlayerStarted(gameObject);
    }

    [Client]
    public void ClientSetupCamera(CinemachineFreeLook camera)
    {
        camera.Follow = cameraFollowTarget;
        camera.LookAt = cameraLookAtTarget;
    }

    [Client]
    public void SetCameraTransform(Transform t)
    {
        cameraTransform = t;
    }

    public override void OnStopClient()
    {
        if (isLocalPlayer)
        {
            //FindObjectOfType<PlayerConfigurator>().LocalPlayerDestroyed();
        }
    }

    [ClientCallback]
    private void Update()
    {
        if (!hasAuthority || isDead || !WasSetup())
        {
            return;
        }

        if (carController.IsDriving())
        {
            if (Input.GetKeyDown(KeyCode.F) && !runningCarAnimation)
            {
                carController.LeaveCar();
                CarAnimationBegin();
            }
        }
        else
        {
            if (Input.GetKeyDown(KeyCode.F) && CanDrive() && !runningCarAnimation)
            {
                carController.EnterClosestCar();
                movement.SetNotMoving();
                CarAnimationBegin();
            }
            else
            {
                if (carController.IsPlayerMovementAllowed)
                {
                    if (Input.GetMouseButtonDown(0))
                    {
                        kamehameha.Charge();
                    }

                    var horizontalMovement = Input.GetAxisRaw("Horizontal");
                    var verticalMovement = Input.GetAxisRaw("Vertical");
                    var jumpMovement = Input.GetButtonDown("Jump");

                    movement.Move(jumpMovement, horizontalMovement, verticalMovement, cameraTransform.eulerAngles.y, transform.eulerAngles.y, Time.deltaTime);
                }
            }
        }
    }

    private void CarAnimationBegin()
    {
        runningCarAnimation = true;
        Invoke(nameof(CarAnimationEnd), 3.8f);
    }

    private void CarAnimationEnd()
    {
        runningCarAnimation = false;
    }

    [Client]
    private bool WasSetup()
    {
        return cameraTransform != null;
    }

    [Client]
    private bool CanDrive()
    {
        return movement.IsGrounded()
            && !kamehameha.IsChargingOrFiring();
    }

    [Client]
    public void Revive()
    {
        if (hasAuthority && isDead)
        {
            CmdRevive();
        }
    }

    [Command]
    private void CmdRevive()
    {
        isDead = false;
        RpcRevive();
    }

    [ClientRpc]
    private void RpcRevive()
    {
        playerNameUI.SetActive(true);
        ragdoll.DisableRagdoll();
    }

    [Client]
    public void Die(uint killerNetId)
    {
        if (!carController.IsDriving())
        {
            ragdoll.EnableRagdoll(); // needs to become a ragdoll as soon as possible in this client
            CmdDie(killerNetId);
        }
    }

    [Command(requiresAuthority = false)]
    private void CmdDie(uint killerNetId)
    {
        ServerDie(killerNetId);
    }

    [Server]
    public void ServerDie(uint killerNetId)
    {
        if (!isDead && !carController.IsDriving())
        {
            isDead = true;
            kamehameha.StopFiring();

            if (isServerOnly)
            {
                ragdoll.EnableRagdoll();
            }

            RpcDie(killerNetId);
        }
    }

    [ClientRpc]
    private void RpcDie(uint killerNetId)
    {
        ragdoll.EnableRagdoll();
        playerNameUI.SetActive(false);

        if (isLocalPlayer)
        {
            if (NetworkIdentity.spawned.TryGetValue(killerNetId, out var networkIdentity))
            {
                var killerName = networkIdentity.gameObject.GetComponentInChildren<NameDisplay>().GetDisplayName();
                FindObjectOfType<GameOverDisplay>().ShowGameOver(killerName);
            }

            dieEvent?.Invoke();
        }
    }

    [Server]
    public void ServerApplyForce(Vector3 force)
    {
        if (isServerOnly)
        {
            ApplyForceImpl(force);
        }

        RpcApplyForce(force);
    }

    [ClientRpc]
    private void RpcApplyForce(Vector3 force)
    {
        ApplyForceImpl(force);
    }

    private void ApplyForceImpl(Vector3 force)
    {
        var bodies = GetComponentsInChildren<Rigidbody>();

        foreach (var rb in bodies)
        {
            rb.AddForce(force);
        }
    }

    [Server]
    public void ServerApplyExplosionForce(float force, Vector3 center, float radius)
    {
        if (isDead)
        {
            if (isServerOnly)
            {
                ApplyExplosionForceImpl(force, center, radius);
            }

            RpcApplyExplosionForce(force, center, radius);
        }
    }

    [ClientRpc]
    private void RpcApplyExplosionForce(float force, Vector3 center, float radius)
    {
        ApplyExplosionForceImpl(force, center, radius);
    }

    private void ApplyExplosionForceImpl(float force, Vector3 center, float radius)
    {
        var bodies = GetComponentsInChildren<Rigidbody>();

        foreach (var rb in bodies)
        {
            rb.AddExplosionForce(force, center, radius);
        }
    }
}
