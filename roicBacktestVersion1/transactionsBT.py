import config
import pymssql
import pandas as pd
import warnings


def ConnectDB(dbName='dbTrade'):
    try:
        connect = pymssql.connect(
            config.serverNameBT, config.userNameBT, config.passwordBT, dbName)
        # print('Database server connection successful.')
        return connect
    except Exception as e:
        # print(e)
        print('Database server connection failed.')
        raise(e)


def fncGetDataAsList(sqlQuery, dbName='dbTrade'):
    try:
        connect = ConnectDB(dbName)
        if connect is not None:
            cursor = connect.cursor()
            cursor.execute(sqlQuery)
            result = cursor.fetchall()
            return result
    except:
        return None
    finally:
        connect.close()


def fncGetDataAsPanda(sqlQuery, dbName='dbTrade'):
    try:
        connect = ConnectDB(dbName)
        if connect is not None:
            with warnings.catch_warnings():
                warnings.simplefilter('ignore', UserWarning)
                SQL_Query = pd.read_sql_query(sqlQuery, connect)
                df = pd.DataFrame(SQL_Query)
                # print(df) 
                return df
    except:
        return None
    finally:
        connect.close()


def fncGetDataAsDictionary(sqlQuery, dbName='dbTrade'):
    try:
        connect = ConnectDB(dbName)
        resultset = []

        if connect is not None:
            cursor = connect.cursor()

            cursor.execute(sqlQuery)
            columnNames = [column[0] for column in cursor.description]
            rows = cursor.fetchall()
            for row in rows:
                resultset.append(dict(zip(columnNames, row)))

            return resultset
    except Exception as e:
        raise(e)
    finally:
        cursor.close()
        connect.close()


def fncSaveData(sqlHeads, sqlData, dbName='dbTrade'):
    try:
        connect = ConnectDB(dbName)
        if connect is not None:
            cursor = connect.cursor()
            cursor.execute(sqlHeads, sqlData)
            connect.commit()
    except Exception as e:
        raise(e)
    finally:
        cursor.close()
        connect.close()

def fncSaveDataMany(sqlHeads, sqlData, dbName='dbTrade'):
    try:
        n=999
        for i in range(0, len(sqlData), n):
            # yield lst[i:i + n]
            chunk_Data=sqlData[i:i + n]
            fncSaveChunks(sqlHeads, chunk_Data,dbName)
    except Exception as e:
        raise(e)

def fncSaveChunks(sqlHeads, sqlData, dbName):
    try:
        connect = ConnectDB(dbName)
        if connect is not None:
            cursor = connect.cursor()
            cursor.executemany(sqlHeads, sqlData)
            connect.commit()
    except Exception as e:
        raise(e)
    finally:
        cursor.close()
        connect.close()


def fncUpdateData(sqlQuery, dbName="dbTrade"):
    try:
        connect = ConnectDB(dbName)
        if connect is not None:
            cursor = connect.cursor()
            cursor.execute(sqlQuery)
            connect.commit()
    except Exception as e:
        raise(e)
    finally:
        cursor.close()
        connect.close()


def fncProcessData(sqlQuery, dbName="dbTrade"):
    try:
        connect = ConnectDB(dbName)
        if connect is not None:
            cursor = connect.cursor()
            cursor.execute(sqlQuery)
            connect.commit()
    except Exception as e:
        raise(e)
    finally:
        cursor.close()
        connect.close()


# def fncSaveDataStackOverFlow(sqlHeads, sqlData, dbName='dbTrade'):
#     try:
#         connect = ConnectDB(dbName)
#         print("Stackoverflow fnction.")
#         if connect is not None:
#             cursor = connect.cursor()
#             cursor.executemany(sqlHeads, sqlData)
#             connect.commit()
#     except Exception as e:
#         raise(e)
#     finally:
#         cursor.close()
#         connect.close()


# insert_query = """INSERT INTO dbo.temptable(CHECK_TIME, DEVICE, METRIC, VALUE, TOWER, LOCATION, ANOMALY, ANOMALY_SCORE, ANOMALY_SEVERITY)
#             VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s)"""
# write_data = tuple(map(tuple, data_frame.values))
# cursor.executemany(insert_query, write_data)
# con.commit()
# cursor.close()
# con.close()




def fncGetDataForROICGreaterThanWACC():
    try:
        connect = ConnectDB()
        if connect is not None:
            cursor = connect.cursor()
            stored_procedure = 'prcGetROICandWaccData'
            cursor.execute(f'EXEC {stored_procedure}')
            data = cursor.fetchall()
            columns = [column[0] for column in cursor.description]
            df = pd.DataFrame(data, columns=columns)
            return df
    except Exception as e:
        print(f"Error: {str(e)}")
    finally:
        connect.close()