using DevExpress.Xpo.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System;

namespace XpoSerialization {

    internal class ConfigureJsonOptions : IConfigureOptions<JsonOptions>, IServiceProvider {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IServiceProvider _serviceProvider;

        public ConfigureJsonOptions(
            IHttpContextAccessor httpContextAccessor,
            IServiceProvider serviceProvider) {
            _httpContextAccessor = httpContextAccessor;
            _serviceProvider = serviceProvider;
        }

        public void Configure(JsonOptions options) {
            options.JsonSerializerOptions.Converters.Add(new PersistentBaseConverterFactory(this));
        }

        public object GetService(Type serviceType) {
            return (_httpContextAccessor.HttpContext?.RequestServices ?? _serviceProvider).GetService(serviceType);
        }
    }



}