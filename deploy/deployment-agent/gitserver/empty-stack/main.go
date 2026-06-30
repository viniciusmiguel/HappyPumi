// A resourceless Pulumi program served over git:// for the remote-workspace deployment demo. It
// registers the stack and one output but provisions no provider-backed resources, so a remote
// `pulumi up` exercises the full deployment + update lifecycle against HappyPumi with zero cloud infra.
package main

import "github.com/pulumi/pulumi/sdk/v3/go/pulumi"

func main() {
	pulumi.Run(func(ctx *pulumi.Context) error {
		ctx.Export("ok", pulumi.String("happypumi"))
		return nil
	})
}
