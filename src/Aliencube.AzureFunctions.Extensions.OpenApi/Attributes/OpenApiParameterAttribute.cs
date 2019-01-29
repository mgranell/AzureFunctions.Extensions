﻿using System;

using Microsoft.OpenApi.Models;

namespace Aliencube.AzureFunctions.Extensions.OpenApi.Attributes
{
    /// <summary>
    /// This represents the attribute entity for HTTP triggers to define Open API parameter.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    public class OpenApiParameterAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OpenApiParameterAttribute"/> class.
        /// </summary>
        /// <param name="name"></param>
        public OpenApiParameterAttribute(string name)
        {
            this.Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        /// <summary>
        /// Gets the parameter name.
        /// </summary>
        public virtual string Name { get; }

        /// <summary>
        /// Gets or sets the parameter description.
        /// </summary>
        public virtual string Description { get; set; }

        /// <summary>
        /// Gets or sets the parameter type. Default is <c>string</c>.
        /// </summary>
        public virtual Type Type { get; set; } = typeof(string);

        /// <summary>
        /// Gets or sets the parameter location. Default is <see cref="ParameterLocation.Path"/>.
        /// </summary>
        public virtual ParameterLocation In { get; set; } = ParameterLocation.Path;

        /// <summary>
        /// Gets or sets the value indicating whether the parameter is required or not. Default is <c>false</c>.
        /// </summary>
        public virtual bool Required { get; set; } = false;
    }
}