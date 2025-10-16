// Copyright (c) Meta Platforms, Inc. and affiliates.

// MODIFIED BY DANIEL HE

using System;
using UnityEngine;
using System.Collections.Generic;
using Fusion;
using Meta.XR.Movement;
using Assert = UnityEngine.Assertions.Assert;

namespace Oculus.Movement.Effects
{
    /// <summary>
    /// Mirrors an object by copying its local transformation values.
    /// </summary>
    [DefaultExecutionOrder(300)]
    public class MirrorObject : NetworkBehaviour
    {
        /// <summary>
        /// Contains information about a mirrored transform pair.
        /// </summary>
        [System.Serializable]
        public class MirroredTransformPair
        {
            /// <summary>
            /// The name of the mirrored transform pair.
            /// </summary>
            [HideInInspector] public string Name;

            /// <summary>
            /// The original transform.
            /// </summary>
            [Tooltip(LateMirroredObjectTooltips.MirroredTransformPairTooltips.OriginalTransform)]
            public Transform OriginalTransform;

            /// <summary>
            /// The mirrored transform.
            /// </summary>
            [Tooltip(LateMirroredObjectTooltips.MirroredTransformPairTooltips.MirroredTransform)]
            public Transform MirroredTransform;
        }

        /// <summary>
        /// Returns the original transform.
        /// </summary>
        public Transform OriginalTransform => _transformToCopy;

        /// <summary>
        /// Returns the mirrored transform.
        /// </summary>
        public Transform MirroredTransform => _myTransform;

        /// <summary>
        /// The transform which transform values are being mirrored from.
        /// </summary>
        [SerializeField]
        [Tooltip(LateMirroredObjectTooltips.TransformToCopy)]
        public Transform _transformToCopy;

        /// <summary>
        /// The target transform which transform values are being mirrored to.
        /// </summary>
        [SerializeField]
        [Tooltip(LateMirroredObjectTooltips.MyTransform)]
        public Transform _myTransform;

        /// <summary>
        /// The array of mirrored transform pairs.
        /// </summary>
        [SerializeField]
        [Tooltip(LateMirroredObjectTooltips.MirroredTransformPairs)]
        public MirroredTransformPair[] _mirroredTransformPairs;

        public List<Transform> _mirroredTransformList;

        /// <summary>
        /// Mirror scale.
        /// </summary>
        [SerializeField]
        [Tooltip(LateMirroredObjectTooltips.MirrorScale)]
        protected bool _mirrorScale = false;

        private void Awake()
        {
            // _transformToCopy = FindObjectOfType<GameManager>()._OriginalTransform;

            PopulateMirror();
            
            Assert.IsNotNull(_transformToCopy);
            Assert.IsNotNull(_myTransform);
            foreach (var mirroredTransformPair in _mirroredTransformPairs)
            {
                Assert.IsNotNull(mirroredTransformPair.OriginalTransform);
                Assert.IsNotNull(mirroredTransformPair.MirroredTransform);
            }
        }

        private void Start()
        {
            if (_transformToCopy == null)
            {
                // _transformToCopy = FindObjectOfType<GameManager>()._OriginalTransform;
            }
        }

        private void Update()
        {
            if (_transformToCopy == null)
            {
                // _transformToCopy = FindObjectOfType<GameManager>()._OriginalTransform;
            }
        }

        private void PopulateMirror()
        {
            var mirroredObject = (MirrorObject)this;
            var childTransforms = new List<Transform>(mirroredObject.MirroredTransform.GetComponentsInChildren<Transform>(true));
            childTransforms.Remove(transform);
            _mirroredTransformList = childTransforms;
            _mirroredTransformPairs = new MirroredTransformPair[childTransforms.Count];
            int i = 0;
            foreach (var transform in childTransforms)
            {
                var originalTransform =
                    mirroredObject.OriginalTransform.transform.FindChildRecursive(transform.name);
                if (originalTransform != null)
                {
                    MirroredTransformPair pair = new MirroredTransformPair();
                    pair.OriginalTransform = originalTransform;
                    pair.MirroredTransform = transform;
                    _mirroredTransformPairs[i] = pair;
                }
                else
                {
                    Debug.LogError($"Missing a mirrored transform for: {transform.name}");
                }
                i++;
            }
        }

        /// <summary>
        /// Mirror in late update.
        /// </summary>
        private void LateUpdate()
        {
            // RPC_Mirror();
            // _myTransform.localPosition = _transformToCopy.localPosition;
            // _myTransform.localRotation = _transformToCopy.localRotation;
            // if (_mirrorScale)
            // {
            //     _myTransform.localScale = _transformToCopy.localScale;
            // }
            // foreach (var transformPair in _mirroredTransformPairs)
            // {
            //     transformPair.MirroredTransform.localPosition = transformPair.OriginalTransform.localPosition;
            //     transformPair.MirroredTransform.localRotation = transformPair.OriginalTransform.localRotation;
            //     if (_mirrorScale)
            //     {
            //         transformPair.MirroredTransform.localScale = transformPair.OriginalTransform.localScale;
            //     }
            // }
        }
        
        [Rpc(RpcSources.InputAuthority, RpcTargets.All)] // the RPC is called by a client, and is executed on all clients
        public void RPC_Mirror()
        {
            _myTransform.localPosition = _transformToCopy.localPosition;
            _myTransform.localRotation = _transformToCopy.localRotation;
            if (_mirrorScale)
            {
                _myTransform.localScale = _transformToCopy.localScale;
            }
            foreach (var transformPair in _mirroredTransformPairs)
            {
                transformPair.MirroredTransform.localPosition = transformPair.OriginalTransform.localPosition;
                transformPair.MirroredTransform.localRotation = transformPair.OriginalTransform.localRotation;
                if (_mirrorScale)
                {
                    transformPair.MirroredTransform.localScale = transformPair.OriginalTransform.localScale;
                }
            }
        }
    }
}
