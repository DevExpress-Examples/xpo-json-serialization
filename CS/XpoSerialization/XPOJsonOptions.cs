using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using DevExpress.Xpo;
using System.Linq;
using DevExpress.Xpo.Helpers;

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

    public class XpoMetadataProvider : DefaultModelMetadataProvider {
        public XpoMetadataProvider(ICompositeMetadataDetailsProvider detailsProvider) : base(detailsProvider) {

        }
        public XpoMetadataProvider(ICompositeMetadataDetailsProvider detailsProvider, IOptions<MvcOptions> optionsAccessor) : base(detailsProvider, optionsAccessor) {

        }
        protected override DefaultMetadataDetails[] CreatePropertyDetails(ModelMetadataIdentity key) {
            DefaultMetadataDetails[] result = base.CreatePropertyDetails(key);
            if(typeof(PersistentBase).IsAssignableFrom(key.ModelType))
                return result.Where(x => !IsServiceField(x.Key)).ToArray();
            else
                return result;
        }
        static bool IsServiceField(ModelMetadataIdentity identity) {
            Type declaringType = identity.PropertyInfo.DeclaringType;
            return declaringType == typeof(PersistentBase)
                || declaringType == typeof(XPBaseObject);
        }
    }
}