using System.Collections;
using System.Collections.Generic;
using System.Data;
using UnityEngine;

public class Terrain : MonoBehaviour
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
    }

    public int vitesse = 0;
    private int angle = 0;

    // Méthode appelée au démarrage
    void Start()
    {
        // Désactiver le prefab capsule au démarrage
        capsulePrefab.gameObject.SetActive(false);
        // Créer le terrain
        CreerTerrain();
    }

    // Méthode appelée à chaque frame
    void Update()
    {
        // Appeler LateUpdate
        LateUpdate();
    }

    // Méthode appelée après Update
    private void LateUpdate()
    {
        // Rotation de l'objet si la touche RightControl est enfoncée
        if (Input.GetKey(KeyCode.RightControl))
        {
            angle += vitesse;
            transform.rotation = Quaternion.Euler(0, angle, 0);
        }

        // Activer le prefab capsule si la touche F2 est enfoncée
        if (Input.GetKey(KeyCode.F2))
        {
            capsulePrefab.gameObject.SetActive(true);
        }
    }
}