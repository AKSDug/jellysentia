<script>
  import Dashboard from './components/Dashboard.svelte';
  import PlaylistBuilder from './components/PlaylistBuilder.svelte';
  import FeatureVisualizer from './components/FeatureVisualizer.svelte';
  
  let activeTab = 'dashboard';
  
  const tabs = [
    { id: 'dashboard', label: 'Dashboard', component: Dashboard },
    { id: 'playlists', label: 'Smart Playlists', component: PlaylistBuilder },
    { id: 'visualizer', label: 'Feature Visualizer', component: FeatureVisualizer }
  ];
</script>

<main>
  <header>
    <h1>JellySentia</h1>
    <p>Essentia-powered music analysis for Jellyfin</p>
  </header>
  
  <nav>
    {#each tabs as tab}
      <button
        class:active={activeTab === tab.id}
        on:click={() => activeTab = tab.id}
      >
        {tab.label}
      </button>
    {/each}
  </nav>
  
  <div class="content">
    {#each tabs as tab}
      {#if activeTab === tab.id}
        <svelte:component this={tab.component} />
      {/if}
    {/each}
  </div>
</main>

<style>
  :global(body) {
    margin: 0;
    font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, Cantarell, sans-serif;
    background-color: #f5f5f5;
  }
  
  main {
    max-width: 1200px;
    margin: 0 auto;
    padding: 20px;
  }
  
  header {
    text-align: center;
    margin-bottom: 30px;
  }
  
  h1 {
    color: #333;
    margin-bottom: 10px;
  }
  
  nav {
    display: flex;
    gap: 10px;
    margin-bottom: 30px;
    border-bottom: 2px solid #ddd;
  }
  
  button {
    background: none;
    border: none;
    padding: 10px 20px;
    cursor: pointer;
    font-size: 16px;
    color: #666;
    transition: all 0.3s;
  }
  
  button:hover {
    color: #333;
  }
  
  button.active {
    color: #007bff;
    border-bottom: 2px solid #007bff;
  }
  
  .content {
    background: white;
    padding: 30px;
    border-radius: 8px;
    box-shadow: 0 2px 4px rgba(0,0,0,0.1);
  }
</style>