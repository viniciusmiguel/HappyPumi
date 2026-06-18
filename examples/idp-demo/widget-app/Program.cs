using Pulumi;
using System.Collections.Generic;

return await Deployment.RunAsync(() =>
{
    var widget = new Widget("demo", new WidgetArgs { Size = "large" });
    return new Dictionary<string, object?> { ["widgetId"] = widget.WidgetId };
});
