import pandas as pd
import plotly.graph_objects as go
import dbTransaction
import numpy as np

def strategyPerformanceForSymbols(strategy):
    df = dbTransaction.fncStrategyPerformance(strategy)
    
    data = pd.DataFrame(df, columns=['SymName', 'long', 'short', 'MaxReturn'])
    data['MaxReturn'] = data['MaxReturn'].astype(float)  
    min_long = data['long'].min()
    max_long = data['long'].max()
    min_short = data['short'].min()
    max_short = data['short'].max()

    long_range = list(range(int(min_long), int(max_long) + 1))
    short_range = list(range(int(min_short), int(max_short) + 1))

    z = np.empty((len(long_range), len(short_range)), dtype=object)
    z.fill('')

    for index, row in data.iterrows():
        long_value = row["long"]
        short_value = row["short"]
        max_return_value = row["MaxReturn"]

        long_index = long_range.index(int(long_value))
        short_index = short_range.index(int(short_value))
        z[long_index, short_index] = max_return_value
        



    heatmap_data = data.pivot(index='long', columns='short', values='MaxReturn')
    fig = go.Figure(data=go.Heatmap(
            z=z,
            x=short_range,  # Set x to short_range to ensure the correct tick values
            y=long_range,   # Set y to long_range to ensure the correct tick values
            colorscale='RdYlGn',
            zmin=-2,
            zmax=2,
            # text=z,
            # texttemplate="%{text}"
        ))
    
    fig.update_xaxes(title_text="Short")
    fig.update_yaxes(title_text="Long")
    for i in range(len(long_range)):
        for j in range(len(short_range)):
            if z[i, j] != '':
                sym_name = data[(data['long'] == long_range[i]) & (data['short'] == short_range[j])]['SymName'].values[0]
                fig.add_annotation(
                    x=short_range[j], y=long_range[i],
                    text=f"{sym_name}<br> {z[i, j]}",
                    showarrow=False,
                    font=dict(size=10, color='black'),
                    xanchor="center",
                    yanchor="middle"
                )

    fig.update_layout(title=f"Performance of the symbols in {strategy}")
            
    # Show the plot
    # fig.show()
    return fig
   



strategyPerformanceForSymbols('BB_RSI')