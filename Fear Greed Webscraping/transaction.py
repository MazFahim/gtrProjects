import config
import pymssql
import pandas as pd


def ConnectDB():
    try:
        connect = pymssql.connect(
            config.serverNameBT, config.userNameBT, config.passwordBT, config.dbName)
        # print('Database server connection successful.')
        return connect
    except Exception as e:
        # print(e)
        print('Database server connection failed.')
        raise(e)


def fncInsertMeterIntoTable(dtDate, Index):
    try:
        connect = ConnectDB()
        if connect is not None:
            cursor = connect.cursor()
            query = "SELECT COUNT(*) FROM tblFear_Greed WHERE dtDate = %s"
            # Execute the query with the value to check
            cursor.execute(query, (dtDate))
            # Fetch the result (it will be a single integer)
            result = cursor.fetchone()[0]
            if result == 0:
                query = "INSERT INTO tblFear_Greed (dtDate,FGIndex) VALUES (%s, %s)"
                values = (dtDate, Index)
                cursor.execute(query, values)
                connect.commit()
                print("Data inserted successfully!")
            print('Data exist in the table')
    except:
        print("Data insertion Error")
        return None
    finally:
        connect.close()

