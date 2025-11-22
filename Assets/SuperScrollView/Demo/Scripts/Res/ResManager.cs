// 11/22/2025 AI-Tag
// This was created with the help of Assistant, a Unity Artificial Intelligence product.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SuperScrollView
{
    public class ResManager : MonoBehaviour
    {
        static ResManager instance = null;

        public static ResManager Get
        {
            get
            {
                if (instance == null)
                {
                    instance = Object.FindFirstObjectByType<ResManager>();
                }
                return instance;
            }
        }

        public Sprite[] spriteObjArray;

        public string GetSpriteNameByIndex(int index)
        {
            if (index < 0 || index >= spriteObjArray.Length)
            {
                return "";
            }
            return spriteObjArray[index].name;
        }

        public Sprite GetSpriteByName(string spriteName)
        {
            int count = spriteObjArray.Length;
            for (int i = 0; i < count; ++i)
            {
                if (spriteObjArray[i].name == spriteName)
                {
                    return spriteObjArray[i];
                }
            }
            return null;
        }
    }
}