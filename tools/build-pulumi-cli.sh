#!/usr/bin/env bash
# Builds the Apache-2.0 Pulumi CLI from source so the integration tests can drive
# a *real* client against HappyPumi over the wire. Clean-room: we use the CLI only
# as a black-box HTTP client (see docs/adr/0008-clean-room-implementation.md).
#
# The binary is cached at .tools/bin/pulumi and rebuilt only when missing (pass
# --force to rebuild). Override the source checkout with PULUMI_SRC.
set -euo pipefail

HAPPYPUMI_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PULUMI_SRC="${PULUMI_SRC:-$(cd "${HAPPYPUMI_ROOT}/../pulumi" 2>/dev/null && pwd || true)}"
OUT_DIR="${HAPPYPUMI_ROOT}/.tools/bin"
OUT_BIN="${OUT_DIR}/pulumi"
# Stamp the real version of the ../pulumi checkout (v3.246.0 line) so the Automation API SDK's
# per-feature version gates (e.g. ">= 3.181.0 required for --client with refresh") pass — the CLI is
# built from that source, so a lower dev-flavoured string would falsely fail those gates.
PULUMI_BUILD_VERSION="${PULUMI_BUILD_VERSION:-v3.246.0}"

if [[ "${1:-}" != "--force" && -x "${OUT_BIN}" ]]; then
  echo "pulumi CLI already built: ${OUT_BIN}"
  echo "${OUT_BIN}"
  exit 0
fi

if [[ -z "${PULUMI_SRC}" || ! -d "${PULUMI_SRC}/pkg/cmd/pulumi" ]]; then
  echo "error: Pulumi CLI source not found." >&2
  echo "       expected a checkout at ../pulumi or set PULUMI_SRC=<path to github.com/pulumi/pulumi>." >&2
  exit 1
fi

echo "Building pulumi CLI from ${PULUMI_SRC} (version ${PULUMI_BUILD_VERSION})..."
mkdir -p "${OUT_DIR}"
# Mirrors the upstream Makefile 'bin/pulumi' target: build ./cmd/pulumi from the pkg module.
( cd "${PULUMI_SRC}/pkg" && go build \
    -o "${OUT_BIN}" \
    -ldflags "-X github.com/pulumi/pulumi/sdk/v3/go/common/version.Version=${PULUMI_BUILD_VERSION}" \
    ./cmd/pulumi )

# The Go language host, built from the same monorepo and dropped next to the CLI so the engine
# resolves it locally (no network) — see workspace/plugins.go. This lets `pulumi up` run a
# resourceless Go program through the FULL backend lifecycle WITHOUT any cloud infra. The DIY /
# internal mock backends would instead bypass HappyPumi, so they can't validate our API.
echo "Building pulumi-language-go host (no-infra lifecycle tests)..."
# pulumi-language-go is its own Go module, so build it from inside its directory.
( cd "${PULUMI_SRC}/sdk/go/pulumi-language-go" && go build \
    -o "${OUT_DIR}/pulumi-language-go" \
    -ldflags "-X github.com/pulumi/pulumi/sdk/v3/go/common/version.Version=${PULUMI_BUILD_VERSION}" \
    . )

echo "Built: ${OUT_BIN}"
"${OUT_BIN}" version || true
echo "${OUT_BIN}"
