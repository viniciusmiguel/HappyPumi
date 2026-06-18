module happypumi-empty-stack

go 1.26

require github.com/pulumi/pulumi/sdk/v3 v3.0.0

// Use the local Apache-2.0 SDK checkout so the program builds offline / clean-room (no network).
// Path is relative to this fixture: HappyPumi/HappyPumi.Cli.IntegrationTests/fixtures/empty-stack.
replace github.com/pulumi/pulumi/sdk/v3 => ../../../../pulumi/sdk
