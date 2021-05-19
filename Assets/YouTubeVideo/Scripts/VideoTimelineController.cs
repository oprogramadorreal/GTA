using UnityEngine;
using UnityEngine.Playables;

public class VideoTimelineController : MonoBehaviour
{
    [SerializeField]
    private PlayableDirector playable;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            playable.Play();
        }
    }
}
