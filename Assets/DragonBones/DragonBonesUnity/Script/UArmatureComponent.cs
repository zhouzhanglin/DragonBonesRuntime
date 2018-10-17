using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DragonBones
{
    public enum DBSortingMode
    {
        SortByZ,
        SortByOrder
    }

    ///<inheritDoc/>
    [ExecuteInEditMode, DisallowMultipleComponent]
    public class UArmatureComponent : DBEventDispatcher, IArmatureProxy
    {
        public const int ORDER_SPACE = 10;

        public UDragonBonesData unityData = null;
       
        [SerializeField]
        internal string _armatureName = null;
        public string ArmatureName{
            get{ 
                return _armatureName;
            }
            set{ 
                #if UNITY_EDITOR
                if(!Application.isPlaying) _armatureName = value;
                #endif
            }
        }

        [SerializeField]
        internal bool _isUGUI = false;
        public bool IsUGUI{
            get{ 
                return _isUGUI;
            }
            set{ 
                #if UNITY_EDITOR
                _isUGUI = value;
                #endif
            }
        }

        public bool debugDraw = false;
        internal readonly ColorTransform _colorTransform = new ColorTransform();

        /// <private/>
        [SerializeField]
        internal string _animationName = null;
        public string AnimationName{
            get{ 
                return _animationName;
            }
            set{ 
                _animationName = value;
                if (Application.isPlaying && animation != null )
                {
                    animation.Play(_animationName);
                }
            }
        }

        /// <private/>
        private bool _disposeProxy = true;
        /// <private/>
        internal Armature _armature = null;
        [Tooltip("0 : Loop")]
        [Range(0, 100)]
        [SerializeField]
        protected int _playTimes = 0;
        [Range(-2f, 2f)]
        [SerializeField]
        protected float _timeScale = 1.0f;

        [SerializeField]
        internal DBSortingMode _sortingMode = DBSortingMode.SortByZ;
        [SerializeField]
        internal string _sortingLayerName = "Default";
        [SerializeField]
        internal int _sortingOrder = 0;
        [SerializeField]
        internal float _zSpace = 0.0f;

        [SerializeField]
        protected bool _flipX = false;
        [SerializeField]
        protected bool _flipY = false;
        //default open combineMeshs
        [SerializeField]
        protected bool _closeCombineMeshs = false;

        private bool _hasSortingGroup = false;
        private Material _debugDrawer;

        public GameObject slotsRoot;
        public GameObject bonesRoot;
        public List<UBone> unityBones = null;
        [SerializeField]
        internal bool _boneHierarchy = false;

        //
        internal int _armatureZ;

        /// <private/>
        public void DBClear()
        {
            if (this._armature != null)
            {
                this._armature = null;
                if (this._disposeProxy)
                {
                    try
                    {
                        var go = gameObject;
                        UFactoryHelper.DestroyUnityObject(gameObject);
                    }
                    catch
                    {

                    }
                }
            }

            this.unityData = null;
            this._armatureName = null;
            this._animationName = null;
            this._isUGUI = false;
            this.debugDraw = false;

            this._disposeProxy = true;
            this._armature = null;
            this._colorTransform.Identity();
            this._sortingMode = DBSortingMode.SortByZ;
            this._sortingLayerName = "Default";
            this._sortingOrder = 0;
            this._playTimes = 0;
            this._timeScale = 1.0f;
            this._zSpace = 0.0f;
            this._flipX = false;
            this._flipY = false;

            this._hasSortingGroup = false;

            this._debugDrawer = null;

            this._armatureZ = 0;
            this._closeCombineMeshs = false;
        }
        ///
        public void DBInit(Armature armature)
        {
            this._armature = armature;
        }

        public void DBUpdate()
        {

        }

        void CreateLineMaterial()
        {
            if (!_debugDrawer)
            {
                // Unity has a built-in shader that is useful for drawing
                // simple colored things.
                Shader shader = Shader.Find("Hidden/Internal-Colored");
                _debugDrawer = new Material(shader);
                _debugDrawer.hideFlags = HideFlags.HideAndDontSave;
                // Turn on alpha blending
                _debugDrawer.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                _debugDrawer.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                // Turn backface culling off
                _debugDrawer.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
                // Turn off depth writes
                _debugDrawer.SetInt("_ZWrite", 0);
            }
        }

        void OnRenderObject()
        {
            var drawed = DragonBones.debugDraw || this.debugDraw;
            if (drawed)
            {
                Color boneLineColor = new Color(0.0f, 1.0f, 1.0f, 0.7f);
                Color boundingBoxLineColor = new Color(1.0f, 0.0f, 1.0f, 1.0f);
                CreateLineMaterial();
                // Apply the line material
                _debugDrawer.SetPass(0);

                GL.PushMatrix();
                // Set transformation matrix for drawing to
                // match our transform
                GL.MultMatrix(transform.localToWorldMatrix);
                //
                var bones = this._armature.GetBones();
                var offset = 0.02f;
                // draw bone line
                for (int i = 0; i < bones.Count; i++)
                {
                    var bone = bones[i];
                    var boneLength = System.Math.Max(bone.boneData.length, offset);

                    var startPos = new Vector3(bone.globalTransformMatrix.tx, bone.globalTransformMatrix.ty, 0.0f);
                    var endPos = new Vector3(bone.globalTransformMatrix.a * boneLength, bone.globalTransformMatrix.b * boneLength, 0.0f) + startPos;

                    var torwardDir = (startPos - endPos).normalized;
                    var leftStartPos = Quaternion.AngleAxis(90, Vector3.forward) * torwardDir * offset + startPos;
                    var rightStartPos = Quaternion.AngleAxis(-90, Vector3.forward) * torwardDir * offset + startPos;
                    var newStartPos = startPos + torwardDir * offset;
                    //
                    GL.Begin(GL.LINES);
                    GL.Color(boneLineColor);
                    GL.Vertex(leftStartPos);
                    GL.Vertex(rightStartPos);
                    GL.End();
                    GL.Begin(GL.LINES);
                    GL.Color(boneLineColor);
                    GL.Vertex(newStartPos);
                    GL.Vertex(endPos);
                    GL.End();
                }

                // draw boundingBox
                Point result = new Point();
                var slots = this._armature.GetSlots();
                for (int i = 0; i < slots.Count; i++)
                {
                    var slot = slots[i] as USlot;
                    var boundingBoxData = slot.boundingBoxData;

                    if (boundingBoxData == null)
                    {
                        continue;
                    }

                    var bone = slot.parent;

                    slot.UpdateTransformAndMatrix();
                    slot.UpdateGlobalTransform();

                    var tx = slot.globalTransformMatrix.tx;
                    var ty = slot.globalTransformMatrix.ty;
                    var boundingBoxWidth = boundingBoxData.width;
                    var boundingBoxHeight = boundingBoxData.height;
                    //
                    switch (boundingBoxData.type)
                    {
                        case BoundingBoxType.Rectangle:
                            {
                                GL.Begin(GL.LINE_STRIP);
                                GL.Color(boundingBoxLineColor);

                                var leftTopPos = new Vector3(tx - boundingBoxWidth * 0.5f, ty + boundingBoxHeight * 0.5f, 0.0f);
                                var leftBottomPos = new Vector3(tx - boundingBoxWidth * 0.5f, ty - boundingBoxHeight * 0.5f, 0.0f);
                                var rightTopPos = new Vector3(tx + boundingBoxWidth * 0.5f, ty + boundingBoxHeight * 0.5f, 0.0f);
                                var rightBottomPos = new Vector3(tx + boundingBoxWidth * 0.5f, ty - boundingBoxHeight * 0.5f, 0.0f);

                                GL.Vertex(leftTopPos);
                                GL.Vertex(rightTopPos);
                                GL.Vertex(rightBottomPos);
                                GL.Vertex(leftBottomPos);
                                GL.Vertex(leftTopPos);

                                GL.End();
                            }
                            break;
                        case BoundingBoxType.Ellipse:
                            {

                            }
                            break;
                        case BoundingBoxType.Polygon:
                            {
                                var vertices = (boundingBoxData as PolygonBoundingBoxData).vertices;
                                GL.Begin(GL.LINE_STRIP);
                                GL.Color(boundingBoxLineColor);
                                for (var j = 0; j < vertices.Count; j += 2)
                                {
                                    slot.globalTransformMatrix.TransformPoint(vertices[j], vertices[j + 1], result);
                                    GL.Vertex3(result.x, result.y, 0.0f);
                                }

                                slot.globalTransformMatrix.TransformPoint(vertices[0], vertices[1], result);
                                GL.Vertex3(result.x, result.y, 0.0f);
                                GL.End();
                            }
                            break;
                        default:
                            break;
                    }
                }

                GL.PopMatrix();
            }

        }

        /// <inheritDoc/>
        public void Dispose(bool disposeProxy = true)
        {
            _disposeProxy = disposeProxy;

            if (_armature != null)
            {
                _armature.Dispose();
            }
        }
        /// <summary>
        /// Get the Armature.
        /// </summary>
        /// <readOnly/>
        /// <version>DragonBones 4.5</version>
        /// <language>en_US</language>

        /// <summary>
        /// 获取骨架。
        /// </summary>
        /// <readOnly/>
        /// <version>DragonBones 4.5</version>
        /// <language>zh_CN</language>
        public Armature armature
        {
            get { return _armature; }
        }

        /// <summary>
        /// Get the animation player
        /// </summary>
        /// <readOnly/>
        /// <version>DragonBones 4.5</version>
        /// <language>en_US</language>

        /// <summary>
        /// 获取动画播放器。
        /// </summary>
        /// <readOnly/>
        /// <version>DragonBones 4.5</version>
        /// <language>zh_CN</language>
        public new Animation animation
        {
            get { return _armature != null ? _armature.animation : null; }
        }

        /// <summary>
        /// The slots sorting mode
        /// </summary>
        /// <version>DragonBones 4.5</version>
        /// <language>en_US</language>
        /// 
        /// <summary>
        /// 插槽排序模式
        /// </summary>
        /// <version>DragonBones 4.5</version>
        /// <language>zh_CN</language>
        public DBSortingMode sortingMode
        {
            get { return _sortingMode; }
            set
            {
                if (_sortingMode == value)
                {
                    return;
                }

#if UNITY_5_6_OR_NEWER
                var isWarning = false;
#else
                var isWarning = value == SortingMode.SortByOrder;
#endif

                if (isWarning)
                {
                    ULogHelper.LogWarning("SortingMode.SortByOrder is userd by Unity 5.6 or highter only.");
                    return;
                }

                _sortingMode = value;

                //
#if UNITY_5_6_OR_NEWER
                if (_sortingMode == DBSortingMode.SortByOrder)
                {
                    _sortingGroup = GetComponent<UnityEngine.Rendering.SortingGroup>();
                    if (_sortingGroup == null)
                    {
                        _sortingGroup = gameObject.AddComponent<UnityEngine.Rendering.SortingGroup>();
                    }
                }
                else
                {
                    _sortingGroup = GetComponent<UnityEngine.Rendering.SortingGroup>();

                    if (_sortingGroup != null)
                    {
                        DestroyImmediate(_sortingGroup);
                    }
                }
#endif

                _UpdateSlotsSorting();
            }
        }

        /// <summary>
        /// Name of the Renderer's sorting layer.
        /// </summary>
        /// <version>DragonBones 4.5</version>
        /// <language>en_US</language>
        /// 
        /// <summary>
        /// sorting layer名称。
        /// </summary>
        /// <version>DragonBones 4.5</version>
        /// <language>zh_CN</language>
        public string sortingLayerName
        {
            get { return _sortingLayerName; }
            set
            {
                _sortingLayerName = value;
                _UpdateSlotsSorting();
            }
        }

        /// <summary>
        /// Renderer's order within a sorting layer.
        /// </summary>
        /// <version>DragonBones 4.5</version>
        /// <language>en_US</language>
        /// 
        /// <summary>
        /// 插槽按照sortingOrder在同一层sorting layer中排序
        /// </summary>
        /// <version>DragonBones 4.5</version>
        /// <language>zh_CN</language>
        public int sortingOrder
        {
            get { return _sortingOrder; }
            set
            {
                _sortingOrder = value;

                _UpdateSlotsSorting();
            }
        }
        /// <summary>
        /// The Z axis spacing of slot display objects
        /// </summary>
        /// <version>DragonBones 4.5</version>
        /// <language>zh_CN</language>
        /// 
        /// <summary>
        /// 插槽显示对象的z轴间隔
        /// </summary>
        /// <version>DragonBones 4.5</version>
        /// <language>zh_CN</language>
        public float zSpace
        {
            get { return _zSpace; }
            set
            {
                if (value < 0.0f || float.IsNaN(value))
                {
                    value = 0.0f;
                }

                if (_zSpace == value)
                {
                    return;
                }

                _zSpace = value;

                _UpdateSlotsSorting();
            }
        }
        /// <summary>
        /// - The armature color.
        /// </summary>
        /// <version>DragonBones 4.5</version>
        /// <language>en_US</language>
        /// 
        /// <summary>
        /// - 骨架的颜色。
        /// </summary>
        /// <version>DragonBones 4.5</version>
        /// <language>zh_CN</language>
        public ColorTransform color
        {
            get { return this._colorTransform; }
            set
            {
                this._colorTransform.CopyFrom(value);

                foreach (var slot in this._armature.GetSlots())
                {
                    slot._colorDirty = true;
                }
            }
        }


#if UNITY_5_6_OR_NEWER
        internal UnityEngine.Rendering.SortingGroup _sortingGroup;
        public UnityEngine.Rendering.SortingGroup sortingGroup
        {
            get { return _sortingGroup; }
        }

        private void _UpdateSortingGroup()
        {
            //发现骨架有SortingGroup，那么子骨架也都加上，反之删除
            _sortingGroup = GetComponent<UnityEngine.Rendering.SortingGroup>();
            if (_sortingGroup != null)
            {
                _sortingMode = DBSortingMode.SortByOrder;
                _sortingLayerName = _sortingGroup.sortingLayerName;
                _sortingOrder = _sortingGroup.sortingOrder;

                foreach (USlot slot in _armature.GetSlots())
                {
                    if (slot.childArmature != null)
                    {
                        var childArmatureProxy = slot.childArmature.proxy as UArmatureComponent;
                        childArmatureProxy._sortingGroup = childArmatureProxy.GetComponent<UnityEngine.Rendering.SortingGroup>();
                        if (childArmatureProxy._sortingGroup == null)
                        {
                            childArmatureProxy._sortingGroup = childArmatureProxy.gameObject.AddComponent<UnityEngine.Rendering.SortingGroup>();
                        }

                        childArmatureProxy._sortingGroup.sortingLayerName = _sortingLayerName;
                        childArmatureProxy._sortingGroup.sortingOrder = _sortingOrder;
                    }
                }
            }
            else
            {
                _sortingMode = DBSortingMode.SortByZ;
                foreach (USlot slot in _armature.GetSlots())
                {
                    if (slot.childArmature != null)
                    {
                        var childArmatureProxy = slot.childArmature.proxy as UArmatureComponent;
                        childArmatureProxy._sortingGroup = childArmatureProxy.GetComponent<UnityEngine.Rendering.SortingGroup>();
                        if (childArmatureProxy._sortingGroup != null)
                        {
                            DestroyImmediate(childArmatureProxy._sortingGroup);
                        }
                    }
                }
            }

#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif

            _UpdateSlotsSorting();
        }
#endif
        private void _UpdateSlotsSorting()
        {
            if (_armature == null)
            {
                return;
            }

            if (!_isUGUI)
            {
#if UNITY_5_6_OR_NEWER
                if (_sortingGroup)
                {
                    _sortingMode = DBSortingMode.SortByOrder;
                    _sortingGroup.sortingLayerName = _sortingLayerName;
                    _sortingGroup.sortingOrder = _sortingOrder;
#if UNITY_EDITOR
                    if (!Application.isPlaying)
                    {
                        EditorUtility.SetDirty(_sortingGroup);
                    }
#endif
                }
#endif
            }

            //
            foreach (USlot slot in _armature.GetSlots())
            {
                var display = slot._renderDisplay;
                if (display == null)
                {
                    continue;
                }

                slot._SetZorder(new Vector3(display.transform.localPosition.x, display.transform.localPosition.y, -slot._zOrder * (_zSpace + 0.001f)));

                if (slot.childArmature != null)
                {
                    (slot.childArmature.proxy as UArmatureComponent)._UpdateSlotsSorting();
                }

#if UNITY_EDITOR
                if (!Application.isPlaying && slot.meshRenderer != null)
                {
                    EditorUtility.SetDirty(slot.meshRenderer);
                }
#endif
            }
        }

#if UNITY_EDITOR
        private bool _IsPrefab()
        {
            return PrefabUtility.GetPrefabParent(gameObject) == null
                && PrefabUtility.GetPrefabObject(gameObject) != null;
        }
#endif

        /// <private/>
        void Awake()
        {
#if UNITY_EDITOR
            if (_IsPrefab())
            {
                return;
            }
#endif

            if(slotsRoot==null){
                var slotsContainer = transform.Find("Slots");
                if (slotsContainer == null)
                {
                    GameObject go = new GameObject("Slots");
                    go.transform.SetParent(transform);
                    go.transform.localPosition = Vector3.zero;
                    go.transform.localRotation = Quaternion.identity;
                    go.transform.localScale = Vector3.one;
                    slotsRoot = go;
                    go.hideFlags = HideFlags.NotEditable;
                }
                else
                {
                    slotsRoot = slotsContainer.gameObject;
                    slotsContainer.hideFlags = HideFlags.NotEditable;
                }
            }

            List<UnityEngine.Transform> destroySlots = new List<UnityEngine.Transform>();
            for (int i = 0; i < slotsRoot.transform.childCount; ++i)
            {
                destroySlots.Add(slotsRoot.transform.GetChild(i));
            }
            for (int i = 0; i < destroySlots.Count; ++i)
            {
                DestroyImmediate(destroySlots[i].gameObject);
            }
            destroySlots.Clear();
            destroySlots = null;


            if (unityData != null && unityData.dragonBonesJSON != null && unityData.textureAtlas != null)
            {
                var dragonBonesData = unityData.dbData == null ? UFactory.factory.LoadData(unityData, _isUGUI) : unityData.dbData ;
                if (dragonBonesData != null && !string.IsNullOrEmpty(_armatureName))
                {
                    UFactory.factory.BuildArmatureComponent(unityData,_armatureName, null, null, gameObject, _isUGUI);
                }
            }

            if (_armature != null)
            {
#if UNITY_5_6_OR_NEWER
                if (!_isUGUI)
                {
                    _sortingGroup = GetComponent<UnityEngine.Rendering.SortingGroup>();
                }
#endif
                _UpdateSlotsSorting();

                _armature.flipX = _flipX;
                _armature.flipY = _flipY;

                _armature.animation.timeScale = _timeScale;

                if (!string.IsNullOrEmpty(_animationName))
                {
                    _armature.animation.Play(_animationName, _playTimes);
                }
            }

            CollectBones();
        }

        void Start()
        {
            //默认开启合并
            if (this._closeCombineMeshs && GetComponent<UCombineMeshs>() == null)
            {
                this.CloseCombineMeshs();
            }
            else
            {
                _closeCombineMeshs = false;
                this.OpenCombineMeshs();
            }
        }

        void LateUpdate()
        {
            if (_armature == null)
            {
                return;
            }

            _flipX = _armature.flipX;
            _flipY = _armature.flipY;

#if UNITY_5_6_OR_NEWER
            var hasSortingGroup = GetComponent<UnityEngine.Rendering.SortingGroup>() != null;
            if (hasSortingGroup != _hasSortingGroup)
            {
                _hasSortingGroup = hasSortingGroup;

                _UpdateSortingGroup();
            }
#endif

            if(unityBones!=null){
                int len = unityBones.Count;
                for(int i=0;i<len;++i){
                    UBone bone = unityBones[i];
                    if(bone) bone._Update();
                }
            }

        }

        /// <private/>
        void OnDestroy()
        {
            if (_armature != null)
            {
                var armature = _armature;
                _armature = null;

                armature.Dispose();

                if (!Application.isPlaying)
                {
                    UFactory.factory._dragonBones.AdvanceTime(0.0f);
                }
            }

            _armature = null;
        }

        private void OpenCombineMeshs()
        {
            if (this._isUGUI)
            {
                return;
            }

            //
            var cm = gameObject.GetComponent<UCombineMeshs>();
            if (cm == null)
            {
                cm = gameObject.AddComponent<UCombineMeshs>();
            }
            //

            if (this._armature == null)
            {
                return;
            }
            var slots = this._armature.GetSlots();
            foreach (var slot in slots)
            {
                if (slot.childArmature != null)
                {
                    (slot.childArmature.proxy as UArmatureComponent).OpenCombineMeshs();
                }
            }
        }

        public void CloseCombineMeshs()
        {
            this._closeCombineMeshs = true;
            //
            var cm = gameObject.GetComponent<UCombineMeshs>();
            if (cm != null)
            {
                DestroyImmediate(cm);
            }

            if (this._armature == null)
            {
                return;
            }
            //
            var slots = this._armature.GetSlots();
            foreach (var slot in slots)
            {
                if (slot.childArmature != null)
                {
                    (slot.childArmature.proxy as UArmatureComponent).CloseCombineMeshs();
                }
            }
        }


        #region Bones
        public void CollectBones(){
            if(unityBones!=null )
            {
                foreach(UBone unityBone in unityBones){
                    foreach(Bone bone in armature.GetBones()){
                        if(unityBone.name.Equals(bone.name)){
                            unityBone._bone = bone;
                            unityBone._proxy=this;
                        }
                    }
                }
            }
        }
        public void ShowBones(){
            RemoveBones();
            if(bonesRoot==null){
                var bonesContainer = transform.Find("Bones");
                if (bonesContainer == null)
                {
                    GameObject go = new GameObject("Bones");
                    go.transform.SetParent(transform);
                    go.transform.localPosition = Vector3.zero;
                    go.transform.localRotation = Quaternion.identity;
                    go.transform.localScale = Vector3.one;
                    bonesRoot = go;
                    go.hideFlags = HideFlags.NotEditable;
                }
                else
                {
                    bonesRoot = bonesContainer.gameObject;
                    bonesContainer.hideFlags = HideFlags.NotEditable;
                }
            }
            if(armature!=null)
            {
                unityBones = new List<UBone>();
                foreach(Bone bone in armature.GetBones()){
                    GameObject go = new GameObject(bone.name);
                    UBone ub = go.AddComponent<UBone> ();
                    ub._bone = bone;
                    ub._proxy = this;
                    unityBones.Add(ub);

                    go.transform.SetParent(bonesRoot.transform);
                }
                foreach(UBone bone in unityBones){
                    bone.GetParentGameObject();
                }
                foreach (UArmatureComponent child in slotsRoot.GetComponentsInChildren<UArmatureComponent>(true))
                {
                    child.ShowBones();
                }
            }
        }
        public void RemoveBones(){
            foreach (UArmatureComponent child in slotsRoot.GetComponentsInChildren<UArmatureComponent>(true))
            {
                child.RemoveBones();
            }
            if(unityBones!=null){
                unityBones = null;
            }
            if(bonesRoot){
                DestroyImmediate(bonesRoot);
            }
        }
        #endregion

    }
}