"use strict";
const { PolicyPack } = require("@pulumi/policy");

// HappyPumi demo policy pack. The fake-cloud "RandomPet" resource backs every Widget; this advisory
// policy flags pets whose generated id has fewer than 5 words, which our demo Widget (length 2) trips —
// producing a visible policy finding while still allowing the deployment to succeed.
new PolicyPack("widget-policy", {
    policies: [
        {
            name: "randompet-min-length",
            description: "RandomPet length should be at least 5 words for readable widget ids.",
            enforcementLevel: "advisory",
            validateResource: (args, reportViolation) => {
                if (args.type !== "random:index/randomPet:RandomPet") {
                    return;
                }
                const length = args.props.length || 0;
                if (length < 5) {
                    reportViolation(
                        `RandomPet length ${length} is below the recommended minimum of 5.`);
                }
            },
        },
    ],
});
