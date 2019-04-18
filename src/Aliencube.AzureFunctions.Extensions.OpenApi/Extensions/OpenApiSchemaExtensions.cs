using System;
using System.Collections;
using System.Linq;
using System.Reflection;

using Aliencube.AzureFunctions.Extensions.OpenApi.Attributes;

using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;

using Newtonsoft.Json;

namespace Aliencube.AzureFunctions.Extensions.OpenApi.Extensions
{
    using System.Collections.Generic;

    using Newtonsoft.Json.Converters;
    using Newtonsoft.Json.Linq;
    using Newtonsoft.Json.Schema;
    using Newtonsoft.Json.Serialization;

    /// <summary>
    /// This represents the extension entity for <see cref="OpenApiSchema"/>.
    /// </summary>
    public static class OpenApiSchemaExtensions
    {
        /// <summary>
        /// Converts <see cref="Type"/> to <see cref="OpenApiSchema"/>.
        /// </summary>
        /// <param name="type"><see cref="Type"/> instance.</param>
        /// <param name="attribute"><see cref="OpenApiSchemaVisibilityAttribute"/> instance. Default is <c>null</c>.</param>
        /// <returns><see cref="OpenApiSchema"/> instance.</returns>
        /// <remarks>
        /// It runs recursively to build the entire object type. It only takes properties without <see cref="JsonIgnoreAttribute"/>.
        /// </remarks>
        public static OpenApiSchema ToOpenApiSchema(
            this Type type,
            NamingStrategy namingStrategy,
            OpenApiSchemaVisibilityAttribute attribute = null)
        {
            type.ThrowIfNullOrDefault();
            OpenApiSchema schema = null;

            var unwrappedValueType = Nullable.GetUnderlyingType(type);
            if (unwrappedValueType != null)
            {
                schema = unwrappedValueType.ToOpenApiSchema(namingStrategy);
                schema.Nullable = true;
                return schema;
            }

            schema = new OpenApiSchema() { Type = type.ToDataType(), Format = type.ToDataFormat() };
            if (attribute != null)
            {
                var visibility = new OpenApiString(attribute.Visibility.ToDisplayName());

                schema.Extensions.Add("x-ms-visibility", visibility);
            }

            if (typeof(Enum).IsAssignableFrom(type))
            {
                bool isFlags = type.IsDefined(typeof(FlagsAttribute), false);

                if (type.IsDefined(typeof(JsonConverterAttribute)))
                {
                    var converterAttribute = type.GetCustomAttribute<JsonConverterAttribute>();
                    if (typeof(StringEnumConverter).IsAssignableFrom(converterAttribute.ConverterType))
                    {
                        var names = Enum.GetNames(type);
                        schema.Enum = names.Select(n => (IOpenApiAny)new OpenApiString(namingStrategy.GetPropertyName(n,false))).ToList();
                        schema.Type = "string";
                        schema.Format = null;
                    }
                }
            }

            if (type.IsSimpleType())
            {
                return schema;
            }

            if (typeof(IDictionary).IsAssignableFrom(type))
            {
                schema.AdditionalProperties = type.GetGenericArguments()[1].ToOpenApiSchema(namingStrategy);

                return schema;
            }

            if (typeof(IList).IsAssignableFrom(type))
            {
                schema.Type = "array";
                schema.Items = (type.GetElementType() ?? type.GetGenericArguments()[0]).ToOpenApiSchema(namingStrategy);

                return schema;
            }



            var properties = type.GetProperties().Where(p => !p.ExistsCustomAttribute<JsonIgnoreAttribute>());
            foreach (var property in properties)
            {
                var visiblity = property.GetCustomAttribute<OpenApiSchemaVisibilityAttribute>(inherit: false);

                schema.Properties[namingStrategy.GetPropertyName(property.Name, false)] = property.PropertyType.ToOpenApiSchema(namingStrategy, visiblity);
            }

            return schema;
        }

        /// <summary>
        /// Converts <see cref="Type"/> to <see cref="OpenApiSchema"/>.
        /// </summary>
        /// <param name="type"><see cref="Type"/> instance.</param>
        /// <param name="attribute"><see cref="OpenApiSchemaVisibilityAttribute"/> instance. Default is <c>null</c>.</param>
        /// <returns><see cref="OpenApiSchema"/> instance.</returns>
        /// <remarks>
        /// It runs recursively to build the entire object type. It only takes properties without <see cref="JsonIgnoreAttribute"/>.
        /// </remarks>
        public static OpenApiSchema ToOpenApiSchema(this JsonSchema jsonSchema, OpenApiSchemaVisibilityAttribute attribute = null)
        {
            if (jsonSchema == null)
            {
                return null;
            }

            return new OpenApiSchema
                       {
                           AdditionalProperties = jsonSchema.AdditionalProperties.ToOpenApiSchema(),
                           AnyOf = jsonSchema.Items?.Select(s => s.ToOpenApiSchema()).ToList(),
                           Default = jsonSchema.Default?.ToOpenApi(),
                           Description = jsonSchema.Description,
                           ExclusiveMaximum = jsonSchema.ExclusiveMaximum,
                           ExclusiveMinimum = jsonSchema.ExclusiveMinimum,
                           Enum = jsonSchema.Enum?.Select(jt => jt.ToOpenApi()).ToList(),
                           Format = jsonSchema.Format,
                           MaxItems = jsonSchema.MaximumItems,
                           MaxLength = jsonSchema.MaximumLength,
                           MinItems = jsonSchema.MinimumItems,
                           MinLength = jsonSchema.MinimumLength,
                           Maximum = (decimal?)jsonSchema.Maximum,
                           Minimum = (decimal?)jsonSchema.Minimum,
                           Pattern = jsonSchema.Pattern,
                           Properties =
                               jsonSchema.Properties?.ToDictionary(kv => kv.Key, kv => kv.Value.ToOpenApiSchema()),
                           ReadOnly = jsonSchema.ReadOnly ?? false,
                           Title = jsonSchema.Title,
                           Type = jsonSchema.Type?.ToString().ToLower()
                       };
        }

        public static IOpenApiAny ToOpenApi(this JToken token)
        {
            switch (token)
            {
                case null:
                    return new OpenApiNull();

                case JArray arrayToken:
                    {
                        var result = new OpenApiArray();
                        result.AddRange(arrayToken.Select(t => t.ToOpenApi()));
                        return result;
                    }
                case JObject objectToken:
                    {
                        var result = new OpenApiObject();
                        foreach (var kv in objectToken)
                        {
                            result[kv.Key] = kv.Value.ToOpenApi();
                        }

                        return result;
                    }
                case JValue valueToken:
                    switch (valueToken.Value)
                    {
                        case bool value:
                            return new OpenApiBoolean(value);

                        case string value:
                            return new OpenApiString(value);

                        case byte value:
                            return new OpenApiByte(value);

                        case Enum value:
                            return new OpenApiString(((Enum)valueToken.Value).ToString());

                        case int value:
                            return new OpenApiInteger(value);

                        case long value:
                            return new OpenApiLong(value);

                        case float value:
                            return new OpenApiFloat(value);

                        case double value:
                            return new OpenApiDouble(value);

                        case decimal value:
                            return new OpenApiDouble((double)value);

                        case DateTime value:
                            return new OpenApiDate(value);

                        case DateTimeOffset value:
                            return new OpenApiDateTime(value);

                        case byte[] value:
                            return new OpenApiBinary(value);

                        case Guid value:
                            return new OpenApiString(value.ToString("D"));

                        case Uri value:
                            return new OpenApiString(value.ToString());

                        case TimeSpan value:
                            return new OpenApiString(value.ToString("c"));

                        case null:
                            return new OpenApiNull();

                        default:
                            throw new Exception("Unhandled value type for JToken: " + valueToken.Value.GetType());
                    }
                default:
                    throw new Exception("Unhandled token type " + token.GetType());
            }
        }
    }
}
