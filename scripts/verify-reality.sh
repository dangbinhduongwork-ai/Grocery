#!/usr/bin/env bash
# Scans the repo for signs of fake/unfinished work. Exit code 1 when violations are found.
# Requires ripgrep (rg). Run from the repository root.
set -u
fail=0
check() {
  local title="$1"; shift
  local out
  out=$(rg --no-heading -n "$@" 2>/dev/null || true)
  if [ -n "$out" ]; then
    echo "### VIOLATION: $title"; echo "$out" | head -n 40; echo; fail=1
  fi
}
SRC=(src web/src)
check "NotImplementedException in source"        -g '*.cs' 'NotImplementedException' "${SRC[@]}"
check "TODO/FIXME without issue id (TODO(Q-123))" -g '*.cs' -g '*.ts' -g '*.tsx' '(TODO|FIXME)(?!\(Q-[0-9]+\))' --pcre2 "${SRC[@]}"
check "Skipped tests (C#)"                        -g '*.cs' '\[(Fact|Theory)\(Skip' tests
check "Skipped tests (JS/TS)"                     -g '*.ts' -g '*.tsx' '(\.skip\(|\bxit\(|\bxdescribe\(|test\.fixme)' web
check "Thread.Sleep in source/tests"              -g '*.cs' 'Thread\.Sleep' src tests
check "Fake/seed libraries in production code"    -g '*.cs' '(Bogus|Faker)' src
check "Client-side paid confirmation"             -g '*.ts' -g '*.tsx' '(markAsPaid|setPaid|paymentSucceeded\s*=\s*true)' web/src
check "Tokens in browser storage"                 -g '*.ts' -g '*.tsx' '(localStorage|sessionStorage)\.(set|get)Item\(.*(token|jwt)' --ignore-case web/src
check "Emoji used in UI source"                   -g '*.tsx' '[\x{1F300}-\x{1FAFF}\x{2600}-\x{27BF}]' --pcre2 web/src
check "Hard-coded secrets patterns"               -g '!*.md' '(sk_live|Authorization: Apikey [A-Za-z0-9]{8,}|Password=[^;{$]{4,};)' src web/src deploy
check "Commercial-license libs referenced"        -g '*.csproj' -g 'Directory.Packages.props' '(MediatR|AutoMapper|FluentAssertions|MassTransit)' .
if [ $fail -eq 0 ]; then echo "verify-reality: no violations"; fi
exit $fail
