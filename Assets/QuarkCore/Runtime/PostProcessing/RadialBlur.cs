using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class RadialBlur : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        [Range(1, 8)] public int blurTimes = 2;
        [Range(0.0001f, 1.0f)] public float blurRange = 0.00015f;
    }
    public Settings settings;

    RadialBlurPass radialBlurPass;

    public override void Create()
    {
        radialBlurPass = new RadialBlurPass(RenderPassEvent.BeforeRenderingPostProcessing);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        radialBlurPass.SetupSrc(renderer.cameraColorTarget, settings);
        renderer.EnqueuePass(radialBlurPass);
    }
}

public class RadialBlurPass : ScriptableRenderPass
{
    static readonly int mainTexId = Shader.PropertyToID("_MainTex");
    static readonly int tmpTargetId = Shader.PropertyToID("_TempTargetRadial");

    private Material radialBlurMaterial;

    private RenderTargetIdentifier src;
    private RadialBlur.Settings settings;

    static readonly string cmdName = "RadialBlur";
    private CommandBuffer cmd;

    public void SetupSrc(in RenderTargetIdentifier src, RadialBlur.Settings settings)
    {
        this.src = src;
        this.settings = settings;
    }

    public RadialBlurPass(RenderPassEvent evt)
    {
        renderPassEvent = evt;
        Shader radialBlurShader = Shader.Find("QuarkPostProcessing/RadialBlur");
        if (radialBlurShader is null)
        {
            Debug.LogError("RadialBlur shader not found.");
            return;
        }
        radialBlurMaterial = CoreUtils.CreateEngineMaterial(radialBlurShader);
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        if (!renderingData.cameraData.postProcessEnabled)
            return;

        if (radialBlurMaterial is null)
        {
            Debug.LogError("RadialBlur material not created.");
            return;
        }

        cmd = CommandBufferPool.Get(cmdName);
        var camera = renderingData.cameraData.camera;

        radialBlurMaterial.SetFloat("_BlurRange", settings.blurRange);
        radialBlurMaterial.SetFloat("_Iteration", settings.blurTimes);

        var width = camera.scaledPixelWidth;
        var height = camera.scaledPixelHeight;

        cmd.GetTemporaryRT(tmpTargetId, width, height, 0, FilterMode.Trilinear, RenderTextureFormat.Default);

        cmd.Blit(src, tmpTargetId, radialBlurMaterial);

        cmd.Blit(tmpTargetId, src);

        cmd.ReleaseTemporaryRT(tmpTargetId);
        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }
}