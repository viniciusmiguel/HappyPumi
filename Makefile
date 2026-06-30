# HappyPumi developer & test tooling.
#
#   make build             build the whole solution
#   make dev               run the full Aspire topology + dashboard locally
#   make test              run everything (unit + CLI integration + automation API)
#   make test-unit         in-process component tests (needs Docker: Postgres via Testcontainers)
#   make test-integration  drive the REAL pulumi CLI against a live HappyPumi (HTTPS, self-signed)
#   make test-automation   drive the Go Automation API SDK against a live HappyPumi (needs Go)
#   make pulumi            build the Apache-2.0 pulumi CLI + Go language host from ../pulumi
#   make certs             create + trust the self-signed HTTPS dev certificate
#   make coverage          run tests with coverage collection
#   make docker            build the production container image
#   make clean             remove build outputs (keeps the built CLI)
#
# The pulumi CLI is built from source (clean-room: used only as a black-box client over the wire —
# see docs/adr/0008). Integration tests need no cloud infra: login/stack/config/export hit the
# backend directly, and the update lifecycle is exercised with a resourceless Go program.

SLN            := HappyPumi.slnx
UNIT_TESTS     := HappyPumi.Api.Tests
INTEG_TESTS    := HappyPumi.Cli.IntegrationTests
AUTO_TESTS     := HappyPumi.AutomationApi.IntegrationTests
PULUMI_BIN     := $(CURDIR)/.tools/bin/pulumi
DOCKER_IMAGE   := happypumi-api
CONFIG         ?= Debug

.DEFAULT_GOAL := help

.PHONY: help
help:
	@grep -E '^#   make ' Makefile | sed 's/^#   /  /'

.PHONY: build
build:
	dotnet build $(SLN) -c $(CONFIG)

.PHONY: dev
dev: certs
	dotnet run --project HappyPumi.AppHost

# Create + trust the self-signed ASP.NET Core HTTPS dev certificate (ADR-0007). On Linux this
# populates ~/.aspnet/dev-certs/trust, which the tests and the pulumi CLI consult via SSL_CERT_DIR.
.PHONY: certs
certs:
	dotnet dev-certs https --trust || true

# Build the pulumi CLI + Go language host only if they aren't already present.
$(PULUMI_BIN):
	bash tools/build-pulumi-cli.sh

.PHONY: pulumi
pulumi: $(PULUMI_BIN)

.PHONY: test
test: test-unit test-integration test-automation

.PHONY: test-unit
test-unit:
	dotnet test $(UNIT_TESTS) -c $(CONFIG)

# Integration tests require the built CLI and the trusted self-signed cert; PULUMI_BIN points the
# harness at the locally-built CLI.
.PHONY: test-integration
test-integration: pulumi certs build
	PULUMI_BIN=$(PULUMI_BIN) dotnet test $(INTEG_TESTS) -c $(CONFIG) --no-build

.PHONY: test-automation
test-automation: pulumi certs build  ## Drive the Go Automation API SDK against a live HappyPumi (needs Go + Docker)
	PULUMI_BIN=$(PULUMI_BIN) dotnet test $(AUTO_TESTS) -c $(CONFIG) --no-build

.PHONY: coverage
coverage:
	dotnet test $(SLN) -c $(CONFIG) --collect:"XPlat Code Coverage"

.PHONY: docker
docker:
	docker build -f HappyPumi.Api/Dockerfile -t $(DOCKER_IMAGE) .

.PHONY: clean
clean:
	dotnet clean $(SLN) || true
	rm -rf */bin */obj
