using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace CreditCardRealTimeDeposit
{
    public class PosRequestResponse
    {
        [XmlRoot(ElementName = "Response", Namespace = "http://schemas.compassplus.com/two/1.0/fimi.xsd")]
        public class Response
        {
            [XmlElement(ElementName = "ApprovalCode", Namespace = "http://schemas.compassplus.com/two/1.0/fimi_types.xsd")]
            public string ApprovalCode { get; set; }
            [XmlElement(ElementName = "AuthRespCode", Namespace = "http://schemas.compassplus.com/two/1.0/fimi_types.xsd")]
            public string AuthRespCode { get; set; }
            [XmlElement(ElementName = "AvailBalance", Namespace = "http://schemas.compassplus.com/two/1.0/fimi_types.xsd")]
            public string AvailBalance { get; set; }
            [XmlElement(ElementName = "BalanceCurrency", Namespace = "http://schemas.compassplus.com/two/1.0/fimi_types.xsd")]
            public string BalanceCurrency { get; set; }
            [XmlElement(ElementName = "BonusDebt", Namespace = "http://schemas.compassplus.com/two/1.0/fimi_types.xsd")]
            public string BonusDebt { get; set; }
            [XmlElement(ElementName = "CVxOK", Namespace = "http://schemas.compassplus.com/two/1.0/fimi_types.xsd")]
            public string CVxOK { get; set; }
            [XmlElement(ElementName = "Currency", Namespace = "http://schemas.compassplus.com/two/1.0/fimi_types.xsd")]
            public string Currency { get; set; }
            [XmlElement(ElementName = "Fee", Namespace = "http://schemas.compassplus.com/two/1.0/fimi_types.xsd")]
            public string Fee { get; set; }
            [XmlElement(ElementName = "FromAcct", Namespace = "http://schemas.compassplus.com/two/1.0/fimi_types.xsd")]
            public string FromAcct { get; set; }
            [XmlElement(ElementName = "LedgerBalance", Namespace = "http://schemas.compassplus.com/two/1.0/fimi_types.xsd")]
            public string LedgerBalance { get; set; }
            [XmlElement(ElementName = "MaskBalances", Namespace = "http://schemas.compassplus.com/two/1.0/fimi_types.xsd")]
            public string MaskBalances { get; set; }
            [XmlElement(ElementName = "ThisTranId", Namespace = "http://schemas.compassplus.com/two/1.0/fimi_types.xsd")]
            public string ThisTranId { get; set; }
            [XmlAttribute(AttributeName = "NextChallenge")]
            public string NextChallenge { get; set; }
            [XmlAttribute(AttributeName = "Response")]
            public string _Response { get; set; }
            [XmlAttribute(AttributeName = "TranId")]
            public string TranId { get; set; }
            [XmlAttribute(AttributeName = "Ver")]
            public string Ver { get; set; }
            [XmlAttribute(AttributeName = "Product")]
            public string Product { get; set; }
        }

        [XmlRoot(ElementName = "POSRequestRp", Namespace = "http://schemas.compassplus.com/two/1.0/fimi.xsd")]
        public class POSRequestRp
        {
            [XmlElement(ElementName = "Response", Namespace = "http://schemas.compassplus.com/two/1.0/fimi.xsd")]
            public Response Response { get; set; }
        }

        [XmlRoot(ElementName = "Body", Namespace = "http://www.w3.org/2003/05/soap-envelope")]
        public class Body
        {
            [XmlElement(ElementName = "POSRequestRp", Namespace = "http://schemas.compassplus.com/two/1.0/fimi.xsd")]
            public POSRequestRp POSRequestRp { get; set; }
        }

        [XmlRoot(ElementName = "Envelope", Namespace = "http://www.w3.org/2003/05/soap-envelope")]
        public class Envelope
        {
            [XmlElement(ElementName = "Body", Namespace = "http://www.w3.org/2003/05/soap-envelope")]
            public Body Body { get; set; }
            [XmlAttribute(AttributeName = "env", Namespace = "http://www.w3.org/2000/xmlns/")]
            public string Env { get; set; }
            [XmlAttribute(AttributeName = "m0", Namespace = "http://www.w3.org/2000/xmlns/")]
            public string M0 { get; set; }
            [XmlAttribute(AttributeName = "m1", Namespace = "http://www.w3.org/2000/xmlns/")]
            public string M1 { get; set; }
        }
    }
}
