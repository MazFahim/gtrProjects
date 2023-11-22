from datetime import datetime, timedelta
import dbScript
import requests
import config
import pandas as pd
import csv

symbols = dbScript.fncAllSymbols()

API_KEY = config.API_KEY

newSymbolList = []
for i in range(0,len(symbols)):
    newSymbolList.append(symbols[i][0])


dailyDataCSVpath = 'dailyData2.csv'
with open(dailyDataCSVpath, 'w', newline='') as csvfile:
    fieldnames = ['Date', 'Symbol']
    writer = csv.DictWriter(csvfile, fieldnames=fieldnames)
    if csvfile.tell() == 0:
        writer.writeheader()

startDate = datetime(2023, 8, 30)
endDate = datetime(2023, 9, 1)
# endDate = datetime.today() - timedelta(days=1)
step = timedelta(days=1)
current_date = startDate

while current_date <= endDate:
    date_str = current_date.date().strftime('%Y-%m-%d')
    for symbol in newSymbolList:
        #for daily data
        url = f'https://financialmodelingprep.com/api/v3/historical-price-full/{symbol}?from={date_str}&to={date_str}&apikey={API_KEY}'
        response = requests.get(url)
        if response.status_code == 200:
            try:
                # Parsing
                json_data = response.json()
                if json_data:
                    print(f"Data received for {symbol} on {date_str}.")
                else:
                    print(f"Empty Data for {symbol} on {date_str}")
                    with open(dailyDataCSVpath, 'a', newline='') as csvfile:
                        fieldnames = ['Date', 'Symbol']
                        writer = csv.DictWriter(csvfile, fieldnames=fieldnames)
                        writer.writerow({'Date': date_str, 'Symbol': symbol})
            except ValueError:
                print("Invalid JSON format in the response.")
        else:
            print(f"Request failed with status code: {response.status_code}")
        
    current_date += step

#print(dailyEmptyData)