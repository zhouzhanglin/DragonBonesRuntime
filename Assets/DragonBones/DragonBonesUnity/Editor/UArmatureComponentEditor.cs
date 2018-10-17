using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Reflection;
using UnityEditor.SceneManagement;

namespace DragonBones
{
    [CustomEditor(typeof(UArmatureComponent))]
    public class UArmatureComponentEditor : Editor
    {
        private long _nowTime = 0;
        private float _frameRate = 1.0f / 24.0f;

        private int _armatureIndex = -1;
        private int _animationIndex = -1;
        private int _sortingModeIndex = -1;
        private int _sortingLayerIndex = -1;

        private List<string> _armatureNames = null;
        private List<string> _animationNames = null;
        private List<string> _sortingLayerNames = null;

        private UArmatureComponent _armatureComponent = null;

        private SerializedProperty _playTimesPro;
        private SerializedProperty _timeScalePro;
        private SerializedProperty _flipXPro;
        private SerializedProperty _flipYPro;
        private SerializedProperty _closeCombineMeshsPro;
        private SerializedProperty _boneHierarchyPro;

        private string[] _sortingMode = new string[]{DBSortingMode.SortByZ.ToString(), DBSortingMode.SortByOrder.ToString()};

        void ClearUp()
        {
            this._armatureIndex = -1;
            this._animationIndex = -1;

            this._armatureNames = null;
            this._animationNames = null;
        }

        void OnEnable()
        {
            this._armatureComponent = target as UArmatureComponent;
            if (_IsPrefab())
            {
                return;
            }

            this._nowTime = System.DateTime.Now.Ticks;

            this._sortingModeIndex = (int)this._armatureComponent.sortingMode;
            this._sortingLayerNames = _GetSortingLayerNames();
            this._sortingLayerIndex = this._sortingLayerNames.IndexOf(this._armatureComponent.sortingLayerName);

            this._playTimesPro = serializedObject.FindProperty("_playTimes");
            this._timeScalePro = serializedObject.FindProperty("_timeScale");
            this._flipXPro = serializedObject.FindProperty("_flipX");
            this._flipYPro = serializedObject.FindProperty("_flipY");
            this._closeCombineMeshsPro = serializedObject.FindProperty("_closeCombineMeshs");
            this._boneHierarchyPro = serializedObject.FindProperty("_boneHierarchy");

            // Update armature.
            if (!EditorApplication.isPlayingOrWillChangePlaymode &&
                _armatureComponent.armature == null &&
                _armatureComponent.unityData != null &&
                !string.IsNullOrEmpty(_armatureComponent.ArmatureName))
            {
                // Clear cache
                UFactory.factory.Clear(true);

                // Unload
                EditorUtility.UnloadUnusedAssetsImmediate();
                System.GC.Collect();

                // Load data.
                var dragonBonesData = UFactory.factory.LoadData(_armatureComponent.unityData,_armatureComponent.IsUGUI);

                // Refresh texture atlas.
                UFactory.factory.RefreshAllTextureAtlas(_armatureComponent);

                // Refresh armature.
                _ClearSlotsGameObjects();
                UDragonBonesEditor.ChangeArmatureData(_armatureComponent, _armatureComponent.ArmatureName, dragonBonesData.name);

                // Refresh texture.
                _armatureComponent.armature.InvalidUpdate(null, true);

                // Play animation.
                if (!string.IsNullOrEmpty(_armatureComponent.AnimationName))
                {
                    _armatureComponent.animation.Play(_armatureComponent.AnimationName, _playTimesPro.intValue);
                }
            }

            // Update hideFlags.
            if (!EditorApplication.isPlayingOrWillChangePlaymode &&
                _armatureComponent.armature != null &&
                _armatureComponent.armature.parent != null)
            {
                _armatureComponent.gameObject.hideFlags = HideFlags.NotEditable;
            }
            else
            {
                _armatureComponent.gameObject.hideFlags = HideFlags.None;
            }

            _UpdateParameters();
        }

        public override void OnInspectorGUI()
        {
            if (_IsPrefab())
            {
                return;
            }

            serializedObject.Update();

            if (_armatureIndex == -1)
            {
                _UpdateParameters();
            }

            // DragonBones Data
            EditorGUILayout.BeginHorizontal();

            _armatureComponent.unityData = EditorGUILayout.ObjectField("DragonBones Data", _armatureComponent.unityData, typeof(UDragonBonesData), false) as UDragonBonesData;

            var created = false;
            if (_armatureComponent.unityData != null)
            {
                if (_armatureComponent.armature == null)
                {
                    if (GUILayout.Button("Create"))
                    {
                        created = true;
                    }
                }
                else
                {
                    if (GUILayout.Button("Reload"))
                    {
                        if (EditorUtility.DisplayDialog("DragonBones Alert", "Are you sure you want to reload data", "Yes", "No"))
                        {
                            created = true;
                        }
                    }
                }
            }

            if (created)
            {
                _ClearSlotsGameObjects();
                //clear cache
                UFactory.factory.Clear(true);
                ClearUp();
                _armatureComponent.AnimationName = null;

                if (UDragonBonesEditor.ChangeDragonBonesData(_armatureComponent, _armatureComponent.unityData.dragonBonesJSON))
                {
                    _UpdateParameters();
                }
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();

            if (_armatureComponent.armature != null)
            {
                var dragonBonesData = _armatureComponent.armature.armatureData.parent;

                // Armature
                if (dragonBonesData != null && _armatureNames != null)
                {
                    var armatureIndex = EditorGUILayout.Popup("Armature", _armatureIndex, _armatureNames.ToArray());
                    if (_armatureIndex != armatureIndex)
                    {
                        _armatureIndex = armatureIndex;

                        var armatureName = _armatureNames[_armatureIndex];
                        UDragonBonesEditor.ChangeArmatureData(_armatureComponent, armatureName, dragonBonesData.name);
                        _UpdateParameters();

                        _armatureComponent.gameObject.name = armatureName;

                        MarkSceneDirty();
                    }
                }
                else if (dragonBonesData == null)
                {

                    _ClearSlotsGameObjects();

                    var armatureName = _armatureComponent.ArmatureName;
                    var animName = _armatureComponent.AnimationName;
                    UDragonBonesEditor.ChangeArmatureData(_armatureComponent, armatureName, _armatureComponent.unityData.dataName);
                    _armatureComponent.ArmatureName = armatureName;
                    _armatureComponent.AnimationName = animName;
                    _UpdateParameters();
                    _armatureComponent.gameObject.name = armatureName;
                    MarkSceneDirty();
                }
               
                // Animation
                if (_animationNames != null && _animationNames.Count > 0)
                {
                    EditorGUILayout.BeginHorizontal();
                    List<string> anims = new List<string>(_animationNames);
                    anims.Insert(0, "<None>");
                    var animationIndex = EditorGUILayout.Popup("Animation", _animationIndex + 1, anims.ToArray()) - 1;
                    if (animationIndex != _animationIndex)
                    {
                        _animationIndex = animationIndex;
                        if (animationIndex >= 0)
                        {
                            _armatureComponent.AnimationName = _animationNames[animationIndex];
                            var animationData = _armatureComponent.animation.animations[_armatureComponent.AnimationName];
                            _armatureComponent.animation.Play(_armatureComponent.AnimationName, _playTimesPro.intValue);
                            _UpdateParameters();
                        }
                        else
                        {
                            _armatureComponent.AnimationName = null;
                            _playTimesPro.intValue = 0;
                            _armatureComponent.animation.Stop();
                        }

                        MarkSceneDirty();
                    }

                    if (_animationIndex >= 0)
                    {
                        if (_armatureComponent.animation.isPlaying)
                        {
                            if (GUILayout.Button("Stop"))
                            {
                                _armatureComponent.animation.Stop();
                            }
                        }
                        else
                        {
                            if (GUILayout.Button("Play"))
                            {
                                _armatureComponent.animation.Play(null, _playTimesPro.intValue);
                            }
                        }
                    }

                    EditorGUILayout.EndHorizontal();

                    //playTimes
                    var playTimes = _playTimesPro.intValue;
                    EditorGUILayout.PropertyField(_playTimesPro, false);
                    if (playTimes != _playTimesPro.intValue)
                    {
                        if (!string.IsNullOrEmpty(_armatureComponent.AnimationName))
                        {
                            _armatureComponent.animation.Reset();
                            _armatureComponent.animation.Play(_armatureComponent.AnimationName, _playTimesPro.intValue);
                        }
                    }

                    // TimeScale
                    var timeScale = _timeScalePro.floatValue;
                    EditorGUILayout.PropertyField(_timeScalePro, false);
                    if (timeScale != _timeScalePro.floatValue)
                    {
                        _armatureComponent.animation.timeScale = _timeScalePro.floatValue;
                    }
                }

                //
                EditorGUILayout.Space();

                if (!_armatureComponent.IsUGUI)
                {
                    //Sorting Mode
                    _sortingModeIndex = EditorGUILayout.Popup("Sorting Mode", (int)_armatureComponent.sortingMode, _sortingMode);
                    if (_sortingModeIndex != (int)_armatureComponent.sortingMode)
                    {
                        Undo.RecordObject(_armatureComponent, "Sorting Mode");
                        _armatureComponent.sortingMode = (DBSortingMode)_sortingModeIndex;
                        // 里面return了，没有赋值成功
                        if (_armatureComponent.sortingMode != (DBSortingMode)_sortingModeIndex)
                        {
                            _sortingModeIndex = (int)_armatureComponent.sortingMode;
                        }

                        MarkSceneDirty();
                    }

                    // Sorting Layer
                    _sortingLayerIndex = EditorGUILayout.Popup("Sorting Layer", _sortingLayerIndex, _sortingLayerNames.ToArray());
                    if (_sortingLayerNames[_sortingLayerIndex] != _armatureComponent.sortingLayerName)
                    {
                        Undo.RecordObject(_armatureComponent, "Sorting Layer");
                        _armatureComponent.sortingLayerName = _sortingLayerNames[_sortingLayerIndex];

                        MarkSceneDirty();
                    }

                    // Sorting Order
                    var sortingOrder = EditorGUILayout.IntField("Order in Layer", _armatureComponent.sortingOrder);
                    if (sortingOrder != _armatureComponent.sortingOrder)
                    {
                        Undo.RecordObject(_armatureComponent, "Edit Sorting Order");
                        _armatureComponent.sortingOrder = sortingOrder;

                        MarkSceneDirty();
                    }

                    // ZSpace
                    _armatureComponent.zSpace = EditorGUILayout.Slider("Z Space", _armatureComponent.zSpace, 0.0f, 0.5f);
                }

                EditorGUILayout.Space();

                // Flip
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel("Flip");
                var flipX = _flipXPro.boolValue;
                var flipY = _flipYPro.boolValue;
                _flipXPro.boolValue = GUILayout.Toggle(_flipXPro.boolValue, "X", GUILayout.Width(30));
                _flipYPro.boolValue = GUILayout.Toggle(_flipYPro.boolValue, "Y", GUILayout.Width(30));
                if (flipX != _flipXPro.boolValue || flipY != _flipYPro.boolValue)
                {
                    _armatureComponent.armature.flipX = _flipXPro.boolValue;
                    _armatureComponent.armature.flipY = _flipYPro.boolValue;

                    MarkSceneDirty();
                }

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space();
            }

            if(_armatureComponent.armature!=null && _armatureComponent.armature.parent==null)
            {
                if(_armatureComponent.unityBones!=null && _armatureComponent.bonesRoot!=null)
                {
                    _boneHierarchyPro.boolValue = EditorGUILayout.Toggle("Bone Hierarchy", _boneHierarchyPro.boolValue);
                }
                if(!Application.isPlaying){
                    EditorGUILayout.BeginHorizontal();
                    if(_armatureComponent.unityBones!=null && _armatureComponent.bonesRoot!=null){
                        if(GUILayout.Button("Remove Bones",GUILayout.Height(20))){
                            if(EditorUtility.DisplayDialog("DragonBones Alert", "Are you sure you want to remove bones", "Yes", "No")){
                                _armatureComponent.RemoveBones();
                            }
                        }
                    }else{
                        if(GUILayout.Button("Show Bones",GUILayout.Height(20))){
                            _armatureComponent.ShowBones();
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }


            if (_armatureComponent.armature != null && _armatureComponent.armature.parent == null)
            {
                if (!Application.isPlaying && !this._armatureComponent.IsUGUI)
                {
                    var oldValue = this._closeCombineMeshsPro.boolValue;
                    this._closeCombineMeshsPro.boolValue = EditorGUILayout.Toggle("CloseCombineMeshs", this._closeCombineMeshsPro.boolValue);

                    if (!this._closeCombineMeshsPro.boolValue)
                    {
                        if (GUILayout.Button("Show Slots"))
                        {
                            ShowSlotsWindow.OpenWindow(this._armatureComponent);
                        }
                    }

                    if(oldValue != this._closeCombineMeshsPro.boolValue)
                    {
                        if(this._closeCombineMeshsPro.boolValue)
                        {
                            this._armatureComponent.CloseCombineMeshs();
                        }
                    }
                    if (!this._closeCombineMeshsPro.boolValue && _armatureComponent.gameObject.GetComponent<UCombineMeshs>() == null)
                    {
                        _armatureComponent.gameObject.AddComponent<UCombineMeshs>();
                    }
                }
            }

            serializedObject.ApplyModifiedProperties();

            if (!EditorApplication.isPlayingOrWillChangePlaymode && Selection.activeObject == _armatureComponent.gameObject)
            {
                EditorUtility.SetDirty(_armatureComponent);
                HandleUtility.Repaint();
            }
        }

        void OnSceneGUI()
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode && _armatureComponent.armature != null)
            {
                var dt = (System.DateTime.Now.Ticks - _nowTime) * 0.0000001f;
                if (dt >= _frameRate)
                {
                    _armatureComponent.armature.AdvanceTime(dt);

                    foreach (var slot in _armatureComponent.armature.GetSlots())
                    {
                        if (slot.childArmature != null)
                        {
                            slot.childArmature.AdvanceTime(dt);
                        }
                    }

                    //
                    _nowTime = System.DateTime.Now.Ticks;
                }
            }
        }

        private void _ClearSlotsGameObjects(){
            if (_armatureComponent.slotsRoot != null)
            {
                List<UnityEngine.Transform> destroySlots = new List<UnityEngine.Transform>();
                for (int i = 0; i < _armatureComponent.slotsRoot.transform.childCount; ++i)
                {
                    destroySlots.Add(_armatureComponent.slotsRoot.transform.GetChild(i));
                }
                for (int i = 0; i < destroySlots.Count; ++i)
                {
                    DestroyImmediate(destroySlots[i].gameObject);
                }
                destroySlots.Clear();
                destroySlots = null;
            }
        }

        private void _UpdateParameters()
        {
            if (_armatureComponent.armature != null)
            {
                _frameRate = 1.0f / (float)_armatureComponent.armature.armatureData.frameRate;

                if (_armatureComponent.armature.armatureData.parent != null)
                {
                    _armatureNames = _armatureComponent.armature.armatureData.parent.armatureNames;
                    _animationNames = _armatureComponent.animation.animationNames;
                    _armatureIndex = _armatureNames.IndexOf(_armatureComponent.armature.name);
                    //
                    if (!string.IsNullOrEmpty(_armatureComponent.AnimationName))
                    {
                        _animationIndex = _animationNames.IndexOf(_armatureComponent.AnimationName);
                    }
                }
                else
                {
                    _armatureNames = null;
                    _animationNames = null;
                    _armatureIndex = -1;
                    _animationIndex = -1;
                }
            }
            else
            {
                _armatureNames = null;
                _animationNames = null;
                _armatureIndex = -1;
                _animationIndex = -1;
            }
        }

        private bool _IsPrefab()
        {
            return PrefabUtility.GetPrefabParent(_armatureComponent.gameObject) == null
                && PrefabUtility.GetPrefabObject(_armatureComponent.gameObject) != null;
        }

        private List<string> _GetSortingLayerNames()
        {
            var internalEditorUtilityType = typeof(InternalEditorUtility);
            var sortingLayersProperty = internalEditorUtilityType.GetProperty("sortingLayerNames", BindingFlags.Static | BindingFlags.NonPublic);

            return new List<string>(sortingLayersProperty.GetValue(null, new object[0]) as string[]);
        }

        private void MarkSceneDirty()
        {
            EditorUtility.SetDirty(_armatureComponent);
            //
            if (!Application.isPlaying && !_IsPrefab())
            {
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }
        }
    }
}