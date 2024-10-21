using System.Collections;
using UnityEngine;

public class CharacterController : MonoBehaviour
{
    public GameObject character; // Capsule+Sphere (Body+Head)
    public Camera thirdPersonCamera;
    public Camera firstPersonCamera;
    public float moveSpeed = 5f;
    public float transitionDuration = 0.5f;

    private Vector3 destination;
    private bool isMoving = false;
    private bool isControlMode = false;
    private bool isFirstPerson = false;

    void Start()
    {
        // Set initial state
        firstPersonCamera.enabled = false;
        thirdPersonCamera.enabled = true;
        character.SetActive(false); // Hide character initially
    }

    void Update()
    {
        // Toggle control mode with F2
        if (Input.GetKeyDown(KeyCode.F2))
        {
            isControlMode = !isControlMode;
            character.SetActive(isControlMode);
        }

        // Handle exit from movement mode
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            isControlMode = false;
            character.SetActive(false);
        }

        // Camera toggle between 1P and 3P
        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            isFirstPerson = !isFirstPerson;
            StartCoroutine(SwitchCamera(isFirstPerson));
        }

        if (isControlMode && Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                destination = hit.point;
                isMoving = true;
                OrientCharacter(destination);
            }
        }

        if (isMoving)
        {
            MoveCharacter();
        }
    }

    // Rotate character towards the destination
    void OrientCharacter(Vector3 target)
    {
        Vector3 direction = (target - character.transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
        character.transform.rotation = Quaternion.Slerp(character.transform.rotation, lookRotation, Time.deltaTime * 5f);
    }

    // Move character towards the destination
    void MoveCharacter()
    {
        float step = moveSpeed * Time.deltaTime;
        character.transform.position = Vector3.MoveTowards(character.transform.position, destination, step);

        if (Vector3.Distance(character.transform.position, destination) < 0.1f)
        {
            isMoving = false;
        }
    }

    // Smooth camera transition between 1P and 3P
    IEnumerator SwitchCamera(bool firstPerson)
    {
        float elapsedTime = 0;

        while (elapsedTime < transitionDuration)
        {
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        firstPersonCamera.enabled = firstPerson;
        thirdPersonCamera.enabled = !firstPerson;
    }
}