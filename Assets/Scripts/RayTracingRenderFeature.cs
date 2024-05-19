using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class RayTracingRenderFeature : ScriptableRendererFeature
{
    RayTracingPass rayTracingPass;
    
    public override void Create()
    {
        rayTracingPass = new RayTracingPass(RenderPassEvent.BeforeRenderingPostProcessing);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(rayTracingPass);
    }

    public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
    {
        rayTracingPass.Setup(renderer.cameraColorTargetHandle);
    }
}


public class RayTracingPass : ScriptableRenderPass
{
    static readonly string k_RenderTag = "Ray Tracing";

    static readonly int CameraToWorldId = Shader.PropertyToID("_CameraToWorld");
    static readonly int CameraInverseProjectionId = Shader.PropertyToID("_CameraInverseProjection");
    static readonly int SkyboxTextureId = Shader.PropertyToID("_SkyboxTexture");
    static readonly int SkyboxTexisNullId = Shader.PropertyToID("_SkyboxTexisNull");
    static readonly int ResultTextureId = Shader.PropertyToID("Result");
    static readonly int PixelOffsetId = Shader.PropertyToID("_PixelOffset");
    static readonly int RayBounceId = Shader.PropertyToID("_RayBounce");
    static readonly int SeedId = Shader.PropertyToID("_Seed");
    static readonly int DirectionalLightId = Shader.PropertyToID("_DirectionalLight");

    MyRayTracing m_rayTracing;
    RTHandle currentTarget;
    RenderTexture resultTexture;
    RenderTexture cachingTexture;

    uint currentSample = 0;
    Material progressiveSampleMat;
    Matrix4x4 cachingC2W = Matrix4x4.identity;

    public RayTracingPass(RenderPassEvent evt)
    {
        renderPassEvent = evt;
    }

    public void Setup(in RTHandle currentTarget)
    {
        this.currentTarget = currentTarget;
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        // 判断是否执行Execute
        if (!renderingData.cameraData.postProcessEnabled) return; //若没有开启后处理
        var stack = VolumeManager.instance.stack; 
        m_rayTracing = stack.GetComponent<MyRayTracing>();
        if (m_rayTracing == null) return; //若没有光线追踪volume组件
        if (!m_rayTracing.IsActive()) return; //若光线追踪volume组件不可用

        var cmd = CommandBufferPool.Get(k_RenderTag);
        Render(cmd, ref renderingData);
        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    void InitResultTexture(int width, int height)
    {
        if (resultTexture == null || resultTexture.width != width || resultTexture.height != height)
        {
            if (resultTexture != null)
                resultTexture.Release();

            resultTexture = new RenderTexture(width, height, 0, RenderTextureFormat.DefaultHDR, RenderTextureReadWrite.Linear)
            {
                enableRandomWrite = true
            };
            resultTexture.Create();
        }
    }

    void Render(CommandBuffer cmd, ref RenderingData renderingData)
    {
        // 获取相机数据，得到渲染图片的宽w和高h
        ref var cameraData = ref renderingData.cameraData;
        var w = cameraData.camera.scaledPixelWidth;
        var h = cameraData.camera.scaledPixelHeight;

        var source = currentTarget;
        InitResultTexture(w, h);

        //SetShaderParameter
        {
            m_rayTracing.RayTracingShader.SetMatrix(CameraToWorldId, cameraData.GetViewMatrix().inverse);                   //
            m_rayTracing.RayTracingShader.SetMatrix(CameraInverseProjectionId, cameraData.GetProjectionMatrix().inverse);   //
            m_rayTracing.RayTracingShader.SetTexture(0, ResultTextureId, resultTexture);                                    //
            m_rayTracing.RayTracingShader.SetVector(PixelOffsetId, new Vector2(Random.value, Random.value));                //
            m_rayTracing.RayTracingShader.SetInt(RayBounceId, m_rayTracing.rayBounce.value);                                //
            m_rayTracing.RayTracingShader.SetFloat(SeedId, Random.value);                                                   //

            // SetSkyboxTexture
            if (m_rayTracing.SkyboxTexture == null)
            {
                m_rayTracing.RayTracingShader.SetBool(SkyboxTexisNullId, true);
            }
            else
            {
                m_rayTracing.RayTracingShader.SetTexture(0, SkyboxTextureId, m_rayTracing.SkyboxTexture.value);
                m_rayTracing.RayTracingShader.SetBool(SkyboxTexisNullId, false);
            }

            //SceneLights
            var mainLight = renderingData.lightData.visibleLights[renderingData.lightData.mainLightIndex].light;
            m_rayTracing.RayTracingShader.SetVector(DirectionalLightId, new Vector4(mainLight.transform.forward.x, mainLight.transform.forward.y, mainLight.transform.forward.z, mainLight.intensity));

            //RayTracing Objects
            //m_rayTracing.SetRayTracingObjectsParameters();

            //Compute
            int threadGroupsX = Mathf.CeilToInt(w / 8.0f);
            int threadGroupsY = Mathf.CeilToInt(h / 8.0f);
            m_rayTracing.RayTracingShader.Dispatch(0, threadGroupsX, threadGroupsY, 1);
        }

        //Accumulate Sampling
        if(m_rayTracing.AccSample.value)
        {
            if (cameraData.GetViewMatrix().inverse != cachingC2W)
            {
                cachingC2W = cameraData.GetViewMatrix().inverse;
                cachingTexture = new RenderTexture(w, h, 0, RenderTextureFormat.DefaultHDR);
                MyRayTracing.isSetObjects = false;
                currentSample = 0;
            }

            if (progressiveSampleMat == null)
            {
                progressiveSampleMat = new Material(Shader.Find("Hidden/RayTracing/ProgressiveSample"));
            }

            progressiveSampleMat.SetFloat("_Sample", currentSample);
            cmd.Blit(resultTexture, cachingTexture, progressiveSampleMat);
            cmd.Blit(cachingTexture, resultTexture);
            currentSample++;
            //Debug.Log(currentSample);
            m_rayTracing.SetAccSamplingCount(currentSample);
        }

        cmd.Blit(resultTexture, source);
    }
}