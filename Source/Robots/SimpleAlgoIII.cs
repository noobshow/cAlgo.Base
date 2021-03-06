//================================================================================
//                                                                  SimpleWeekRobo
// Start at Week start(Day start 00:00)
// (M15-M30 regression(2000,2,3)(1.8,2,2000))
// 100Pips levels, suport or resistance
// Simple Monday-Friday script without any security with close when earn
// If CloseWhenEarn == 0 then dont close positions
// If TP or SL == 0 dont modifing positions
// If TrailingStop > 0  ==> BackStop == 0
// Multi positions yes or no
//================================================================================
using System;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Requests;
using cAlgo.Indicators;

namespace cAlgo.Robots
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.FullAccess)]
    public class Fire : Robot
    {
//================================================================================
//                                                                    Parametrs
//================================================================================

        private Position _position;
        private double WeekStart;
        private double LastProfit = 0;
        private double StopMoneyLoss = 0;

        [Parameter(DefaultValue = true)]
        public bool SetBUY { get; set; }

        [Parameter(DefaultValue = true)]
        public bool SetSELL { get; set; }

        [Parameter(DefaultValue = 300000, MinValue = 1000)]
        public int Volume { get; set; }

        [Parameter(DefaultValue = true)]
        public bool MultiVolume { get; set; }

        [Parameter(DefaultValue = 10, MinValue = 5)]
        public int Spacing { get; set; }

        [Parameter(DefaultValue = 30, MinValue = 1)]
        public int HowMuchPositions { get; set; }

        [Parameter("TakeProfitPips", DefaultValue = 0, MinValue = 0)]
        public int TP { get; set; }

        [Parameter("StopLossPips", DefaultValue = 0, MinValue = 0)]
        public int SL { get; set; }

        [Parameter(DefaultValue = 0, MinValue = 0)]
        public int BackStop { get; set; }

        [Parameter(DefaultValue = 5, MinValue = 0)]
        public int BackStopValue { get; set; }

        [Parameter(DefaultValue = 0, MinValue = 0)]
        public int TrailingStop { get; set; }

        // close when earn
        [Parameter(DefaultValue = 0, MinValue = 0)]
        public int CloseWhenEarn { get; set; }

        // safe deposit works when earn 100%
        [Parameter(DefaultValue = false)]
        public bool MoneyStopLossOn { get; set; }

        [Parameter(DefaultValue = 1000, MinValue = 100)]
        public int StartDeposit { get; set; }

        [Parameter(DefaultValue = 50, MinValue = 10, MaxValue = 100)]
        public int StopLossPercent { get; set; }

        [Parameter(DefaultValue = 2, MinValue = 1, MaxValue = 100)]
        public int OnWhenDepositX { get; set; }


        private int Multi = 0;
        private int VolumeBuy = 0;
        private int VolumeSell = 0;
//================================================================================
//                                                                      OnStart
//================================================================================
        protected override void OnStart()
        {
            VolumeSell = Volume;
            VolumeBuy = Volume;
            WeekStart = Symbol.Ask;

            // set pending up(BUY)
            if (SetBUY)
            {
                for (int i = 1; i < HowMuchPositions; i++)
                {

                    Multi = Multi + Spacing;
                    if (MultiVolume == true)
                    {
                        if (Multi > 100)
                        {
                            VolumeBuy = VolumeBuy + Volume;
                        }
                        if (Multi > 200)
                        {
                            VolumeBuy = VolumeBuy + Volume;
                        }
                        if (Multi > 300)
                        {
                            VolumeBuy = VolumeBuy + Volume;
                        }
                    }
                    PlaceStopOrder(TradeType.Buy, Symbol, VolumeBuy, Symbol.Ask + Spacing * i * Symbol.PipSize);
                }
            }

            // set pending down(SELL)
            if (SetSELL)
            {
                Multi = 0;
                for (int j = 1; j < HowMuchPositions; j++)
                {

                    Multi = Multi + Spacing;
                    if (MultiVolume == true)
                    {
                        if (Multi > 100)
                        {
                            VolumeSell = VolumeSell + Volume;
                        }
                        if (Multi > 200)
                        {
                            VolumeSell = VolumeSell + Volume;
                        }
                        if (Multi > 300)
                        {
                            VolumeSell = VolumeSell + Volume;
                        }
                    }
                    PlaceStopOrder(TradeType.Sell, Symbol, VolumeSell, Symbol.Bid - Spacing * j * Symbol.PipSize);
                }
            }
        }

//================================================================================
//                                                                       OnTick
//================================================================================
        protected override void OnTick()
        {
            var netProfit = 0.0;

            // Take profit
            if (TP > 0)
            {
                foreach (var position in Positions)
                {

                    // Modifing position tp
                    if (position.TakeProfit == null)
                    {
                        Print("Modifying {0}", position.Id);
                        ModifyPosition(position, position.StopLoss, GetAbsoluteTakeProfit(position, TP));
                    }

                }

            }

            // Stop loss 
            if (SL > 0)
            {
                foreach (var position in Positions)
                {

                    // Modifing position sl and tp
                    if (position.StopLoss == null)
                    {
                        Print("Modifying {0}", position.Id);
                        ModifyPosition(position, GetAbsoluteStopLoss(position, SL), position.TakeProfit);
                    }

                }

            }



            foreach (var openedPosition in Positions)
            {
                netProfit += openedPosition.NetProfit;
            }

            if (MoneyStopLossOn == true)
            {
                // safe deposit works when earn 100%
                if (LastProfit < Account.Equity && Account.Equity > StartDeposit * OnWhenDepositX)
                {
                    LastProfit = Account.Equity;
                }

                if (Account.Equity > StartDeposit * 3)
                {
                    //StopLossPercent = 90;
                }

                StopMoneyLoss = LastProfit * (StopLossPercent * 0.01);
                Print("LastProfit: ", LastProfit);
                Print("Equity: ", Account.Equity);
                Print("StopLossMoney: ", StopMoneyLoss);

                if (Account.Equity < StopMoneyLoss)
                {
                    //open orders
                    foreach (var openedPosition in Positions)
                    {
                        ClosePosition(openedPosition);
                    }
                    // pending orders
                    foreach (var order in PendingOrders)
                    {
                        CancelPendingOrder(order);
                    }
                    Stop();
                }
            }

            if (netProfit >= CloseWhenEarn && CloseWhenEarn > 0)
            {
                // open orders
                foreach (var openedPosition in Positions)
                {
                    ClosePosition(openedPosition);
                }
                // pending orders
                foreach (var order in PendingOrders)
                {
                    CancelPendingOrder(order);
                }
                Stop();
            }


            //===================================================== Back Trailing
            if (BackStop > 0)
            {
                foreach (var openedPosition in Positions)
                {
                    Position Position = openedPosition;
                    if (Position.TradeType == TradeType.Buy)
                    {
                        double distance = Symbol.Bid - Position.EntryPrice;

                        if (distance >= BackStop * Symbol.PipSize)
                        {
                            double newStopLossPrice = Math.Round(Symbol.Bid - BackStopValue * Symbol.PipSize, Symbol.Digits);

                            if (Position.StopLoss == null)
                            {
                                ModifyPosition(Position, newStopLossPrice, Position.TakeProfit);
                            }
                        }
                    }
                    else
                    {
                        double distance = Position.EntryPrice - Symbol.Ask;

                        if (distance >= BackStop * Symbol.PipSize)
                        {

                            double newStopLossPrice = Math.Round(Symbol.Ask + BackStopValue * Symbol.PipSize, Symbol.Digits);

                            if (Position.StopLoss == null)
                            {
                                ModifyPosition(Position, newStopLossPrice, Position.TakeProfit);
                            }
                        }
                    }
                }
            }

            //===================================================== Trailing
            if (TrailingStop > 0 && BackStop == 0)
            {
                foreach (var openedPosition in Positions)
                {
                    Position Position = openedPosition;
                    if (Position.TradeType == TradeType.Buy)
                    {
                        double distance = Symbol.Bid - Position.EntryPrice;

                        if (distance >= TrailingStop * Symbol.PipSize)
                        {
                            double newStopLossPrice = Math.Round(Symbol.Bid - TrailingStop * Symbol.PipSize, Symbol.Digits);

                            if (Position.StopLoss == null || newStopLossPrice > Position.StopLoss)
                            {
                                ModifyPosition(Position, newStopLossPrice, Position.TakeProfit);
                            }
                        }
                    }
                    else
                    {
                        double distance = Position.EntryPrice - Symbol.Ask;

                        if (distance >= TrailingStop * Symbol.PipSize)
                        {

                            double newStopLossPrice = Math.Round(Symbol.Ask + TrailingStop * Symbol.PipSize, Symbol.Digits);

                            if (Position.StopLoss == null || newStopLossPrice < Position.StopLoss)
                            {
                                ModifyPosition(Position, newStopLossPrice, Position.TakeProfit);
                            }
                        }
                    }
                }
            }









        }

//================================================================================
//                                                             OnPositionOpened
//================================================================================
        protected override void OnPositionOpened(Position openedPosition)
        {
            int BuyPos = 0;
            int SellPos = 0;

            foreach (var position in Positions)
            {
                // Count opened positions
                if (position.TradeType == TradeType.Buy)
                {
                    BuyPos = BuyPos + 1;
                }
                if (position.TradeType == TradeType.Sell)
                {
                    SellPos = SellPos + 1;
                }
            }

            Print("All Buy positions: " + BuyPos);
            Print("All Sell positions: " + SellPos);


        }
        // end OnPositionOpened

//================================================================================
//                                                                        OnBar
//================================================================================
        protected override void OnBar()
        {

        }
//================================================================================
//                                                             OnPositionClosed
//================================================================================
        protected override void OnPositionClosed(Position closedPosition)
        {

        }

//================================================================================
//                                                                       OnStop
//================================================================================
        protected override void OnStop()
        {

        }

//================================================================================
//                                                                     Function
//================================================================================
        private void Buy()
        {
            ExecuteMarketOrder(TradeType.Buy, Symbol, 10000, "", 1000, 1000);
        }

        private void Sell()
        {
            ExecuteMarketOrder(TradeType.Sell, Symbol, 10000, "", 1000, 1000);
        }

        private void ClosePosition()
        {
            if (_position != null)
            {
                ClosePosition(_position);
                _position = null;
            }
        }


        //================================================================================
        //                                                                modify SL and TP
        //================================================================================
        private double GetAbsoluteStopLoss(Position position, int stopLossInPips)
        {
            //Symbol Symbol = MarketData.GetSymbol(position.SymbolCode);
            return position.TradeType == TradeType.Buy ? position.EntryPrice - (Symbol.PipSize * stopLossInPips) : position.EntryPrice + (Symbol.PipSize * stopLossInPips);
        }

        private double GetAbsoluteTakeProfit(Position position, int takeProfitInPips)
        {
            //Symbol Symbol = MarketData.GetSymbol(position.SymbolCode);
            return position.TradeType == TradeType.Buy ? position.EntryPrice + (Symbol.PipSize * takeProfitInPips) : position.EntryPrice - (Symbol.PipSize * takeProfitInPips);
        }
//================================================================================
//                                                                    Robot end
//================================================================================
    }
}
