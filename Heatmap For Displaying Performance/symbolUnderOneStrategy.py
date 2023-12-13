import pandas as pd
import plotly.graph_objects as go
import plotly.express as px
import dbTransaction


def fncsymbolUnderAStrategy(strategy, symbol):
    # strategy = 'BB_RSI'
    # symbol = 'tsla'
    df = dbTransaction.fncForASymbolUnderAStrategy(strategy, symbol)

    data = pd.DataFrame(df, columns=['tempId', 'long', 'short', 'MaxReturn', 'SymName'])
    data['MaxReturn'] = data['MaxReturn'].astype(float)  
    
    max_return_lists = []
    long_to_max_return = {}
    for index, row in data.iterrows():
        long_value = row['long']
        max_return_value = row['MaxReturn']
    
        # Check if the long_value is already in the dictionary
        if long_value in long_to_max_return:
            long_to_max_return[long_value].append(max_return_value)
        else:
            long_to_max_return[long_value] = [max_return_value]

    # Append the MaxReturn value lists to the max_return_lists
    for long_value, max_return_values in long_to_max_return.items():
        max_return_lists.append(max_return_values)

    heatmap_data = data.pivot(index='long', columns='short', values='MaxReturn')

    # Create the heatmap using Plotly
    fig = go.Figure(data=go.Heatmap(
        z=heatmap_data.values,
        x=heatmap_data.columns,
        y=heatmap_data.index,
        colorscale='RdYlGn',  # You can choose a different colorscale
        zmin=-2,  # Min value
        zmax=2,   # Max value
        colorbar=dict(title='Max Return'),
        text=max_return_lists,
        texttemplate="%{text}"
    ))
    
    fig.update_xaxes(title_text="Short")
    fig.update_yaxes(title_text="Long")

    fig.update_layout(title=f"{symbol} in {strategy}")
    return fig