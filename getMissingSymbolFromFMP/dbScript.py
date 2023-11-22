import config
import pymssql

def fncConnectDb():
    try:
        dbName = 'dbTrade'
        connect = pymssql.connect(
        config.serverNameBT, config.userNameBT, config.passwordBT, dbName)
        #print('Database server connection successful.')
        return connect
    except Exception as e:
        
        print('Database server connection failed.')
        raise(e)
    

def fncAllSymbols():
    try:
        connect = fncConnectDb()
        if connect is not None:
            cursor = connect.cursor()
            cursor.execute("SELECT SymName FROM tblSymbol")
            rows = cursor.fetchall()
            return rows
    except Exception as e:
        raise(e)
    
