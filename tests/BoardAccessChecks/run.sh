#!/usr/bin/env bash
set -euo pipefail

test_dir=$(cd -- "$(dirname -- "$0")" && pwd)
container_name="ds28-board-access-tests-$$"
trap 'docker rm -f "$container_name" >/dev/null 2>&1 || true' EXIT

docker run --rm -d --name "$container_name" \
    -e POSTGRES_PASSWORD=board-check-local \
    -e POSTGRES_DB=ds28_board_access_tests \
    -p 127.0.0.1::5432 --tmpfs /var/lib/postgresql/data \
    postgres:16-alpine >/dev/null

for attempt in {1..150}; do
    if docker exec "$container_name" pg_isready -U postgres -d ds28_board_access_tests >/dev/null 2>&1; then
        break
    fi
    if [ "$attempt" -eq 150 ]; then
        echo 'Test database did not become ready.' >&2
        exit 1
    fi
    sleep 0.2
done

port=$(docker port "$container_name" 5432/tcp)
port=${port##*:}
export BOARD_ACCESS_TEST_CONNECTION="Host=127.0.0.1;Port=$port;Username=postgres;Password=board-check-local;Database=ds28_board_access_tests"
dotnet run --project "$test_dir/BoardAccessChecks.csproj" --disable-build-servers
