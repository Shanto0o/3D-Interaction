using UnityEngine;

public class AudioVelocity : MonoBehaviour{
    public Rigidbody rb;
    public AudioSource audioSource;

    void Update(){
        float v = rb.linearVelocity.magnitude;
        audioSource.volume = Mathf.Lerp(0f, 1f, v / 5f);
        audioSource.pitch = Mathf.Lerp(1f, 2f, v / 5f);
    }
}
