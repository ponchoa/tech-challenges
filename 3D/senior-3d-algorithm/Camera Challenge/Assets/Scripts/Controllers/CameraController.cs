using IWD.Mechanics;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace IWD.Controllers
{
    public class CameraController : MonoBehaviour
    {
        #region Singleton
        static CameraController instance;
        public static CameraController Instance
        {
            get
            {
                if (instance is null)
                    instance = FindObjectOfType<CameraController>();
                return instance;
            }
        }

        void MakeInstance()
        {
            instance = this;
        }
        #endregion

        [SerializeField]
        [Tooltip("The camera that will be controlled. (Main camera by default)")]
        Camera controlledCamera;
        [SerializeField]
        [Tooltip("The speed of the camera, used both for repositioning and zooming.")]
        float cameraSpeed = 1f;

        HashSet<CameraTarget> targetSet;
        Vector3 cameraVelocity;
        Vector3 cameraTargetPosition;
        GameObject cameraContainer;

        private void Awake()
        {
            MakeInstance();
            InitializeValues();
        }

        private void LateUpdate()
        {
            UpdateCameraPosition();
        }

        void InitializeValues()
        {
            targetSet = new HashSet<CameraTarget>();
            if (controlledCamera is null)
                controlledCamera = Camera.main;

            // We set up a container for the camera to help with zooming.
            cameraContainer = new GameObject("Camera Container");
            cameraContainer.transform.position = controlledCamera.transform.position;
            cameraContainer.transform.rotation = controlledCamera.transform.rotation;
            cameraContainer.transform.parent = controlledCamera.transform.parent;
            controlledCamera.transform.parent = cameraContainer.transform;
        }

        #region Position and Zoom
        void UpdateCameraPosition()
        {
            FindTargetPosition();
            cameraContainer.transform.position = 
                Vector3.SmoothDamp(cameraContainer.transform.position, cameraTargetPosition, ref cameraVelocity, 1f / cameraSpeed);
            UpdateCameraZoom();
        }

        void FindTargetPosition()
        {
            Bounds targetsBounds = new Bounds();
            foreach (CameraTarget target in targetSet)
            {
                targetsBounds.Encapsulate(target.transform.position);
            }
            cameraTargetPosition = targetsBounds.center;
        }

        void UpdateCameraZoom()
        {
            if (IsEveryTargetInFrustum())
            {
                controlledCamera.transform.localPosition += new Vector3(0f, 0f, Time.deltaTime * cameraSpeed * 3f);
            }
            if (!IsEveryTargetInFrustum())
            {
                controlledCamera.transform.localPosition -= new Vector3(0f, 0f, Time.deltaTime * cameraSpeed * 3f);
            }
        }

        bool IsEveryTargetInFrustum()
        {
            foreach (CameraTarget target in targetSet)
            {
                Plane[] frustumPlanes = GeometryUtility.CalculateFrustumPlanes(controlledCamera);

                if (target.GetRenderer() is null)
                    continue;

                Bounds rendererBounds = target.GetRenderer().bounds;
                Vector3 boundsSize = rendererBounds.size;
                Vector3 boundsMin = rendererBounds.min;

                // The 8 corners of the bounding box.
                List<Vector3> boundsCorners = new List<Vector3>(8)
                {
                     boundsMin,
                     boundsMin + new Vector3(0f, 0f, boundsSize.z),
                     boundsMin + new Vector3(boundsSize.x, 0f, boundsSize.z),
                     boundsMin + new Vector3(boundsSize.x, 0f, 0f),
                     boundsMin + new Vector3(0f, boundsSize.y, 0f),
                     boundsMin + new Vector3(0f, boundsSize.y, boundsSize.z),
                     boundsMin + new Vector3(boundsSize.x, boundsSize.y, 0f),
                     boundsMin + new Vector3(boundsSize.x, boundsSize.y, boundsSize.z)
                };


                // For each plane of the frustrum, we check if the corner is inside or outside.
                // If it is outside, then the object is cropped.
                for (int p = 0; p < frustumPlanes.Length; p++)
                {
                    for (int i = 0; i < boundsCorners.Count; i++)
                    {
                        if (!frustumPlanes[p].GetSide(boundsCorners[i]))
                            return false;
                    }
                }
            }
            return true;
        }
        #endregion

        #region Targets Controls
        /// <summary>
        /// Adds a target that the controlled camera has to include in its frustrum.
        /// </summary>
        /// <param name="target">The target to add.</param>
        public void AddTarget(CameraTarget target)
        {
            targetSet.Add(target);
        }
        /// <summary>
        /// Removes a target from the controlled camera's tracking.
        /// </summary>
        /// <param name="target">The target to remove.</param>
        public void RemoveTarget(CameraTarget target)
        {
            if (targetSet.Contains(target))
                targetSet.Remove(target);
        }
        #endregion
    }
}

