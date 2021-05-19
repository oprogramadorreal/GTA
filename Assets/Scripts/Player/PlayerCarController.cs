using Mirror;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public sealed class PlayerCarController : NetworkBehaviour
{
    [SerializeField]
    private TransformInterpolator interpolator;

    [SerializeField]
    private PlayerController playerController;

    [SerializeField]
    private PlayerMovement playerMovement;

    [SerializeField]
    private NetworkParentingManager networkParenting;

    [SerializeField]
    private GameObject playerNameUI;

    [SerializeField]
    private UnityEvent carEnterEvent;

    [SerializeField]
    private UnityEvent carLeaveEvent;

    private CameraManager cameraManager;

    private GameObject enterCarMessage;

    private CarController closestCar;

    private bool isPlayerMovementAllowed = true;

    [SyncVar]
    private bool isDriving = false;

    public bool IsPlayerMovementAllowed => isPlayerMovementAllowed;

    [Client]
    public void ClientSetup(CameraManager cameraManager, GameObject enterCarMessage)
    {
        this.cameraManager = cameraManager;
        this.enterCarMessage = enterCarMessage;
    }

    [ClientCallback]
    private void Update()
    {
        if (!hasAuthority || !WasSetup())
        {
            return;
        }

        var needsMessage = NeedsToShowEnterCarMessage();

        if (enterCarMessage.activeInHierarchy != needsMessage)
        {
            enterCarMessage.SetActive(needsMessage);
        }
    }

    [Client]
    private bool WasSetup()
    {
        return cameraManager != null
            && enterCarMessage != null;
    }

    [Client]
    private bool NeedsToShowEnterCarMessage()
    {
        return !isDriving
            && closestCar != null
            && closestCar.IsAvailable()
            && isPlayerMovementAllowed;
    }

    [Client]
    public void EnterClosestCar()
    {
        if (!hasAuthority || !CanEnterClosestCar() || !isPlayerMovementAllowed)
        {
            return;
        }

        CmdSetDriving(true);

        closestCar.RequestAuthority();
        closestCar.OpenDoor();

        networkParenting.OnClientSetParent(closestCar.transform, closestCar.netId);

        interpolator.LerpTo(
            new Vector3(-2.1f, 0.1357903f, -0.0547173f),
            Quaternion.Euler(0.0f, 81.441f, 0.0f),
            1.5f
        );

        interpolator.LerpTo(
            new Vector3(-2.18f, 0.0f, 0.3f),
            Quaternion.Euler(0.0f, 92.4f, -13.92f),
            0.5f
        );

        isPlayerMovementAllowed = false;
        cameraManager.LookAtCar(closestCar.CarCamera, 0.5f);

        Invoke(nameof(AllowCarControl), 2.5f);

        carEnterEvent?.Invoke();
    }

    [Command]
    private void CmdSetDriving(bool driving)
    {
        if (isServerOnly)
        {
            playerMovement.SetMovementEnabled(!driving);
            playerNameUI.SetActive(!driving);
        }

        isDriving = driving;

        RpcSetDriving(driving);
    }

    [ClientRpc]
    private void RpcSetDriving(bool driving)
    {
        playerMovement.SetMovementEnabled(!driving);
        playerNameUI.SetActive(!driving);
    }

    [Client]
    private bool CanEnterClosestCar()
    {
        return !isDriving
            && closestCar != null
            && closestCar.IsAvailable();
    }

    [Client]
    private void AllowCarControl()
    {
        closestCar.AllowCarControl(true);
    }

    [Client]
    public void LeaveCar()
    {
        if (!hasAuthority || !isDriving || isPlayerMovementAllowed)
        {
            return;
        }

        closestCar.OpenDoor();
        closestCar.AllowCarControl(false);
        Invoke(nameof(LeaveCarLerp), 1.5f); // lerp must be completed before DetachFromCar
        Invoke(nameof(DetachFromCar), 3.7f);
        cameraManager.LookAtPlayer(1.0f);
        carLeaveEvent?.Invoke();
    }

    [Client]
    private void LeaveCarLerp()
    {
        interpolator.LerpTo(
            new Vector3(-2.1f, 0.1357903f, -0.0547173f),
            Quaternion.Euler(0.0f, 81.441f, 0.0f),
            1.0f
        );
    }

    [Client]
    private void DetachFromCar()
    {
        StartCoroutine(DetachFromCarImpl());
    }

    [Client]
    private IEnumerator DetachFromCarImpl()
    {
        while (interpolator.IsWorking())
        {
            yield return null;
        }

        networkParenting.OnClientSetParent(null, 0u);

        Invoke(nameof(FinishDriving), 0.1f); // workaround
    }

    [Client]
    private void FinishDriving()
    {
        CmdSetDriving(false);
        isPlayerMovementAllowed = true;
        closestCar.RemoveAuthority();
    }

    [Client]
    public int GetClosestCarId()
    {
        return closestCar.GetCarId();
    }

    public bool IsDriving()
    {
        return isDriving;
    }

    [ClientCallback]
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("CarDoorSpot") && !isDriving)
        {
            var closeCar = other.GetComponentInParent<CarController>();

            if (closeCar.IsAvailable())
            {
                closestCar = closeCar;
            }
        }
    }

    [ClientCallback]
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("CarDoorSpot") && !isDriving)
        {
            closestCar = null;
        }
    }
}
