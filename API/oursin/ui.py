"""Interactive components within notebooks"""
from . import particles
from . import meshes
from vbl_aquarium.models.generic import IDListFloatList

class interactive_plot :
    def __init__(self):
        self.avg_data = None
        self.neurons = None
        self.neuron_colors = None
        self.stim_id = None
        self.fig = None
        self.ax = None
        self.vline = None

    def avg_and_bin(self,  spike_times_raw_data, spike_clusters_data, event_start, event_ids, bin_size=0.02, bin_size_sec=0.02, window_start_sec = 0.1, window_end_sec = 0.5):
        #binning data:
        try:
            import numpy as np
        except ImportError:
            raise ImportError("Numpy package is not installed. Please pip install numpy to use this function.")
        # bin the spike times and clusters
        spike_times_raw_data = np.squeeze(spike_times_raw_data)
        spike_clusters_data = np.squeeze(spike_clusters_data)
        spike_times_sec = spike_times_raw_data / 3e4 # convert from 30khz samples to seconds
        # set up bin edges - 20 ms here
        bins_seconds = np.arange(np.min(spike_times_sec), np.max(spike_times_sec), bin_size)
        # make list of lists for spike times specific to each cluster
        spikes = [spike_times_sec[spike_clusters_data == cluster] for cluster in np.unique(spike_clusters_data)]
        # bin
        binned_spikes = []
        for cluster in spikes:
            counts, _ = np.histogram(cluster, bins_seconds)  
            binned_spikes.append(counts)
        binned_spikes = np.array(binned_spikes) # should be [#neurons, #bins]
        self.binned_spikes = binned_spikes

        #averaging data:
        bintime_prev = int(window_start_sec * 50)
        bintime_post = int(window_end_sec * 50 + 1)
        windowsize = bintime_prev + bintime_post
        bin_size = bin_size_sec * 1000

        # To bin: divide by 20, floor
        stim_binned = np.floor(event_start * 1000 / bin_size).astype(int)
        stim_binned = np.transpose(stim_binned)


        u_stim_ids = np.unique(event_ids)

        # Initialize final_avg matrix
        final_avg = np.empty((binned_spikes.shape[0], len(u_stim_ids), windowsize))

        for neuron_id in range(binned_spikes.shape[0]):

            for stim_id in u_stim_ids:
                stim_indices = np.where(event_ids[0] == stim_id)[0]

                neuron_stim_data = np.empty((len(stim_indices), windowsize))
                
                for i, stim_idx in enumerate(stim_indices):
                    bin_id = int(stim_binned[0][stim_idx])
                    selected_columns = binned_spikes[neuron_id, bin_id - bintime_prev: bin_id + bintime_post]
                    neuron_stim_data[i,:] = selected_columns

                bin_average = np.mean(neuron_stim_data, axis=0)/bin_size_sec
                final_avg[neuron_id, int(stim_id) - 1, :] = bin_average
        self.avg_data = final_avg

    def slope_viz_stimuli_per_neuron(self,change, t=-100):
        """Visualizes and creates interactive plot for the average of each stimulus per neuron
        
        Parameters
        ----------
        prepped_data: 3D array
            prepped data of averages of binned spikes and events in the format of [neuron_id, stimulus_id, time]
        t: int
                time in milliseconds of where to initially place the vertical line
            neuron_id: int
                id of neuron
            
        Examples
        --------
        >>> urchin.ui.slope_viz_stimuli_per_neuron(t=-100, neuron_id = 0)
        """
        try:
            import numpy as np
        except ImportError:
            raise ImportError("Numpy package is not installed. Please pip install numpy to use this function.")
        try:
            import matplotlib.pyplot as plt
        except ImportError:
            raise ImportError("Matplotlib package is not installed. Please pip install matplotlib to use this function.")
        
        # global global_prepped_data
        # global n_fig, n_ax, n_vline
        
        prepped_data = self.avg_data

        if isinstance(change, int):
            neuron_id = change
        else:
            neuron_id = change.new

        # Plotting data:
        self.ax.clear()
        for i in range(0,prepped_data.shape[1]):
            y = prepped_data[neuron_id][i]
            x = np.arange(-100, 520, step=20)
            self.ax.plot(x,y, color='dimgray')

        # Labels:
        self.ax.set_xlabel('Time from stimulus onset')
        self.ax.set_ylabel('Number of Spikes Per Second')
        self.ax.set_title(f'Neuron {neuron_id} Spiking Activity with Respect to Each Stimulus')

        #Accessories:
        self.ax.axvspan(0, 300, color='gray', alpha=0.3)
        self.vline = self.ax.axvline(t, color='red', linestyle='--',)
        # Set y-axis limits
        # Calculate y-axis limits
        max_y = max([max(prepped_data[neuron_id][i]) for i in range(10)])  # Maximum y-value across all lines
        if max_y < 10:
            max_y = 10  # Set ymax to 10 if the default max is lower than 10
        self.ax.set_ylim(0, max_y)
    

        
    def update_neuron_sizing(self, t):    
        prepped_data = self.avg_data
        stim_id = self.stim_id

        t_id = round((t+100)/20)
            
        size_list = []
        for i in range(prepped_data.shape[0]):
            size = round(prepped_data[i][stim_id][t_id]/200,4)
            size_list.append([size, size, size])


        meshes.set_scales(self.neurons, size_list)


    def slope_viz_neurons_per_stimuli(self, change, t=-100):
        """Visualizes and creates interactive plot for the average of every neuron per stimulus
        
        Parameters
        ----------
        prepped_data: 3D array
            prepped data of averages of binned spikes and events in the format of [neuron_id, stimulus_id, time]
        t: int
            time in milliseconds of where to initially place the vertical line
        stim_id: int
            id of neuron
        
        Examples
        --------
        >>> urchin.ui.slope_viz_stimuli_per_neuron(t=-100, stim_id = 0)
        """
        try:
            import numpy as np
        except ImportError:
            raise ImportError("Numpy package is not installed. Please pip install numpy to use this function.")
        try:
            import matplotlib.pyplot as plt
        except ImportError:
            raise ImportError("Matplotlib package is not installed. Please pip install matplotlib to use this function.")
        
        prepped_data = self.avg_data

        
        n_color = self.neuron_colors


        if isinstance(change, int):
            self.stim_id = change
        else:
            self.stim_id = change.new

        stim_id = self.stim_id

        # Plotting data:
        self.ax.clear()
        for i in range(0,prepped_data.shape[0]):
            y = prepped_data[i][stim_id]
            x = np.arange(-100, 520, step=20)
            self.ax.plot(x,y, color = n_color[i])
        
        # Labels:
        self.ax.set_xlabel(f'Time from Stimulus {stim_id} display (20 ms bins)')
        self.ax.set_ylabel('Number of Spikes Per Second')
        self.ax.set_title(f'Neuron Spiking Activity with Respect to Stimulus ID {stim_id}')

         #Accessories:
        self.ax.axvspan(0, 300, color='gray', alpha=0.3)
        self.vline = self.ax.axvline(t, color='red', linestyle='--',)


    def update_nline(self, position):
        # global n_vline, n_fig
        position = position.new
        self.vline.set_xdata([position, position])  # Update x value of the vertical line
        self.fig.canvas.draw_idle()

    def update_sline(self,t):
        t = t.new
        self.vline.set_xdata([t, t])  # Update x value of the vertical line
        self.fig.canvas.draw_idle()
        self.update_neuron_sizing(t)

    def plot_appropriate_interactive_graph(self, view = "stim", window_start_sec = 0.1, window_end_sec = 0.5):
        """Plots appropriate interactive graph based on view
        
        Parameters
        ----------
        prepped_data: 3D array
            prepped data of averages of binned spikes and events in the format of [neuron_id, stimulus_id, time]
        view: str
            view type, either "stim" or "neuron"
        window_start_sec: float
            start of window in seconds, default value is 0.1
        window_end_sec: float
            end of window in seconds, default value is 0.5
        
        Examples
        --------
        >>> urchin.ui.plot_appropriate_interactie_graph(prepped_data, view = "stim")
        """
        try:
            import ipywidgets as widgets
        except ImportError:
            raise ImportError("Widgets package is not installed. Please pip install ipywidgets to use this function.")
        
        try:
            import matplotlib.pyplot as plt
        except ImportError:
            raise ImportError("Matplotlib package is not installed. Please pip install matplotlib to use this function.")
        
        from IPython.display import display
            
        
        prepped_data = self.avg_data
        
        
        
        if view == "stim":
            self.fig, self.ax = plt.subplots()

            time_slider = widgets.IntSlider(value=-1e3 * window_start_sec, min=-1e3 * window_start_sec, max=5e3 * window_start_sec, step=5, description='Time')
            time_slider.layout.width = '6.53in'
            time_slider.layout.margin = '0 -4px'

            stimuli_dropdown = widgets.Dropdown(
                options= range(0,prepped_data.shape[1]),
                value=0,
                description='Stimulus ID:',
            )
            stimuli_dropdown.layout.margin = "20px 20px"

            ui = widgets.VBox([stimuli_dropdown,time_slider])
            self.slope_viz_neurons_per_stimuli(stimuli_dropdown.value)
            time_slider.observe(self.update_sline, names = "value")
            stimuli_dropdown.observe(self.slope_viz_neurons_per_stimuli, names = "value")
            display(ui)
        
        elif view == "neuron":
            self.fig, self.ax = plt.subplots()

            time_slider = widgets.IntSlider(value=-1e3 * window_start_sec, min=-1e3 * window_start_sec, max=5e3 * window_start_sec, step=5, description='Time')
            time_slider.layout.width = '6.53in'
            time_slider.layout.margin = '0 -4px'

            neuron_dropdown = widgets.Dropdown(
                options= range(0,prepped_data.shape[0]),
                value=354,
                description='Neuron ID:',
            )
            neuron_dropdown.layout.margin = "20px 20px"


            ui = widgets.VBox([neuron_dropdown, time_slider])
            self.slope_viz_stimuli_per_neuron(neuron_dropdown.value)
            time_slider.observe(self.update_nline, names='value')
            neuron_dropdown.observe(self.slope_viz_stimuli_per_neuron,names='value')
            display(ui)
        