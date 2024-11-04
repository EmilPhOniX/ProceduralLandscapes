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

    public int vitesse = 0;
    private int angle = 0;
    private bool IsInRotMode = false;
    private bool IsCharacterActive = false;


    // Variables pour la gestion de l'interface utilisateur (UI)
    public GameObject settingsCanvas;  // Interface pour le menu des paramètres
    public InputField dimensionInput;  // Champ de saisie pour la dimension
    public InputField resolutionInput; // Champ de saisie pour la résolution

    //Deformation usage
    [Range(1f, 50f)]
    public float radius = 25f;
    [Range(1f, 50f)]
    public float deformationStrength = 50f;

    public AnimationCurve attenuationCurve;
    private Vector3[] vertices, modifiedVerts;

    //pattern
    public List<AnimationCurve> patterns; // Liste des patterns
    private int patternIndex = 0; // Indice du pattern actuel

    //brush
    public List<Texture2D> brushTextures; // Liste des textures utilisées comme brushes
    private int brushIndex = 0; // Indice du brush actuellement sélectionné
    private bool useBrushMode = false; // Booléen pour indiquer le mode de déformation (false pour pattern, true pour brush)


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
        HandleBrushSwitch();
        ToggleDeformationMode();
    }

    void HandleDeformation()
    {
        RaycastHit hit;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out hit, Mathf.Infinity))
        {
            Vector3 hitPoint = hit.point;
            int closestVertexIndex = FindClosestVertex(hitPoint);

            // Applique la déformation selon le mode sélectionné
            if (useBrushMode)
            {
                ApplyBrushDeformation(closestVertexIndex); // Déformation avec brush
            }
            else
            {
                ApplyPatternDeformation(closestVertexIndex); // Déformation avec pattern
            }
            RecalculateMesh(); // Recalcule le mesh pour appliquer la déformation
        }
    }

    void ApplyPatternDeformation(int closestVertexIndex)
    {
        for (int v = 0; v < modifiedVerts.Length; v++)
        {
            Vector3 distance = modifiedVerts[v] - modifiedVerts[closestVertexIndex];

            // Vérifie que le vertex est dans le rayon du pattern
            if (distance.sqrMagnitude < radius * radius)
            {
                float normalizedDistance = distance.magnitude / radius;
                float force = deformationStrength * attenuationCurve.Evaluate(normalizedDistance);

                // Applique la déformation en fonction de la touche de la souris et du contrôle
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
    }

    void ApplyBrushDeformation(int closestVertexIndex)
    {
        Texture2D currentBrush = brushTextures[brushIndex];
        int brushSize = currentBrush.width;

        for (int v = 0; v < modifiedVerts.Length; v++)
        {
            Vector3 distance = modifiedVerts[v] - modifiedVerts[closestVertexIndex];

            if (distance.sqrMagnitude < radius * radius)
            {
                float normalizedDistance = distance.magnitude / radius;

                // Calcul des coordonnées dans la texture
                int pixelX = Mathf.FloorToInt((distance.x / radius + 0.5f) * brushSize);
                int pixelY = Mathf.FloorToInt((distance.z / radius + 0.5f) * brushSize);

                // Vérifie si les coordonnées sont dans les limites de la texture
                if (pixelX >= 0 && pixelX < brushSize && pixelY >= 0 && pixelY < brushSize)
                {
                    float pixelIntensity = currentBrush.GetPixel(pixelX, pixelY).r;
                    float force = deformationStrength * pixelIntensity;

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
        }
    }

    void HandleBrushSwitch()
    {
        if (Input.GetKeyDown(KeyCode.B) && useBrushMode)
        {
            brushIndex = (brushIndex + 1) % brushTextures.Count;
            Debug.Log("Brush changé : " + brushIndex);
        }
    }

    void ToggleDeformationMode()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            useBrushMode = !useBrushMode;
            Debug.Log(useBrushMode ? "Mode brush activé" : "Mode pattern activé");
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
        p_mesh = GetComponentInChildren<MeshFilter>().mesh;
        vertices = p_mesh.vertices;
        modifiedVerts = p_mesh.vertices;

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
        if (Input.GetKeyDown(KeyCode.RightAlt))
        {
            deformationStrength = Mathf.Min(deformationStrength + 1f, 50f);
        }

        else if (Input.GetKeyDown(KeyCode.LeftAlt))
        {
            deformationStrength = Mathf.Max(deformationStrength - 1f, 1f);
        }
    }

    void HandlePatternRadius()
    {
        if (Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.Plus)) // Augmente le rayon
        {
            radius = Mathf.Min(radius + 1f, 50f); // Limite max
        }
        else if (Input.GetKeyDown(KeyCode.Minus)) // Diminue le rayon
        {
            radius = Mathf.Max(radius - 1f, 1f); // Limite min
        }
    }

    void HandlePatternSwitch()
    {
        if (Input.GetKeyDown(KeyCode.P) && !useBrushMode)
        {
            patternIndex = (patternIndex + 1) % patterns.Count;
            attenuationCurve = patterns[patternIndex];
            Debug.Log("Pattern changé : " + patternIndex);
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