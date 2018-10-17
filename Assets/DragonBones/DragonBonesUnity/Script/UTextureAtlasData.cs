using System.Collections.Generic;
using UnityEngine;

namespace DragonBones
{
    public class UTextureAtlasData : TextureAtlasData
    {
        public Material texture;
        public Material uiTexture;
   
        public override TextureData CreateTexture()
        {
            return BaseObject.BorrowObject<UTextureData>();
        }
    }

    internal class UTextureData : TextureData
    {
        public const string SHADER_PATH = "Shaders/";
        public const string SHADER_GRAP = "DB_BlendMode_Grab";
        public const string SHADER_FRAME_BUFFER = "DB_BlendMode_Framebuffer";
        public const string UI_SHADER_GRAP = "DB_BlendMode_UIGrab";
        public const string UI_SHADER_FRAME_BUFFER = "DB_BlendMode_UIFramebuffer";

        /// <summary>
        /// 叠加模式材质球的缓存池
        /// </summary>
        internal Dictionary<string, Material> _cacheBlendModeMats = new Dictionary<string, Material>();

        protected override void _OnClear()
        {
            base._OnClear();
            this._cacheBlendModeMats.Clear();
        }

        private Material _GetMaterial(BlendMode blendMode)
        {
            //normal model, return the parent shareMaterial
            if (blendMode == BlendMode.Normal)
            {
                return (this.parent as UTextureAtlasData).texture;
            }

            var blendModeStr = blendMode.ToString();

            if (this._cacheBlendModeMats.ContainsKey(blendModeStr))
            {
                return this._cacheBlendModeMats[blendModeStr];
            }

            var newMaterial = new Material(Resources.Load<Shader>(SHADER_PATH + SHADER_GRAP));
            var texture = (this.parent as UTextureAtlasData).texture.mainTexture;
            newMaterial.name = texture.name + "_" + SHADER_GRAP + "_Mat";
            newMaterial.hideFlags = HideFlags.HideAndDontSave;
            newMaterial.mainTexture = texture;

            this._cacheBlendModeMats.Add(blendModeStr, newMaterial);

            return newMaterial;
        }

        private Material _GetUIMaterial(BlendMode blendMode)
        {
            //normal model, return the parent shareMaterial
            if (blendMode == BlendMode.Normal)
            {
                return (this.parent as UTextureAtlasData).uiTexture;
            }

            var blendModeStr = "UI_" + blendMode.ToString();

            if (this._cacheBlendModeMats.ContainsKey(blendModeStr))
            {
                return this._cacheBlendModeMats[blendModeStr];
            }

            var newMaterial = new Material(Resources.Load<Shader>(SHADER_PATH + UI_SHADER_GRAP));
            var texture = (this.parent as UTextureAtlasData).uiTexture.mainTexture;
            newMaterial.name = texture.name + "_" + SHADER_GRAP + "_Mat";
            newMaterial.hideFlags = HideFlags.HideAndDontSave;
            newMaterial.mainTexture = texture;

            this._cacheBlendModeMats.Add(blendModeStr, newMaterial);

            return newMaterial;
        }

        internal Material GetMaterial(BlendMode blendMode, bool isUGUI = false)
        {
            if (isUGUI)
            {
                return _GetUIMaterial(blendMode);
            }
            else
            {
                return _GetMaterial(blendMode);
            }
        }

        public override void CopyFrom(TextureData value)
        {
            base.CopyFrom(value);

            (value as UTextureData)._cacheBlendModeMats = this._cacheBlendModeMats;
        }
    }
}