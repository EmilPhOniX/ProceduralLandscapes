using System.Collections;
using System.Collections.Generic;
using System.Data;
using UnityEngine;
using UnityEngine.UI;



public class TerrainController : MonoBehaviour
{
    // Déclaration des variables de la classe
    private Mesh p_mesh;
    private Vector3[] p_vertices;
    private Vector3[] p_normals;
    private int[] p_triangles;
    private MeshFilter p_meshFilter;
    private MeshCollider p_meshCollider;
    public bool CentrerPivot;
    public int dimension;
    public int resolution;

    private MeshRenderer p_meshRenderer;
    public GameObject capsulePrefab;
    public GameObject terrainPrefab; // Préfabriqué pour chaque chunk

    public int vitesse = 0;
    private int angle = 0;
    private bool IsInRotMode = false;
    private bool IsCharacterActive = false;
    private Dictionary<Vector2Int, GameObject> chunks = new Dictionary<Vector2Int, GameObject>();


    // Variables pour la gestion de l'interface utilisateur (UI)
    public GameObject settingsCanvas;  // Interface pour le menu des paramètres
    public InputField dimensionInput;  // Champ de saisie pour la dimension
    public InputField resolutionInput; // Champ de saisie pour la résolution

    //Deformation usage
    [Range(1.5f, 50f)]
    public float radius = 25f;
    [Range(0.5f, 50f)]
    public float deformationStrength = 50f;

    public AnimationCurve attenuationCurve;
    private Vector3[] vertices, modifiedVerts;

    //pattern
    public List<AnimationCurve> patterns; // Liste des patterns
    private int patternIndex = 0; // Indice du pattern actuel

    // Méthode appelée au démarrage
    void Start()
    {
        // Créer le terrain
        CreateInitialChunk();
        settingsCanvas.SetActive(false);

    }

    // Méthode appelée à chaque frame
    void Update()
    {
        HandleTerrainRotation();
        HandleCharacterSpawn();
        HandleDeformation();
        HandleDeformationIntensity();
        HandlePatternRadius();
        HandlePatternSwitch();
        ActivationCanvas();

        if (Input.GetKeyDown(KeyCode.F5))
        {
            if (Input.GetKey(KeyCode.UpArrow))
                AddChunk(Vector2Int.up);
            else if (Input.GetKey(KeyCode.DownArrow))
                AddChunk(Vector2Int.down);
            else if (Input.GetKey(KeyCode.LeftArrow))
                AddChunk(Vector2Int.left);
            else if (Input.GetKey(KeyCode.RightArrow))
                AddChunk(Vector2Int.right);
        }

        if (Input.GetKeyDown(KeyCode.M))
        {
            StartCoroutine(HighlightChunks());
        }

    }


    private void CreateInitialChunk()
    {
        GameObject initialChunk = Instantiate(terrainPrefab, Vector3.zero, Quaternion.identity);
        initialChunk.transform.SetParent(transform);
        chunks.Add(Vector2Int.zero, initialChunk);
        CreerTerrain(initialChunk);
    }


    private void AddChunk(Vector2Int direction)
    {
        Vector2Int newChunkPosition = Vector2Int.zero + direction;
        if (!chunks.ContainsKey(newChunkPosition))
        {
            GameObject newChunk = Instantiate(terrainPrefab);
            newChunk.transform.position = new Vector3(newChunkPosition.x * dimension, 0, newChunkPosition.y * dimension);
            newChunk.transform.SetParent(transform);
            chunks.Add(newChunkPosition, newChunk);

            SetChunkVertices(newChunk, direction);
        }
    }

    private void SetChunkVertices(GameObject newChunk, Vector2Int direction)
    {
        Mesh mesh = newChunk.GetComponent<MeshFilter>().mesh;
        Vector3[] vertices = mesh.vertices;

        if (chunks.ContainsKey(Vector2Int.zero - direction))
        {
            GameObject adjacentChunk = chunks[Vector2Int.zero - direction];
            Mesh adjacentMesh = adjacentChunk.GetComponent<MeshFilter>().mesh;
            Vector3[] adjacentVertices = adjacentMesh.vertices;

            int resolution = Mathf.RoundToInt(Mathf.Sqrt(vertices.Length)); // Nombre de vertices par ligne de chunk

            // Copier les vertices du bord en fonction de la direction
            if (direction == Vector2Int.up) // Nouveau chunk au-dessus
            {
                for (int i = 0; i < resolution; i++)
                {
                    vertices[i] = adjacentVertices[vertices.Length - resolution + i];
                }
            }
            else if (direction == Vector2Int.down) // Nouveau chunk en-dessous
            {
                for (int i = 0; i < resolution; i++)
                {
                    vertices[vertices.Length - resolution + i] = adjacentVertices[i];
                }
            }
            else if (direction == Vector2Int.left) // Nouveau chunk à gauche
            {
                for (int i = 0; i < resolution; i++)
                {
                    vertices[i * resolution] = adjacentVertices[i * resolution + (resolution - 1)];
                }
            }
            else if (direction == Vector2Int.right) // Nouveau chunk à droite
            {
                for (int i = 0; i < resolution; i++)
                {
                    vertices[i * resolution + (resolution - 1)] = adjacentVertices[i * resolution];
                }
            }
        }

        mesh.vertices = vertices;
        mesh.RecalculateNormals();
    }


    private IEnumerator HighlightChunks()
    {
        foreach (var chunk in chunks.Values)
        {
            Renderer renderer = chunk.GetComponent<Renderer>();
            Material originalMaterial = renderer.material;
            renderer.material.color = Random.ColorHSV();
            yield return new WaitForSeconds(3);
            renderer.material = originalMaterial;
        }
    }


    void HandleDeformation()
    {
        RaycastHit hit;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out hit, Mathf.Infinity))
        {
            Vector3 hitPoint = hit.point;

            foreach (var chunk in chunks.Values)
            {
                Mesh mesh = chunk.GetComponent<MeshFilter>().mesh;
                Vector3[] vertices = mesh.vertices;
                int closestVertexIndex = FindClosestVertex(hitPoint);

                for (int v = 0; v < vertices.Length; v++)
                {
                    Vector3 distance = vertices[v] - vertices[closestVertexIndex];

                    if (distance.sqrMagnitude < radius * radius)
                    {
                        float normalizedDistance = distance.magnitude / radius;
                        float force = deformationStrength * attenuationCurve.Evaluate(normalizedDistance);

                        if (Input.GetMouseButtonDown(0) && Input.GetKey(KeyCode.LeftControl))
                            vertices[v] += Vector3.down * force;
                        else if (Input.GetMouseButtonDown(0))
                            vertices[v] += Vector3.up * force;
                    }
                }
                mesh.vertices = vertices;
                mesh.RecalculateNormals();
            }
        }
    }

    int FindClosestVertex(Vector3 point)
    {
        int closestIndex = 0;
        float closestDistance = float.MaxValue;

        for (int i = 0; i < modifiedVerts.Length; i++)
        {
            float distance = (modifiedVerts[i] - point).sqrMagnitude;
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestIndex = i;
            }
        }
        return closestIndex;
    }

    void RecalculateMesh()
    {
        p_mesh.vertices = modifiedVerts;
        GetComponentInChildren<MeshCollider>().sharedMesh = p_mesh;
        p_mesh.RecalculateNormals();
    }

    // Méthode pour créer le terrain
    void CreerTerrain(GameObject terrainChunk)
    {
        Mesh mesh = new Mesh();
        Vector3[] vertices = new Vector3[resolution * resolution];
        int[] triangles = new int[6 * (resolution - 1) * (resolution - 1)];

        int indice_vertex = 0;
        for (int j = 0; j < resolution; j++)
        {
            for (int i = 0; i < resolution; i++)
            {
                vertices[indice_vertex] = new Vector3((float)i / resolution * dimension, 0, (float)j / resolution * dimension);
                indice_vertex++;
            }
        }

        int indice_triangle = 0;
        for (int j = 0; j < resolution - 1; j++)
        {
            for (int i = 0; i < resolution - 1; i++)
            {
                triangles[indice_triangle] = j * resolution + i;
                triangles[indice_triangle + 1] = (j + 1) * resolution + i;
                triangles[indice_triangle + 2] = j * resolution + (i + 1);
                triangles[indice_triangle + 3] = j * resolution + (i + 1);
                triangles[indice_triangle + 4] = (j + 1) * resolution + i;
                triangles[indice_triangle + 5] = (j + 1) * resolution + (i + 1);
                indice_triangle += 6;
            }
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        terrainChunk.GetComponent<MeshFilter>().mesh = mesh;
        terrainChunk.GetComponent<MeshCollider>().sharedMesh = mesh;
    }


    void HandleTerrainRotation()
    {
        // Rotation de l'objet si la touche RightControl est enfoncée
        if (Input.GetKeyDown(KeyCode.RightControl))
        {
            IsInRotMode = !IsInRotMode;
            Debug.Log(IsInRotMode ? "Rotation activé" : "Rotation désactivé");
        }

        if (IsInRotMode)
        {
            if (Input.GetKey(KeyCode.A)) //A correspond a Q en AZERTY
            {
                angle -= vitesse; // Rotation dans le sens inverse
                transform.rotation = Quaternion.Euler(0, angle, 0);
            }

            if (Input.GetKey(KeyCode.D))
            {
                angle += vitesse; // Rotation dans le sens horaire
                transform.rotation = Quaternion.Euler(0, angle, 0);
            }
        }
    }

    void HandleCharacterSpawn()
    {
        // Activer le prefab capsule si la touche F2 est enfoncée
        if (Input.GetKeyDown(KeyCode.F2))
        {
            IsCharacterActive = !IsCharacterActive;
            capsulePrefab.gameObject.SetActive(IsCharacterActive);
        }
    }

    void HandleDeformationIntensity()
    {
        if (Input.GetKeyDown(KeyCode.LeftAlt))
        {
            deformationStrength = Mathf.Min(deformationStrength + 0.1f, 5f);
        }

        else if (Input.GetKeyDown(KeyCode.RightAlt))
        {
            deformationStrength = Mathf.Max(deformationStrength - 0.1f, 0.5f);
        }
    }

    void HandlePatternRadius()
    {
        if (Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.Plus)) // Augmente le rayon
        {
            radius = Mathf.Min(radius + 0.5f, 5f); // Limite max
        }
        else if (Input.GetKeyDown(KeyCode.Minus)) // Diminue le rayon
        {
            radius = Mathf.Max(radius - 0.5f, 1.5f); // Limite min
        }
    }

    void HandlePatternSwitch()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            patternIndex = (patternIndex + 1) % patterns.Count;
            attenuationCurve = patterns[patternIndex]; // Applique le nouveau pattern
            Debug.Log("Pattern changé: " + patternIndex);
        }
    }

    void ActivationCanvas()
    {
        // Ouvrir/fermer le menu des paramètres avec F10
        if (Input.GetKeyDown(KeyCode.F10))
        {
            settingsCanvas.SetActive(!settingsCanvas.activeSelf);
        }

        // Appliquer les paramètres et recréer le terrain quand Enter est pressé
        if (Input.GetKeyDown(KeyCode.Return) && settingsCanvas.activeSelf)
        {
            ApplySettings();
        }
    }

    public void ApplySettings()
    {
        // Récupérer les nouvelles dimensions et résolution depuis les champs de saisie de l'UI
        if (int.TryParse(dimensionInput.text, out int newDimension) && int.TryParse(resolutionInput.text, out int newResolution))
        {
            // Mettre à jour les dimensions et la résolution avec les nouvelles valeurs
            dimension = newDimension;
            resolution = newResolution;

            // Supprimer tous les chunks existants
            foreach (var chunk in chunks.Values)
            {
                Destroy(chunk);
            }
            chunks.Clear();

            // Créer le chunk initial avec les nouvelles dimensions et résolution
            CreateInitialChunk();

            // Si des chunks supplémentaires avaient été ajoutés auparavant, 
            // on peut les récréer pour obtenir une grille complète avec continuité
            AddChunk(Vector2Int.up);
            AddChunk(Vector2Int.down);
            AddChunk(Vector2Int.left);
            AddChunk(Vector2Int.right);

            // Fermer le menu des paramètres
            settingsCanvas.SetActive(false);
        }
        else
        {
            Debug.LogWarning("Entrée invalide pour la dimension ou la résolution.");
        }
    }


}