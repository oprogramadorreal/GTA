using UnityEngine;
using UnityEngine.Events;

public sealed class VideoPlayerKamehameha : MonoBehaviour
{
    [SerializeField]
    private UnityEvent<State> firingEvent;

    [SerializeField]
    private GameObject kamehamehaPrefab;

    private float timeAcc = 0.0f;

    private bool isCharging = false;
    private bool isFiring = false;

    [SerializeField] private LayerMask playersLayerMask;
    [SerializeField] private LayerMask objectsLayerMask;
    [SerializeField] private LayerMask groundLayerMask;

    private int foesCount = 0;

    private const float explosionRadius = 5.0f;
    private const float explosionForce = 5000.0f;

    private GameObject currentKamehamehaEffect = null;
    private GameObject currentExplosion = null;

    public int FoesCount { get => foesCount; }

    private void Update()
    {
        timeAcc += Time.deltaTime;

        if (isCharging)
        {
            if (timeAcc >= 2.1f)
            {
                StartFiring();
            }
        }
        else if (isFiring)
        {
            if (timeAcc >= 3.74f)
            {
                StopFiring();
            }
            else
            {
                Fire();
            }
        }
    }

    private void StartFiring()
    {
        isCharging = false;
        isFiring = true;

        var pointOnTerrain = Fire();
        OnFireEvent(State.Firing);
    }

    private Vector3? Fire()
    {
        var pointOnTerrain = FireOnTerrain();

        if (pointOnTerrain.HasValue)
        {
            ExplodeNearbyObjects(pointOnTerrain.Value, 0.5f);
            //UpdateDebrisSound(pointOnTerrain.Value);
        }

        FireOnObjects();

        return pointOnTerrain;
    }

    public void StopFiring()
    {
        if (!isFiring)
        {
            return;
        }

        isCharging = false;
        isFiring = false;

        if (currentKamehamehaEffect != null)
        {
            Destroy(currentKamehamehaEffect);
            currentKamehamehaEffect = null;
        }

        OnFireEvent(State.Done);
    }

    public void Charge()
    {
        if (!isCharging && !isFiring)
        {
            timeAcc = 0.0f;
            isCharging = true;
            OnFireEvent(State.Charging);

            currentKamehamehaEffect = Instantiate(kamehamehaPrefab, Vector3.zero, Quaternion.identity, transform);
            currentKamehamehaEffect.transform.localPosition = GetKamehamehaLocalStartPosition();
            currentKamehamehaEffect.transform.localRotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);
        }
    }

    private Vector3 GetKamehamehaWorldEndPosition(Vector3 startPosition)
    {
        const float kamehamehaReach = 21.0f;
        return startPosition + transform.forward * kamehamehaReach;
    }

    private Vector3 GetKamehamehaWorldStartPosition()
    {
        const float kamehamehaHeight = 1.0f;
        return transform.TransformPoint(GetKamehamehaLocalStartPosition()) + Vector3.up * kamehamehaHeight;
    }

    private static Vector3 GetKamehamehaLocalStartPosition()
    {
        return new Vector3(-0.025f, 0.0f, 1.065f);
    }

    private void OnFireEvent(State state)
    {
        firingEvent?.Invoke(state);
    }

    private Vector3? FireOnTerrain()
    {
        if (!Physics.Raycast(GetKamehamehaRay(out var rayMaxDistance), out var hit, rayMaxDistance, groundLayerMask))
        {
            return null;
        }

        return hit.point;
    }

    private Ray GetKamehamehaRay(out float rayMaxDistance)
    {
        var rayBegin = GetKamehamehaWorldStartPosition();
        var rayEnd = GetKamehamehaWorldEndPosition(rayBegin);
        var rayDir = rayEnd - rayBegin;
        rayMaxDistance = rayDir.magnitude;
        rayDir /= rayMaxDistance;

        return new Ray(rayBegin, rayDir);
    }

    private void FireOnObjects()
    {
        var rayBegin = GetKamehamehaWorldStartPosition();
        var rayEnd = GetKamehamehaWorldEndPosition(rayBegin);

        FireOnObjects(rayBegin, rayEnd);
    }

    private void FireOnObjects(Vector3 kamehamehaBegin, Vector3 kamehamehaEnd)
    {
        const float explosionForceMultiplier = 0.075f;

        var forceDirection = (kamehamehaEnd - kamehamehaBegin).normalized;
        var forceToApply = forceDirection * explosionForce * explosionForceMultiplier;
        var capsuleRadius = explosionRadius * 0.3f;

        var colliders = Physics.OverlapCapsule(kamehamehaBegin, kamehamehaEnd, capsuleRadius, playersLayerMask);

        foreach (var c in colliders)
        {
            if (!ReferenceEquals(gameObject, c.gameObject) && c.CompareTag("Player"))
            {
                var player = c.GetComponent<VideoPlayerController>();
                player.DieKamehameha();
                player.ApplyForce(forceToApply);
            }
        }

        colliders = Physics.OverlapCapsule(kamehamehaBegin, kamehamehaEnd, capsuleRadius, objectsLayerMask);

        foreach (var c in colliders)
        {
            var rb = FindComponent<Rigidbody>(c);

            if (rb != null)
            {
                rb.isKinematic = false;
                rb.AddForce(forceToApply);
            }
        }
    }

    private void ExplodeNearbyObjects(Vector3 explosionCenter, float explosionMultiplier)
    {
        var usedExplosionForce = explosionForce * explosionMultiplier;
        var usedExplosionRadius = explosionRadius * 1.5f * explosionMultiplier;

        var colliders = Physics.OverlapSphere(explosionCenter, usedExplosionRadius, playersLayerMask);

        foreach (var c in colliders)
        {
            //if (!ReferenceEquals(gameObject, c.gameObject) && c.CompareTag("Player"))
            //{
            //    var player = c.GetComponent<PlayerController>();
            //    player.OnServerApplyExplosionForce(usedExplosionForce, explosionCenter, usedExplosionRadius);
            //}
        }

        colliders = Physics.OverlapSphere(explosionCenter, usedExplosionRadius, objectsLayerMask);

        foreach (var c in colliders)
        {
            var rb = FindComponent<Rigidbody>(c);

            if (rb != null)
            {
                rb.isKinematic = false;
                rb.AddExplosionForce(usedExplosionForce, explosionCenter, usedExplosionRadius);
            }
        }
    }

    private static T FindComponent<T>(Collider collider) where T : Component
    {
        var component = collider.GetComponentInChildren<T>();

        if (component != null)
        {
            return component;
        }

        return collider.GetComponentInParent<T>();
    }

    public enum State
    {
        Charging = 0,
        Firing,
        Done
    }
}
