import config
import pymssql

def fncConnectDb():
    try:
        dbName = 'dbTrade'
        connect = pymssql.connect(
        config.serverNameBT, config.userNameBT, config.passwordBT, dbName)
        print('Database server connection successful.')
        return connect
    except Exception as e:
        # print(e)
        print('Database server connection failed.')
        raise(e)
    
def fncForASymbolUnderAStrategy(strategy, symbol):
    try:
        connect = fncConnectDb()
        if connect is not None:
            cursor = connect.cursor()
            procName = 'oneSymbolUnderOneStrategy'
            params = (strategy, symbol)
            cursor.callproc(procName, params)
            #cursor.execute("EXEC oneSymbolUnderOneStrategy strategy=%s symName=%s", )
            rows = cursor.fetchall()
            #print(rows)
            return rows
    except Exception as e:
        raise(e)

def fncStrategyPerformance(strategy):
    try:
        connect = fncConnectDb()
        if connect is not None:
            cursor = connect.cursor()
            cursor.execute(f"EXEC strategyPerformance @strategy={strategy}")
            rows = cursor.fetchall()
            #print(rows)
            return rows
    except Exception as e:
        raise(e)
        
#fncStrategyPerformance('BB_RSI')