using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using Unity.VisualScripting;
using UnityEngine;


[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshCollider))]

public class CreateCubeMultiRes : MonoBehaviour
{

    public enum TypeCube { Cube24, Cube8 , CubeMultiRes }
    public TypeCube typeCube;
    public float widthCube ;

    [Header("Résolution")]
    [Tooltip("Chaque face du cube sera partagée en 2x(res x res) triangles .")]
    [Range(1, 256)]
    public int resolution = 1;

    public enum ResLevel{ Perso = 0 , Min=1, Middle=50, Max=100 };
    [Header("Niveau de résolution")]
    [Tooltip("Choisissez le niveau de résolution du cube" )]
    public ResLevel resLevel;



    // public GameObject PickObj;   
    private Camera cam;

    private Vector3[] p_vertices;
    private Vector3[] p_normals;
    private int[] p_triangles;
    private Mesh p_mesh;

    private Vector3 p0;
    private Vector3 p1;
    private Vector3 p2;
    private Vector3 p3;
    private Vector3 p4;
    private Vector3 p5;
    private Vector3 p6;
    private Vector3 p7;

    [HideInInspector] public ushort nb_vertices;
    [HideInInspector] public ushort nb_triangles;
    [HideInInspector] public ushort nb_triangles_par_face;
    [HideInInspector] public ushort nb_vertices_par_face;
    [HideInInspector] public int    res;

    private int indexTriangle;


    private void CreerCubeMultiRes()
    {
        res = 1;

        if (resLevel > 0)
            res = (int)resLevel;
        else
            res = resolution;

        long nb_vertices_par_face_théoriques = (res + 1) * (res + 1);
        long nb_vertices_théoriques = nb_vertices_par_face_théoriques * 6;

        if (nb_vertices_théoriques >= ushort.MaxValue)
        {
            print("trop de vertices pour type d'indices ushort par defaut ");
            print("max possible " + ushort.MaxValue + "   demandé = " + nb_vertices_théoriques);
            print("il faudrait modifier le type des indices avec mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;");
            return;
        }

        nb_triangles_par_face = (ushort)(2 * res * res);
        nb_vertices_par_face = (ushort)nb_vertices_par_face_théoriques;
        nb_vertices = (ushort)nb_vertices_théoriques;
        nb_triangles = (ushort)(nb_triangles_par_face * 6);


        p_mesh = new Mesh();
        p_mesh.name = "MyProceduralCubeMultiRes";

        p_vertices = new Vector3[nb_vertices];
        p_normals = new Vector3[nb_vertices];
        p_triangles = new int[nb_triangles*3];
      
       
        indexTriangle = 0;
        construireFace(0, Vector3.right, Vector3.up, Vector3.back );     // face avant
        construireFace(1, Vector3.left, Vector3.up, Vector3.forward);    // face arrière
        construireFace(2, Vector3.forward, Vector3.up, Vector3.right);    // face droite 
        construireFace(3, Vector3.back, Vector3.up, Vector3.left);          // face gauche 
        construireFace(4, Vector3.right, Vector3.forward, Vector3.up);    // face dessus 
        construireFace(5, Vector3.left, Vector3.forward, Vector3.down);    // face dessous 

        // CALCUL des NORMALES
        for (int num_face = 0; num_face < 6; num_face++)
            {
            Vector3 normalFaceEnCours = normaleDuTriangle(nb_triangles_par_face * num_face);
            for (int i = 0; i <= res; i++)
                for (int j = 0; j <= res; j++)
                    p_normals[num_face * nb_vertices_par_face + i * (res + 1) + j] = normalFaceEnCours;
            }

        p_mesh.Clear();
        p_mesh.vertices = p_vertices;
        p_mesh.triangles = p_triangles;
        p_mesh.normals = p_normals;

        //  p_mesh.RecalculateNormals();         // ==> recalcule toutes les normales / ne plus gerer p_normals ni calcul des normales
        p_mesh.RecalculateBounds();             // pas nécessaire car triangles ré affecté (= automatique)
                                                // ne mets pas à jour les collider pour autant : voir manip ci dessous




        MeshCollider mc;
        if (mc = gameObject.GetComponent<MeshCollider>())
            Destroy(mc);
        mc = gameObject.AddComponent<MeshCollider>();
        mc.sharedMesh = GetComponent<MeshFilter>().sharedMesh;

        // il faut supprimer le box collider s'il existe ! 
        // le recréer pour qu'il s'adapte à la nouvelle géométrie de l'objet 
        // le box collider est satisfaisant pour la physique mais
        // ne permet pas la sélection d'un face du mailkage avec Physics.Raycast
        /*BoxCollider bc;
        if (bc = gameObject.GetComponent<BoxCollider>())
            Destroy(bc);
        gameObject.AddComponent<BoxCollider>();
        */

        /* Rigidbody rb;
         if (rb = gameObject.GetComponent<Rigidbody>())
             Destroy(bc);
         gameObject.AddComponent<Rigidbody>();
         */

        GetComponent<MeshFilter>().mesh = p_mesh;

    }
    // FIN CreerCubeMultiRes


    private void construireFace(int numero_face, 
                                Vector3 axeDroit, 
                                Vector3 axeHaut, 
                                Vector3 axeProfondeur)
    {
        // construire VERTICES
        float decal = widthCube / 2f;
        for (int i = 0; i <= res; i++)
            for (int j = 0; j <= res; j++)
                p_vertices[numero_face * nb_vertices_par_face + i * (res + 1) + j] =
                    axeHaut  * (-decal + (float)(i) / res * widthCube) +
                    axeDroit * (-decal + (float)(j) / res * widthCube) + 
                    axeProfondeur * decal;

        // construire TRIANGLES
        int num_vertex;
        for (int i = 0; i < res; i++)
            for (int j = 0; j < res; j++)
            {
                num_vertex = numero_face * nb_vertices_par_face + i * (res + 1) + j;

                p_triangles[indexTriangle++] = num_vertex;
                p_triangles[indexTriangle++] = num_vertex + res + 1;
                p_triangles[indexTriangle++] = num_vertex + 1;

                 p_triangles[indexTriangle++] = num_vertex + res + 1;
                p_triangles[indexTriangle++] = num_vertex + res + 1 + 1;
                p_triangles[indexTriangle++] = num_vertex + 1;
 
            }
    }


    private void CreerCube24()
    {

        p_mesh = new Mesh();
        p_mesh.name = "MyProceduralCube24";

        p_vertices = new Vector3[]{
            p0,p1,p2,p3,  // devant
            p4,p5,p1,p0,  // gauche
            p3,p2,p6,p7,  // Droite
            p7,p6,p5,p4,  // Derrière
            p1,p5,p6,p2,  // Dessus
            p4,p0,p3,p7   // dessous
            };

        p_triangles = new int[12 * 3];
        int index = 0;
        for (int i = 0; i < 6; i++)   // 6 faces à 2 triangles
        {   // triangle 1
            p_triangles[index++] = i * 4;
            p_triangles[index++] = i * 4 + 1;
            p_triangles[index++] = i * 4 + 3;
            // triangle 2
            p_triangles[index++] = i * 4 + 1;
            p_triangles[index++] = i * 4 + 2;
            p_triangles[index++] = i * 4 + 3;

        }

        // calcul manuel des normales  (alternative à  p_mesh.RecalculateNormals(); ) 
        // le principal intérêt serait de le faire quand une partie seulement des normales ont été modifiées
        // calcul manuel de toutes les normales  : alternative optimisée à p_mesh.RecalculateNormals();
        p_normals = new Vector3[p_vertices.Length];

        Vector3 v1, v2, pv;
        for (int i = 0; i < 6; i++)
        {
            v1 = p_vertices[i * 4 + 1] - p_vertices[i * 4 + 0];
            v2 = p_vertices[i * 4 + 2] - p_vertices[i * 4 + 0];
            pv = Vector3.Cross(v1, v2);
            pv = pv / pv.magnitude;

            p_normals[i * 4 + 0] = pv;
            p_normals[i * 4 + 1] = pv;
            p_normals[i * 4 + 2] = pv;
            p_normals[i * 4 + 3] = pv;
        }

        p_mesh.Clear();
        p_mesh.vertices = p_vertices;
        p_mesh.triangles = p_triangles;
        p_mesh.normals = p_normals;

        //  p_mesh.RecalculateNormals();         // ==> recalcule toutes les normales / ne plus gerer p_normals ni calcul des normales
        p_mesh.RecalculateBounds();             // pas nécessaire car triangles ré affecté (= automatique)
                                                // ne mets pas à jour les collider pour autant : voir manip ci dessous




        MeshCollider mc;
        if (mc = gameObject.GetComponent<MeshCollider>())
            Destroy(mc);
        mc = gameObject.AddComponent<MeshCollider>();
        mc.sharedMesh = GetComponent<MeshFilter>().sharedMesh;

        // il faut supprimer le box collider s'il existe
        // le recréer pour qu'il s'adapte à la nouvelle géométrie de l'objet 
        // le box collider est satisfaisant pour la physique mais ne permet pas la sélection d'un face du mailkage avec Physics.Raycast
        /*BoxCollider bc;
        if (bc = gameObject.GetComponent<BoxCollider>())
            Destroy(bc);
        gameObject.AddComponent<BoxCollider>();
        */

        /* Rigidbody rb;
         if (rb = gameObject.GetComponent<Rigidbody>())
             Destroy(bc);
         gameObject.AddComponent<Rigidbody>();
         */

        GetComponent<MeshFilter>().mesh = p_mesh;

    }
    // FIN CreerCube24

    private void CreerCube8()
    {

        p_mesh = new Mesh();
        p_mesh.name = "MyProceduralCube8";

        p_vertices = new Vector3[]{
            p0,p1,p2,p3,
            p4,p5,p6,p7
            };

        p_triangles = new int[12 * 3] { 0, 1, 3,    1, 2, 3,
                                       3,2,6,    6,7,3,
                                        5,1,4,    1,0,4,
                                        1,5,2,    2,5,6,
                                        7,6,5,    5,4,7,
                                        0,3,4,    3,7,4 };

        // calcul manuel des normales  (alternative à  p_mesh.RecalculateNormals(); ) 
        // le principal intérêt serait de le faire quand une partie seulement des normales ont été modifiées
        p_normals = new Vector3[p_vertices.Length];

        /* Vector3 v1, v2, pv;
         for (int i = 0; i < 6; i++)
         {
             v1 = p_vertices[i * 4 + 1] - p_vertices[i * 4 + 0];
             v2 = p_vertices[i * 4 + 2] - p_vertices[i * 4 + 0];
             pv = Vector3.Cross(v1, v2);
             pv = pv / pv.magnitude;

             p_normals[i * 4 + 0] = pv;
             p_normals[i * 4 + 1] = pv;
             p_normals[i * 4 + 2] = pv;
             p_normals[i * 4 + 3] = pv;
         }*/

        // calcul manuel des normales  ou 
        /*
        p_normals[0] = new Vector3(-1, -1, -1);
        p_normals[1] = new Vector3(-1, +1, -1);
        p_normals[2] = new Vector3(+1, +1, -1);
        p_normals[3] = new Vector3(+1, -1, -1);

        p_normals[4] = new Vector3(-1, -1, +1);
        p_normals[5] = new Vector3(-1, +1, +1);
        p_normals[6] = new Vector3(+1, +1, +1);
        p_normals[7] = new Vector3(+1, -1, +1);
        */

        // calcul des normales d'un vertex partagé  (ou non) 
        // calcul des normales des triangles 
        int nbTri = p_triangles.Length / 3; 
        Vector3[] normaleTri = new Vector3[nbTri];
        for (int numTri = 0; numTri < nbTri; numTri++)
            normaleTri[numTri] = normaleDuTriangle(numTri);
        // pour tous les vertices
        for (int i = 0; i < p_vertices.Length; i++)
        {   Vector3 cumulNormales = new Vector3();
            int nbNormales = 0;
            // cumul des normales des triangles associés
            for(int j =0;j<nbTri;j++) 
                if ((p_triangles[j*3+0]==i)||(p_triangles[j * 3 + 1] == i) || (p_triangles[j * 3 + 2] == i))
                {
                    cumulNormales += normaleTri[j];
                    nbNormales++;
                }
            // on affect au vertex partagé la moyennes des normales des triangles associés
            p_normals[i] = cumulNormales / nbNormales;
        }
        //p_mesh.RecalculateNormals();   ... ne recalcule pas ICI !! 

        p_mesh.Clear();
        p_mesh.vertices = p_vertices;
        p_mesh.triangles = p_triangles;
        p_mesh.normals = p_normals;

        //  p_mesh.RecalculateNormals();         // ==> recalcule toutes les normales / ne plus gerer p_normals ni calcul des normales
        p_mesh.RecalculateBounds();             // pas nécessaire car triangles ré affecté (= automatique)
                                                // ne mets pas à jour les collider pour autant : voir manip ci dessous




        MeshCollider mc;
        if (mc = gameObject.GetComponent<MeshCollider>())
            Destroy(mc);
        mc = gameObject.AddComponent<MeshCollider>();
        mc.sharedMesh = GetComponent<MeshFilter>().sharedMesh;

        // il faut supprimer le box collider s'il existe
        // le recréer pour qu'il s'adapte à la nouvelle géométrie de l'objet 
        // le box collider est satisfaisant pour la physique mais ne permet pas la sélection d'un face du mailkage avec Physics.Raycast
        /*BoxCollider bc;
        if (bc = gameObject.GetComponent<BoxCollider>())
            Destroy(bc);
        gameObject.AddComponent<BoxCollider>();
        */

        /* Rigidbody rb;
         if (rb = gameObject.GetComponent<Rigidbody>())
             Destroy(bc);
         gameObject.AddComponent<Rigidbody>();
         */

        GetComponent<MeshFilter>().mesh = p_mesh;

    }
    // FIN CreerCube8


    void Awake()
    {

        float w = -widthCube / 2.0f;
        float W = widthCube / 2.0f;

        p0 = new Vector3(w, w, w);
        p1 = new Vector3(w, W, w);
        p2 = new Vector3(W, W, w);
        p3 = new Vector3(W, w, w);
        p4 = new Vector3(w, w, W);
        p5 = new Vector3(w, W, W);
        p6 = new Vector3(W, W, W);
        p7 = new Vector3(W, w, W);

        switch (typeCube)
        {
            case TypeCube.Cube8:
                CreerCube8(); break;
            case TypeCube.Cube24:
                CreerCube24(); break;
            case TypeCube.CubeMultiRes:
                CreerCubeMultiRes(); break;
        }

        // GenererPickObjects();

        cam = Camera.main;
    }

   private void CalculNormalesDesVerticesDuTriangle(int i)          // sans vertice partagé
    {
        Vector3 v1, v2, pv;
        v1 = p_vertices[p_triangles[i * 3 + 1]] - p_vertices[p_triangles[i * 3]];
        v2 = p_vertices[p_triangles[i * 3 + 2]] - p_vertices[p_triangles[i * 3]];
        pv = Vector3.Cross(v1, v2);
        pv = pv / pv.magnitude;

        p_normals[p_triangles[i * 3 + 0]] = pv;
        p_normals[p_triangles[i * 3 + 1]] = pv;
        p_normals[p_triangles[i * 3 + 2]] = pv;

    }

    private Vector3 normaleDuTriangle(int i)          // utilisé pour calculé normale d'un vertex partagé 
    {
        Vector3 v1, v2, pv;
        v1 = p_vertices[p_triangles[i * 3 + 1]] - p_vertices[p_triangles[i * 3]];
        v2 = p_vertices[p_triangles[i * 3 + 2]] - p_vertices[p_triangles[i * 3]];
        pv = Vector3.Cross(v1, v2);
        pv = pv / pv.magnitude;
        return pv; 
    }

    private void DebugNormals()
    {   if (p_vertices.Length ==0) return; 
        for (int num_vert = 0; num_vert < p_vertices.Length; num_vert++)
            Debug.DrawRay(transform.position + p_vertices[num_vert], p_normals[num_vert]*widthCube/3, Color.red, 30, false);
    }

    void Update()
    {
        DebugNormals();


    }
}
