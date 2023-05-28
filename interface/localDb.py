import pypyodbc as odbc

def fncConnection():
    try:
        DRIVER_NAME = 'SQL SERVER'
        SERVER_NAME = 'DESKTOP-TR2I0VJ\SQLEXPRESS01'
        DATABASE_NAME = 'angkoTrial'
        user = 'sa'
        pswd = '007'

        connection_string = f"""
            DRIVER={{{DRIVER_NAME}}};
            SERVER={SERVER_NAME};
            DATABASE={DATABASE_NAME};
            Trust_Connection=yes;
            uid={user};
            pwd={pswd};
        """
        conn =  odbc.connect(connection_string)
        #print('Connected')
        return conn
    except Exception as e:
        print('Database server connection failed.')
        raise(e)
    
def fncCallStoredPrcedure(dtFrom, dtTo, longLow, longHigh, shortLow, shortHigh):
    #result = fncCheckForDuplicate(dtFrom, dtTo, longLow, longHigh, shortLow, shortHigh)
    try:
        connect = fncConnection()
        if connect is not None:
            cursor = connect.cursor()
            cursor.execute(f"EXEC [dbo].[prcAutomatedBacktest] '{dtFrom}', '{dtTo}', {longLow}, {longHigh}, {shortLow}, {shortHigh}")
            connect.commit()
            # result = cursor.fetchone()
            # print(result[0])
            cursor.close()
            connect.close()
            return 0
    except Exception as e:
        raise(e)
    

def fncCheckForDuplicate(dtFrom, dtTo, longLow, longHigh, shortLow, shortHigh):
    try:
        connect = fncConnection()
        if connect is not None:
            result = 0
            cursor = connect.cursor()
            query = "EXEC [dbo].[prcAutomatedBacktest] ?, ?, ?, ?, ?, ?"
            params = (dtFrom, dtTo, longLow, longHigh, shortLow, shortHigh)
            cursor.execute(query, params)
            result = cursor.fetchall()
            print(result)
            if cursor.description is not None:
                return_value = cursor.fetchone()[0]
                print(return_value)
                cursor.close()
                connect.close()
                return 1
            else:
                print('Irfan Bhai')
                cursor.close()
                connect.close()
                return 2
        else:
            cursor.close()
            connect.close()
            return result
    except Exception as e:
        raise e
