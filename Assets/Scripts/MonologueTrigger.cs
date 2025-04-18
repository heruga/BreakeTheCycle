using UnityEngine;

public class MonologueTrigger : MonoBehaviour
{
    public int monologueID = 0;

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Триггер сработал с объектом: " + other.name);
        
        if (other.CompareTag("Player"))
        {
            Debug.Log("Объект определён как игрок");
            MonologueManager manager = other.GetComponent<MonologueManager>();
            
            if (manager != null)
            {
                Debug.Log("MonologueManager найден, вызываем монолог");
                manager.PlayMonologue(monologueID);
            }
            else
            {
                Debug.LogError("MonologueManager не найден на игроке!");
            }
        }
    }
} 