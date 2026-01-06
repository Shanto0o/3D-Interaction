using UnityEngine;
public class AudioDistanceTrigger : MonoBehaviour{
    public Transform player;
    public AudioSource audioSource;
    public float playInterval = 3f; // Intervalle en secondes entre chaque son
    
    private float timer = 0f;

    void Update(){
        float distance = Vector3.Distance(player.position, transform.position);
        audioSource.pitch = Mathf.Lerp(0.8f, 1.4f, 1 / (distance + 0.1f));
        
        // Jouer le son toutes les X secondes
        timer += Time.deltaTime;
        if (timer >= playInterval){
            audioSource.Play();
            timer = 0f;
        }
    }
}
