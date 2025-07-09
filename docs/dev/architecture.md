# JellySentia Architecture

## Overview

JellySentia is a microservices-based plugin system that integrates Essentia audio analysis with Jellyfin. The architecture consists of three main components:

1. **Jellyfin Plugin** (C#/.NET)
2. **Analysis Core** (Python/Essentia)
3. **Web UI** (Svelte)

## Component Architecture

```
┌─────────────────┐     ┌──────────────────┐     ┌─────────────────┐
│                 │     │                  │     │                 │
│  Jellyfin       │────▶│  JellySentia     │────▶│  Analysis       │
│  Server         │     │  Plugin          │     │  Core           │
│                 │     │                  │     │                 │
└─────────────────┘     └──────────────────┘     └─────────────────┘
         │                       │                         │
         │                       │                         │
         ▼                       ▼                         ▼
┌─────────────────┐     ┌──────────────────┐     ┌─────────────────┐
│                 │     │                  │     │                 │
│  Media          │     │  SQLite          │     │  FAISS          │
│  Library        │     │  Database        │     │  Index          │
│                 │     │                  │     │                 │
└─────────────────┘     └──────────────────┘     └─────────────────┘
```

## Component Details

### Jellyfin Plugin

**Technology**: C# .NET 7.0  
**Responsibilities**:
- Integration with Jellyfin API
- Task scheduling and management
- REST API endpoints
- Database operations
- Tag writing

**Key Classes**:
- `Plugin.cs`: Main plugin entry point
- `AnalysisService.cs`: Scheduled task for library analysis
- `SimilarityService.cs`: Similar track recommendations
- `PlaylistService.cs`: Smart playlist generation

### Analysis Core

**Technology**: Python 3.11, Essentia, gRPC  
**Responsibilities**:
- Audio feature extraction
- Feature vector generation
- Similarity indexing
- gRPC service implementation

**Key Modules**:
- `extractors.py`: Essentia-based feature extraction
- `processors.py`: Feature normalization and vectorization
- `client.py`: FAISS similarity index management
- `server.py`: gRPC service implementation

### Web UI

**Technology**: Svelte, Vite  
**Responsibilities**:
- Analysis monitoring dashboard
- Smart playlist builder
- Feature visualization
- Configuration management

## Data Flow

### Analysis Workflow

1. **Track Discovery**
   - Plugin queries Jellyfin library
   - Identifies unanalyzed tracks
   - Queues for analysis

2. **Feature Extraction**
   - Plugin sends file path to Analysis Core via gRPC
   - Essentia extracts audio features
   - Features normalized into vector

3. **Data Persistence**
   - Features stored in SQLite database
   - Optional tag writeback to files
   - Feature vectors indexed in FAISS

4. **Similarity Index**
   - Batch indexing of feature vectors
   - HNSW algorithm for fast search
   - Periodic index rebuilds

### API Request Flow

```
Client Request
     │
     ▼
Jellyfin API
     │
     ▼
JellySentia Controller
     │
     ├──▶ Database Query
     │
     └──▶ gRPC Call to Analysis Core
              │
              ▼
         Response
```

## Database Schema

### analysis_results Table

```sql
CREATE TABLE analysis_results (
    id INTEGER PRIMARY KEY,
    item_id TEXT UNIQUE,
    file_path TEXT,
    analyzed_at DATETIME,
    tempo REAL,
    key TEXT,
    scale TEXT,
    energy REAL,
    danceability REAL,
    valence REAL,
    acousticness REAL,
    instrumentalness REAL,
    speechiness REAL,
    loudness REAL,
    spectral_centroid REAL,
    spectral_rolloff REAL,
    zero_crossing_rate REAL,
    mfcc_mean TEXT,
    chroma_mean TEXT,
    feature_vector BLOB
);
```

## Communication Protocols

### gRPC Services

**Analysis Service**:
- `AnalyzeTrack`: Single track analysis
- `AnalyzeBatch`: Batch analysis with progress
- `GetFeatures`: Retrieve stored features

**Similarity Service**:
- `FindSimilar`: Query similar tracks
- `BuildIndex`: Rebuild similarity index
- `QueryIndex`: Direct index queries

### REST API

All endpoints prefixed with `/api/jellysentia/`:

- `GET /analyze/track/{id}`: Analyze single track
- `POST /analyze/library`: Start library analysis
- `GET /similarity/track/{id}`: Get similar tracks
- `POST /playlists/smart`: Create smart playlist

## Performance Considerations

### Optimization Strategies

1. **Concurrent Processing**
   - Configurable worker threads
   - Semaphore-based rate limiting
   - Async/await patterns

2. **Caching**
   - In-memory feature cache
   - Persistent FAISS index
   - Database query optimization

3. **Resource Management**
   - Memory-mapped file access
   - Streaming audio processing
   - Batch database operations

### Scalability

- Horizontal scaling via multiple Analysis Core instances
- Load balancing through gRPC
- Distributed FAISS index sharding (future)

## Security

### Authentication
- Jellyfin token-based auth
- Plugin inherits Jellyfin permissions

### Data Protection
- Local-only gRPC communication
- Encrypted database storage (optional)
- No external data transmission

## Development Setup

### Prerequisites
- .NET SDK 7.0+
- Python 3.11+
- Docker & Docker Compose
- Node.js 18+

### Local Development

1. **Plugin Development**:
   ```bash
   cd plugin/jellyfin
   dotnet restore
   dotnet build
   ```

2. **Analysis Core**:
   ```bash
   cd analysis-core
   pip install -r server/requirements.txt
   python -m analysis_core.server.server
   ```

3. **Web UI**:
   ```bash
   cd webui
   npm install
   npm run dev
   ```

### Testing

- Unit tests: `pytest` (Python), `dotnet test` (C#)
- Integration tests: `docker-compose -f docker-compose.test.yml up`
- Load testing: `locust -f tests/load/locustfile.py`

## Deployment

### Docker Deployment
- Multi-stage builds for optimization
- Health checks for all services
- Automatic restart policies
- Volume persistence

### Configuration
- Environment variables
- Plugin settings UI
- Configuration hot-reload

## Future Enhancements

1. **Machine Learning**
   - Genre classification
   - Mood detection models
   - Personalized recommendations

2. **Advanced Features**
   - Real-time analysis during playback
   - Collaborative filtering
   - Cross-library analysis

3. **Performance**
   - GPU acceleration for analysis
   - Distributed processing
   - Edge caching
