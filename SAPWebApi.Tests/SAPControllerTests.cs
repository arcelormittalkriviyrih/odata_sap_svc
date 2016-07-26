using System;
using System.Net.Http;
using System.Web.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SAPWebApi.Models;
using System.Web.OData.Builder;
using System.Web.OData.Extensions;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http.Headers;
using Newtonsoft.Json.Linq;

namespace SAPWebApi.Tests
{
	[TestClass]
	public class SAPControllerTests
	{
        private HttpClient client;

        public SAPControllerTests()
        {
            var configuration = new HttpConfiguration();
            var builder = new ODataConventionModelBuilder();

            builder.EntitySet<SAPParameter>("SAP");

            var function = builder.Function("GetSAPInfo").ReturnsFromEntitySet<SAPParameter>("SAPInfo");
            function.Parameter<string>("orderNo");

            var model = builder.GetEdmModel();
            configuration.MapODataServiceRoute("ODataRoute", null, model);
            //configuration.MapRestierRoute<DynamicApi>("DynamicApi", "DynamicApi").Wait();
            client = new HttpClient(new HttpServer(configuration));
        }

        [TestMethod()]
        public async Task GetSAP()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "http://host/SAP");
            request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json;odata.metadata=full"));
            HttpResponseMessage response = await client.SendAsync(request);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }

        [TestMethod()]
        public async Task GetSAPInfoTest()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, @"http://host/GetSAPInfo?orderNo='5000339689'");
            request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json;odata.metadata=full"));
            var response = await client.SendAsync(request);
            var result = await response.Content.ReadAsStringAsync();

            JObject jsonResult = JObject.Parse(result);
            JToken sapValue = jsonResult.GetValue("value");
            JToken sapFirst = sapValue.First;
            JToken sapSelesOrder = sapFirst.SelectToken("Name");
            JToken sapSelesOrderValue = sapFirst.SelectToken("Value");

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.IsTrue(sapSelesOrder.Value<string>() == "SelesOrder");
            Assert.IsTrue(sapSelesOrderValue.Value<string>() == "5000339689");
        }
    }
}
