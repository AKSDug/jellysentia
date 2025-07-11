# CONCEPT JellySentia

Essentia-powered music analysis plugin for Jellyfin that brings Plexamp-class sonic analysis and smart playlist generation to your self-hosted media server.

## UNTESTED AS OF JULY 9, 2025

## Features

- 🎵 **Comprehensive Audio Analysis**: Extract 80+ musical descriptors using Essentia
- 🏷️ **Smart Tagging**: Automatic tagging with tempo, key, mood, energy, and more
- 🎯 **Similarity Engine**: Find similar tracks based on sonic characteristics
- 📊 **Smart Playlists**: Create dynamic playlists based on musical features
- 🚀 **Fast Processing**: Incremental scanning with resumable queues
- 🐳 **Easy Deployment**: Single-command Docker deployment

## Quick Start

```bash
# Clone the repository
git clone https://github.com/AKSDug/jellysentia.git
cd jellysentia

# Copy environment template
cp .env.example .env

# Start services
docker-compose up -d

# Access web UI at http://localhost:8096
```

## Requirements

- Docker & Docker Compose
- Jellyfin 10.8.0+
- 4GB RAM minimum (8GB recommended)
- 2 CPU cores minimum (4+ recommended)

## Installation

### Docker Installation (Recommended)

1. Clone this repository
2. Configure environment variables in `.env`
3. Run `docker-compose up -d`
4. Access Jellyfin and enable the JellySentia plugin

### Manual Installation

See [docs/user/installation.md](docs/user/installation.md) for detailed instructions.

## Configuration

Configure JellySentia through the Jellyfin plugin settings or web UI:

- **Analysis Depth**: Choose between lightweight, standard, or comprehensive analysis
- **Tag Writing**: Enable/disable writing analysis results to file tags
- **Similarity Index**: Configure similarity search parameters
- **Resource Limits**: Set CPU and memory limits for analysis

## API Documentation

JellySentia exposes REST endpoints for analysis and playlist generation:

- `GET /api/jellysentia/analyze/track/{id}` - Analyze single track
- `POST /api/jellysentia/analyze/library` - Start library analysis
- `GET /api/jellysentia/similarity/track/{id}` - Get similar tracks
- `POST /api/jellysentia/playlists/smart` - Generate smart playlist

See [API Documentation](docs/api/openapi.yaml) for complete reference.

## Development

```bash
# Install dependencies
cd plugin/jellyfin && dotnet restore
cd analysis-core && pip install -r requirements.txt
cd webui && npm install

# Run tests
./scripts/test.sh

# Build plugin
./scripts/build.sh
```

## License

AGPL-3.0 - See [LICENSE](LICENSE) for details.

## Contributing

Contributions welcome! Please read our [Contributing Guide](CONTRIBUTING.md) first.

## Acknowledgments

- [Essentia](https://essentia.upf.edu/) for audio analysis algorithms
- [Jellyfin](https://jellyfin.org/) for the media server platform
- Inspired by Plexamp's sonic analysis features
