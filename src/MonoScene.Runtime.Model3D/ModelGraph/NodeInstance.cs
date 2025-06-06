﻿using System;
using System.Collections.Generic;

using MonoScene.Graphics.Content;

using XFORM = Microsoft.Xna.Framework.Matrix;
// using SPARSE8 = Microsoft.Xna.Framework.Vector4;

namespace MonoScene.Graphics
{
    /// <summary>
    /// Represents a hierarchical element within an <see cref="ArmatureInstance"/>.<br/>    
    /// </summary>
    /// <remarks>
    /// <para>
    /// Lifecycle flow:<br/>
    /// <see cref="NodeContent"/> ➔ <b><see cref="NodeInstance"/></b>
    /// </para>
    /// </remarks>
    [System.Diagnostics.DebuggerDisplay("{Name}")]
    public sealed class NodeInstance
    {
        #region lifecycle
        internal NodeInstance(NodeContent template, NodeInstance parent)
        {
            _Template = template;
            _Parent = parent;
        }

        #endregion

        #region data

        private readonly NodeContent _Template;
        private readonly NodeInstance _Parent;

        private XFORM _LocalMatrix;
        private XFORM? _ModelMatrix;

        // private SPARSE8 _MorphWeights;

        #endregion

        #region properties

        public String Name => _Template.Name;

        public Object Tag => _Template.Tag;

        /// <summary>
        /// Parent node.
        /// </summary>
        public NodeInstance VisualParent => _Parent;                

        /// <summary>
        /// Transform matrix in local space
        /// </summary>
        public XFORM LocalMatrix
        {
            get => _LocalMatrix;
            set
            {
                _LocalMatrix = value;
                _ModelMatrix = null;
            }
        }

        /// <summary>
        /// Transform matrix in world space
        /// </summary>
        public XFORM ModelMatrix
        {
            get => _GetModelMatrix();
            set => _SetModelMatrix(value);
        }

        // public SPARSE8 MorphWeights { get => _MorphWeights; set => _MorphWeights = value; }

        /// <summary>
        /// Gets a value indicating whether any of the transforms down the node tree graph has been modified.
        /// </summary>
        /// <remarks>
        /// When this is true, we need to recalculate our <see cref="ModelMatrix"/>.
        /// </remarks>
        private bool _TransformChainIsDirty
        {
            get
            {
                if (!_ModelMatrix.HasValue) return true;

                return _Parent == null ? false : _Parent._TransformChainIsDirty;
            }
        }

        #endregion

        #region API

        private XFORM _GetModelMatrix()
        {
            if (!_TransformChainIsDirty) return _ModelMatrix.Value;

            _ModelMatrix = _Parent == null ? _LocalMatrix : XFORM.Multiply(_LocalMatrix, _Parent.ModelMatrix);

            return _ModelMatrix.Value;
        }

        private void _SetModelMatrix(XFORM xform)
        {
            if (_Parent == null) { LocalMatrix = xform; return; }

            var pxform = _Parent._GetModelMatrix();
            XFORM.Invert(ref pxform, out XFORM ipwm);

            LocalMatrix = XFORM.Multiply(xform, ipwm);
        }

        public void SetPoseTransform() { SetAnimationFrame(-1, 0); }

        public void SetAnimationFrame(int trackLogicalIndex, float time)
        {            
            this.LocalMatrix = _Template.GetLocalMatrix(trackLogicalIndex, time);

            // this.MorphWeights = _Template.GetMorphWeights(trackLogicalIndex, time);
        }

        public void SetAnimationFrame(ReadOnlySpan<int> track, ReadOnlySpan<float> time, ReadOnlySpan<float> weight)
        {            
            this.LocalMatrix = _Template.GetLocalMatrix(track, time, weight);

            // this.MorphWeights = _Template.GetMorphWeights(track, time, weight);
        }

        #endregion
    }
}
