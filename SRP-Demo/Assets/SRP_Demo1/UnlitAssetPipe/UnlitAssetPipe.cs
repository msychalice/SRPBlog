using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

[ExecuteInEditMode]
public class UnlitAssetPipe : RenderPipelineAsset
{
#if UNITY_EDITOR
    [UnityEditor.MenuItem("SRP-Demo/04 - Create Unlit Asset Pipeline")]
    static void CreateBasicAssetPipeline()
    {
        UnlitAssetPipe instance = ScriptableObject.CreateInstance<UnlitAssetPipe>();
        UnityEditor.AssetDatabase.CreateAsset(instance, "Assets/SRP_Demo1/UnlitAssetPipe/UnlitAssetPipe.asset");
    }
#endif

    protected override IRenderPipeline InternalCreatePipeline()
    {
        return new UnlitAssetPipeInstance();
    }
}

public class UnlitAssetPipeInstance : RenderPipeline
{
    CommandBuffer _cb;

    //这个函数在管线被销毁的时候调用。
    public override void Dispose()
    {
        base.Dispose();
        if (_cb != null)
        {
            _cb.Dispose();
            _cb = null;
        }
    }

    //这个函数在需要绘制管线的时候调用。
    public override void Render(ScriptableRenderContext renderContext, Camera[] cameras)
    {
        base.Render(renderContext, cameras);

        if (_cb == null)
        {
            _cb = new CommandBuffer();
        }

        //对于每一个相机执行操作。
        foreach (var camera in cameras)
        {
            //将上下文设置为当前相机的上下文。
            renderContext.SetupCameraProperties(camera);

            _cb.name = "Setup";
            //显式将当前渲染目标设置为相机Backbuffer。
            _cb.SetRenderTarget(BuiltinRenderTextureType.CameraTarget);
            //设置渲染目标的颜色为相机背景色。
            _cb.ClearRenderTarget(true, true, camera.backgroundColor);
            renderContext.ExecuteCommandBuffer(_cb);
            _cb.Clear();

            //绘制天空盒子，注意需要在ClearRenderTarget之后进行，不然颜色会被覆盖。
            renderContext.DrawSkybox(camera);

            //执行裁剪
            CullResults culled = new CullResults();
            CullResults.Cull(camera, renderContext, out culled);

            //设置Filtering Settings，并初始化所有参数。
            FilterRenderersSettings fs = new FilterRenderersSettings(true);
            //设置只绘制不透明物体。
            fs.renderQueueRange = RenderQueueRange.all;
            //设置绘制所有层
            fs.layerMask = ~0;

            //设置Renderer Settings
            //注意在构造的时候就需要传入Lightmode参数
            DrawRendererSettings rs = new DrawRendererSettings(camera, new ShaderPassName("Unlit"));
            //由于绘制不透明物体可以借助Z-Buffer，因此不需要额外的排序。
            rs.sorting.flags = SortFlags.None;

            //绘制物体
            renderContext.DrawRenderers(culled.visibleRenderers, ref rs, fs);

            //开始执行管线
            renderContext.Submit();
        }
    }

}
