<script>
  import { onMount } from 'svelte';
  import Chart from 'chart.js/auto';
  
  let canvas;
  let chart;
  let selectedTrack = null;
  
  // Mock data - would fetch from API
  const mockFeatures = {
    tempo: 120,
    energy: 0.75,
    danceability: 0.82,
    valence: 0.65,
    acousticness: 0.15,
    instrumentalness: 0.89,
    speechiness: 0.05,
    loudness: -5.2
  };
  
  onMount(() => {
    createChart();
    
    return () => {
      if (chart) {
        chart.destroy();
      }
    };
  });
  
  function createChart() {
    const ctx = canvas.getContext('2d');
    
    chart = new Chart(ctx, {
      type: 'radar',
      data: {
        labels: ['Energy', 'Danceability', 'Valence', 'Acousticness', 'Instrumentalness', 'Speechiness'],
        datasets: [{
          label: 'Track Features',
          data: [
            mockFeatures.energy,
            mockFeatures.danceability,
            mockFeatures.valence,
            mockFeatures.acousticness,
            mockFeatures.instrumentalness,
            mockFeatures.speechiness
          ],
          backgroundColor: 'rgba(0, 123, 255, 0.2)',
          borderColor: 'rgba(0, 123, 255, 1)',
          borderWidth: 2,
          pointBackgroundColor: 'rgba(0, 123, 255, 1)',
          pointBorderColor: '#fff',
          pointHoverBackgroundColor: '#fff',
          pointHoverBorderColor: 'rgba(0, 123, 255, 1)'
        }]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        scales: {
          r: {
            beginAtZero: true,
            max: 1,
            ticks: {
              stepSize: 0.2
            }
          }
        },
        plugins: {
          legend: {
            display: false
          },
          tooltip: {
            callbacks: {
              label: function(context) {
                return context.label + ': ' + context.parsed.r.toFixed(2);
              }
            }
          }
        }
      }
    });
  }
</script>

<div class="visualizer">
  <h2>Feature Visualizer</h2>
  
  <div class="track-selector">
    <p>Select a track from your library to visualize its audio features</p>
    <button>Select Track</button>
  </div>
  
  {#if selectedTrack || true}
    <div class="visualization">
      <div class="chart-container">
        <canvas bind:this={canvas}></canvas>
      </div>
      
      <div class="feature-details">
        <h3>Feature Details</h3>
        <dl>
          <dt>Tempo</dt>
          <dd>{mockFeatures.tempo} BPM</dd>
          
          <dt>Energy</dt>
          <dd>{(mockFeatures.energy * 100).toFixed(0)}%</dd>
          
          <dt>Danceability</dt>
          <dd>{(mockFeatures.danceability * 100).toFixed(0)}%</dd>
          
          <dt>Valence (Mood)</dt>
          <dd>{(mockFeatures.valence * 100).toFixed(0)}%</dd>
          
          <dt>Acousticness</dt>
          <dd>{(mockFeatures.acousticness * 100).toFixed(0)}%</dd>
          
          <dt>Instrumentalness</dt>
          <dd>{(mockFeatures.instrumentalness * 100).toFixed(0)}%</dd>
          
          <dt>Speechiness</dt>
          <dd>{(mockFeatures.speechiness * 100).toFixed(0)}%</dd>
          
          <dt>Loudness</dt>
          <dd>{mockFeatures.loudness.toFixed(1)} dB</dd>
        </dl>
      </div>
    </div>
  {/if}
</div>

<style>
  .visualizer {
    max-width: 1000px;
  }
  
  .track-selector {
    text-align: center;
    margin-bottom: 30px;
  }
  
  button {
    padding: 10px 20px;
    background: #007bff;
    color: white;
    border: none;
    border-radius: 4px;
    cursor: pointer;
    font-size: 16px;
  }
  
  button:hover {
    background: #0056b3;
  }
  
  .visualization {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 30px;
  }
  
  .chart-container {
    height: 400px;
    position: relative;
  }
  
  .feature-details {
    background: #f8f9fa;
    padding: 20px;
    border-radius: 8px;
  }
  
  dl {
    margin: 0;
  }
  
  dt {
    font-weight: bold;
    margin-top: 10px;
    color: #666;
  }
  
  dd {
    margin: 5px 0 15px 0;
    font-size: 18px;
  }
</style>