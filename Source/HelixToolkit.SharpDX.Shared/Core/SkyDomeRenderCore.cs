﻿/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/
using System;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.Direct3D;
#if !NETFX_CORE
namespace HelixToolkit.Wpf.SharpDX.Core
#else
namespace HelixToolkit.UWP.Core
#endif
{
    using Shaders;
    using System.IO;
    using Utilities;
    using Render;
    /// <summary>
    /// 
    /// </summary>
    public class SkyDomeRenderCore : GeometryRenderCore<int>, ISkyboxRenderParams
    {
        #region Default Mesh
        private static readonly MeshGeometry3D SphereMesh;

        static SkyDomeRenderCore()
        {
            var builder = new MeshBuilder(false, false);
            builder.AddSphere(Vector3.Zero, 1);
            SphereMesh = builder.ToMesh();
        }
        #endregion

        private Stream cubeTexture = null;
        /// <summary>
        /// Gets or sets the cube texture.
        /// </summary>
        /// <value>
        /// The cube texture.
        /// </value>
        public Stream CubeTexture
        {
            set
            {
                if (SetAffectsRender(ref cubeTexture, value) && IsAttached)
                {
                    cubeTextureRes.CreateView(value, true);
                }
            }
            get
            {
                return cubeTexture;
            }
        }

        private SamplerStateDescription samplerDescription = DefaultSamplers.CubeSampler;
        /// <summary>
        /// Gets or sets the sampler description.
        /// </summary>
        /// <value>
        /// The sampler description.
        /// </value>
        public SamplerStateDescription SamplerDescription
        {
            set
            {
                if (SetAffectsRender(ref samplerDescription, value) && IsAttached)
                {
                    RemoveAndDispose(ref textureSampler);
                    textureSampler = Collect(EffectTechnique.EffectsManager.StateManager.Register(value));
                }
            }
            get
            {
                return samplerDescription;
            }
        }
        /// <summary>
        /// Gets or sets the name of the shader cube texture.
        /// </summary>
        /// <value>
        /// The name of the shader cube texture.
        /// </value>
        public string ShaderCubeTextureName { set; get; } = DefaultBufferNames.CubeMapTB;
        /// <summary>
        /// Gets or sets the name of the shader cube texture sampler.
        /// </summary>
        /// <value>
        /// The name of the shader cube texture sampler.
        /// </value>
        public string ShaderCubeTextureSamplerName { set; get; } = DefaultSamplerStateNames.CubeMapSampler;

        private ShaderResourceViewProxy cubeTextureRes;
        private int cubeTextureSlot;
        private SamplerState textureSampler;
        private int textureSamplerSlot;
        /// <summary>
        /// Initializes a new instance of the <see cref="SkyBoxRenderCore"/> class.
        /// </summary>
        public SkyDomeRenderCore()
        {
            RasterDescription = DefaultRasterDescriptions.RSSkyDome;
        }
        /// <summary>
        /// Called when [attach].
        /// </summary>
        /// <param name="technique">The technique.</param>
        /// <returns></returns>
        protected override bool OnAttach(IRenderTechnique technique)
        {
            if (base.OnAttach(technique))
            {
                var buffer = Collect(new SkyDomeBufferModel());
                buffer.Geometry = SphereMesh;
                GeometryBuffer = buffer;
                cubeTextureRes = Collect(new ShaderResourceViewProxy(Device));
                if (cubeTexture != null)
                {
                    cubeTextureRes.CreateView(cubeTexture, true);
                }
                textureSampler = Collect(technique.EffectsManager.StateManager.Register(SamplerDescription));
                return true;
            }
            else
            {
                return false;
            }
        }
        /// <summary>
        /// Gets the model constant buffer description.
        /// </summary>
        /// <returns></returns>
        protected override ConstantBufferDescription GetModelConstantBufferDescription()
        {
            return null;
        }
        /// <summary>
        /// Called when [upload per model constant buffers].
        /// </summary>
        /// <param name="context">The context.</param>
        protected override void OnUploadPerModelConstantBuffers(DeviceContext context)
        {

        }

        /// <summary>
        /// Called when [default pass changed].
        /// </summary>
        /// <param name="pass">The pass.</param>
        protected override void OnDefaultPassChanged(ShaderPass pass)
        {
            cubeTextureSlot = pass.GetShader(ShaderStage.Pixel).ShaderResourceViewMapping.TryGetBindSlot(ShaderCubeTextureName);
            textureSamplerSlot = pass.GetShader(ShaderStage.Pixel).SamplerMapping.TryGetBindSlot(ShaderCubeTextureSamplerName);
        }

        protected override bool CanRender(RenderContext context)
        {
            return base.CanRender(context) && GeometryBuffer.VertexBuffer.Length > 0;
        }
        /// <summary>
        /// Called when [render].
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="deviceContext">The device context.</param>
        protected override void OnRender(RenderContext context, DeviceContextProxy deviceContext)
        {
            DefaultShaderPass.BindShader(deviceContext);
            DefaultShaderPass.BindStates(deviceContext, StateType.BlendState | StateType.DepthStencilState);
            DefaultShaderPass.GetShader(ShaderStage.Pixel).BindTexture(deviceContext, cubeTextureSlot, cubeTextureRes);
            DefaultShaderPass.GetShader(ShaderStage.Pixel).BindSampler(deviceContext, textureSamplerSlot, textureSampler);
            deviceContext.DeviceContext.DrawIndexed(GeometryBuffer.IndexBuffer.ElementCount, 0, 0);
        }

        /// <summary>
        /// Called when [render shadow].
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="deviceContext">The device context.</param>
        protected override void OnRenderShadow(RenderContext context, DeviceContextProxy deviceContext)
        {

        }
        /// <summary>
        /// Called when [update per model structure].
        /// </summary>
        /// <param name="model">The model.</param>
        /// <param name="context">The context.</param>
        protected override void OnUpdatePerModelStruct(ref int model, RenderContext context)
        {

        }

        /// <summary>
        /// 
        /// </summary>
        private sealed class SkyDomeBufferModel : MeshGeometryBufferModel<Vector3>
        {
            public SkyDomeBufferModel() : base(Vector3.SizeInBytes)
            {
                Topology = PrimitiveTopology.TriangleList;
            }

            protected override Vector3[] BuildVertexArray(MeshGeometry3D geometry)
            {
                return geometry.Positions.ToArray();
            }
        }
    }
}
