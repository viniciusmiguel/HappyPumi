// A resourceless Pulumi program: it registers the stack and a single output but provisions no
// provider-backed resources. This is enough to exercise HappyPumi's update lifecycle end-to-end
// with zero cloud infra — see UpdateLifecycleTests.
package main

import "github.com/pulumi/pulumi/sdk/v3/go/pulumi"

func main() {
	pulumi.Run(func(ctx *pulumi.Context) error {
		ctx.Export("ok", pulumi.String("happypumi"))
		return nil
	})
}
