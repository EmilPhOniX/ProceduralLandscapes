using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class capsule : MonoBehaviour
{

    private MeshFilter p_meshFilter;
    private MeshCollider p_meshCollider;
    private MeshRenderer p_meshRenderer;
    public GameObject capsulePrefab;

    // Start is called before the first frame update
    void Start()
    {
        capsulePrefab.gameObject.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        LateUpdate();
    }

    private void LateUpdate()
    {
        if (Input.GetKey(KeyCode.F2))
        {
            capsulePrefab.gameObject.SetActive(true);
        }
    }
}
