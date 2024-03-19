# pip install sqlalchemy
# pip install yfinance
# Azure Setup of App configuration: https://learn.microsoft.com/en-us/python/api/overview/azure/appconfiguration-readme?view=azure-python
# Azure config connection R/W: Endpoint=https://
# Azure config connection R/O: Endpoint=https://
# Calculate VWAP:    https://www.yahoo.com/now/volume-weighted-average-price-vwap-204423899.html

from azure.appconfiguration import AzureAppConfigurationClient, ConfigurationSetting
from datetime import date

import json
import sqlalchemy
import yfinance as yf

az_config_connection = "Endpoint=https://"
az_config_client = AzureAppConfigurationClient.from_connection_string(az_config_connection)
sql_database = 'ShowcaseData'
sql_driver = "ODBC+DRIVER+17+for+SQL+Server"
sql_passwd = ''
sql_server = ''
sql_username = ''
date_range = "1d"

class StockInfo:
    def __init__(self, ticker, as_of_date, opening_price, closing_price, low_price, high_price, volume):
        self.ticker = ticker
        self.as_of_date = as_of_date
        self.opening_price = opening_price
        self.closing_price = closing_price
        self.low_price = low_price
        self.high_price = high_price
        self.volume = volume

    def __str__(self):
        return f"  Ticker: {self.ticker}, AsOfDate: {self.as_of_date}, OpeningPrice: {self.opening_price}, ClosingPrice: {self.closing_price}, LowPrice: {self.low_price}, HighPrice: {self.high_price}, Volume: {self.volume}"
        #return self.__dict__

    def __repr__(self):
        return self.__str__()


def prompt_for_stock_ticker():
    stock_ticker = input("  Enter a Stock Ticker: ")
    return stock_ticker


def get_formatted_as_of_date(as_of_date):
    return as_of_date.strftime("%Y-%m-%d")


def get_db_engine_with_alchemy():
    print(f"  START - {get_db_engine_with_alchemy.__name__}")
    sql_server = az_config_client.get_configuration_setting(key="dev-sql-server", label="dev-sql-server").value
    sql_username = az_config_client.get_configuration_setting(key="dev-sql-username", label="dev-sql-username").value
    sql_passwd = az_config_client.get_configuration_setting(key="dev-sql-passwd", label="dev-sql-passwd").value
    if sql_server == '' or sql_username == '' or sql_passwd == '':
        print("    ERROR: Unable to get SQL Server settings from Azure Configuration")
        exit(1)
    print(f"    SQL Server from Azure Configuration: {sql_server}")

    db_engine_statement = 'mssql+pyodbc://{}:{}@{}/{}?driver={}'.format(sql_username,
                                                            sql_passwd,
                                                            sql_server,
                                                            sql_database,
                                                            sql_driver)
    db_engine = sqlalchemy.create_engine(db_engine_statement)

    print(f"    END - {get_db_engine_with_alchemy.__name__}")
    return db_engine


def upsert_stock_info(db_engine, stock_info):
    print(f"  START - {upsert_stock_info.__name__}")
    sql_table_stockinfo = 'StockInfo'

    db_connection = db_engine.connect()

    # Check if StockInfo row exists for Ticker and AsOfDate
    select_statement = sqlalchemy.text(f"SELECT Id FROM {sql_table_stockinfo} WHERE Ticker='{stock_info.ticker}' AND AsOfDate='{stock_info.as_of_date}'")
    with db_connection.connect() as current_db_connection:
        select_result = current_db_connection.execute(select_statement).fetchall()
        #print(select_result)
        selected_row_count = len(select_result)
        print(f"    Found {selected_row_count} rows from {sql_table_stockinfo}")

    if selected_row_count == 0:
        # Insert new row into StockInfo
        insert_statement = f"INSERT INTO {sql_table_stockinfo} (Ticker,AsOfDate,OpeningPrice,ClosingPrice,LowPrice,HighPrice,Volume) VALUES ('{stock_info.ticker}','{stock_info.as_of_date}',{stock_info.opening_price},{stock_info.closing_price},{stock_info.low_price},{stock_info.high_price},{stock_info.volume})"
        with db_connection.connect() as current_db_connection:
            insert_result = current_db_connection.execute(insert_statement)
            inserted_row_count = insert_result.rowcount
            print(f"    Inserted {inserted_row_count} rows into {sql_table_stockinfo}")
            current_db_connection.close()
    else:
        # Update the existing StockInfo row
        update_statement = sqlalchemy.text(f"UPDATE {sql_table_stockinfo} SET OpeningPrice = {stock_info.opening_price}, ClosingPrice = {stock_info.closing_price}, LowPrice = {stock_info.low_price}, HighPrice = {stock_info.high_price}, Volume = {stock_info.volume}, LastUpdated = GETDATE() WHERE Ticker='{stock_info.ticker}' AND AsOfDate='{stock_info.as_of_date}'")
        with db_connection.connect() as current_db_connection:
            update_result = current_db_connection.execute(update_statement)
            updated_row_count = update_result.rowcount
            print(f"    Updated {updated_row_count} rows in {sql_table_stockinfo}")

    # Working select statement - to get row data
    with db_connection.connect() as current_db_connection:
        for row in current_db_connection.execute(select_statement):
            print(f"      {sql_table_stockinfo}.Id: {row.Id}")

    print(f"    END - {upsert_stock_info.__name__}")


def get_stock_price(stock_ticker):
    # Get the stock data from Yahoo Finance API
    lookup_stock = yf.Ticker(stock_ticker)
    #print(lookup_stock.info)
    #print(lookup_stock.get_recommendations())

    stock_history = lookup_stock.history(period=date_range)
    #print(stock_history)
    if (len(stock_history) == 0):
        print("  No stock_history data found for stock_ticker: " + stock_ticker)
        return None
    else :
        print("  Found stock_history data for stock_ticker: " + stock_ticker)
        #stock_history_as_json = stock_history.to_json()
        #print(stock_history_as_json)
        stock_history_as_dict = stock_history.to_dict()
        #print(stock_history_as_dict)

        return stock_history_as_dict


def main():
    print(f"  START - {main.__name__}")

    stock_ticker = prompt_for_stock_ticker()
    stock_history = get_stock_price(stock_ticker)
    if (stock_history is not None):
        #print(stock_history)
        as_of_date_formatted = ""
        for item in stock_history['Open']:
            as_of_date = item
            as_of_date_formatted = get_formatted_as_of_date(as_of_date)

        stock_info = StockInfo(stock_ticker, as_of_date_formatted,
            next(iter(stock_history['Open'].values())),
            next(iter(stock_history['Close'].values())),
            next(iter(stock_history['Low'].values())),
            next(iter(stock_history['High'].values())),
            next(iter(stock_history['Volume'].values())))
        print(f"    stock_info.ticker: {stock_info.ticker}")
        print(f"    stock_info.as_of_date: {stock_info.as_of_date}")
        print(f"    stock_info.opening_price: {stock_info.opening_price}")
        print(f"    stock_info.volume: {stock_info.volume}")
        #print(stock_info.__str__)

    db_engine = get_db_engine_with_alchemy()
    upsert_stock_info(db_engine, stock_info)

    print(f"    END - {main.__name__}")


# Call main function
if __name__ == "__main__":
    print("Stock data collection program - version 0.1")
    main()

# End of program
