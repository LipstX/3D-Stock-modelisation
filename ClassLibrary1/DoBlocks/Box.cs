using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace DoBlocks
{
    [Serializable]
    public class Box
    {
        [XmlElement("BlckName")]
        public string BlckName { get; set; }

        [XmlElement("Type")]
        public int Type { get; set; }

        [XmlElement("Ref")]
        public int Ref { get; set; }

        [XmlElement("Rank")]
        public int Rank { get; set; }

        [XmlElement("Line")]
        public int Line { get; set; }

        [XmlElement("Pos")]
        public int Pos { get; set; }

        [XmlElement("Height")]
        public int Height { get; set; }

        [XmlElement("Width")]
        public int Width { get; set; }

        [XmlElement("Deep")]
        public int Deep { get; set; }

        [XmlElement("Double")]
        public int Double { get; set; }

        #region ICloneable Members
        public object Clone()
        {
            Box boxBck = this;
            return boxBck;
        }
        #endregion


    }

    [Serializable]
    [XmlRoot("BoxCollection")]
    public class BoxCollection
    {
        [XmlArray("Boxs")]
        [XmlArrayItem("Box", typeof(Box))]
        public List<Box> ArrayBox { get; set; }

    }
}
