using System.Windows.Data;
using System.Windows.Markup;

namespace MarkdownExporter.Localization;

[MarkupExtensionReturnType(typeof(string))]
public sealed class TrExtension(string key) : MarkupExtension
{
    public override object ProvideValue(IServiceProvider serviceProvider) => new Binding($"[{key}]")
    {
        Source = LanguageManager.Instance,
        Mode = BindingMode.OneWay
    }.ProvideValue(serviceProvider);
}
