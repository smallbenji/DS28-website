#!/usr/bin/env bash
set -euo pipefail

image_tag="${SSH_ORIGINAL_COMMAND:-${1:-}}"
if [[ ! "$image_tag" =~ ^[0-9]+\.[0-9]+\.[0-9]+(_[0-9]+)?$ ]]; then
    echo "Ugyldigt image-tag: '$image_tag'" >&2
    exit 2
fi

config="${DS28_DEPLOY_CONFIG:-/opt/ds28/deploy.env}"
source "$config"
: "${COMPOSE_DIR:?}" "${DB_CONTAINER:?}" "${DB_NAME:?}" "${DB_USER:?}" "${BACKUP_DIR:?}"
read -ra services <<< "${SERVICES:-hq worker}"

mkdir -p "$BACKUP_DIR"
backup="$BACKUP_DIR/ds28-$(date -u +%Y%m%dT%H%M%SZ)-before-$image_tag.dump"
echo "Tager databasedump: $backup"
docker exec "$DB_CONTAINER" pg_dump -U "$DB_USER" -Fc "$DB_NAME" > "$backup.partial"
mv "$backup.partial" "$backup"

cd "$COMPOSE_DIR"
export DS28_VERSION="$image_tag"
docker compose pull "${services[@]}"
docker compose up -d "${services[@]}"
echo "$image_tag" > "$COMPOSE_DIR/.ds28-version"
echo "Deployet $image_tag"
