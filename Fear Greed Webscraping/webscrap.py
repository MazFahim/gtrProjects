import requests
from bs4 import BeautifulSoup
from datetime import date
import transaction


def getMeterValue():
    url = 'https://feargreedmeter.com/' 
    today = date.today()
    response = requests.get(url)
    if response.status_code == 200:
        soup = BeautifulSoup(response.text, 'html.parser')
        #div_class = 'element element--intraday'
        div_class = 'bg-[#303032] rounded p-4'
        target_div = soup.find(class_=div_class)
        if target_div:
            nested_divs = target_div.find_all('div')
            
            for div in nested_divs:
                text = div.get_text().strip()
                if text.isdigit():
                    value = int(text)
                    break
            print(today)
            print(value)
            transaction.fncInsertMeterIntoTable(today, value)
        else:
            print(f"Data not found on the page.")
    else:
        print(f"Failed to retrieve the webpage. Status code: {response.status_code}")

getMeterValue()