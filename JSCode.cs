using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.IO;

namespace PasWinForms
{

    public partial class Form1 : Form
    {
        static string _jsCode =
    @"


var Pas =
{

    dragCursor:""move"",
    stLeft:50,
    stTop:100,
    srcRow:0,
    imageWidth:0,
    imageHeight:0,
    currentCard:0,
    layout:null,
    offsetX:0,
    offsetY:0,
    targetObj:null,
    x:0,
    y:0,
    hist:null,
    histPos:0,
    pasStr:null,
    histStr:null,
    charTab:null,
    destRow:-1,
    nJokers:0,
    grab:false,	
    getRow:function(obj, dragBegin)
    {

        var rx = obj.clientX;
        var ry = obj.clientY;
        var row = -1;

        if (rx >= this.stLeft && rx < this.stLeft + this.imageWidth * 9)
        {
            row = Math.floor((rx - this.stLeft) / this.imageWidth);
            var y1 = this.stTop + this.imageHeight * (this.layout[row].length - 1) / 3;
            if (dragBegin != 0 && (ry < y1 || ry >= y1 + this.imageHeight))
                row = -1;
        }
        else
        {
            if (rx >= (this.stLeft + this.imageWidth * 9.5) && rx < (this.stLeft + this.imageWidth * 10.5))
            {
                if (ry >= this.stTop && ry < this.stTop + this.imageHeight * 4)
                {
                    if (dragBegin == 0)
                        row = Math.floor((ry - this.stTop) / this.imageHeight) + 9;
                }
                else
                {
                    if (ry >= this.stTop + this.imageHeight * 4 + this.imageHeight / 3 && ry < this.stTop + this.imageHeight * 6 + this.imageHeight / 3)
                        row = Math.floor((ry - (this.stTop + this.imageHeight * 4 + this.imageHeight / 3)) / this.imageHeight) + 13;
                }
            }
        }
        return row;
    },

    canPlace:function(row)
    {
        var range;
        var srcRange;
        var suit;

        this.destRow = -1;
        this.nJokers = 0;
        if (row == this.srcRow)
        {
            return false;
        }
        if (row >= 13)
        {
            if (this.currentCard >= 52)
            {
                this.destRow = this.layout[13].length == 0 ? 13 : 14;
                return true;
            }
            return false;
        }
        if (row >= 9 && row < 13 && this.currentCard < 52)
        {// Кладем в кучу
            row = (this.currentCard % 4) + 9;
            if (this.layout[row].length == 0)
                range = -1;
            else
            {
                range = this.layout[row][this.layout[row].length - 1];
                range = Math.floor(range / 4);
            }
            srcRange = Math.floor(this.currentCard / 4);
            if (srcRange - range == 1)
            {
                this.destRow = row;
                return true;
            }
            return false;
        }
        if (row >= 0 && row < 9)
        {
            var cardSuit = this.currentCard % 4 < 2 ? 0 : 1;
            var cardRange = Math.floor(this.currentCard / 4);
            // В пустой ряд кладём всё подряд, кроме джокера.	
            if (this.layout[row].length == 0)
                return this.currentCard < 52 ? true : false;

            // Джокера можно положить куда хочешь, только не в пустой ряд
            if (this.currentCard >= 52)
                return this.layout[row].length != 0;

            var underCard = this.layout[row][this.layout[row].length - 1];
            if (underCard >= 52)
            {
                underCard = this.layout[row][this.layout[row].length - 2];
                if (underCard >= 52)
                {
                    underCard = this.layout[row][this.layout[row].length - 3];
                    range = Math.floor(underCard / 4);
                    suit = underCard % 4 < 2 ? 0 : 1;
                    return suit != cardSuit && range - cardRange == 3;
                }
                else
                {
                    range = Math.floor(underCard / 4);
                    suit = underCard % 4 < 2 ? 0 : 1;
                    return suit == cardSuit && range - cardRange == 2;
                }
            }
            var dRange = Math.floor(underCard / 4) - cardRange;
            if (dRange == 1)
            {
                return (underCard % 4 < 2 ? 0 : 1) != cardSuit;
            }
            else
                if (dRange == 2)
            {
                if ((underCard % 4 < 2 ? 0 : 1) == cardSuit)
                {// Если есть хотя бы 1 джокер
                    if (this.layout[13].length != 0 || this.layout[14].length != 0)
                    {
                        this.nJokers = 1;
                        return true;
                    }
                }
                return false;
            }
            else
                    if (dRange == 3)
            {
                if ((underCard % 4 < 2 ? 0 : 1) != cardSuit)
                {// Если есть 2 джокера
                    if (this.layout[13].length != 0 && this.layout[14].length != 0)
                    {
                        this.nJokers = 2;
                        return true;
                    }
                }
            }
        }
        return false;
    },

    dropCard:function(e)
    {
        if (this.targetObj != null)
        {
            var evtobj = window.event? window.event : e;
            var row = this.getRow(evtobj, 0);
            if (row == -1)
            {// Вернуть на своё место
                row = this.srcRow;
            }
            else
                if (!this.canPlace(row))
            {
                row = this.srcRow;
            }
            else
            {
                if (this.destRow != -1)
                    row = this.destRow;
            }

            // Перемещаем джокеров
            while (this.nJokers != 0)
            {
                var jr = this.layout[13].length != 0 ? 13 : 14;
                var jc = this.layout[jr].pop();
                this.layout[row].push(jc);

                this.setCardPlace(row);

                while (this.histPos != this.hist.length)
                    this.hist.pop();
                this.hist.push(jr);
                this.hist.push(row);
                this.histPos += 2;
                this.updateButtons();
                this.makeHistStr();
                this.updateCursor(this.srcRow);

                this.nJokers--;
            }



            this.layout[row].push(this.currentCard);
            this.setCardPlace(row);
            this.targetObj = null;
            if (row != this.srcRow)
            {
                while (this.histPos != this.hist.length)
                    this.hist.pop();
                this.hist.push(this.srcRow);
                this.hist.push(row);
                this.histPos += 2;
                this.updateButtons();
                this.makeHistStr();
                this.updateCursor(this.srcRow);

                if (this.srcRow >= 0 && this.srcRow < 9)
                {
                    while (this.layout[this.srcRow].length != 0)
                    {
                        var cc = this.layout[this.srcRow].pop();
                        if (cc < 52)
                        {
                            this.layout[this.srcRow].push(cc);
                            break;
                        }
                        else
                        {
                            jr = this.layout[13].length == 0 ? 13 : 14;
                            this.layout[jr].push(cc);
                            this.setCardPlace(jr);
                            while (this.histPos != this.hist.length)
                                this.hist.pop();
                            this.hist.push(this.srcRow);
                            this.hist.push(jr);
                            this.histPos += 2;
                            this.updateButtons();
                            this.makeHistStr();
                            this.updateCursor(this.srcRow);
                        }
                    }
                }

            }
        }
    },

    updateCursor:function(row)
    {
        if (row < 9 && this.layout[row].length > 1)
        {// Установить курсор
            var img = document.images[String(this.layout[row][this.layout[row].length - 1])];
            img.style.cursor = this.dragCursor;
        }
    },


    newLayout:function()
    {
        document.images[""bk""].style.left = 0 + ""px"";
        document.images[""bk""].style.top = 0 + ""px"";
        document.images[""bk""].style.zIndex = -1;
        document.images[""bk""].width = 1400;
        document.images[""bk""].height = 1000;

        var i, row, col, card, style;
        this.hist = new Array();
        this.histPos = 0;
        this.layout = new Array(15);
        for (i = 0; i < 15; i++)
            this.layout[i] = new Array();

        var cards = new Array($Generated$);
        for (row = 0; row < 6; row++)
        {
            for (col = 0; col < 9; col++)
            {
                card = row * 9 + col;
                this.layout[col].push(cards[card]);
                style = document.images[String(cards[card])].style;
                style.top = (this.stTop + row * this.imageHeight / 3) + ""px"";
                style.left = (this.stLeft + col * this.imageWidth) + ""px"";
                style.zIndex = row + 1;
                style.cursor = row == 5 ? this.dragCursor : ""default"";
            }
        }
        for (i = 56; i < 60; i++)
        {
            style = document.images[String(i)].style;
            style.top = (this.stTop + this.imageHeight * (i - 56)) + ""px"";
            style.left = (this.stLeft + this.imageWidth * 9.5) + ""px"";
            style.zIndex = 0;
        }


        document.images[""54""].style.left = (this.stLeft + this.imageWidth * 9.5) + ""px"";
        document.images[""54""].style.top = (this.stTop + this.imageHeight * 4 + this.imageHeight / 3) + ""px"";
        document.images[""54""].style.zIndex = 0;
        document.images[""54""].style.cursor = this.dragCursor;

        document.images[""55""].style.left = (this.stLeft + this.imageWidth * 9.5) + ""px"";
        document.images[""55""].style.top = (this.stTop + this.imageHeight * 5 + this.imageHeight / 3) + ""px"";
        document.images[""55""].style.zIndex = 0;
        document.images[""55""].style.cursor = this.dragCursor;

        this.updateButtons();
        this.makeHistStr();

    },


    initialize:function()
    {
        document.onmousedown = this.onLeftButtonDown;
        document.onmouseup = this.onLeftButtonUp;
        this.imageWidth = document.images[""0""].width;
        this.imageHeight = document.images[""0""].height;
        this.charTab = new Array('0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F');
        this.newLayout();
        var par = document.images[""0""].parentNode;
        for (var i = 1; i <= 9; i++)
        {
            var d = document.createElement('DIV');
            d.style.position = 'absolute';
            d.style.left = this.stLeft + (i - 1) * this.imageWidth + ""px"";
            d.style.top = this.stTop + ""px"";
            d.style.border = ""solid 1px black"";
            d.style.width = this.imageWidth - 1 + ""px"";
            d.style.height = this.imageHeight + ""px"";
            d.style.zIndex = 0;
            par.insertBefore(d, document.images[""0""]);
        }
    },

    grabCard:function(e)
    {
        var evtobj = window.event? window.event : e
          var row = this.getRow(evtobj, 1);
        if (row == -1)
            return;
        if (this.layout[row].length == 0)
            return;

        this.targetObj = window.event ? event.srcElement: e.target
        if (this.targetObj.className == ""card"")
        {
            this.srcRow = row;

            this.targetObj.style.zIndex = 200;
            if (isNaN(parseInt(this.targetObj.style.left)))
            {
                this.targetObj.style.left = 0;
            }
            if (isNaN(parseInt(this.targetObj.style.top)))
            {
                this.targetObj.style.top = 0;
            }
            this.offsetX = parseInt(this.targetObj.style.left)
            this.offsetY = parseInt(this.targetObj.style.top)
            this.x = evtobj.clientX;
            this.y = evtobj.clientY;
            this.currentCard = this.layout[row].pop();
            if (evtobj.preventDefault)
                evtobj.preventDefault();
            else
                evtobj.returnValue = false;
            document.onmousemove = this.onMouseMove;
        }
        else
        {
            this.targetObj = null;
        }
    },

    moveCard:function(e)
    {
        var evtobj = window.event ? window.event : e
        if (this.targetObj != null)
        {
            this.targetObj.style.left = this.offsetX + evtobj.clientX - this.x + ""px"";
            this.targetObj.style.top = this.offsetY + evtobj.clientY - this.y + ""px"";
            return false;
        }
        return false;
    },

    setCardPlace:function(row)
    {
        var l, t;
        if (row < 9)
        {
            l = this.stLeft + this.imageWidth * row;
            t = this.stTop + this.imageHeight * (this.layout[row].length - 1) / 3;
        }
        else
        {
            l = this.stLeft + this.imageWidth * 9.5;
            if (row < 13)
                t = this.stTop + (row - 9) * this.imageHeight;
            else
                t = this.stTop + this.imageHeight * 4 + this.imageHeight / 3 + this.imageHeight * (row - 13);
        }
        var img = document.images[String(this.layout[row][this.layout[row].length - 1])];
        img.style.left = l + ""px"";
        img.style.top = t + ""px"";
        img.style.zIndex = this.layout[row].length;
        img.style.cursor = row >= 9 && row < 13 ? ""default"" : this.dragCursor;
        if (row < 9 && this.layout[row].length > 1)
        {// Установить курсор
            img = document.images[String(this.layout[row][this.layout[row].length - 2])];
            img.style.cursor = ""default"";
        }

    },

    goBack:function()
    {
        if (this.histPos > 1)
        {
            var card;
            var row = this.hist[this.histPos - 2];
            var prevRow = this.hist[this.histPos - 1];
            var tmpf = function(pas)
                {
                card = pas.layout[prevRow].pop();
                pas.histPos -= 2;
                pas.layout[row].push(card);
                pas.setCardPlace(row);
                pas.updateButtons();
                pas.updateCursor(prevRow);
            };
            tmpf(this);
            while (this.hist.length >= 2)
            {
                row = this.hist[this.histPos - 2];
                prevRow = this.hist[this.histPos - 1];
                if (row >= 13 && row < 17 && prevRow < 9)
                    tmpf(this);
                else
                    break;
            }

        }
    },

    goAhead:function()
    {
        while (this.histPos < this.hist.length)
        {
            var prevRow = this.hist[this.histPos];
            var row = this.hist[this.histPos + 1];
            var card = this.layout[prevRow].pop();
            this.histPos += 2;
            this.layout[row].push(card);
            this.setCardPlace(row);
            this.updateButtons();
            this.updateCursor(prevRow);
            if (card < 52)
                break;
        }
    },
    updateButtons:function()
    {
        var btn = document.getElementById(""btnBack"");
        if (btn != null)
            btn.disabled = this.histPos == 0;
        btn = document.getElementById(""btnForward"");
        if (btn != null)
            btn.disabled = this.histPos == this.hist.length;
    },
    giveUp:function()
    {
    },
    makeHistStr:function()
    {
        var histTxt = document.getElementById(""History"");
        if (histTxt != null)
        {
            this.histStr = """";
            for (var i = 0; i < this.hist.length; i++)
            {
                this.histStr += this.charTab[this.hist[i]];
            }
            histTxt.value = this.histStr;
        }
    },
    setLayout:function(str)
    {
        this.hist = new Array();
        this.histPos = 0;

        var i;
        for (i = 0; i < 9; i++)
            for (j = 0; j < 6; j++)
            {
                var index = (j * 9 + i) * 2;
                var card = Number(str.slice(index, index + 2));
            }
        i = 0;
        while (str[i] != "":"" && i < str.length)
            i++;
        while (i < str.length)
        {
            var s1 = str.charAt(i * 2);
            s1 = s1 >= 'A' ? s1 - 'A' + 10 : s1 - '0';
            var s2 = str.charAt(i * 2 + 1);
            s2 = s2 >= 'A' ? s2 - 'A' + 10 : s2 - '0';
            this.hist.push(s1);
            this.hist.push(s2);
            i += 2;
        }
        this.updateButtons();
        this.makeHistStr();
    },

    onMouseMove:function(e)
    {
        return Pas.moveCard(e);
    },
    onLeftButtonDown:function(e)
    {
        if (e.button == 0)
        {
            if (!Pas.grab)
            {
                Pas.grab = true;
                Pas.grabCard(e);
            }
        }
    },
    onLeftButtonUp:function(e)
    {
        if (e.button == 0)
        {
            if (Pas.grab)
            {
                Pas.grab = false;
                Pas.dropCard(e);
            }
        }
    },
      				
        makeLayout:function(number )
    {
        document.images[""bk""].style.left = 0 + ""px"";
        document.images[""bk""].style.top = 0 + ""px"";
        document.images[""bk""].style.zIndex = -1;
        document.images[""bk""].width = 1400;
        document.images[""bk""].height = 1000;

        var i, row, col, card, style;
        this.hist = new Array();
        this.histPos = 0;
        this.layout = new Array(15);
        for (i = 0; i < 15; i++)
            this.layout[i] = new Array();
        var cards;
        if (number == 1)
            cards = new Array(27, 15, 49, 25, 48, 37, 21, 24, 46,
                                   43, 28, 6, 12, 16, 2, 26, 9, 31, 29, 36, 50, 32, 40, 7, 3, 33, 17, 18, 22, 23, 45, 14, 11, 42, 19, 51, 38, 5, 1, 20, 10, 34, 47, 30, 44, 0, 39, 4, 41, 35, 52, 53, 8, 13);
        else
    if (number == 2)
            cards = new Array
        (15, 3, 11, 10, 7, 13, 14, 32, 0, 21, 4, 24, 48, 51, 12, 37, 26, 50, 22, 23, 16, 25, 9, 30, 6, 31, 1, 8, 2, 17, 38, 19, 49, 45, 33, 20, 5, 43, 35, 39, 41, 27, 36, 18, 34, 44, 42, 29, 46, 47, 52, 28, 53, 40);
        else
    if (number == 3)
            cards = new Array
        (9, 38, 37, 17, 50, 30, 51, 41, 42, 4, 1, 49, 21, 46, 2, 47, 44, 29, 19, 27, 35, 6, 0, 43, 14, 15, 11, 18, 22, 8, 28, 10, 26, 16, 39, 13, 20, 12, 7, 33, 3, 24, 48, 45, 36, 5, 40, 52, 32, 25, 53, 34, 23, 31);
    else
    if (number == 4)
            cards = new Array
        ($Generated$);

        for (row = 0; row < 6; row++)
        {
            for (col = 0; col < 9; col++)
            {
                card = row * 9 + col;
                this.layout[col].push(cards[card]);
                style = document.images[String(cards[card])].style;
                style.top = (this.stTop + row * this.imageHeight / 3) + ""px"";
                style.left = (this.stLeft + col * this.imageWidth) + ""px"";
                style.zIndex = row + 1;
                style.cursor = row == 5 ? this.dragCursor : ""default"";
            }
        }
        for (i = 56; i < 60; i++)
        {
            style = document.images[String(i)].style;
            style.top = (this.stTop + this.imageHeight * (i - 56)) + ""px"";
            style.left = (this.stLeft + this.imageWidth * 9.5) + ""px"";
            style.zIndex = 0;
        }


        document.images[""54""].style.left = (this.stLeft + this.imageWidth * 9.5) + ""px"";
        document.images[""54""].style.top = (this.stTop + this.imageHeight * 4 + this.imageHeight / 3) + ""px"";
        document.images[""54""].style.zIndex = 0;
        document.images[""54""].style.cursor = this.dragCursor;

        document.images[""55""].style.left = (this.stLeft + this.imageWidth * 9.5) + ""px"";
        document.images[""55""].style.top = (this.stTop + this.imageHeight * 5 + this.imageHeight / 3) + ""px"";
        document.images[""55""].style.zIndex = 0;
        document.images[""55""].style.cursor = this.dragCursor;

        this.updateButtons();
        this.makeHistStr();
    }
}
";


    }
}