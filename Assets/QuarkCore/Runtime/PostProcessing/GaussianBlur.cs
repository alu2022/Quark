using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class GaussianBlur : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        [Range(1, 8)] public int blurTimes = 2;
        [Range(0.0001f, 0.01f)] public float blurRange = 0.00015f;
        [Range(1, 5)] public int downSampling = 4;
    }
    public Settings settings;

    GaussianBlurPass gaussianBlurPass;

    public override void Create()
    {
        gaussianBlurPass = new GaussianBlurPass(RenderPassEvent.BeforeRenderingPostProcessing);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        gaussianBlurPass.SetupSrc(renderer.cameraColorTarget, settings);
        renderer.EnqueuePass(gaussianBlurPass);
    }
}

public class GaussianBlurPass : ScriptableRenderPass
{
    static readonly int mainTexId = Shader.PropertyToID("_MainTex");
    static readonly int tmpTargetId = Shader.PropertyToID("_TempTargetGaussian");

    private Material gaussianBlurMaterial;

    private RenderTargetIdentifier src;
    private GaussianBlur.Settings settings;

    static readonly string cmdName = "GaussianBlur";
    private CommandBuffer cmd;

    public void SetupSrc(in RenderTargetIdentifier src, GaussianBlur.Settings settings)
    {
        this.src = src;
        this.settings = settings;
    }

    public GaussianBlurPass(RenderPassEvent evt)
    {
        renderPassEvent = evt;
        Shader GaussianBlurShader = Shader.Find("QuarkPostProcessing/GaussianBlur");
        if (GaussianBlurShader is null)
        {
            Debug.LogError("GaussianBlur shader not found.");
            return;
        }
        gaussianBlurMaterial = CoreUtils.CreateEngineMaterial(GaussianBlurShader);
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        if (!renderingData.cameraData.postProcessEnabled)
            return;

        if (gaussianBlurMaterial is null)
        {
            Debug.LogError("GaussianBlur material not created.");
            return;
        }

        cmd = CommandBufferPool.Get(cmdName);
        var camera = renderingData.cameraData.camera;

        cmd.SetGlobalTexture(mainTexId, src);
        gaussianBlurMaterial.SetFloat("_BlurRange", settings.blurRange);

        var width = camera.scaledPixelWidth / settings.downSampling;
        var height = camera.scaledPixelHeight / settings.downSampling;
        
        cmd.GetTemporaryRT(tmpTargetId, width, height, 0, FilterMode.Point, RenderTextureFormat.Default);
        cmd.Blit(src, tmpTargetId);

        for (int i = 0; i < settings.blurTimes; ++i)
        {
            cmd.Blit(tmpTargetId, src, gaussianBlurMaterial, 0);
            cmd.Blit(src, tmpTargetId, gaussianBlurMaterial, 1);
        }

        cmd.Blit(tmpTargetId, src);

        cmd.ReleaseTemporaryRT(tmpTargetId);
        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }
}