using Mirror;
using UnityEngine;
using UnityEngine.Events;

public sealed class PlayerKamehameha : NetworkBehaviour
{
    [SerializeField]
    private UnityEvent<State> firingEvent;

    [SerializeField]
    private UnityEvent<Vector3> explosionEvent;

    [SerializeField]
    private GameObject kamehamehaPrefab;

    [SerializeField]
    private GameObject explosionPrefab;

    [SerializeField]
    private GameObject firingLight;

    [SerializeField]
    private GameObject chargingLight;

    [SerializeField]
    private PlayerMovement playerMovement;

    private float timeAcc = 0.0f;

    [SyncVar] private bool isCharging = false;
    [SyncVar] private bool isFiring = false;

    [SerializeField] private LayerMask playersLayerMask;
    [SerializeField] private LayerMask objectsLayerMask;
    [SerializeField] private LayerMask groundLayerMask;

    private int foesCount = 0;

    private const float explosionRadius = 5.0f;
    private const float explosionForce = 5000.0f;

    private GameObject currentKamehamehaEffect = null;
    private GameObject currentExplosion = null;

    public int FoesCount { get => foesCount; }

    [ServerCallback]
    private void Update()
    {
        timeAcc += Time.deltaTime;

        if (isCharging)
        {
            if (timeAcc >= 2.1f)
            {
                StartFiring();
            }
            else if (timeAcc >= 0.3f)
            {
                if (chargingLight != null && !chargingLight.activeInHierarchy)
                {
                    chargingLight.SetActive(true);
                }
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

    [ClientCallback]
    private void FixedUpdate()
    {
        if (hasAuthority)
        {
            if (isFiring)
            {
                const float recoilForce = 1.0f;
                playerMovement.AddExternalForce(-GetKamehamehaRay(out var _).direction * recoilForce);
            }
        }
    }

    [Server]
    private void StartFiring()
    {
        //Time.timeScale = 0.5f;

        isCharging = false;
        isFiring = true;

        if (firingLight != null && !firingLight.activeInHierarchy)
        {
            firingLight.SetActive(true);
        }

        var pointOnTerrain = Fire();

        if (pointOnTerrain.HasValue)
        {
            StartExplosion(pointOnTerrain.Value);
            //audioManager.CreateTemporaryAudioSourceAt("Explosion", explosionCenter.Value);
        }

        //if (debrisSound != null)
        //{
        //    Destroy(debrisSound.gameObject);
        //    debrisSound = null;
        //}

        Target_OnFireEvent(State.Firing);
    }

    [Server]
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

    [Server]
    public void StopFiring()
    {
        if (!isFiring)
        {
            return;
        }

        //Time.timeScale = 1.0f;

        isCharging = false;
        isFiring = false;

        if (firingLight != null)
        {
            firingLight.SetActive(false);
        }

        if (chargingLight != null)
        {
            chargingLight.SetActive(false);
        }

        if (currentKamehamehaEffect != null)
        {
            NetworkServer.Destroy(currentKamehamehaEffect);
            currentKamehamehaEffect = null;
        }

        Target_OnFireEvent(State.Done);
    }

    //private void UpdateDebrisSound(Vector3 soundLocation)
    //{
    //    if (debrisSound == null)
    //    {
    //        debrisSound = audioManager.CreateAudioSourceAt("Debris", soundLocation);
    //    }
    //    else
    //    {
    //        debrisSound.transform.position = soundLocation;
    //    }
    //}

    [Client]
    public void Charge()
    {
        if (hasAuthority)
        {
            CmdCharge();
        }
    }

    [Command]
    private void CmdCharge()
    {
        if (!isCharging && !isFiring)
        {
            //CameraShaker.Instance.ShakeOnce(3.0f, 3.0f, 4.0f, 5.0f); // TODO

            timeAcc = 0.0f;
            isCharging = true;
            Target_OnFireEvent(State.Charging);

            currentKamehamehaEffect = Instantiate(kamehamehaPrefab, Vector3.zero, Quaternion.identity, transform);
            currentKamehamehaEffect.transform.localPosition = GetKamehamehaLocalStartPosition();
            currentKamehamehaEffect.transform.localRotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);

            currentKamehamehaEffect.GetComponent<NetworkParentingManager>().OnServerSetParentNetId(netId);

            NetworkServer.Spawn(currentKamehamehaEffect, connectionToClient);

            //audioManager.CreateTemporaryAudioSourceWithin("Kamehameha", transform);
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
        return new Vector3(-0.025f, -0.54f, 1.065f);
    }

    /// <summary>
    /// Using TargetRpc here to trigger 'firingEvent' and start the animation
    /// only for the object of the owner client. NetworkAnimator attached to
    /// the Player will sync the animation on the other clients.
    /// </summary>
    [TargetRpc]
    private void Target_OnFireEvent(State state)
    {
        if (hasAuthority)
        {
            firingEvent?.Invoke(state);
        }
    }

    [Client]
    public bool IsChargingOrFiring()
    {
        return isCharging
            || isFiring;
    }

    [Server]
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

    [Server]
    private void StartExplosion(Vector3 explosionCenter)
    {
        ExplodeNearbyObjects(explosionCenter.normalized, 1.0f);
        PlayExplosion(explosionPrefab, explosionCenter, new Vector3(2.0f, 2.0f, 2.0f));

        Target_OnExplosionEvent(explosionCenter);
    }

    [TargetRpc]
    private void Target_OnExplosionEvent(Vector3 explosionCenter)
    {
        if (hasAuthority)
        {
            explosionEvent?.Invoke(explosionCenter);
        }
    }

    [Server]
    private void FireOnObjects()
    {
        var rayBegin = GetKamehamehaWorldStartPosition();
        var rayEnd = GetKamehamehaWorldEndPosition(rayBegin);

        FireOnObjects(rayBegin, rayEnd);
    }

    [Server]
    private void FireOnObjects(Vector3 kamehamehaBegin, Vector3 kamehamehaEnd)
    {
        const float explosionForceMultiplier = 0.2f;

        var forceDirection = (kamehamehaEnd - kamehamehaBegin).normalized;
        var forceToApply = forceDirection * explosionForce * explosionForceMultiplier;
        var capsuleRadius = explosionRadius * 0.3f;

        var colliders = Physics.OverlapCapsule(kamehamehaBegin, kamehamehaEnd, capsuleRadius, playersLayerMask);

        foreach (var c in colliders)
        {
            if (!ReferenceEquals(gameObject, c.gameObject) && c.CompareTag("Player"))
            {
                var player = c.GetComponent<PlayerController>();
                player.ServerDie(netId);
                player.ServerApplyForce(forceToApply);
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

    [Server]
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

    [Server]
    private void PlayExplosion(GameObject prefab, Vector3 position, Vector3 scale)
    {
        if (currentExplosion != null)
        {
            DestroyCurrentExplosion();
        }

        currentExplosion = Instantiate(prefab, Vector3.zero, Quaternion.identity);
        currentExplosion.transform.localPosition = position;
        currentExplosion.transform.localScale = scale;

        NetworkServer.Spawn(currentExplosion, connectionToClient);

        Invoke(nameof(DestroyCurrentExplosion), 1.5f);
    }

    private void DestroyCurrentExplosion()
    {
        NetworkServer.Destroy(currentExplosion);
        currentExplosion = null;
    }

    public enum State
    {
        Charging = 0,
        Firing,
        Done
    }
}
