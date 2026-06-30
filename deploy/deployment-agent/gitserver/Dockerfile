# Hermetic git source for the remote-workspace deployment demo. Serves the resourceless empty-stack
# program over git:// so a remote `pulumi up` has a repo to clone WITHOUT any public-internet dependency
# at deploy time (clean-room). go.sum is generated here (build-time network) so the served repo is
# self-contained for the executor's `pulumi up`.
FROM golang:1.26

WORKDIR /src/empty-stack
COPY empty-stack/ ./
# Pin a self-contained module graph (no local replace) so the executor can build the program offline-ish
# from the committed go.sum.
RUN go mod tidy

# Commit on branch `master` (the remote test clones refs/heads/master), then expose a bare repo.
RUN git -c init.defaultBranch=master init -q \
 && git -c user.email=demo@happypumi -c user.name=happypumi add -A \
 && git -c user.email=demo@happypumi -c user.name=happypumi commit -qm "empty-stack" \
 && git clone --bare -q . /repos/empty-stack.git

EXPOSE 9418
# --export-all serves every repo under base-path without per-repo export markers; demo-only, no auth.
CMD ["git", "daemon", "--reuseaddr", "--export-all", "--base-path=/repos", "--verbose"]
