using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class DepthNormal : ScriptableRendererFeature
{
    DepthNormalPass depthNormalPass;
    RenderTargetHandle depthNormalsTexture;
    Material depthNormalsMaterial;

    public override void Create()
    {
        depthNormalsMaterial = CoreUtils.CreateEngineMaterial("Hidden/Internal-DepthNormalsTexture");
        depthNormalPass = new DepthNormalPass(RenderQueueRange.opaque, -1, depthNormalsMaterial);
        depthNormalPass.renderPassEvent = RenderPassEvent.AfterRenderingPrePasses;
        depthNormalsTexture.Init("_CameraDepthNormalsTexture");
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        depthNormalPass.Setup(renderingData.cameraData.cameraTargetDescriptor, depthNormalsTexture);
        renderer.EnqueuePass(depthNormalPass);
    }
}

public class DepthNormalPass : ScriptableRenderPass
{
    static readonly string cmdName = "DepthNormal";
    private CommandBuffer cmd;

    int depthBufferBits = 32;
    private RenderTargetHandle depthAttachmentHandle { get; set; }
    internal RenderTextureDescriptor descriptor { get; private set; }

    private Material depthNormalMaterial = null;
    private FilteringSettings filteringSettings;
    ShaderTagId shaderTagId = new ShaderTagId("DepthOnly");

    public void Setup(RenderTextureDescriptor baseDescriptor, RenderTargetHandle depthAttachmentHandle)
    {
        this.depthAttachmentHandle = depthAttachmentHandle;
        baseDescriptor.colorFormat = RenderTextureFormat.ARGB32;
        baseDescriptor.depthBufferBits = depthBufferBits;
        descriptor = baseDescriptor;
    }

    public DepthNormalPass(RenderQueueRange renderQueueRange, LayerMask layerMask, Material material)
    {
        filteringSettings = new FilteringSettings(renderQueueRange, layerMask);
        depthNormalMaterial = material;
    }

    public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
    {
        cmd.GetTemporaryRT(depthAttachmentHandle.id, descriptor, FilterMode.Point);
        ConfigureTarget(depthAttachmentHandle.Identifier());
        ConfigureClear(ClearFlag.All, Color.black);
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        cmd = CommandBufferPool.Get(cmdName);

        using (new ProfilingScope(cmd, new ProfilingSampler(cmdName)))
        {
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            var sortFlags = renderingData.cameraData.defaultOpaqueSortFlags;
            var drawSettings = CreateDrawingSettings(shaderTagId, ref renderingData, sortFlags);
            drawSettings.perObjectData = PerObjectData.None;

            ref CameraData cameraData = ref renderingData.cameraData;
            Camera camera = cameraData.camera;

            drawSettings.overrideMaterial = depthNormalMaterial;

            context.DrawRenderers(renderingData.cullResults, ref drawSettings, ref filteringSettings);

            cmd.SetGlobalTexture("_CameraDepthNormalsTexture", depthAttachmentHandle.id);
        }

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    public override void FrameCleanup(CommandBuffer cmd)
    {
        if (depthAttachmentHandle != RenderTargetHandle.CameraTarget)
        {
            cmd.ReleaseTemporaryRT(depthAttachmentHandle.id);
            depthAttachmentHandle = RenderTargetHandle.CameraTarget;
        }
    }
}