# JellySentia Quick Start Guide

Get up and running with JellySentia in under 10 minutes!

## Prerequisites

- Docker and Docker Compose installed
- Jellyfin server (10.8.0 or later)
- At least 4GB RAM available
- Music library accessible to Docker

## Installation

### 1. Clone the Repository

```bash
git clone https://github.com/AKSDug/jellysentia.git
cd jellysentia
```

### 2. Configure Environment

Copy the example environment file and edit it:

```bash
cp .env.example .env
nano .env
```

Key settings to configure:
- `MEDIA_PATH`: Path to your music library
- `JELLYFIN_URL`: Your Jellyfin server URL (if not using the bundled one)
- `ANALYSIS_THREADS`: Number of CPU threads for analysis (default: 4)

### 3. Start Services

```bash
docker-compose up -d
```

This will start:
- Jellyfin server (if not already running)
- JellySentia analysis core
- JellySentia plugin
- Web UI

### 4. Enable Plugin in Jellyfin

1. Open Jellyfin in your browser: `http://localhost:8096`
2. Log in as administrator
3. Go to Dashboard → Plugins
4. Find "JellySentia" and click "Enable"
5. Restart Jellyfin server

### 5. Configure Plugin

1. Go to Dashboard → Plugins → JellySentia
2. Configure settings:
   - **Analysis Depth**: Choose between Lightweight, Standard, or Comprehensive
   - **Tag Writeback**: Enable to save analysis results to file tags
   - **Concurrent Analysis**: Number of tracks to analyze simultaneously

### 6. Start Analysis

#### Option A: Analyze Entire Library
1. Go to Dashboard → Scheduled Tasks
2. Find "Music Analysis"
3. Click "Run Now"

#### Option B: Analyze Specific Tracks
1. Navigate to a track in your library
2. Click the "..." menu
3. Select "Analyze with JellySentia"

## Using JellySentia

### Access Web UI

Open `http://localhost:8097` to access the JellySentia web interface.

### Create Smart Playlists

1. In Jellyfin, go to Playlists
2. Click "Create Playlist"
3. Select "Smart Playlist (JellySentia)"
4. Set your criteria:
   - Tempo range (BPM)
   - Energy level
   - Mood (valence)
   - Musical key
5. Click "Create"

### Find Similar Tracks

1. Play any track in Jellyfin
2. Click "More Like This" (powered by JellySentia)
3. Browse similar tracks based on sonic characteristics

## Monitoring Progress

### Via Web UI
- Open `http://localhost:8097`
- View real-time analysis progress
- See recently analyzed tracks
- Check for any errors

### Via Jellyfin
- Go to Dashboard → Scheduled Tasks
- Check "Music Analysis" status

## Troubleshooting

### Analysis Not Starting
- Check Docker logs: `docker-compose logs analysis-core`
- Ensure media path is correctly mounted
- Verify file permissions

### Plugin Not Appearing
- Restart Jellyfin: `docker-compose restart jellyfin`
- Check plugin directory is mounted correctly
- Verify plugin compatibility with Jellyfin version

### Slow Analysis
- Reduce concurrent analysis threads in settings
- Check CPU and memory usage
- Consider using "Lightweight" analysis depth

### Features Not Appearing
- Ensure tracks have been analyzed
- Check database for results
- Verify tag writeback is enabled (if using file tags)

## Next Steps

- Read the [User Guide](user-guide.md) for detailed features
- Learn about [Smart Playlists](smart-playlists.md)
- Explore [API Documentation](../api/openapi.yaml) for automation

## Getting Help

- GitHub Issues: https://github.com/AKSDug/jellysentia/issues
- Documentation: https://github.com/AKSDug/jellysentia/docs
- Community Forum: [Link to forum]