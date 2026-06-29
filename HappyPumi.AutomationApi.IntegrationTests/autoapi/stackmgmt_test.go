package autoapi

import (
	"context"
	"testing"

	"github.com/pulumi/pulumi/sdk/v3/go/auto"
)

// TestOrgDefault exercises Workspace.OrgGetDefault -> GET /api/user/organizations/default.
func TestOrgDefault(t *testing.T) {
	ctx := context.Background()
	ws, err := LoginWorkspace(ctx)
	if err != nil {
		t.Fatalf("LoginWorkspace: %v", err)
	}
	org, err := ws.OrgGetDefault(ctx)
	if err != nil {
		t.Fatalf("OrgGetDefault: %v", err)
	}
	if org == "" {
		t.Fatal("OrgGetDefault returned an empty org")
	}
}

// TestStackLifecycle exercises CreateStack/Select (UpsertStackInlineSource), Info (GetStack),
// ListStacks (ListUserStacks), and RemoveStack (DeleteStack) — the stack-management surface.
func TestStackLifecycle(t *testing.T) {
	ctx := context.Background()
	proj := "hp-auto-mgmt"
	name := qualified(proj, "mgmt1")
	s, err := auto.UpsertStackInlineSource(ctx, name, proj, noop)
	if err != nil {
		t.Fatalf("UpsertStackInlineSource: %v", err)
	}
	defer func() { _ = s.Workspace().RemoveStack(ctx, name) }()

	info, err := s.Info(ctx)
	if err != nil {
		t.Fatalf("Info: %v", err)
	}
	if info.Name == "" {
		t.Fatal("Info returned an empty stack name")
	}

	stacks, err := s.Workspace().ListStacks(ctx)
	if err != nil {
		t.Fatalf("ListStacks: %v", err)
	}
	if len(stacks) == 0 {
		t.Fatal("ListStacks returned no stacks after creating one")
	}
}
