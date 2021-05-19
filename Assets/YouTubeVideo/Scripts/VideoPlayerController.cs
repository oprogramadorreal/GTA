using UnityEngine;

public sealed class VideoPlayerController : MonoBehaviour
{
    [SerializeField]
    private Animator animator;

    [SerializeField]
    private RagdollController ragdoll;

    [SerializeField]
    private Rigidbody rb;

    [SerializeField]
    private TransformInterpolator interpolator;

    [SerializeField]
    private VideoPlayerKamehameha kamehameha;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            DieRanOver();
        }

        if (Input.GetKeyDown(KeyCode.K) && kamehameha != null)
        {
            kamehameha.Charge();
        }
    }

    public void DieRanOver()
    {
        ragdoll.EnableRagdoll();
        rb.AddForce(Vector3.up * 7000.0f);
    }

    public void DieKamehameha()
    {
        ragdoll.EnableRagdoll();
        rb.AddForce(Vector3.up * 1000.0f);
    }

    public void ApplyForce(Vector3 force)
    {
        var bodies = GetComponentsInChildren<Rigidbody>();

        foreach (var rb in bodies)
        {
            rb.AddForce(force);
        }
    }

    public void LeaveCar()
    {
        Invoke(nameof(LeaveCarLerp), 1.5f);
    }

    public void LeaveCarLerp()
    {
        interpolator.LerpTo(
            new Vector3(-2.1f, -0.54f, -0.0547173f),
            Quaternion.Euler(0.0f, 81.441f, 0.0f),
            1.0f
        );
    }

    public void OnKamehameha(VideoPlayerKamehameha.State state)
    {
        var kamehamehaAnimation = state == VideoPlayerKamehameha.State.Charging || state == VideoPlayerKamehameha.State.Firing;
        animator.SetBool("IsFiring", kamehamehaAnimation);
    }
}
