import pandas as pd
import pandas_datareader.data as web
#from datetime import datetime
#from johansen import Johansen
from johansen_test import *

#start = datetime(2014, 10, 1)
#end = datetime(2018, 10, 1)
#google = web.DataReader('GOOG', 'yahoo', start, end)
#tesla = web.DataReader('TSLA', 'yahoo', start, end)
#apple = web.DataReader('AAPL', 'yahoo', start, end)

#secs = ['EWA', 'EWC', 'IGE']
#secs = ['SPY', 'IVV', 'VOO']
secs = ['EWA', 'EWC']
#data = web.DataReader(secs, 'yahoo', '2010-1-1', '2013-01-31')['Adj Close']
data = web.DataReader(secs, 'yahoo', '2006-4-26', '2012-4-9')['Adj Close']

#print(data)

#jt = Johansen(data, 0)
#print(jt.johansen())

result = coint_johansen(data, 0, 1)