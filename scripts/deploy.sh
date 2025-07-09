#!/bin/bash
set -e

echo "Deploying JellySentia..."

# Check prerequisites
command -v docker >/dev/null 2>&1 || { echo "Docker is required but not installed. Aborting." >&2; exit 1; }
command -v docker-compose >/dev/null 2>&1 || { echo "Docker Compose is required but not installed. Aborting." >&2; exit 1; }

# Load environment
if [ -f .env ]; then
    export $(cat .env | grep -v '^#' | xargs)
else
    echo "No .env file found. Copying from .env.example..."
    cp .env.example .env
    echo "Please edit .env file and run deploy again."
    exit 1
fi

# Validate environment
if [ -z "$MEDIA_PATH" ]; then
    echo "MEDIA_PATH not set in .env file"
    exit 1
fi

if [ ! -d "$MEDIA_PATH" ]; then
    echo "MEDIA_PATH does not exist: $MEDIA_PATH"
    exit 1
fi

# Create necessary directories
mkdir -p data config/jellyfin cache

# Pull/build images
echo "Building Docker images..."
docker-compose build

# Start services
echo "Starting services..."
docker-compose up -d

# Wait for services to be ready
echo "Waiting for services to start..."
sleep 10

# Check service health
echo "Checking service health..."
docker-compose ps

# Run initial setup
echo "Running initial setup..."
docker-compose exec analysis-core python -c "
from analysis_core.client.client import SimilarityIndex
index = SimilarityIndex()
print('Similarity index initialized')
"

echo "Deployment complete!"
echo ""
echo "Access points:"
echo "  - Jellyfin: http://localhost:8096"
echo "  - JellySentia Web UI: http://localhost:8097"
echo ""
echo "Next steps:"
echo "  1. Log into Jellyfin and enable the JellySentia plugin"
echo "  2. Configure plugin settings"
echo "  3. Start library analysis from Scheduled Tasks"