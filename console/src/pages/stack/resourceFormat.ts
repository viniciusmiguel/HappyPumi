// Display helpers for stack resources, shared by the Resources tab, the resource graph, and the
// resource detail dialog. Pure formatting — no React, no I/O.
import { Layers, Boxes, Cloud, Database, Zap, Box } from "lucide-react";

export function urnName(urn: string): string {
  return urn.split("::").pop() || urn;
}

// The provider/package name is the first segment of the type token ("aws:s3/bucketV2:BucketV2" -> "aws").
export function providerOf(type: string): string {
  return type.split(":")[0] ?? "";
}

// Normalize "pkg:module/sub:Type" -> "pkg:module:Type" for display, matching the console.
export function normalizeType(type: string): string {
  const [pkg, mod, name] = type.split(":");
  if (!mod || !name) return type;
  return `${pkg}:${mod.split("/")[0]}:${name}`;
}

// Cloud providers whose console we deep-link to; others (random, tls, pulumi) get no link.
export const CLOUD_LINKS: Record<string, string> = {
  aws: "https://console.aws.amazon.com/",
  gcp: "https://console.cloud.google.com/",
  azure: "https://portal.azure.com/",
  "azure-native": "https://portal.azure.com/",
  azuread: "https://portal.azure.com/",
  kubernetes: "https://kubernetes.io/docs/",
  cloudflare: "https://dash.cloudflare.com/",
  digitalocean: "https://cloud.digitalocean.com/",
};

export function resourceIcon(type: string) {
  if (type === "pulumi:pulumi:Stack") return Layers;
  if (type.startsWith("pulumi:providers:")) return Boxes;
  const pkg = providerOf(type);
  if (pkg === "aws" || pkg === "gcp" || pkg === "azure" || pkg === "azure-native") return Cloud;
  if (type.includes("dynamodb") || type.includes("rds") || type.includes("sql") || type.includes("database")) return Database;
  if (type.includes("lambda") || type.includes("function")) return Zap;
  return Box;
}
