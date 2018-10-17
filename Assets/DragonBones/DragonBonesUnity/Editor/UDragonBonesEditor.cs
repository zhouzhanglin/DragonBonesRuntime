using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Text.RegularExpressions;

namespace DragonBones
{
    public class UDragonBonesEditor
    {
        [MenuItem("DragonBones/Armature Object", false, 10)]
        private static void _CreateArmatureObjectMenuItem()
        {
            _CreateEmptyObject(GetSelectionParentTransform());
        }

        [MenuItem("Assets/DragonBones/Armature Object", true)]
        private static bool _CreateArmatureObjectFromSkeValidateMenuItem()
        {
            return _GetDragonBonesSkePaths().Count > 0;
        }

        [MenuItem("Assets/DragonBones/Armature Object", false, 10)]
        private static void _CreateArmatureObjectFromSkeMenuItem()
        {
            var parentTransform = GetSelectionParentTransform();
            foreach (var dragonBonesJSONPath in _GetDragonBonesSkePaths())
            {
                var armatureComponent = _CreateEmptyObject(parentTransform);
                var dragonBonesJSON = AssetDatabase.LoadMainAssetAtPath(dragonBonesJSONPath) as TextAsset;

                ChangeDragonBonesData(armatureComponent, dragonBonesJSON);
            }
        }

        [MenuItem("DragonBones/Armature Object(UGUI)", false, 11)]
        private static void _CreateUGUIArmatureObjectMenuItem()
        {
            var armatureComponent = _CreateEmptyObject(GetSelectionParentTransform());
            armatureComponent.IsUGUI = true;
            if (armatureComponent.GetComponentInParent<Canvas>() == null)
            {
                var canvas = GameObject.Find("/Canvas");
                if (canvas)
                {
                    armatureComponent.transform.SetParent(canvas.transform);
                }
            }

            armatureComponent.transform.localScale = Vector2.one * 100.0f;
            armatureComponent.transform.localPosition = Vector3.zero;
        }

        [MenuItem("Assets/DragonBones/Armature Object(UGUI)", true)]
        private static bool _CreateUGUIArmatureObjectFromJSONValidateMenuItem()
        {
            return _GetDragonBonesSkePaths().Count > 0;
        }

        [MenuItem("Assets/DragonBones/Armature Object(UGUI)", false, 11)]
        private static void _CreateUGUIArmatureObjectFromJSOIMenuItem()
        {
            var parentTransform = GetSelectionParentTransform();
            foreach (var dragonBonesJSONPath in _GetDragonBonesSkePaths())
            {
                var armatureComponent = _CreateEmptyObject(parentTransform);
                armatureComponent.IsUGUI = true;
                if (armatureComponent.GetComponentInParent<Canvas>() == null)
                {
                    var canvas = GameObject.Find("/Canvas");
                    if (canvas)
                    {
                        armatureComponent.transform.SetParent(canvas.transform);
                    }
                }
                armatureComponent.transform.localScale = Vector2.one * 100.0f;
                armatureComponent.transform.localPosition = Vector3.zero;
                var dragonBonesJSON = AssetDatabase.LoadMainAssetAtPath(dragonBonesJSONPath) as TextAsset;

                ChangeDragonBonesData(armatureComponent, dragonBonesJSON);
            }
        }


        [MenuItem("Assets/DragonBones/Create Unity Data", true)]
        private static bool _CreateUnityDataValidateMenuItem()
        {
            return _GetDragonBonesSkePaths(true).Count > 0;
        }

        [MenuItem("Assets/DragonBones/Create Unity Data", false, 32)]
        private static void _CreateUnityDataMenuItem()
        {
            foreach (var dragonBonesSkePath in _GetDragonBonesSkePaths(true))
            {
                var dragonBonesSke = AssetDatabase.LoadMainAssetAtPath(dragonBonesSkePath) as TextAsset;
                var textureAtlasJSONs = new List<string>();
                GetTextureAtlasConfigs(textureAtlasJSONs, AssetDatabase.GetAssetPath(dragonBonesSke.GetInstanceID()));
                UDragonBonesData.UTextureAtlas[] textureAtlas = new UDragonBonesData.UTextureAtlas[textureAtlasJSONs.Count];

                for (int i = 0; i < textureAtlasJSONs.Count; ++i)
                {
                    string path = textureAtlasJSONs[i];
                    //load textureAtlas data
                    UDragonBonesData.UTextureAtlas ta = new UDragonBonesData.UTextureAtlas();
                    ta.textureAtlasJSON = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
                    //load texture
                    path = path.Substring(0, path.LastIndexOf(".json"));
                    ta.texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path + ".png");

                    //texture size
                    string filePath = Application.dataPath + "/"+path.Substring(7)+".png";
                    if (File.Exists(filePath))
                    {
                        byte[] fileData = File.ReadAllBytes(filePath);
                        Texture2D tex = new Texture2D(2, 2);
                        tex.LoadImage(fileData);
                        if (tex)
                        {
                            ta.size = new Vector2(tex.width, tex.height);
                        }
                    }

                    //load material
                    ta.material = AssetDatabase.LoadAssetAtPath<Material>(path + "_Mat.mat");
                    ta.uiMaterial = AssetDatabase.LoadAssetAtPath<Material>(path + "_UI_Mat.mat");
                    textureAtlas[i] = ta;
                }

                //
                CreateUDragonBonesData(dragonBonesSke, textureAtlas);
            }
        }

        public static UDragonBonesData.UTextureAtlas[] GetTextureAtlasByJSONs(List<string> textureAtlasJSONs)
        {
            UDragonBonesData.UTextureAtlas[] textureAtlas = new UDragonBonesData.UTextureAtlas[textureAtlasJSONs.Count];

            for (int i = 0; i < textureAtlasJSONs.Count; ++i)
            {
                string path = textureAtlasJSONs[i];
                //load textureAtlas data
                UDragonBonesData.UTextureAtlas ta = new UDragonBonesData.UTextureAtlas();
                ta.textureAtlasJSON = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
                //load texture
                path = path.Substring(0, path.LastIndexOf(".json"));
                ta.texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path + ".png");

                //texture size
                string filePath = Application.dataPath + "/"+path.Substring(7)+".png";
                if (File.Exists(filePath))
                {
                    byte[] fileData = File.ReadAllBytes(filePath);
                    Texture2D tex = new Texture2D(2, 2);
                    tex.LoadImage(fileData);
                    if (tex)
                    {
                        ta.size = new Vector2(tex.width, tex.height);
                    }
                }

                //load material
                ta.material = AssetDatabase.LoadAssetAtPath<Material>(path + "_Mat.mat");
                ta.uiMaterial = AssetDatabase.LoadAssetAtPath<Material>(path + "_UI_Mat.mat");
                textureAtlas[i] = ta;
            }

            return textureAtlas;
        }


        public static bool ChangeDragonBonesData(UArmatureComponent _armatureComponent, TextAsset dragonBoneJSON)
        {
            if (dragonBoneJSON != null)
            {
                var textureAtlasJSONs = new List<string>();
                UDragonBonesEditor.GetTextureAtlasConfigs(textureAtlasJSONs, AssetDatabase.GetAssetPath(dragonBoneJSON.GetInstanceID()));

                UDragonBonesData.UTextureAtlas[] textureAtlas = GetTextureAtlasByJSONs(textureAtlasJSONs);

                UDragonBonesData data = CreateUDragonBonesData(dragonBoneJSON, textureAtlas);

                data.dbData = null;
                DragonBonesData dragonBonesData = UFactory.factory.LoadData(data, _armatureComponent.IsUGUI);
                data.dbData = dragonBonesData;

                if (dragonBonesData != null)
                {
                    Undo.RecordObject(_armatureComponent, "Set DragonBones");

                    _armatureComponent.unityData = data;

                    var armatureName = dragonBonesData.armatureNames[0];
                    ChangeArmatureData(_armatureComponent, armatureName, _armatureComponent.unityData.dataName);

                    _armatureComponent.gameObject.name = armatureName;

                    EditorUtility.SetDirty(_armatureComponent);

                    return true;
                }
                else
                {
                    data.dbData = null;
                    EditorUtility.DisplayDialog("Error", "Could not load dragonBones data.", "OK", null);
                    return false;
                }
            }
            else if (_armatureComponent.unityData != null)
            {
                Undo.RecordObject(_armatureComponent, "Set DragonBones");

                _armatureComponent.unityData = null;

                if (_armatureComponent.armature != null)
                {
                    _armatureComponent.Dispose(false);
                }

                EditorUtility.SetDirty(_armatureComponent);

                return true;
            }

            return false;
        }

        public static void ChangeArmatureData(UArmatureComponent _armatureComponent, string armatureName, string dragonBonesName)
        {
            UDragonBonesData unityData = _armatureComponent.unityData;
            bool isUGUI = _armatureComponent.IsUGUI;
            Slot slot = null;
            if (_armatureComponent.armature != null)
            {
                slot = _armatureComponent.armature.parent;

                _armatureComponent.Dispose(false);

                UFactory.factory._dragonBones.AdvanceTime(0.0f);
            }

            _armatureComponent.ArmatureName = armatureName;
            _armatureComponent.IsUGUI = isUGUI;

            _armatureComponent.unityData = unityData;
            _armatureComponent = UFactory.factory.BuildArmatureComponent(unityData,armatureName, null, unityData.dataName, _armatureComponent.gameObject,  isUGUI);
            if (slot != null)
            {
                slot.childArmature = _armatureComponent.armature;
            }

            _armatureComponent.sortingLayerName = _armatureComponent.sortingLayerName;
            _armatureComponent.sortingOrder = _armatureComponent.sortingOrder;
        }

        public static UnityEngine.Transform GetSelectionParentTransform()
        {
            var parent = Selection.activeObject as GameObject;
            return parent != null ? parent.transform : null;
        }

        public static void GetTextureAtlasConfigs(List<string> textureAtlasFiles, string filePath, string rawName = null, string suffix = "tex")
        {
            var folder = Directory.GetParent(filePath).ToString();

            var name = rawName != null ? rawName : filePath.Substring(0, filePath.LastIndexOf(".")).Substring(filePath.LastIndexOf("/") + 1);
            if (name.LastIndexOf("_ske") == name.Length - 4)
            {
                name = name.Substring(0, name.LastIndexOf("_ske"));
            }
            int index = 0;
            var textureAtlasName = "";
            var textureAtlasConfigFile = "";

            textureAtlasName = !string.IsNullOrEmpty(name) ? name + (!string.IsNullOrEmpty(suffix) ? "_" + suffix : suffix) : suffix;
            textureAtlasConfigFile = folder + "/" + textureAtlasName + ".json";

            if (File.Exists(textureAtlasConfigFile))
            {
                textureAtlasFiles.Add(textureAtlasConfigFile);
                return;
            }

            while (true)
            {
                textureAtlasName = (!string.IsNullOrEmpty(name) ? name + (!string.IsNullOrEmpty(suffix) ? "_" + suffix : suffix) : suffix) + "_" + (index++);
                textureAtlasConfigFile = folder + "/" + textureAtlasName + ".json";
                if (File.Exists(textureAtlasConfigFile))
                {
                    textureAtlasFiles.Add(textureAtlasConfigFile);
                }
                else if (index > 1)
                {
                    break;
                }
            }

            if (textureAtlasFiles.Count > 0 || rawName != null)
            {
                return;
            }

            GetTextureAtlasConfigs(textureAtlasFiles, filePath, "", suffix);
            if (textureAtlasFiles.Count > 0)
            {
                return;
            }

            index = name.LastIndexOf("_");
            if (index >= 0)
            {
                name = name.Substring(0, index);

                GetTextureAtlasConfigs(textureAtlasFiles, filePath, name, suffix);
                if (textureAtlasFiles.Count > 0)
                {
                    return;
                }

                GetTextureAtlasConfigs(textureAtlasFiles, filePath, name, "");
                if (textureAtlasFiles.Count > 0)
                {
                    return;
                }
            }

            if (suffix != "texture")
            {
                GetTextureAtlasConfigs(textureAtlasFiles, filePath, null, "texture");
            }
        }

        public static UDragonBonesData CreateUDragonBonesData(TextAsset dragonBonesAsset, UDragonBonesData.UTextureAtlas[] textureAtlas)
        {
            if (dragonBonesAsset != null)
            {
                bool isDirty = false;
                string path = AssetDatabase.GetAssetPath(dragonBonesAsset);
                path = path.Substring(0, path.Length - 5);
                int index = path.LastIndexOf("_ske");
                if (index > 0)
                {
                    path = path.Substring(0, index);
                }
                //
                string dataPath = path + ".asset";

                var jsonObject = (Dictionary<string, object>)MiniJSON.Json.Deserialize(dragonBonesAsset.text);
                if (dragonBonesAsset.text == "DBDT")
                {
                    int headerLength  = 0;
                    jsonObject = BinaryDataParser.DeserializeBinaryJsonData(dragonBonesAsset.bytes, out headerLength);
                }
                else
                {
                    jsonObject = MiniJSON.Json.Deserialize(dragonBonesAsset.text) as Dictionary<string, object>;
                }

                var dataName = jsonObject.ContainsKey("name") ? jsonObject["name"] as string : "";

                //先从缓存里面取
                UDragonBonesData data = AssetDatabase.LoadAssetAtPath<UDragonBonesData>(dataPath);

                //资源里面也没有，那么重新创建
                if (data == null)
                {
                    data = UDragonBonesData.CreateInstance<UDragonBonesData>();
                    data.dataName = dataName;
                    AssetDatabase.CreateAsset(data, dataPath);
                    isDirty = true;
                }

                //
                if (string.IsNullOrEmpty(data.dataName) || !data.dataName.Equals(dataName))
                {
                    //走到这里，说明原先已经创建了，之后手动改了名字,既然又走了创建流程，那么名字也重置下
                    data.dataName = dataName;
                    isDirty = true;
                }

                if (data.dragonBonesJSON != dragonBonesAsset)
                {
                    data.dragonBonesJSON = dragonBonesAsset;
                    isDirty = true;
                }

                if (textureAtlas != null && textureAtlas.Length > 0 && textureAtlas[0] != null && textureAtlas[0].texture != null)
                {
                    if (data.textureAtlas == null || data.textureAtlas.Length != textureAtlas.Length)
                    {
                        isDirty = true;
                    }
                    else
                    {
                        for (int i = 0; i < textureAtlas.Length; ++i)
                        {
                            if (textureAtlas[i].material != data.textureAtlas[i].material ||
                                textureAtlas[i].uiMaterial != data.textureAtlas[i].uiMaterial ||
                                textureAtlas[i].texture != data.textureAtlas[i].texture ||
                                textureAtlas[i].textureAtlasJSON != data.textureAtlas[i].textureAtlasJSON
                            )
                            {
                                isDirty = true;
                                break;
                            }
                        }
                    }
                    data.textureAtlas = textureAtlas;
                }

                if (isDirty)
                {
                    AssetDatabase.Refresh();
                    EditorUtility.SetDirty(data);
                }

                AssetDatabase.SaveAssets();
                return data;
            }
            return null;
        }


        private static List<string> _GetDragonBonesSkePaths(bool isCreateUnityData = false)
        {
            var dragonBonesSkePaths = new List<string>();
            foreach (var guid in Selection.assetGUIDs)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (assetPath.EndsWith(".json"))
                {
                    var jsonCode = File.ReadAllText(assetPath);
                    if (jsonCode.IndexOf("\"armature\":") > 0)
                    {
                        dragonBonesSkePaths.Add(assetPath);
                    }
                }
                if (assetPath.EndsWith(".bytes"))
                {
                    TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(assetPath);
                    if (asset && asset.text == "DBDT")
                    {
                        dragonBonesSkePaths.Add(assetPath);
                    }
                }
                else if (!isCreateUnityData && assetPath.EndsWith(".asset"))
                {
                    UDragonBonesData data = AssetDatabase.LoadAssetAtPath<UDragonBonesData>(assetPath);

                    dragonBonesSkePaths.Add(AssetDatabase.GetAssetPath(data.dragonBonesJSON));
                }
            }

            return dragonBonesSkePaths;
        }


        private static UArmatureComponent _CreateEmptyObject(UnityEngine.Transform parentTransform)
        {
            var gameObject = new GameObject("New Armature Object", typeof(UArmatureComponent));
            var armatureComponent = gameObject.GetComponent<UArmatureComponent>();
            gameObject.transform.SetParent(parentTransform, false);

            EditorUtility.FocusProjectWindow();
            Selection.activeObject = gameObject;
            EditorGUIUtility.PingObject(Selection.activeObject);
            Undo.RegisterCreatedObjectUndo(gameObject, "Create Armature Object");
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            return armatureComponent;
        }

    }
}