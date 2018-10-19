using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

[ExecuteInEditMode]
public class BaseDirLitAssetPipe : RenderPipelineAsset
{
#if UNITY_EDITOR
    [UnityEditor.MenuItem("SRP-Demo/05 - Create BaseDirLit Asset Pipeline")]
    static void CreateBasicAssetPipeline()
    {
        BaseDirLitAssetPipe instance = ScriptableObject.CreateInstance<BaseDirLitAssetPipe>();
        UnityEditor.AssetDatabase.CreateAsset(instance, "Assets/SRP_Demo1/BaseDirLitAssetPipe/BaseDirLitAssetPipe.asset");
    }
#endif

    protected override IRenderPipeline InternalCreatePipeline()
    {
        return new BaseDirLitAssetPipeInstance();
    }
}

public class BaseDirLitAssetPipeInstance : RenderPipeline
{
    CommandBuffer _cb;

    //这个向量用于保存平行光方向。
    Vector3 _LightDir;

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

        if (_cb == null) _cb = new CommandBuffer();
        //准备好光源名称
        int _LightDir = Shader.PropertyToID("_LightDir");
        int _LightColor = Shader.PropertyToID("_LightColor");
        int _CameraPos = Shader.PropertyToID("_CameraPos");



        //对于每一个相机执行操作。
        foreach (Camera camera in cameras)
        {
            //将上下文设置为当前相机的上下文。
            renderContext.SetupCameraProperties(camera);

            _cb.name = "Setup";
            _cb.SetRenderTarget(BuiltinRenderTextureType.CameraTarget);
            //设置渲染目标的颜色为相机背景色。
            _cb.ClearRenderTarget(true, true, camera.backgroundColor);

            //设置相机的着色器全局变量_CameraPos
            Vector4 CameraPosition = new Vector4(camera.transform.localPosition.x, camera.transform.localPosition.y, camera.transform.localPosition.z, 1.0f);
            _cb.SetGlobalVector(_CameraPos, camera.transform.localToWorldMatrix * CameraPosition);
            renderContext.ExecuteCommandBuffer(_cb);
            _cb.Clear();

            //绘制SkyBox
            renderContext.DrawSkybox(camera);

            //执行裁剪
            CullResults culled = new CullResults();
            CullResults.Cull(camera, renderContext, out culled);

            /*
             裁剪会给出三个参数：
             可见的物体列表：visibleRenderers
             可见的灯光：visibleLights
             可见的反射探针（Cubemap）：visibleReflectionProbes
             所有的东西都是未排序的。
             */

            //获取所有的灯光
            List<VisibleLight> lights = culled.visibleLights;
            _cb.name = "RenderLights";
            foreach (VisibleLight light in lights)
            {
                //我们只处理平行光
                if (light.lightType != LightType.Directional)
                    continue;

                //获取光源方向
                Vector4 pos = light.localToWorld.GetColumn(2);
                Vector4 lightDirection = new Vector4(pos.x, pos.y, pos.z, 0);
                //获取光源颜色
                Color lightColor = light.finalColor;
                //构建shader常量缓存
                _cb.SetGlobalVector(_LightDir, lightDirection);
                _cb.SetGlobalColor(_LightColor, lightColor);
                renderContext.ExecuteCommandBuffer(_cb);
                _cb.Clear();

                FilterRenderersSettings rs = new FilterRenderersSettings(true);
                //只渲染固体范围
                rs.renderQueueRange = RenderQueueRange.opaque;
                //包括所有层
                rs.layerMask = ~0;

                //使用Shader中指定光照模式为BaseLit的pass
                DrawRendererSettings ds = new DrawRendererSettings(camera, new ShaderPassName("BaseLit"));
                //绘制物体
                renderContext.DrawRenderers(culled.visibleRenderers, ref ds, rs);

                break;
            }

            //开始执行上下文
            renderContext.Submit();
        }
    }

}
