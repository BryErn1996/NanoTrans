﻿using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace NanoTrans
{
    /// <summary>
    /// Interaction logic for WinHelp.xaml
    /// </summary>
    public partial class WinHelp : Window
    {
        public WinHelp()
        {
            InitializeComponent();
            richTextBox1.AppendText("Klávesové zkratky\nTab... přehrání/zastavení audio (video) souboru\rCtrl+Tab... Přehrání vybrané části souboru dokola\rAlt+Left... posun audio souboru o 1s zpet\rAlt+Right... posun audio souboru o 1s vpred\n");
            richTextBox1.AppendText("Ctrl+N... nový přepis\rCtrl+O... otevře soubor s přepisem\rCtrl+S... uloží soubor s přepisem\r");
            richTextBox1.AppendText("Ctrl+M... otevře nové okno, kde lze vytvořit nového mluvčího, který se v přepisu vyskytuje a nastavit ho aktuálnímu elementu\r");
            //richTextBox1.AppendText("Ctrl+R... otevře nové okno, ve kterém lze vybrat některý ze zvuků, které se v přepisovaném audio souboru mohou vyskytovat\r");
            richTextBox1.AppendText("F2... nová kapitola přepisu\rF3... nová sekce přepisu\rShift+F3... nová sekce přepisu na aktuální pozici\rShift+Del... smaže aktuální kapitolu, sekci nebo odstavec\r");
            richTextBox1.AppendText("Ctrl+Del... smaže počáteční časový index aktuálního elementu\rCtrl+Home... nastaví poč. časový index daného elementu podle pozice kurzoru\rCtrl+End... nastaví koncový časový index daného elementu podle pozice kurzoru\r");
            richTextBox1.AppendText("Alt+Enter...Maximalizace formuláře\r");
            richTextBox1.AppendText("F5...Automatické přepsání vybraného elementu přepisu\r");
            richTextBox1.AppendText("F6...Možnost diktování\r");
            richTextBox1.AppendText("F7...Hlasové ovládání programu\r");
            richTextBox1.AppendText("F9...Normalizace textu pro fonetický přepis\r");
            richTextBox1.AppendText("F10...Fonetický přepis pomocí HTK\r");
            richTextBox1.AppendText("F12...pořízení obrázku mluvčího z video souboru\r");
            richTextBox1.AppendText("Ctrl+klik myši v textu...skok kurzoru ve zvukovém signálu\r");
            richTextBox1.AppendText("Shift+táhnutí myší v signálu...změna hranic mluvčích a povolení překryvu\r");
            richTextBox1.AppendText("F1...Nápověda - klávesové zkratky\r");
            richTextBox1.AppendText("Ctrl+F1...Nápověda - O programu\r");
            





        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            try
            {
                Window1.spustenoOknoNapovedy = false;
            }
            catch
            {

            }
        }
    }
}
