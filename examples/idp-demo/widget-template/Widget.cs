using Pulumi;
using Pulumi.Random;
using System.Collections.Generic;

// A reusable C# component: a "Widget" provisioned on the fake-cloud (Pulumi random) provider.
public class WidgetArgs : ResourceArgs
{
    [Input("size")] public Input<string> Size { get; set; } = "medium";
}

public class Widget : ComponentResource
{
    [Output("widgetId")] public Output<string> WidgetId { get; private set; }

    public Widget(string name, WidgetArgs args, ComponentResourceOptions? opts = null)
        : base("widgets:index:Widget", name, args, opts)
    {
        var id = new RandomPet($"{name}-id", new RandomPetArgs { Length = 2 },
            new CustomResourceOptions { Parent = this });
        WidgetId = id.Id;
        RegisterOutputs(new Dictionary<string, object?> { ["widgetId"] = WidgetId });
    }
}
