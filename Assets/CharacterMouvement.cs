using System.Collections;
using UnityEngine;

public class CharacterController : MonoBehaviour
{
    public GameObject character; // Capsule + Sphere (Corps + Tête)
    public Camera thirdPersonCamera;
    public Camera firstPersonCamera;
    public float moveSpeed = 5f;
    public float transitionDuration = 0.5f;

    private Vector3 destination;
    private bool isMoving = false;
    private bool isControlMode = false;
    private bool isFirstPerson = false;

    public LayerMask terrainLayer;  // Ajoute cette ligne pour assigner le Layer dans Unity


    void Start()
    {
        // Définir l'état initial : désactiver le personnage, activer la caméra à la troisième personne
        firstPersonCamera.enabled = false;
        thirdPersonCamera.enabled = true;
        character.SetActive(false); // Masquer le personnage initialement
    }

    void Update()
    {
        HandleControlModeToggle();
        HandleCameraSwitch();
        HandleMovement();
    }

    void HandleControlModeToggle()
    {
        // Basculer le mode de contrôle avec F2
        if (Input.GetKeyDown(KeyCode.F2))
        {
            isControlMode = !isControlMode;
            character.SetActive(isControlMode);
        }

        // Quitter le mode de contrôle avec Échap
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            isControlMode = false;
            character.SetActive(false);
        }
    }

    void HandleCameraSwitch()
    {
        // Basculer entre les caméras à la première et à la troisième personne avec Left Control
        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            isFirstPerson = !isFirstPerson;
            StartCoroutine(SwitchCamera(isFirstPerson));
        }
    }

    void HandleMovement()
    {
        // Déplacer le personnage avec un clic gauche de la souris lorsque le mode de contrôle est actif
        if (Input.GetMouseButtonDown(0))
        {
            //S'adapte en fonction de si la camera est TPS ou FPS
            Ray ray;
            if (thirdPersonCamera.enabled)
            {
                ray = thirdPersonCamera.ScreenPointToRay(Input.mousePosition);
            }
            else { ray = firstPersonCamera.ScreenPointToRay(Input.mousePosition); }
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, terrainLayer))
            {
                if (hit.collider.CompareTag("Terrain"))
                {
                    destination = hit.point;
                    isMoving = true;
                    OrientCharacter(destination);
                }
            }
            else
            {
                Debug.Log("Raycast didn't hit anything"); // Affiche si rien n'est touché
            }
        }

        if (isControlMode || isMoving)
        {
            MoveCharacter();
        }
    }

    // Tourner le personnage vers la destination
    void OrientCharacter(Vector3 target)
    {
        Vector3 direction = (target - character.transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
        character.transform.rotation = Quaternion.Slerp(character.transform.rotation, lookRotation, Time.deltaTime * 5f);
        Debug.Log("Orienting character towards target at: " + target);
    }

    // Déplacer le personnage vers la destination
    void MoveCharacter()
    {
        if (isMoving)
        {
            float step = moveSpeed * Time.deltaTime;
            character.transform.position = Vector3.MoveTowards(character.transform.position, destination, step);
            Debug.Log("Moving character to: " + destination);  // Vérifie que le mouvement est bien effectué

            if (Vector3.Distance(character.transform.position, destination) < 0.1f)
            {
                isMoving = false;
                Debug.Log("Character arrived at destination");  // Vérifie que l'arrêt du mouvement est correct
            }
        }
    }


    // Transition fluide de la caméra entre la première et la troisième personne
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