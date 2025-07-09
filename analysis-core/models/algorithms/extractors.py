"""
Audio feature extraction using Essentia
"""

import os
import logging
from typing import Dict, List, Optional, Any
import numpy as np
import essentia
import essentia.standard as es

logger = logging.getLogger(__name__)


class AudioAnalyzer:
    """Main audio analysis class using Essentia"""
    
    def __init__(self):
        self.sample_rate = 44100
        self.frame_size = 2048
        self.hop_size = 1024
        
    def analyze_file(self, file_path: str, depth: str = "standard", 
                    descriptors: Optional[List[str]] = None) -> Dict[str, Any]:
        """
        Analyze audio file and extract features
        
        Args:
            file_path: Path to audio file
            depth: Analysis depth (lightweight, standard, comprehensive)
            descriptors: List of specific descriptors to extract
            
        Returns:
            Dictionary of extracted features
        """
        try:
            # Load audio
            audio = self._load_audio(file_path)
            
            # Extract features based on depth
            features = {}
            
            if depth == "lightweight":
                features.update(self._extract_basic_features(audio))
            elif depth == "standard":
                features.update(self._extract_basic_features(audio))
                features.update(self._extract_rhythm_features(audio))
                features.update(self._extract_tonal_features(audio))
            else:  # comprehensive
                features.update(self._extract_basic_features(audio))
                features.update(self._extract_rhythm_features(audio))
                features.update(self._extract_tonal_features(audio))
                features.update(self._extract_spectral_features(audio))
                features.update(self._extract_highlevel_features(audio))
            
            # Filter by requested descriptors if specified
            if descriptors:
                features = {k: v for k, v in features.items() if k in descriptors}
            
            return features
            
        except Exception as e:
            logger.error(f"Error analyzing {file_path}: {str(e)}")
            raise
    
    def _load_audio(self, file_path: str) -> np.ndarray:
        """Load audio file using Essentia"""
        loader = es.MonoLoader(filename=file_path, sampleRate=self.sample_rate)
        return loader()
    
    def _extract_basic_features(self, audio: np.ndarray) -> Dict[str, float]:
        """Extract basic audio features"""
        features = {}
        
        # Loudness
        loudness = es.Loudness()(audio)
        features['loudness'] = float(loudness)
        
        # Dynamic complexity
        dynamic_complexity = es.DynamicComplexity()(audio)
        features['dynamic_complexity'] = float(dynamic_complexity)
        
        # Zero crossing rate
        zcr = es.ZeroCrossingRate()(audio)
        features['zero_crossing_rate'] = float(np.mean(zcr))
        
        # Energy
        energy = es.Energy()(audio)
        features['energy'] = float(energy)
        
        return features
    
    def _extract_rhythm_features(self, audio: np.ndarray) -> Dict[str, Any]:
        """Extract rhythm-related features"""
        features = {}
        
        # Beat tracking
        rhythm_extractor = es.RhythmExtractor2013()
        bpm, beats, beats_confidence, _, beats_intervals = rhythm_extractor(audio)
        
        features['tempo'] = float(bpm)
        features['beats_confidence'] = float(beats_confidence)
        features['beat_count'] = len(beats)
        
        # Onset detection
        onset_detector = es.OnsetDetection(method='complex')
        onsets = []
        for frame in es.FrameGenerator(audio, frameSize=self.frame_size, hopSize=self.hop_size):
            onsets.append(onset_detector(frame))
        
        features['onset_rate'] = float(len(onsets) / (len(audio) / self.sample_rate))
        
        # Danceability
        danceability = es.Danceability()(audio)
        features['danceability'] = float(danceability)
        
        return features
    
    def _extract_tonal_features(self, audio: np.ndarray) -> Dict[str, Any]:
        """Extract tonal/harmonic features"""
        features = {}
        
        # Key detection
        key_extractor = es.KeyExtractor()
        key, scale, key_strength = key_extractor(audio)
        
        features['key'] = key
        features['scale'] = scale
        features['key_strength'] = float(key_strength)
        
        # Chords
        chord_extractor = es.ChordsDetection()
        chords, chord_strengths = chord_extractor(audio)
        
        # Get most common chord
        if len(chords) > 0:
            unique_chords, counts = np.unique(chords, return_counts=True)
            features['dominant_chord'] = unique_chords[np.argmax(counts)]
        
        # Dissonance
        spectral_peaks = es.SpectralPeaks()(audio)
        dissonance = es.Dissonance()(spectral_peaks[0], spectral_peaks[1])
        features['dissonance'] = float(np.mean(dissonance))
        
        return features
    
    def _extract_spectral_features(self, audio: np.ndarray) -> Dict[str, float]:
        """Extract spectral features"""
        features = {}
        
        # Spectral centroid
        centroid = es.Centroid()(audio)
        features['spectral_centroid'] = float(centroid)
        
        # Spectral rolloff
        rolloff = es.RollOff()(audio)
        features['spectral_rolloff'] = float(rolloff)
        
        # Spectral flux
        flux_values = []
        for frame in es.FrameGenerator(audio, frameSize=self.frame_size, hopSize=self.hop_size):
            spectrum = es.Spectrum()(frame)
            flux = es.Flux()(spectrum)
            flux_values.append(flux)
        features['spectral_flux'] = float(np.mean(flux_values))
        
        # MFCC
        mfcc_extractor = es.MFCC()
        mfcc_values = []
        for frame in es.FrameGenerator(audio, frameSize=self.frame_size, hopSize=self.hop_size):
            spectrum = es.Spectrum()(frame)
            _, mfcc = mfcc_extractor(spectrum)
            mfcc_values.append(mfcc)
        
        mfcc_mean = np.mean(mfcc_values, axis=0)
        features['mfcc_mean'] = ','.join(map(str, mfcc_mean))
        
        return features
    
    def _extract_highlevel_features(self, audio: np.ndarray) -> Dict[str, float]:
        """Extract high-level semantic features"""
        features = {}
        
        # Mood detection (simplified - would use ML model in production)
        # Using energy and valence as proxies
        energy = es.Energy()(audio)
        centroid = es.Centroid()(audio)
        
        # Valence (positivity) - simplified heuristic
        valence = (centroid / 22050) * energy  # Normalize and combine
        features['valence'] = float(min(1.0, valence))
        
        # Acousticness - based on spectral features
        zcr = es.ZeroCrossingRate()(audio)
        acousticness = 1.0 - (np.mean(zcr) / 0.5)  # Inverse of ZCR
        features['acousticness'] = float(max(0.0, min(1.0, acousticness)))
        
        # Instrumentalness - simplified (would use ML model)
        # Using high frequency content as proxy
        hfc = es.HFC()(audio)
        instrumentalness = min(1.0, hfc / 1000)
        features['instrumentalness'] = float(instrumentalness)
        
        # Speechiness - simplified
        # Using zero crossing rate and spectral rolloff
        rolloff = es.RollOff()(audio)
        speechiness = (np.mean(zcr) * 2) * (1 - rolloff / 22050)
        features['speechiness'] = float(max(0.0, min(1.0, speechiness)))
        
        return features