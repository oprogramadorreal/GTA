using Cinemachine;
using UnityEngine;

public sealed class CameraManager : MonoBehaviour
{
    [SerializeField]
    private CinemachineFreeLook gameCamera;

    private CinemachineVirtualCamera carCamera;

    public void LookAtPlayer(float delay)
    {
        Invoke(nameof(LookAtPlayerImpl), delay);
    }

    public void LookAtCar(CinemachineVirtualCamera carCamera, float delay)
    {
        this.carCamera = carCamera;
        Invoke(nameof(LookAtCarImpl), delay);
    }

    private void LookAtCarImpl()
    {
        carCamera.Priority = 11;
    }

    private void LookAtPlayerImpl()
    {
        if (carCamera != null)
        {
            carCamera.Priority = 9;
        }
    }
}
