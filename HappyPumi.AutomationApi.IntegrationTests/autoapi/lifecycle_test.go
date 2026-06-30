package autoapi

import (
	"bytes"
	"context"
	"testing"

	"github.com/pulumi/pulumi/sdk/v3/go/auto"
	"github.com/pulumi/pulumi/sdk/v3/go/auto/optdestroy"
	"github.com/pulumi/pulumi/sdk/v3/go/auto/optpreview"
	"github.com/pulumi/pulumi/sdk/v3/go/auto/optrefresh"
	"github.com/pulumi/pulumi/sdk/v3/go/auto/optup"
	"github.com/pulumi/pulumi/sdk/v3/go/pulumi"
)

// withOutput is a resourceless inline program that exports a stack output, so Up/Outputs have
// something to read back without provisioning any real resource.
func withOutput(c *pulumi.Context) error {
	c.Export("ok", pulumi.String("yes"))
	return nil
}

// TestInlineLifecycle drives the full update lifecycle (preview -> up -> refresh -> destroy) plus
// History (GetStackUpdates) and Outputs (ExportStack) through an inline-source stack against HappyPumi.
func TestInlineLifecycle(t *testing.T) {
	ctx := context.Background()
	proj := "hp-auto-life"
	name := qualified(proj, "life1")
	s, err := auto.UpsertStackInlineSource(ctx, name, proj, withOutput)
	if err != nil {
		t.Fatalf("UpsertStackInlineSource: %v", err)
	}
	defer func() {
		_, _ = s.Destroy(ctx, optdestroy.ProgressStreams(&bytes.Buffer{}))
		_ = s.Workspace().RemoveStack(ctx, name)
	}()

	if _, err := s.Preview(ctx, optpreview.ProgressStreams(&bytes.Buffer{})); err != nil {
		t.Fatalf("Preview: %v", err)
	}

	up, err := s.Up(ctx, optup.ProgressStreams(&bytes.Buffer{}))
	if err != nil {
		t.Fatalf("Up: %v", err)
	}
	if got := up.Outputs["ok"].Value; got != "yes" {
		t.Fatalf("Up output 'ok' = %v, want \"yes\"", got)
	}

	if _, err := s.Refresh(ctx, optrefresh.ProgressStreams(&bytes.Buffer{})); err != nil {
		t.Fatalf("Refresh: %v", err)
	}

	hist, err := s.History(ctx, 10, 1)
	if err != nil {
		t.Fatalf("History: %v", err)
	}
	if len(hist) == 0 {
		t.Fatal("History returned no updates after Up")
	}

	outs, err := s.Outputs(ctx)
	if err != nil {
		t.Fatalf("Outputs: %v", err)
	}
	if got := outs["ok"].Value; got != "yes" {
		t.Fatalf("Outputs 'ok' = %v, want \"yes\"", got)
	}
}
