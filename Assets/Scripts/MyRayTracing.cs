using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using static MyRayTracing;
using _Hittable;
using _RayTracingMaterial;
using _BVHAccel;

public class MyRayTracing : VolumeComponent, IPostProcessComponent
{
    public ComputeShader RayTracingShader;

    public SampleCounterUI sampleCounterUI;

    public BoolParameter Enable = new(false);

    public TextureParameter SkyboxTexture = new(null);

    [Tooltip("Accumulate sampling")]
    public BoolParameter AccSample = new(false);

    [Range(0, 10), Tooltip("Ray bounce times between objects")]
    public IntParameter rayBounce = new(2);

    public BoolParameter CreateTestScene = new(false);

    public BoolParameter EnableBVH = new(false);

    [Tooltip("Random seed for test scene. Notice: Computational cost of scene creating is expensive. Reselect 'Create Test Scene' after changing random seed.")]
    public IntParameter SceneSeed = new(0);

    private static bool isSceneCreated = false;

    private static bool isRelease = false;

    public static bool isSetObjects = false;

    // Spheres
    private static ComputeBuffer _sphereBuffer = null;
    private static ComputeBuffer _BVHNodesBuffer = null;

    // MeshObject
    private static List<MeshObject> _meshObjects = new();
    private static List<Vector3> _vertices = new();
    private static List<int> _indices = new();
    private static ComputeBuffer _meshObjectBuffer;
    private static ComputeBuffer _vertexBuffer;
    private static ComputeBuffer _indexBuffer;

    public bool IsActive()
    {
        if (Enable.value)
        {
            sampleCounterUI = FindObjectOfType<Canvas>().GetComponentInChildren<SampleCounterUI>();
            RayTracingShader = Resources.Load<ComputeShader>("Shaders/RayTracingShader");
            if (RayTracingShader == null)
            {
                Debug.LogError("ComputeShader not found in Resources/Shaders folder!");
            }


            if (!isSceneCreated)
            {
                CreateOneWeekendScene();
                isSceneCreated = true;
                isRelease = false;
            }
            else
            {
                if (!CreateTestScene.value)
                {
                    _sphereBuffer?.Release();
                    isRelease = true;
                }
                else if (isRelease)
                {
                    CreateOneWeekendScene();
                    isRelease = false;
                }
            }
        }
        return Enable.value;
    }

    public bool IsTileCompatible() => false;

    
    
    struct MeshObject
    {
        public Matrix4x4 localToWorldMatrix;
        public int indices_offset;
        public int indices_count;
        public Vector3 albedo;
        public Vector3 specular;
        public float smoothness;
        public float ior;
        public Vector3 emission;
    }

    private void CreateOneWeekendScene()
    {
        List<Sphere> spheres = SceneData.CreateSpheres(SceneSeed.value);

        _sphereBuffer?.Release();
        if (spheres.Count > 0)
        {
            _sphereBuffer = new ComputeBuffer(spheres.Count, 60);
            _sphereBuffer.SetData(spheres);
        }
        if (_sphereBuffer != null)
            RayTracingShader.SetBuffer(0, "_Spheres", _sphereBuffer);



        // BVH
        List<BVHNodeFlat> sphereBVHNodes = SceneData.CreateSphereBVH(spheres);
        Debug.Log(sphereBVHNodes[0].bbox.x.size());
        _BVHNodesBuffer?.Release();
        if(sphereBVHNodes.Count > 0)
        {
            _BVHNodesBuffer = new ComputeBuffer(sphereBVHNodes.Count, 36);
            _BVHNodesBuffer.SetData(sphereBVHNodes);
        }
        if (_BVHNodesBuffer != null)
            RayTracingShader.SetBuffer(0, "_BVHNodes", _BVHNodesBuffer);

        return;
    }

    private static void CreateComputeBuffer<T>(ref ComputeBuffer buffer, List<T> data, int stride) where T : struct
    {
        if(buffer != null)
        {
            if(data.Count == 0 || buffer.count != data.Count || buffer.stride != stride)
            {
                buffer.Release();
                buffer = null;
            }
        }

        if(data.Count != 0)
        {
            buffer ??= new ComputeBuffer(data.Count, stride);

            buffer.SetData(data);
        }
    }

    private void SetComputeBuffer(string name, ComputeBuffer buffer)
    {
        if(buffer != null)
        {
            RayTracingShader.SetBuffer(0, name, buffer);
        }
    }

    public void SetRayTracingObjectsParameters()
    {
        if (isSetObjects) return;

        _meshObjects.Clear();
        _vertices.Clear();
        _indices.Clear();

        var _rayTracingObjects = GameObject.FindGameObjectsWithTag("RayTracing");

        foreach (var obj in _rayTracingObjects)
        {
            Mesh mesh = obj.GetComponent<MeshFilter>().sharedMesh;

            int firstVertex = _vertices.Count;
            _vertices.AddRange(mesh.vertices);

            int firstIndex = _indices.Count;
            var indices = mesh.GetIndices(0);
            _indices.AddRange(indices.Select(index => index + firstVertex));

            Vector3 albedo = 0.5f * Vector3.one;
            Vector3 specular = Vector3.zero;
            Vector3 emission = Vector3.zero;
            float smoothness = 0.2f;
            float ior = 0.0f;
            var mat = obj.GetComponent<RayTracingMat>();
            if (mat)
            {
                albedo = new Vector3(mat.albedo.r, mat.albedo.g, mat.albedo.b);
                specular = new Vector3(mat.specular.r, mat.specular.g, mat.specular.b);
                emission = new Vector3(mat.emission.r, mat.emission.g, mat.emission.b) * mat.emission_intensity;
                smoothness = mat.smoothness;
                ior = mat.IOR;
            }

            _meshObjects.Add(new MeshObject()
            {
                localToWorldMatrix = obj.transform.localToWorldMatrix,
                indices_offset = firstIndex,
                indices_count = indices.Length,
                albedo = albedo,
                specular = specular,
                emission = emission,
                smoothness = smoothness,
                ior = ior
            });
        }

        CreateComputeBuffer(ref _meshObjectBuffer, _meshObjects, 116);
        CreateComputeBuffer(ref _vertexBuffer, _vertices, 12);
        CreateComputeBuffer(ref _indexBuffer, _indices, 4);

        SetComputeBuffer("_MeshObjects", _meshObjectBuffer);
        SetComputeBuffer("_Vertices", _vertexBuffer);
        SetComputeBuffer("_Indices", _indexBuffer);

        isSetObjects = true;
    }
    public void SetAccSamplingCount(uint count)
    {
        if(sampleCounterUI!=null)
        {
            sampleCounterUI.sampleCount = count;
            sampleCounterUI.sampleCounterText.text = count.ToString();
        }
    }
}

