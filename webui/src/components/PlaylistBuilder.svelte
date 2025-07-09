<script>
  import { onMount } from 'svelte';
  import axios from 'axios';
  
  let playlistName = 'My Smart Playlist';
  let criteria = {
    minTempo: null,
    maxTempo: null,
    minEnergy: null,
    maxEnergy: null,
    minValence: null,
    maxValence: null,
    key: '',
    scale: '',
    limit: 50
  };
  
  let previewTracks = [];
  let isCreating = false;
  
  const musicalKeys = ['C', 'C#', 'D', 'D#', 'E', 'F', 'F#', 'G', 'G#', 'A', 'A#', 'B'];
  const scales = ['major', 'minor'];
  
  async function previewPlaylist() {
    try {
      const params = new URLSearchParams();
      Object.entries(criteria).forEach(([key, value]) => {
        if (value !== null && value !== '') {
          params.append(key, value);
        }
      });
      
      const response = await axios.get(`/api/jellysentia/playlists/smart/preview?${params}`);
      previewTracks = response.data.tracks;
    } catch (error) {
      console.error('Error previewing playlist:', error);
    }
  }
  
  async function createPlaylist() {
    isCreating = true;
    try {
      const response = await axios.post('/api/jellysentia/playlists/smart', {
        name: playlistName,
        ...criteria
      });
      
      alert(`Playlist created with ${response.data.itemCount} tracks!`);
    } catch (error) {
      console.error('Error creating playlist:', error);
      alert('Error creating playlist');
    } finally {
      isCreating = false;
    }
  }
</script>

<div class="playlist-builder">
  <h2>Smart Playlist Builder</h2>
  
  <div class="form-section">
    <label>
      Playlist Name:
      <input type="text" bind:value={playlistName} />
    </label>
  </div>
  
  <div class="criteria-section">
    <h3>Criteria</h3>
    
    <div class="criteria-grid">
      <div class="criteria-group">
        <h4>Tempo (BPM)</h4>
        <label>
          Min: <input type="number" bind:value={criteria.minTempo} min="60" max="200" />
        </label>
        <label>
          Max: <input type="number" bind:value={criteria.maxTempo} min="60" max="200" />
        </label>
      </div>
      
      <div class="criteria-group">
        <h4>Energy</h4>
        <label>
          Min: <input type="number" bind:value={criteria.minEnergy} min="0" max="1" step="0.1" />
        </label>
        <label>
          Max: <input type="number" bind:value={criteria.maxEnergy} min="0" max="1" step="0.1" />
        </label>
      </div>
      
      <div class="criteria-group">
        <h4>Valence (Mood)</h4>
        <label>
          Min: <input type="number" bind:value={criteria.minValence} min="0" max="1" step="0.1" />
        </label>
        <label>
          Max: <input type="number" bind:value={criteria.maxValence} min="0" max="1" step="0.1" />
        </label>
      </div>
      
      <div class="criteria-group">
        <h4>Musical Key</h4>
        <label>
          Key:
          <select bind:value={criteria.key}>
            <option value="">Any</option>
            {#each musicalKeys as key}
              <option value={key}>{key}</option>
            {/each}
          </select>
        </label>
        <label>
          Scale:
          <select bind:value={criteria.scale}>
            <option value="">Any</option>
            {#each scales as scale}
              <option value={scale}>{scale}</option>
            {/each}
          </select>
        </label>
      </div>
    </div>
    
    <div class="form-section">
      <label>
        Track Limit:
        <input type="number" bind:value={criteria.limit} min="1" max="500" />
      </label>
    </div>
  </div>
  
  <div class="actions">
    <button on:click={previewPlaylist}>Preview</button>
    <button class="primary" on:click={createPlaylist} disabled={isCreating}>
      {isCreating ? 'Creating...' : 'Create Playlist'}
    </button>
  </div>
  
  {#if previewTracks.length > 0}
    <div class="preview-section">
      <h3>Preview</h3>
      <p>Found {previewTracks.length} matching tracks</p>
    </div>
  {/if}
</div>

<style>
  .playlist-builder {
    max-width: 800px;
  }
  
  .form-section {
    margin-bottom: 20px;
  }
  
  label {
    display: block;
    margin-bottom: 10px;
  }
  
  input[type="text"],
  input[type="number"],
  select {
    width: 100%;
    padding: 8px;
    border: 1px solid #ddd;
    border-radius: 4px;
    font-size: 14px;
  }
  
  .criteria-section {
    background: #f8f9fa;
    padding: 20px;
    border-radius: 8px;
    margin-bottom: 20px;
  }
  
  .criteria-grid {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
    gap: 20px;
  }
  
  .criteria-group h4 {
    margin-bottom: 10px;
    color: #666;
  }
  
  .actions {
    display: flex;
    gap: 10px;
    margin-bottom: 20px;
  }
  
  button {
    padding: 10px 20px;
    border: 1px solid #ddd;
    border-radius: 4px;
    background: white;
    cursor: pointer;
    font-size: 16px;
  }
  
  button:hover {
    background: #f8f9fa;
  }
  
  button.primary {
    background: #007bff;
    color: white;
    border-color: #007bff;
  }
  
  button.primary:hover {
    background: #0056b3;
  }
  
  button:disabled {
    opacity: 0.6;
    cursor: not-allowed;
  }
  
  .preview-section {
    background: #f8f9fa;
    padding: 20px;
    border-radius: 8px;
  }
</style>