using System;
using System.Globalization;
using System.Reflection;
using System.Resources;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace sospect.Helpers
{
    [ContentProperty("Text")]
    public class TranslateExtension : IMarkupExtension
    {
        readonly CultureInfo ci;
        const string ResourceId = "sospect.Resources.AppResources";

        static readonly Lazy<ResourceManager> ResMgr = new Lazy<ResourceManager>(() => new ResourceManager(ResourceId, typeof(TranslateExtension).GetTypeInfo().Assembly));

        public TranslateExtension()
        {
            ci = CultureInfo.CurrentCulture;
        }

        public string Text { get; set; }

        public static string Translate(string key)
        {
            var ci = CultureInfo.CurrentCulture;
            var translation = ResMgr.Value.GetString(key, ci);

            if (translation == null)
            {
                throw new ArgumentException($"Key '{key}' was not found in resources '{ResourceId}' for culture '{ci.Name}'.");
            }

            return translation;
        }

        public object ProvideValue(IServiceProvider serviceProvider)
        {
            if (Text == null)
                return "";

            var translation = ResMgr.Value.GetString(Text, ci);

            if (translation == null)
            {
                throw new ArgumentException($"Key '{Text}' was not found in resources '{ResourceId}' for culture '{ci.Name}'.");
            }

            return translation;
        }
    }
}
