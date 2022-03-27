using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace PasWinForms
{
    using ROW = List<byte>;

    public struct Move
    {
        public int Src;
        public int Dest;
    }

    public struct Variant
    {
        public int Src;
        public int Dest;
        public int nJokers;
    }
    public class StackItem
    {
        public List<Variant> Variants;
        public int CurrentVariant = -1;
        public StackItem()
        {
            Variants = new List<Variant>();
        }
    }


    public class ROWS : List<ROW>
    {
        public enum Flags
        { 
            bad,
            good,
            good1Joker,
            good2Joker
        }

        private int _hashCode = 0;
        Flags[] _flags;
        public void UpdateHash()
        {
            _hashCode = 0;
        }
        public ROWS()
        {
        }
        public void Prepare()
        {
            _flags = new Flags[9] { Flags.bad, Flags.bad, Flags.bad, Flags.bad, Flags.bad, Flags.bad, Flags.bad, Flags.bad, Flags.bad };
        }
        public override bool Equals(Object obj)
        {
            ROWS b = (ROWS)obj;
            if (this[9].Count != b[9].Count)
                return false;
            if (this[10].Count != b[10].Count)
                return false;
            if (this[11].Count != b[11].Count)
                return false;
            if (this[12].Count != b[12].Count)
                return false;
            for (int i = 0; i < 9; i++)
            {
                int c = this[i].Count;
                if (c != b[i].Count)
                    return false;
                for (int j = c - 1; j >= 0; j--)
                {
                    if (this[i][j] != b[i][j])
                        return false;
                }
            }
            return true;
        }
        public override int GetHashCode()
        {
            if (_hashCode == 0)
            {
                for (int j = 0; j < 9; j++)
                {
                    int c = this[j].Count;
                    if (c == 0)
                        _hashCode += 17923 * (j + 1);
                    else
                        _hashCode += (c + 1397) * (j * 4579 + 1) * (this[j][c - 1] + 15197);
                }
            }
            return _hashCode;
        }
        public ROWS Clone()
        {
            ROWS ret = new ROWS(){ Capacity = 14 };
            for (int i = 0; i < 14; i++)
                ret.Add(new ROW(this[i]));
            ret._hashCode = this._hashCode;
            return ret;
        }

        static int CardRange(int card)
        {
            return card / 4;
        }
        static int CardColor(int card)
        {
            return (card % 4) / 2;
        }

        public bool IsIdeal(int src)
        {
            //           return _flags[src] == Flags.good;
            ROW row = this[src];
            if (row.Count == 0)
                return true;
            int r = CardRange(row[0]);
            if (r == 12)
            {
                int c = CardColor(row[0]);
                for (int i = 1; i < row.Count; i++)
                {
                    int r2 = CardRange(row[i]);
                    int c2 = CardColor(row[i]);
                    if (c2 == c || r - r2 != 1)
                        return false;
                    c = c2;
                    r = r2;
                }
                return true;
            }
            return false;
        }

        public byte Get(int rowNum)
        {
            byte ret = this[rowNum].Last();
            this[rowNum].RemoveAt(this[rowNum].Count - 1);
            //if (rowNum < 9)
            //{
            //    if (_flags[rowNum] != Flags.bad)
            //    {
            //        if (this[rowNum].Count == 0)
            //            _flags[rowNum] = Flags.good;
            //        else
            //            if (this[rowNum].Count == 1 && cardRange(this[rowNum][0]) == 12)
            //                _flags[rowNum] = Flags.good;
            //    }
            //}
            return ret;
        }
        public void Put(int rowNum, byte c)
        {
            this[rowNum].Add(c);
            //if (rowNum < 9)
            //{
            //    if (_flags[rowNum] == Flags.good)
            //    {
            //        if (this[rowNum].Count == 1)
            //        {
            //            if (cardRange(this[rowNum][0]) != 12)
            //            {
            //                _flags[rowNum] = Flags.bad;
            //            }
            //        }
            //        else
            //        {
            //            if (c >= 52)
            //            {
            //                _flags[rowNum] = Flags.good1Joker;
            //            }
            //            else
            //            {
            //                int cUnder = this[rowNum][this[rowNum].Count - 2];
            //                if ((cardRange(cUnder) - cardRange(c) != 1) || (cardColor(cUnder) == cardColor(c)))
            //                    _flags[rowNum] = Flags.bad;
            //            }
            //        }
            //    }
            //}
        }
        public void Move(int src, int dest)
        {
            Put(dest, Get(src));
        }
    }


    public class HistoryItem
    {

        List<Move> _items;
        public Move PrevMove;
        public HistoryItem()
        {
            _items = new List<Move>();
        }
        public void AddStep(int src, int dest)
        {
            _items.Add(new Move() { Src = src, Dest = dest });
        }
        public void Undo(ROWS rows)
        {
            for (int i = _items.Count - 1; i >= 0; i--)
            {
                int src = _items[i].Src;
                int dest = _items[i].Dest;
                rows.Put(src, rows.Get(dest));
            }
        }
    };


    public partial class Pas
    {
        readonly object _drawLock = new object();
        int jokerRow = 13;
        public ROWS _rows;
        Stack<HistoryItem> _history;
        HashSet<ROWS> _state;
        ROWS _saveRows;
        Stack<StackItem> _stack;
        public Pas()
        {
            MaxStack = 1000000;
            _state = new HashSet<ROWS>();
            _history = new Stack<HistoryItem>();
            HistoryItem hi = new HistoryItem();
            hi.PrevMove = new Move() { Src = -1, Dest = -1 };
            _history.Push(hi);
            _stack = new Stack<StackItem>();
        }


        void AddToState()
        {
            _state.Add(_rows.Clone());
        }
        public void SafeReset()
        {
            if (_stack.Count == 0 || IsVictory())
            {
                Reset();
            }
        }

        public void Reset()
        {
            _rows = _saveRows;
            _rows.Prepare();
            _saveRows = _rows.Clone();
            _state.Clear();
            AddToState();

            _history.Clear();
            HistoryItem hi = new HistoryItem();
            hi.PrevMove = new Move() { Src = -1, Dest = -1 };
            _history.Push(hi);

            PrepareStack();
        }

        static int CardRange(int card)
        {
            return card / 4;
        }
        static int CardColor(int card)
        {
            return (card % 4) / 2;
        }
        static int CardSuit(int card)
        {
            return card % 4;
        }
        void UndoInternal()
        {
            HistoryItem hi = _history.Peek();
            hi.Undo(_rows);
            _history.Pop();
            _rows.UpdateHash();
        }
        void Undo()
        {
            UndoInternal();
        }
        public bool IsVictory()
        {
            return _rows[9].Count + _rows[10].Count + _rows[11].Count + _rows[12].Count > 52 - 9;
        }

        static int[][] _layouts = new int[][]
        {
            new int[]{27,15,49,25,48,37,21,24,46,43,28,6,12,16,2,26,9,31,29,36,50,32,40,7,3,33,17,18,22,23,45,14,11,42,19,51,38,5,1,20,10,34,47,30,44,0,39,4,41,35,52,53,8,13},
            new int[]{6,33,15,47,24,31,41,30,36,35,29,16,32,3,39,18,5,27,49,46,37,25,0,40,2,34,21,4,20,11,44,48,50,23,22,43,10,7,14,13,19,26,8,1,17,38,12,45,53,42,28,51,9,52},
            new int[]{15,3,11,10,7,13,14,32,0,21,4,24,48,51,12,37,26,50,22,23,16,25,9,30,6,31,1,8,2,17,38,19,49,45,33,20,5,43,35,39,41,27,36,18,34,44,42,29,46,47,52,28,53,40},
            new int[]{9,38,37,17,50,30,51,41,42,4,1,49,21,46,2,47,44,29,19,27,35,6,0,43,14,15,11,18,22,8,28,10,26,16,39,13,20,12,7,33,3,24,48,45,36,5,40,52,32,25,53,34,23,31},
            new int[]{10,46,43,29,36,7,11,34,37,9,2,35,4,26,27,50,17,21,40,3,25,45,47,33,8,38,16,51,14,39,42,20,13,15,24,19,18,28,0,30,1,44,31,41,12,6,52,5,32,23,48,53,22,49},
            new int[]{10,11,3,39,1,38,31,7,40,21,23,12,36,14,15,44,4,34,16,0,8,25,22,13,30,32,20,28,37,9,46,33,5,35,18,2,19,43,50,48,24,45,42,6,51,29,26,52,17,53,47,27,41,49 },
        };
        int[] MakeRandom()
        {
            Random random = new Random();
            int[] cards = new int[54];
            for (int i = 0; i < 54; i++)
            {
                cards[i] = i;
            }
            for (int i = 0; i < 54; i++)
            {
                int mod = 52;
                if (i >= 45)
                    mod = 54;
                int num = random.Next() % (mod - i) + i;
                int t = cards[i];
                cards[i] = cards[num];
                cards[num] = t;
            }
            return cards;
        }
        void PushStack()
        {
            StackItem si = new StackItem();
            MakeVariants(si.Variants);
            _stack.Push(si);
        }
        void PrepareStack()
        {
            _stack.Clear();
            PushStack();
        }
        public void Init(int nLayout, string layoutStr = "")
        {
            _rows = new ROWS();
            _rows.Prepare();

            for (int i = 0; i < 14; i++)
            {
                _rows.Add(new ROW());
            }
            for (int i = 0; i < 9; i++)
            {
                for (int j = 0; j < 6; j++)
                    _rows[i].Add(0);
            }
            int[] layout = null;
            if (layoutStr.Length == 0)
            {
                if (nLayout == 0)
                {
                    layout = MakeRandom();
                }
                else
                {
                    layout = _layouts[nLayout - 1];
                }
            }
            else
            {
                string[] strs = layoutStr.Split(new char[] { ',' });
                if (strs.Length >= 54)
                {
                    layout = new int[54];
                    for (int i = 0; i < 54; i++)
                        layout[i] = int.Parse(strs[i]);
                }
            }

            if (layout != null)
            {
                int num = 0;
                for (int i = 0; i < 6; i++)
                {
                    for (int j = 0; j < 9; j++)
                    {
                        _rows[j][i] = (byte)layout[num++];
                    }
                }
            }
            for (int j = 0; j < 9; j++)
            {
                if (_rows[j].Last() >= 52)
                {
                    _rows.Last().Add(_rows[j].Last());
                    _rows[j].RemoveAt(_rows[j].Count - 1);
                }
            }
            _rows.UpdateHash();
            _history.Clear();
            HistoryItem hi = new HistoryItem();
            hi.PrevMove = new Move() { Src = -1, Dest = -1 };
            _history.Push(hi);

            PrepareStack();
            AddToState();
            _saveRows = _rows.Clone();
        }

        static string[][] _trueCards = new string[][] 
        { 
            new string[]  {"🂡","🂢","🂣","🂤","🂥","🂦","🂧","🂨","🂩","🂪","🂫","🂭","🂮", "🃏", "🃟"},
            new string[]  {"🂱","🂲","🂳","🂴","🂵","🂶","🂷","🂸","🂹","🂺","🂻","🂽","🂾", "🃏", "🃟" },
            new string[]  {"🃁","🃂","🃃","🃄","🃅","🃆","🃇","🃈","🃉","🃊","🃋","🃍","🃎", "🃏", "🃟" },
            new string[]  {"🃑","🃒","🃓","🃔","🃕","🃖","🃗","🃘","🃙","🃚","🃛","🃝","🃞", "🃏", "🃟" }
        };



        static string[] _cards = new string[] { "A", "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K", "🃏" };
        static string[] _suits = new string[] { "\u2660", "\u2663", "\u2666", "\u2665" };
        public void Draw(Graphics g)
        {
            int fontSize = 20;
            int top = 50;
            int left = 20;
            int dx = fontSize * 5 / 2;
            int dy = fontSize * 3 / 2;
            Font f = new Font(FontFamily.GenericMonospace, fontSize);
            lock (_drawLock)
            {
                for (int i = 0; i < 9; i++)
                {
                    if (_rows.IsIdeal(i))
                    {
                        TextRenderer.DrawText(g, "g", f, new Point(i * dx + left, top - dy), Color.Blue);
                    }
    
                    for (int j = 0; j < _rows[i].Count; j++)
                    {
                        int card = _rows[i][j];
                        int range = CardRange(card);
                        int suit = CardSuit(card);
                        int color = CardColor(card);
                        string str = _cards[range];
//                        string str = _trueCards[suit][range];
                        Color clr;
                        if (card < 52)
                        {
                            str += _suits[suit];
                            clr = color == 0 ? Color.Black : Color.Red;
                        }
                        else
                        {
                            clr = Color.Green;
                        }
                        TextRenderer.DrawText(g, str, f, new Point(i * dx + left, j * dy + top), clr);
                    }
                }
                for (int i = 9; i < 13; i++)
                {
                    if (_rows[i].Count != 0)
                    {
                        int card = _rows[i].Last();
                        int range = CardRange(card);
                        int suit = CardSuit(card);
                        int color = CardColor(card);
                        string str = _cards[range] + _suits[suit];
                        Color clr = color == 0 ? Color.Black : Color.Red;
                        TextRenderer.DrawText(g, str, f, new Point(10 * dx + left, (i - 9) * dy + top), clr);
                    }
                }
                for (int j = 0; j < _rows[13].Count; j++)
                {
                    int card = _rows[13][j];
                    int range = CardRange(card);
                    int color = CardColor(card);
                    string str = _cards[range];
                    TextRenderer.DrawText(g, str, f, new Point(11 * dx + left, j * dy + top), Color.Green);
                }
                TextRenderer.DrawText(g, 
                    _stack.Count.ToString() + " "  + _state.Count.ToString(), 
                    f, new Point(12 * dx + left, top), Color.Black);
                f.Dispose();
            }
        }

        public string LayoutString
        {
            get
            {
                string str = "";
                int jok = 52;
                for (int j = 0; j < 6; j++)
                {
                    for (int i = 0; i < 9; i++)
                    {
                        int sym;
                        if (j == 5)
                        {
                            if (_saveRows[i].Count < 6)
                                sym = jok++;
                            else
                                sym = _saveRows[i][j];
                        }
                        else
                            sym = _saveRows[i][j];
                        str += sym.ToString() + ",";
                    }
                }
                str = str.Remove(str.Length - 1);
                return str;
            }
        }            


        public void Save(string fileName)
        {
            File.WriteAllText(fileName, LayoutString);
        }

        public string Result
        {
            get
            {
                string ret;
                if (IsVictory())
                    ret = "Пасьянс сходится.\n";
                else if (_stack.Count < 1)
                        ret = "Пасьянс не сходится.\n";
                else if (_state.Count >= MaxStack)
                        ret = "Превышен размер стека.\nРезультат неизвестен.\n";
                else
                    ret = "Расчёт остановлен.\nРезультат неизвестен.\n";
                return ret;
            }
        }
        public bool Break { get; set; }

        public delegate void OnStepCallback();
        public OnStepCallback OnStep;

        void MakeVariants(List<Variant> Variants)
        {
            int nJokersA;
            for (int i = 0; i < 9; i++)
            {
                if (CanMove(i, 9, out nJokersA))
                {
                    Variants.Add(new Variant() { Src = i, Dest = 9, nJokers = nJokersA });
                }
            }
            for (int src = 0; src < 9; src++)
            {
                bool freeRow = false;
                for (int dest = 0; dest < 9; dest++)
                {
                    if (dest != src)
                    {
                        if (CanMove(src, dest, out nJokersA))
                        {
                            if (_rows[dest].Count == 0)
                            {
                                if (!freeRow)
                                {
                                    freeRow = true;
                                    Variants.Add(new Variant() { Src = src, Dest = dest, nJokers = nJokersA });
                                }
                            }
                            else
                                Variants.Add(new Variant() { Src = src, Dest = dest, nJokers = nJokersA });
                        }
                    }
                }
            }
        }



        bool CanMove(int srcRow, int destRow, out int nJokers)
        {
            nJokers = 0;
            Move prevMove = _history.Peek().PrevMove;
            if (srcRow == prevMove.Dest && destRow == prevMove.Src)
                return false;
            if (_rows[srcRow].Count == 0)
                return false;
            int card = _rows[srcRow].Last();
            int range = CardRange(card);
            if (range == 0)
            { // Туз
                if (destRow > 8)
                {
                    return true;
                }
                return false;
            }
            if (destRow > 8)
            {
                int suit = CardSuit(card);
                destRow = 9 + suit;
                return range == _rows[destRow].Count;
            }
            if (_rows.IsIdeal(srcRow))
                return false;

            if (_rows[destRow].Count == 0)
            {
                return _rows[srcRow].Count > 1;
            }
            int under = _rows[destRow].Last();
            int underRange = CardRange(under);
            if (underRange < 2)
                return false;
            if (underRange - range == 1)
            {
                if (CardColor(card) != CardColor(under))
                {
                    return true;
                }
            }
            else if (underRange - range == 2)
            {
                if (_rows[jokerRow].Count == 0)
                    return false;
                if (CardColor(card) == CardColor(under))
                {
                    nJokers = 1;
                    return true;
                }
            }
            else if (underRange - range == 3)
            {
                if (_rows[jokerRow].Count < 2)
                    return false;
                if (CardColor(card) != CardColor(under))
                {
                    nJokers = 2;
                    return true;
                }
            }
            return false;
        }

        void InternalMove(Variant v)
        {
            HistoryItem hi = new HistoryItem();
            int srcRow = v.Src;
            int destRow = v.Dest;
            hi.PrevMove = new Move() { Src = srcRow, Dest = destRow };
            if (destRow > 8)
            {
                int card = _rows[srcRow].Last();
                int suit = CardSuit(card);
                destRow = 9 + suit;
            }
            for (int i = 0; i < v.nJokers; i++)
            {
                _rows.Move(jokerRow, destRow);
                hi.AddStep(jokerRow, destRow);
            }
            _rows.Move(srcRow, destRow);
            hi.AddStep(srcRow, destRow);
            if (srcRow < 9)
            {
                if (_rows[srcRow].Count > 0)
                {
                    if (_rows[srcRow].Last() > 51)
                    {
                        _rows.Move(srcRow, jokerRow);
                        hi.AddStep(srcRow, jokerRow);
                        if (_rows[srcRow].Count > 0)
                        {
                            if (_rows[srcRow].Last() > 51)
                            {
                                _rows.Move(srcRow, jokerRow);
                                hi.AddStep(srcRow, jokerRow);
                            }
                        }
                    }
                }
            }
            _history.Push(hi);
        }
        void MoveRest()
        {
            int nEmptyRows = 0;
            while (nEmptyRows < 9)
            {
                nEmptyRows = 0;
                for (int i = 0; i < 9; i++)
                {
                    if (_rows[i].Count != 0)
                    {
                        if (CanMove(i, 9, out _))
                        {
                            StackItem si = new StackItem();
                            Variant v = new Variant { Src = i, Dest = 9, nJokers = 0 };
                            si.Variants.Add(v);
                            si.CurrentVariant = 0;
                            _stack.Push(si);
                            InternalMove(v);
                        }
                        else
                        {
                            for (int j = 0; j < 9; j++)
                            {
                                if (j != i)
                                {
                                    if (CanMove(i, j, out _))
                                    {
                                        StackItem si = new StackItem();
                                        Variant v = new Variant { Src = i, Dest = j, nJokers = 0 };
                                        si.Variants.Add(v);
                                        si.CurrentVariant = 0;
                                        _stack.Push(si);
                                        InternalMove(v);
                                    }
                                }
                            }
                        }
                    }
                    else
                        nEmptyRows++;
                }
            }
        }


        bool Move(Variant v)
        {
            InternalMove(v);
            _rows.UpdateHash();
            if (_state.Contains(_rows))
            {
                UndoInternal();
                return false;
            }
            else
            {
                AddToState();
                return true;
            }
        }


        public void Run()
        {
            while (Step() && !Break)
            {
            }
            if (IsVictory())
            {
                MoveRest();
            }
            Break = false;
        }

        public void RunAnimate()
        {
            while(Step())
            {
                OnStep.Invoke();
                if (Break)
                    break;
            }
            if (IsVictory())
            {
                MoveRest();
            }
            Break = false;
        }

        public int MaxStack { get; set; }
        public bool Step()
        {
            lock (_drawLock)
            {
                while (_stack.Count > 0 && !IsVictory())
                {

                    if (_state.Count > MaxStack)
                    {
                        //MessageBox.Show("Очистка стека состояний");
                        //_state.Clear();
                        //GC.Collect();
                        //GC.WaitForPendingFinalizers();
                        return false;
                    }
                    StackItem si = _stack.Peek();
                    int num = si.CurrentVariant + 1;
                    if (num == si.Variants.Count)
                    {
                        Undo();
                        _stack.Pop();
                    }
                    else
                    {
                        si.CurrentVariant++;
                        if (Move(si.Variants[num]))
                        {
                            PushStack();
                            break;
                        }
                    }
                }
                return _stack.Count > 0 && !IsVictory();
            }
        }
        public void StepBack()
        {
            if (_stack.Count > 1 && _history.Count > 0)
            {
                _state.Remove(_rows.Clone());
                Undo();
                _stack.Pop();
                _stack.Peek().CurrentVariant--;
            }
        }
    };
}


