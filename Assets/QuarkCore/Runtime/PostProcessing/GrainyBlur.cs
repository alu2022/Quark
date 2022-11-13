using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class GrainyBlur : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        [Range(1, 8)] public int blurTimes = 2;
        [Range(0.0001f, 0.01f)] public float blurRange = 0.00015f;
    }
    public Settings settings;

    GrainyBlurPass grainyBlurPass;

    public override void Create()
    {
        grainyBlurPass = new GrainyBlurPass(RenderPassEvent.BeforeRenderingPostProcessing);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        grainyBlurPass.SetupSrc(renderer.cameraColorTarget, settings);
        renderer.EnqueuePass(grainyBlurPass);
    }
}

public class GrainyBlurPass : ScriptableRenderPass
{
    static readonly int mainTexId = Shader.PropertyToID("_MainTex");
    static readonly int tmpTargetId = Shader.PropertyToID("_TempTargetGrainy");

    private Material grainyBlurMaterial;

    private RenderTargetIdentifier src;
    private GrainyBlur.Settings settings;

    static readonly string cmdName = "GrainyBlur";
    private CommandBuffer cmd;

    public void SetupSrc(in RenderTargetIdentifier src, GrainyBlur.Settings settings)
    {
        this.src = src;
        this.settings = settings;
    }

    public GrainyBlurPass(RenderPassEvent evt)
    {
        renderPassEvent = evt;
        Shader grainyBlurShader = Shader.Find("QuarkPostProcessing/GrainyBlur");
        if (grainyBlurShader is null)
        {
            Debug.LogError("GrainyBlur shader not found.");
            return;
        }
        grainyBlurMaterial = CoreUtils.CreateEngineMaterial(grainyBlurShader);
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        if (!renderingData.cameraData.postProcessEnabled)
            return;

        if (grainyBlurMaterial is null)
        {
            Debug.LogError("GrainyBlur material not created.");
            return;
        }

        cmd = CommandBufferPool.Get(cmdName);
        var camera = renderingData.cameraData.camera;

        grainyBlurMaterial.SetFloat("_BlurRange", settings.blurRange);
        grainyBlurMaterial.SetFloat("_Iteration", settings.blurTimes);

        var width = camera.scaledPixelWidth;
        var height = camera.scaledPixelHeight;

        cmd.GetTemporaryRT(tmpTargetId, width, height, 0, FilterMode.Trilinear, RenderTextureFormat.Default);

        cmd.Blit(src, tmpTargetId, grainyBlurMaterial);

        cmd.Blit(tmpTargetId, src);

        cmd.ReleaseTemporaryRT(tmpTargetId);
        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }
}