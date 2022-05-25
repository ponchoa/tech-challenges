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

        /// <summary>
        /// This float is used to adjust the speed so that it is similar between zooming and moving.
        /// </summary>
        const float SPEED_ADJUST = 5f;

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
            InitializeSet();
            InitializeCamera();
            InitializeCameraContainer();
        }

        private void LateUpdate()
        {
            UpdateCameraPosition();
            UpdateCameraZoom();
        }

        #region Initialization
        /// <summary>
        /// We initialize the HashSet to be used later.
        /// </summary>
        void InitializeSet()
        {
            targetSet = new HashSet<CameraTarget>();
        }
        /// <summary>
        /// If the camera was not set in the inspector, we use Camera.main.
        /// We also log a warning, as it might not be expected behaviour.
        /// </summary>
        void InitializeCamera()
        {
            if (controlledCamera is null)
            {
                Debug.LogWarning($"CameraController({name}) : No camera was set in the inspector, the main camera is used by default.", this);
                controlledCamera = Camera.main;
            }
        }
        /// <summary>
        /// We set up a container for the camera to help with zooming,
        /// as it will allow the camera to only move along its own Z-Axis regardless of its angle.
        /// </summary>
        void InitializeCameraContainer()
        {
            cameraContainer = new GameObject("Camera Container");
            cameraContainer.transform.position = controlledCamera.transform.position;
            cameraContainer.transform.rotation = controlledCamera.transform.rotation;
            cameraContainer.transform.parent = controlledCamera.transform.parent;
            controlledCamera.transform.parent = cameraContainer.transform;
        }
        #endregion

        #region Position
        /// <summary>
        /// Updates smoothly the position of the camera container to be in the center of all targets.
        /// </summary>
        void UpdateCameraPosition()
        {
            // I was debating whether or not to check if any target has moved using Transform.hasChanged,
            // but I wanted to do some performance checks, to see if using callbacks, iterating through
            // the set once before, or simply calculating the bounds every frame was better,
            // unfortunately I didn't have time, so I'm going on the assumption that adding an
            // Update to each target, or processing the HashSet twice per frame was less
            // optimized than simply recalculating bounds every frame.
            FindTargetPosition();
            cameraContainer.transform.position =
                Vector3.SmoothDamp(cameraContainer.transform.position, cameraTargetPosition, ref cameraVelocity, SPEED_ADJUST / cameraSpeed);
        }

        /// <summary>
        /// Finds the target position the container will try to reach.
        /// </summary>
        void FindTargetPosition()
        {
            Bounds targetsBounds = new Bounds();
            foreach (CameraTarget target in targetSet)
            {
                targetsBounds.Encapsulate(target.transform.position);
            }
            cameraTargetPosition = targetsBounds.center;
        }
        #endregion

        #region Zoom
        /// <summary>
        /// Zooms the camera out if any object is cropped, and in if it is possible.
        /// </summary>
        void UpdateCameraZoom()
        {
            if (IsEveryTargetInFrustum())
            {
                if (controlledCamera.orthographic)
                    controlledCamera.orthographicSize -= cameraSpeed * Time.deltaTime;
                controlledCamera.transform.localPosition += new Vector3(0f, 0f, Time.deltaTime * cameraSpeed * SPEED_ADJUST);
            }
            // No 'else', because if we zoomed in too much, we correct the position before rendering to avoid spazzing.
            if (!IsEveryTargetInFrustum())
            {
                if (controlledCamera.orthographic)
                    controlledCamera.orthographicSize += cameraSpeed * Time.deltaTime;
                controlledCamera.transform.localPosition -= new Vector3(0f, 0f, Time.deltaTime * cameraSpeed * SPEED_ADJUST);
            }
        }

        /// <summary>
        /// Checks if every CameraTarget in the targetSet is entierely contained within the frustum of the camera.
        /// </summary>
        /// <returns>A boolean that is True if every target is in the frustum.</returns>
        bool IsEveryTargetInFrustum()
        {
            foreach (CameraTarget target in targetSet)
            {
                Plane[] frustumPlanes = GeometryUtility.CalculateFrustumPlanes(controlledCamera);

                // Unfortunately, GeometryUtility.TestPlanesAABB() checks only if at least one
                // corner is in the planes, so we have to implement our own way.

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
        /// Adds a target that the controlled camera has to include in its frustum.
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

