using SAPWebApi.Models;
using System.Web.Http;
using System.Web.OData.Builder;
using System.Web.OData.Extensions;

namespace SAPWebApi
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            var builder = new ODataConventionModelBuilder();

            builder.EntitySet<SAPParameter>("SAP");

            var function = builder.Function("GetSAPInfo").ReturnsFromEntitySet<SAPParameter>("SAPInfo");
            function.Parameter<string>("orderNo");

            var model = builder.GetEdmModel();
            config.MapODataServiceRoute("ODataRoute", null, model);
        }
    }
}
