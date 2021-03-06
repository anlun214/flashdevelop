using System;
using System.Xml.Serialization;

namespace ScintillaNet.Configuration
{
    [Serializable]
    public class CharacterClass : ConfigItem
    {
        [XmlAttribute]
        public string name;

        [XmlAttribute("inherit")]
        public string inherit;

        [XmlText]
        public string val = "qwertyuiopasdfghjklzxcvbnmQWERTYUIOPASDFGHJKLZXCVBNM0123456789_";

        public string Characters
        {
            get
            {
                var result = val;
                if (!string.IsNullOrEmpty(inherit))
                {
                    var cc = _parent.MasterScintilla.GetCharacterClass(inherit);         
                    if (cc != null) result += cc.Characters;
                }
                return result;
            }
        }
    }
}