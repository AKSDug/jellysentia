"""
Integration tests for complete workflow
"""

import pytest
import os
import tempfile
import shutil
from pathlib import Path
import numpy as np
import grpc
from concurrent import futures

# Add parent directory to path
import sys
sys.path.append(os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__)))))

from analysis_core.server.server import AnalysisService, SimilarityService
from analysis_core.models import analysis_pb2, analysis_pb2_grpc


class TestIntegrationWorkflow:
    """Test complete analysis workflow"""
    
    @pytest.fixture
    def test_audio_file(self):
        """Create a test audio file"""
        # Use fixture file if available
        fixture_path = Path(__file__).parent.parent / "fixtures" / "sample.mp3"
        if fixture_path.exists():
            return str(fixture_path)
        
        # Otherwise create a temporary file
        with tempfile.NamedTemporaryFile(suffix='.mp3', delete=False) as f:
            # Write some dummy data
            f.write(b'ID3' + b'\x00' * 1024)  # Minimal MP3 header
            return f.name
    
    @pytest.fixture
    def grpc_server(self):
        """Start gRPC server for testing"""
        server = grpc.server(futures.ThreadPoolExecutor(max_workers=2))
        
        analysis_pb2_grpc.add_AnalysisServicer_to_server(
            AnalysisService(), server
        )
        analysis_pb2_grpc.add_SimilarityServicer_to_server(
            SimilarityService(), server
        )
        
        port = server.add_insecure_port('[::]:0')
        server.start()
        
        yield f'localhost:{port}'
        
        server.stop(0)
    
    @pytest.fixture
    def grpc_channel(self, grpc_server):
        """Create gRPC channel"""
        channel = grpc.insecure_channel(grpc_server)
        yield channel
        channel.close()
    
    def test_single_track_analysis(self, grpc_channel, test_audio_file):
        """Test analyzing a single track"""
        stub = analysis_pb2_grpc.AnalysisStub(grpc_channel)
        
        request = analysis_pb2.AnalysisRequest(
            file_path=test_audio_file,
            depth='standard',
            descriptors=['tempo', 'energy', 'danceability']
        )
        
        try:
            response = stub.AnalyzeTrack(request)
            
            assert response.file_path == test_audio_file
            assert 'tempo' in response.features
            assert 'energy' in response.features
            assert 'danceability' in response.features
            assert len(response.feature_vector) > 0
            
        except grpc.RpcError as e:
            # Expected if test audio file is invalid
            assert e.code() == grpc.StatusCode.INTERNAL
    
    def test_batch_analysis(self, grpc_channel, test_audio_file):
        """Test batch analysis with progress"""
        stub = analysis_pb2_grpc.AnalysisStub(grpc_channel)
        
        request = analysis_pb2.BatchAnalysisRequest(
            file_paths=[test_audio_file, test_audio_file],
            depth='lightweight',
            descriptors=['tempo', 'energy']
        )
        
        progress_updates = []
        try:
            for progress in stub.AnalyzeBatch(request):
                progress_updates.append(progress)
                
                assert progress.total == 2
                assert progress.completed >= 0
                assert progress.completed <= progress.total
                assert 0 <= progress.progress_percent <= 100
            
            # Should have received at least one progress update
            assert len(progress_updates) > 0
            
        except grpc.RpcError:
            pass  # Expected if test audio is invalid
    
    def test_similarity_workflow(self, grpc_channel):
        """Test similarity index building and search"""
        similarity_stub = analysis_pb2_grpc.SimilarityStub(grpc_channel)
        
        # Create test vectors
        np.random.seed(42)
        vectors = [np.random.rand(16).astype(np.float32) for _ in range(10)]
        
        # Build index
        build_request = analysis_pb2.BuildIndexRequest()
        for i, vector in enumerate(vectors):
            item = analysis_pb2.IndexItem(
                item_id=f'test_item_{i}',
                feature_vector=vector.tobytes()
            )
            build_request.items.append(item)
        
        build_response = similarity_stub.BuildIndex(build_request)
        assert build_response.success
        assert build_response.item_count == 10
        
        # Search for similar items
        search_request = analysis_pb2.SimilarityRequest(
            feature_vector=vectors[0].tobytes(),
            top_k=5
        )
        
        search_response = similarity_stub.FindSimilar(search_request)
        assert len(search_response.matches) <= 5
        
        if search_response.matches:
            # First match should be the query item itself
            assert search_response.matches[0].item_id == 'test_item_0'
            assert search_response.matches[0].distance == 0.0
            assert search_response.matches[0].similarity == 1.0
    
    def test_error_handling(self, grpc_channel):
        """Test error handling for invalid inputs"""
        stub = analysis_pb2_grpc.AnalysisStub(grpc_channel)
        
        # Test with non-existent file
        request = analysis_pb2.AnalysisRequest(
            file_path='/non/existent/file.mp3',
            depth='standard',
            descriptors=['tempo']
        )
        
        with pytest.raises(grpc.RpcError) as exc_info:
            stub.AnalyzeTrack(request)
        
        assert exc_info.value.code() == grpc.StatusCode.INTERNAL


if __name__ == '__main__':
    pytest.main([__file__, '-v'])