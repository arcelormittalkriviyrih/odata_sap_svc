using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;

namespace SAPWebApi.Models
{
    /// <summary>	A SAP parameter. </summary>
    [XmlRoot(ElementName = "SAPParameter")]
    public class SAPParameter
    {
        /// <summary>	Gets or sets the name. </summary>
        ///
        /// <value>	The name. </value>
        [XmlAttribute(AttributeName = "Name")]
        [KeyAttribute]
        public string Name { get; set; }

        /// <summary>	Gets or sets the value. </summary>
        ///
        /// <value>	The value. </value>
        [XmlText]
        public string Value { get; set; }
    }
}