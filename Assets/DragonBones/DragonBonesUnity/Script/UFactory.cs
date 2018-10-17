using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DragonBones
{
    /// <pjrivate/>
    internal class ClockHandler : MonoBehaviour
    {
        void Update()
        {
            UFactory.factory._dragonBones.AdvanceTime(Time.deltaTime);
        }
    }

    public class UBuildArmaturePackage:BuildArmaturePackage{
        public UDragonBonesData unityData;
    }

    public class UFactory:BaseFactory {

        internal const string defaultShaderName = "Sprites/Default";
        internal const string defaultUIShaderName = "UI/Default";

        internal static DragonBones _dragonBonesInstance = null;
        private static UFactory _factory = null;
        private static GameObject _gameObject = null;

        private GameObject _armatureGameObject = null;
        private bool _isUGUI = false;

        public UFactory(DataParser dataParser = null) : base(dataParser)
        {
            Init();
        }

        public static UFactory factory
        {
            get
            {
                if (_factory == null)
                {
                    _factory = new UFactory();
                }

                return _factory;
            }
        }

        private void Init()
        {
            if (Application.isPlaying)
            {
                if (_gameObject == null)
                {
                    _gameObject = GameObject.Find("DragonBones Object");
                    if (_gameObject == null)
                    {
                        _gameObject = new GameObject("DragonBones Object", typeof(ClockHandler));

                        _gameObject.isStatic = true;
                        _gameObject.hideFlags = HideFlags.HideInHierarchy|HideFlags.DontSaveInEditor;
                    }
                }

                //全局的
                GameObject.DontDestroyOnLoad(_gameObject);

                var clockHandler = _gameObject.GetComponent<ClockHandler>();
                if (clockHandler == null)
                {
                    _gameObject.AddComponent<ClockHandler>();
                }

                var eventManager = _gameObject.GetComponent<DBEventDispatcher>();
                if (eventManager == null)
                {
                    eventManager = _gameObject.AddComponent<DBEventDispatcher>();
                }

                if (_dragonBonesInstance == null)
                {
                    _dragonBonesInstance = new DragonBones(eventManager);
                    //
                    DragonBones.yDown = false;
                }
            }
            else
            {
                if (_dragonBonesInstance == null)
                {
                    _dragonBonesInstance = new DragonBones(null);
                    //
                    DragonBones.yDown = false;
                }
            }

            _dragonBones = _dragonBonesInstance;
        }

        protected override TextureAtlasData _BuildTextureAtlasData(TextureAtlasData textureAtlasData, object textureAtlas)
        {
            if (textureAtlasData != null)
            {
                if (textureAtlas != null)
                {
                    (textureAtlasData as UTextureAtlasData).uiTexture = (textureAtlas as UDragonBonesData.UTextureAtlas).uiMaterial;
                    (textureAtlasData as UTextureAtlasData).texture = (textureAtlas as UDragonBonesData.UTextureAtlas).material;
                }
            }
            else
            {
                textureAtlasData = BaseObject.BorrowObject<UTextureAtlasData>();
            }
            return textureAtlasData;
        }

        /// <private/>
        protected override Armature _BuildArmature(BuildArmaturePackage dataPackage)
        {
            var armature = BaseObject.BorrowObject<Armature>();
            var armatureDisplay = _armatureGameObject == null ? new GameObject(dataPackage.armature.name) : _armatureGameObject;
            var armatureComponent = armatureDisplay.GetComponent<UArmatureComponent>();
            if (armatureComponent == null)
            {
                armatureComponent = armatureDisplay.AddComponent<UArmatureComponent>();
                armatureComponent.IsUGUI = _isUGUI;

                if (armatureComponent.IsUGUI)
                {
                    armatureComponent.transform.localScale = Vector2.one * (1.0f / dataPackage.armature.scale);
                }
            }
            armatureComponent._armature = armature;
            armature.Init(dataPackage.armature, armatureComponent, armatureDisplay, this._dragonBones);
            _armatureGameObject = null;
            return armature;
        }


        protected override Armature _BuildChildArmature(BuildArmaturePackage dataPackage, Slot slot, DisplayData displayData)
        {
            UBuildArmaturePackage uPackage = dataPackage as UBuildArmaturePackage;
            UDragonBonesData unityData = uPackage.unityData;

            var childDisplayName = slot.slotData.name + " (" + displayData.path + ")"; //
            var proxy = slot.armature.proxy as UArmatureComponent;
            var childTransform = proxy.transform.Find(childDisplayName);
            Armature childArmature = null;
            if (childTransform == null)
            {
                childArmature = BuildArmature(unityData,displayData.path, dataPackage.dataName);
            }
            else
            {
                childArmature = BuildArmatureComponent(unityData,displayData.path, dataPackage.dataName, dataPackage.textureAtlasName, childTransform.gameObject).armature;
            }

            if (childArmature == null)
            {
                return null;
            }

            var childArmatureDisplay = childArmature.display as GameObject;
            childArmatureDisplay.GetComponent<UArmatureComponent>().IsUGUI = proxy.GetComponent<UArmatureComponent>().IsUGUI;
            childArmatureDisplay.name = childDisplayName;
            childArmatureDisplay.transform.SetParent(proxy.transform, false);
            childArmatureDisplay.gameObject.hideFlags = HideFlags.HideInHierarchy;
            childArmatureDisplay.SetActive(false);
            return childArmature;
        }

        /// <private/>
        protected override Slot _BuildSlot(BuildArmaturePackage dataPackage, SlotData slotData, Armature armature)
        {
            var slot = BaseObject.BorrowObject<USlot>();
            var armatureDisplay = armature.display as GameObject;
            var transform = armatureDisplay.transform.Find(slotData.name);
            var gameObject = transform == null ? null : transform.gameObject;
            var isNeedIngoreCombineMesh = false;
            if (gameObject == null)
            {
                gameObject = new GameObject(slotData.name);
            }
            else
            {
                if (gameObject.hideFlags == HideFlags.None)
                {
                    var combineMeshs = (armature.proxy as UArmatureComponent).GetComponent<UCombineMeshs>();
                    if (combineMeshs != null)
                    {
                        isNeedIngoreCombineMesh = !combineMeshs.slotNames.Contains(slotData.name);
                    }
                }
            }
            slot.Init(slotData, armature, gameObject, gameObject);

            if (isNeedIngoreCombineMesh)
            {
                slot.DisallowCombineMesh();
            }

            return slot;
        }


        public UArmatureComponent BuildArmatureComponent(UDragonBonesData unityData,string armatureName, string skinName = "", string textureAtlasName = "", GameObject gameObject = null, bool isUGUI = false)
        {
            _armatureGameObject = gameObject;
            _isUGUI = isUGUI;
            var armature = BuildArmature(unityData, armatureName, skinName, textureAtlasName);

            if (armature != null)
            {
                _dragonBones.clock.Add(armature);

                var armatureDisplay = armature.display as GameObject;
                var armatureComponent = armatureDisplay.GetComponent<UArmatureComponent>();

                return armatureComponent;
            }
            return null;
        }

        public Armature BuildArmature(UDragonBonesData unityData, string armatureName, string dragonBonesName = "", string skinName = null, string textureAtlasName = null)
        {
            var dataPackage = new UBuildArmaturePackage();
            dataPackage.unityData = unityData;
            var armatureData = unityData.dbData.GetArmature(armatureName);
            if (armatureData != null)
            {
                dataPackage.dataName = dragonBonesName;
                dataPackage.textureAtlasName = textureAtlasName;
                dataPackage.data = unityData.dbData;
                dataPackage.armature = armatureData;
                dataPackage.skin = null;
                if (!string.IsNullOrEmpty(skinName))
                {
                    dataPackage.skin = armatureData.GetSkin(skinName);
                }
                if (dataPackage.skin == null)
                {
                    dataPackage.skin = armatureData.defaultSkin;
                }
            }

            var armature = this._BuildArmature(dataPackage);
            this._BuildBones(dataPackage, armature);
            this._BuildSlots(dataPackage, armature);
            this._BuildConstraints(dataPackage, armature);
            armature.InvalidUpdate(null, true);
            // Update armature pose.
            armature.AdvanceTime(0.0f);
            return armature;
        }

        public override void AddTextureAtlasData(TextureAtlasData data, string name = null)
        {
            throw new UnityException("void AddTextureAtlasData(TextureAtlasData data, string name = null)");
        }
        protected override TextureData _GetTextureData(string textureAtlasName, string textureName)
        {
            throw new UnityException("TextureData _GetTextureData(string textureAtlasName, string textureName)");
        }
        protected TextureData _GetTextureData( UDragonBonesData unityData, string textureAtlasName, string textureName)
        {
            if (unityData.textureAtlasData!=null)
            {
                for(int i = 0 ;i<unityData.textureAtlasData.Length;++i){
                    var textureAtlasData = unityData.textureAtlasData[i];
                    var textureData = textureAtlasData.GetTexture(textureName);
                    if (textureData != null)
                    {
                        return textureData;
                    }
                }
            }
            return null;
        }

        protected override object _GetSlotDisplay(BuildArmaturePackage dataPackage, DisplayData displayData, DisplayData rawDisplayData, Slot slot)
        {
            UBuildArmaturePackage uPackage = dataPackage as UBuildArmaturePackage;
            var dataName = dataPackage.dataName;
            object display = null;
            switch (displayData.type)
            {
                case DisplayType.Image:
                    {
                        var imageDisplayData = displayData as ImageDisplayData;
                        if (imageDisplayData.texture == null)
                        {
                            imageDisplayData.texture = this._GetTextureData(uPackage.unityData,dataName, displayData.path);
                        }
                        else if (dataPackage != null && !string.IsNullOrEmpty(dataPackage.textureAtlasName))
                        {
                            imageDisplayData.texture = this._GetTextureData(uPackage.unityData,dataPackage.textureAtlasName, displayData.path);
                        }

                        if (rawDisplayData != null && rawDisplayData.type == DisplayType.Mesh && this._IsSupportMesh())
                        {
                            display = slot.meshDisplay;
                        }
                        else
                        {
                            display = slot.rawDisplay;
                        }
                    }
                    break;
                case DisplayType.Mesh:
                    {
                        var meshDisplayData = displayData as MeshDisplayData;
                        if (meshDisplayData.texture == null)
                        {
                            meshDisplayData.texture = this._GetTextureData(uPackage.unityData,dataName, meshDisplayData.path);
                        }
                        else if (dataPackage != null && !string.IsNullOrEmpty(dataPackage.textureAtlasName))
                        {
                            meshDisplayData.texture = this._GetTextureData(uPackage.unityData,dataPackage.textureAtlasName, meshDisplayData.path);
                        }

                        if (this._IsSupportMesh())
                        {
                            display = slot.meshDisplay;
                        }
                        else
                        {
                            display = slot.rawDisplay;
                        }
                    }
                    break;
                default:
                    display = base._GetSlotDisplay(dataPackage, displayData, rawDisplayData, slot);
                    break;
            }

            return display;
        }


        public ArmatureData GetArmatureData(UDragonBonesData unityData, string armatureName)
        {
            if (unityData.dbData != null)
            {
                return unityData.dbData.GetArmature(armatureName);
            }
            return null;
        }


        protected void _RefreshTextureAtlas(UTextureAtlasData textureAtlasData, bool isUGUI, bool isEditor = false)
        {
            Material material = null;
            if (isUGUI && textureAtlasData.uiTexture == null)
            {
                #if UNITY_EDITOR
                if (isEditor && !Application.isPlaying)
                {
                    material = AssetDatabase.LoadAssetAtPath<Material>(textureAtlasData.imagePath + "_UI_Mat.mat");
                }
                #endif

                if (material == null)
                {
                    Texture2D textureAtlas = null;
                    #if UNITY_EDITOR
                    if (isEditor)
                    {
                        textureAtlas = AssetDatabase.LoadAssetAtPath<Texture2D>(textureAtlasData.imagePath + ".png");
                    }
                    #endif
                    material = UFactoryHelper.GenerateMaterial(defaultUIShaderName, textureAtlas.name + "_UI_Mat", textureAtlas);
                    if (textureAtlasData.width < 2)
                    {
                        textureAtlasData.width = (uint)textureAtlas.width;
                    }

                    if (textureAtlasData.height < 2)
                    {
                        textureAtlasData.height = (uint)textureAtlas.height;
                    }

                    #if UNITY_EDITOR
                    if (!Application.isPlaying)
                    {
                        string path = AssetDatabase.GetAssetPath(textureAtlas);
                        path = path.Substring(0, path.Length - 4);
                        AssetDatabase.CreateAsset(material, path + "_UI_Mat.mat");
                        AssetDatabase.SaveAssets();
                    }
                    #endif
                }

                textureAtlasData.uiTexture = material;
            }
            else if (!isUGUI && textureAtlasData.texture == null)
            {
                if (isEditor)
                {
                    #if UNITY_EDITOR
                    if (!Application.isPlaying)
                    {
                        material = AssetDatabase.LoadAssetAtPath<Material>(textureAtlasData.imagePath + "_Mat.mat");
                    }
                    #endif
                }

                if (material == null)
                {
                    Texture2D textureAtlas = null;
                    if (isEditor)
                    {
                        #if UNITY_EDITOR
                        if (!Application.isPlaying)
                        {
                            textureAtlas = AssetDatabase.LoadAssetAtPath<Texture2D>(textureAtlasData.imagePath + ".png");
                        }
                        #endif
                    }

                    material = UFactoryHelper.GenerateMaterial(defaultShaderName, textureAtlas.name + "_Mat", textureAtlas);
                    if (textureAtlasData.width < 2)
                    {
                        textureAtlasData.width = (uint)textureAtlas.width;
                    }

                    if (textureAtlasData.height < 2)
                    {
                        textureAtlasData.height = (uint)textureAtlas.height;
                    }

                    #if UNITY_EDITOR
                    if (!Application.isPlaying)
                    {
                        string path = AssetDatabase.GetAssetPath(textureAtlas);
                        path = path.Substring(0, path.Length - 4);
                        AssetDatabase.CreateAsset(material, path + "_Mat.mat");
                        AssetDatabase.SaveAssets();
                    }
                    #endif
                }
                textureAtlasData.texture = material;
            }
        }

        /// <inheritDoc/>
        public override void Clear(bool disposeData = true)
        {
            base.Clear(disposeData);

            _armatureGameObject = null;
            _isUGUI = false;
        }

        public IEventDispatcher<EventObject> soundEventManager
        {
            get
            {
                return _dragonBonesInstance.eventManager;
            }
        }
       
        public DragonBonesData LoadData(UDragonBonesData data, bool isUGUI = false, float armatureScale = 0.01f)
        {
            DragonBonesData dragonBonesData = null;

            if (data.dragonBonesJSON != null)
            {
                dragonBonesData = data.dbData == null ? CreateDragonBonesData(data.dragonBonesJSON, data.dataName, armatureScale) : data.dbData;
                data.dbData = dragonBonesData;

                if (dragonBonesData!=null && !string.IsNullOrEmpty(data.dataName) && data.textureAtlas != null)
                {
                    #if UNITY_EDITOR
                    bool isDirty = false;
                    if (!Application.isPlaying)
                    {
                        for (int i = 0; i < data.textureAtlas.Length; ++i)
                        {
                            if (isUGUI)
                            {
                                if (data.textureAtlas[i].uiMaterial == null)
                                {
                                    isDirty = true;
                                    break;
                                }
                            }
                            else
                            {
                                if (data.textureAtlas[i].material == null)
                                {
                                    isDirty = true;
                                    break;
                                }
                            }
                        }
                    }
                    #endif

                    var textureAtlasDatas = data.textureAtlasData;
                    if (textureAtlasDatas != null)
                    {
                        for (int i = 0, l = textureAtlasDatas.Length; i < l; ++i)
                        {
                            if (i < data.textureAtlas.Length)
                            {
                                var textureAtlasData = textureAtlasDatas[i] as UTextureAtlasData;
                                var textureAtlas = data.textureAtlas[i];

                                textureAtlasData.uiTexture = textureAtlas.uiMaterial;
                                textureAtlasData.texture = textureAtlas.material;
                                #if UNITY_EDITOR
                                if (!Application.isPlaying)
                                {
                                    textureAtlasData.imagePath = AssetDatabase.GetAssetPath(textureAtlas.texture);
                                    textureAtlasData.imagePath = textureAtlasData.imagePath.Substring(0, textureAtlasData.imagePath.Length - 4);
                                    _RefreshTextureAtlas(textureAtlasData, isUGUI, true);
                                    if (isUGUI)
                                    {
                                        textureAtlas.uiMaterial = textureAtlasData.uiTexture;
                                    }
                                    else
                                    {
                                        textureAtlas.material = textureAtlasData.texture;
                                    }
                                }
                                #endif
                            }
                        }
                    }
                    else
                    {
                        if (data.textureAtlasData == null || data.textureAtlas.Length != data.textureAtlasData.Length )
                        {
                            data.textureAtlasData = new UTextureAtlasData[data.textureAtlas.Length];
                            for (int i = 0; i < data.textureAtlas.Length; ++i)
                            {
                                data.textureAtlasData[i] = CreateTextureAtlasData(data.textureAtlas[i], data.dataName, data.textureScale, isUGUI);
                            }
                        }
                    }

                    #if UNITY_EDITOR
                    if (isDirty)
                    {
                        AssetDatabase.Refresh();
                        EditorUtility.SetDirty(data);
                        AssetDatabase.SaveAssets();
                    }
                    #endif
                }
            }

            return dragonBonesData;
        }

        public DragonBonesData CreateDragonBonesData(TextAsset dragonBonesJSON, string name = "", float scale = 0.01f)
        {
            DragonBonesData data = null;
            if (dragonBonesJSON.text == "DBDT")
            {
                BinaryDataParser.jsonParseDelegate = MiniJSON.Json.Deserialize;
                data = ParseDragonBonesData(dragonBonesJSON.bytes, name, scale); // Unity default Scale Factor.
            }
            else
            {
                data = ParseDragonBonesData((Dictionary<string, object>)MiniJSON.Json.Deserialize(dragonBonesJSON.text), name, scale); // Unity default Scale Factor.
            }
            name = !string.IsNullOrEmpty(name) ? name : data.name;
            return data;
        }
      
        public UTextureAtlasData CreateTextureAtlasData(UDragonBonesData.UTextureAtlas textureAtlas, string name, float scale = 1.0f, bool isUGUI = false)
        {
            Dictionary<string, object> textureJSONData = (Dictionary<string, object>)MiniJSON.Json.Deserialize(textureAtlas.textureAtlasJSON.text);
            UTextureAtlasData textureAtlasData = _ParseTextureAtlasData(textureJSONData, null, name, scale) as UTextureAtlasData;

            textureAtlasData.width = (uint)textureAtlas.size.x;
            textureAtlasData.height = (uint)textureAtlas.size.y;

            if (textureAtlasData != null)
            {
                textureAtlasData.uiTexture = textureAtlas.uiMaterial;
                textureAtlasData.texture = textureAtlas.material;
                #if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    textureAtlasData.imagePath = AssetDatabase.GetAssetPath(textureAtlas.texture);
                    textureAtlasData.imagePath = textureAtlasData.imagePath.Substring(0, textureAtlasData.imagePath.Length - 4);
                    _RefreshTextureAtlas(textureAtlasData, isUGUI, true);
                    if (isUGUI)
                    {
                        textureAtlas.uiMaterial = textureAtlasData.uiTexture;
                    }
                    else
                    {
                        textureAtlas.material = textureAtlasData.texture;
                    }
                }
                #endif
            }

            return textureAtlasData;
        }


        private TextureAtlasData _ParseTextureAtlasData(Dictionary<string, object> rawData, object textureAtlas, string name = null, float scale = 1.0f)
        {
            var textureAtlasData = this._BuildTextureAtlasData(null, null);
            this._dataParser.ParseTextureAtlasData(rawData, textureAtlasData, scale);
            this._BuildTextureAtlasData(textureAtlasData, textureAtlas);
            return textureAtlasData;
        }

        /// <summary>
        /// Refresh the Armature textureAtlas data.
        /// </summary>
        /// <param name="unityArmature">UArmatureComponent</param>
        /// <version>DragonBones 4.5</version>
        /// <language>en_US</language>

        /// <summary>
        /// 刷新骨架的贴图集数据。
        /// </summary>
        /// <param name="unityArmature">骨架</param>
        /// <version>DragonBones 4.5</version>
        /// <language>zh_CN</language>
        public void RefreshAllTextureAtlas(UArmatureComponent unityArmature)
        {
            if (unityArmature.unityData != null && unityArmature.unityData.textureAtlas != null)
            {
                for (int i = 0; i < unityArmature.unityData.textureAtlas.Length; ++i)
                {
                    _RefreshTextureAtlas(unityArmature.unityData.textureAtlasData[i], unityArmature._isUGUI);
                }
            }
        }
        /// <private/>
        public override void ReplaceDisplay(Slot slot, DisplayData displayData, int displayIndex = -1)
        {
            //UGUI Display Object and Normal Display Object cannot be replaced with each other
            if (displayData.type == DisplayType.Image || displayData.type == DisplayType.Mesh)
            {
                var dataName = displayData.parent.parent.parent.name;
                var textureData = this._GetTextureData(dataName, displayData.path);
                if (textureData != null)
                {
                    var textureAtlasData = textureData.parent as UTextureAtlasData;
                    var oldIsUGUI = (slot._armature.proxy as UArmatureComponent)._isUGUI;
                    if ((oldIsUGUI && textureAtlasData.uiTexture == null) || (!oldIsUGUI && textureAtlasData.texture == null))
                    {
                        ULogHelper.LogWarning("ugui display object and normal display object cannot be replaced with each other");
                        return;
                    }
                }
            }
            base.ReplaceDisplay(slot, displayData, displayIndex);
        }


        public void ReplaceDisplayBytexture(UDragonBonesData unityData, string armatureName, string slotName, string displayName,
            Slot slot, Texture2D texture, Material material = null,
            bool isUGUI = false, int displayIndex = -1)
        {
            var armatureData = this.GetArmatureData(unityData,armatureName);
            if (armatureData == null || armatureData.defaultSkin == null)
            {
                return;
            }

            var displays = armatureData.defaultSkin.GetDisplays(slotName);
            if (displays == null)
            {
                return;
            }

            DisplayData prevDispalyData = null;
            foreach (var displayData in displays)
            {
                if (displayData.name == displayName)
                {
                    prevDispalyData = displayData;
                    break;
                }
            }

            if (prevDispalyData == null || !((prevDispalyData is ImageDisplayData) || (prevDispalyData is MeshDisplayData)))
            {
                return;
            }

            TextureData prevTextureData = null;
            if(prevDispalyData is ImageDisplayData)
            {
                prevTextureData = (prevDispalyData as ImageDisplayData).texture;
            }
            else
            {
                prevTextureData = (prevDispalyData as MeshDisplayData).texture;
            }

            UTextureData newTextureData = new UTextureData();
            newTextureData.CopyFrom(prevTextureData);
            newTextureData.rotated = false;
            newTextureData.region.x = 0.0f;
            newTextureData.region.y = 0.0f;
            newTextureData.region.width = texture.width;
            newTextureData.region.height = texture.height;
            newTextureData.frame = newTextureData.region;
            newTextureData.name = prevTextureData.name;
            newTextureData.parent = new UTextureAtlasData();
            newTextureData.parent.width = (uint)texture.width;
            newTextureData.parent.height = (uint)texture.height;
            newTextureData.parent.scale = prevTextureData.parent.scale;

            if (material == null)
            {
                if (isUGUI)
                {
                    material = UFactoryHelper.GenerateMaterial(defaultUIShaderName, texture.name + "_UI_Mat", texture);
                }
                else
                {
                    material = UFactoryHelper.GenerateMaterial(defaultShaderName, texture.name + "_Mat", texture);
                }
            }

            if (isUGUI)
            {
                (newTextureData.parent as UTextureAtlasData).uiTexture = material;
            }
            else
            {
                (newTextureData.parent as UTextureAtlasData).texture = material;
            }

            material.mainTexture = texture;

            DisplayData newDisplayData = null;
            if (prevDispalyData is ImageDisplayData)
            {
                newDisplayData = new ImageDisplayData();
                newDisplayData.type = prevDispalyData.type;
                newDisplayData.name = prevDispalyData.name;
                newDisplayData.path = prevDispalyData.path;
                newDisplayData.transform.CopyFrom(prevDispalyData.transform);
                newDisplayData.parent = prevDispalyData.parent;
                (newDisplayData as ImageDisplayData).pivot.CopyFrom((prevDispalyData as ImageDisplayData).pivot);
                (newDisplayData as ImageDisplayData).texture = newTextureData;
            }
            else if (prevDispalyData is MeshDisplayData)
            {
                newDisplayData = new MeshDisplayData();
                newDisplayData.type = prevDispalyData.type;
                newDisplayData.name = prevDispalyData.name;
                newDisplayData.path = prevDispalyData.path;
                newDisplayData.transform.CopyFrom(prevDispalyData.transform);
                newDisplayData.parent = prevDispalyData.parent;
                (newDisplayData as MeshDisplayData).texture = newTextureData;

                (newDisplayData as MeshDisplayData).vertices.inheritDeform = (prevDispalyData as MeshDisplayData).vertices.inheritDeform;
                (newDisplayData as MeshDisplayData).vertices.offset = (prevDispalyData as MeshDisplayData).vertices.offset;
                (newDisplayData as MeshDisplayData).vertices.data = (prevDispalyData as MeshDisplayData).vertices.data;
                (newDisplayData as MeshDisplayData).vertices.weight = (prevDispalyData as MeshDisplayData).vertices.weight;
            }

            ReplaceDisplay(slot, newDisplayData, displayIndex);
        }


        public override bool ReplaceSlotDisplay(string dragonBonesName,
            string armatureName,
            string slotName,
            string displayName,
            Slot slot, int displayIndex = -1)
        {
            throw new UnityException("Please Use 'ReplaceSlotDisplay(ArmatureData armatureData, string slotName, string displayName, Slot slot, int displayIndex = -1)'");
        }
        /// <private/>
        public override bool ReplaceSlotDisplayList(string dragonBonesName, string armatureName, string slotName, Slot slot)
        {
            throw new UnityException("Please Use 'ReplaceSlotDisplayList(ArmatureData armatureData, string slotName, Slot slot)'");
        }

        public bool ReplaceSlotDisplay(ArmatureData armatureData, string slotName, string displayName, Slot slot, int displayIndex = -1)
        {
            if (armatureData == null || armatureData.defaultSkin == null)
            {
                return false;
            }

            var displayData = armatureData.defaultSkin.GetDisplay(slotName, displayName);
            if (displayData == null)
            {
                return false;
            }

            this.ReplaceDisplay(slot, displayData, displayIndex);
            return true;
        }

        public bool ReplaceSlotDisplayList(ArmatureData armatureData, string slotName, Slot slot)
        {
            if (armatureData == null || armatureData.defaultSkin == null)
            {
                return false;
            }

            var displays = armatureData.defaultSkin.GetDisplays(slotName);
            if (displays == null)
            {
                return false;
            }

            var displayIndex = 0;
            // for (const displayData of displays) 
            for (int i = 0, l = displays.Count; i < l; ++i)
            {
                var displayData = displays[i];
                this.ReplaceDisplay(slot, displayData, displayIndex++);
            }

            return true;
        }
    }

    /// <summary>
    /// UnityFactory 辅助类
    /// </summary>
    internal static class UFactoryHelper
    {
        internal static Material GenerateMaterial(string shaderName, string materialName, Texture texture)
        {
            //创建材质球
            Shader shader = Shader.Find(shaderName);
            Material material = new Material(shader);
            material.name = materialName;
            material.mainTexture = texture;

            return material;
        }
        internal static void DestroyUnityObject(UnityEngine.Object obj)
        {
            if (obj == null)
            {
                return;
            }

            #if UNITY_EDITOR
            UnityEngine.Object.DestroyImmediate(obj);
            #else
            UnityEngine.Object.Destroy(obj);
            #endif
        }
    }

    internal static class ULogHelper
    {
        internal static void LogWarning(object message)
        {
            UnityEngine.Debug.LogWarning("[DragonBones]" + message);
        }
    }
}