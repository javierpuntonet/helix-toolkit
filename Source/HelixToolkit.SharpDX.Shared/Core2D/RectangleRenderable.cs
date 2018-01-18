﻿/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

#if NETFX_CORE
namespace HelixToolkit.UWP.Core2D
#else
namespace HelixToolkit.Wpf.SharpDX.Core2D
#endif
{
    public class RectangleRenderable : ShapeRenderable2DBase
    {
        protected override void OnRender(IRenderContext2D context)
        {
            if (FillBrush != null)
            {
                context.D2DTarget.FillRectangle(LocalDrawingRect, FillBrush);
            }
            if (StrokeBrush != null && StrokeStyle != null)
            {
                context.D2DTarget.DrawRectangle(LocalDrawingRect, StrokeBrush, StrokeWidth, StrokeStyle);
            }
        }
    }
}