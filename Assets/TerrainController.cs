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
    public float deformationStrength = 25f;

    public AnimationCurve attenuationCurve;
    private Vector3[] vertices, modifiedVerts;
    public List<AnimationCurve> patterns; // Liste des patterns
    private int patternIndex = 0; // Indice du pattern actuel
    private bool useApproximation = false; // Active ou d�sactive l'approximation
    private bool recalculateSelectiveNormals = false;
    private bool isDeforming = false;
    private bool useGridSpaceForNeighbors = true;

    enum DistanceType { Euclidean, Manhattan, Chebyshev }
    private DistanceType currentDistanceType = DistanceType.Euclidean;
    private DistanceType neighborDistanceType = DistanceType.Euclidean;

    //brush
    public List<Texture2D> brushTextures; // Liste des textures utilisées comme brushes
    private int brushIndex = 0; // Indice du brush actuellement sélectionné
    private bool useBrushMode = false; // Booléen pour indiquer le mode de déformation (false pour pattern, true pour brush)

    private Stack<Vector3[]> undoStack = new Stack<Vector3[]>(); // Pile pour Undo
    private Stack<Vector3[]> redoStack = new Stack<Vector3[]>(); // Pile pour Redo


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
        HandleUndoRedo();
    }
    
    void HandleUndoRedo()
    {
        if (Input.GetKeyDown(KeyCode.J)) // Undo avec la touche J
        {
            UndoDeformation();
        }
        else if (Input.GetKeyDown(KeyCode.K)) // Redo avec la touche K
        {
            RedoDeformation();
        }
    }

    // Fonction pour annuler la dernière déformation
    void UndoDeformation()
    {
        if (undoStack.Count > 0)
        {
            redoStack.Push((Vector3[])modifiedVerts.Clone()); // Sauvegarde dans Redo avant l'annulation
            modifiedVerts = undoStack.Pop();
            RecalculateMesh();
            UpdateMeshCollider();
        }
    }

    // Fonction pour rétablir une déformation annulée
    void RedoDeformation()
    {
        if (redoStack.Count > 0)
        {
            undoStack.Push((Vector3[])modifiedVerts.Clone()); // Sauvegarde dans Undo avant le rétablissement
            modifiedVerts = redoStack.Pop();
            RecalculateMesh();
            UpdateMeshCollider();
        }
    }
    void SaveCurrentState()
    {
        undoStack.Push((Vector3[])modifiedVerts.Clone()); // Sauvegarde de l'état actuel pour Undo
        redoStack.Clear(); // Réinitialise la pile Redo
    }

    void ApplyPatternDeformation(int closestVertexIndex)
    {
        SaveCurrentState(); // Sauvegarde de l'état initial

        for (int v = 0; v < modifiedVerts.Length; v++)
        {
            Vector3 distance = modifiedVerts[v] - modifiedVerts[closestVertexIndex];

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
        UpdateMeshCollider();
    }

    void ApplyBrushDeformation(int closestVertexIndex)
    {
        SaveCurrentState(); // Sauvegarde de l'état initial

        Texture2D currentBrush = brushTextures[brushIndex];
        int brushSize = currentBrush.width;

        for (int v = 0; v < modifiedVerts.Length; v++)
        {
            Vector3 distance = modifiedVerts[v] - modifiedVerts[closestVertexIndex];

            if (distance.sqrMagnitude < radius * radius)
            {
                float normalizedDistance = distance.magnitude / radius;

                int pixelX = Mathf.FloorToInt((distance.x / radius + 0.5f) * brushSize);
                int pixelY = Mathf.FloorToInt((distance.z / radius + 0.5f) * brushSize);

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
        RecalculateMesh();
        UpdateMeshCollider();
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

    void HandleDeformation()
    {
        if (Input.GetMouseButtonDown(0))
        {
            SaveCurrentState(); // Sauvegarde de l'état initial
            isDeforming = true;
        }
        if (Input.GetKeyDown(KeyCode.F))
        {
            currentDistanceType = (DistanceType)(((int)currentDistanceType + 1) % 3);
            Debug.Log("Distance type pour le vertex le plus proche chang�: " + currentDistanceType);
        }

        if (Input.GetKeyDown(KeyCode.V))
        {
            neighborDistanceType = (DistanceType)(((int)neighborDistanceType + 1) % 3);
            Debug.Log("Distance type pour la recherche des voisins chang�: " + neighborDistanceType);
        }

        if (Input.GetKeyDown(KeyCode.T))
        {
            useApproximation = !useApproximation;
            Debug.Log(useApproximation ? "Approximation activ�e" : "Approximation d�sactiv�e");
        }

        if (Input.GetKeyDown(KeyCode.N))
        {
            recalculateSelectiveNormals = !recalculateSelectiveNormals;
            Debug.Log(recalculateSelectiveNormals ? "Recalcul s�lectif des normales activ�" : "Recalcul global des normales activ�");
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            useGridSpaceForNeighbors = !useGridSpaceForNeighbors;
            Debug.Log(useGridSpaceForNeighbors ? "Mode grille activ� pour le voisinage" : "Mode monde activ� pour le voisinage");
        }

        RaycastHit hit;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out hit, Mathf.Infinity))
        {
            Vector3 hitPoint = hit.point;

            int closestVertexIndex;
            if (useApproximation)
            {
                closestVertexIndex = FindClosestVertexApproximation(hit.triangleIndex);
            }
            else
            {
                closestVertexIndex = FindClosestVertex(hitPoint, currentDistanceType);
            }

            if (Input.GetMouseButtonDown(0))
            {
                isDeforming = true;
            }

            for (int v = 0; v < modifiedVerts.Length; v++)
            {
                float distanceToVertex = CalculateDistance(modifiedVerts[v], modifiedVerts[closestVertexIndex], neighborDistanceType, useGridSpaceForNeighbors);

                // V�rifier que le vertex est dans le rayon
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
        if (Input.GetMouseButtonUp(0) && isDeforming)
        {
            isDeforming = false;
            UpdateMeshCollider();
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

    // ---Fonctions utilitaires---

    void RecalculateMesh()
    {
        p_mesh.vertices = modifiedVerts;

        if (recalculateSelectiveNormals)
        {
            RecalculateNormalsSelective();
        }
        else
        {
            p_mesh.RecalculateNormals();
        }
    }

    // Exercice 5
    // Phase 1

    float CalculateDistance(Vector3 pointA, Vector3 pointB, DistanceType distanceType, bool useGridSpace)
    {
        if (useGridSpace)
        {
            // Calcul de la distance en utilisant les coordonn�es de grille
            Vector2 gridPointA = new Vector2(Mathf.Round(pointA.x), Mathf.Round(pointA.z));
            Vector2 gridPointB = new Vector2(Mathf.Round(pointB.x), Mathf.Round(pointB.z));

            switch (distanceType)
            {
                case DistanceType.Manhattan:
                    return Mathf.Abs(gridPointA.x - gridPointB.x) + Mathf.Abs(gridPointA.y - gridPointB.y);
                case DistanceType.Chebyshev:
                    return Mathf.Max(Mathf.Abs(gridPointA.x - gridPointB.x), Mathf.Abs(gridPointA.y - gridPointB.y));
                default:
                    return Vector2.Distance(gridPointA, gridPointB);
            }
        }
        else
        {
            // Calcul de la distance en utilisant les positions en espace monde
            switch (distanceType)
            {
                case DistanceType.Manhattan:
                    return Mathf.Abs(pointA.x - pointB.x) + Mathf.Abs(pointA.y - pointB.y) + Mathf.Abs(pointA.z - pointB.z);
                case DistanceType.Chebyshev:
                    return Mathf.Max(Mathf.Abs(pointA.x - pointB.x), Mathf.Abs(pointA.y - pointB.y), Mathf.Abs(pointA.z - pointB.z));
                default: // Euclidean
                    return Vector3.Distance(pointA, pointB);
            }
        }
    }

    // Modification de FindClosestVertex pour accepter un type de distance en param�tre (Phase 1)
    int FindClosestVertex(Vector3 point, DistanceType distanceType)
    {
        int closestIndex = -1;
        float closestDistance = Mathf.Infinity;

        // Parcours des vertices du triangle s�lectionn�
        for (int i = 0; i < modifiedVerts.Length; i++)
        {
            float distance = CalculateDistance(point, modifiedVerts[i], distanceType, useGridSpaceForNeighbors);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestIndex = i;
            }
        }
        return closestIndex;
    }

    int FindClosestVertexApproximation(int triangleIndex)
    {
        // On obtient les indices des sommets du triangle touch�
        int vert1 = p_triangles[triangleIndex * 3];

        // On retourne simplement le premier sommet du triangle s�lectionn� comme approximation
        return vert1;
    }

    // Phase 4

    void RecalculateNormalsSelective()
    {
        Vector3[] normals = p_mesh.normals;

        // Boucle sur les sommets modifi�s pour recalculer leurs normales
        for (int v = 0; v < modifiedVerts.Length; v++)
        {
            if (modifiedVerts[v] != vertices[v]) // Si le sommet a �t� modifi�
            {
                normals[v] = Vector3.zero;

                // Calcul de la normale en fonction des triangles adjacents
                foreach (int t in p_mesh.triangles)
                {
                    // Calcule la normale du triangle si le sommet appartient � ce triangle
                    // (Note : Ajoutez ici la logique pour trouver les triangles auxquels le sommet appartient)
                }
            }
        }

        p_mesh.normals = normals;
        p_mesh.RecalculateBounds();
    }

    void UpdateMeshCollider()
    {
        p_meshCollider.sharedMesh = null;
        p_meshCollider.sharedMesh = p_meshFilter.mesh;
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