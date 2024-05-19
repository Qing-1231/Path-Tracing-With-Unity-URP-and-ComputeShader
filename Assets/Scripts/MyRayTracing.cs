using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using static MyRayTracing;

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

    private static ComputeBuffer _sphereBuffer = null;

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
    public struct Material
    {
        public Vector3 albedo;
        public float fuzz;
        public float refraction_index;
        public Material(Vector3 a, float f, float ior)
        {
            albedo = a;
            fuzz = f;
            refraction_index = ior;
        }
        public Material(Vector3 a)
        {
            albedo = a;
            fuzz = 1;
            refraction_index = 0;
        }
        public Material(float ior)
        {
            albedo = new Vector3(1,1,1);
            fuzz = 0;
            refraction_index = ior;
        }
    }
    public struct Sphere
    {
        public Vector3 position;
        public float radius;
        public Material mat;
        public Sphere(Vector3 p, float r, Material m)
        {
            position = p;
            radius = r;
            mat = m;
        }
    }
    //public struct Sphere
    //{
    //    public Vector3 position;
    //    public float radius;
    //    public Vector3 albedo;
    //    public Vector3 specular;
    //    public float smoothness;
    //    public float ior;
    //    public Vector3 emission;
    //}
    
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
        Random.InitState(SceneSeed.value);
        List<Sphere> spheres = new();

        Material ground_lambertian_mat = new(new Vector3(0.5f, 0.5f, 0.5f), 1.0f, 0);
        Sphere ground = new(new Vector3(0, -1000f, -1), 1000, ground_lambertian_mat);
        spheres.Add(ground);

        for (int a = -11; a < 11; a++)
        {
            for (int b = -11; b < 11; b++)
            {
                float choose_mat = Random.value;
                Vector3 center = new(a + 0.9f * Random.value, 0.2f, b + 0.9f * Random.value);
                if ((center - new Vector3(4f, 0.2f, 0)).magnitude > 0.9f)
                {
                    if (choose_mat < 0.8f)
                    {
                        // diffuse
                        Sphere s = new()
                        {
                            position = center,
                            radius = 0.2f,
                            mat = new Material(new Vector3(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f))),
                        };
                        spheres.Add(s);
                    }
                    else if (choose_mat < 0.95f)
                    {
                        // metal
                        Sphere s = new()
                        {
                            position = center,
                            radius = 0.2f,
                            mat = new Material(new Vector3(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f)), Random.Range(0f, 0.5f), 0),
                        };
                        spheres.Add(s);
                    }
                    else
                    {
                        // glass
                        Sphere s = new()
                        {
                            position = center,
                            radius = 0.2f,
                            mat = new Material(new Vector3(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f)), 
                            1, Random.Range(1.5f, 3f)),
                        };
                        spheres.Add(s);
                    }
                }
            }
        }

        Material material1 = new(1.5f);
        spheres.Add(new Sphere(new Vector3(0, 1, 0), 1.0f, material1));

        Material material2 = new(new Vector3(0.4f, 0.2f, 0.1f));
        spheres.Add(new Sphere(new Vector3(-4, 1, 0), 1.0f, material2));

        Material material3 = new(new Vector3(0.4f, 0.2f, 0.1f), 0.5f, 0);
        spheres.Add(new Sphere(new Vector3(4, 1, 0), 1.0f, material3));

        _sphereBuffer?.Release();
        if (spheres.Count > 0)
        {
            _sphereBuffer = new ComputeBuffer(spheres.Count, 36);
            _sphereBuffer.SetData(spheres);
        }

        if (_sphereBuffer != null)
            RayTracingShader.SetBuffer(0, "_spheres", _sphereBuffer);

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

