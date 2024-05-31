"""Interactive components within notebooks"""
from . import meshes
from . import utils
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
        self.window_start_sec = None
        self.window_end_sec = None
        self.event_duration_sec = None

    def is_1d(self, arr):
        return arr.ndim == 1

    def avg_and_bin(self,  spike_times_raw_data, spike_clusters_data, event_start, event_ids,  window_start_sec, event_duration_sec, window_end_sec, bin_size_sec=0.02, sample = True):
        """Bins data and calculates averages across stimuli, outputting a 3d array of data in format [neuron_cluster_id, stimulus_id, time]
        
        Parameters
        ----------
        spike_times_raw_data: 1D array
            raw data of spiking times in samples
        spike_clusters_data: 1D array
            raw data of spiking clusters in a 1d array
        event_start: 1d array
            time in which the events analyzed start within the experiment
        event_ids: 1d array
            ids of the different event types
        window_start_sec: float
            length of time within data you want to include before the event start, in seconds
        event_duration_sec: float
            length of time of the event, primarily used for visual graphing parts later, in seconds
        window_end_sec: float
            length of time within data you want to include after the event ends, in seconds
        bin_size_sec: float
            size of the bin, in seconds. defaults to 20 ms / 0.02 sec
        sample: boolean
            whether the data for spike times is saved in seconds (true = samples, false = seconds)

            
        Examples
        -------- 
        >>> neuron_graph.avg_and_bin(st_samp, sc, event_start, event_ids, window_start_sec=0.1, window_end_sec=0.5, sample= True)
        """
        # checking inputs:
        try:
            import numpy as np
        except ImportError:
            raise ImportError("Numpy package is not installed. Please pip install numpy to use this function.")
        if not self.is_1d(spike_times_raw_data):
            raise TypeError("Please ensure that inputs are all 1d arrays, be sure to call np.squeeze if needed.")
        
        if not self.is_1d(spike_clusters_data):
            raise TypeError("Please ensure that inputs are all 1d arrays, be sure to call np.squeeze if needed.")
        
        if not self.is_1d(event_start):
            raise TypeError("Please ensure that inputs are all 1d arrays, be sure to call np.squeeze if needed.")
        
        if not self.is_1d(event_ids):
            raise TypeError("Please ensure that inputs are all 1d arrays, be sure to call np.squeeze if needed.")
        
        self.window_start_sec = window_start_sec
        self.event_duration_sec = event_duration_sec
        self.window_end_sec = event_duration_sec + window_end_sec # since the rest of the code relies on just what the total is, event_duration is j used for graphing purposes
        window_end_sec = self.window_end_sec
        
        if sample:
            spike_times_sec = spike_times_raw_data / 3e4 # convert from 30khz samples to seconds
        else:
            spike_times_sec = spike_times_raw_data


        # set up bin edges - 20 ms here
        bins_seconds = np.arange(np.min(spike_times_sec), np.max(spike_times_sec), bin_size_sec)
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
        bin_size = bin_size_sec * 1000
        bintime_prev = int(window_start_sec * (1000/bin_size))
        bintime_post = int(window_end_sec * (1000/bin_size) + 1)
        windowsize = bintime_prev + bintime_post
        

        # To bin: divide by 20, floor
        stim_binned = np.floor(event_start * 1000 / bin_size).astype(int)
        self.stim_binned = stim_binned


        u_stim_ids = np.unique(event_ids)

        # Initialize final_avg matrix
        final_avg = np.empty((binned_spikes.shape[0], len(u_stim_ids), windowsize))

        for neuron_id in range(binned_spikes.shape[0]):

            for stim_id in u_stim_ids:
                stim_indices = np.where(event_ids == stim_id)[0]

                neuron_stim_data = np.empty((len(stim_indices), windowsize))
                
                for i, stim_idx in enumerate(stim_indices):
                    bin_id = int(stim_binned[stim_idx])
                    selected_columns = binned_spikes[neuron_id, bin_id - bintime_prev: bin_id + bintime_post]

                    # # Check if selected_columns is empty
                    # if selected_columns.size == 0:
                    #     print(f"Empty selected_columns for neuron {neuron_id}, stim {stim_id}, stim_idx {stim_idx}")
                    #     selected_columns = np.zeros(windowsize)
                    
                    # # Check if selected_columns can be reshaped to match neuron_stim_data's row shape
                    # if selected_columns.shape[0] != windowsize:
                    #     print(f"selected_columns shape mismatch for neuron {neuron_id}, stim {stim_id}, stim_idx {stim_idx}")
                    #     selected_columns = np.zeros(windowsize)


                    neuron_stim_data[i,:] = selected_columns

                bin_average = np.mean(neuron_stim_data, axis=0)/bin_size_sec
                final_avg[neuron_id, int(stim_id) - 1, :] = bin_average
        self.avg_data = final_avg

    def slope_viz_stimuli_per_neuron(self, change, step = 20):
        """Visualizes and creates interactive plot for the average of each stimulus per neuron
        
        Parameters
        ----------
        step: int
            the increment along the x axis, in ms. defaults to 20
            
        Examples
        --------
        >>> urchin.ui.slope_viz_stimuli_per_neuron()
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

        if isinstance(change, int):
            neuron_id = change
        else:
            neuron_id = change.new

        # Plotting data:
        self.ax.clear()
        for i in range(0,prepped_data.shape[1]):
            y = prepped_data[neuron_id][i]
            x = np.arange(-1e3 * self.window_start_sec, 1e3 * self.window_end_sec + step, step=step)
            self.ax.plot(x,y, color='dimgray')

        # Labels:
        self.ax.set_xlabel('Time from stimulus onset (ms)')
        self.ax.set_ylabel('Number of Spikes Per Second')
        self.ax.set_title(f'Neuron cluster {neuron_id} Spiking Activity with Respect to Each Stimulus')

        #Accessories:
        self.ax.axvspan(0, self.event_duration_sec * 1000, color='gray', alpha=0.3)
        self.vline = self.ax.axvline(-1e3 * self.window_start_sec, color='red', linestyle='--',)
        # Set y-axis limits
        # Calculate y-axis limits
        
        max_y = max([max(prepped_data[neuron_id][i]) for i in range(prepped_data.shape[1])])  # Maximum y-value across all lines
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


    def slope_viz_neurons_per_stimuli(self, change, step = 20):
        """Visualizes and creates interactive plot for the average of every neuron per stimulus
        
        Parameters
        ----------
        step: int
            the increment along the x axis, in ms. defaults to 20

        if you want to set custom colors for the graph without generating neurons in a brainview, 
        you can set self.neuron_colors=[list of colors of length of # neurons] outside of this function,
        and self.neuron_colors = None if trying to go back to random.
        
        Examples
        --------
        >>> urchin.ui.slope_viz_stimuli_per_neuron()
        """
        try:
            import numpy as np
        except ImportError:
            raise ImportError("Numpy package is not installed. Please pip install numpy to use this function.")
        try:
            import matplotlib.pyplot as plt
        except ImportError:
            raise ImportError("Matplotlib package is not installed. Please pip install matplotlib to use this function.")
        try:
            import random
        except ImportError:
            raise ImportError("random package is not installed. Please pip install random to use this function.")
    
        
        prepped_data = self.avg_data

        # if self.neuron_colors is not None:
        #     n_color = self.neuron_colors
        # else:
        #     n_color = [[random.random() for _ in range(3)] for _ in range(prepped_data.shape[0])]


        if isinstance(change, int):
            self.stim_id = change
        else:
            self.stim_id = change.new

        stim_id = self.stim_id

        # Plotting data:
        self.ax.clear()
        for i in range(0,prepped_data.shape[0]):
            y = prepped_data[i][stim_id]
            x = np.arange(-1e3 * self.window_start_sec, 1e3 * self.window_end_sec + step, step=step)
            self.ax.plot(x,y, color = self.neuron_colors[i])
        
        # Labels:
        self.ax.set_xlabel(f'Time from Stimulus {stim_id} display (20 ms bins)')
        self.ax.set_ylabel('Number of Spikes Per Second')
        self.ax.set_title(f'Neuron Cluster Spiking Activity with Respect to Stimulus ID {stim_id}')

         #Accessories:
        self.ax.axvspan(0, self.event_duration_sec * 1000, color='gray', alpha=0.3)
        self.vline = self.ax.axvline(-1e3 * self.window_start_sec, color='red', linestyle='--',)




    def update_nline(self, position):
        position = position.new
        self.vline.set_xdata([position, position])  # Update x value of the vertical line
        self.fig.canvas.draw_idle()

    def update_sline(self,t):
        t = t.new
        self.vline.set_xdata([t, t])  # Update x value of the vertical line
        self.fig.canvas.draw_idle()
        if self.neurons is not None:
            self.update_neuron_sizing(t)

    def plot_neuron_view_interactive_graph(self):
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
        >>> graph_object.plot_neuron_view_interactive_graph()
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
    
        self.fig, self.ax = plt.subplots()

        time_slider = widgets.IntSlider(value=-1e3 * self.window_start_sec, min=-1e3 * self.window_start_sec, max=1e3 * self.window_end_sec, step=5, description='Time')
        time_slider.layout.width = '6.53in'
        time_slider.layout.margin = '0 -4px'

        neuron_dropdown = widgets.Dropdown(
            options= range(0,prepped_data.shape[0]),
            value=0,
            description='Neuron ID:',
        )
        neuron_dropdown.layout.margin = "20px 20px"


        ui = widgets.VBox([neuron_dropdown, time_slider])
        self.slope_viz_stimuli_per_neuron(neuron_dropdown.value)
        time_slider.observe(self.update_nline, names='value')
        neuron_dropdown.observe(self.slope_viz_stimuli_per_neuron,names='value')
        display(ui)


    def plot_stim_view_interactive_graph(self, locations = None):
        """Plots appropriate interactive graph based on view
        
        Parameters
        ----------
        locations: pandas dataframe of len how many neurons
            dataframe with the following columns: ['left_right_ccf_coordinate', 'anterior_posterior_ccf_coordinate',
       'dorsal_ventral_ccf_coordinate', 'color'] for if you are trying to run the function with the corresponding interactive urchin component

        if you want to set custom colors for the graph without generating neurons in a brainview, 
        you can set self.neuron_colors=[list of colors of length of # neurons] outside of this function,
        and self.neuron_colors = None if trying to go back to random.
        
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
        try:
            import random
        except ImportError:
            raise ImportError("random package is not installed. Please pip install random to use this function.")
        
        from IPython.display import display
            
        
        prepped_data = self.avg_data
        
        if locations is not None:
            self.neurons = meshes.create(len(locations))
            positions_list = []

            for i, row in locations.iterrows():
                position = [round(row.left_right_ccf_coordinate), round(row.anterior_posterior_ccf_coordinate), round(row.dorsal_ventral_ccf_coordinate)]
                positions_list.append(position)
            
            meshes.set_positions(self.neurons, positions_list)
            meshes.set_scales(self.neurons, [[0.05,0.05,0.05]]* len(self.neurons))

            if 'color' in locations.columns:
                self.neuron_colors = list(locations["color"])
                self.neuron_colors = [utils.hex_to_rgb(x) for x in self.neuron_colors]
                meshes.set_colors(self.neurons, self.neuron_colors)
        if self.neuron_colors is None:
            self.neuron_colors = [[random.random() for _ in range(3)] for _ in range(prepped_data.shape[0])]
        
        self.fig, self.ax = plt.subplots()

        time_slider = widgets.IntSlider(value=-1e3 * self.window_start_sec, min=-1e3 * self.window_start_sec, max=1e3 * self.window_end_sec, step=5, description='Time')
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

        