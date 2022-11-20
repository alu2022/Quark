using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class Outline : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        public float outlineThickness;
        public float depthSensitivity;
        public float normalsSensitivity;
        public float colorSensitivity;
        public Color outlineColor;
    }
    public Settings settings;

    OutlinePass outlinePass;

    public override void Create()
    {
        outlinePass = new OutlinePass(RenderPassEvent.BeforeRenderingPostProcessing);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        outlinePass.SetupSrc(renderer.cameraColorTarget, settings);
        renderer.EnqueuePass(outlinePass);
    }
}

public class OutlinePass : ScriptableRenderPass
{
    static readonly int tmpTargetId = Shader.PropertyToID("_TempTargetOutline");

    private Material outlineMaterial;

    private RenderTargetIdentifier src;
    private Outline.Settings settings;

    static readonly string cmdName = "Outline";
    private CommandBuffer cmd;

    public void SetupSrc(in RenderTargetIdentifier src, Outline.Settings settings)
    {
        this.src = src;
        this.settings = settings;
    }

    public OutlinePass(RenderPassEvent evt)
    {
        renderPassEvent = evt;
        Shader outlineShader = Shader.Find("QuarkPostProcessing/Outline");
        if (outlineShader is null)
        {
            Debug.LogError("Outline shader not found.");
            return;
        }
        outlineMaterial = CoreUtils.CreateEngineMaterial(outlineShader);
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        if (!renderingData.cameraData.postProcessEnabled)
            return;

        if (outlineMaterial is null)
        {
            Debug.LogError("Outline material not created.");
            return;
        }

        cmd = CommandBufferPool.Get(cmdName);
        var camera = renderingData.cameraData.camera;

        outlineMaterial.SetFloat("_OutlineThickness", settings.outlineThickness);
        outlineMaterial.SetFloat("_DepthSensitivity", settings.depthSensitivity);
        outlineMaterial.SetFloat("_NormalsSensitivity", settings.normalsSensitivity);
        outlineMaterial.SetFloat("_ColorSensitivity", settings.colorSensitivity);
        outlineMaterial.SetColor("_OutlineColor", settings.outlineColor);

        var width = camera.scaledPixelWidth;
        var height = camera.scaledPixelHeight;

        cmd.GetTemporaryRT(tmpTargetId, width, height, 0, FilterMode.Trilinear, RenderTextureFormat.Default);

        cmd.Blit(src, tmpTargetId, outlineMaterial);
        cmd.Blit(tmpTargetId, src);

        cmd.ReleaseTemporaryRT(tmpTargetId);
        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }
}