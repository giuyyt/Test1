using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class SphereInstanceController : MonoBehaviour
{
    // How many meshes to draw.
    public int _population1= 512;

    public Mesh mesh;

    public float R=6f;

    public ComputeShader _compute;

    public int randomSeed = 2;

    public float speed = 0.2f;
    

    [SerializeField] Material _material;


    // Material to use for drawing the meshes.
    public Material material
    {
        get { return _material; }
    }

    private Matrix4x4[] matrices;
    private MaterialPropertyBlock block;

    private ComputeBuffer _positionBuffer;
    private ComputeBuffer _matBuffer;
    private ComputeBuffer _colorBuffer;
    private float randomNum;

    private float time;
    private float speedMulTime;


    const int kThreadCount = 64;
    int ThreadGroupCount { get { return Mathf.Max(_population1 / kThreadCount,1); } }
    int population { get { return ThreadGroupCount* kThreadCount; } }
    void OnDestroy() 
    {
        if (_positionBuffer != null) _positionBuffer.Release();
        if (_matBuffer != null) _matBuffer.Release();
        if (_colorBuffer != null) _colorBuffer.Release();
        Destroy(_material);
    }

    private void Setup()
    {
        randomNum = Random.Range(0.01f, 0.99f);

        _material = new Material(_material);
        _material.name += " (cloned)";


        time = 0;
        speedMulTime = 0;
        //for (int i = 0; i < population; i++)
        //{
        //    Random.InitState(randomSeed);
        //    float x = Random.Range(0f, 1f);
        //    float y = Random.Range(0f, 1f);
        //    // Build matrix.
        //    Vector3 position = GetSphereRandom(new Vector2(x,y));
        //    Quaternion rotation = Quaternion.Euler(0,0,0);
        //    Vector3 scale = Vector3.one*0.1f;

        //    var mat = Matrix4x4.TRS(position, rotation, scale);

        //    matrices[i] = mat;

        //    colors[i] = Color.Lerp(Color.red, Color.blue,Random.value);
        //}

        //// Custom shader needed to read these!!
        //block.SetVectorArray("_Colors", colors);
    }


    private Vector3 GetSphereRandom(Vector2 randomIn) 
    {
        float phi = 2 * Mathf.PI * randomIn.x;
        float cosTheta = 1 - 2 * randomIn.y;
        float sinTheta = Mathf.Sqrt(1 - cosTheta * cosTheta);
        return new Vector3(sinTheta * Mathf.Cos(phi)* R, sinTheta * Mathf.Sin(phi)* R, cosTheta* R);
    }


    private void Start()
    {
        Setup();
        
    }

    private void Update()
    {
        matrices = new Matrix4x4[population];
        Vector4[] colors = new Vector4[population];

        block = new MaterialPropertyBlock();

        //colors[i] = Color.Lerp(Color.red, Color.blue, Random.value);
        speedMulTime += speed * Time.deltaTime;
        if (speedMulTime >= 2 * Mathf.PI) speedMulTime = 0;




        if (_positionBuffer != null) _positionBuffer.Release();
        _positionBuffer = new ComputeBuffer(population, 12);

        if (_matBuffer != null) _matBuffer.Release();
        _matBuffer = new ComputeBuffer(population, 64);

        if (_colorBuffer != null) _colorBuffer.Release();
        _colorBuffer = new ComputeBuffer(population, 16);


        var kernel = _compute.FindKernel("Update");

        _compute.SetFloat("R", R);
        _compute.SetFloat("RandomNum", randomNum);
        _compute.SetFloat("SpeedMulTime", speedMulTime);

        _compute.SetBuffer(kernel, "PositionBuffer", _positionBuffer);
        _compute.SetBuffer(kernel, "MatBuffer", _matBuffer);
        _compute.SetBuffer(kernel, "ColorBuffer", _colorBuffer);

        _compute.Dispatch(kernel, ThreadGroupCount, 1, 1);

        _matBuffer.GetData(matrices);
        _colorBuffer.GetData(colors);

        //block.SetVectorArray("_BaseColor", colors);
        //material.SetBuffer("ColorBuffer", _colorBuffer);


        // Draw a bunch of meshes each frame.
        Graphics.DrawMeshInstanced(mesh, 0, material, matrices, population, block);
    }
}
