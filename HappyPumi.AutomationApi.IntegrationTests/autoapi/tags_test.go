package autoapi

import (
	"context"
	"strings"
	"testing"

	"github.com/pulumi/pulumi/sdk/v3/go/auto"
	"github.com/pulumi/pulumi/sdk/v3/go/auto/optrename"
)

// TestTagRoundTrip exercises SetTag (AddStackTag), GetTag/ListTags (GetStack), and RemoveTag
// (DeleteStackTag) — the tag operation that was an unimplemented stub before this work.
func TestTagRoundTrip(t *testing.T) {
	ctx := context.Background()
	proj := "hp-auto-tags"
	name := qualified(proj, "tags1")
	s, err := auto.UpsertStackInlineSource(ctx, name, proj, noop)
	if err != nil {
		t.Fatalf("UpsertStackInlineSource: %v", err)
	}
	defer func() { _ = s.Workspace().RemoveStack(ctx, name) }()

	if err := s.SetTag(ctx, "team", "platform"); err != nil {
		t.Fatalf("SetTag: %v", err)
	}
	got, err := s.GetTag(ctx, "team")
	if err != nil || got != "platform" {
		t.Fatalf("GetTag = %q err=%v, want \"platform\"", got, err)
	}
	if err := s.RemoveTag(ctx, "team"); err != nil {
		t.Fatalf("RemoveTag: %v", err)
	}
	tags, err := s.ListTags(ctx)
	if err != nil {
		t.Fatalf("ListTags: %v", err)
	}
	if _, ok := tags["team"]; ok {
		t.Fatal("tag 'team' still present after RemoveTag")
	}
}

// TestRename exercises Stack.Rename -> POST /api/stacks/.../rename.
//
// The auto SDK's Rename runs `pulumi stack rename <new>` (which succeeds against HappyPumi) and then
// re-reads History under the PRE-rename name without updating its own handle. On any backend that frees
// the old name on rename — HappyPumi and Pulumi Cloud alike — that post-step 404s ("no stack named
// <old>"). That is correct backend behavior and an upstream SDK quirk, not a HappyPumi gap (the SDK's
// own TestRename only unit-tests arg construction, never this flow). So tolerate that specific error and
// assert the rename actually took effect on the backend: the new stack is selectable, the old one is not.
func TestRename(t *testing.T) {
	ctx := context.Background()
	proj := "hp-auto-rename"
	name := qualified(proj, "rename1")
	s, err := auto.UpsertStackInlineSource(ctx, name, proj, noop)
	if err != nil {
		t.Fatalf("UpsertStackInlineSource: %v", err)
	}
	renamed := qualified(proj, "rename2")
	defer func() { _ = s.Workspace().RemoveStack(ctx, renamed) }()

	if _, err := s.Rename(ctx, optrename.StackName(renamed)); err != nil && !strings.Contains(err.Error(), "no stack named") {
		t.Fatalf("Rename: %v", err)
	}

	if _, err := auto.SelectStackInlineSource(ctx, renamed, proj, noop); err != nil {
		t.Fatalf("renamed stack %q not selectable after rename: %v", renamed, err)
	}
	if _, err := auto.SelectStackInlineSource(ctx, name, proj, noop); err == nil {
		t.Fatalf("old stack name %q still resolves after rename", name)
	}
}
