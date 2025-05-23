﻿using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Graphics.PackedVector;

using XNAV2 = Microsoft.Xna.Framework.Vector2;
using XNAV3 = Microsoft.Xna.Framework.Vector3;
using XNAV4 = Microsoft.Xna.Framework.Vector4;

using BYTES = System.Span<byte>;

namespace MonoScene.Graphics.Pipeline
{
    static class MeshPrimitiveDecoder
    {
        public static VertexDeclaration GetVertexDeclaration(this IMeshPrimitiveDecoder src)
        {
            // because the PBR shaders have a limited number of techniques,
            // we require adding these attributes even if they're not used.
            var nColors = Math.Max(src.ColorsCount, 1);
            var nTextrs = Math.Max(src.TexCoordsCount, 2);

            var e = new List<VertexElement>();
            int offset = 0;

            e.Add(new VertexElement(offset, VertexElementFormat.Vector3, VertexElementUsage.Position, 0)); offset += 12;
            e.Add(new VertexElement(offset, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0)); offset += 12;
            e.Add(new VertexElement(offset, VertexElementFormat.Vector4, VertexElementUsage.Tangent, 0)); offset += 16;

            for (int i = 0; i < nColors; ++i)
            {
                e.Add(new VertexElement(offset, VertexElementFormat.Color, VertexElementUsage.Color, i)); offset += 4;
            }

            for (int i = 0; i < nTextrs; ++i)
            {
                e.Add(new VertexElement(offset, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, i)); offset += 8;
            }

            if (src.JointsWeightsCount > 0)
            {
                e.Add(new VertexElement(offset, VertexElementFormat.Short4, VertexElementUsage.BlendIndices, 0)); offset += 8;
                e.Add(new VertexElement(offset, VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 0)); offset += 16;
            }

            return new VertexDeclaration(e.ToArray());
        }

        public static Byte[] ToXnaVertices(this IMeshPrimitiveDecoder src, VertexDeclaration decl)
        {
            var dst = new Byte[src.VertexCount * decl.VertexStride];

            var elements = decl.GetVertexElements();

            for (int i = 0; i < src.VertexCount; ++i)
            {
                var vrt = new VertexEncoder(dst, decl.VertexStride, i);

                var jw = src.GetSkinWeights(i);

                foreach (var e in elements)
                {
                    switch (e.VertexElementUsage)
                    {
                        case VertexElementUsage.Position: vrt.Encode(e, src.GetPosition(i)); break;
                        case VertexElementUsage.Normal: vrt.Encode(e, src.GetNormal(i)); break;
                        case VertexElementUsage.Tangent: vrt.Encode(e, src.GetTangent(i)); break;
                        case VertexElementUsage.Color: vrt.Encode(e, src.GetColor(i, e.UsageIndex)); break;
                        case VertexElementUsage.TextureCoordinate: vrt.Encode(e, src.GetTextureCoord(i, e.UsageIndex)); break;
                        case VertexElementUsage.BlendIndices: vrt.Encode(e, jw.Indices); break;
                        case VertexElementUsage.BlendWeight: vrt.Encode(e, jw.Weights); break;
                    }
                }
            }

            return dst;
        }
    }

    /// <summary>
    /// helps encoding a vertex byte array
    /// TODO: if the host platform is Big Endian, we need to reverse the byte order here.
    /// </summary>
    readonly ref struct VertexEncoder
    {
        #region constructors
        public VertexEncoder(BYTES array, int vertexStride, int index)
        {
            _Vertex = array.Slice(index * vertexStride, vertexStride);
        }

        public VertexEncoder(BYTES vertex) { _Vertex = vertex; }

        #endregion

        #region data

        private readonly BYTES _Vertex;

        #endregion

        #region API

        public void Encode(VertexElement element, XNAV2 value)
        {
            var dstVertex = _Vertex.Slice(element.Offset);

            if (element.VertexElementFormat == VertexElementFormat.Vector2)
            {
                System.Runtime.InteropServices.MemoryMarshal.Write(dstVertex, value);
                return;
            }

            throw new ArgumentException(nameof(element));
        }

        public void Encode(VertexElement element, XNAV3 value)
        {
            var dstVertex = _Vertex.Slice(element.Offset);

            if (element.VertexElementFormat == VertexElementFormat.Vector3)
            {
                System.Runtime.InteropServices.MemoryMarshal.Write(dstVertex, value);
                return;
            }

            if (element.VertexElementFormat == VertexElementFormat.Color)
            {
                var c = new Color(value);
                System.Runtime.InteropServices.MemoryMarshal.Write(dstVertex, c);
                return;
            }

            throw new ArgumentException(nameof(element));
        }

        public void Encode(VertexElement element, XNAV4 value)
        {
            var dstVertex = _Vertex.Slice(element.Offset);

            if (element.VertexElementFormat == VertexElementFormat.Vector4)
            {
                System.Runtime.InteropServices.MemoryMarshal.Write(dstVertex, value);
                return;
            }

            if (element.VertexElementFormat == VertexElementFormat.Byte4)
            {
                var c = new Byte4(value);
                System.Runtime.InteropServices.MemoryMarshal.Write(dstVertex, c);
                return;
            }

            if (element.VertexElementFormat == VertexElementFormat.Color)
            {
                var c = new Color(value);
                System.Runtime.InteropServices.MemoryMarshal.Write(dstVertex, c);
                return;
            }

            if (element.VertexElementFormat == VertexElementFormat.Short4)
            {
                var ns = new Short4(value);
                System.Runtime.InteropServices.MemoryMarshal.Write(dstVertex, ns);
                return;
            }

            if (element.VertexElementFormat == VertexElementFormat.NormalizedShort4)
            {
                var ns = new NormalizedShort4(value);
                System.Runtime.InteropServices.MemoryMarshal.Write(dstVertex, ns);
                return;
            }

            if (element.VertexElementFormat == VertexElementFormat.HalfVector4)
            {
                var ns = new HalfVector4(value);
                System.Runtime.InteropServices.MemoryMarshal.Write(dstVertex, ns);
                return;
            }

            throw new ArgumentException(nameof(element));
        }

        public void Encode(VertexElement element, Short4 value)
        {
            var dstVertex = _Vertex.Slice(element.Offset);

            if (element.VertexElementFormat == VertexElementFormat.Vector4)
            {
                var v4 = value.ToVector4();
                System.Runtime.InteropServices.MemoryMarshal.Write(dstVertex, v4);
                return;
            }

            if (element.VertexElementFormat == VertexElementFormat.Short4)
            {
                System.Runtime.InteropServices.MemoryMarshal.Write(dstVertex, value);
                return;
            }

            throw new ArgumentException(nameof(element));
        }

        #endregion
    }
}
