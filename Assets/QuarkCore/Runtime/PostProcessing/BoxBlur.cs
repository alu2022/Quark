using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class BoxBlur : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        [Range(1, 8)] public int blurTimes = 2;
        [Range(0.0001f, 0.01f)] public float blurRange = 0.00015f;
    }
    public Settings settings;

    BoxBlurPass boxBlurPass;

    public override void Create()
    {
        boxBlurPass = new BoxBlurPass(RenderPassEvent.BeforeRenderingPostProcessing);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        boxBlurPass.SetupSrc(renderer.cameraColorTarget, settings);
        renderer.EnqueuePass(boxBlurPass);
    }
}

public class BoxBlurPass : ScriptableRenderPass
{
    static readonly int mainTexId = Shader.PropertyToID("_MainTex");
    static readonly int tmpTargetId = Shader.PropertyToID("_TempTargetGaussian");

    private Material boxBlurMaterial;

    private RenderTargetIdentifier src;
    private BoxBlur.Settings settings;

    static readonly string cmdName = "BoxBlur";
    private CommandBuffer cmd;

    public void SetupSrc(in RenderTargetIdentifier src, BoxBlur.Settings settings)
    {
        this.src = src;
        this.settings = settings;
    }

    public BoxBlurPass(RenderPassEvent evt)
    {
        renderPassEvent = evt;
        Shader boxBlurShader = Shader.Find("QuarkPostProcessing/BoxBlur");
        if (boxBlurShader is null)
        {
            Debug.LogError("BoxBlur shader not found.");
            return;
        }
        boxBlurMaterial = CoreUtils.CreateEngineMaterial(boxBlurShader);
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        if (!renderingData.cameraData.postProcessEnabled)
            return;

        if (boxBlurMaterial is null)
        {
            Debug.LogError("BoxBlur material not created.");
            return;
        }

        cmd = CommandBufferPool.Get(cmdName);
        var camera = renderingData.cameraData.camera;

        cmd.SetGlobalTexture(mainTexId, src);
        boxBlurMaterial.SetFloat("_BlurRange", settings.blurRange);

        for (int i = 0; i < settings.blurTimes; ++i) {
            cmd.GetTemporaryRT(
                tmpTargetId,
                camera.pixelWidth,
                camera.pixelHeight,
                0,
                FilterMode.Point,
                RenderTextureFormat.Default
            );

            cmd.Blit(src, tmpTargetId, boxBlurMaterial, 0);
            cmd.Blit(tmpTargetId, src);

            cmd.ReleaseTemporaryRT(tmpTargetId);
        }

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }
}