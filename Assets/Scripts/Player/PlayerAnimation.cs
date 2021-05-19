using Mirror;
using UnityEngine;

public sealed class PlayerAnimation : MonoBehaviour
{
    [SerializeField]
    private Animator animator;

    [SerializeField]
    private NetworkAnimator networkAnimator;

    private int isRunningHash;
    private int isJumpingHash;
    private int isFiringHash;
    private int carEnterHash;
    private int carLeaveHash;

    [ClientCallback]
    private void Awake()
    {
        isRunningHash = Animator.StringToHash("IsRunning");
        isJumpingHash = Animator.StringToHash("IsJumping");
        isFiringHash = Animator.StringToHash("IsFiring");
        carEnterHash = Animator.StringToHash("CarEnter");
        carLeaveHash = Animator.StringToHash("CarLeave");
    }

    [Client]
    public void OnPlayerJump(bool isJumping)
    {
        animator.SetBool(isJumpingHash, isJumping);
    }

    [Client]
    public void OnPlayerMove(bool isMoving)
    {
        animator.SetBool(isRunningHash, isMoving);
    }

    [Client]
    public void OnKamehameha(PlayerKamehameha.State state)
    {
        var kamehamehaAnimation = state == PlayerKamehameha.State.Charging || state == PlayerKamehameha.State.Firing;
        animator.SetBool(isFiringHash, kamehamehaAnimation);
    }

    [Client]
    public void OnCarEnter()
    {
        animator.SetBool(isJumpingHash, false);
        animator.SetBool(isFiringHash, false);
        animator.SetBool(isRunningHash, false);

        // Need to use network animator for triggers
        // https://mirror-networking.com/docs/Articles/Components/NetworkAnimator.html
        networkAnimator.SetTrigger(carEnterHash);
    }

    [Client]
    public void OnCarLeave()
    {
        // Need to use network animator for triggers
        // https://mirror-networking.com/docs/Articles/Components/NetworkAnimator.html
        networkAnimator.SetTrigger(carLeaveHash);
    }
}
