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
    public GameObject terrainPrefab;
    public int extensionSize = 300;
    public int vitesse = 0;
    private int angle = 0;
    private bool IsInRotMode = false;
    private bool IsCharacterActive = false;

    // Variables pour la gestion de l'interface utilisateur (UI)
    public GameObject settingsCanvas;  // Interface pour le menu des paramètres
    public InputField dimensionInput;  // Champ de saisie pour la dimension
    public InputField resolutionInput; // Champ de saisie pour la résolution

    public GameObject infoCanvas; // Associe ici le Canvas d'information depuis l'éditeur
    public Text infoText; // Associe ici le Text du menu pour afficher les informations
    public Text infoText2;

    //Deformation usage
    [Range(1f, 50f)]
    public float radius = 25f;
    [Range(1f, 50f)]
    public float deformationStrength = 25f;

    public AnimationCurve attenuationCurve;
    private Vector3[] vertices, modifiedVerts;
    public List<AnimationCurve> patterns; // Liste des patterns
    private int patternIndex = 0; // Indice du pattern actuel
    private bool useApproximation = false; // Active ou d sactive l'approximation
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
        infoCanvas.SetActive(false);
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

        if (Input.GetKeyDown(KeyCode.F1))
        {
            ToggleInfoPanel();
        }

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


    }

    // Implémentation F1

    void ToggleInfoPanel()
    {
        if (infoCanvas.activeSelf)
        {
            infoCanvas.SetActive(false);
        }
        else
        {
            UpdateInfoText(); // Met à jour le contenu
            infoCanvas.SetActive(true);
        }
    }

    void UpdateInfoText()
    {
        // Informations générales sur le terrain
        infoText.text = "Propriétés du Terrain :\n";
        infoText.text += $"Dimension : {dimension}\n";
        infoText.text += $"Résolution : {resolution}\n\n";
        infoText.text += $"Rayon de Déformation : {radius}\n";
        infoText.text += $"Force de Déformation : {deformationStrength}\n\n";

        // Informations spécifiques à la déformation (Exercice 3)
        infoText.text += "Déformation du Terrain :\n";
        infoText.text += $"Mode de Déformation : {(useBrushMode ? "Brush" : "Pattern")}\n";
        infoText.text += $"Nom du Pattern/Brush : {(useBrushMode ? brushTextures[brushIndex].name : $"Pattern {patternIndex}")}\n";
        infoText.text += $"Rayon de Déformation : {radius}\n";
        infoText.text += $"Intensité de Déformation : {deformationStrength}\n";
        infoText.text += $"Type de Distance : {currentDistanceType}\n\n";

        // Raccourcis pour le contrôle de la déformation
        infoText.text += "Contrôles de Déformation :\n";
        infoText.text += "F - Alterner les types de distance pour la déformation (Euclidean, Manhattan, Chebyshev)\n";
        infoText.text += "N - Basculer le recalcul sélectif des normales\n";
        infoText.text += "R - Basculer entre le mode de voisinage grille et espace monde\n";
        infoText.text += "CTRL+Click - Creuser\n";
        infoText.text += "+ / - : Ajuster le rayon de déformation\n";
        infoText.text += "Alt / AltGr : Ajuster l'intensité de déformation\n";
        infoText.text += "P - Changer de pattern de déformation\n";
        infoText.text += "B - Changer de brush de déformation\n";
        infoText.text += "C - Basculer entre les modes Brush et Pattern\n\n";

        // Contrôles du Terrain et de l'Interface (Exercice 2 et Exercice 4)
        infoText.text += "Contrôles du Terrain :\n";
        infoText.text += "F10 - Ouvrir le menu des paramètres (dimension, résolution)\n\n";
        infoText.text += "Entrée - Appliquer les paramètres\n\n";

        // Contrôles de la caméra (FlyCamera) et du personnage (CharacterMove)
        infoText.text += "Contrôles de la Caméra :\n";
        infoText.text += "Right Control - Activer/désactiver la rotation de la caméra\n";
        infoText.text += "Z / A - Monter / Descendre\n";
        infoText.text += "ZQSD ou flèches - Déplacer la caméra\n";
        infoText.text += "Shift gauche - Déplacement rapide\n";
        infoText.text += "CTRL gauche - Déplacement lent\n\n";

        infoText2.text = "Contrôles du Personnage :\n";
        infoText2.text += "F2 - Activer le mode de déplacement vers une destination\n";
        infoText2.text += "F3 - Activer le mode libre\n";
        infoText2.text += "Échap - Quitter le mode libre\n";
        infoText2.text += "CTRL gauche - Passer en vue première personne\n";
        infoText2.text += "ZQSD ou flèches - Déplacer en mode libre\n";
        infoText2.text += "Shift gauche - Sprint\n";

        // --- Optimisations (Exercice 5) ---
        infoText2.text += "Paramètres d'Optimisation :\n";
        infoText2.text += $"Distance pour le vertex sélectionné : {currentDistanceType}\n";
        infoText2.text += $"Distance pour les voisins : {neighborDistanceType}\n";
        infoText2.text += $"Approximation activée : {(useApproximation ? "Oui" : "Non")}\n";
        infoText2.text += $"Recalcul sélectif des normales : {(recalculateSelectiveNormals ? "Oui" : "Non")}\n";
        infoText2.text += $"Mode de voisinage : {(useGridSpaceForNeighbors ? "Grille" : "Espace Monde")}\n\n";

        // Contrôles des Optimisations
        infoText2.text += "Contrôles d'Optimisation :\n";
        infoText2.text += "F - Alterner les types de distance pour la sélection du vertex\n";
        infoText2.text += "V - Alterner les types de distance pour les voisins\n";
        infoText2.text += "T - Activer/désactiver l'approximation\n";
        infoText2.text += "N - Activer/désactiver le recalcul sélectif des normales\n";
        infoText2.text += "R - Basculer entre le mode de voisinage grille et espace monde\n";

        if (useBrushMode)
        {
            infoText2.text += "Déformation par Brush :\n";
            infoText2.text += $"Brush Actuel : {brushTextures[brushIndex].name}\n";
            infoText2.text += $"Rayon du Brush : {radius}\n";
            infoText2.text += $"Intensité du Brush : {deformationStrength}\n";
            infoText2.text += "B - Changer de brush de déformation\n";
        }
        else
        {
            infoText2.text += "Déformation par Pattern :\n";
            infoText2.text += $"Pattern Actuel : $\"Pattern {{patternIndex}}\"\n";
            infoText2.text += "P - Changer de pattern de déformation\n";
        }

        infoText2.text += "\nC - Basculer entre les modes Brush et Pattern\n";
        // Contrôles pour les Chunks
        infoText2.text += "Contrôles des Chunks :\n";
        infoText2.text += "F5 + flèches - Ajouter un Chunk dans une direction\n";
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
            Debug.Log("Distance type pour le vertex le plus proche chang : " + currentDistanceType);
        }

        if (Input.GetKeyDown(KeyCode.V))
        {
            neighborDistanceType = (DistanceType)(((int)neighborDistanceType + 1) % 3);
            Debug.Log("Distance type pour la recherche des voisins chang : " + neighborDistanceType);
        }

        if (Input.GetKeyDown(KeyCode.T))
        {
            useApproximation = !useApproximation;
            Debug.Log(useApproximation ? "Approximation activ e" : "Approximation d sactiv e");
        }

        if (Input.GetKeyDown(KeyCode.N))
        {
            recalculateSelectiveNormals = !recalculateSelectiveNormals;
            Debug.Log(recalculateSelectiveNormals ? "Recalcul s lectif des normales activ " : "Recalcul global des normales activ ");
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            useGridSpaceForNeighbors = !useGridSpaceForNeighbors;
            Debug.Log(useGridSpaceForNeighbors ? "Mode grille activ  pour le voisinage" : "Mode monde activ  pour le voisinage");
        }

        RaycastHit hit;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out hit, Mathf.Infinity))
        {
            Vector3 hitPoint = hit.point;

            int closestVertexIndex = FindClosestVertex(hitPoint);
            if (useApproximation)
            {
                closestVertexIndex = FindClosestVertexApproximation(hit.triangleIndex);
            }
            else
            {
                closestVertexIndex = FindClosestVertex(hitPoint, currentDistanceType);
            }

            if (useBrushMode)
            {
                ApplyBrushDeformation(closestVertexIndex); // Déformation avec brush
            }
            else
            {
                ApplyPatternDeformation(closestVertexIndex); // Déformation avec pattern
            }


            if (Input.GetMouseButtonDown(0))
            {
                isDeforming = true;
            }

            for (int v = 0; v < modifiedVerts.Length; v++)
            {
                float distanceToVertex = CalculateDistance(modifiedVerts[v], modifiedVerts[closestVertexIndex], neighborDistanceType, useGridSpaceForNeighbors);

                // V rifier que le vertex est dans le rayon
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
            // Calcul de la distance en utilisant les coordonn es de grille
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

    // Modification de FindClosestVertex pour accepter un type de distance en param tre (Phase 1)
    int FindClosestVertex(Vector3 point, DistanceType distanceType)
    {
        int closestIndex = -1;
        float closestDistance = Mathf.Infinity;

        // Parcours des vertices du triangle s lectionn 
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
        // On obtient les indices des sommets du triangle touch 
        int vert1 = p_triangles[triangleIndex * 3];

        // On retourne simplement le premier sommet du triangle s lectionn  comme approximation
        return vert1;
    }

    // Phase 4
    void RecalculateNormalsSelective()
    {
        Vector3[] normals = p_mesh.normals;

        // Boucle sur les sommets modifi s pour recalculer leurs normales
        for (int v = 0; v < modifiedVerts.Length; v++)
        {
            if (modifiedVerts[v] != vertices[v]) // Si le sommet a  t  modifi 
            {
                normals[v] = Vector3.zero;

                // Calcul de la normale en fonction des triangles adjacents
                foreach (int t in p_mesh.triangles)
                {
                    // Calcule la normale du triangle si le sommet appartient   ce triangle
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