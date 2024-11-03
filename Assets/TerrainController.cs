using System.Collections.Generic;
using UnityEngine;

public class TerrainTerrainController : MonoBehaviour
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
    public int vitesse = 0;
    private int angle = 0;
    private bool IsInRotMode = false;
    private bool IsCharacterActive = false;
    [Range(1.5f, 5f)]
    public float radius = 2f;
    [Range(0.5f, 5f)]
    public float deformationStrength = 2f;
    public AnimationCurve attenuationCurve;
    private Vector3[] vertices, modifiedVerts;
    public List<AnimationCurve> patterns; // Liste des patterns
    private int patternIndex = 0; // Indice du pattern actuel

    enum DistanceType { Euclidean, Manhattan, Chebyshev }
    private DistanceType currentDistanceType = DistanceType.Euclidean;
    private DistanceType neighborDistanceType = DistanceType.Euclidean;

    // Méthode appelée au démarrage
    void Start()
    {
        CreerTerrain();

        p_mesh = GetComponentInChildren<MeshFilter>().mesh;
        vertices = p_mesh.vertices;
        modifiedVerts = p_mesh.vertices;
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
    }

    // ---Fonctions de gestion du terrain---

    void CreerTerrain()
    {
        // Initialisation du mesh
        p_mesh = new Mesh();
        p_mesh.Clear();
        p_mesh.name = "ProceduralTerrainMESH";

        // Initialisation des tableaux de vertices, normales et triangles
        p_vertices = new Vector3[resolution * resolution];
        p_normals = new Vector3[p_vertices.Length];
        p_triangles = new int[3 * 2 * (resolution - 1) * (resolution - 1)];

        // Récupération des composants MeshFilter et MeshCollider
        p_meshFilter = GetComponent<MeshFilter>();
        p_meshCollider = GetComponent<MeshCollider>();

        int indice_vertex = 0;
        // Boucle pour définir les vertices et les normales
        for (int j = 0; j < resolution; j++)
        {
            for (int i = 0; i < resolution; i++)
            {
                p_vertices[indice_vertex] = new Vector3((float)i / resolution * dimension, 0, (float)j / resolution * dimension);
                p_normals[indice_vertex] = new Vector3(0, 1, 0);
                indice_vertex++;
            }
        }

        // Centrer le pivot si nécessaire
        if (CentrerPivot)
        {
            Vector3 decalCentrage = new Vector3(dimension / 2, 0, dimension / 2);
            for (int k = 0; k < p_vertices.Length; k++)
                p_vertices[k] -= decalCentrage;
        }

        int indice_triangle = 0;
        // Boucle pour définir les triangles
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

        // Assignation des vertices, normales et triangles au mesh
        p_mesh.vertices = p_vertices;
        p_mesh.normals = p_normals;
        p_mesh.triangles = p_triangles;

        // Assignation du mesh au MeshFilter et au MeshCollider
        p_meshFilter.mesh = p_mesh;
        p_meshCollider.sharedMesh = null;
        p_meshCollider.sharedMesh = p_meshFilter.mesh;
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

    void HandleDeformation()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            currentDistanceType = (DistanceType)(((int)currentDistanceType + 1) % 3);
            Debug.Log("Distance type pour le vertex le plus proche changé: " + currentDistanceType);
        }

        if (Input.GetKeyDown(KeyCode.V))
        {
            neighborDistanceType = (DistanceType)(((int)neighborDistanceType + 1) % 3);
            Debug.Log("Distance type pour la recherche des voisins changé: " + neighborDistanceType);
        }

        RaycastHit hit;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out hit, Mathf.Infinity))
        {
            Vector3 hitPoint = hit.point;

            int closestVertexIndex = FindClosestVertex(hitPoint, currentDistanceType);

            for (int v = 0; v < modifiedVerts.Length; v++)
            {
                float distanceToVertex = CalculateDistance(modifiedVerts[v], modifiedVerts[closestVertexIndex], neighborDistanceType);

                // Vérifier que le vertex est dans le rayon
                if (distanceToVertex < radius)
                {
                    float normalizedDistance = distanceToVertex / radius;
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

    void HandleDeformationIntensity()
    {
        if (Input.GetKeyDown(KeyCode.RightAlt))
        {
            deformationStrength = Mathf.Min(deformationStrength + 0.1f, 5f);
        }

        else if (Input.GetKeyDown(KeyCode.LeftAlt))
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

    // ---Fonctions utilitaires---

    void RecalculateMesh()
    {
        p_mesh.vertices = modifiedVerts;
        GetComponentInChildren<MeshCollider>().sharedMesh = p_mesh;
        p_mesh.RecalculateNormals();
    }

    float CalculateDistance(Vector3 pointA, Vector3 pointB, DistanceType distanceType)
    {
        switch (distanceType)
        {
            case DistanceType.Manhattan:
                return Mathf.Abs(pointA.x - pointB.x) + Mathf.Abs(pointA.y - pointB.y) + Mathf.Abs(pointA.z - pointB.z);
            case DistanceType.Chebyshev:
                return Mathf.Max(Mathf.Abs(pointA.x - pointB.x), Mathf.Abs(pointA.y - pointB.y), Mathf.Abs(pointA.z - pointB.z));
            default:
                return Vector3.Distance(pointA, pointB);
        }
    }

    // Modification de FindClosestVertex pour accepter un type de distance en paramètre (Phase 1)
    int FindClosestVertex(Vector3 point, DistanceType distanceType)
    {
        int closestIndex = -1;
        float closestDistance = Mathf.Infinity;

        // Parcours des vertices du triangle sélectionné
        for (int i = 0; i < modifiedVerts.Length; i++)
        {
            float distance = CalculateDistance(point, modifiedVerts[i], distanceType);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestIndex = i;
            }
        }
        return closestIndex;
    }
}