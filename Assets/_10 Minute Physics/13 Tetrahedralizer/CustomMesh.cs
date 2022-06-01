using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomMesh
{
    public Vector3[] vertices;

    public int[] triangles;

    public string name;



    public CustomMesh()
    {

    }



    public CustomMesh(Transform meshTransform, bool toGlobal)
    {
        Mesh mesh = meshTransform.GetComponent<MeshFilter>().mesh;
    
        if (toGlobal)
        {
            Vector3[] verticesLocal = mesh.vertices;
        
            vertices = new Vector3[verticesLocal.Length];

            for (int i = 0; i < verticesLocal.Length; i++)
            {
                vertices[i] = meshTransform.TransformPoint(verticesLocal[i]);
            }
        }
        else
        {
            vertices = mesh.vertices;
        }

        triangles = mesh.triangles;
    }



    //A list of all triangles, making it easier to display a triangle with a different color, etc
    public List<Triangle> GetTriangles(List<int> markedTriangles = null)
    {
        List<Triangle> tris = new List<Triangle>();

        for (int i = 0; i < triangles.Length; i += 3)
        {
            Vector3 a = vertices[triangles[i + 0]];
            Vector3 b = vertices[triangles[i + 1]];
            Vector3 c = vertices[triangles[i + 2]];

            Triangle t = new Triangle(a, b, c);

            //Should this triangle be marked
            if (markedTriangles != null && markedTriangles.Contains(i))
            {
                t.isIntersecting = true;
            }

            tris.Add(t);
        }

        return tris;
    }
}