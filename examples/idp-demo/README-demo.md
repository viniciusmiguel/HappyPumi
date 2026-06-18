# IdP end-to-end demo

Exercises the full IdP flow against HappyPumi using the **fake-cloud (Pulumi random) provider**:

1. Publish the component package schema to the private registry:
   `pulumi package publish ./widgets-schema.json --readme ./README.md --publisher happypumi`
2. `widget-app/` is a C# Pulumi program with a reusable **Widget** component (ComponentResource in
   Widget.cs) that provisions a fake-cloud `RandomPet`. Run it against HappyPumi:
   `pulumi stack init happypumi/widget-app/dev && pulumi up`
3. The resulting stack/resources/updates render in both the ported console (:5173) and the real
   pulumi/console (:3000).

Login: `pulumi login http://localhost:5118` with `PULUMI_ACCESS_TOKEN=dev`.
