using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class FastBloom : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        public float threshold = 0.0f;
        public float intensity = 1.0f;
        public float blurRange = 0.00015f;
        public int downSampling = 4;
        public int blurTimes = 4;
    }
    public Settings settings;

    FastBloomPass bloomPass;

    public override void Create()
    {
        bloomPass = new FastBloomPass(RenderPassEvent.BeforeRenderingPostProcessing);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        bloomPass.SetupSrc(renderer.cameraColorTarget, settings);
        renderer.EnqueuePass(bloomPass);
    }
}

public class FastBloomPass : ScriptableRenderPass
{
    static readonly int tmpTargetId1 = Shader.PropertyToID("_TempTargetBloom");

    int[] tmpTargetIds;

    private Material bloomMaterial;

    private RenderTargetIdentifier src;
    private FastBloom.Settings settings;

    static readonly string cmdName = "Bloom";
    private CommandBuffer cmd;

    public void SetupSrc(in RenderTargetIdentifier src, FastBloom.Settings settings)
    {
        this.src = src;
        this.settings = settings;
    }

    public FastBloomPass(RenderPassEvent evt)
    {
        renderPassEvent = evt;
        Shader FastBloomShader = Shader.Find("QuarkPostProcessing/Bloom");
        if (FastBloomShader is null)
        {
            Debug.LogError("Bloom shader not found.");
            return;
        }
        bloomMaterial = CoreUtils.CreateEngineMaterial(FastBloomShader);
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        if (!renderingData.cameraData.postProcessEnabled)
            return;

        if (bloomMaterial is null)
        {
            Debug.LogError("Bloom material not created.");
            return;
        }

        cmd = CommandBufferPool.Get(cmdName);
        var camera = renderingData.cameraData.camera;

        //cmd.SetGlobalTexture(mainTexId, src);
        bloomMaterial.SetFloat("_Threshold", settings.threshold);
        bloomMaterial.SetFloat("_Intensity", settings.intensity);
        bloomMaterial.SetFloat("_BlurRange", settings.blurRange);

        var width = camera.scaledPixelWidth;
        var height = camera.scaledPixelHeight;
        
        cmd.GetTemporaryRT(tmpTargetId1, width, height, 0, FilterMode.Point, RenderTextureFormat.Default);
        
        cmd.Blit(src, tmpTargetId1, bloomMaterial, 0);

        int half = settings.blurTimes / 2;
        tmpTargetIds = new int[half];
        int idx = 0;
        for (; idx < half; ++idx)
        {
            width /= settings.downSampling;
            height /= settings.downSampling;
            tmpTargetIds[idx] = Shader.PropertyToID($"_TempTargetBloomBlur{idx+1}");
            cmd.GetTemporaryRT(tmpTargetIds[idx], width, height, 0, FilterMode.Point, RenderTextureFormat.Default);

            cmd.Blit(tmpTargetId1, tmpTargetIds[idx], bloomMaterial, 1);
            cmd.Blit(tmpTargetIds[idx], tmpTargetId1, bloomMaterial, 2);
        }
        --idx;
        for (; idx >= 0; --idx)
        {
            width *= settings.downSampling;
            height *= settings.downSampling;
            cmd.GetTemporaryRT(tmpTargetIds[idx], width, height, 0, FilterMode.Point, RenderTextureFormat.Default);

            cmd.Blit(tmpTargetId1, tmpTargetIds[idx], bloomMaterial, 1);
            cmd.Blit(tmpTargetIds[idx], tmpTargetId1, bloomMaterial, 2);
        }
        ++idx;

        cmd.SetGlobalTexture("_BloomTex", tmpTargetId1);
        cmd.Blit(src, tmpTargetIds[idx], bloomMaterial, 3); 
        cmd.Blit(tmpTargetIds[idx], src);

        cmd.ReleaseTemporaryRT(tmpTargetId1);
        for (; idx < half; ++idx) cmd.ReleaseTemporaryRT(tmpTargetIds[idx]);
        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }
}