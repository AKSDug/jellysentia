"""
Feature processing and vector generation
"""

import logging
from typing import Dict, Any, List
import numpy as np
from sklearn.preprocessing import StandardScaler, MinMaxScaler

logger = logging.getLogger(__name__)


class FeatureProcessor:
    """Process extracted features into normalized vectors"""
    
    def __init__(self):
        self.scaler = StandardScaler()
        self.feature_order = [
            'tempo', 'energy', 'danceability', 'valence', 'acousticness',
            'instrumentalness', 'speechiness', 'loudness', 'spectral_centroid',
            'spectral_rolloff', 'zero_crossing_rate', 'dynamic_complexity',
            'beats_confidence', 'onset_rate', 'key_strength', 'dissonance'
        ]
    
    def create_feature_vector(self, features: Dict[str, Any]) -> np.ndarray:
        """
        Create normalized feature vector from extracted features
        
        Args:
            features: Dictionary of extracted features
            
        Returns:
            Normalized feature vector
        """
        # Extract numeric features in consistent order
        vector_values = []
        
        for feature_name in self.feature_order:
            if feature_name in features:
                value = features[feature_name]
                
                # Handle different value types
                if isinstance(value, (int, float)):
                    vector_values.append(float(value))
                elif isinstance(value, str) and value.replace('.', '').isdigit():
                    vector_values.append(float(value))
                else:
                    # Use default value for missing/invalid features
                    vector_values.append(0.0)
            else:
                vector_values.append(0.0)
        
        # Convert to numpy array
        vector = np.array(vector_values, dtype=np.float32)
        
        # Normalize
        vector = self._normalize_vector(vector)
        
        return vector
    
    def _normalize_vector(self, vector: np.ndarray) -> np.ndarray:
        """Normalize feature vector"""
        # Handle specific feature ranges
        normalized = vector.copy()
        
        # Tempo: typically 60-200 BPM
        if len(normalized) > 0:
            normalized[0] = (normalized[0] - 60) / 140 if normalized[0] > 0 else 0
        
        # Most features are already 0-1, but ensure bounds
        normalized = np.clip(normalized, 0, 1)
        
        # L2 normalization for similarity search
        norm = np.linalg.norm(normalized)
        if norm > 0:
            normalized = normalized / norm
        
        return normalized
    
    def process_batch(self, feature_list: List[Dict[str, Any]]) -> np.ndarray:
        """Process multiple feature dictionaries into matrix"""
        vectors = []
        
        for features in feature_list:
            vector = self.create_feature_vector(features)
            vectors.append(vector)
        
        return np.array(vectors, dtype=np.float32)
    
    def reduce_dimensions(self, vectors: np.ndarray, n_components: int = 50) -> np.ndarray:
        """
        Reduce dimensionality of feature vectors using PCA
        
        Args:
            vectors: Feature vector matrix
            n_components: Number of components to keep
            
        Returns:
            Reduced dimension vectors
        """
        from sklearn.decomposition import PCA
        
        if vectors.shape[0] < n_components:
            return vectors
        
        pca = PCA(n_components=n_components)
        reduced = pca.fit_transform(vectors)
        
        logger.info(f"Reduced dimensions from {vectors.shape[1]} to {reduced.shape[1]}")
        logger.info(f"Explained variance ratio: {sum(pca.explained_variance_ratio_):.2f}")
        
        return reduced