using System.Linq;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.AspNetCore.Http;
using System.Reflection;

namespace Shagun.Swagger
{
    public class FileUploadOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var parameters = context.MethodInfo.GetParameters();
            var fileParams = parameters.Where(p => p.ParameterType == typeof(IFormFile) || p.ParameterType == typeof(IFormFileCollection) || p.GetCustomAttributes().Any(a => a.GetType().Name == "FromFormAttribute")).ToList();
            if (!fileParams.Any()) return;

            // Build multipart/form-data request body schema
            var schema = new OpenApiSchema { Type = "object", Properties = new System.Collections.Generic.Dictionary<string, OpenApiSchema>() };
            foreach (var p in fileParams)
            {
                schema.Properties[p.Name] = new OpenApiSchema { Type = "string", Format = "binary" };
            }

            operation.RequestBody = new OpenApiRequestBody
            {
                Content =
                {
                    ["multipart/form-data"] = new OpenApiMediaType
                    {
                        Schema = schema
                    }
                }
            };
        }
    }
}
