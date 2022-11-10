using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class KawaseBlur : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        [Range(1, 8)] public int blurTimes = 2;
        [Range(0.0001f, 0.01f)] public float blurRange = 0.00015f;
        [Range(1, 5)] public int downSampling = 4;
    }
    public Settings settings;

    KawaseBlurPass kawaseBlurPass;

    public override void Create()
    {
        kawaseBlurPass = new KawaseBlurPass(RenderPassEvent.BeforeRenderingPostProcessing);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        kawaseBlurPass.SetupSrc(renderer.cameraColorTarget, settings);
        renderer.EnqueuePass(kawaseBlurPass);
    }
}

public class KawaseBlurPass : ScriptableRenderPass
{
    static readonly int mainTexId = Shader.PropertyToID("_MainTex");
    static readonly int tmpTargetId = Shader.PropertyToID("_TempTargetKawase");

    private Material kawaseBlurMaterial;

    private RenderTargetIdentifier src;
    private KawaseBlur.Settings settings;

    static readonly string cmdName = "KawaseBlur";
    private CommandBuffer cmd;

    public void SetupSrc(in RenderTargetIdentifier src, KawaseBlur.Settings settings)
    {
        this.src = src;
        this.settings = settings;
    }

    public KawaseBlurPass(RenderPassEvent evt)
    {
        renderPassEvent = evt;
        Shader KawaseBlurShader = Shader.Find("QuarkPostProcessing/KawaseBlur");
        if (KawaseBlurShader is null)
        {
            Debug.LogError("KawaseBlur shader not found.");
            return;
        }
        kawaseBlurMaterial = CoreUtils.CreateEngineMaterial(KawaseBlurShader);
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        if (!renderingData.cameraData.postProcessEnabled)
            return;

        if (kawaseBlurMaterial is null)
        {
            Debug.LogError("KawaseBlur material not created.");
            return;
        }

        cmd = CommandBufferPool.Get(cmdName);
        var camera = renderingData.cameraData.camera;

        cmd.SetGlobalTexture(mainTexId, src);
        kawaseBlurMaterial.SetFloat("_BlurRange", settings.blurRange);

        var width = camera.scaledPixelWidth / settings.downSampling;
        var height = camera.scaledPixelHeight / settings.downSampling;

        cmd.GetTemporaryRT(tmpTargetId, width, height, 0, FilterMode.Bilinear, RenderTextureFormat.DefaultHDR);

        RenderTargetIdentifier buffer = tmpTargetId;
        
        cmd.Blit(src, buffer, kawaseBlurMaterial);

        for (int i = 0; i < settings.blurTimes; i++)
        {
            kawaseBlurMaterial.SetFloat("_BlurRange", i * settings.blurRange); 
            cmd.Blit(src, buffer, kawaseBlurMaterial);
            var temRT = src;
            src = buffer;
            buffer = temRT;
        }
        kawaseBlurMaterial.SetFloat("_BlurRange", settings.blurTimes * settings.blurRange); 
        cmd.Blit(buffer, src, kawaseBlurMaterial);

        cmd.ReleaseTemporaryRT(tmpTargetId);
        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }
}