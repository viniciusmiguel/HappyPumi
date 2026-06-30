// Package autoapi exercises the Pulumi Automation API (Go SDK) against a live HappyPumi.
//
// The SDK shells out to the pulumi binary on PATH; the xUnit runner (GoTestRunner) prepends
// the locally-built .tools/bin and exports PULUMI_BACKEND_URL / PULUMI_ACCESS_TOKEN / SSL_CERT_DIR
// before invoking `go test`. Clean-room note: the CLI is used purely as a black-box client
// (see docs/adr/0008-clean-room-implementation.md).
package autoapi

import (
	"context"
	"fmt"
	"os"

	"github.com/pulumi/pulumi/sdk/v3/go/auto"
	"github.com/pulumi/pulumi/sdk/v3/go/pulumi"
)

// LoginWorkspace returns a LocalWorkspace pointed at the HappyPumi backend under test.
// PULUMI_BACKEND_URL and PULUMI_ACCESS_TOKEN are honored by the pulumi CLI directly, so the
// workspace authenticates implicitly on first use. A fixed passphrase keeps secret config
// non-interactive across the suite.
func LoginWorkspace(ctx context.Context) (auto.Workspace, error) {
	if os.Getenv("PULUMI_BACKEND_URL") == "" {
		return nil, fmt.Errorf("PULUMI_BACKEND_URL not set; the xUnit runner (GoTestRunner) must export it")
	}
	return auto.NewLocalWorkspace(ctx, auto.EnvVars(map[string]string{
		"PULUMI_CONFIG_PASSPHRASE": "hp-test",
	}))
}

// noop is a resourceless inline Pulumi program — the inline-source mirror of the empty-stack
// fixture. It provisions nothing, so the full update lifecycle runs without any provider.
func noop(_ *pulumi.Context) error { return nil }

// qualified builds a fully-qualified stack name under the "organization" org. HappyPumi accepts
// any org segment; the CLI wire-compat suite uses "organization", so the auto suite matches it.
func qualified(project, stack string) string {
	return "organization/" + project + "/" + stack
}

