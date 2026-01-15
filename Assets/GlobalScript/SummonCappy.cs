using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SummonCappy : MonoBehaviour
{
    public GameObject cappyPrefab;
    public float forceAmount = 500f;
    public Vector3 forceDirection = Vector3.forward;
    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(SpawnCappy());
    }

    IEnumerator SpawnCappy()
    {   
        while (true)
        {
            GameObject cappyInstance = Instantiate(cappyPrefab, transform.position, Quaternion.identity);
            Rigidbody cappyRigidbody = cappyInstance.GetComponent<Rigidbody>();
            if (cappyRigidbody != null)
            {
                cappyRigidbody.AddForce(forceDirection.normalized * forceAmount);
            }
            yield return new WaitForSeconds(2.5f);
        }
        
    }
}
