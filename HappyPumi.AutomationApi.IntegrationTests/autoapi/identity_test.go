package autoapi

import (
	"context"
	"testing"
)

// TestWhoAmI is the smoke test for the layer: prove the Go auto SDK can authenticate against
// HappyPumi and resolve the current user (GET /api/user).
func TestWhoAmI(t *testing.T) {
	ctx := context.Background()
	ws, err := LoginWorkspace(ctx)
	if err != nil {
		t.Fatalf("LoginWorkspace: %v", err)
	}
	who, err := ws.WhoAmI(ctx)
	if err != nil {
		t.Fatalf("WhoAmI: %v", err)
	}
	if who == "" {
		t.Fatal("WhoAmI returned an empty user")
	}
}
