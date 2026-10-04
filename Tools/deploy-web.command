#!/bin/zsh
set -e
PROJECT_DIR="$(cd "$(dirname "$0")/.." && pwd)"
cd "$PROJECT_DIR"
: "${CLOUDFLARE_ACCOUNT_ID:?Set CLOUDFLARE_ACCOUNT_ID to your Cloudflare account ID}"
if (( $(node -p "Number(process.versions.node.split('.')[0])") < 22 )); then
  print -u2 'Node.js 22 or newer is required. Select it with nvm use 22.'
  exit 1
fi
if [[ ! -f Builds/Web/index.html ]]; then
  print -u2 'Build the web version with Tools/build-web.command first.'
  exit 1
fi
npx --yes wrangler@4.147.0 pages deploy Builds/Web --project-name itetris --branch main
