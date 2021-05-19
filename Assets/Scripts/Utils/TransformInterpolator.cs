using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Based on https://gamedevbeginner.com/the-right-way-to-lerp-in-unity-with-examples/
/// </summary>
public sealed class TransformInterpolator : MonoBehaviour
{
    private Queue<Target> targets = new Queue<Target>();
    private Target currentTarget = null;

    private void Update()
    {
        if (currentTarget == null && targets.Count > 0)
        {
            currentTarget = targets.Dequeue();
            StartCoroutine(LerpToCurrentTarget());
        }
    }

    public bool IsWorking()
    {
        return currentTarget != null
            || targets.Count > 0;
    }

    private IEnumerator LerpToCurrentTarget()
    {
        var timeAcc = 0.0f;

        var startPosition = transform.localPosition;
        var startRotation = transform.localRotation;

        while (timeAcc < currentTarget.duration)
        {
            var t = timeAcc / currentTarget.duration;
            t = t * t * (3f - 2f * t);

            if (currentTarget.position.HasValue)
            {
                transform.localPosition = Vector3.Lerp(startPosition, currentTarget.position.Value, t);
            }

            if (currentTarget.rotation.HasValue)
            {
                transform.localRotation = Quaternion.Lerp(startRotation, currentTarget.rotation.Value, t);
            }

            timeAcc += Time.deltaTime;
            yield return null;
        }

        currentTarget = null;
    }

    public void LerpTo(Vector3 targetPosition, float duration)
    {
        targets.Enqueue(
            new Target
            {
                position = targetPosition,
                rotation = null,
                duration = duration
            }
        );
    }

    public void LerpTo(Vector3 targetPosition, Quaternion targetRotation, float duration)
    {
        targets.Enqueue(
            new Target
            {
                position = targetPosition,
                rotation = targetRotation,
                duration = duration
            }
        );
    }

    private sealed class Target
    {
        public Vector3? position;
        public Quaternion? rotation;
        public float duration;
    }
}
