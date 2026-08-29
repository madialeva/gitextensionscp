#!/usr/bin/env bash

set -uo pipefail

if (( $# > 1 )); then
    printf 'Usage: %s [Release|Debug]\n' "$0" >&2
    exit 2
fi

configuration="${1:-Release}"
case "$configuration" in
    Release|Debug)
        ;;
    *)
        printf 'Invalid configuration: %s. Expected Release or Debug.\n' "$configuration" >&2
        exit 2
        ;;
esac

script_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)" || exit 1
repo_root="$(cd -- "$script_dir/.." && pwd)" || exit 1
solution="$repo_root/GitExtensions.slnx"
tests_project="$repo_root/tests/app/UnitTests/GitCommands.Tests/GitCommands.Tests.csproj"
test_results_dir="$repo_root/artifacts/$configuration/TestResults"
test_results_file="$test_results_dir/GitCommands.Tests.trx"

mkdir -p "$test_results_dir" || {
    printf 'VERIFY-LINUX FAILED: could not create the test results directory.\n' >&2
    exit 1
}
rm -f "$test_results_file" || {
    printf 'VERIFY-LINUX FAILED: could not remove the previous test result.\n' >&2
    exit 1
}

start_time=$SECONDS

printf '\n=== Verify-Linux: build GitExtensions.slnx ===\n'
if ! dotnet build "$solution" -c "$configuration"; then
    printf '\nVERIFY-LINUX FAILED: build of the cross-platform solution failed.\n' >&2
    exit 1
fi

printf '\n=== Verify-Linux: GitCommands.Tests ===\n'
test_exit_code=0
dotnet test "$tests_project" -c "$configuration" \
    -p:VSTestResultsDirectory="$test_results_dir" \
    --logger 'trx;LogFileName=GitCommands.Tests.trx' \
    --results-directory "$test_results_dir" || test_exit_code=$?

elapsed_seconds=$((SECONDS - start_time))
elapsed_minutes=$((elapsed_seconds / 60))
elapsed_remainder=$((elapsed_seconds % 60))

printf '\n=== Verify-Linux: summary ===\n'
if (( test_exit_code == 0 )); then
    if [[ ! -f "$test_results_file" ]]; then
        printf '  FAIL GitCommands.Tests (no TRX result was produced)\n'
        test_exit_code=1
    else
        counters_line="$(grep -m1 '<Counters ' "$test_results_file" || true)"
        total_tests="$(sed -n 's/.*total="\([0-9][0-9]*\)".*/\1/p' <<< "$counters_line")"
        executed_tests="$(sed -n 's/.*executed="\([0-9][0-9]*\)".*/\1/p' <<< "$counters_line")"
        if [[ -z "$total_tests" || -z "$executed_tests" || "$total_tests" -eq 0 || "$executed_tests" -eq 0 ]]; then
            printf '  FAIL GitCommands.Tests (no tests were executed)\n'
            test_exit_code=1
        else
            printf '  PASS GitCommands.Tests (%s executed)\n' "$executed_tests"
        fi
    fi
else
    printf '  FAIL GitCommands.Tests\n'
fi

printf '\nElapsed: %02d:%02d. TRX logs: %s\n' \
    "$elapsed_minutes" "$elapsed_remainder" "$test_results_dir"

if (( test_exit_code != 0 )); then
    printf 'VERIFY-LINUX FAILED: GitCommands.Tests failed.\n' >&2
    exit "$test_exit_code"
fi

printf 'VERIFY-LINUX OK: cross-platform solution builds and GitCommands.Tests passes on Linux.\n'
exit 0