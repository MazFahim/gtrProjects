from common import helper as hp
from common import ta_lib as ta
from database import transactionsBT as db
import pandas as pd
import numpy as np
import datetime

class GoldenDeath():
    def __init__(self,back_id,strategy,symbols,dtFrom,dtTo):
        self.BACKID=back_id
        self.SYMBOLS=symbols
        self.TRAN_DATE_FROM=dtFrom
        self.TRAN_DATE_TO=dtTo
        self.STRATEGY=strategy
        self.TRADESTART=None
        self.TRADEEND=None
        self.CASH=2000
        self.RATEIN=0
        self.RATEOUT=0
        self.ACTIVEORDERS=[]
        self.TRADECLOSED=[]
        self.BUYSELLSIGNAL=None
        self.DF=None
        self.FASTSMA=None
        self.SLOWSMA=None
        self.STOPLOSS_LONG=5
        self.STOPLOSS_SHORT=None
        self.POSITION=0
    def fnc_main(self):
        try:
            self.fnc_get_setting_data()
            for symbol in self.SYMBOLS:
                self.fnc_reset_variables(symbol)
                self.DF=hp.fnc_get_data_from_database(self.STRATEGY,symbol,self.TRAN_DATE_FROM,self.TRAN_DATE_TO)
                if self.DF is None:
                    print("No data found for symbol: ",+symbol)
                    continue
                # stragey specific technical analysis
                self.fnc_ta_analysis()
                self.fnc_trade_on_signal() 
                if len(self.TRADECLOSED)==0:
                    print("No trade for symbol: ",+symbol)
                    continue  
                hp.fnc_save_trade_close(self.TRADECLOSED)
                self.DF=hp.fnc_prepare_backtest_final(self.BACKID,self.STRATEGY,self.DF,self.ACTIVEORDERS)
                hp.fnc_save_backtest_final(self.BACKID,self.DF)
                hp.fnc_update_summary(symbol,self.BACKID)
                #self.DF.to_csv('AAPLBacktestFinal.csv',index=False)
            
            hp.fnc_compared_index(self.BACKID)  
                              
            
        except Exception as e:
            raise(e)       
    def fnc_get_setting_data(self):
        try:
            result=hp.fnc_get_setting_data(self.BACKID)
            for row in result:
                if row['Remarks'].upper()=='SL-PERCENT': 
                    if row['SFlag'].upper()=='LONG':
                        self.STOPLOSS_LONG = int(row['SValue'])
                    else:
                        self.STOPLOSS_SHORT = int(row['SValue'])
                else:
                    if row['RemarksCaption']=='Fast Moving Avg':
                        self.FASTSMA=int(row['SValue'])
                    else:
                        self.SLOWSMA=int(row['SValue'])                 
        except Exception as e:
            raise(e)
    
    def fnc_reset_variables(self,symbol):
        self.DF=None
        self.ACTIVEORDERS=[]
        self.TRADECLOSED=[]
        self.POSITION=0
        hp.fnc_remove_existing(symbol,self.BACKID)
        
    def fnc_ta_analysis(self):
        self.DF=ta.get_sma(self.DF,self.FASTSMA)
        self.DF=ta.get_sma(self.DF,self.SLOWSMA)
        self.DF['signal']=np.where((self.DF[f"sma{self.FASTSMA}"] > self.DF[f"sma{self.SLOWSMA}"]),1,0)
        self.DF['signal']=np.where((self.DF[f"sma{self.FASTSMA}"] < self.DF[f"sma{self.SLOWSMA}"]),-1,self.DF['signal'])
        self.DF['crossover']=self.DF['signal'].diff()
        #self.DF.to_csv('AAPLSignal.csv')
          
    def fnc_trade_on_signal(self):
        for i in range(len(self.DF)):
            if self.POSITION == 0:
                # if self.DF.iloc[i]['crossover'] in [1,2]:
                if self.DF.iloc[i]['signal']==1:
                    self.POSITION=1
                    self.fnc_place_order(i) 
                    
                # elif self.DF.iloc[i]['crossover'] in [-1,-2]:
                elif self.DF.iloc[i]['signal']==-1:
                    self.POSITION=-1
                    self.fnc_place_order(i)
                    
                    
            elif self.POSITION == 1:
                self.CURRENTROW=self.DF.iloc[i].to_dict()
                self.fnc_stoploss_for_long(self.ACTIVEORDERS[-1],hp.fnc_prepare_order(self.CURRENTROW,self.POSITION,self.CASH,self.STOPLOSS_LONG))
                
            elif self.POSITION == -1:
                self.CURRENTROW=self.DF.iloc[i].to_dict()
                self.fnc_stoploss_for_short(self.ACTIVEORDERS[-1],hp.fnc_prepare_order(self.CURRENTROW,self.POSITION,self.CASH,self.STOPLOSS_LONG))
                    
    def fnc_place_order(self,j):
        self.ACTIVEORDERS.append(hp.fnc_prepare_order(self.DF.iloc[j].to_dict(),self.POSITION,self.CASH,self.STOPLOSS_LONG))
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
    
    def fnc_stoploss_for_short(self,active_order,current_order):
        stoploss=active_order['stoploss']
        # check the stoploss is greater then the current day close or match the exit time if yes the sell and set the POSITION=0
        if stoploss <= current_order['close']:
            current_order['trans']='Buy'
            current_order=hp.fnc_close_order(active_order,current_order)
            self.RATEOUT=active_order['stoploss']
            self.TRADEEND=current_order['trandate']
            self.ACTIVEORDERS.append(current_order)
            self.fnc_prepare_close_order_data(current_order)
            self.POSITION=0
        else:
            stoploss_new=current_order['stoploss']
            stoploss=min([stoploss,stoploss_new])
            self.ACTIVEORDERS.append(hp.fnc_update_stoploss_order(active_order,current_order,stoploss))
            
            
    def fnc_prepare_close_order_data(self,current_order):
        qty=current_order['qty']
        close_data=(str(self.TRADESTART),str(current_order['symname']),str(self.STRATEGY),\
                    self.RATEIN,qty,round(qty*self.RATEIN,2),round(self.RATEOUT,3),qty,round(qty*self.RATEOUT,2),\
                    0,str('stoploss'),str(self.TRADEEND),'Long' if self.POSITION == 1 else 'Short',self.BACKID )
        self.TRADECLOSED.append(close_data)
                
   