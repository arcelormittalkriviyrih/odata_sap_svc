using System;
using System.Collections.Generic;
using System.Net;
using System.Web.Http;
using SAPWebApi.Models;
using System.Web.OData;
using System.Web.OData.Routing;
using System.Web.Configuration;
using System.IO;
using System.Text;
using System.Web.Hosting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SAPWebApi.Log;

namespace SAPWebApi.Controllers
{
    public class SAPController : ODataController
    {
        #region Const

        /// <summary>	Test Structure XML from SAP file name. </summary>
        private static readonly string cTestStructureXMLFromSAPFileName = @"testsructurexml.xml";

        /// <summary>	The OData service url. </summary>
        private static readonly string cODataServiceURL = WebConfigurationManager.AppSettings["ODataServiceURL"];

        /// <summary>	The OData service user domain. </summary>
        private static readonly string cODataServiceUserDomain = WebConfigurationManager.AppSettings["ODataServiceUserDomain"];

        /// <summary>	The OData service user login. </summary>
        private static readonly string cODataServiceUserLogin = WebConfigurationManager.AppSettings["ODataServiceUserLogin"];

        /// <summary>	The OData service user password. </summary>
        private static readonly string cODataServiceUserPassword = WebConfigurationManager.AppSettings["ODataServiceUserPassword"];

        #endregion

        #region Fields

        /// <summary>	The SAP parameters. </summary>
        private List<SAPParameter> sapParameters = new List<SAPParameter>();

        #endregion

        #region Methods

        /// <summary>
        /// Get the SAP - set.
        /// </summary>
        /// <returns>Just return empty collection for SAP - set.</returns>
        public List<SAPParameter> Get()
        {
            return sapParameters;
        }

        /// <summary>
        /// Request SAP info.
        /// example: http://localhost:1443/GetSAPInfo?orderNo='5000339689'
        /// </summary>
        [HttpGet]
        [ODataRoute("GetSAPInfo")]
        public List<SAPParameter> GetSAPInfo(string orderNo)
        {
            #region Call Odata service procedure

            var product = new { COMM_ORDER = orderNo, URL = string.Empty };
            string json = JsonConvert.SerializeObject(product);

            string sapURL = string.Empty;
            try
            {
                string responseText = MakeRequestOdata(json);
                dynamic jsonResult = JObject.Parse(responseText);
                sapURL = jsonResult.ActionParameters[0].Value;
            }
            catch (Exception ex)
            {
                SAPLogger.Instance.WriteLoggerLogError("GetSAPInfo->Call Odata service:", ex);
                throw ex;
            }

            #endregion

            #region Call SAP service info

            if (!string.IsNullOrEmpty(sapURL))
            {
                try
                {
                    string xmlStructureFromSAP = MakeRequestSAP(sapURL);
                    dynamic sapObject = DynamicXml.Parse(xmlStructureFromSAP);

                    System.Xml.Linq.XElement rootRowSet = sapObject.Rowset.Row.Root;
                    foreach (var xmlElement in rootRowSet.Elements())
                    {
                        sapParameters.Add(new SAPParameter() { Name = xmlElement.Name.LocalName, Value = xmlElement.Value });
                    }
                }
                catch (Exception ex)
                {
                    SAPLogger.Instance.WriteLoggerLogError("GetSAPInfo->Call SAP service:", ex);

                    string xmlTestStructureFromSAP = Properties.Resources.testsructurexml;
                    dynamic sapObject = DynamicXml.Parse(xmlTestStructureFromSAP);

                    System.Xml.Linq.XElement rootRowSet = sapObject.Rowset.Row.Root;
                    foreach (var xmlElement in rootRowSet.Elements())
                    {
                        sapParameters.Add(new SAPParameter() { Name = xmlElement.Name.LocalName, Value = xmlElement.Value });
                    }
                }
            }

            #endregion

            return sapParameters;
        }

        /// <summary>
        /// Request data from Odata service.
        /// </summary>
        public string MakeRequestOdata(string json)
        {
            string responseText = string.Empty;
            string payload = json;
            byte[] body = Encoding.UTF8.GetBytes(payload);
            var url = new Uri(cODataServiceURL);
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);

            request.Method = "POST";
            request.ContentType = "application/json";
            request.ContentLength = body.Length;
            request.Credentials = new NetworkCredential(cODataServiceUserLogin, cODataServiceUserPassword, cODataServiceUserDomain);

            using (Stream stream = request.GetRequestStream())
            {
                stream.Write(body, 0, body.Length);
                stream.Close();
            }

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                var encoding = ASCIIEncoding.ASCII;
                using (var reader = new System.IO.StreamReader(response.GetResponseStream(), encoding))
                {
                    responseText = reader.ReadToEnd();
                }
                response.Close();
            }

            return responseText;
        }

        /// <summary>
        /// Request data from SAP service.
        /// </summary>
        public string MakeRequestSAP(string requestUrl)
        {
            string responseText = string.Empty;
            HttpWebRequest request = WebRequest.Create(requestUrl) as HttpWebRequest;
            using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
            {
                if (response.StatusCode != HttpStatusCode.OK)
                    throw new Exception(string.Format(
                    "Server error (HTTP {0}: {1}).",
                    response.StatusCode,
                    response.StatusDescription));
                var encoding = ASCIIEncoding.ASCII;
                using (var reader = new System.IO.StreamReader(response.GetResponseStream(), encoding))
                {
                    responseText = reader.ReadToEnd();
                }
                response.Close();
            }
            return responseText;
        }

        #endregion
    }
}
