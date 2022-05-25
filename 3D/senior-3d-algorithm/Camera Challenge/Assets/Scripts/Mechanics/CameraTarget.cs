using IWD.Controllers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace IWD.Mechanics
{
    [RequireComponent(typeof(Renderer))]
    public class CameraTarget : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("The renderer attached to this object.")]
        Renderer targetRenderer;

        private void Start()
        {
            if (targetRenderer is null)
                targetRenderer = GetComponent<Renderer>();
            CameraController.Instance.AddTarget(this);
        }

        private void OnDisable()
        {
            CameraController.Instance.RemoveTarget(this);
        }

        /// <summary>
        /// Returns the Renderer component attached to this object.
        /// </summary>
        /// <returns>The renderer attached.</returns>
        public Renderer GetRenderer()
        {
            if (targetRenderer is null)
                targetRenderer = GetComponent<Renderer>();
            return targetRenderer;
        }
    }
}

