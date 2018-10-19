using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace DragonBones
{
    [CustomEditor(typeof(UnityDragonBonesData))]
    public class UnityDragonBonesDataEditor : Editor {

        private PreviewRenderUtility m_PreviewUtility;
        Camera PreviewUtilityCamera {
            get {
                if (m_PreviewUtility == null) return null;

                #if UNITY_2017_1_OR_NEWER
                return m_PreviewUtility.camera;
                #else
                return m_PreviewUtility.m_Camera;
                #endif
            }
        }

        static readonly int SliderHash = "Slider".GetHashCode();

        float cameraOrthoGoal = 1;
        Vector3 cameraPositionGoal = new Vector3(0, 0, -10);
        double cameraAdjustEndFrame = 0;

        private UnityDragonBonesData _unityDragonbonesData;
        private UnityArmatureComponent _previewUnityArmatureComp;

        private int _armatureIndex;

         private List<string> _armatureNames;
         private List<string> _animationNames;

        private long _nowTime = 0;
        private float _frameRate = 1.0f / 24.0f;

        private Vector2 _pos = new Vector2();
        private bool _foldAnimation = true;
        private AnimationData _animData;

        void OnEnable(){
            if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }
            _unityDragonbonesData = target as UnityDragonBonesData;
            EditorApplication.update -= EditorUpdate;
            EditorApplication.update += EditorUpdate;
            this._nowTime = System.DateTime.Now.Ticks;
            EditorApplication.playmodeStateChanged += OnPlayModeChange;
            InitPreview();
        }

        void OnPlayModeChange(){
            if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Selection.activeObject = null;
            }
        }

        public override bool HasPreviewGUI()
        {
            return true;
        }

        /// 标题
        public override GUIContent GetPreviewTitle()
        {
            if (_previewUnityArmatureComp != null && !string.IsNullOrEmpty(_previewUnityArmatureComp.armatureName))
            {
                return new GUIContent(_previewUnityArmatureComp.armatureName);
            }
            else if (_unityDragonbonesData != null)
            {
                return new GUIContent(_unityDragonbonesData.dataName);
            }
            return new GUIContent("");
        }

        public override void OnPreviewSettings()
        {
            if (_previewUnityArmatureComp == null || _previewUnityArmatureComp.armature == null || _previewUnityArmatureComp.animation == null)
                return;
            //time scale
            const float SliderWidth = 150;
            const float SliderSnap = 0.25f;
            const float SliderMin = 0f;
            const float SliderMax = 2f;
            float timeScale = GUILayout.HorizontalSlider(_previewUnityArmatureComp.animation.timeScale, SliderMin, SliderMax, GUILayout.MaxWidth(SliderWidth));
            timeScale = Mathf.RoundToInt(timeScale/SliderSnap) * SliderSnap;
            _previewUnityArmatureComp.animation.timeScale = timeScale;
        }

        override public void OnInteractivePreviewGUI (Rect r, GUIStyle background) {
            base.OnInteractivePreviewGUI(r,background);

            if (_previewUnityArmatureComp == null || _previewUnityArmatureComp.armature == null)
                return;

            //change position
            Rect posRect = new Rect(r);
            posRect.y += 12;
            posRect.height = 34;
            posRect.width = 100;
            _pos = EditorGUI.Vector2Field(posRect, "", _pos);

            //draw animation progress
            if (_animData!=null)
            {
                AnimationState state = _previewUnityArmatureComp.animation.GetState(_previewUnityArmatureComp.animationName);
                float time = 0;
                if (state != null)
                {
                    time = state.currentTime;
//                    GUILayout.HorizontalSlider(state.currentTime, 0, _animData.duration);
                }
                posRect = new Rect(r);
                posRect.x = 10;
                posRect.y += r.height+2;
                posRect.height = 16;
                posRect.width /= 1.2f;
                EditorGUI.Slider(posRect,time, 0, _animData.duration);
            }
                
            MouseScroll(r);
        }

        override public void OnInspectorGUI () {
            base.OnInspectorGUI();
            if (_previewUnityArmatureComp == null || _unityDragonbonesData == null || _previewUnityArmatureComp.armature==null)
                return;
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            DrawArmatures();
            DrawAnimations();
            DrawSlots();
        }

        void DrawArmatures(){
            if (_armatureNames != null && _armatureNames.Count > 0)
            {
                var armatureIndex = EditorGUILayout.Popup("Armature", _armatureIndex, _armatureNames.ToArray());
                if (_armatureIndex != armatureIndex)
                {
                    _armatureIndex = armatureIndex;
                    var armatureName = _armatureNames[_armatureIndex];
                    _previewUnityArmatureComp.unityData = _unityDragonbonesData;
                    UnityEditor.ChangeArmatureData(_previewUnityArmatureComp, armatureName, _unityDragonbonesData.dataName);
                    UpdateParameters();
                    SetEnabledRecursive(_previewUnityArmatureComp.gameObject,false);
                }
            }
        }
        GUIStyle activePlayButtonStyle, idlePlayButtonStyle;
        void DrawAnimations(){
            if (_animationNames != null && _animationNames.Count > 0)
            {
                if (GUILayout.Button("Setup Pose", GUILayout.Width(105), GUILayout.Height(18))) {
                    if (_previewUnityArmatureComp != null)
                    {
                        var armatureName = _armatureNames[_armatureIndex];
                        _previewUnityArmatureComp.unityData = _unityDragonbonesData;
                        UnityEditor.ChangeArmatureData(_previewUnityArmatureComp, armatureName, _unityDragonbonesData.dataName);
                        UpdateParameters();
                        SetEnabledRecursive(_previewUnityArmatureComp.gameObject,false);
                    }
                }

                idlePlayButtonStyle = idlePlayButtonStyle ?? new GUIStyle(EditorStyles.miniButton);
                if (activePlayButtonStyle == null) {
                    activePlayButtonStyle = new GUIStyle(idlePlayButtonStyle);
                    activePlayButtonStyle.normal.textColor = Color.red;
                }
                if (activePlayButtonStyle == null) {
                    activePlayButtonStyle = new GUIStyle(idlePlayButtonStyle);
                    activePlayButtonStyle.normal.textColor = Color.red;
                }
                _foldAnimation = EditorGUILayout.Foldout(_foldAnimation,"Animations");
                if (_foldAnimation)
                {
                    EditorGUILayout.LabelField("Name", "      Duration");
                    foreach (string animation in _animationNames)
                    {
                        using (new GUILayout.HorizontalScope())
                        {
                            AnimationData animData = _previewUnityArmatureComp.armature.armatureData.GetAnimation(animation);
                            bool active = _previewUnityArmatureComp.animationName == animation;
                            if (GUILayout.Button("\u25BA", active ? activePlayButtonStyle : idlePlayButtonStyle, GUILayout.Width(24)))
                            {
                                this._animData = animData;
                                _previewUnityArmatureComp.animationName = animation;
                                _previewUnityArmatureComp.animation.Play(animation);
                            }
                            string frameCountString = (_frameRate > 0) ? ("(" + (Mathf.RoundToInt(animData.duration/_frameRate)) + ")").PadLeft(12, ' ') : string.Empty;
                            EditorGUILayout.LabelField(new GUIContent(animation), new GUIContent(animData.duration.ToString("f3") + "s" + frameCountString));
                        }
                    }
                }
            }
        }

        void DrawSlots(){
            
        }

        void UpdateParameters(){
            if (_previewUnityArmatureComp != null && _previewUnityArmatureComp.armature != null)
            {
                _frameRate = 1.0f / (float)_previewUnityArmatureComp.armature.armatureData.frameRate;

                if (_previewUnityArmatureComp.armature.armatureData.parent != null)
                {
                    _armatureNames = _previewUnityArmatureComp.armature.armatureData.parent.armatureNames;
                    _animationNames = _previewUnityArmatureComp.animation.animationNames;
                    _armatureIndex = _armatureNames.IndexOf(_previewUnityArmatureComp.armature.name);
                }
                else
                {
                    _armatureNames = null;
                    _animationNames = null;
                    _armatureIndex = 0;
                }
            }
            else
            {
                _armatureNames = null;
                _animationNames = null;
                _armatureIndex = 0;
            }
        }

        private void InitPreview()
        {
            CreatePreviewUtility();
            // 创建预览的游戏对象
            CreatePreviewInstances();
        }
        private void CreatePreviewUtility(){
            if (m_PreviewUtility == null)
            {
                const int previewLayer = 31;
                const int previewCameraCullingMask = 1 << previewLayer;

                // 参数true代表绘制场景内的游戏对象
                m_PreviewUtility = new PreviewRenderUtility(true);
                // 设置摄像机的一些参数
                var c = this.PreviewUtilityCamera;
                c.orthographic = true;
                c.orthographicSize = 1;
                c.cullingMask = previewCameraCullingMask;
                c.nearClipPlane = 0.01f;
                c.farClipPlane = 1000f; 
            }
        }

        private void DestroyPreview()
        {
            if (m_PreviewUtility != null)
            {
                m_PreviewUtility.Cleanup();
                m_PreviewUtility = null;
            }
        }

        private void CreatePreviewInstances()
        {
            if (_previewUnityArmatureComp != null)
                return;
            // 绘制场景上已经存在的游戏对象
            DragonBonesData dragonBonesData = UnityFactory.factory.LoadData(_unityDragonbonesData,false);
           
            _previewUnityArmatureComp = UnityFactory.factory.BuildArmatureComponent(dragonBonesData.armatureNames[0],_unityDragonbonesData.dataName);
            if(_previewUnityArmatureComp!=null){
                _previewUnityArmatureComp.gameObject.hideFlags = HideFlags.HideAndDontSave;
                SetEnabledRecursive(_previewUnityArmatureComp.gameObject,false);
                UpdateParameters();

                AdjustCameraGoals(true);
            }
        }

        private void DestroyPreviewInstances()
        {
            if (_previewUnityArmatureComp != null)
            {
                _previewUnityArmatureComp.Dispose(false);
                DestroyImmediate(_previewUnityArmatureComp.gameObject);
                _previewUnityArmatureComp = null;
            }
            m_PreviewUtility = null;
        }

        void OnDestroy()
        {
            EditorApplication.update -= EditorUpdate;
            EditorApplication.playmodeStateChanged -= OnPlayModeChange;
            DestroyPreviewInstances();
            DestroyPreview();
        }

        void SetEnabledRecursive(GameObject go, bool enabled)
        {
            const int previewLayer = 31;
            Renderer[] componentsInChildren = go.GetComponentsInChildren<Renderer>();
            for (int i = 0; i < componentsInChildren.Length; i++)
            {
                Renderer renderer = componentsInChildren[i];
                renderer.gameObject.layer = previewLayer;
                renderer.enabled = enabled;
            }
        }

        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }
            InitPreview();
            if (m_PreviewUtility == null || _previewUnityArmatureComp == null || Event.current.type != EventType.Repaint)
            {
                return;
            }
            SetEnabledRecursive(_previewUnityArmatureComp.gameObject,true);
            m_PreviewUtility.BeginPreview(r, background);
            PreviewUtilityCamera.Render();
            m_PreviewUtility.EndAndDrawPreview(r);
            SetEnabledRecursive(_previewUnityArmatureComp.gameObject,false);
        }

        public void AdjustCamera () {
            if (m_PreviewUtility == null || _previewUnityArmatureComp == null)
                return;

            if (EditorApplication.timeSinceStartup < cameraAdjustEndFrame)
                AdjustCameraGoals();

            var c = this.PreviewUtilityCamera;
            float orthoSet = Mathf.Lerp(c.orthographicSize, cameraOrthoGoal, 0.1f);

            c.orthographicSize = orthoSet;

            float dist = Vector3.Distance(c.transform.position, cameraPositionGoal);
            if(dist > 0f) {
                Vector3 pos = Vector3.Lerp(c.transform.position, cameraPositionGoal, 0.1f);
                pos.x = 0;
                c.transform.position = pos;
                c.transform.rotation = Quaternion.identity;
            }
        }

        void AdjustCameraGoals (bool calculateMixTime = false) {
            if (m_PreviewUtility == null || _previewUnityArmatureComp == null)
                return;

            if (calculateMixTime) {
                if (_previewUnityArmatureComp.animation!=null&&!string.IsNullOrEmpty(_previewUnityArmatureComp.animationName))
                    cameraAdjustEndFrame = EditorApplication.timeSinceStartup + _previewUnityArmatureComp.animation.GetState(_previewUnityArmatureComp.animationName).fadeTotalTime;
            }

            Bounds bounds = new Bounds(Vector3.zero,Vector3.zero);
            foreach (Renderer child in _previewUnityArmatureComp.slotsRoot.GetComponentsInChildren<Renderer>()){
                bounds.Encapsulate(child.bounds);   
            }
            cameraOrthoGoal = bounds.center.y + 1f;
            cameraPositionGoal = new Vector3(bounds.center.x,bounds.center.y, -10f);
        }

        void MouseScroll (Rect position) {
            Event current = Event.current;
            int controlID = GUIUtility.GetControlID(SliderHash, FocusType.Passive);
            switch (current.GetTypeForControl(controlID)) {
                case EventType.ScrollWheel:
                    if (position.Contains(current.mousePosition)) {
                        cameraOrthoGoal += current.delta.y * 0.06f;
                        cameraOrthoGoal = Mathf.Max(0.01f, cameraOrthoGoal);
                        GUIUtility.hotControl = controlID;
                        current.Use();
                    }
                    break;
            }
        }


        void EditorUpdate () {
            if (_previewUnityArmatureComp == null)
                return;

            _previewUnityArmatureComp.transform.localPosition = (Vector3)_pos;

            AdjustCamera();

            if (_previewUnityArmatureComp != null && _previewUnityArmatureComp.armature != null && _previewUnityArmatureComp.animation.isPlaying)
            {

                var dt = (System.DateTime.Now.Ticks - _nowTime) * 0.0000001f;
                if (dt >= _frameRate)
                {
                    _previewUnityArmatureComp.armature.AdvanceTime(dt);
                    foreach (var slot in _previewUnityArmatureComp.armature.GetSlots())
                    {
                        if (slot.childArmature != null)
                        {
                            slot.childArmature.AdvanceTime(dt);
                        }
                    }
                    _nowTime = System.DateTime.Now.Ticks;
                }
            }
            Repaint();
        }
    }
}