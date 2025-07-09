<script>
  import { onMount } from 'svelte';
  import axios from 'axios';
  
  let analysisStatus = {
    isRunning: false,
    progress: 0,
    analyzed: 0,
    total: 0,
    errors: 0
  };
  
  let recentAnalysis = [];
  let isLoading = true;
  
  onMount(async () => {
    await fetchStatus();
    await fetchRecentAnalysis();
    
    // Poll for updates while analysis is running
    const interval = setInterval(async () => {
      if (analysisStatus.isRunning) {
        await fetchStatus();
      }
    }, 2000);
    
    return () => clearInterval(interval);
  });
  
  async function fetchStatus() {
    try {
      const response = await axios.get('/api/jellysentia/analyze/status');
      analysisStatus = response.data;
    } catch (error) {
      console.error('Error fetching status:', error);
    }
  }
  
  async function fetchRecentAnalysis() {
    try {
      // Mock data for now
      recentAnalysis = [
        { name: 'Track 1', artist: 'Artist 1', analyzedAt: new Date().toISOString() },
        { name: 'Track 2', artist: 'Artist 2', analyzedAt: new Date().toISOString() },
      ];
      isLoading = false;
    } catch (error) {
      console.error('Error fetching recent analysis:', error);
      isLoading = false;
    }
  }
  
  async function startLibraryAnalysis() {
    try {
      await axios.post('/api/jellysentia/analyze/library');
      await fetchStatus();
    } catch (error) {
      console.error('Error starting analysis:', error);
    }
  }
</script>

<div class="dashboard">
  <h2>Analysis Dashboard</h2>
  
  <div class="status-card">
    <h3>Library Analysis Status</h3>
    
    {#if analysisStatus.isRunning}
      <div class="progress-bar">
        <div class="progress-fill" style="width: {analysisStatus.progress}%"></div>
      </div>
      <p>Analyzing... {analysisStatus.analyzed} / {analysisStatus.total} tracks</p>
      {#if analysisStatus.errors > 0}
        <p class="error">Errors: {analysisStatus.errors}</p>
      {/if}
    {:else}
      <p>Analysis not running</p>
      <button class="primary" on:click={startLibraryAnalysis}>
        Start Library Analysis
      </button>
    {/if}
  </div>
  
  <div class="recent-analysis">
    <h3>Recently Analyzed</h3>
    
    {#if isLoading}
      <p>Loading...</p>
    {:else if recentAnalysis.length === 0}
      <p>No recent analysis</p>
    {:else}
      <table>
        <thead>
          <tr>
            <th>Track</th>
            <th>Artist</th>
            <th>Analyzed</th>
          </tr>
        </thead>
        <tbody>
          {#each recentAnalysis as track}
            <tr>
              <td>{track.name}</td>
              <td>{track.artist}</td>
              <td>{new Date(track.analyzedAt).toLocaleString()}</td>
            </tr>
          {/each}
        </tbody>
      </table>
    {/if}
  </div>
</div>

<style>
  .dashboard {
    max-width: 800px;
  }
  
  .status-card {
    background: #f8f9fa;
    padding: 20px;
    border-radius: 8px;
    margin-bottom: 30px;
  }
  
  .progress-bar {
    width: 100%;
    height: 20px;
    background: #e0e0e0;
    border-radius: 10px;
    overflow: hidden;
    margin: 10px 0;
  }
  
  .progress-fill {
    height: 100%;
    background: #007bff;
    transition: width 0.3s ease;
  }
  
  button.primary {
    background: #007bff;
    color: white;
    border: none;
    padding: 10px 20px;
    border-radius: 4px;
    cursor: pointer;
    font-size: 16px;
  }
  
  button.primary:hover {
    background: #0056b3;
  }
  
  table {
    width: 100%;
    border-collapse: collapse;
  }
  
  th, td {
    text-align: left;
    padding: 10px;
    border-bottom: 1px solid #ddd;
  }
  
  th {
    font-weight: bold;
    background: #f8f9fa;
  }
  
  .error {
    color: #dc3545;
  }
</style>