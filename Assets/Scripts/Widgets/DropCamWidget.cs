// Copyright 2020 The Tilt Brush Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using TMPro;
using UnityEngine;
using System.Collections;

namespace TiltBrush
{

    [System.Serializable]
    public class FrustumBeam
    {
        public Transform m_Beam;
        public Renderer m_BeamMesh;
        [NonSerialized] public Vector3 m_BaseScale;
    }

    public class DropCamWidget : GrabWidget
    {
        public enum Mode
        {
            SlowFollow,
            Stationary,
            Wobble,
            Circular
        }

        public const Mode kDefaultMode = Mode.Circular;

        [SerializeField] private TextMeshPro m_TitleText;
        [SerializeField] private GameObject m_HintText;
        [SerializeField] private FrustumBeam[] m_FrustumBeams;
        [SerializeField] private GameObject m_Scene;

        [SerializeField] private Transform m_GhostMesh;

        [Header("Wobble Mode")]
        [SerializeField] private float m_WobbleSpeed;
        [SerializeField] private float m_WobbleScale;

        [Header("Circle Mode")]
        [SerializeField] public bool m_CircleAuto = true;
        [SerializeField] public GameObject m_CircleTarget;
        [SerializeField] public GameObject m_CircleRadiusTarget;
        [SerializeField] public float m_CircleSpeed;
        [SerializeField] public float m_CircleTimer = 0.25f;
        [SerializeField] public float m_CircleRadius;
        [Range(0f, 12f)]
        [SerializeField] public float m_YCircleRadius;
        [SerializeField] public float m_YCircleRange = 0.25f;
        [SerializeField] public float m_DefaultCircleRadius;
        [SerializeField] public float m_CircleZoom;
        [SerializeField] public float m_CircleSmooth = 0.125f;
        [SerializeField] public bool m_CircleReady = true;
        [SerializeField] private GameObject m_GuideCircleObject;


        [Header("Slow Follow Mode")]
        [SerializeField] private float m_SlowFollowSmoothing;

        private float m_GuideBeamShowRatio;
        private Renderer[] m_Renderers;

        private Mode m_CurrentMode;

        private Vector3 m_vWobbleBase_RS;
        private Vector3 m_vCircleBase_RS;

        private float m_AnimatedPathTime;
        private float m_CircleRadians;


        private Vector3 m_SlowFollowMoveVel;
        private Vector3 m_SlowFollowRotVel;

        private void OnEnable()
        {
            StartCoroutine(Randomizercirculartruefalse());
        }

        override protected void Awake()
        {
            base.Awake();
            m_Renderers = GetComponentsInChildren<Renderer>();

            //initialize beams
            for (int i = 0; i < m_FrustumBeams.Length; ++i)
            {
                //cache scale and set to zero to prep for first time use
                m_FrustumBeams[i].m_BaseScale = m_FrustumBeams[i].m_Beam.localScale;
                m_FrustumBeams[i].m_Beam.localScale = Vector3.zero;
            }

            m_GuideBeamShowRatio = 0.0f;
            m_CurrentMode = kDefaultMode;
            ResetCam();

            // Register the drop camera with scene settings
            Camera camera = GetComponentInChildren<Camera>();
            SceneSettings.m_Instance.RegisterCamera(camera);

            InitSnapGhost(m_GhostMesh, transform);
        }

        protected override TrTransform GetDesiredTransform(TrTransform xf_GS)
        {
            if (SnapEnabled)
            {
                return GetSnappedTransform(xf_GS);
            }
            return xf_GS;
        }

        protected override TrTransform GetSnappedTransform(TrTransform xf_GS)
        {
            TrTransform outXf_GS = xf_GS;

            Vector3 forward = xf_GS.rotation * Vector3.forward;
            forward.y = 0;
            outXf_GS.rotation = Quaternion.LookRotation(forward);

            Vector3 grabSpot = InputManager.m_Instance.GetControllerPosition(m_InteractingController);
            Vector3 grabToCenter = xf_GS.translation - grabSpot;
            outXf_GS.translation = grabSpot +
                grabToCenter.magnitude * (grabToCenter.y > 0 ? Vector3.up : Vector3.down);

            return outXf_GS;
        }

        override public void Show(bool bShow, bool bPlayAudio = true)
        {
            base.Show(bShow, bPlayAudio);

            RefreshRenderers();
        }

        public double GetYCenterFromScaleFactor(float scalefactor)
        {
            //return 0.91234 * Math.Log(7.92169* scalefactor - 1.38873) + 9.34827;
            //faire un slider
            return 0.91234 * Math.Log(7.92169 * scalefactor - 1.38873) + 9.34827;
        }

        override protected void OnShow()
        {
            base.OnShow();

            TrTransform xfSpawn = TrTransform.FromTransform(ViewpointScript.Head);
            InitIntroAnim(xfSpawn, xfSpawn, true);
            m_IntroAnimState = IntroAnimState.In;
            m_IntroAnimValue = 0.0f;
        }

        override protected void UpdateIntroAnimState()
        {
            IntroAnimState prevState = m_IntroAnimState;
            base.UpdateIntroAnimState();

            // If we're exiting the in state, notify our panel.
            if (prevState != m_IntroAnimState)
            {
                if (m_IntroAnimState == IntroAnimState.On)
                {
                    ResetCam();
                }
            }
        }

        static public string GetModeName(Mode mode)
        {
            switch (mode)
            {
                case Mode.SlowFollow: return "Head Camera";
                case Mode.Stationary: return "Stationary";
                case Mode.Wobble: return "Figure 8";
                case Mode.Circular: return "Circular";
            }
            return "";
        }
        /*
        void ResetCamold()
        {

            // Reset wobble cam
            m_AnimatedPathTime = (float)(0.5 * Math.PI);
            m_vWobbleBase_RS = Coords.AsRoom[transform].translation;

            // Figure out which way points in for circle cam.
            Vector3 vInwards = transform.forward;
            vInwards.y = 0;
            vInwards.Normalize();

            // Set the center of the circle that we rotate around.
            m_vCircleBase_RS = transform.position - m_DefaultCircleRadius * vInwards;
            m_CircleRadians = (float)Math.Atan2(-vInwards.z, -vInwards.x);

            // Set the initial orientation for circle cam.
            Quaternion qCamOrient = transform.rotation;
            Vector3 eulers = new Vector3(0, (float)(m_CircleRadians * Mathf.Rad2Deg), 0);
            m_CircleOrientation = Quaternion.Euler(eulers) * qCamOrient;

            // Set the initial orientation for circle cam.
            Vector3 eieulers = new Vector3(0, 0, 5);

            // Position the guide circle.
            m_GuideCircleObject.transform.localPosition = Quaternion.Inverse(transform.rotation) * Quaternion.Euler(0, (float)(-m_CircleRadians * Mathf.Rad2Deg), 0) * new Vector3(-m_DefaultCircleRadius, 0, 0);
            m_GuideCircleObject.transform.localRotation = Quaternion.Inverse(transform.rotation) * Quaternion.Euler(0, (float)(-m_CircleRadians * Mathf.Rad2Deg), 0);
            m_GuideCircleObject.transform.localScale = 2.0f * m_DefaultCircleRadius * Vector3.one;

            // On slow follow reset, snap to head.
            if (m_CurrentMode == Mode.SlowFollow)
            {
                transform.position = ViewpointScript.Head.position;
            }

        }
        */
        void ResetCam()
        {

            // Reset wobble cam
            m_AnimatedPathTime = (float)(0.5 * Math.PI);
            m_vWobbleBase_RS = Coords.AsRoom[transform].translation;

            // Figure out which way points in for circle cam.
            Vector3 vInwards = m_CircleTarget.transform.position - ViewpointScript.Head.position;
            vInwards.y = 0;
            vInwards.Normalize();

            // Set the center of the circle that we rotate around.
            m_vCircleBase_RS = m_CircleTarget.transform.position;
            m_CircleRadians = (float)Math.Atan2(-vInwards.z, -vInwards.x);

            // Set the initial orientation for circle cam.
            Quaternion qCamOrient = transform.rotation;
            Vector3 eulers = new Vector3(0, (float)(m_CircleRadians * Mathf.Rad2Deg), 0);

            // Position the guide circle.
            m_GuideCircleObject.transform.localPosition = Quaternion.Inverse(transform.rotation) * Quaternion.Euler(0, (float)(-m_CircleRadians * Mathf.Rad2Deg), 0) * new Vector3(-m_DefaultCircleRadius, 0, 0);
            m_GuideCircleObject.transform.localRotation = Quaternion.Inverse(transform.rotation) * Quaternion.Euler(0, (float)(-m_CircleRadians * Mathf.Rad2Deg), 0);
            m_GuideCircleObject.transform.localScale = 2.0f * m_DefaultCircleRadius * Vector3.one;

            // On slow follow reset, snap to head.
            if (m_CurrentMode == Mode.SlowFollow)
            {
                transform.position = ViewpointScript.Head.position;
            }

        }

        override protected void OnUpdate()
        {
            //Set center Y position
            Vector3 centery = m_CircleTarget.transform.localPosition;
            centery.y = (float)GetYCenterFromScaleFactor(m_Scene.transform.localScale.magnitude);
            m_CircleTarget.transform.localPosition = centery;

#if (UNITY_EDITOR || EXPERIMENTAL_ENABLED)
            if (Debug.isDebugBuild && Config.IsExperimental)
            {
                if (InputManager.m_Instance.GetKeyboardShortcutDown(
                    InputManager.KeyboardShortcut.ToggleHeadStationaryOrWobble))
                {
                    m_CurrentMode = (m_CurrentMode == Mode.Wobble) ? Mode.Stationary : Mode.Wobble;
                    RefreshRenderers();
                }
                else if (InputManager.m_Instance.GetKeyboardShortcutDown(
                    InputManager.KeyboardShortcut.ToggleHeadStationaryOrFollow))
                {
                    m_CurrentMode = (m_CurrentMode == Mode.SlowFollow) ? Mode.Stationary : Mode.SlowFollow;
                    RefreshRenderers();
                }
            }
#endif

            //animate the guide beams in and out, relatively to activation
            float fShowRatio = GetShowRatio();

            //if our transform changed, update the beams
            if (m_GuideBeamShowRatio != fShowRatio)
            {
                for (int i = 0; i < m_FrustumBeams.Length; ++i)
                {
                    //update scale
                    Vector3 vScale = m_FrustumBeams[i].m_BaseScale;
                    vScale.z *= fShowRatio;
                    m_FrustumBeams[i].m_Beam.localScale = vScale;
                }
            }
            m_GuideBeamShowRatio = fShowRatio;

            if (m_GuideBeamShowRatio >= 1.0f)
            {
                switch (m_CurrentMode)
                {
                    case Mode.Wobble:
                        if (m_UserInteracting)
                        {
                            ResetCam();
                        }
                        else
                        {
                            m_AnimatedPathTime += Time.deltaTime * m_WobbleSpeed;
                            Vector3 vWidgetPos = m_vWobbleBase_RS;

                            //sideways figure 8, or infinity symbol path
                            float fCosTime = Mathf.Cos(m_AnimatedPathTime);
                            float fSinTime = Mathf.Sin(m_AnimatedPathTime);
                            float fSqrt2 = Mathf.Sqrt(2.0f);
                            float fDenom = fSinTime * fSinTime + 1.0f;

                            float fX = (m_WobbleScale * fSqrt2 * fCosTime) / fDenom;
                            float fY = (m_WobbleScale * fSqrt2 * fCosTime * fSinTime) / fDenom;

                            vWidgetPos += transform.right * fX * m_WobbleScale;
                            vWidgetPos += transform.up * fY * m_WobbleScale;
                            transform.position = vWidgetPos;
                        }
                        break;
                    case Mode.SlowFollow:
                        {
#if (UNITY_EDITOR || EXPERIMENTAL_ENABLED)
                            if (Debug.isDebugBuild && Config.IsExperimental)
                            {
                                if (InputManager.m_Instance.GetKeyboardShortcutDown(
                                    InputManager.KeyboardShortcut.DecreaseSlowFollowSmoothing))
                                {
                                    m_SlowFollowSmoothing -= 0.001f;
                                    m_SlowFollowSmoothing = Mathf.Max(m_SlowFollowSmoothing, 0.0f);
                                }
                                else if (InputManager.m_Instance.GetKeyboardShortcutDown(
                                    InputManager.KeyboardShortcut.IncreaseSlowFollowSmoothing))
                                {
                                    m_SlowFollowSmoothing += 0.001f;
                                }
                            }
#endif
                            transform.position = Vector3.SmoothDamp(transform.position, ViewpointScript.Head.position,
                                ref m_SlowFollowMoveVel, m_SlowFollowSmoothing, Mathf.Infinity, Time.deltaTime);

                            Vector3 eulers = transform.rotation.eulerAngles;
                            Vector3 targetEulers = ViewpointScript.Head.eulerAngles;

                            eulers.x = Mathf.SmoothDampAngle(eulers.x, targetEulers.x,
                                ref m_SlowFollowRotVel.x, m_SlowFollowSmoothing, Mathf.Infinity, Time.deltaTime);
                            eulers.y = Mathf.SmoothDampAngle(eulers.y, targetEulers.y,
                                ref m_SlowFollowRotVel.y, m_SlowFollowSmoothing, Mathf.Infinity, Time.deltaTime);
                            eulers.z = Mathf.SmoothDampAngle(eulers.z, targetEulers.z,
                                ref m_SlowFollowRotVel.z, m_SlowFollowSmoothing, Mathf.Infinity, Time.deltaTime);

                            transform.rotation = Quaternion.Euler(eulers);

                            //Vector3 eulers = new Vector3(0, (float)(-m_CircleRadians * Mathf.Rad2Deg), 0);
                            //transform.rotation = Quaternion.Euler(eulers) * m_CircleOrientation;


                        }
                        break;
                    case Mode.Circular:
                        if (m_UserInteracting)
                        {
                            ResetCam();
                            if (!m_GuideCircleObject.activeSelf)
                            {
                                m_GuideCircleObject.GetComponentInChildren<MeshRenderer>().material
                                    .SetFloat("_RevealStartTime", Time.time);
                                m_GuideCircleObject.SetActive(true);
                            }
                        }
                        else
                        {
                            if (Input.GetKey(KeyCode.KeypadMinus) && !Input.GetKey(KeyCode.Keypad4) && !Input.GetKey(KeyCode.Keypad6))
                            {
                                m_CircleRadius = m_CircleRadius + m_CircleZoom;
                                Debug.Log("Zoom");
                            }
                            else if (Input.GetKey(KeyCode.KeypadPlus) && m_CircleRadius >= 0 && !Input.GetKey(KeyCode.Keypad4) && !Input.GetKey(KeyCode.Keypad6))
                            {
                                m_CircleRadius = m_CircleRadius - m_CircleZoom;
                                Debug.Log("Dezoom");
                            }

                            if (Input.GetKey(KeyCode.Space) && m_CircleAuto == false)
                            {
                                m_CircleAuto = true;
                            }
                            else if (Input.GetKey(KeyCode.Space) && m_CircleAuto == true)
                            {
                                m_CircleAuto = false;
                            }

                            if (Input.GetKey(KeyCode.Keypad4) && !Input.GetKey(KeyCode.KeypadPlus) && !Input.GetKey(KeyCode.KeypadMinus))
                            {
                                m_CircleRadians -= (float)(Time.deltaTime * m_CircleSpeed * 2 * Math.PI);
                                Debug.Log("Left");
                                m_CircleAuto = false;
                            }
                            else if (Input.GetKey(KeyCode.Keypad6) && !Input.GetKey(KeyCode.KeypadPlus) && !Input.GetKey(KeyCode.KeypadMinus))
                            {
                                m_CircleRadians += (float)(Time.deltaTime * m_CircleSpeed * 2 * Math.PI);
                                Debug.Log("Right");
                                m_CircleAuto = false;
                            }

                            if (Input.GetKey(KeyCode.Keypad8))
                            {
                                if (m_YCircleRadius < 30f)
                                {
                                    m_YCircleRadius += m_YCircleRange;
                                }
                            }
                            else if (Input.GetKey(KeyCode.Keypad2))
                            {
                                if (m_YCircleRadius > 0f)
                                {
                                    m_YCircleRadius -= m_YCircleRange;
                                }
                            }

                            // Set the camera position.
                            Vector3 vWidgetPos = m_vCircleBase_RS;
                            vWidgetPos[0] += m_CircleRadius * (float)Math.Cos(m_CircleRadians);
                            vWidgetPos[2] += m_CircleRadius * (float)Math.Sin(m_CircleRadians);
                            Vector3 smoothedPosition = Vector3.LerpUnclamped(transform.position, vWidgetPos, m_CircleSmooth * Time.deltaTime);
                            transform.position = smoothedPosition;

                            // Set the camera orientation.
                            transform.LookAt(m_CircleTarget.transform, Vector3.up);

                            // Deactivate the guide circle.
                            m_GuideCircleObject.SetActive(false);

                            // Camera Second Rotation
                            transform.RotateAround(m_CircleTarget.transform.position, transform.right, m_YCircleRadius * Time.deltaTime);
                        }
                        break;
                }
            }
        }

        IEnumerator Randomizercirculartruefalse()
        {
            while (true)
            {
                if (m_CircleReady == true && m_CircleAuto == true)
                {
                    switch (UnityEngine.Random.Range(1, 3))
                    {
                        case 1:
                            for (int i = 0; i < UnityEngine.Random.Range(100, 240); i++)
                            {
                                m_CircleRadians += (float)(Time.deltaTime * m_CircleSpeed * 2 * Math.PI);
                                //transform.position = new Vector3(0,0, Mathf.Lerp(m_after, m_CircleRadians, m_CircleSmooth));
                                //Debug.Log("1" + i++);
                            }
                            //Debug.Log("1");
                            m_CircleReady = false;
                            break;
                        case 2:
                            for (int i = 0; i < UnityEngine.Random.Range(100, 240); i++)
                            {
                                m_CircleRadians -= (float)(Time.deltaTime * m_CircleSpeed * 2 * Math.PI);
                                //transform.position = new Vector3(0, 0, Mathf.Lerp(m_after, m_CircleRadians, m_CircleSmooth));
                                //Debug.Log("2" + i++);
                            }
                            //Debug.Log("2");
                            m_CircleReady = false;
                            break;
                    }

                    switch (UnityEngine.Random.Range(1, 3))
                    {
                        case 1:
                            if (m_CircleRadius > 11f)
                            {
                                for (int i = 0; i < UnityEngine.Random.Range(1, 2); i++)
                                {
                                    m_CircleRadius = m_CircleRadius - 10f;
                                    Debug.Log("3" + i++);
                                }
                                //Debug.Log("3");
                                m_CircleReady = false;
                            }
                            break;
                        case 2:
                            if (m_CircleRadius < 20f)
                            {
                                for (int i = 0; i < UnityEngine.Random.Range(1, 2); i++)
                                {
                                    m_CircleRadius = m_CircleRadius + 10f;
                                    Debug.Log("4" + i++);
                                }
                                //Debug.Log("4");
                                m_CircleReady = false;
                            }
                            break;
                    }

                    switch (UnityEngine.Random.Range(1, 3))
                    {
                        case 1:
                            if (m_YCircleRadius > 0f)
                            {
                                m_YCircleRadius -= UnityEngine.Random.Range(1, 5);
                            }
                            break;
                        case 2:
                            if (m_YCircleRadius < 12f)
                            {
                                m_YCircleRadius += UnityEngine.Random.Range(1, 5);
                            }
                            break;
                    }
                }
                yield return new WaitForSeconds(m_CircleTimer);
                // process post-yield
                m_CircleReady = true;
            }
        }

        override public void Activate(bool bActive)
        {
            base.Activate(bActive);
            Color activeColor = bActive ? Color.white : Color.grey;
            m_TitleText.color = activeColor;
            m_HintText.SetActive(bActive);

            for (int i = 0; i < m_FrustumBeams.Length; ++i)
            {
                m_FrustumBeams[i].m_BeamMesh.material.color = activeColor;
            }
        }

        public void SetMode(Mode newMode)
        {
            m_CurrentMode = newMode;
            RefreshRenderers();
            ResetCam();
        }

        public Mode GetMode()
        {
            return m_CurrentMode;
        }

        void RefreshRenderers()
        {
            // Show the widget beams if we're not in slow follow mode, and we're active.
            bool bShow = ShouldHmdBeVisible();
            for (int i = 0; i < m_Renderers.Length; ++i)
            {
                m_Renderers[i].enabled = bShow;
            }
        }

        public bool ShouldHmdBeVisible()
        {
            return (m_CurrentMode != Mode.SlowFollow) &&
                (m_CurrentState == State.Showing || m_CurrentState == State.Visible);
        }
    }
} // namespace TiltBrush