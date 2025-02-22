using PKHeX.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace SysBot.Pokemon
{
    public class PokeTradeDetail<TPoke> : IEquatable<PokeTradeDetail<TPoke>>, IFavoredEntry where TPoke : PKM, new()
    {
        private static readonly string TradeCountFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "trade_count.txt");

        private static int CreatedCount;

        static PokeTradeDetail()
        {
            CreatedCount = LoadTradeCount();
        }

        public bool IsFavored { get; }
        public Dictionary<string, object> Context = [];
        public readonly int Code;
        public TPoke TradeData;
        public readonly PokeTradeTrainerInfo Trainer;
        public readonly IPokeTradeNotifier<TPoke> Notifier;
        public readonly PokeTradeType Type;
        public readonly DateTime Time;
        public readonly int ID;
        public bool IsSynchronized => Type == PokeTradeType.Random;
        public bool IsRetry;
        public bool IsProcessing;
        public List<Pictocodes> LGPETradeCode;
        public readonly int BatchTradeNumber;
        public readonly int TotalBatchTrades;
        public readonly int UniqueTradeID;
        public bool IsCanceled { get; set; }
        public bool IsMysteryEgg { get; }
        public bool IsMysteryMon { get; }
        public bool IgnoreAutoOT { get; }
        public bool SetEdited { get; set; }

        public PokeTradeDetail(TPoke pkm, PokeTradeTrainerInfo info, IPokeTradeNotifier<TPoke> notifier, PokeTradeType type, int code, bool favored = false, List<Pictocodes>? lgcode = null, int batchTradeNumber = 0, int totalBatchTrades = 0, bool isMysteryMon = false, bool isMysteryEgg = false, int uniqueTradeID = 0, bool ignoreAutoOT = false, bool setEdited = false)
        {
            ID = GetNextTradeID();
            Code = code;
            TradeData = pkm;
            Trainer = info;
            Notifier = notifier;
            Type = type;
            Time = DateTime.Now;
            IsFavored = favored;
            LGPETradeCode = lgcode ?? new List<Pictocodes>();
            BatchTradeNumber = batchTradeNumber;
            TotalBatchTrades = totalBatchTrades;
            IsMysteryEgg = isMysteryEgg;
            IsMysteryMon = isMysteryMon;
            UniqueTradeID = uniqueTradeID;
            IgnoreAutoOT = ignoreAutoOT;
            SetEdited = setEdited;
        }

        private static int LoadTradeCount()
        {
            try
            {
                if (!File.Exists(TradeCountFile))
                {
                    Console.WriteLine("Trade count file not found. Creating a new one...");
                    File.WriteAllText(TradeCountFile, "0"); // Create the file with initial count
                    return 0;
                }

                string content = File.ReadAllText(TradeCountFile);
                if (int.TryParse(content, out int count))
                    return count;

                Console.WriteLine("Invalid trade count file format. Resetting to 0.");
                File.WriteAllText(TradeCountFile, "0"); // Reset if invalid content
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling trade count file: {ex.Message}");
            }
            return 0;
        }


        private static int GetNextTradeID()
        {
            int newCount = Interlocked.Increment(ref CreatedCount);

            try
            {
                File.WriteAllText(TradeCountFile, newCount.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing trade count file: {ex.Message}");
            }

            return newCount;
        }

        public void TradeInitialize(PokeRoutineExecutor<TPoke> routine) => Notifier.TradeInitialize(routine, this);
        public void TradeSearching(PokeRoutineExecutor<TPoke> routine) => Notifier.TradeSearching(routine, this);
        public void TradeCanceled(PokeRoutineExecutor<TPoke> routine, PokeTradeResult msg) => Notifier.TradeCanceled(routine, this, msg);
        public virtual void TradeFinished(PokeRoutineExecutor<TPoke> routine, TPoke result) => Notifier.TradeFinished(routine, this, result);
        public void SendNotification(PokeRoutineExecutor<TPoke> routine, string message) => Notifier.SendNotification(routine, this, message);
        public void SendTrainerInfo(PokeRoutineExecutor<TPoke> routine, string message) => Notifier.SendNotification(routine, this, message);
        public void SendNotification(PokeRoutineExecutor<TPoke> routine, PokeTradeSummary obj) => Notifier.SendNotification(routine, this, obj);
        public void SendNotification(PokeRoutineExecutor<TPoke> routine, TPoke obj, string message) => Notifier.SendNotification(routine, this, obj, message);

        public bool Equals(PokeTradeDetail<TPoke>? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Trainer.ID == other.Trainer.ID && UniqueTradeID == other.UniqueTradeID;
        }

        public override bool Equals(object? obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((PokeTradeDetail<TPoke>)obj);
        }

        public override int GetHashCode() => HashCode.Combine(Trainer.ID, UniqueTradeID);
        public override string ToString() => $"{Trainer.TrainerName} - {Code}";

        public string Summary(int queuePosition)
        {
            if (TradeData.Species == 0)
                return $"{queuePosition:00}: {Trainer.TrainerName}";
            return $"{queuePosition:00}: {Trainer.TrainerName}, {(Species)TradeData.Species}";
        }
    }

    public enum Pictocodes
    {
        Pikachu,
        Eevee,
        Bulbasaur,
        Charmander,
        Squirtle,
        Pidgey,
        Caterpie,
        Rattata,
        Jigglypuff,
        Diglett
    }
}
