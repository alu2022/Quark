using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class DualKawaseBlur : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        [Range(1, 8)] public int blurTimes = 2;
        [Range(0.0001f, 0.01f)] public float blurRange = 0.00015f;
        [Range(1, 5)] public int downSampling = 4;
    }
    public Settings settings;

    DualKawaseBlurPass dualKawaseBlurPass;

    public override void Create()
    {
        dualKawaseBlurPass = new DualKawaseBlurPass(RenderPassEvent.BeforeRenderingPostProcessing);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        dualKawaseBlurPass.SetupSrc(renderer.cameraColorTarget, settings);
        renderer.EnqueuePass(dualKawaseBlurPass);
    }
}

public class DualKawaseBlurPass : ScriptableRenderPass
{
    static readonly int mainTexId = Shader.PropertyToID("_MainTex");
    static readonly int tmpTargetId = Shader.PropertyToID("_TempTargetKawase");

    private Material dualKawaseBlurMaterial;

    private RenderTargetIdentifier src;
    private DualKawaseBlur.Settings settings;

    static readonly string cmdName = "DualKawaseBlur";
    private CommandBuffer cmd;

    public void SetupSrc(in RenderTargetIdentifier src, DualKawaseBlur.Settings settings)
    {
        this.src = src;
        this.settings = settings;
    }

    public DualKawaseBlurPass(RenderPassEvent evt)
    {
        renderPassEvent = evt;
        Shader DualKawaseBlurShader = Shader.Find("QuarkPostProcessing/DualKawaseBlur");
        if (DualKawaseBlurShader is null)
        {
            Debug.LogError("DualKawaseBlur shader not found.");
            return;
        }
        dualKawaseBlurMaterial = CoreUtils.CreateEngineMaterial(DualKawaseBlurShader);
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        if (!renderingData.cameraData.postProcessEnabled)
            return;

        if (dualKawaseBlurMaterial is null)
        {
            Debug.LogError("DualKawaseBlur material not created.");
            return;
        }

        cmd = CommandBufferPool.Get(cmdName);
        var camera = renderingData.cameraData.camera;

        //cmd.SetGlobalTexture(mainTexId, src);
        dualKawaseBlurMaterial.SetFloat("_BlurRange", settings.blurRange);

        var width = camera.scaledPixelWidth / settings.downSampling;
        var height = camera.scaledPixelHeight / settings.downSampling;

        int[] sampleRT = new int[settings.blurTimes];

        for (int i = 0; i < settings.blurTimes; i++)
        {
            sampleRT[i] = Shader.PropertyToID("Sample" + i);
        }

        RenderTargetIdentifier tmpRT = src;

        for (int i = 0; i < settings.blurTimes; ++i)
        {
            cmd.GetTemporaryRT(sampleRT[i], width, height, 0, FilterMode.Bilinear, RenderTextureFormat.Default);
            width = Mathf.Max(width / 2, 1);
            height = Mathf.Max(height / 2, 1);
            cmd.Blit(tmpRT, sampleRT[i], dualKawaseBlurMaterial, 0);
            tmpRT = sampleRT[i];
        }

        int tmp = 0;
        cmd.GetTemporaryRT(tmp, width, height, 0, FilterMode.Bilinear, RenderTextureFormat.Default);
        cmd.Blit(tmpRT, tmp);
        tmpRT = tmp;

        for (int j = settings.blurTimes - 1; j >= 0; --j) 
        {
            cmd.Blit(tmpRT, sampleRT[j], dualKawaseBlurMaterial, 1);
            tmpRT = sampleRT[j];
        }

        cmd.Blit(tmpRT, src);

        cmd.ReleaseTemporaryRT(tmp);
        for (int i = 0; i < settings.blurTimes; i++)
        {
            cmd.ReleaseTemporaryRT(sampleRT[i]);
        }
        
        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }
}