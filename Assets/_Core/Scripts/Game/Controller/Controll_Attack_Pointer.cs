using UnityEngine;
using UnityEngine.Events;
using static UnityEngine.GraphicsBuffer;

public class Controll_Attack_Pointer : MonoBehaviour
{
    public UnityAction<Collider2D> OnTriggerEnter;
    public UnityAction<Collider2D> OnTriggerExit;

    private void OnTriggerEnter2D(Collider2D _collision)
    {
        if (_collision.CompareTag("CharacterBody"))
            OnTriggerEnter?.Invoke(_collision);
    }

    private void OnTriggerExit2D(Collider2D _collision)
    {
        if (_collision.CompareTag("CharacterBody"))
            OnTriggerExit?.Invoke(_collision);
    }
}
