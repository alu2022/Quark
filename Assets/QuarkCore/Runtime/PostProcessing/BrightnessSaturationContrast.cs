using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class BrightnessSaturationContrast : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        public float brightness = 1.0f;
        public float saturation = 1.0f;
        public float contrast = 1.0f;
    }
    public Settings settings;

    BrightnessSaturationContrastPass briSatConPass;

    public override void Create()
    {
        briSatConPass = new BrightnessSaturationContrastPass(RenderPassEvent.BeforeRenderingPostProcessing);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        briSatConPass.SetupSrc(renderer.cameraColorTarget, settings);
        renderer.EnqueuePass(briSatConPass);
    }
}

public class BrightnessSaturationContrastPass : ScriptableRenderPass
{
    static readonly int tmpTargetId = Shader.PropertyToID("_TempTargetBrightnessSaturationContrast");

    private Material briSatConMaterial;

    private RenderTargetIdentifier src;
    private BrightnessSaturationContrast.Settings settings;

    static readonly string cmdName = "BrightnessSaturationContrast";
    private CommandBuffer cmd;

    public void SetupSrc(in RenderTargetIdentifier src, BrightnessSaturationContrast.Settings settings)
    {
        this.src = src;
        this.settings = settings;
    }

    public BrightnessSaturationContrastPass(RenderPassEvent evt)
    {
        renderPassEvent = evt;
        Shader briSatConShader = Shader.Find("QuarkPostProcessing/BrightnessSaturationContrast");
        if (briSatConShader is null)
        {
            Debug.LogError("BrightnessSaturationContrast shader not found.");
            return;
        }
        briSatConMaterial = CoreUtils.CreateEngineMaterial(briSatConShader);
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        if (!renderingData.cameraData.postProcessEnabled)
            return;

        if (briSatConMaterial is null)
        {
            Debug.LogError("BrightnessSaturationContrast material not created.");
            return;
        }

        cmd = CommandBufferPool.Get(cmdName);
        var camera = renderingData.cameraData.camera;

        briSatConMaterial.SetFloat("_Brightness", settings.brightness);
        briSatConMaterial.SetFloat("_Saturation", settings.saturation);
        briSatConMaterial.SetFloat("_Contrast", settings.contrast);

        var width = camera.scaledPixelWidth;
        var height = camera.scaledPixelHeight;

        cmd.GetTemporaryRT(tmpTargetId, width, height, 0, FilterMode.Bilinear, RenderTextureFormat.Default);

        cmd.Blit(src, tmpTargetId, briSatConMaterial);
        cmd.Blit(tmpTargetId, src);

        cmd.ReleaseTemporaryRT(tmpTargetId);
        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }
}