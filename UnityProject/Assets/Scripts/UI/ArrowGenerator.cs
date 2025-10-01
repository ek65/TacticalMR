using System.Collections.Generic;
using Fusion;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class ArrowGenerator : NetworkBehaviour
{
    public float stemLength;
    public float stemWidth;
    public float tipLength;
    public float tipWidth;

    public Vector3 origin;
    public Vector3 target;
    

    [System.NonSerialized]
    public List<Vector3> verticesList;
    [System.NonSerialized]
    public List<int> trianglesList;

    Mesh mesh;
    
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_SetActiveState(bool active) {
        this.transform.parent.gameObject.SetActive(active);
    }
    
    public void SetOrigin(Vector3 origin)
    {
        this.origin = origin;
    }
    public void SetTarget(Vector3 target)
    {
        this.target = target;
    }

    // length is destination position minus origin position
    private void CalculateStemLength(Vector3 origin, Vector3 destination)
    {
        stemLength = Vector3.Distance(origin, destination);
        stemLength = stemLength - 2; // subtract 1 to prevent tip from clipping into target (and another 1 to keep it 1 meter away)
    }
    
    void Start()
    {
        //make sure Mesh Renderer has a material
        mesh = new Mesh();
        this.GetComponent<MeshFilter>().mesh = mesh;
    }

    void Update()
    {
        if (Runner.IsClient)
        {
            return;
        }
        GenerateArrow();
        
        if (target == new Vector3(0,0,0))
        {
            return;
        }
        
        transform.parent.position = origin;
        
        // rotate parent to face target
        transform.parent.right = target - transform.parent.position;
        
        // calculate stem length
        CalculateStemLength(origin, target);
    }

    //arrow is generated starting at Vector3.zero
    //arrow is generated facing right, towards radian 0.
    void GenerateArrow()
    {
        //setup
        verticesList = new List<Vector3>();
        trianglesList = new List<int>();

        //stem setup
        Vector3 stemOrigin = Vector3.zero;
        float stemHalfWidth = stemWidth/2f;
        //Stem points
        verticesList.Add(stemOrigin+(stemHalfWidth*Vector3.down));
        verticesList.Add(stemOrigin+(stemHalfWidth*Vector3.up));
        verticesList.Add(verticesList[0]+(stemLength*Vector3.right));
        verticesList.Add(verticesList[1]+(stemLength*Vector3.right));

        //Stem triangles
        trianglesList.Add(0);
        trianglesList.Add(1);
        trianglesList.Add(3);

        trianglesList.Add(0);
        trianglesList.Add(3);
        trianglesList.Add(2);
        
        //tip setup
        Vector3 tipOrigin = stemLength*Vector3.right;
        float tipHalfWidth = tipWidth/2;

        //tip points
        verticesList.Add(tipOrigin+(tipHalfWidth*Vector3.up));
        verticesList.Add(tipOrigin+(tipHalfWidth*Vector3.down));
        verticesList.Add(tipOrigin+(tipLength*Vector3.right));

        //tip triangle
        trianglesList.Add(4);
        trianglesList.Add(6);
        trianglesList.Add(5);

        //assign lists to mesh.
        mesh.vertices = verticesList.ToArray();
        mesh.triangles = trianglesList.ToArray();
    }
}