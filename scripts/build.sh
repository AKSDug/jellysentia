#!/bin/bash
set -e

echo "Building JellySentia..."

# Build directories
BUILD_DIR="$(pwd)/build"
DIST_DIR="$(pwd)/dist"

# Clean previous builds
rm -rf "$BUILD_DIR" "$DIST_DIR"
mkdir -p "$BUILD_DIR" "$DIST_DIR"

# Build .NET plugin
echo "Building Jellyfin plugin..."
cd plugin/jellyfin
dotnet publish -c Release -o "$BUILD_DIR/plugin"
cd ../..

# Build Python analysis core
echo "Building analysis core..."
cd analysis-core
python -m pip install --target "$BUILD_DIR/analysis-core" -r server/requirements.txt
cp -r server algorithms models "$BUILD_DIR/analysis-core/"
cd ..

# Build Web UI
echo "Building web UI..."
cd webui
npm install
npm run build
cp -r dist "$BUILD_DIR/webui"
cd ..

# Build Docker images
echo "Building Docker images..."
docker build -f docker/Dockerfile.analysis -t jellysentia/analysis:latest .
docker build -f docker/Dockerfile.plugin -t jellysentia/plugin:latest .

# Package distribution
echo "Creating distribution package..."
cp -r docker "$DIST_DIR/"
cp docker-compose.yml "$DIST_DIR/"
cp README.md LICENSE "$DIST_DIR/"
cp -r docs "$DIST_DIR/"

# Create .env.example
cat > "$DIST_DIR/.env.example" << EOF
# JellySentia Configuration
MEDIA_PATH=/path/to/your/music
JELLYFIN_URL=http://localhost:8096
ANALYSIS_THREADS=4
ANALYSIS_DEPTH=standard
ENABLE_TAG_WRITEBACK=true
EOF

# Create archive
cd "$DIST_DIR"
tar -czf ../jellysentia-v1.0.0.tar.gz .
cd ..

echo "Build complete! Distribution package: jellysentia-v1.0.0.tar.gz"