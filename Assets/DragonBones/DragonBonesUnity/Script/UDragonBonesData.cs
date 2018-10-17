using UnityEngine;
using System.Collections.Generic;

namespace DragonBones
{
    [System.Serializable]
    public class UDragonBonesData : ScriptableObject
    {
        [System.Serializable]
        public class UTextureAtlas
        {
            public TextAsset textureAtlasJSON; 
            public Texture2D texture;
            public Vector2 size=new Vector2(2048,2048);
            public Material material;
            public Material uiMaterial;
        }

        public string dataName;
        public TextAsset dragonBonesJSON;
        public UTextureAtlas[] textureAtlas;
        public float textureScale = 1f;

        public DragonBonesData dbData;
        public UTextureAtlasData[] textureAtlasData;

        /// <summary>
        /// Gets the armature data.
        /// </summary>
        /// <returns>The armature data.</returns>
        /// <param name="armatureName">Armature name.</param>
        public ArmatureData GetArmatureData(string armatureName){
            if (dbData != null)
            {
                return dbData.GetArmature(armatureName);
            }
            return null;
        }
    }
}
