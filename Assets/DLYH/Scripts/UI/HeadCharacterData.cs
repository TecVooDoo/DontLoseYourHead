using UnityEngine;

namespace DLYH.TableUI
{
    [CreateAssetMenu(fileName = "HeadCharacterData", menuName = "DLYH/Head Character Data")]
    public class HeadCharacterData : ScriptableObject
    {
        public HeadCharacter[] Characters;
    }

    [System.Serializable]
    public class HeadCharacter
    {
        public string Name;
        public Texture2D HeadTexture;
        public Texture2D HairTexture;
        public Texture2D HairBackTexture;
        [Tooltip("6 faces: 1=Neutral, 2=Worried, 3=Scared, 4=Horrified, 5=Dead, 6=Evil")]
        public Texture2D[] FaceTextures;
    }
}
