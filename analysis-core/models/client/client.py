"""
Client utilities and similarity index
"""

import logging
import os
from typing import List, Tuple, Optional
import numpy as np
import faiss
import pickle

logger = logging.getLogger(__name__)


class SimilarityIndex:
    """FAISS-based similarity index for fast nearest neighbor search"""
    
    def __init__(self, index_path: Optional[str] = None):
        self.index_path = index_path or os.environ.get('FAISS_INDEX_PATH', '/data/index')
        self.index = None
        self.item_ids = []
        self.dimension = None
        
        # Try to load existing index
        self._load_index()
    
    def build(self, vectors: np.ndarray, item_ids: List[str]):
        """
        Build similarity index from feature vectors
        
        Args:
            vectors: Feature vector matrix (n_samples, n_features)
            item_ids: List of item IDs corresponding to vectors
        """
        if len(vectors) != len(item_ids):
            raise ValueError("Number of vectors must match number of item IDs")
        
        self.dimension = vectors.shape[1]
        self.item_ids = item_ids
        
        # Create FAISS index
        # Using IndexHNSWFlat for good performance/accuracy tradeoff
        self.index = faiss.IndexHNSWFlat(self.dimension, 32)
        self.index.hnsw.efConstruction = 40
        
        # Add vectors to index
        self.index.add(vectors)
        
        logger.info(f"Built index with {len(vectors)} vectors of dimension {self.dimension}")
        
        # Save index
        self._save_index()
    
    def search(self, query_vector: np.ndarray, k: int = 10) -> List[Tuple[str, float, float]]:
        """
        Search for similar items
        
        Args:
            query_vector: Query feature vector
            k: Number of results to return
            
        Returns:
            List of (item_id, distance, similarity) tuples
        """
        if self.index is None:
            raise RuntimeError("Index not built or loaded")
        
        # Ensure query vector has correct shape
        if query_vector.ndim == 1:
            query_vector = query_vector.reshape(1, -1)
        
        # Search
        distances, indices = self.index.search(query_vector, k)
        
        # Convert to results
        results = []
        for i in range(len(indices[0])):
            idx = indices[0][i]
            if idx < len(self.item_ids):  # Valid index
                distance = distances[0][i]
                # Convert distance to similarity (1 - normalized distance)
                similarity = 1.0 / (1.0 + distance)
                results.append((self.item_ids[idx], distance, similarity))
        
        return results
    
    def add_items(self, vectors: np.ndarray, item_ids: List[str]):
        """Add new items to existing index"""
        if self.index is None:
            # First items - build new index
            self.build(vectors, item_ids)
            return
        
        # Add to existing index
        self.index.add(vectors)
        self.item_ids.extend(item_ids)
        
        logger.info(f"Added {len(vectors)} items to index")
        self._save_index()
    
    def _save_index(self):
        """Save index to disk"""
        os.makedirs(self.index_path, exist_ok=True)
        
        # Save FAISS index
        index_file = os.path.join(self.index_path, 'faiss.index')
        faiss.write_index(self.index, index_file)
        
        # Save metadata
        metadata = {
            'item_ids': self.item_ids,
            'dimension': self.dimension
        }
        metadata_file = os.path.join(self.index_path, 'metadata.pkl')
        with open(metadata_file, 'wb') as f:
            pickle.dump(metadata, f)
        
        logger.info(f"Saved index to {self.index_path}")
    
    def _load_index(self):
        """Load index from disk"""
        index_file = os.path.join(self.index_path, 'faiss.index')
        metadata_file = os.path.join(self.index_path, 'metadata.pkl')
        
        if os.path.exists(index_file) and os.path.exists(metadata_file):
            try:
                # Load FAISS index
                self.index = faiss.read_index(index_file)
                
                # Load metadata
                with open(metadata_file, 'rb') as f:
                    metadata = pickle.load(f)
                
                self.item_ids = metadata['item_ids']
                self.dimension = metadata['dimension']
                
                logger.info(f"Loaded index with {len(self.item_ids)} items")
            except Exception as e:
                logger.error(f"Error loading index: {str(e)}")
                self.index = None