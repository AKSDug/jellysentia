#!/usr/bin/env python3
"""
JellySentia Analysis Core Server
Provides gRPC service for audio analysis using Essentia
"""

import os
import sys
import logging
import asyncio
from concurrent import futures
import grpc
import numpy as np
from typing import Dict, List, Optional, Tuple

# Add parent directory to path for imports
sys.path.append(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from models import analysis_pb2, analysis_pb2_grpc
from algorithms.extractors import AudioAnalyzer
from algorithms.processors import FeatureProcessor
from client.client import SimilarityIndex

# Configure logging
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s'
)
logger = logging.getLogger(__name__)


class AnalysisService(analysis_pb2_grpc.AnalysisServicer):
    """gRPC service implementation for audio analysis"""
    
    def __init__(self):
        self.analyzer = AudioAnalyzer()
        self.processor = FeatureProcessor()
        logger.info("Analysis service initialized")
    
    def AnalyzeTrack(self, request, context):
        """Analyze a single audio track"""
        try:
            logger.info(f"Analyzing track: {request.file_path}")
            
            # Extract features
            features = self.analyzer.analyze_file(
                request.file_path,
                depth=request.depth,
                descriptors=list(request.descriptors)
            )
            
            # Process into feature vector
            feature_vector = self.processor.create_feature_vector(features)
            
            # Convert features to string format for protobuf
            string_features = {k: str(v) for k, v in features.items()}
            
            return analysis_pb2.AnalysisResponse(
                file_path=request.file_path,
                features=string_features,
                feature_vector=feature_vector.tobytes()
            )
            
        except Exception as e:
            logger.error(f"Error analyzing {request.file_path}: {str(e)}")
            context.set_code(grpc.StatusCode.INTERNAL)
            context.set_details(str(e))
            return analysis_pb2.AnalysisResponse(
                file_path=request.file_path,
                error=str(e)
            )
    
    def AnalyzeBatch(self, request, context):
        """Analyze multiple tracks with progress updates"""
        total = len(request.file_paths)
        completed = 0
        errors = 0
        
        for file_path in request.file_paths:
            try:
                # Analyze single file
                single_request = analysis_pb2.AnalysisRequest(
                    file_path=file_path,
                    depth=request.depth,
                    descriptors=request.descriptors
                )
                
                self.AnalyzeTrack(single_request, context)
                completed += 1
                
            except Exception as e:
                logger.error(f"Error in batch analysis: {str(e)}")
                errors += 1
            
            # Send progress update
            progress = analysis_pb2.AnalysisProgress(
                total=total,
                completed=completed,
                errors=errors,
                current_file=file_path,
                progress_percent=(completed / total) * 100
            )
            yield progress


class SimilarityService(analysis_pb2_grpc.SimilarityServicer):
    """gRPC service implementation for similarity search"""
    
    def __init__(self):
        self.index = SimilarityIndex()
        logger.info("Similarity service initialized")
    
    def FindSimilar(self, request, context):
        """Find similar tracks based on feature vector"""
        try:
            # Convert bytes back to numpy array
            query_vector = np.frombuffer(request.feature_vector, dtype=np.float32)
            
            # Search index
            matches = self.index.search(query_vector, k=request.top_k)
            
            # Convert to protobuf format
            similarity_matches = []
            for item_id, distance, similarity in matches:
                match = analysis_pb2.SimilarityMatch(
                    item_id=item_id,
                    similarity=similarity,
                    distance=distance
                )
                similarity_matches.append(match)
            
            return analysis_pb2.SimilarityResponse(matches=similarity_matches)
            
        except Exception as e:
            logger.error(f"Error in similarity search: {str(e)}")
            context.set_code(grpc.StatusCode.INTERNAL)
            context.set_details(str(e))
            return analysis_pb2.SimilarityResponse()
    
    def BuildIndex(self, request, context):
        """Build similarity index from feature vectors"""
        try:
            # Extract vectors and IDs
            vectors = []
            item_ids = []
            
            for item in request.items:
                vector = np.frombuffer(item.feature_vector, dtype=np.float32)
                vectors.append(vector)
                item_ids.append(item.item_id)
            
            # Build index
            self.index.build(np.array(vectors), item_ids)
            
            return analysis_pb2.BuildIndexResponse(
                success=True,
                item_count=len(item_ids)
            )
            
        except Exception as e:
            logger.error(f"Error building index: {str(e)}")
            return analysis_pb2.BuildIndexResponse(
                success=False,
                error=str(e)
            )


def serve():
    """Start the gRPC server"""
    server = grpc.server(futures.ThreadPoolExecutor(max_workers=10))
    
    # Add services
    analysis_pb2_grpc.add_AnalysisServicer_to_server(
        AnalysisService(), server
    )
    analysis_pb2_grpc.add_SimilarityServicer_to_server(
        SimilarityService(), server
    )
    
    # Listen on port
    port = os.environ.get('GRPC_PORT', '50051')
    server.add_insecure_port(f'[::]:{port}')
    
    logger.info(f"Starting gRPC server on port {port}")
    server.start()
    server.wait_for_termination()


if __name__ == '__main__':
    serve()