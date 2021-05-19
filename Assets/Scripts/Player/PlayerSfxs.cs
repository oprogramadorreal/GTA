using Mirror;
using System.Collections;
using UnityEngine;

public sealed class PlayerSfxs : NetworkBehaviour
{
    private AudioManager audioManager;

    private AudioSource walkingSound;

    [SyncVar(hook = nameof(HandleIsMovingUpdated))]
    private bool isMoving = false;

    [SyncVar(hook = nameof(HandleIsJumpingUpdated))]
    private bool isJumping = false;

    [SyncVar(hook = nameof(HandleKamehamehaStateUpdated))]
    private PlayerKamehameha.State kamehamehaState = PlayerKamehameha.State.Done;

    [SerializeField]
    private Transform playerFeet;

    [SerializeField]
    private PlayerCarController carController;

    private AudioSource cityBackground;

    private AudioSource carEngineSound;

    private readonly AudioSource[] carRadioMusic = new AudioSource[3];

    public void ClientSetup(AudioManager audio)
    {
        audioManager = audio;
        walkingSound = audioManager.CreateAudioSourceWithin("Walking", transform);

        carEngineSound = audioManager.CreateAudioSourceWithin("Car", transform);

        carRadioMusic[0] = audioManager.CreateAudioSourceWithin("CarRadioA", transform);
        carRadioMusic[1] = audioManager.CreateAudioSourceWithin("CarRadioB", transform);
        carRadioMusic[2] = audioManager.CreateAudioSourceWithin("CarRadioC", transform);

        cityBackground = audioManager.CreateAudioSource("CityBackground");
    }

    public override void OnStopClient()
    {
        cityBackground.Stop();
    }

    [Client]
    public void OnMoveEvent(bool isMoving)
    {
        if (hasAuthority)
        {
            CmdOnMoveEvent(isMoving);
        }
    }

    [Command]
    private void CmdOnMoveEvent(bool value)
    {
        isMoving = value;
    }

    [Client]
    private void HandleIsMovingUpdated(bool oldValue, bool newValue)
    {
        if (newValue)
        {
            if (!isJumping && !walkingSound.isPlaying)
            {
                walkingSound.Play();
            }
        }
        else
        {
            walkingSound.Stop();
        }
    }

    [Client]
    public void OnCarEnterEvent()
    {
        if (hasAuthority)
        {
            CmdOnMoveEvent(false);
            CmdPlayCarBeginSound();
        }
    }

    [Client]
    public void OnCarLeaveEvent()
    {
        if (hasAuthority)
        {
            CmdPlayCarEndSound();
        }
    }

    [Command]
    private void CmdPlayCarBeginSound()
    {
        RpcPlayCarBeginSound();
    }

    [ClientRpc]
    private void RpcPlayCarBeginSound()
    {
        Invoke(nameof(PlayCarBeginSound), 2.8f);
    }

    [Client]
    private void PlayCarBeginSound()
    {
        audioManager.CreateTemporaryAudioSourceWithin("CarBegin", transform);
        Invoke(nameof(PlayCarEngineSound), 3.4f);
    }

    [Client]
    private void PlayCarEngineSound()
    {
        carRadioMusic[carController.GetClosestCarId()].Play();
        carEngineSound.Play();
    }

    [Command]
    private void CmdPlayCarEndSound()
    {
        RpcPlayCarEndSound();
    }

    [ClientRpc]
    private void RpcPlayCarEndSound()
    {
        StartCoroutine(StopCarSound());
    }

    [Client]
    private IEnumerator StopCarSound()
    {
        while (!carEngineSound.isPlaying)
        {
            yield return null;
        }

        carEngineSound.Stop();
        foreach (var c in carRadioMusic) c.Stop();
        audioManager.CreateTemporaryAudioSourceWithin("CarEnd", transform);
    }

    [Client]
    public void OnJumpEvent(bool isJumping)
    {
        if (hasAuthority)
        {
            CmdOnJumpEvent(isJumping);
        }
    }

    [Command]
    private void CmdOnJumpEvent(bool value)
    {
        isJumping = value;
    }

    [Client]
    private void HandleIsJumpingUpdated(bool oldValue, bool newValue)
    {
        if (newValue)
        {
            if (isMoving)
            {
                walkingSound.Stop();
            }
        }
        else
        {
            audioManager.CreateTemporaryAudioSourceAt("Landing", playerFeet.position);

            if (isMoving && !walkingSound.isPlaying)
            {
                walkingSound.Play();
            }
        }
    }

    [Client]
    public void OnKamehamehaFireEvent(PlayerKamehameha.State state)
    {
        CmdOnKamehamehaFireEvent(state);
    }

    [Command]
    private void CmdOnKamehamehaFireEvent(PlayerKamehameha.State state)
    {
        kamehamehaState = state;
    }

    [Client]
    private void HandleKamehamehaStateUpdated(PlayerKamehameha.State oldValue, PlayerKamehameha.State newValue)
    {
        if (newValue == PlayerKamehameha.State.Charging)
        {
            audioManager.CreateTemporaryAudioSourceWithin("Kamehameha", playerFeet);
        }
    }

    [Client]
    public void OnKamehamehaExplosionEvent(Vector3 explosionCenter)
    {
        if (hasAuthority)
        {
            CmdOnKamehamehaExplosionEvent(explosionCenter);
        }
    }

    [Command]
    private void CmdOnKamehamehaExplosionEvent(Vector3 explosionCenter)
    {
        RpcOnKamehamehaExplosionEvent(explosionCenter);
    }

    [ClientRpc]
    private void RpcOnKamehamehaExplosionEvent(Vector3 explosionCenter)
    {
        audioManager.CreateTemporaryAudioSourceAt("Explosion", explosionCenter);
    }

    [Client]
    public void OnDieEvent()
    {
        CmdOnDieEvent();
    }

    [Command]
    private void CmdOnDieEvent()
    {
        RpcOnDieEvent();
    }

    [ClientRpc]
    private void RpcOnDieEvent()
    {
        audioManager.CreateTemporaryAudioSourceWithin("Die", playerFeet);
    }
}
