using System;
using System.Collections.Generic;
using System.Globalization;

namespace CodeWatcher
{
    public class TimeBox
    {
        DateTime _startDateTime;
        DateTime _endDateTime;

        DateTime _storeStartDateTime;
        DateTime _storeEndDateTime;

        public Guid UID { get; set; }
        public FileChangeProject Project { get; private set; }
        public TimeBox(DateTime start, DateTime end, FileChangeProject project)
        {
            StartDate = start;
            EndDate = end;
            Project = project;
            UID = Guid.NewGuid();
        }
        public TimeBox(DateTime start, DateTime end)
        {
            StartDate = start;
            EndDate = end;
            UID = Guid.NewGuid();
        }
        public override string ToString()
        {
            return StartDate.ToString(CultureInfo.InvariantCulture) + " - " + EndDate.ToString(CultureInfo.InvariantCulture);
        }
        static void Test()
        {
            TimeBox trA = new TimeBox(new DateTime(2001, 4, 18), new DateTime(2002, 7, 17));
            TimeBox trB = new TimeBox(new DateTime(2002, 7, 17), new DateTime(2002, 8, 17));
            // 1 day overlap expected
            trA.IntersectExclusive(trB);

            trB = new TimeBox(new DateTime(2002, 7, 18), new DateTime(2002, 8, 17));
            // no overlap
            trA.IntersectExclusive(trB);

            trB = new TimeBox(new DateTime(2001, 4, 18), new DateTime(2001, 4, 18));// one day period
            // no overlap
            trA.IntersectExclusive(trB);

        }

        static public void TestSubtract()
        {
            //      BOX: 4 / 06 / 2020 12:00:00 AM - 7 / 06 / 2020 12:00:00 AM
            //     PROJ:
            //9 / 06 / 2020 12:00:00 AM - 15 / 06 / 2020 12:00:00 AM
            //     REM:
            //15 / 06 / 2020 12:00:00 AM - 7 / 06 / 2020 12:00:00 AM
            {
                TimeBox tbFromBox = new TimeBox(new DateTime(2020, 6, 4, 0, 0, 0), new DateTime(2020, 6, 7, 0, 0, 0));
                Console.WriteLine("BOX:" + tbFromBox.ToString());
                Console.WriteLine("PROJ:");
                List<TimeBox> timeBoxes = new List<TimeBox>();
                timeBoxes.Add(new TimeBox(new DateTime(2020, 6, 9, 0, 0, 0), new DateTime(2020, 6, 15, 0, 0, 0)));
                timeBoxes.ForEach(trb => Console.WriteLine("  " + trb.ToString()));
                Console.WriteLine("REM:");
                var remainingTRBs = TimeBox.Subtract(tbFromBox, timeBoxes);
                if (remainingTRBs.Count == 0) Console.WriteLine("  Nothing");
                remainingTRBs.ForEach(trb => Console.WriteLine("  " + trb.ToString()));
            }
                   
                 
            {
                TimeBox tbFromBox = new TimeBox(new DateTime(2020, 6, 13, 0, 0, 0), new DateTime(2020, 6, 17, 0, 0, 0));
                Console.WriteLine("BOX:" + tbFromBox.ToString());
                Console.WriteLine("PROJ:");
                List<TimeBox> timeBoxes = new List<TimeBox>();
                timeBoxes.Add(new TimeBox(new DateTime(2020, 6, 9, 0, 0, 0), new DateTime(2020, 6, 21, 0, 0, 0)));
                timeBoxes.ForEach(trb => Console.WriteLine("  " + trb.ToString()));
                Console.WriteLine("REM:");
                var remainingTRBs = TimeBox.Subtract(tbFromBox, timeBoxes);
                if (remainingTRBs.Count == 0) Console.WriteLine("  Nothing");
                remainingTRBs.ForEach(trb => Console.WriteLine("  " + trb.ToString()));
            }

            {
                TimeBox tbFromBox = new TimeBox(new DateTime(2020, 6, 5, 0, 0, 0), new DateTime(2020, 6, 12, 0, 0, 0));
                Console.WriteLine("BOX:" + tbFromBox.ToString());
                Console.WriteLine("PROJ:");
                List<TimeBox> timeBoxes = new List<TimeBox>();
                timeBoxes.Add(new TimeBox(new DateTime(2020, 6, 9, 0, 0, 0), new DateTime(2020, 6, 19, 0, 0, 0)));
                timeBoxes.ForEach(trb => Console.WriteLine("  " + trb.ToString()));
                Console.WriteLine("REM:");
                var remainingTRBs = TimeBox.Subtract(tbFromBox, timeBoxes);
                if (remainingTRBs.Count == 0) Console.WriteLine("  Nothing");
                remainingTRBs.ForEach(trb => Console.WriteLine("  " + trb.ToString()));
            }



        }


        public bool Contains(DateTime dt)
        {
            return ((dt >= StartDate) && (dt < EndDate));
        }

        static internal void _sort(List<TimeBox> collection)
        {
            collection.Sort((x, y) => DateTime.Compare(x.StartDate, y.StartDate));
        }

        static TimeBox _matchingTimeBox(DateTime t0, DateTime t1, TimeBox src)
        {
            var tb = new TimeBox(t0, t1);
            tb.CopyStates(src);
            return (tb);
        }



        public void CopyStates(TimeBox src)
        {
            StartState = src.StartState;
            EndState = src.EndState;
            WholeState = src.WholeState;
        }





        public static List<TimeBox> Subtract(TimeBox tbA, TimeBox tbB)
        {
            List<TimeBox> tbListA = new List<TimeBox>();
            tbListA.Add(tbA);
            List<TimeBox> tbListB = new List<TimeBox>();
            tbListB.Add(tbB);
            return (Subtract(tbListA, tbListB));
        }
        public static List<TimeBox> Subtract(TimeBox tbA, List<TimeBox> tbListB)
        {
            List<TimeBox> tbListA = new List<TimeBox>();
            tbListA.Add(tbA);
            return (Subtract(tbListA, tbListB));
        }

        public static List<TimeBox> Subtract(List<TimeBox> tbListA, List<TimeBox> tbListB)
        {
            bool changing = true;

            // make copy
            List<TimeBox> ret = new List<TimeBox>();
            tbListA.ForEach(tbr => ret.Add(tbr.Duplicate()));

            while (changing)
            {
                changing = _subtract(ret, tbListB);
            }

            return (ret);
        }

        private static bool _subtract(List<TimeBox> tbListA, List<TimeBox> tbListB)
        {
            for (int i = 0; i < tbListA.Count; i++)
            {
                var tbA = tbListA[i];

                foreach (var tbB in tbListB)
                {
                    bool change = false;
                    var res = _subtract(tbA, tbB, ref change);
                    if (change)
                    {
                        // insert res at i 
                        if (res.Count > 0)
                            tbListA.AddRange(res);
                        // remove original
                        tbListA.Remove(tbA);
                        TimeBox._sort(tbListA);
                        return (true);

                    }
                }
            }

            return (false);
        }

        private static List<TimeBox> _subtract(TimeBox tbA, TimeBox tbB, ref bool change)
        {
            List<TimeBox> tmp = new List<TimeBox>();
            change = true;

            // 1: B after A
            // A :      [----]
            // B :             [-----]
            //Res:      [----]
            if (tbB.StartDate >= tbA.EndDate)
            {
                tmp.Add(_matchingTimeBox(tbA.StartDate, tbA.EndDate, tbA));
                change = false;
            }
            // 2: B before A
            // A :         [----]
            // B : [-----]
            //Res:         [----]
            else if (tbA.StartDate >= tbB.EndDate)
            {
                tmp.Add(_matchingTimeBox(tbA.StartDate, tbA.EndDate, tbA));
                change = false;
            }
            // 3: total delete
            // A :      [----]
            // B : [---------------]
            //Res: 
            else if (tbB.StartDate <= tbA.StartDate && tbB.EndDate >= tbA.EndDate)
            {
                // do nothing
            }
            // 4: deletes that split..
            // A : [---------------]
            // B :      [----]
            //Res: [---]      [----]
            else if (tbB.StartDate > tbA.StartDate && tbB.EndDate < tbA.EndDate)
            {
                tmp.Add(_matchingTimeBox(tbA.StartDate, tbB.StartDate, tbA));
                tmp.Add(_matchingTimeBox(tbB.EndDate, tbA.EndDate, tbA));
            }
            // 5: deletes that truncate start
            // A :     [---------------]
            // B :   [----]
            //Res:         [-----------] 
            else if (tbB.StartDate <= tbA.StartDate && tbB.EndDate > tbA.StartDate)
            {
                tmp.Add(_matchingTimeBox(tbB.EndDate, tbA.EndDate, tbA));
            }
            // 6: deletes that truncate end
            // A :     [---------------]
            // B :                  [----]
            //Res:     [-----------] 
            else if (tbB.EndDate >= tbA.EndDate && tbB.StartDate < tbA.EndDate)
            {
                tmp.Add(_matchingTimeBox(tbA.StartDate, tbB.StartDate, tbA));
            }
            //Res:      [----]
            else
            {
                // effectively no change
                tmp.Add(_matchingTimeBox(tbA.StartDate, tbA.EndDate, tbA));
                change = false;
            }

            if (tmp.RemoveAll(trb => trb.StartDate == trb.EndDate) > 0)// remove zeros
                change = true;

            return (tmp);
        }

        internal string GetSetting()
        {
            // start, end, states
            string str = _serialize(StartDate) + " " + _serialize(EndDate) + " " + StartState + " " + EndState + " " + WholeState;
            return (str);
        }

        internal static TimeBox FromSetting(string txt)
        {
            try
            {
                var part = txt.Split(" ".ToCharArray(), StringSplitOptions.None);

                DateTime dt0 = _deserialize(part[0]);
                DateTime dt1 = _deserialize(part[1]);
                TRB_STATE startState = (TRB_STATE)Enum.Parse(typeof(TRB_STATE), part[2]);
                TRB_STATE endState = (TRB_STATE)Enum.Parse(typeof(TRB_STATE), part[3]);
                TRB_STATE wholeState = (TRB_STATE)Enum.Parse(typeof(TRB_STATE), part[4]);

                TimeBox tbox = new TimeBox(dt0, dt1);
                tbox.StartState = startState;
                tbox.EndState = endState;
                tbox.WholeState = wholeState;
                return (tbox);
            }
            catch
            {
                // ignored
            }

            return (null);
        }
        private const string DateTimeOffsetFormatString = "yyyy-MM-ddTHH:mm:sszzz";

        static string _serialize(DateTime dt) { return (dt.ToString(DateTimeOffsetFormatString)); }
        static DateTime _deserialize(string str) { return (DateTime.Parse(str)); }


        public TRB_STATE StartState { get; set; }
        public TRB_STATE EndState { get; set; }
        public TRB_STATE WholeState { get; set; }
        public DateTime StartDate { get => _startDateTime; set => _startDateTime = value; }
        public DateTime EndDate { get => _endDateTime; set => _endDateTime = value; }
        public TimeSpan TimeSpan { get { return (_endDateTime - _startDateTime); } }

        public bool IntersectExclusive(TimeBox trb)
        {
            return (IntersectExclusive(trb.StartDate, trb.EndDate));
        }
        public bool IntersectExclusive(DateTime t0, DateTime t1)
        {
            var a0 = _startDateTime;
            var a1 = _endDateTime;
            var b0 = t0;
            var b1 = t1;
            return ((a0 < b1) && (b0 < a1));
        }


        public bool IntersectInclusive(TimeBox trb)
        {
            return (IntersectInclusive(trb.StartDate, trb.EndDate));
        }
        public bool IntersectInclusive(DateTime t0, DateTime t1)
        {
            var a0 = _startDateTime;
            var a1 = _endDateTime;
            var b0 = t0;
            var b1 = t1;
            return ((a0 <= b1) && (b0 <= a1));
        }


        internal List<DateTime> GetDateTimes(TRB_PART part, TRB_STATE state)
        {
            List<DateTime> ret = new List<DateTime>();
            if (part.HasFlag(TRB_PART.START) && HasState(StartState, state)) ret.Add(_startDateTime);
            if (part.HasFlag(TRB_PART.END) && HasState(EndState, state)) ret.Add(_endDateTime);
            return (ret);
        }

        internal bool HasState(TRB_STATE itemState, TRB_STATE queryState)
        {
            bool reqFlagState = !queryState.HasFlag(TRB_STATE.NOT);
            if (queryState.HasFlag(TRB_STATE.ANY) && reqFlagState) return (true);
            queryState &= ~TRB_STATE.NOT;
            return (itemState.HasFlag(queryState) == reqFlagState);
        }

        internal bool HasState(TRB_PART part, TRB_STATE queryState)
        {
            TRB_STATE itemState = GetState(part);
            return (HasState(itemState, queryState));
        }

        internal DateTime? GetDateTime(TRB_PART part, TRB_STATE state)
        {
            if (state.HasFlag(TRB_STATE.ANY)) return (GetDateTime(part));
            if (part.HasFlag(TRB_PART.START) && HasState(StartState, state)) return (_startDateTime);
            if (part.HasFlag(TRB_PART.END) && HasState(EndState, state)) return (_endDateTime);
            return (null);
        }

        internal DateTime? GetDateTime(TRB_PART part)
        {
            if (part.HasFlag(TRB_PART.START)) return (_startDateTime);
            if (part.HasFlag(TRB_PART.END)) return (_endDateTime);
            return (null);
        }
        internal DateTime? GetStoredDateTime(TRB_PART part)
        {
            if (part.HasFlag(TRB_PART.START)) return (_storeStartDateTime);
            if (part.HasFlag(TRB_PART.END)) return (_storeEndDateTime);
            return (null);
        }
        internal TRB_STATE GetState(TRB_PART part)
        {
            if (part.HasFlag(TRB_PART.START)) return (StartState);
            if (part.HasFlag(TRB_PART.END)) return (EndState);
            if (part.HasFlag(TRB_PART.WHOLE)) return (WholeState);
            return (TRB_STATE.NONE);
        }

        bool _isHere(TRB_PART where, TRB_STATE state)
        {
            return (HasState(GetState(where), state));
        }

        internal void SetDateTime(TRB_PART part, TRB_STATE state, DateTime dt)
        {
            if (part.HasFlag(TRB_PART.START) && HasState(StartState, state)) StartDate = dt;
            if (part.HasFlag(TRB_PART.END) && HasState(EndState, state)) EndDate = dt;
        }


        internal void WriteState(TRB_PART part, TRB_STATE state)
        {
            if (part.HasFlag(TRB_PART.START)) StartState = state;
            if (part.HasFlag(TRB_PART.END)) EndState = state;
            if (part.HasFlag(TRB_PART.WHOLE)) WholeState = state;
        }

        internal void WriteState(TRB_PART part, TRB_PART where, TRB_STATE whereState, TRB_STATE state)
        {
            if (whereState.HasFlag(TRB_STATE.ANY)) { WriteState(part, state); return; }
            if (_isHere(where, whereState))
            {
                if (part.HasFlag(TRB_PART.START)) StartState = state;
                if (part.HasFlag(TRB_PART.END)) EndState = state;
                if (part.HasFlag(TRB_PART.WHOLE)) WholeState = state;
            }
        }
        internal void WriteState(TRB_PART part, TRB_PART where, TRB_STATE state)
        {
            WriteState(part, where, state, state);
        }



        internal void SetState(TRB_PART part, TRB_STATE state)
        {
            if (part.HasFlag(TRB_PART.START)) StartState |= state;
            if (part.HasFlag(TRB_PART.END)) EndState |= state;
            if (part.HasFlag(TRB_PART.WHOLE)) WholeState |= state;
        }
        internal void SetState(TRB_PART part, TRB_PART where, TRB_STATE whereState, TRB_STATE state)
        {
            if (whereState.HasFlag(TRB_STATE.ANY)) { SetState(part, state); return; }
            if (_isHere(where, whereState))
            {
                if (part.HasFlag(TRB_PART.START)) StartState |= state;
                if (part.HasFlag(TRB_PART.END)) EndState |= state;
                if (part.HasFlag(TRB_PART.WHOLE)) WholeState |= state;
            }
        }
        internal void SetState(TRB_PART part, TRB_PART where, TRB_STATE state)
        {
            SetState(part, where, state, state);
        }

        internal void UnsetState(TRB_PART part, TRB_STATE state)
        {
            if (part.HasFlag(TRB_PART.START)) StartState &= ~state;
            if (part.HasFlag(TRB_PART.END)) EndState &= ~state;
            if (part.HasFlag(TRB_PART.WHOLE)) WholeState &= ~state;
        }

        internal void UnsetState(TRB_PART part, TRB_PART where, TRB_STATE whereState, TRB_STATE state)
        {
            if (whereState.HasFlag(TRB_STATE.ANY)) { UnsetState(part, state); return; }
            if (_isHere(where, whereState))
            {
                if (part.HasFlag(TRB_PART.START)) StartState &= ~state;
                if (part.HasFlag(TRB_PART.END)) EndState &= ~state;
                if (part.HasFlag(TRB_PART.WHOLE)) WholeState &= ~state;
            }
        }

        internal void UnsetState(TRB_PART part, TRB_PART where, TRB_STATE state)
        {
            UnsetState(part, where, state, state);
        }

        internal void ToggleState(TRB_PART part, TRB_STATE state)
        {
            if (part.HasFlag(TRB_PART.START)) StartState ^= state;
            if (part.HasFlag(TRB_PART.END)) EndState ^= state;
            if (part.HasFlag(TRB_PART.WHOLE)) WholeState ^= state;
        }

        internal void ToggleState(TRB_PART part, TRB_PART where, TRB_STATE whereState, TRB_STATE state)
        {
            if (whereState.HasFlag(TRB_STATE.ANY)) { ToggleState(part, state); return; }
            if (_isHere(where, whereState))
            {
                if (part.HasFlag(TRB_PART.START)) StartState ^= state;
                if (part.HasFlag(TRB_PART.END)) EndState ^= state;
                if (part.HasFlag(TRB_PART.WHOLE)) WholeState ^= state;
            }
        }
        internal void ToggleState(TRB_PART part, TRB_PART where, TRB_STATE state)
        {
            ToggleState(part, where, state, state);
        }

        internal void Process(Action<TimeBox> p)
        {
            p(this);
        }

        internal void StoreDates()
        {
            _storeStartDateTime = _startDateTime;
            _storeEndDateTime = _endDateTime;
        }

        internal void ShiftDateTime(TRB_PART part, TimeSpan spanDt)
        {
            if (part.HasFlag(TRB_PART.START)) _startDateTime = _storeStartDateTime + spanDt;
            if (part.HasFlag(TRB_PART.END)) _endDateTime = _storeEndDateTime + spanDt;
        }

        internal TimeBox Duplicate()
        {
            TimeBox trb = new TimeBox(this.StartDate, this.EndDate);
            trb.CopyStates(this);
            trb.UID = this.UID;
            return (trb);
        }

        public void ClearContainedEdits()
        {
            this.Project.Collection.RemoveAll(fci =>
            {
                bool contains = Contains(fci.DateTime);
                if (contains) //also remove from big list
                    this.Project.Table.ItemCollection.Remove(fci);
                return (contains);
            });
        }
    }




    [Flags]
    public enum TRB_STATE
    {
        NONE = 0,
        SELECTED = 1,
        ARMED = 2,
        WARNING = 4,
        SNAPPED = 8,
        SNAPPED_TO = 16,

        NOT = 32,
        ANY = 64,
    }
    [Flags]
    public enum TRB_PART
    {
        NONE = 0,
        START = 1,
        END = 2,
        WHOLE = 4
    }
    public enum TRB_KIND
    {
        MINIMUM,
        MAXIMUM,
        FIRST,
        LAST
    }

    public enum TRB_ACTION
    {
        NONE,
        BEGIN,
        MOVE,
        FINISH,
    }

}
