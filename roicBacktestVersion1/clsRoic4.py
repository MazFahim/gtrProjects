import pandas as pd
import numpy as np
import helper as hp
import transactionsBT as db
import pandas as pd
import numpy as np
from datetime import datetime, timedelta


class clsRoic():
    def __init__(self, symbols):
        self.BACKID=502
        self.SYMBOLS=symbols
        self.TRAN_DATE_FROM=None
        self.TRAN_DATE_TO=None
        self.STRATEGY="ROIC"
        self.TRADESTART=None
        self.TRADEEND=None
        self.BUYSELLSIGNAL=None
        self.CASH=2000
        self.threshold_value=10.0
        self.DF = None
        self.DF_ROIC_DATA=[]
        self.DF_History_Data=None
        self.ACTIVEORDERS=[]
        self.TRADECLOSED=[]
        self.RATEIN=0
        self.RATEOUT=0
        self.STOPLOSS=5
        self.STOPLOSSACTIVE=0
        self.POSITION=0

    def fnc_main(self):
        try:
            current_year = datetime.now().year
            end_date = datetime(current_year - 1, 12, 31)
            start_date = end_date - timedelta(days=365 * 5)
            self.TRAN_DATE_FROM=start_date
            self.TRAN_DATE_TO=end_date
            # self.fnc_get_setting_data()
            summary_df = pd.DataFrame(columns=['SymName', 'TotalAmountIn', 'TotalAmountOut', 'TotalTrades', 'profit', 'Year', 'ROIC', \
                                               'WACC', 'ATH', 'ATL'])
            start_date = pd.to_datetime(self.TRAN_DATE_FROM)
            end_date = pd.to_datetime(self.TRAN_DATE_TO)
            start_date = pd.to_datetime(start_date)
            end_date = pd.to_datetime(end_date)

            # Create a list to store date ranges
            date_ranges = []

            # Extract year from the start and end date
            start_year = start_date.year
            end_year = end_date.year
        
            for year in range(start_year, end_year + 1):
                year_start = pd.to_datetime(f"{year}-01-01")
                year_end = pd.to_datetime(f"{year}-12-31")
                if year == start_year:
                    year_start = start_date

                # If the year is the end year, adjust the range
                if year == end_year:
                    year_end = end_date

                date_ranges.append((year, year_start, year_end))
            
            average_roic_df = pd.DataFrame(columns=['SymName', 'Average_ROIC'])

            for symbol in self.SYMBOLS:
                symbolROICdata=hp.fnc_get_data_from_database_for_roic(symbol, self.TRAN_DATE_FROM, self.TRAN_DATE_TO)
                symbolROICdata['ROIC'] = symbolROICdata['NOPAT']/symbolROICdata['investedCapital']  
                symbolROICdata['ROIC'] = symbolROICdata['ROIC']*100
                average_roic = symbolROICdata['ROIC'].mean()
                if pd.isna(average_roic): 
                    continue
                average_roic_df = average_roic_df.append({'SymName': symbol, 'Average_ROIC': average_roic}, ignore_index=True)
                # print(average_roic_df)
            print("Final average df")
            average_roic_df = average_roic_df.sort_values(by='Average_ROIC', ascending=False)
            top_10_roic_df = average_roic_df.head(10)
            print(top_10_roic_df)
            self.SYMBOLS=top_10_roic_df['SymName']
            print(self.SYMBOLS)
            for symbol in self.SYMBOLS:
            # # #     # symbol = self.SYMBOL
                for year, start, end in date_ranges:
                    self.TRAN_DATE_FROM=start
                    self.TRAN_DATE_TO=end   
                    self.DF_ROIC_DATA=hp.fnc_get_data_from_database_for_roic(symbol, self.TRAN_DATE_FROM, self.TRAN_DATE_TO)
                    self.DF_ROIC_DATA['ROIC'] = self.DF_ROIC_DATA['NOPAT']/self.DF_ROIC_DATA['investedCapital']  
                    self.DF_ROIC_DATA['ROIC'] = self.DF_ROIC_DATA['ROIC']*100
                    roic=self.DF_ROIC_DATA['ROIC']
                    wacc=self.DF_ROIC_DATA['wacc']
                    print(self.DF_ROIC_DATA)
                    self.fnc_reset_variables(symbol)    
                    self.DF_History_Data=hp.fnc_get_data_from_database(self.STRATEGY,symbol,self.TRAN_DATE_FROM,self.TRAN_DATE_TO)
                    # print(self.DF_History_Data)
                    if self.DF_History_Data is None:
                        print("No data found for symbol: ",+symbol)
                        continue
            #     # # stragey specific technical analysis
                    self.DF = pd.merge(self.DF_ROIC_DATA, self.DF_History_Data, on=['symname', 'calendaryear'])
                    # self.DF['ROIC'] = self.DF['ROIC']*100
                    print("ROIC", roic)
                    # self.DF.at[0, 'wacc'] = round(self.DF.at[0, 'wacc'], 4)
                    if not self.DF.empty:
                        self.DF.at[0, 'wacc'] = round(self.DF.at[0, 'wacc'], 4)
                    else:
                        continue
                    # print(self.DF)
                    self.DF.at[0, 'ROIC'] = round(self.DF.at[0, 'ROIC'], 4)
                    self.fnc_ta_analysis()
                    self.fnc_trade_on_signal() 
                    if len(self.TRADECLOSED)==0:
                        print("No trade for the symbol: ")#,+symbol)
                        continue  
                    TradeClosedDataFrame = pd.DataFrame(self.TRADECLOSED, columns=['TradeStart', 'SymName', 'Strategy', 'RateIn', 'Quantity',\
                        'AmountIn', 'RateOut', 'Quantity', 'AmountOut', 0, 'Stop Loss', 'Trade Out', 'Long/Short', 'BackId'])
            #     # # pd.DataFrame(self.TRADECLOSED).to_csv('Tradeclosed.csv')
            #     # # TradeClosedDataFrame.to_csv('Tradeclosed.csv', index=False)
                    hp.fnc_save_trade_close(self.TRADECLOSED)
                    self.DF=hp.fnc_prepare_backtest_final(self.BACKID,self.STRATEGY,self.DF,self.ACTIVEORDERS)
                    hp.fnc_save_backtest_final(self.BACKID,self.DF)
                    hp.fnc_update_summary(symbol,self.BACKID)
                    # print(self.DF)            
                    hp.fnc_compared_index(self.BACKID)  
                    totalTrades = len(TradeClosedDataFrame)
                    totalAmountIn = TradeClosedDataFrame['AmountIn'].sum()
                    totalAmountOut = TradeClosedDataFrame['AmountOut'].sum()
                    profit = totalAmountOut - totalAmountIn
                    priceHighLow = hp.fnc_get_ATH_ATL_from_database(symbol, end)
                    summary_df = pd.concat([summary_df, pd.DataFrame({'SymName': symbol,'TotalAmountIn': [totalAmountIn], 'TotalAmountOut': [totalAmountOut],
                     'TotalTrades': totalTrades, 'profit': profit, 'Year': year, 'ROIC': roic, 'WACC': wacc, 'ATH':priceHighLow['High'],\
                        'ATL':priceHighLow['Low']})], ignore_index=True)
                    print(summary_df)
                    
                # break
            print("Final Summary")
            
            summary_df.to_csv('top10smbl.csv', index=False)
        except Exception as e:
            raise(e)    
    
    def fnc_reset_variables(self,symbol):
        self.DF=None
        self.ACTIVEORDERS=[] 
        self.TRADECLOSED=[]
        self.POSITION=0
        self.STOPLOSS=5
        hp.fnc_remove_existing(symbol,self.BACKID)   
        
    def fnc_ta_analysis(self):
        self.DF['signal'] = np.where((self.DF['ROIC'] > self.threshold_value) & (self.DF['ROIC'] > 2 * self.DF['wacc']), 1, 0)
        #self.DF['signal'] = np.where((self.DF['ROIC'] < self.threshold_value) & (self.DF['ROIC'] < self.DF['wacc']), -1, 0)
        
    
    def fnc_trade_on_signal(self):
        for i in range(len(self.DF)):
            if self.POSITION == 0:
                if self.DF.iloc[i]['signal']==1:
                    self.POSITION=1
                    self.fnc_place_order(i)
                                        
            elif self.POSITION == 1:
                self.CURRENTROW=self.DF.iloc[i].to_dict()
                self.fnc_stoploss_for_long(self.ACTIVEORDERS[-1], hp.fnc_prepare_order(self.CURRENTROW,self.POSITION,self.CASH,self.STOPLOSS))
            
    def fnc_place_order(self,j):
        self.ACTIVEORDERS.append(hp.fnc_prepare_order(self.DF.iloc[j].to_dict(),self.POSITION,self.CASH,self.STOPLOSS))
        self.RATEIN= self.ACTIVEORDERS[-1]['close']
        self.TRADESTART=self.ACTIVEORDERS[-1]['trandate']    

        
    def fnc_stoploss_for_long(self,active_order,current_order):
        stoploss=active_order['stoploss']
        # check the stoploss is greater then the current day close or match the exit time if yes the sell and set the POSITION=0
        if stoploss >= current_order['close']:
            current_order['trans']='Sell'
            # self.fncCloseOrder(active_order,current_order)
            current_order=hp.fnc_close_order(active_order,current_order)
            self.RATEOUT=active_order['stoploss']
            self.TRADEEND=current_order['trandate']
            self.ACTIVEORDERS.append(current_order)
            self.fnc_prepare_close_order_data(current_order)
            self.POSITION=0
        
        # Activate the stoploss when FG index is greater then or equal to 50
        else:
            stoploss_new=current_order['stoploss']
            stoploss=max([stoploss,stoploss_new])
            self.ACTIVEORDERS.append(hp.fnc_update_stoploss_order(active_order,current_order,stoploss))
      
    def fnc_prepare_close_order_data(self,current_order):
        qty=current_order['qty']
        close_data=(str(self.TRADESTART),str(current_order['symname']),str(self.STRATEGY),\
                    self.RATEIN,qty,round(qty*self.RATEIN,2),round(self.RATEOUT,3),qty,round(qty*self.RATEOUT,2),\
                    0,str('stoploss'),str(self.TRADEEND),'Long' if self.POSITION == 1 else 'Short',self.BACKID )
        if close_data not in self.TRADECLOSED:
            self.TRADECLOSED.append(close_data) 
        # self.TRADECLOSED.append(close_data)


nasdaqSymbols = hp.fnc_get_nasdaq_symbols_from_database()
symnames_list = nasdaqSymbols['SymName'].tolist()

roic = clsRoic(symnames_list)
roic.fnc_main()