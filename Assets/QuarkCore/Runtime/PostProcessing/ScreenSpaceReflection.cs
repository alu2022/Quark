using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class ScreenSpaceReflection : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        public float MaxStep = 10;
        public float StepSize = 1;
        public float MaxDistance = 10;
        public float Thickness = 1;
        public float blurRange = 0.00015f;
        public int downSampling = 4;
        public int blurTimes = 2;
    }
    public Settings settings;

    ScreenSpaceReflectionPass screenSpaceReflectionPass;
    RenderTargetHandle ssrHandle;

    public override void Create()
    {
        screenSpaceReflectionPass = new ScreenSpaceReflectionPass(RenderPassEvent.AfterRenderingTransparents);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        ssrHandle.Init("_SSRTexture");
        screenSpaceReflectionPass.SetupSrc(renderer.cameraColorTarget, settings, ssrHandle);
        renderer.EnqueuePass(screenSpaceReflectionPass);
    }
}

public class ScreenSpaceReflectionPass : ScriptableRenderPass
{
    private Material screenSpaceReflectionMaterial;

    private RenderTextureDescriptor dst;
    private RenderTargetHandle ssrHandle;
    private RenderTargetIdentifier src;
    private ScreenSpaceReflection.Settings settings;

    private int[] downSampleID;
    private int[] upSampleID;

    private static readonly string cmdName = "ScreenSpaceReflection";
    private CommandBuffer cmd;

    public void SetupSrc(in RenderTargetIdentifier src, ScreenSpaceReflection.Settings settings, RenderTargetHandle ssrHandle)
    {
        this.src = src;
        this.settings = settings;
        this.ssrHandle = ssrHandle;
    }

    public ScreenSpaceReflectionPass(RenderPassEvent evt)
    {
        renderPassEvent = evt;
        Shader ScreenSpaceReflectionShader = Shader.Find("QuarkPostProcessing/ScreenSpaceReflection");
        if (ScreenSpaceReflectionShader is null)
        {
            Debug.LogError("ScreenSpaceReflection shader not found.");
            return;
        }
        screenSpaceReflectionMaterial = CoreUtils.CreateEngineMaterial(ScreenSpaceReflectionShader);
    }

    public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
    {
        dst = cameraTextureDescriptor;
        cmd.GetTemporaryRT(ssrHandle.id, dst, FilterMode.Bilinear);
        ConfigureTarget(ssrHandle.Identifier());
        ConfigureClear(ClearFlag.All, Color.black);
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        if (!renderingData.cameraData.postProcessEnabled)
            return;

        if (screenSpaceReflectionMaterial is null)
        {
            Debug.LogError("ScreenSpaceReflection material not created.");
            return;
        }

        cmd = CommandBufferPool.Get(cmdName);
        var camera = renderingData.cameraData.camera;

        using (new ProfilingScope(cmd, new ProfilingSampler(cmdName)))
        {

            cmd.Blit(src, ssrHandle.Identifier(), screenSpaceReflectionMaterial, 0);
            cmd.SetGlobalTexture("_SSRTexture", ssrHandle.Identifier());
            screenSpaceReflectionMaterial.SetFloat("_MaxStep", settings.MaxStep);
            screenSpaceReflectionMaterial.SetFloat("_StepSize", settings.StepSize);
            screenSpaceReflectionMaterial.SetFloat("_MaxDistance", settings.MaxDistance);
            screenSpaceReflectionMaterial.SetFloat("_Thickness", settings.Thickness);

            int width = dst.width / settings.downSampling;
            int height = dst.width / settings.downSampling;
            downSampleID = new int[settings.blurTimes];
            upSampleID = new int[settings.blurTimes];
            screenSpaceReflectionMaterial.SetFloat("_BlurRange", settings.blurRange);
            for (int i = 0; i < settings.blurTimes; ++i)
            {
                downSampleID[i] = Shader.PropertyToID("_DownSample" + i);
                upSampleID[i] = Shader.PropertyToID("_UpSample" + i);
            }
            RenderTargetIdentifier temp = ssrHandle.Identifier();
            for (int i = 0; i < settings.blurTimes; ++i)
            {
                cmd.GetTemporaryRT(downSampleID[i], width, height, dst.depthBufferBits, FilterMode.Bilinear, RenderTextureFormat.ARGB32);
                cmd.GetTemporaryRT(upSampleID[i], width, height, dst.depthBufferBits, FilterMode.Bilinear, RenderTextureFormat.ARGB32);
                height = Mathf.Max(height / 2, 1);
                width = Mathf.Max(width / 2, 1);
                cmd.Blit(temp, downSampleID[i], screenSpaceReflectionMaterial, 1);
                temp = downSampleID[i];
            }
            for (int j = settings.blurTimes - 2; j >= 0; --j)
            {
                cmd.Blit(temp, upSampleID[j], screenSpaceReflectionMaterial, 2);
                temp = upSampleID[j];
            }
            cmd.Blit(temp, ssrHandle.Identifier());
            for (int k = 0; k < settings.blurTimes; ++k)
            {
                cmd.ReleaseTemporaryRT(downSampleID[k]);
                cmd.ReleaseTemporaryRT(upSampleID[k]);
            }

            cmd.Blit(ssrHandle.Identifier(), src);
        }

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }
}