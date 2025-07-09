"""
Unit tests for analysis core
"""

import pytest
import numpy as np
import tempfile
import os
from unittest.mock import Mock, patch

# Add parent directory to path
import sys
sys.path.append(os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__)))))

from analysis_core.algorithms.extractors import AudioAnalyzer
from analysis_core.algorithms.processors import FeatureProcessor
from analysis_core.client.client import SimilarityIndex


class TestAudioAnalyzer:
    """Test audio analysis functionality"""
    
    @pytest.fixture
    def analyzer(self):
        return AudioAnalyzer()
    
    @pytest.fixture
    def sample_audio(self):
        """Create a simple sine wave for testing"""
        sample_rate = 44100
        duration = 1.0
        frequency = 440.0
        t = np.linspace(0, duration, int(sample_rate * duration))
        audio = np.sin(2 * np.pi * frequency * t).astype(np.float32)
        return audio
    
    def test_analyzer_initialization(self, analyzer):
        """Test analyzer initializes correctly"""
        assert analyzer.sample_rate == 44100
        assert analyzer.frame_size == 2048
        assert analyzer.hop_size == 1024
    
    @patch('analysis_core.algorithms.extractors.es.MonoLoader')
    def test_load_audio(self, mock_loader, analyzer, sample_audio):
        """Test audio loading"""
        mock_loader.return_value = Mock(return_value=sample_audio)
        
        audio = analyzer._load_audio('test.mp3')
        
        assert len(audio) == len(sample_audio)
        mock_loader.assert_called_once()
    
    def test_extract_basic_features(self, analyzer, sample_audio):
        """Test basic feature extraction"""
        features = analyzer._extract_basic_features(sample_audio)
        
        assert 'loudness' in features
        assert 'energy' in features
        assert 'zero_crossing_rate' in features
        assert 'dynamic_complexity' in features
        
        # Check value ranges
        assert isinstance(features['energy'], float)
        assert 0 <= features['energy'] <= 1
    
    @patch('analysis_core.algorithms.extractors.es.RhythmExtractor2013')
    def test_extract_rhythm_features(self, mock_rhythm, analyzer, sample_audio):
        """Test rhythm feature extraction"""
        # Mock rhythm extractor output
        mock_rhythm.return_value = Mock(
            return_value=(120.0, np.array([0.5, 1.0, 1.5]), 0.95, None, None)
        )
        
        features = analyzer._extract_rhythm_features(sample_audio)
        
        assert 'tempo' in features
        assert 'beats_confidence' in features
        assert 'beat_count' in features
        assert 'onset_rate' in features
        assert 'danceability' in features
        
        assert features['tempo'] == 120.0
        assert features['beats_confidence'] == 0.95
        assert features['beat_count'] == 3


class TestFeatureProcessor:
    """Test feature processing functionality"""
    
    @pytest.fixture
    def processor(self):
        return FeatureProcessor()
    
    @pytest.fixture
    def sample_features(self):
        return {
            'tempo': 120.0,
            'energy': 0.75,
            'danceability': 0.82,
            'valence': 0.65,
            'acousticness': 0.15,
            'instrumentalness': 0.89,
            'speechiness': 0.05,
            'loudness': -5.2,
            'spectral_centroid': 2500.0,
            'spectral_rolloff': 5000.0,
            'zero_crossing_rate': 0.1,
            'dynamic_complexity': 0.6,
            'beats_confidence': 0.95,
            'onset_rate': 2.5,
            'key_strength': 0.8,
            'dissonance': 0.3
        }
    
    def test_create_feature_vector(self, processor, sample_features):
        """Test feature vector creation"""
        vector = processor.create_feature_vector(sample_features)
        
        assert isinstance(vector, np.ndarray)
        assert vector.dtype == np.float32
        assert len(vector) == len(processor.feature_order)
        
        # Check normalization
        assert np.all(vector >= 0)
        assert np.all(vector <= 1)
    
    def test_missing_features(self, processor):
        """Test handling of missing features"""
        incomplete_features = {
            'tempo': 120.0,
            'energy': 0.75
        }
        
        vector = processor.create_feature_vector(incomplete_features)
        
        assert len(vector) == len(processor.feature_order)
        # Missing features should be 0
        assert vector[2] == 0.0  # danceability missing
    
    def test_normalize_vector(self, processor):
        """Test vector normalization"""
        vector = np.array([120.0, 0.5, 0.8, 0.6], dtype=np.float32)
        normalized = processor._normalize_vector(vector)
        
        # First element (tempo) should be normalized
        assert normalized[0] == (120.0 - 60) / 140
        
        # Check L2 normalization
        norm = np.linalg.norm(normalized)
        assert np.isclose(norm, 1.0, atol=1e-6)
    
    def test_process_batch(self, processor, sample_features):
        """Test batch processing"""
        feature_list = [sample_features, sample_features]
        matrix = processor.process_batch(feature_list)
        
        assert matrix.shape == (2, len(processor.feature_order))
        assert matrix.dtype == np.float32


class TestSimilarityIndex:
    """Test similarity index functionality"""
    
    @pytest.fixture
    def index(self):
        with tempfile.TemporaryDirectory() as tmpdir:
            return SimilarityIndex(index_path=tmpdir)
    
    @pytest.fixture
    def sample_vectors(self):
        # Create some random vectors for testing
        np.random.seed(42)
        return np.random.rand(100, 16).astype(np.float32)
    
    @pytest.fixture
    def sample_ids(self):
        return [f"item_{i}" for i in range(100)]
    
    def test_build_index(self, index, sample_vectors, sample_ids):
        """Test building similarity index"""
        index.build(sample_vectors, sample_ids)
        
        assert index.index is not None
        assert len(index.item_ids) == 100
        assert index.dimension == 16
    
    def test_search(self, index, sample_vectors, sample_ids):
        """Test similarity search"""
        index.build(sample_vectors, sample_ids)
        
        # Search with first vector
        results = index.search(sample_vectors[0], k=5)
        
        assert len(results) == 5
        assert results[0][0] == "item_0"  # Should find itself first
        assert results[0][1] == 0.0  # Distance to itself is 0
        assert results[0][2] == 1.0  # Similarity to itself is 1
        
        # Check results are sorted by distance
        distances = [r[1] for r in results]
        assert distances == sorted(distances)
    
    def test_add_items(self, index, sample_vectors, sample_ids):
        """Test adding items to existing index"""
        # Build initial index with half the data
        index.build(sample_vectors[:50], sample_ids[:50])
        
        # Add remaining items
        index.add_items(sample_vectors[50:], sample_ids[50:])
        
        assert len(index.item_ids) == 100
    
    def test_save_load_index(self, index, sample_vectors, sample_ids):
        """Test saving and loading index"""
        index.build(sample_vectors, sample_ids)
        
        # Create new index instance with same path
        new_index = SimilarityIndex(index_path=index.index_path)
        
        assert new_index.index is not None
        assert len(new_index.item_ids) == 100
        assert new_index.dimension == 16
        
        # Test search works after loading
        results = new_index.search(sample_vectors[0], k=5)
        assert len(results) == 5


if __name__ == '__main__':
    pytest.main([__file__, '-v'])