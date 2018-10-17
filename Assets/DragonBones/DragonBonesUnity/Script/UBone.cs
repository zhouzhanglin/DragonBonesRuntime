using UnityEngine;
using System.Collections;

namespace DragonBones
{
	[DisallowMultipleComponent]
	public class UBone :MonoBehaviour  {
		
		private static Vector3 _helpVector3 = new Vector3();
		internal UArmatureComponent _proxy;
		internal Bone _bone;
		public Bone bone{
			get{return _bone;}
		}

		[SerializeField]
		private GameObject _parent = null;

		/**
		 * 获取父骨骼
		 * 
		 */ 
		public GameObject GetParentGameObject(){
			if(_parent) return _parent;
			if(_bone!=null && _bone.parent!=null){
                UnityEngine.Transform child = transform.parent.Find(_bone.parent.name);
                if(child) _parent = child.gameObject;
                if(_proxy._boneHierarchy && _parent){
					transform.SetParent(_parent.transform);
				}
			}
			return _parent;
		}

		internal void _Update(){
            if(_bone!=null && _proxy!=null && _proxy.armature!=null)
			{
				GameObject parent = null;
                if(_proxy._boneHierarchy){
					parent = GetParentGameObject();
                    if(parent && transform.parent!=_proxy.bonesRoot.transform ) transform.SetParent(_proxy.bonesRoot.transform);
				}else if(transform.parent!=_proxy.bonesRoot){
					transform.SetParent(_proxy.bonesRoot.transform);
				}

				Armature armature = _proxy.armature;

				var flipX = armature.flipX;
				var flipY = armature.flipY;
				var scaleX = flipX ? -_bone.global.scaleX : _bone.global.scaleX;
				var scaleY = flipY ? -_bone.global.scaleY : _bone.global.scaleY;

				_helpVector3.x = _bone.globalTransformMatrix.tx;
				_helpVector3.y = -_bone.globalTransformMatrix.ty;

				if (flipX)
				{
					_helpVector3.x = -_helpVector3.x;
				}
				if (flipY)
				{
					_helpVector3.y = -_helpVector3.y;
				}
				_helpVector3.z = 0f;
				transform.localPosition = _helpVector3;

				if (scaleY >= 0.0f )
				{
					_helpVector3.x = 0.0f;
				}
				else
				{
					_helpVector3.x = 180.0f;
				}

				if (scaleX >= 0.0f)
				{
					_helpVector3.y = 0.0f;
				}
				else
				{
					_helpVector3.y = 180.0f;
				}

                _helpVector3.z = -_bone.global.skew*Mathf.Rad2Deg;
				transform.localEulerAngles = _helpVector3;

				_helpVector3.x = scaleX >= 0.0f ? scaleX : -scaleX;
				_helpVector3.y = scaleY >= 0.0f ? scaleY : -scaleY;
				_helpVector3.z = 1f;

				transform.localScale = _helpVector3;

                if(_proxy._boneHierarchy && parent && transform.parent != parent.transform ) transform.SetParent(parent.transform);
			}
		}

	}
}