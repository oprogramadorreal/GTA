using Mirror;
using UnityEngine;

public sealed class Billboard : MonoBehaviour
{
    [SerializeField]
    private Transform cameraTransform;

    private Quaternion originalRotation;

    private void Start()
    {
        originalRotation = transform.rotation;
    }

    [ClientCallback]
    private void Update()
    {
        if (cameraTransform != null)
        {
            transform.rotation = cameraTransform.rotation * originalRotation;
        }
    }

    public void SetCameraTransform(Transform t)
    {
        cameraTransform = t;
    }
}
