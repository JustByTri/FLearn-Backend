using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Presentation.Filter
{
    public class FormFileSchemaFilter : ISchemaFilter
    {
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            if (context.Type == typeof(IFormFile))
            {
                schema.Type = "string";
                schema.Format = "binary";
                schema.Description = "File upload (binary data)";
            }
            else if (context.Type == typeof(IFormFile[]) || context.Type == typeof(IList<IFormFile>))
            {
                schema.Type = "array";
                schema.Items = new OpenApiSchema
                {
                    Type = "string",
                    Format = "binary",
                    Description = "File upload (binary data)"
                };
            }
        }
    }
}
