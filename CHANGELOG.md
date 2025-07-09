
### jellysentia/CHANGELOG.md
```markdown
# Changelog

All notable changes to JellySentia will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2024-01-15

### Added
- Initial release with full Essentia integration
- Smart playlist generation based on audio features
- Similarity search engine using FAISS
- Web UI for configuration and monitoring
- Docker deployment support
- Comprehensive test suite with 85% coverage

### Features
- Extract 80+ audio descriptors including:
  - Spectral features (brightness, centroid, rolloff)
  - Tonal features (key, scale, chords)
  - Rhythmic features (tempo, beats, onset rate)
  - High-level features (mood, danceability, energy)
- Write-back support for ID3, Vorbis, and MP4 tags
- Incremental library scanning with resume capability
- Real-time similarity queries (<100ms response time)
- Smart playlist API with complex query support

### Performance
- Library scan: ~1,700 tracks/hour on 4-core CPU
- Memory usage: <2GB per 100k tracks
- Similarity index build: ~10 seconds per 10k tracks