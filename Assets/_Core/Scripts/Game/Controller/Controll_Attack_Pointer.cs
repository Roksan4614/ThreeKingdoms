using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class Controll_Attack_Pointer : MonoBehaviour
{
    public Character_Enemy enemy { get; private set; }

    private void OnTriggerEnter2D(Collider2D _collision)
    {
        if (_collision.CompareTag("CharacterBody"))
        {
            enemy = _collision.transform.parent.parent.parent.GetComponent<Character_Enemy>();
        }
    }

    private void OnTriggerExit2D(Collider2D _collision)
    {
        if (_collision.CompareTag("CharacterBody"))
        {
            if (enemy == _collision.transform.parent.parent.parent.GetComponent<Character_Enemy>())
                enemy = null;
        }
    }
}
