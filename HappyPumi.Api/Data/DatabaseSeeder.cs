#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace HappyPumi.Api.Data;

/// <summary>
/// Seeds realistic demo data into an empty database (gated by <c>Seed:Enabled</c>) so the pulumi CLI has
/// real stacks/orgs/registry/policy/deployment data to query against in local dev (`make dev`) and the
/// CLI integration tests. Idempotent: it no-ops once the seed org's stacks exist.
/// </summary>
public static class DatabaseSeeder
{
    public const string Org = "happypumi";
    private const string Project = "webstore";

    public static void Seed(HappyPumiDbContext db)
    {
        if (db.Stacks.Any(s => s.Org == Org && s.Project == Project))
            return; // already seeded

        SeedIdentity(db);
        SeedStacks(db);
        SeedRegistry(db);
        SeedPolicy(db);
        SeedDeployments(db);
        SeedAgentPool(db);
        db.SaveChanges();
    }

    /// <summary>
    /// A workflow-runner pool with a fixed, well-known token so the docker-compose demo's agent (configured
    /// with this token) authenticates out of the box. Real pools mint a random token on creation.
    /// </summary>
    private static void SeedAgentPool(HappyPumiDbContext db)
        => db.AgentPools.Add(new AgentPoolRow
        {
            Id = Guid.NewGuid().ToString(), Org = Org, Name = "demo", Description = "compose demo pool",
            Token = "happypumi-demo-pool", Created = DateTime.UtcNow,
        });

    private static void SeedIdentity(HappyPumiDbContext db)
    {
        db.Members.AddRange(
            new MemberRow { Org = Org, UserLogin = "happypumi", Role = "admin", Created = DateTime.UtcNow },
            new MemberRow { Org = Org, UserLogin = "alice", Role = "member", Created = DateTime.UtcNow },
            new MemberRow { Org = Org, UserLogin = "bob", Role = "member", Created = DateTime.UtcNow });

        db.Roles.Add(new RoleRow
        {
            Id = Guid.NewGuid().ToString(), Org = Org, Created = DateTime.UtcNow, Modified = DateTime.UtcNow,
            // "role" is the default purpose `pulumi org role list` filters by (--purpose role).
            Version = 1, Name = "deployer", Description = "Can run deployments", UxPurpose = "role",
        });
    }

    private static void SeedStacks(HappyPumiDbContext db)
    {
        foreach (var (stack, version) in new[] { ("dev", 3L), ("prod", 7L) })
        {
            db.Stacks.Add(new StackRow
            {
                Org = Org, Project = Project, Stack = stack, Version = version,
                Tags = new Dictionary<string, string> { ["environment"] = stack, ["team"] = "platform" },
                Config = new AppStackConfig { SecretsProvider = "service" },
                Deployment = EmptyDeployment(),
            });

            // A couple of history entries so `pulumi stack history` shows real updates.
            for (long v = 1; v <= version; v += Math.Max(1, version - 1))
                db.StackUpdates.Add(new StackUpdateRow
                {
                    UpdateId = Guid.NewGuid().ToString(), Org = Org, Project = Project, Stack = stack,
                    Version = v, Kind = "update", Result = "succeeded", Message = $"Update {v}",
                    StartTime = DateTimeOffset.UtcNow.AddMinutes(-v * 5).ToUnixTimeSeconds(),
                    EndTime = DateTimeOffset.UtcNow.AddMinutes(-v * 5 + 1).ToUnixTimeSeconds(),
                    Config = new Dictionary<string, AppConfigValue>(),
                });
        }
    }

    private static void SeedRegistry(HappyPumiDbContext db)
    {
        foreach (var (name, ver) in new[] { ("widgets", "1.0.0"), ("widgets", "1.1.0"), ("gadgets", "1.0.0") })
            db.Packages.Add(new PackageVersionRow
            {
                Source = "private", Publisher = Org, Name = name, Version = ver,
                CreatedAt = DateTime.UtcNow, PublishedAt = DateTime.UtcNow, Published = true,
            });

        db.Templates.Add(new TemplateVersionRow
        {
            Source = "private", Publisher = Org, Name = "webstore-starter", Version = "1.0.0",
            UpdatedAt = DateTime.UtcNow, Language = "go", Description = "Webstore starter template", Published = true,
        });
    }

    private static void SeedPolicy(HappyPumiDbContext db)
    {
        db.PolicyGroups.Add(new PolicyGroupRow
        {
            Org = Org, Name = "production", IsOrgDefault = true,
            Stacks = new List<string> { $"{Project}/prod" }, AppliedPolicyPacks = new List<string> { "security" },
        });
        db.PolicyPackVersions.Add(new PolicyPackVersionRow
        {
            Org = Org, Name = "security", Version = 1, DisplayName = "Security Baseline", Published = true,
            Policies = new List<AppPolicy>(),
        });
    }

    private static readonly string[] StepNames =
        ["Provision", "Install dependencies", "Run pulumi update", "Collect outputs", "Finalize"];

    private static void SeedDeployments(HappyPumiDbContext db)
    {
        db.DeploymentSettings.Add(new DeploymentSettingsRow
        {
            Org = Org, Project = Project, Stack = "prod",
            Settings = new DeploymentSettings { Source = "git", Tag = "v1" },
        });

        // Three deployments across the project's stacks (the org Deployments console page + detail/logs).
        SeedDeployment(db, "prod", 11, "update", "succeeded", agoMinutes: 60);
        SeedDeployment(db, "staging", 7, "preview", "succeeded", agoMinutes: 120);
        SeedDeployment(db, "dev", 4, "update", "failed", agoMinutes: 1440);

        db.Webhooks.Add(new WebhookRow
        {
            Id = Guid.NewGuid().ToString(), Org = Org, Project = Project, Stack = "prod",
            Webhook = new WebhookResponse
            {
                Name = "ci", DisplayName = "CI notifications", PayloadUrl = "https://ci.happypumi.dev/hook",
                OrganizationName = Org, Active = true,
            },
        });
    }

    private static void SeedDeployment(HappyPumiDbContext db, string stack, long version, string op, string status, int agoMinutes)
    {
        var id = Guid.NewGuid().ToString();
        var started = DateTime.UtcNow.AddMinutes(-agoMinutes);
        var finished = started.AddMinutes(2);
        // A failed run fails on its last step; otherwise every step succeeds.
        var steps = StepNames.Select((name, i) =>
        {
            var last = i == StepNames.Length - 1;
            var stepStatus = status == "failed" && last ? "failed" : "succeeded";
            return new StepRun
            {
                Name = name, Status = stepStatus,
                Started = started.AddSeconds(i * 20), LastUpdated = started.AddSeconds(i * 20 + 18),
            };
        }).ToList();

        db.Deployments.Add(new DeploymentRow
        {
            Id = id, Org = Org, Project = Project, Stack = stack, Version = version, Operation = op,
            Status = status, Created = started, Modified = finished,
            RequestedByLogin = "happypumi", RequestedByName = "HappyPumi",
            Jobs = new List<DeploymentJob>
            {
                new() { Status = status, Started = started, LastUpdated = finished, Steps = steps },
            },
            Updates = new List<DeploymentNestedUpdate>(),
        });

        var logLines = new[]
        {
            "Provisioning deployment environment...", "Installing dependencies (go mod download)",
            "Running `pulumi up --yes`", $"Updating ({Project}/{stack})",
            status == "failed" ? "error: update failed" : "    + 12 created",
            status == "failed" ? "Update failed." : "Resources: 12 unchanged",
        };
        for (var i = 0; i < logLines.Length; i++)
            db.DeploymentLogs.Add(new DeploymentLogRow
            {
                DeploymentId = id, Step = 0, Header = "pulumi", Line = logLines[i],
                Timestamp = started.AddSeconds(i * 15),
            });
    }

    private static AppUntypedDeployment EmptyDeployment() => new()
    {
        Version = 3,
        Deployment = new Dictionary<string, object?> { ["manifest"] = new Dictionary<string, object?>() },
    };
}
