using UnityEngine;

public sealed class CapsizeDetector : MonoBehaviour
{
    private bool fixRequested = false;

    private void Start()
    {
        InvokeRepeating(nameof(CheckCapsizing), 1.0f, 1.0f);
    }

    private void CheckCapsizing()
    {
        if (!fixRequested && NeedsFix())
        {
            Invoke(nameof(FixCapsizing), 2.0f);
            fixRequested = true;
        }
    }

    private void FixCapsizing()
    {
        if (NeedsFix()) // still need fix?
        {
            var rot = transform.localEulerAngles;
            transform.localEulerAngles = new Vector3(rot.x, rot.y, 0.0f);
        }

        fixRequested = false;
    }

    private bool NeedsFix()
    {
        //var up = transform.TransformDirection(Vector3.up);
        return Vector3.Angle(transform.up, Vector3.up) > 85.0f;
    }
}
