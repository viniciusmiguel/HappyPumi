package autoapi

import (
	"context"
	"testing"

	"github.com/pulumi/pulumi/sdk/v3/go/auto"
)

// TestExportImport exercises Stack.Export (ExportStack) and Stack.Import (ImportStack): export the
// current (initial) deployment and re-import it, round-tripping state through HappyPumi.
func TestExportImport(t *testing.T) {
	ctx := context.Background()
	proj := "hp-auto-state"
	name := qualified(proj, "state1")
	s, err := auto.UpsertStackInlineSource(ctx, name, proj, noop)
	if err != nil {
		t.Fatalf("UpsertStackInlineSource: %v", err)
	}
	defer func() { _ = s.Workspace().RemoveStack(ctx, name) }()

	dep, err := s.Export(ctx)
	if err != nil {
		t.Fatalf("Export: %v", err)
	}
	if err := s.Import(ctx, dep); err != nil {
		t.Fatalf("Import: %v", err)
	}
}
