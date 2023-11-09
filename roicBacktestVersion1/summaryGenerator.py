import pandas as pd

# 1. Read the CSV file into a DataFrame
file_path = 'performanceSummary.csv'
df = pd.read_csv(file_path)

# 2. Calculate the return percentage and add it as a new column
df['return_percent'] = ((df['TotalAmountOut'] - df['TotalAmountIn']) / df['TotalAmountIn']) * 100

# 3. Sort the DataFrame based on the return percentage
sorted_df = df.sort_values(by='return_percent', ascending=False)
sorted_df.to_csv('returnPercentage.csv', index=False)
print(sorted_df)
