import transactionsBT as db
import pandas as pd
import datetime

def fnc_get_main_data(backid):
    try:
        sql_query= f"Select Strategy,dtfrom,dtTo,Symbols from tblBacktest_Main where Backid={backid}"
        return(db.fncGetDataAsDictionary(sql_query))
    except Exception as e:
        raise(e)

def fnc_get_short_symbols(backid):
    try:
        sql_query= f"Select [SymbolsShort] from tblBacktest_Main where Backid={backid}"
        result=db.fncGetDataAsDictionary(sql_query)
        return(result[0]['SymbolsShort'].split(','))
    except Exception as e:
        raise(e)

def fnc_get_setting_data(backid):
    try:
        sql_query= f"Select * from tblBacktest_Setting where Backid={backid}"
        return(db.fncGetDataAsDictionary(sql_query))
    except Exception as e:
        raise(e)

def fnc_remove_existing(symbol,backid):
        try:
            sql_query = f"Exec prcRemoveExistingBacktestData {backid}, '{symbol}'"
            db.fncUpdateData(sql_query)
            print(f"Delete backtest existing data Done, backId: {backid}, Symbol: {symbol}")
        except Exception as e:
            print(e)

def fnc_get_investment_type(strategy):
    try:
        sql_query= f"Select InvestType from tblStrategies where StratName='{strategy}'"
        return(db.fncGetDataAsDictionary(sql_query))
    except Exception as e:
        raise(e)
        
def fnc_get_data_from_database(strategy,symname,dt_from,dt_to):
    # print(f'Executing function - fncGetDataFromDatabase')
        try:
            invest_type=fnc_get_investment_type(strategy)
            #  write a code to check the invest_type has data or not.If not then return a remark    
            if len(invest_type)==0 :
                return "Investment type is not defined"
            
            if invest_type[0]['InvestType']=='Intraday':
                sql_query = f"SELECT DISTINCT dtdate as [date],symname,[open],[high],[low],[close],[volume]\
                            FROM [P3Data].[dbo].[tblHistoryData_1Min] WHERE SymName='{symname}'\
                            AND CONVERT(SMALLDATETIME,CONVERT(VARCHAR,dtDate,106)) BETWEEN'{dt_from}'\
                            AND '{dt_to}'\
                            AND CONVERT(SMALLDATETIME,CONVERT(VARCHAR,dtDate,108)) BETWEEN '09:00' and '16:00'\
                            ORDER BY dtDate"
                df = db.fncGetDataAsPanda(sql_query, "dbTrade")
            else:
                sql_query = f"SELECT DISTINCT dtDate as [date],symname,[open],[high],[low],[close],[volume], YEAR(dtDate) AS calendaryear\
                              FROM [P3Data].[dbo].[tblHistoryData] WHERE SymName='{symname}' AND \
                              dtDate BETWEEN '{dt_from}' AND '{dt_to}'"
                df = db.fncGetDataAsPanda(sql_query, "dbTrade")
                
            return df
        except Exception as e:
            print(e)

def fnc_get_data_from_database_for_fg(symbol,dt_from,dt_to):
        # print(f'Executing function - fncGetDataFromDatabase')
        try:
            sql_query = f"EXEC [P3Data].[dbo].prcGetHistoryDataWithFGIndex '{symbol}','{dt_from}','{dt_to}'"
            return db.fncGetDataAsPanda(sql_query, "dbTrade")
        except Exception as e:
            print(e) 
            
def fnc_get_data_from_database_for_vix(symbol,dt_from,dt_to):
    try:
        sql_query = f"EXEC [P3Data].[dbo].prcGetHistoryDataWithVix '{symbol}','{dt_from}','{dt_to}'"
        return db.fncGetDataAsPanda(sql_query, "dbTrade")
    except Exception as e:
        print(e)


def fnc_get_data_from_database_for_roic(symbol, dt_from,dt_to):
    try:
        sql_query = f"EXEC [P3Data].[dbo].prcGetROICandWaccData '{symbol}', '{dt_from}', '{dt_to}'"
        return db.fncGetDataAsPanda(sql_query, "dbTrade")
    except Exception as e:
        print(e)

def fnc_prepare_order(row, position, cash,stoploss=0):
    """
    Prepare order details based on the given row, position, and cash.

    Args:
        row (dict): A dictionary containing the row data.
        position (int): The position value.
        cash (float): The cash value.

    Returns:
        dict: A dictionary containing the prepared order details.
    """
    # Define the mapping dictionaries
    if stoploss==0:
        stoploss_map = {1: 0, -1: 0}  
    else:
        stoploss_map = {1: row['close'] - (row['close'] * (stoploss/100.0)), -1: row['close'] + (row['close'] * (stoploss/100.0))}
    
    trans_map = {1: 'Buy', -1: 'Sell'}
    tranmode_map = {1: 'Long', -1: 'Short'}

    # Prepare the order details
    item_technical = {
        'trandate': datetime.datetime.strftime(row['date'], '%d-%b-%Y %H:%M'),
        'symname': row['symname'],
        'open': row['open'],
        'high': row['high'],
        'low': row['low'],
        'close': row['close'],
        'stoploss': stoploss_map[position],
        'qty': int(cash // row['close']),
        'closelast': 0,
        'trans': trans_map[position],
        'price': row['close'],
        'tranmode': tranmode_map[position]
    }

    return item_technical
   
   
        
def fnc_close_order(active_order, current_order):
    """
    Close the order by updating the current_order dictionary with the necessary values.
    
    Args:
    active_order (dict): The active order dictionary.
    current_order (dict): The current order dictionary.
    
    Returns:
    dict: The updated current_order dictionary.
    """
    try:
        current_order['closelast'] = active_order['close']
        current_order['qty'] = active_order['qty']
        current_order['price'] = active_order['stoploss']
        current_order['stoploss'] = 0
        return current_order
    except Exception as e:
        raise(e)


def fnc_update_stoploss_order(active_order, current_order, stoploss):
    """
    Update the stoploss order by updating the current_order dictionary with the necessary values.
    
    Args:
    active_order (dict): The active order dictionary.
    current_order (dict): The current order dictionary.
    stoploss (float): The new stoploss value.
    
    Returns:
    dict: The updated current_order dictionary.
    """
    try:
        current_order['closelast'] = active_order['close']
        current_order['qty'] = active_order['qty']
        current_order['price'] = 0
        current_order['stoploss'] = stoploss
        current_order['trans'] = 'stoploss'
        return current_order
    except Exception as e:
        raise(e)

def fnc_save_trade_close(trade_close):
        """ print(f'Executing function - fncSaveBacktestFinal')
        '369','TSLA','19-Jul-2021 00:00','FG','646.22','3','1938.66','1184.4181999999998','3553.2545999999993','02-Nov-2021 00:00'"""
        try:
            # print("Save trade close")
            # print(type(trade_close))
            # Data prepare for insert
            sql_heads = "INSERT INTO tblTran_Close (dtTran, symName,Strategy,RateIn,QtyIn,\
                AmountIn,RateOut,QtyOut,AmountOut,StopLoss,Remarks,dtTranOut,TranMode,BackId) \
                VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s)"
            print("Trade closed: ",len(trade_close))             
            # Insert data to database
            db.fncSaveDataMany(sql_heads, trade_close, dbName='dbTrade') 
            print("Insertion done. Trade close") 
        except Exception as e:
            raise(e)
        
def fnc_remove_temp_table_data(backid):
        sql_head=f"DELETE From TempBacktest_Data WHERE backid='{backid}'"
        db.fncUpdateData(sql_head)
        
def fnc_save_backtest_final(backid,backtest_data):
        # print(f'Executing function - fncSaveBacktestFinal')
        try:
            # Extract column names from the DataFrame (assuming backtest_data is your DataFrame)
            columns,placeholders= fnc_get_column_names(list(backtest_data.columns))
            # Create the INSERT INTO statement dynamically with conditional square brackets using list comprehension
            table_name = "TempBacktest_Data"
            sql_heads = f"INSERT INTO {table_name} ({columns}) VALUES ({placeholders})"
            
            print("Backtest len: ",len(backtest_data)) 
            #Insert into temporary table
            db.fncSaveDataMany(sql_heads, list(zip(*map(backtest_data.get, backtest_data))), dbName='dbTrade') 

            #shift data from temporary table to tblBacktest table.
            sql_head=f"EXEC prcInsertToBacktestData {backid} "
            db.fncUpdateData(sql_head)  
            print("Backtest data insertion done.")
        except Exception as e:
            raise(e)
        
def fnc_get_column_names(columns):
    # Extract column names from the DataFrame (assuming backtest_data is your DataFrame)
    column_dict={ 'date': 'dtDate', 'tranmode': 'transMode'}
    updated_list = [column_dict.get(item,item) for item in columns]
    # Create the INSERT INTO statement dynamically with conditional square brackets using list comprehension
    formatted_columns = [f"[{col}]" for col in updated_list]
    # print("Fomated column: ",formatted_columns)
    # sql_heads = "INSERT INTO TempBacktest_Data (backId, symName, dtDate, [open], [high], [low], [close], stoploss, closeLast, price, trans, transMode, strategy) 
    columns = ', '.join(formatted_columns)
    placeholders = ', '.join(['%s'] * len(updated_list))
    return columns,placeholders

def fnc_prepare_backtest_final(backid,strategy,df,active_order):
        active_orders = pd.DataFrame(active_order)
        df=df.merge(active_orders,how='left',on=['symname','open','high','low','close'])
        # df.to_csv('backtest.csv',index=False)
        df['strategy']=strategy
        df.insert(0,'backId',backid)
        if strategy=='BB_RSI':
            column_to_keep=['backId','symname', 'date','open','high','low','close','stoploss','closelast','price','trans','tranmode','strategy','mStateLong','mStateShort']
        else:
            column_to_keep=['backId','symname', 'date','open','high','low','close','stoploss','closelast','price','trans','tranmode','strategy']
        df=df.loc[:,column_to_keep]
        df.fillna(0, inplace=True)
        return df

def fnc_compared_index(backid):    
    try:
        print("Inserting SPY QQQ for graph.")
        symbols = '|'+'|,|'.join(["SPY","QQQ"])+'|'
        sql_query = f"Exec prcInsertIndexDataForBacktestComparison {backid},'{symbols}'"
        db.fncUpdateData(sql_query)
    except Exception as e:
        raise(e)

def fnc_update_summary(symname,backid):
        try:
            sql_query = f"Exec prcGetBacktestAnatomy '{backid}', '{symname}'"
            db.fncUpdateData(sql_query)
            print(f"Updated summary, backId: {backid}, Symbol: {symname}")

        except Exception as e:
            print(e)
            
def fnc_extract_time(str_date_time, date_format="%Y-%m-%d %H:%M:%S"):
    """
    Extracts the time from a given date and time string.

    Args:
        str_date_time (str): The date and time string.
        date_format (str): The format of the date and time string. Default is "%Y-%m-%d %H:%M:%S".

    Returns:
        str: The extracted time string in the format "%H:%M:%S".

    Raises:
        TypeError: If str_date_time is not a string.
    """
    if not isinstance(str_date_time, str):
        raise TypeError("str_date_time must be a string")
    try:
    
        datetime_obj = datetime.datetime.strptime(str_date_time, date_format)
        time_string = datetime_obj.strftime("%H:%M:%S")
        
        return time_string
    except Exception as e:
        raise(e)
            
def fnc_convert_into_period(df, num, freq):
    """
    Convert the given DataFrame into a specified time period.
    
    Args:
        df (DataFrame): The DataFrame to be converted.
        num (int): The number of time periods.
        freq (str): The frequency of the time periods.
        
    Returns:
        DataFrame: The converted DataFrame.
        
    min=minute
    H=Hourly
    W=weekly
    SM= Semi monthly(15 days)
    SMS= Semi monthly start date
    M=Monthly end date
    MS= Monthly start date
    A=year end date
    AS= Year start date

    """
    
    period = str(num) + freq
    
    if freq in ['min', 'H']:
        df.reset_index(inplace=True)
        df["Day"] = df.apply(lambda x: x.date.date(), axis=1)
        df.set_index('date', inplace=True)
        grp = df.groupby('Day')
        
        resample = pd.concat([res.groupby(pd.Grouper(freq=period,label='right', closed='right',origin='start')).agg({"symname":"first",
                                                                                        "open": "first", 
                                                                                         "high": "max",
                                                                                         "low": "min", 
                                                                                         "close": "last", 
                                                                                         "volume": "sum"}) 
                             for k, res in grp])
        
        
    else:
        df["Day"] = df.apply(lambda x: x.date.date(), axis=1)
        df.set_index('date', inplace=True)
        resample=df.resample('W-MON', origin='start').agg({"symname":"first",
                                                        "open": "first", 
                                                        "high": "max",
                                                        "low": "min", 
                                                        "close": "last", 
                                                        "volume": "sum"})
        resample = resample.dropna(how='all')
    # Resetting the index if needed
    resample.reset_index(inplace=True)
    
    return resample

def fnc_weekly_timeframe_convert(df,windowsize):
    # Define the window size and step size
    window_size = windowsize
    results = []
    # Iterate through the data in 7-day windows with a step of 7 days
    # if i want only week value then, len(df) will be replaced by len(df) - window_size+1
    for i in range(0, len(df) , windowsize):
        window_data = df.iloc[i:i + window_size]
        #print(window_data)

        # Calculate the required statistics for the current window
        window_open = window_data.iloc[0]['open']
        window_high_max = window_data['high'].max()
        window_low_min = window_data['low'].min()
        window_close = window_data.iloc[-1]['close']
        window_volume_sum = window_data['volume'].sum()

        # Store the results in a dictionary
        result = {
            'date': window_data.iloc[-1]['date'],
            'open': window_open,
            'high': window_high_max,
            'low': window_low_min,
            'close': window_close,
            'volume': window_volume_sum
        }

        results.append(result)
        
    # Convert the results to a DataFrame for further analysis or export
    return pd.DataFrame(results)