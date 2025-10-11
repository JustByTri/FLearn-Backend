using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Presentation.Filter
{
    public class FileUploadOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            // ✅ ENHANCED: Handle Voice Assessment specific endpoints
            if (context.ApiDescription.RelativePath?.Contains("submit-voice") == true)
            {
                ApplyVoiceAssessmentSchema(operation, context);
                return;
            }

            // Handle general file upload endpoints
            var formFileParams = context.ApiDescription.ParameterDescriptions
                .Where(x => x.ModelMetadata?.ContainerType == typeof(IFormFile) ||
                           x.Type == typeof(IFormFile) ||
                           x.Type == typeof(IFormFile[]) ||
                           x.Type == typeof(IList<IFormFile>))
                .ToList();

            if (!formFileParams.Any())
                return;

            ApplyFileUploadSchema(operation, context, formFileParams);
            if (context.ApiDescription.RelativePath?.Contains("submit-voice") == true)
            {
                ApplyVoiceAssessmentSchema(operation, context);
                return;
            }

            
            if (context.ApiDescription.RelativePath?.Contains("conversation/send-voice") == true)
            {
                ApplyConversationVoiceSchema(operation, context);
                return;
            }

        }

        private void ApplyVoiceAssessmentSchema(OpenApiOperation operation, OperationFilterContext context)
        {
            operation.RequestBody = new OpenApiRequestBody
            {
                Required = true,
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    ["multipart/form-data"] = new OpenApiMediaType
                    {
                        Schema = new OpenApiSchema
                        {
                            Type = "object",
                            Properties = new Dictionary<string, OpenApiSchema>
                            {
                                ["questionNumber"] = new OpenApiSchema
                                {
                                    Type = "integer",
                                    Description = "Số thứ tự câu hỏi",
                                    Example = new Microsoft.OpenApi.Any.OpenApiInteger(1)
                                },
                                ["isSkipped"] = new OpenApiSchema
                                {
                                    Type = "boolean",
                                    Description = "Có bỏ qua câu hỏi không",
                                    Example = new Microsoft.OpenApi.Any.OpenApiBoolean(false)
                                },
                                ["audioFile"] = new OpenApiSchema
                                {
                                    Type = "string",
                                    Format = "binary",
                                    Description = "File âm thanh (MP3, WAV, M4A, WebM - Max 10MB)"
                                },
                                ["recordingDurationSeconds"] = new OpenApiSchema
                                {
                                    Type = "integer",
                                    Description = "Thời lượng ghi âm (giây)",
                                    Example = new Microsoft.OpenApi.Any.OpenApiInteger(0)
                                }
                            },
                            Required = new HashSet<string> { "questionNumber", "isSkipped" }
                        }
                    }
                }
            };

            // ⭐ CHỈ XÓA form parameters, GIỮ LẠI route parameters (assessmentId)
            var parametersToRemove = operation.Parameters?
                .Where(p => p.In == ParameterLocation.Query ||
                            (p.In == ParameterLocation.Path && p.Name != "assessmentId"))
                .ToList();

            if (parametersToRemove != null)
            {
                foreach (var param in parametersToRemove)
                {
                    operation.Parameters.Remove(param);
                }
            }
        }

        private void ApplyFileUploadSchema(OpenApiOperation operation, OperationFilterContext context, List<Microsoft.AspNetCore.Mvc.ApiExplorer.ApiParameterDescription> formFileParams)
        {
            operation.RequestBody = new OpenApiRequestBody
            {
                Required = true,
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    ["multipart/form-data"] = new OpenApiMediaType
                    {
                        Schema = new OpenApiSchema
                        {
                            Type = "object",
                            Properties = context.ApiDescription.ParameterDescriptions
                                .Where(p => p.Source.Id == "Form" || formFileParams.Contains(p))
                                .ToDictionary(
                                    p => p.Name,
                                    p => CreatePropertySchema(p.Type)
                                ),
                            Required = new HashSet<string>(
                                context.ApiDescription.ParameterDescriptions
                                    .Where(p => p.IsRequired && (p.Source.Id == "Form" || formFileParams.Contains(p)))
                                    .Select(p => p.Name)
                            )
                        }
                    }
                }
            };
        }

        private OpenApiSchema CreatePropertySchema(Type type)
        {
            if (type == typeof(IFormFile))
            {
                return new OpenApiSchema { Type = "string", Format = "binary" };
            }
            if (type == typeof(IFormFile[]) || type == typeof(IList<IFormFile>))
            {
                return new OpenApiSchema
                {
                    Type = "array",
                    Items = new OpenApiSchema { Type = "string", Format = "binary" }
                };
            }
            if (type == typeof(string))
            {
                return new OpenApiSchema { Type = "string" };
            }
            if (type == typeof(int) || type == typeof(int?))
            {
                return new OpenApiSchema { Type = "integer" };
            }
            if (type == typeof(bool) || type == typeof(bool?))
            {
                return new OpenApiSchema { Type = "boolean" };
            }

            return new OpenApiSchema { Type = "string" };
        }

        private void ApplyConversationVoiceSchema(OpenApiOperation operation, OperationFilterContext context)
        {
            operation.RequestBody = new OpenApiRequestBody
            {
                Required = true,
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    ["multipart/form-data"] = new OpenApiMediaType
                    {
                        Schema = new OpenApiSchema
                        {
                            Type = "object",
                            Properties = new Dictionary<string, OpenApiSchema>
                            {
                                ["sessionId"] = new OpenApiSchema
                                {
                                    Type = "string",
                                    Format = "uuid",
                                    Description = "ID của conversation session"
                                },
                                ["audioFile"] = new OpenApiSchema
                                {
                                    Type = "string",
                                    Format = "binary",
                                    Description = "File âm thanh (MP3, WAV, M4A, WebM, OGG - Max 10MB)"
                                },
                                ["audioDuration"] = new OpenApiSchema
                                {
                                    Type = "integer",
                                    Description = "Thời lượng audio (giây)",
                                    Nullable = true
                                },
                                ["transcript"] = new OpenApiSchema
                                {
                                    Type = "string",
                                    Description = "Transcript của voice message (optional)",
                                    Nullable = true
                                }
                            },
                            Required = new HashSet<string> { "sessionId", "audioFile" }
                        }
                    }
                }
            };

            // Add response schema
            operation.Responses["200"] = new OpenApiResponse
            {
                Description = "Voice message sent successfully",
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    ["application/json"] = new OpenApiMediaType
                    {
                        Schema = new OpenApiSchema
                        {
                            Type = "object",
                            Properties = new Dictionary<string, OpenApiSchema>
                            {
                                ["success"] = new OpenApiSchema { Type = "boolean" },
                                ["message"] = new OpenApiSchema { Type = "string" },
                                ["data"] = new OpenApiSchema
                                {
                                    Type = "object",
                                    Properties = new Dictionary<string, OpenApiSchema>
                                    {
                                        ["aiResponse"] = new OpenApiSchema { Type = "object" },
                                        ["voiceInfo"] = new OpenApiSchema
                                        {
                                            Type = "object",
                                            Properties = new Dictionary<string, OpenApiSchema>
                                            {
                                                ["audioUrl"] = new OpenApiSchema { Type = "string" },
                                                ["audioPublicId"] = new OpenApiSchema { Type = "string" },
                                                ["audioDuration"] = new OpenApiSchema { Type = "integer" },
                                                ["fileSize"] = new OpenApiSchema { Type = "integer" },
                                                ["contentType"] = new OpenApiSchema { Type = "string" }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };
        }
    }
}
