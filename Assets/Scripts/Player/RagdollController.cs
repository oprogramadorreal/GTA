using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed class RagdollController : MonoBehaviour
{
    [SerializeField]
    private Animator animator;

    [SerializeField]
    private PlayerMovement playerMovement;

    [SerializeField] private bool ragdollEnabled = false;

    private IEnumerable<Collider> ragdollColliders;
    private IEnumerable<Rigidbody> ragdollBodies;

    private void Awake()
    {
        ragdollColliders = GetComponentsInChildren<Collider>()
            .Where(c => c.gameObject != gameObject)
            .ToList();

        ragdollBodies = GetComponentsInChildren<Rigidbody>().ToList();

        SetRagdollImpl(ragdollEnabled);
    }

    public void EnableRagdoll()
    {
        if (!ragdollEnabled)
        {
            SetRagdollImpl(true);
            ragdollEnabled = true;
        }
    }

    public void DisableRagdoll()
    {
        if (ragdollEnabled)
        {
            SetRagdollImpl(false);
            ragdollEnabled = false;
        }
    }

    private void SetRagdollImpl(bool enabled)
    {
        foreach (var c in ragdollColliders)
        {
            c.isTrigger = !enabled;
        }

        foreach (var b in ragdollBodies)
        {
            b.isKinematic = !enabled;
        }

        if (animator != null)
        {
            animator.enabled = !enabled;
        }

        if (playerMovement != null)
        {
            playerMovement.SetMovementEnabled(!enabled);
        }
    }
}
