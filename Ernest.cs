using System;
using System.Collections.Generic;
using System.Linq;
using NodaTime;

using QuantConnect.Securities;
using QuantConnect.Data.Market;
using QuantConnect.Brokerages;
using QuantConnect.Indicators;
using QuantConnect.Securities.Equity;

namespace QuantConnect.Algorithm.CSharp
{
    /* 
     * The algo is based on pair trading EWA and EWC. 
       I assigned a pre-defined weight to EWA and EWC in the algo in context.evec, 
       which means if I order 0.943 share of EWA, I will order -0.822 share of EWC, and vice versa. 
       Then I look at the combined price of the EWA/EWC pair weighted by their weights in the context.evec, 
       which would be a semi-stationary price. I will have -h*context.unit_shares of the EWA/EWC pair, 
       where h is the z-score of the combined EWA/EWC pair price. 
       So this basically a mean-reversion strategy trading on the spread between EWA and EWC.
       
       The weights come from eigenvectors of Johansen Test. See johansen_test.py for details.
    */
    public class Ernest : QCAlgorithm
    {
        //const values
        private const decimal TOTALCASH = 25000;                //total capital
        private const int HS = 28;                               //history span
        private const decimal USHARE = 1000;                     //Share unit

        private readonly Dictionary<Symbol, SymbolData> _sd = new Dictionary<Symbol, SymbolData>();

        public override void Initialize()
        {
            SetStartDate(2006, 5, 1);  //Set Start Date
            SetEndDate(2015, 5, 1);    //Set End Date

            //Set up total capital
            SetCash(TOTALCASH);             //Set Strategy Cash

            SetBrokerageModel(BrokerageName.InteractiveBrokersBrokerage, AccountType.Margin);

            //select stocks to be traded.
            stockSelection();

            DateTimeZone TimeZone = DateTimeZoneProviders.Tzdb["America/New_York"];

            Schedule.On(DateRules.EveryDay(), TimeRules.At(9, 40, TimeZone), () =>
            {
                foreach (var val in _sd.Values)
                {
                    if (!val.Security.Exchange.DateIsOpen(Time))
                        return;
                    else
                    {
                        Transactions.CancelOpenOrders(val.Symbol);      //close all open orders at the daily beginning
                        if (!val.IsReady)
                            return;
                    }
                }

                Dictionary<Symbol, IEnumerable<TradeBar>> bdic = new Dictionary<Symbol, IEnumerable<TradeBar>>();
                int count = -1;
                foreach (var val in _sd.Values)
                {
                    IEnumerable<TradeBar> bars = History<TradeBar>(val.Symbol, TimeSpan.FromDays(HS), Resolution.Daily);
                    //Debug(Time + " " + val.Symbol + " " + bars.Count());
                    if (bars.Count() != count && count != -1)
                        return;
                    count = bars.Count();
                    bdic.Add(val.Symbol, bars);

                }

                //if less than 2 days, STD will be 0.
                if (count < 2)
                    return;

                decimal[] comb_price_past_window = new decimal[count];
                for (int i = 0; i < count; i++)
                {
                    comb_price_past_window[i] = 0;
                }

                for (int i = 0; i < count; i++)
                {
                    int j = 0;
                    Nullable<DateTime> time = null;
                    foreach (KeyValuePair<Symbol, IEnumerable<TradeBar>> kv in bdic)
                    {
                        if (j > 0)
                        {
                            if (!time.Equals(kv.Value.ElementAt(i).Time))
                                return;
                        }
                        j++;
                        time = kv.Value.ElementAt(i).Time;
                        comb_price_past_window[i] = comb_price_past_window[i]
                            + _sd[kv.Key].Weight * kv.Value.ElementAt(i).Close;
                    }
                }

                decimal meanPrice = comb_price_past_window.Average();
                double sum = comb_price_past_window.Sum(d => Math.Pow((double)(d - meanPrice), 2));
                decimal stdPrice = (decimal)Math.Sqrt(sum / comb_price_past_window.Count());
                decimal comb_price = 0;

                foreach (var val in _sd.Values)
                {
                    comb_price = comb_price + val.Weight * val.Security.Close;
                }
                //Debug("Debug: " + Time + ": std: " + stdPrice);
                decimal h = (comb_price - meanPrice) / stdPrice;

                //update positions
                foreach (var val in _sd.Values)
                {
                    decimal current_position = val.Security.Holdings.Quantity;
                    //Debug("Debug: Time: " + Time + " Symbol: " + val.Symbol +
                    //" current_position:" + current_position);
                    decimal new_position = USHARE * -1 * h * val.Weight;
                    //Debug("Debug: Time: " + Time + " Symbol: " + val.Symbol +
                    //" new_position:" + current_position);
                    MarketOrder(val.Symbol, new_position - current_position);
                }
            });
        }

        private void stockSelection()
        {
            //Add stock names with corresponding weights
            Dictionary<string, decimal> st = new Dictionary<string, decimal>();
            st.Add("EWA", 1.198M);
            st.Add("EWC", -0.911M);

            _sd.Clear();

            foreach (KeyValuePair<string, decimal> kv in st)
            {
                Equity eqt = AddEquity(kv.Key, Resolution.Minute, Market.USA);
                _sd.Add(eqt.Symbol, new SymbolData(eqt.Symbol, kv.Value, this));
            }
        }

        class SymbolData
        {
            public readonly Symbol Symbol;
            public readonly Security Security;
            public readonly decimal Weight;

            public decimal Quantity
            {
                get { return Security.Holdings.Quantity; }
            }

            public readonly Identity Close;

            private readonly Ernest _algorithm;

            public SymbolData(Symbol symbol, decimal wt, Ernest algorithm)
            {
                Symbol = symbol;
                Security = algorithm.Securities[symbol];
                Weight = wt;

                Close = algorithm.Identity(symbol);

                _algorithm = algorithm;
            }

            public bool IsReady
            {
                get { return Close.IsReady; }
            }
        }

    }
}