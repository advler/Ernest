using System;
using System.Collections.Generic;
using System.Linq;

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
        private const decimal TOTALCASH = 10000;                //总资金

        private readonly Dictionary<Symbol, SymbolData> _sd = new Dictionary<Symbol, SymbolData>();

        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);  //Set Start Date
            SetEndDate(2013, 10, 11);    //Set End Date

            //设置总资金
            SetCash(TOTALCASH);             //Set Strategy Cash

            SetBrokerageModel(BrokerageName.InteractiveBrokersBrokerage, AccountType.Margin);

            //select stocks to be traded.
            stockSelection();


        }

        public override void OnData(Slice data)
        {

        }

        private void stockSelection()
        {
            //Add stock names with corresponding weights
            Dictionary<string, double> st = new Dictionary<string, double>();
            st.Add("EWA", 1.198);
            st.Add("EWA", -0.911);

            _sd.Clear();

            foreach (KeyValuePair<string, double> kv in st)
            {
                Equity eqt = AddEquity(kv.Key, Resolution.Daily, Market.USA);
                _sd.Add(eqt.Symbol, new SymbolData(eqt.Symbol, kv.Value, this));
            }
        }

        class SymbolData
        {
            public readonly Symbol Symbol;
            public readonly Security Security;
            public readonly double Weight;

            public decimal Quantity
            {
                get { return Security.Holdings.Quantity; }
            }

            public readonly Identity Close;
            public decimal Return;
            public decimal wt;
            public decimal orders;

            private readonly Ernest _algorithm;

            public SymbolData(Symbol symbol, double wt, Ernest algorithm)
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