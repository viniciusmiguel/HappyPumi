using PulumiApiGenerator;
using PulumiApiGenerator.Generators;

// ---- Defaults ----
string specUrl      = "https://api.pulumi.com/api/openapi/pulumi-spec.json";
string outputDir    = "../../../../HappyPumi.Api";
string rootNs       = "HappyPumi.Api";
bool   cleanOutput  = false;

// ---- CLI parsing (very small, no extra deps) ----
for (int i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--spec" or "-s":   specUrl = args[++i]; break;
        case "--out"  or "-o":   outputDir = args[++i]; break;
        case "--ns"   or "-n":   rootNs = args[++i]; break;
        case "--clean":          cleanOutput = true; break;
        case "--help" or "-h":
            PrintUsage();
            return 0;
        default:
            Console.Error.WriteLine($"Unknown argument: {args[i]}");
            PrintUsage();
            return 1;
    }
}

if (cleanOutput && Directory.Exists(outputDir))
{
    Console.WriteLine($"Cleaning {outputDir}...");
    Directory.Delete(outputDir, recursive: true);
}

Console.WriteLine($"Loading OpenAPI spec from {specUrl} ...");
var doc = await OpenApiLoader.LoadAsync(specUrl);

Console.WriteLine($"  schemas:   {doc.Components?.Schemas?.Count ?? 0}");
Console.WriteLine($"  paths:     {doc.Paths?.Count ?? 0}");
Console.WriteLine($"  operations:{doc.Paths?.Sum(p => p.Value.Operations.Count) ?? 0}");

var contractsDir = Path.Combine(outputDir, "Contracts");
var endpointsDir = outputDir;

Console.WriteLine($"Generating contracts -> {contractsDir}");
new ContractGenerator(doc, rootNs).Generate(contractsDir);

Console.WriteLine($"Generating endpoints -> {endpointsDir}");
new EndpointGenerator(doc, rootNs).Generate(endpointsDir);

Console.WriteLine("Done.");
return 0;

static void PrintUsage()
{
    Console.WriteLine("""
        PulumiApiGenerator — generates FastEndpoints scaffolding from an OpenAPI spec.

        Usage:
          dotnet run -- [options]

        Options:
          -s, --spec <url|path>   OpenAPI spec URL or local file path
                                  (default: https://api.pulumi.com/api/openapi/pulumi-spec.json)
          -o, --out  <dir>        Output directory (default: ../../../../HappyPumi.Api)
          -n, --ns   <namespace>  Root namespace for generated code (default: HappyPumi.Api)
              --clean             Delete the output directory before generating
          -h, --help              Show this help.
        """);
}
