using UnityEngine;
using System.Collections;

public class ZoneAmbience : MonoBehaviour
{
    public AudioSource ambiance;
    public float fadeSpeed = 2f;   // Vitesse d'apparition/disparition

    public float targetVolume = 0.3f;
    Coroutine currentFade;

    void Start()
    {
        ambiance.volume = 0f;    // Démarre silencieux
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (currentFade != null) StopCoroutine(currentFade);
            currentFade = StartCoroutine(FadeTo(targetVolume));  // volume max
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (currentFade != null) StopCoroutine(currentFade);
            currentFade = StartCoroutine(FadeTo(0f));  // volume min
        }
    }

    IEnumerator FadeTo(float target)
    {
        while (!Mathf.Approximately(ambiance.volume, target))
        {
            ambiance.volume = Mathf.MoveTowards(
                ambiance.volume,
                target,
                Time.deltaTime * fadeSpeed
            );
            yield return null;
        }
    }
}
