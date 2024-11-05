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
    public int dimension = 0;
    public int resolution = 8;

    public GameObject terrainPrefab;
    public int extensionSize = 300;

    private MeshRenderer p_meshRenderer;
    public GameObject capsulePrefab;

    public int vitesse = 0;
    private int angle = 0;
    private bool IsInRotMode = false;
    private bool IsCharacterActive = false;


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
        CreerTerrain();
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

        if (Input.GetKey(KeyCode.F5))
        {
            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                AddTerrainExtension(Vector3.forward);
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                AddTerrainExtension(Vector3.back);
            }
            else if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                AddTerrainExtension(Vector3.left);
            }
            else if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                AddTerrainExtension(Vector3.right);
            }
        }

        if (Input.GetKeyDown(KeyCode.M))
        {
            HighlightTerrainChunks();
        }
    }

    void HandleDeformation()
    {
        RaycastHit hit;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out hit, Mathf.Infinity))
        {
            Vector3 hitPoint = hit.point;

            int closestVertexIndex = FindClosestVertex(hitPoint);

            // Appliquer le pattern aux vertices dans le rayon
            for (int v = 0; v < modifiedVerts.Length; v++)
            {
                Vector3 distance = modifiedVerts[v] - modifiedVerts[closestVertexIndex];

                // Vérifier que le vertex est dans le rayon du pattern
                if (distance.sqrMagnitude < radius * radius)
                {
                    float normalizedDistance = distance.magnitude / radius;
                    float force = deformationStrength * attenuationCurve.Evaluate(normalizedDistance);

                    if (Input.GetMouseButtonDown(0) && Input.GetKey(KeyCode.LeftControl))
                    {
                        modifiedVerts[v] += Vector3.down * force;

                    }
                    else if (Input.GetMouseButtonDown(0))
                    {
                        modifiedVerts[v] += Vector3.up * force;
                    }
                }
            }
            RecalculateMesh();
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



    void CreerTerrain()
    {
        p_mesh = new Mesh();
        p_mesh.Clear();
        p_mesh.name = "ProceduralTerrainMESH";

        p_vertices = new Vector3[resolution * resolution];
        p_normals = new Vector3[p_vertices.Length];
        p_triangles = new int[3 * 2 * (resolution - 1) * (resolution - 1)];

        p_meshFilter = GetComponent<MeshFilter>();
        p_meshCollider = GetComponent<MeshCollider>();

        int indice_vertex = 0;
        for (int j = 0; j < resolution; j++)
        {
            for (int i = 0; i < resolution; i++)
            {
                p_vertices[indice_vertex] = new Vector3((float)i / resolution * dimension, 0, (float)j / resolution * dimension);
                p_normals[indice_vertex] = new Vector3(0, 1, 0);
                indice_vertex++;
            }
        }

        if (CentrerPivot)
        {
            Vector3 decalCentrage = new Vector3(dimension / 2, 0, dimension / 2);
            for (int k = 0; k < p_vertices.Length; k++)
                p_vertices[k] -= decalCentrage;
        }

        int indice_triangle = 0;
        for (int j = 0; j < resolution - 1; j++)
        {
            for (int i = 0; i < resolution - 1; i++)
            {
                p_triangles[indice_triangle + 0] = j * resolution + i;
                p_triangles[indice_triangle + 1] = (j + 1) * resolution + i;
                p_triangles[indice_triangle + 2] = j * resolution + (i + 1);
                indice_triangle += 3;
                p_triangles[indice_triangle + 0] = j * resolution + (i + 1);
                p_triangles[indice_triangle + 1] = (j + 1) * resolution + i;
                p_triangles[indice_triangle + 2] = (j + 1) * resolution + (i + 1);
                indice_triangle += 3;
            }
        }

        p_mesh.vertices = p_vertices;
        p_mesh.normals = p_normals;
        p_mesh.triangles = p_triangles;

        p_meshFilter.mesh = p_mesh;
        p_meshCollider.sharedMesh = null;
        p_meshCollider.sharedMesh = p_meshFilter.mesh;
        vertices = p_mesh.vertices;
        modifiedVerts = p_mesh.vertices;
    }

    // Ajout d'une extension de terrain
    void AddTerrainExtension(Vector3 direction)
    {
        // Utilisation de la dimension réelle pour positionner précisément le chunk suivant
        Vector3 spawnPosition = transform.position + direction * (dimension * (resolution - 1) / (float)resolution);
        GameObject newTerrain = Instantiate(terrainPrefab, spawnPosition, Quaternion.identity);
        TerrainController newTerrainGenerator = newTerrain.GetComponent<TerrainController>();

        newTerrainGenerator.dimension = dimension;
        newTerrainGenerator.resolution = resolution;
        newTerrainGenerator.CreerTerrain();

        // Aligner les bords du nouveau chunk avec le chunk voisin
        AlignEdgesWithNeighbor(newTerrainGenerator, direction);
    }

    // Alignement des bords pour continuité entre chunks
    void AlignEdgesWithNeighbor(TerrainController newTerrain, Vector3 direction)
    {
        Vector3[] newVertices = newTerrain.p_mesh.vertices;

        if (direction == Vector3.forward) // Alignement nord
        {
            for (int i = 0; i < resolution; i++)
            {
                newVertices[i] = p_vertices[(resolution - 1) * resolution + i]; // Copier le bord sud de l'ancien terrain
            }
        }
        else if (direction == Vector3.back) // Alignement sud
        {
            for (int i = 0; i < resolution; i++)
            {
                newVertices[(resolution - 1) * resolution + i] = p_vertices[i]; // Copier le bord nord de l'ancien terrain
            }
        }
        else if (direction == Vector3.left) // Alignement ouest
        {
            for (int i = 0; i < resolution; i++)
            {
                newVertices[i * resolution + (resolution - 1)] = p_vertices[i * resolution]; // Copier le bord est de l'ancien terrain
            }
        }
        else if (direction == Vector3.right) // Alignement est
        {
            for (int i = 0; i < resolution; i++)
            {
                newVertices[i * resolution] = p_vertices[i * resolution + (resolution - 1)]; // Copier le bord ouest de l'ancien terrain
            }
        }

        newTerrain.p_mesh.vertices = newVertices;
        newTerrain.p_mesh.RecalculateNormals();
        newTerrain.p_meshCollider.sharedMesh = newTerrain.p_meshFilter.mesh;
    }


    void HighlightTerrainChunks()
    {

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
        if (int.TryParse(dimensionInput.text, out int newDimension) && int.TryParse(resolutionInput.text, out int newResolution))
        {
            dimension = newDimension;
            resolution = newResolution;
            CreerTerrain();  // Recréer le terrain avec les nouvelles valeurs
            settingsCanvas.SetActive(false);  // Fermer le menu des paramètres
        }
        else
        {
            Debug.LogWarning("Entrée invalide pour la dimension ou la résolution.");
        }
    }

}